using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedasStajyerYonetimSistemi.Models
{
    public class TimesheetDetail
    {
        public int Id { get; set; }

        [Required]
        public int TimesheetId { get; set; }

        [Required]
        [Display(Name = "Tarih")]
        public DateTime WorkDate { get; set; }

        [Display(Name = "Gün")]
        [StringLength(20)]
        public string DayName { get; set; } = "";

        [Display(Name = "Gün No")]
        public int DayNumber { get; set; }

        [Display(Name = "Görev Yeri")]
        public WorkLocation WorkLocation { get; set; }

        [Display(Name = "Devam")]
        public bool IsPresent { get; set; }

        [Display(Name = "Başlangıç Saati")]
        public TimeSpan? StartTime { get; set; }

        [Display(Name = "Bitiş Saati")]
        public TimeSpan? EndTime { get; set; }

        [Display(Name = "İzin Bilgisi")]
        [StringLength(200)]
        public string LeaveInfo { get; set; } = "";

        [Display(Name = "İzin Saati")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal LeaveHours { get; set; }

        [Display(Name = "Eğitim Bilgisi")]
        [StringLength(200)]
        public string TrainingInfo { get; set; } = "";

        [Display(Name = "Eğitim Saati")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal TrainingHours { get; set; }

        [Display(Name = "Yemek Yardımı")]
        public bool HasMealAllowance { get; set; }

        [Display(Name = "Açıklama")]
        [StringLength(500)]
        public string Notes { get; set; } = "";

        // Navigation Properties
        public virtual Timesheet Timesheet { get; set; } = null!;
    }
}
