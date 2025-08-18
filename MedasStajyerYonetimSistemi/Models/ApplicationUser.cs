// MEDAŞ Stajyer Yönetim Sistemi - Veritabanı Modelleri
// SQL Server 2017+ Uyumlu - Code First Yaklaşımı

using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MedasStajyerYonetimSistemi.Models
{
    // ============================================================================
    // 1. KULLANICI YÖNETİMİ (Identity Tabanlı)
    // ============================================================================

    public class ApplicationUser : IdentityUser
    {
        [Display(Name = "Adı Soyadı")]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        [Display(Name = "Departman")]
        [StringLength(150)]
        public string Department { get; set; } = "";

        [Display(Name = "Unvan")]
        [StringLength(100)]
        public string Title { get; set; } = "";

        [Display(Name = "Sicil No")]
        [StringLength(20)]
        public string EmployeeNumber { get; set; } = "";

        [Display(Name = "Kayıt Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<LeaveRequest> ApprovedLeaveRequests { get; set; } = new List<LeaveRequest>();
        public virtual ICollection<Timesheet> ApprovedTimesheets { get; set; } = new List<Timesheet>();
    }
}