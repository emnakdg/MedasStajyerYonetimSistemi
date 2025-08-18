using System.ComponentModel.DataAnnotations;

namespace MedasStajyerYonetimSistemi.Models
{
    public class ApprovalHistory
    {
        public int Id { get; set; }

        [Display(Name = "Referans Türü")]
        public ApprovalReferenceType ReferenceType { get; set; }

        [Display(Name = "Referans ID")]
        public int ReferenceId { get; set; }

        [Display(Name = "Onaylayan")]
        [StringLength(100)]
        public string ApproverName { get; set; } = "";

        [Display(Name = "Onaylayan ID")]
        public string? ApproverId { get; set; }

        [Display(Name = "Önceki Durum")]
        public ApprovalStatus PreviousStatus { get; set; }

        [Display(Name = "Yeni Durum")]
        public ApprovalStatus NewStatus { get; set; }

        [Display(Name = "Onay Notu")]
        [StringLength(500)]
        public string? ApprovalNote { get; set; }

        [Display(Name = "İşlem Tarihi")]
        public DateTime ActionDate { get; set; } = DateTime.Now;

        [Display(Name = "IP Adresi")]
        [StringLength(50)]
        public string? IpAddress { get; set; }

        // Navigation Properties
        public virtual ApplicationUser? Approver { get; set; }
    }
}
