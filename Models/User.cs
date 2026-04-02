using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JiASsist.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        [MaxLength(35)]
        [Required]
        public string UserId { get; set; } = null!;

        [Column("username")]
        [MaxLength(35)]
        public string? Username { get; set; }

        [Column("password")]
        [MaxLength(256)]
        public string? Password { get; set; }

        [Column("email")]
        [MaxLength(256)]
        public string? Email { get; set; }

        [Column("fullname")]
        [MaxLength(256)]
        public string? Fullname { get; set; }

        [Column("project_join")]
        public string? ProjectJoin { get; set; }

        [Column("status")]
        public bool? Status { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("created_by")]
        [MaxLength(35)]
        public string? CreatedBy { get; set; }

        [Column("update_at")]
        public DateTime? UpdateAt { get; set; }

        [Column("update_by")]
        [MaxLength(35)]
        public string? UpdateBy { get; set; }
        [Column("admin_yn")]
        public bool? AdminYn { get; set; }
        [Column("pm_yn")]
        public bool? PmYn { get; set; }
        [NotMapped]
        public string? ActionType { get; set; }
    }
}
