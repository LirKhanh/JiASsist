using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JiASsist.Models
{
    [Table("permissions")]
    public class Permission
    {
        [Key]
        [Column("permission_id")]
        [MaxLength(35)]
        [Required]
        public string PermissionId { get; set; } = null!;

        [Column("permission_name")]
        [MaxLength(256)]
        public string? PermissionName { get; set; }

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
