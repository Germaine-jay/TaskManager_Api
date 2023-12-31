﻿namespace TaskManager.Services.Configurations.Cache.Otp
{
    public record OtpCodeDto
    {
        public string Otp { get; set; }

        public int Attempts { get; set; }

        public OtpCodeDto(string otp)
        {
            Otp = otp;
            Attempts = 0;
        }
    }

    public record OtpCode
    {
        public string Otp { get; set; }

        public OtpCode(string otp)
        {
            Otp = otp;
        }
    }
}
