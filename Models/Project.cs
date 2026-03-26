using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JiASsist.Models
{
    [Table("projects")]
    public class Project
    {
        [Key]
        [Column("projects_id")]
        [MaxLength(35)]
        [Required]
        public string ProjectsId { get; set; } = null!;

        [Column("projects_name")]
        [MaxLength(256)]
        public string? ProjectsName { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("pm_id")]
        [MaxLength(35)]
        public string? PmId { get; set; }

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
