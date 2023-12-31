﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskManager.Models.Enums;

namespace TaskManager.Models.Entities
{
    public class Notification : BaseEntity
    {
        [Required]
        public NotificationType Type { get; set; }

        [MaxLength(500)]
        public string Message { get; set; }
        public bool Read { get; set; }
        [Required]
        public DateTime Timestamp { get; set; }

        [ForeignKey("ApplicationUser")]
        public Guid? UserId { get; set; }

        public virtual ApplicationUser? User { get; set; }
    }
}
