﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShutterStream.Domain.Dtos
{
    public class AuthDto
    {
        public class LoginUserDto
        {
            public required bool Successful { get; set; }
            public string SessionId { get; set; } = "";
            public string Username { get; set; } = "";
        }

        public class RegisterUserDto
        {
            public required bool Success { get; set; }
            public string? Reason { get; set; }
        }
    }
}
