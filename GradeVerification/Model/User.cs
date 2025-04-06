using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeVerification.Model
{
    public class User
    {
        [Key]
        public string Id { get; set; } // Custom User ID (e.g., USR-00001)

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; } // Hash this in production

        [Required]
        public string Role { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string FullName => $"{FirstName} {LastName}";
    }
}
