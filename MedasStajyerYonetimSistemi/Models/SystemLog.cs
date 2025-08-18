using System.ComponentModel.DataAnnotations;

namespace MedasStajyerYonetimSistemi.Models
{
    public class SystemLog
    {
        public int Id { get; set; }

        [Display(Name = "Kullanıcı")]
        [StringLength(450)]
        public string? UserId { get; set; }

        [Display(Name = "Kullanıcı Adı")]
        [StringLength(100)]
        public string? UserName { get; set; }

        [Display(Name = "İşlem")]
        [StringLength(200)]
        public string Action { get; set; } = "";

        [Display(Name = "Tablo")]
        [StringLength(100)]
        public string TableName { get; set; } = "";

        [Display(Name = "Kayıt ID")]
        public int? RecordId { get; set; }

        [Display(Name = "Eski Değer")]
        public string? OldValue { get; set; }

        [Display(Name = "Yeni Değer")]
        public string? NewValue { get; set; }

        [Display(Name = "IP Adresi")]
        [StringLength(50)]
        public string? IpAddress { get; set; }

        [Display(Name = "İşlem Tarihi")]
        public DateTime ActionDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual ApplicationUser? User { get; set; }
    }
}
