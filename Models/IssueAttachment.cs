using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JiASsist.Models
{
    [Table("issue_attachments")]
    public class IssueAttachment
    {
        [Key]
        [Column("issue_attachment_id")]
        [MaxLength(35)]
        [Required]
        public string IssueAttachmentId { get; set; } = null!;

        [Column("issue_id")]
        [MaxLength(35)]
        public string? IssueId { get; set; }

        [Column("user_id")]
        [MaxLength(35)]
        public string? UserId { get; set; }

        [Column("file_name")]
        [MaxLength(256)]
        public string? FileName { get; set; }

        [Column("file_path")]
        [MaxLength(256)]
        public string? FilePath { get; set; }

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
