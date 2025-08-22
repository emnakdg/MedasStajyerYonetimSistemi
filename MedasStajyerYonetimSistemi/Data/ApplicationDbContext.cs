using MedasStajyerYonetimSistemi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MedasStajyerYonetimSistemi.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet tanımlamaları
        public DbSet<Intern> Interns { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<Timesheet> Timesheets { get; set; }
        public DbSet<TimesheetDetail> TimesheetDetails { get; set; }
        public DbSet<ApprovalHistory> ApprovalHistories { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }
        public DbSet<EmailNotification> EmailNotifications { get; set; }
        public DbSet<CloudoffixSyncLog> CloudoffixSyncLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ============================================================================
            // ApplicationUser Konfigürasyonu
            // ============================================================================
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.Department).HasMaxLength(150);
                entity.Property(e => e.Title).HasMaxLength(100);
                entity.Property(e => e.EmployeeNumber).HasMaxLength(20);

                // Index'ler
                entity.HasIndex(e => e.EmployeeNumber).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // ============================================================================
            // Intern (Stajyer) Konfigürasyonu
            // ============================================================================
            builder.Entity<Intern>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FullName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Address).HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Company).HasMaxLength(100);
                entity.Property(e => e.Department).HasMaxLength(150);
                entity.Property(e => e.ResponsiblePerson).HasMaxLength(100);
                entity.Property(e => e.School).HasMaxLength(150);
                entity.Property(e => e.Major).HasMaxLength(100);
                entity.Property(e => e.WorkDays).HasMaxLength(100);
                entity.Property(e => e.EmergencyContactName).HasMaxLength(100);
                entity.Property(e => e.EmergencyContactPhone).HasMaxLength(20);
                entity.Property(e => e.SchoolSupervisorName).HasMaxLength(100);
                entity.Property(e => e.SchoolSupervisorPhone).HasMaxLength(20);
                entity.Property(e => e.Workplace).HasMaxLength(100);
                entity.Property(e => e.CompanyEmployeeNumber).HasMaxLength(20);
                entity.Property(e => e.EmployeeNumber).HasMaxLength(20);

                // Foreign Key - Sorumlu Kişi
                entity.HasOne(e => e.ResponsibleUser)
                      .WithMany()
                      .HasForeignKey(e => e.ResponsiblePersonId)
                      .OnDelete(DeleteBehavior.NoAction); // DÜZELTME: SetNull yerine NoAction

                // Index'ler
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.EmployeeNumber);
                entity.HasIndex(e => e.InternshipType);
                entity.HasIndex(e => e.IsActive);
            });

            // ============================================================================
            // LeaveRequest (İzin Talepleri) Konfigürasyonu
            // ============================================================================
            builder.Entity<LeaveRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Reason).HasMaxLength(500);
                entity.Property(e => e.ApproverName).HasMaxLength(100);
                entity.Property(e => e.ApprovalNote).HasMaxLength(500);
                entity.Property(e => e.TotalHours).HasColumnType("decimal(5,2)");

                // Foreign Keys
                entity.HasOne(e => e.Intern)
                      .WithMany(e => e.LeaveRequests)
                      .HasForeignKey(e => e.InternId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Approver)
                      .WithMany() // DÜZELTME: Navigation property kaldırıldı
                      .HasForeignKey(e => e.ApproverId)
                      .OnDelete(DeleteBehavior.NoAction); // DÜZELTME: SetNull yerine NoAction

                // Index'ler
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.LeaveType);
                entity.HasIndex(e => e.StartDateTime);
                entity.HasIndex(e => e.CreatedDate);
            });

            // ============================================================================
            // Timesheet (Puantaj) Konfigürasyonu - DÜZELTİLDİ
            // ============================================================================
            builder.Entity<Timesheet>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ApproverName).HasMaxLength(100);
                entity.Property(e => e.ApprovalNote).HasMaxLength(500);
                entity.Property(e => e.TotalTrainingHours).HasColumnType("decimal(6,2)");

                // YENİ SUPERVISOR ALANLARI
                entity.Property(e => e.SupervisorName).HasMaxLength(100);
                entity.Property(e => e.SupervisorNote).HasMaxLength(500);

                // Foreign Keys
                entity.HasOne(e => e.Intern)
                      .WithMany(e => e.Timesheets)
                      .HasForeignKey(e => e.InternId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Final Approver (İK) ilişkisi
                entity.HasOne(e => e.Approver)
                      .WithMany() // DÜZELTME: Navigation property kaldırıldı
                      .HasForeignKey(e => e.ApproverId)
                      .OnDelete(DeleteBehavior.NoAction); // DÜZELTME: Cascade conflict önlendi

                // Supervisor ilişkisi
                entity.HasOne(e => e.Supervisor)
                      .WithMany() // Navigation property yok
                      .HasForeignKey(e => e.SupervisorId)
                      .OnDelete(DeleteBehavior.NoAction); // DÜZELTME: Cascade conflict önlendi

                // Index'ler
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.PeriodDate);
                entity.HasIndex(e => e.CreatedDate);

                // Unique constraint - Bir stajyerin aynı dönem için sadece bir puantajı olabilir
                entity.HasIndex(e => new { e.InternId, e.PeriodDate }).IsUnique();
            });

            // ============================================================================
            // TimesheetDetail (Puantaj Detay) Konfigürasyonu
            // ============================================================================
            builder.Entity<TimesheetDetail>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DayName).HasMaxLength(20);
                entity.Property(e => e.LeaveInfo).HasMaxLength(200);
                entity.Property(e => e.TrainingInfo).HasMaxLength(200);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.LeaveHours).HasColumnType("decimal(5,2)");
                entity.Property(e => e.TrainingHours).HasColumnType("decimal(5,2)");

                // Foreign Key
                entity.HasOne(e => e.Timesheet)
                      .WithMany(e => e.TimesheetDetails)
                      .HasForeignKey(e => e.TimesheetId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index'ler
                entity.HasIndex(e => e.WorkDate);
                entity.HasIndex(e => e.IsPresent);
                entity.HasIndex(e => e.WorkLocation);

                // Unique constraint - Aynı puantajda aynı gün sadece bir kez olabilir
                entity.HasIndex(e => new { e.TimesheetId, e.WorkDate }).IsUnique();
            });

            // ============================================================================
            // ApprovalHistory (Onay Geçmişi) Konfigürasyonu
            // ============================================================================
            builder.Entity<ApprovalHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ApproverName).HasMaxLength(100);
                entity.Property(e => e.ApprovalNote).HasMaxLength(500);
                entity.Property(e => e.IpAddress).HasMaxLength(50);

                // Foreign Key
                entity.HasOne(e => e.Approver)
                      .WithMany()
                      .HasForeignKey(e => e.ApproverId)
                      .OnDelete(DeleteBehavior.NoAction); // DÜZELTME: SetNull yerine NoAction

                // Index'ler
                entity.HasIndex(e => e.ReferenceType);
                entity.HasIndex(e => e.ReferenceId);
                entity.HasIndex(e => e.ActionDate);
            });

            // ============================================================================
            // SystemLog (Sistem Logları) Konfigürasyonu
            // ============================================================================
            builder.Entity<SystemLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).HasMaxLength(450);
                entity.Property(e => e.UserName).HasMaxLength(100);
                entity.Property(e => e.Action).HasMaxLength(200);
                entity.Property(e => e.TableName).HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(50);

                // Foreign Key
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.NoAction); // DÜZELTME: SetNull yerine NoAction

                // Index'ler
                entity.HasIndex(e => e.ActionDate);
                entity.HasIndex(e => e.TableName);
                entity.HasIndex(e => e.Action);
            });

            // ============================================================================
            // EmailNotification (Email Bildirimleri) Konfigürasyonu
            // ============================================================================
            builder.Entity<EmailNotification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ToEmail).HasMaxLength(200);
                entity.Property(e => e.ToName).HasMaxLength(100);
                entity.Property(e => e.Subject).HasMaxLength(200);
                entity.Property(e => e.ErrorMessage).HasMaxLength(500);
                entity.Property(e => e.ReferenceType).HasMaxLength(50);

                // Index'ler
                entity.HasIndex(e => e.IsSent);
                entity.HasIndex(e => e.CreatedDate);
                entity.HasIndex(e => e.ReferenceType);
            });

            // ============================================================================
            // CloudoffixSyncLog (Cloudoffix Senkronizasyon) Konfigürasyonu
            // ============================================================================
            builder.Entity<CloudoffixSyncLog>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Index'ler
                entity.HasIndex(e => e.SyncType);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.StartDate);
            });
        }
    }
}