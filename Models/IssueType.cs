using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JiASsist.Models
{
    [Table("issue_types")]
    public class IssueType
    {
        [Key]
        [Column("issue_type_id")]
        [MaxLength(35)]
        [Required]
        public string IssueTypeId { get; set; } = null!;

        [Column("issue_type_name")]
        [MaxLength(256)]
        public string? IssueTypeName { get; set; }

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
