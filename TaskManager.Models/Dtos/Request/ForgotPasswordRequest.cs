﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager.Models.Dtos.Request
{
    public class ForgotPasswordRequest
    {
        [Required, DataType(DataType.EmailAddress)]
        public string Email { get; set; }
    }
}
