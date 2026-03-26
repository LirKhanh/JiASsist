using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JiASsist.Models
{
    [Table("issues")]
    public class Issue
    {
        [Key]
        [Column("issue_id")]
        [MaxLength(35)]
        [Required]
        public string IssueId { get; set; } = null!;

        [Key]
        [Column("project_id")]
        [MaxLength(35)]
        [Required]
        public string ProjectId { get; set; } = null!;

        [Column("issue_name")]
        [MaxLength(256)]
        public string? IssueName { get; set; }

        [Column("issue_status")]
        [MaxLength(35)]
        public string? IssueStatus { get; set; }

        [Column("issue_type")]
        [MaxLength(35)]
        public string? IssueType { get; set; }

        [Column("issue_priority_id")]
        [MaxLength(35)]
        public string? IssuePriorityId { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("issue_attachment_id")]
        public string? IssueAttachmentId { get; set; }

        [Column("list_issues")]
        public string? ListIssues { get; set; }

        [Column("sprint_id")]
        [MaxLength(35)]
        public string? SprintId { get; set; }

        [Column("epic_id")]
        [MaxLength(35)]
        public string? EpicId { get; set; }

        [Column("issue_dev_rate")]
        public short? IssueDevRate { get; set; }

        [Column("estimate_dev")]
        public short? EstimateDev { get; set; }

        [Column("estimate_reopen_dev")]
        public short? EstimateReopenDev { get; set; }

        [Column("issue_test_rate")]
        public short? IssueTestRate { get; set; }

        [Column("estimate_test")]
        public short? EstimateTest { get; set; }

        [Column("estimate_reopen_test")]
        public short? EstimateReopenTest { get; set; }

        [Column("reporter_id")]
        [MaxLength(35)]
        public string? ReporterId { get; set; }

        [Column("assignee_id")]
        [MaxLength(35)]
        public string? AssigneeId { get; set; }

        [Column("developer_id")]
        [MaxLength(35)]
        public string? DeveloperId { get; set; }

        [Column("tester_id")]
        [MaxLength(35)]
        public string? TesterId { get; set; }

        [Column("ba_id")]
        [MaxLength(35)]
        public string? BaId { get; set; }

        [Column("cus_request_date")]
        public DateTime? CusRequestDate { get; set; }

        [Column("pm_request_date")]
        public DateTime? PmRequestDate { get; set; }

        [Column("deadline_dev")]
        public DateTime? DeadlineDev { get; set; }

        [Column("deadline_test")]
        public DateTime? DeadlineTest { get; set; }

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
