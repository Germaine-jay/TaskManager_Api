﻿using TaskManager.Models.Dtos.Request;
using TaskManager.Models.Dtos.Response;
using TaskManager.Services.Infrastructure;

namespace TaskManager.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResponse<SuccessResponse>> RegisterUser(UserRegistrationRequest request);
        Task<SuccessResponse> ConfirmEmail(string validToken);
        Task<AuthenticationResponse> UserLogin(LoginRequest request);
        Task<SuccessResponse> ResetPassword(ResetPasswordRequest request);
        Task<ChangePasswordResponse> ForgotPassword(ForgotPasswordRequest request);
        Task<object> GoogleAuth(ExternalAuthRequest externalAuthDto);
    }
}
