using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JiASsist.Models
{
    [Table("issue_change_histories")]
    public class IssueChangeHistory
    {
        [Key]
        [Column("issue_history_id")]
        [MaxLength(35)]
        [Required]
        public int IssueHistoryId { get; set; } 

        [Column("issue_id")]
        [MaxLength(35)]
        public string? IssueId { get; set; }

        [Column("user_id")]
        [MaxLength(35)]
        public string? UserId { get; set; }

        [Column("old_value")]
        public string? OldValue { get; set; }

        [Column("new_value")]
        public string? NewValue { get; set; }

        [Column("status")]
        public bool? Status { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("created_by")]
        [MaxLength(35)]
        public string? CreatedBy { get; set; }
    }
}
