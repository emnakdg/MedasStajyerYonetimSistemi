using System.ComponentModel.DataAnnotations;

namespace MedasStajyerYonetimSistemi.Models
{
    public class Intern
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Adı Soyadı zorunludur")]
        [Display(Name = "Adı Soyadı")]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        [Display(Name = "İkamet (İl/İlçe)")]
        [StringLength(100)]
        public string Address { get; set; } = "";

        [Display(Name = "Cep Telefonu")]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = "";

        [Display(Name = "Email")]
        [StringLength(100)]
        public string Email { get; set; } = "";

        [Display(Name = "Şirket")]
        [StringLength(100)]
        public string Company { get; set; } = "";

        [Display(Name = "Departman/İşletme")]
        [StringLength(150)]
        public string Department { get; set; } = "";

        [Display(Name = "Sorumlu Kişi")]
        [StringLength(100)]
        public string ResponsiblePerson { get; set; } = "";

        [Display(Name = "Sorumlu Kişi ID")]
        public string? ResponsiblePersonId { get; set; }

        [Display(Name = "Okul")]
        [StringLength(150)]
        public string School { get; set; } = "";

        [Display(Name = "Bölüm")]
        [StringLength(100)]
        public string Major { get; set; } = "";

        [Display(Name = "Staj Günleri")]
        [StringLength(100)]
        public string WorkDays { get; set; } = "";

        [Display(Name = "Acil Durumda Aranacak Kişi")]
        [StringLength(100)]
        public string EmergencyContactName { get; set; } = "";

        [Display(Name = "Acil Durumda Aranacak Kişi Telefon")]
        [StringLength(20)]
        public string EmergencyContactPhone { get; set; } = "";

        [Display(Name = "Sorumlu Okul Hocası")]
        [StringLength(100)]
        public string SchoolSupervisorName { get; set; } = "";

        [Display(Name = "Okul Hocası Telefon")]
        [StringLength(20)]
        public string SchoolSupervisorPhone { get; set; } = "";

        [Display(Name = "Staj Türü")]
        public InternshipType InternshipType { get; set; }

        [Display(Name = "İşyeri")]
        [StringLength(100)]
        public string Workplace { get; set; } = "";

        [Display(Name = "Şirket Sicil No")]
        [StringLength(20)]
        public string CompanyEmployeeNumber { get; set; } = "";

        [Display(Name = "Sicil No")]
        [StringLength(20)]
        public string EmployeeNumber { get; set; } = "";

        [Display(Name = "Cloudoffix'ten Geldi")]
        public bool IsFromCloudoffix { get; set; } = false;

        [Display(Name = "Staj Başlangıç Tarihi")]
        public DateTime? InternshipStartDate { get; set; }

        [Display(Name = "Staj Bitiş Tarihi")]
        public DateTime? InternshipEndDate { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Kayıt Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Güncelleme Tarihi")]
        public DateTime? UpdatedDate { get; set; }

        // Navigation Properties
        public virtual ApplicationUser? ResponsibleUser { get; set; }
        public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
        public virtual ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
    }
}
