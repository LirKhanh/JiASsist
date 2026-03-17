using System.ComponentModel.DataAnnotations;

namespace JiASsist.Models
{
    public class User
    {
        [Required]
        public string UserId { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }
        public string? Fullname { get; set; }
        public string? RoleId { get; set; }
        public bool? Status { get; set; }
    }
}
