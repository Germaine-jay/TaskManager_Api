﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Models.Enums;

namespace TaskManager.Models.Dtos
{
    public class ApplicationUserDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public UserType UserType { get; set; }
        public string Active { get; set; }
        public string CreatedAt { get; set; }
        public string? UpdatedAt { get; set; }
        public string EmailConfirmed { get; set; }
        public string LockedOut { get; set; }
    }
}
