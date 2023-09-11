﻿using Microsoft.AspNetCore.Identity;
using TaskManager.Models.Enums;

namespace TaskManager.Models.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public override string? PhoneNumber { get; set; }
        public UserType UserType { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public virtual ICollection<Notification>? Notifications { get; set; }
        public virtual ICollection<Project>? Projects { get; set; }
    }
}
