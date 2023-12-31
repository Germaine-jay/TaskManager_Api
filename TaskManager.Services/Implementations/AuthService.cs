﻿using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using TaskManager.Models.Dtos;
using TaskManager.Models.Dtos.Request;
using TaskManager.Models.Dtos.Response;
using TaskManager.Models.Entities;
using TaskManager.Models.Enums;
using TaskManager.Services.Configurations.Cache.CacheServices;
using TaskManager.Services.Configurations.Cache.Otp;
using TaskManager.Services.Configurations.Cache.Security;
using TaskManager.Services.Infrastructure;
using TaskManager.Services.Interfaces;
using TaskManager.Services.Utilities;


namespace TaskManager.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IJwtAuthenticator _jwtAuthenticator;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IOtpService _otpService;
        private readonly ICacheService _cacheService;
        private readonly ILockoutAttempt _lockoutAttempt;


        public AuthService(IJwtAuthenticator jwtAuthenticator,
            UserManager<ApplicationUser> userManager,RoleManager<ApplicationRole> roleManager, 
            IConfiguration configuration, IEmailService emailService, IOtpService otpService, 
            ICacheService cacheService, ILockoutAttempt lockoutAttempt)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _jwtAuthenticator = jwtAuthenticator;
            _configuration = configuration;
            _emailService = emailService;
            _otpService = otpService;
            _cacheService = cacheService;
            _lockoutAttempt = lockoutAttempt;
        }


        public async Task<SuccessResponse> RegisterUser(UserRegistrationRequest request)
        {
            ApplicationUser? existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                throw new InvalidOperationException($"User already exists with Email {request.Email}");


            var verifyEmail = await _emailService.VerifyEmailAddress(request.Email);
            if (!verifyEmail)
                throw new InvalidOperationException($"Email {request.Email} is invalid");


            ApplicationUser user = new()
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                UserName = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                Active = true,
                UserType = UserType.User,
                Projects = new List<Project>(),
            };


            IdentityResult result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var message = $"Failed to create user: {(result.Errors.FirstOrDefault())?.Description}";
                throw new InvalidOperationException(message);
            }


            var registerMail = await _emailService.RegistrationMail(user);
            if (!registerMail)
                throw new InvalidOperationException($"Could not send verification mail");


            var role = UserType.User.GetStringValue();
            bool roleExists = await _roleManager.RoleExistsAsync(role);

            if (!roleExists)
            {
                ApplicationRole newRole = new ApplicationRole { Name = role };
                await _roleManager.CreateAsync(newRole);
            }

            var newUser = new ApplicationUserDto
            {
                Id = user.Id.ToString(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                UserType = user.UserType
            };

            await _userManager.AddToRoleAsync(user, role);

            return new SuccessResponse
            {
                Success = true,
                Data = newUser
            };

        }

        public async Task<SuccessResponse> ConfirmEmail(string validToken)
        {
            var (existingUser, operation) = await DecodeToken.DecodeVerificationToken(validToken);

            var user = await _userManager.FindByIdAsync(existingUser);
            if (user == null)
                throw new InvalidOperationException($"User Not Found");


            if (operation != OtpOperation.EmailConfirmation.ToString())
                throw new InvalidOperationException($"Invalid Operation");


            var verifyToken = await _otpService.VerifyUniqueOtpAsync(user.Id.ToString(), validToken, OtpOperation.EmailConfirmation);
            if (!verifyToken)
            {
                throw new InvalidOperationException($"Invalid Token");
            }

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            return new SuccessResponse
            {
                Success = true,
                Data = user
            };
        }

        public async Task<AuthenticationResponse> UserLogin(LoginRequest request)
        {
            var maxAttempt = 5;
            ApplicationUser? user = await _userManager.FindByEmailAsync(request.Email.ToLower().Trim());
            if (user == null)
                throw new InvalidOperationException("Invalid username or password");


            if (!user.Active)
                throw new InvalidOperationException("Account is not active");

            if (!user.EmailConfirmed)
                throw new InvalidOperationException("User Not Found");

            if (user.LockoutEnd != null)
                throw new InvalidOperationException($"User Suspended. Time Left {user.LockoutEnd - DateTimeOffset.UtcNow}");

            var key = await _lockoutAttempt.LoginAttemptAsync(user.Id.ToString());
            var check = await _lockoutAttempt.CheckLoginAttemptAsync(user.Id.ToString());
            if (check.Attempts == maxAttempt)
            {
                DateTimeOffset lockoutEnd = DateTimeOffset.UtcNow.AddSeconds(300);
                user.LockoutEnd = lockoutEnd;
                await _userManager.UpdateAsync(user);
                await _lockoutAttempt.ResetLoginAttemptAsync(user.Id.ToString());
                throw new InvalidOperationException($"Account locked, Time Left {user.LockoutEnd - DateTimeOffset.UtcNow}");
            }

            bool result = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!result)
            {
                check.Attempts++;
                await _cacheService.WriteToCache(key, check, null, TimeSpan.FromDays(365));
                throw new InvalidOperationException("Invalid username or password");
            }


            JwtToken userToken = await _jwtAuthenticator.GenerateJwtToken(user);
            string? userType = user.UserType.GetStringValue();

            string fullName = $"{user.LastName} {user.FirstName}";
            return new AuthenticationResponse
            {
                JwtToken = userToken,
                UserType = userType.Normalize(),
                FullName = fullName,
                TwoFactor = false,
                UserId = user.Id.ToString()
            };

        }

        public async Task<ChangePasswordResponse> ForgotPassword(ForgotPasswordRequest request)
        {

            var verify = await _emailService.VerifyEmailAddress(request.Email);
            if (verify == false)
                throw new InvalidOperationException($"Invalid Email Address");


            var user = await _userManager.FindByEmailAsync(request.Email);
            var isConfrimed = await _userManager.IsEmailConfirmedAsync(user);

            if (user == null || !isConfrimed)
                throw new InvalidOperationException($"User does not exist");

            if (user.LockoutEnd != null)
                throw new InvalidOperationException($"User Suspended. Time Left {user.LockoutEnd - DateTimeOffset.UtcNow}");


            var result = await _emailService.ResetPasswordMail(user);
            return new ChangePasswordResponse
            {
                Message = "Token sent",
                Token = result,
                Success = true
            };
        }

        public async Task<SuccessResponse> ResetPassword(ResetPasswordRequest request)
        {
            var (existingUser, operation) = await DecodeToken.DecodeVerificationToken(request.Token);

            ApplicationUser user = await _userManager.FindByIdAsync(existingUser);
            if (user == null || !user.EmailConfirmed)
                throw new InvalidOperationException($"User does not exist");


            if (operation != OtpOperation.PasswordReset.ToString())
                throw new InvalidOperationException($"Invalid Operation");


            bool isOtpValid = await _otpService.VerifyUniqueOtpAsync(user.Id.ToString(), request.Token, OtpOperation.PasswordReset);
            if (!isOtpValid)
                throw new InvalidOperationException($"Invalid Token");


            IdentityResult result = await _userManager.ChangePasswordAsync(user, request.NewPassword, request.ConfirmPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Could not complete operation");


            return new SuccessResponse
            {
                Success = true,
                Data = result,
            };
        }

        public async Task<object> GoogleAuth(ExternalAuthRequest externalAuthDto)
        {
            externalAuthDto.Provider = "Google";
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new List<string>() { _configuration["Authentication:Google:ClientId"] }
            };
            var payload = await GoogleJsonWebSignature.ValidateAsync(externalAuthDto.IdToken, settings);

            if (payload == null)
                throw new InvalidOperationException("Invalid Payload");

            var info = new UserLoginInfo(externalAuthDto.Provider, payload.Subject, externalAuthDto.Provider);
            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(payload.Email);
                if (user == null)
                {
                    var newuser = new ApplicationUser
                    {
                        Email = payload.Email,
                        UserName = payload.Email,
                        FirstName = payload.GivenName,
                        LastName = payload.FamilyName,
                        Active = true,
                        UserType = UserType.User,
                        EmailConfirmed = true,
                    };

                    await _userManager.CreateAsync(newuser);
                    var role = UserType.User.GetStringValue();

                    await _userManager.AddToRoleAsync(newuser, role);
                    await _userManager.AddLoginAsync(newuser, info);

                }
                else
                {
                    await _userManager.AddLoginAsync(user, info);
                }
            }


            var fullName = $"{user.LastName} {user.FirstName}";
            JwtToken userToken = await _jwtAuthenticator.GenerateJwtToken(user);
            return new AuthenticationResponse
            {
                JwtToken = userToken,
                UserType = user.UserType.GetStringValue().Normalize(),
                FullName = fullName,
                TwoFactor = false,
                UserId = user.Id.ToString()
            };
        }

    }
}
