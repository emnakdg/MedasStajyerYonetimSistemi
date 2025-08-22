using System.ComponentModel.DataAnnotations;

namespace MedasStajyerYonetimSistemi.Models
{
    public enum InternshipType
    {
        [Display(Name = "Yaz Dönemi Stajı")]
        SummerInternship = 1,

        [Display(Name = "Talentern")]
        Talentern = 2,

        [Display(Name = "Koza Projesi")]
        KozaProject = 3,

        [Display(Name = "Lise Stajı")]
        HighSchoolInternship = 4
    }

    public enum LeaveType
    {
        [Display(Name = "Özel İzin")]
        PersonalLeave = 1,

        [Display(Name = "Sınav İzni")]
        ExamLeave = 2,

        [Display(Name = "Sağlık İzni")]
        HealthLeave = 3,

        [Display(Name = "Mazeret İzni")]
        ExcuseLeave = 4,

        [Display(Name = "Ücretsiz İzin")]
        UnpaidLeave = 5
    }

    public enum ApprovalStatus
    {
        [Display(Name = "Beklemede")]
        Pending = 0,

        [Display(Name = "Onaylandı")]
        Approved = 1,

        [Display(Name = "Reddedildi")]
        Rejected = 2,

        [Display(Name = "Revizyon")]
        Revision = 3,

        [Display(Name = "İptal Edildi")]
        Cancelled = 4,

        [Display(Name = "Supervisor Onaylandı")] // YENİ DURUM
        SupervisorApproved = 5
    }

    public enum WorkLocation
    {
        [Display(Name = "Genel Müdürlük")]
        HeadOffice = 1,

        [Display(Name = "İşletme")]
        Branch = 2
    }

    public enum ApprovalReferenceType
    {
        [Display(Name = "İzin Talebi")]
        LeaveRequest = 1,

        [Display(Name = "Puantaj")]
        Timesheet = 2
    }

    public enum SyncType
    {
        [Display(Name = "Stajyer Bilgileri")]
        InternData = 1,

        [Display(Name = "Departman Bilgileri")]
        DepartmentData = 2,

        [Display(Name = "Kullanıcı Bilgileri")]
        UserData = 3
    }

    public enum SyncStatus
    {
        [Display(Name = "Devam Ediyor")]
        InProgress = 1,

        [Display(Name = "Tamamlandı")]
        Completed = 2,

        [Display(Name = "Hata")]
        Failed = 3,

        [Display(Name = "İptal Edildi")]
        Cancelled = 4
    }
}