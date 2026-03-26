using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JiASsist.Models
{
    [Table("role_permissions")]
    public class RolePermission
    {
        [Key]
        [Column("permission_id")]
        [MaxLength(35)]
        [Required]
        public string PermissionId { get; set; } = null!;

        [Key]
        [Column("role_id")]
        [MaxLength(35)]
        [Required]
        public string RoleId { get; set; } = null!;

        [Column("accept")]
        public bool? Accept { get; set; }

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
    }
}
