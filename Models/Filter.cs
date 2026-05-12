using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JiASsist.Models
{
    [Table("filters")]
    public class Filter
    {
        [Key]
        [Column("filter_id")]
        [Required]
        public string FilterId { get; set; } = null!;

        [Column("type")]
        public string? Type { get; set; } = "system";

        [Column("strsql")]
        public string? StrSql { get; set; }

        [NotMapped]
        public string? ActionType { get; set; } // A for Add, E for Edit
    }
}
