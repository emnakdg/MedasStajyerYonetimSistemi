using System.ComponentModel.DataAnnotations;

namespace MedasStajyerYonetimSistemi.Models
{
    public class CloudoffixSyncLog
    {
        public int Id { get; set; }

        [Display(Name = "Senkronizasyon Türü")]
        public SyncType SyncType { get; set; }

        [Display(Name = "Başlangıç Tarihi")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Display(Name = "Bitiş Tarihi")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Durum")]
        public SyncStatus Status { get; set; } = SyncStatus.InProgress;

        [Display(Name = "İşlenen Kayıt Sayısı")]
        public int ProcessedRecords { get; set; }

        [Display(Name = "Başarılı Kayıt Sayısı")]
        public int SuccessfulRecords { get; set; }

        [Display(Name = "Hatalı Kayıt Sayısı")]
        public int FailedRecords { get; set; }

        [Display(Name = "Hata Mesajı")]
        public string? ErrorMessage { get; set; }

        [Display(Name = "Detay Log")]
        public string? DetailLog { get; set; }
    }
}
