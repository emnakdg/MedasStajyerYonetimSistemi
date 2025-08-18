using System.ComponentModel.DataAnnotations;

namespace MedasStajyerYonetimSistemi.Models
{
    public class EmailNotification
    {
        public int Id { get; set; }

        [Display(Name = "Alıcı Email")]
        [StringLength(200)]
        public string ToEmail { get; set; } = "";

        [Display(Name = "Alıcı Adı")]
        [StringLength(100)]
        public string ToName { get; set; } = "";

        [Display(Name = "Konu")]
        [StringLength(200)]
        public string Subject { get; set; } = "";

        [Display(Name = "İçerik")]
        public string Body { get; set; } = "";

        [Display(Name = "Gönderildi")]
        public bool IsSent { get; set; } = false;

        [Display(Name = "Gönderim Tarihi")]
        public DateTime? SentDate { get; set; }

        [Display(Name = "Hata Mesajı")]
        [StringLength(500)]
        public string? ErrorMessage { get; set; }

        [Display(Name = "Deneme Sayısı")]
        public int RetryCount { get; set; } = 0;

        [Display(Name = "Referans Türü")]
        [StringLength(50)]
        public string? ReferenceType { get; set; }

        [Display(Name = "Referans ID")]
        public int? ReferenceId { get; set; }

        [Display(Name = "Oluşturma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
