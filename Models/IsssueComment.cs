using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JiASsist.Models
{
    [Table("issue_comments")]
    public class IssueComment
    {
        [Key]
        [Column("issue_comment_id")]
        [MaxLength(35)]
        [Required]
        public int IssueCommentId { get; set; } 

        [Column("content")]
        public string? Content { get; set; }

        [Column("issue_id")]
        [MaxLength(35)]
        public string? IssueId { get; set; }

        [Column("user_id")]
        [MaxLength(35)]
        public string? UserId { get; set; }

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
