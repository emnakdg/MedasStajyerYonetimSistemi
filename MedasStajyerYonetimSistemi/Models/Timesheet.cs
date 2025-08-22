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

        // SUPERVISOR ONAY BİLGİLERİ (YENİ)
        [Display(Name = "Supervisor")]
        [StringLength(100)]
        public string? SupervisorName { get; set; }

        [Display(Name = "Supervisor ID")]
        public string? SupervisorId { get; set; }

        [Display(Name = "Supervisor Onay Tarihi")]
        public DateTime? SupervisorApprovalDate { get; set; }

        [Display(Name = "Supervisor Notu")]
        [StringLength(500)]
        public string? SupervisorNote { get; set; }

        // İK/FINAL ONAY BİLGİLERİ (MEVCUT)
        [Display(Name = "Final Onaylayan")]
        [StringLength(100)]
        public string? ApproverName { get; set; }

        [Display(Name = "Final Onaylayan ID")]
        public string? ApproverId { get; set; }

        [Display(Name = "Final Onay Tarihi")]
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Final Onay Notu")]
        [StringLength(500)]
        public string? ApprovalNote { get; set; }

        // DİĞER ALANLAR (DEĞİŞMEDİ)
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
        public virtual ApplicationUser? Approver { get; set; } // Final onaylayan
        public virtual ApplicationUser? Supervisor { get; set; } // Supervisor
        public virtual ICollection<TimesheetDetail> TimesheetDetails { get; set; } = new List<TimesheetDetail>();
        public virtual ICollection<ApprovalHistory> ApprovalHistories { get; set; } = new List<ApprovalHistory>();
    }
}
