using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JiASsist.Models
{
    [Table("workflow_step")]
    public class WorkflowStep
    {
        [Key]
        [Column("step_id")]
        [MaxLength(35)]
        [Required]
        public string StepId { get; set; } = null!;

        [Column("step_name")]
        [MaxLength(256)]
        public string? StepName { get; set; }

        [Column("step")]
        public short? Step { get; set; }

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
        [Column("pre_step_id")]
        public string? PreStepId { get; set; }
        [Column("next_step_id")]
        public string? NextStepId { get; set; }
        [NotMapped]
        public string? ActionType { get; set; }
    }
}
