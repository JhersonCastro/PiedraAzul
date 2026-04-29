using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Contracts.DTOs
{
    public class UserDto
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Location { get; set; } = "";
        public List<string> Roles { get; set; } = new();
    }
}
