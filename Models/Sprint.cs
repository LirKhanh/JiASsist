using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JiASsist.Models
{
    [Table("sprints")]
    public class Sprint
    {
        [Key]
        [Column("sprint_id")]
        [MaxLength(35)]
        [Required]
        public string SprintId { get; set; } = null!;

        [Key]
        [Column("project_id")]
        [MaxLength(35)]
        [Required]
        public string ProjectId { get; set; } = null!;

        [Column("sprint_name")]
        [MaxLength(256)]
        public string? SprintName { get; set; }

        [Column("start_date")]
        public DateTime? StartDate { get; set; }

        [Column("end_date")]
        public DateTime? EndDate { get; set; }

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
