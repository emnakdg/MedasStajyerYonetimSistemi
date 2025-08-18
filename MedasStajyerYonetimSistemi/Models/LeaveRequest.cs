using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedasStajyerYonetimSistemi.Models
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        [Required]
        public int InternId { get; set; }

        [Required(ErrorMessage = "İzin türü seçilmelidir")]
        [Display(Name = "İzin Türü")]
        public LeaveType LeaveType { get; set; }

        [Required(ErrorMessage = "Başlangıç tarihi gereklidir")]
        [Display(Name = "Çıkması Gereken Tarih/Saat")]
        public DateTime StartDateTime { get; set; }

        [Required(ErrorMessage = "Bitiş tarihi gereklidir")]
        [Display(Name = "Dönmesi Gereken Tarih/Saat")]
        public DateTime EndDateTime { get; set; }

        [Display(Name = "İzin/Görev Sebebi")]
        [StringLength(500)]
        public string Reason { get; set; } = "";

        [Display(Name = "Toplam Gün")]
        public int TotalDays { get; set; }

        [Display(Name = "Toplam Saat")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal TotalHours { get; set; }

        [Display(Name = "Durum")]
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

        [Display(Name = "Onaylayan Kişi")]
        [StringLength(100)]
        public string? ApproverName { get; set; }

        [Display(Name = "Onaylayan Kişi ID")]
        public string? ApproverId { get; set; }

        [Display(Name = "Onay/Red Tarihi")]
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Onay/Red Notu")]
        [StringLength(500)]
        public string? ApprovalNote { get; set; }

        [Display(Name = "Manuel Form")]
        public bool IsManualEntry { get; set; } = false;

        [Display(Name = "Manuel Girişi Yapan")]
        public string? ManualEntryBy { get; set; }

        [Display(Name = "Puantaja Yansıt")]
        public bool ShouldReflectToTimesheet { get; set; } = true;

        [Display(Name = "Oluşturma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Güncelleme Tarihi")]
        public DateTime? UpdatedDate { get; set; }

        // Navigation Properties
        public virtual Intern Intern { get; set; } = null!;
        public virtual ApplicationUser? Approver { get; set; }
        public virtual ICollection<ApprovalHistory> ApprovalHistories { get; set; } = new List<ApprovalHistory>();
    }
}
