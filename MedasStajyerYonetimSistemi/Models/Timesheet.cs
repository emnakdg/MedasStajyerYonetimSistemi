using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedasStajyerYonetimSistemi.Models
{
    public class Timesheet
    {
        public int Id { get; set; }

        [Required]
        public int InternId { get; set; }

        [Required]
        [Display(Name = "Puantaj Dönemi (Ay/Yıl)")]
        public DateTime PeriodDate { get; set; }

        [Display(Name = "Durum")]
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

        [Display(Name = "Onaylayan Kişi")]
        [StringLength(100)]
        public string? ApproverName { get; set; }

        [Display(Name = "Onaylayan Kişi ID")]
        public string? ApproverId { get; set; }

        [Display(Name = "Onay Tarihi")]
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Onay Notu")]
        [StringLength(500)]
        public string? ApprovalNote { get; set; }

        [Display(Name = "Manuel Form")]
        public bool IsManualEntry { get; set; } = false;

        [Display(Name = "Manuel Girişi Yapan")]
        public string? ManualEntryBy { get; set; }

        [Display(Name = "Toplam Çalışma Günü")]
        public int TotalWorkDays { get; set; }

        [Display(Name = "Toplam İzin Günü")]
        public int TotalLeaveDays { get; set; }

        [Display(Name = "Toplam Eğitim Saati")]
        [Column(TypeName = "decimal(6,2)")]
        public decimal TotalTrainingHours { get; set; }

        [Display(Name = "Oluşturma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Güncelleme Tarihi")]
        public DateTime? UpdatedDate { get; set; }

        // Navigation Properties
        public virtual Intern Intern { get; set; } = null!;
        public virtual ApplicationUser? Approver { get; set; }
        public virtual ICollection<TimesheetDetail> TimesheetDetails { get; set; } = new List<TimesheetDetail>();
        public virtual ICollection<ApprovalHistory> ApprovalHistories { get; set; } = new List<ApprovalHistory>();
    }
}
