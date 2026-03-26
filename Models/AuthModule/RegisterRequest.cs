using System.ComponentModel.DataAnnotations;

namespace JiASsist.Models.AuthModule
{
    public class RegisterRequest
    {
        [Required]
        [MaxLength(35)]
        public string Username { get; set; } = string.Empty;
        [Required]
        [MaxLength(256)]
        public string Password { get; set; } = string.Empty;
        [MaxLength(256)]
        public string? Email { get; set; }
        [MaxLength(256)]
        public string? Fullname { get; set; } 
    }
}
