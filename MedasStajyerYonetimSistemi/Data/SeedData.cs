using MedasStajyerYonetimSistemi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MedasStajyerYonetimSistemi.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Veritabanını oluştur (eğer yoksa)
            await context.Database.EnsureCreatedAsync();

            // Rolleri oluştur
            await CreateRoles(roleManager);

            // Test kullanıcılarını oluştur
            await CreateUsers(userManager);

            // Test stajyerlerini oluştur
            await CreateInterns(context, userManager);

            // Test izin taleplerini oluştur
            await CreateLeaveRequests(context);

            await context.SaveChangesAsync();
        }

        private static async Task CreateRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "HR", "Supervisor", "Intern", "PersonelIsleri" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task CreateUsers(UserManager<ApplicationUser> userManager)
        {
            // Admin kullanıcısı
            var adminEmail = "admin@medas.com.tr";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Sistem Yöneticisi",
                    Department = "Bilgi İşlem",
                    Title = "Sistem Yöneticisi",
                    EmployeeNumber = "SYS001",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // İK kullanıcısı
            var hrEmail = "ayskozan@medas.com.tr";
            if (await userManager.FindByEmailAsync(hrEmail) == null)
            {
                var hrUser = new ApplicationUser
                {
                    UserName = hrEmail,
                    Email = hrEmail,
                    FullName = "Ayşe KOZAN",
                    Department = "İnsan Kaynakları ve Organizasyon Metot Müdürlüğü",
                    Title = "İnsan Kaynakları Uzmanı",
                    EmployeeNumber = "HR001",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(hrUser, "Hr123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(hrUser, "HR");
                }
            }

            // Supervisor kullanıcısı
            var supervisorEmail = "supervisor@medas.com.tr";
            if (await userManager.FindByEmailAsync(supervisorEmail) == null)
            {
                var supervisorUser = new ApplicationUser
                {
                    UserName = supervisorEmail,
                    Email = supervisorEmail,
                    FullName = "Ahmet DEMİR",
                    Department = "Bilgi İşlem Müdürlüğü",
                    Title = "Müdür",
                    EmployeeNumber = "SUP001",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(supervisorUser, "Sup123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(supervisorUser, "Supervisor");
                }
            }

            // Personel İşleri kullanıcısı
            var personelEmail = "personel@medas.com.tr";
            if (await userManager.FindByEmailAsync(personelEmail) == null)
            {
                var personelUser = new ApplicationUser
                {
                    UserName = personelEmail,
                    Email = personelEmail,
                    FullName = "Fatma YILMAZ",
                    Department = "Personel ve İdari İşler Müdürlüğü",
                    Title = "Personel Uzmanı",
                    EmployeeNumber = "PER001",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(personelUser, "Per123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(personelUser, "PersonelIsleri");
                }
            }
        }

        private static async Task CreateInterns(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            if (context.Interns.Any())
            {
                return; // Zaten stajyer var
            }

            var hrUser = await userManager.FindByEmailAsync("ayskozan@medas.com.tr");
            var supervisorUser = await userManager.FindByEmailAsync("supervisor@medas.com.tr");

            var interns = new List<Intern>
            {
                new Intern
                {
                    FullName = "Hatice BAKICI",
                    Address = "Konya/Selçuklu",
                    PhoneNumber = "555-555-5555",
                    Email = "hatice.bakici@student.edu.tr",
                    Company = "MEDAŞ",
                    Department = "İnsan Kaynakları ve Organizasyon Metot Müdürlüğü",
                    ResponsiblePerson = "Ayşe KOZAN",
                    ResponsiblePersonId = hrUser?.Id,
                    School = "Hekimoğlu ML",
                    Major = "Bilişim",
                    WorkDays = "Çarşamba-Perşembe-Cuma",
                    EmergencyContactName = "Ramazan BAKICI",
                    EmergencyContactPhone = "505-555-5555",
                    SchoolSupervisorName = "Celal ŞENGÖR",
                    SchoolSupervisorPhone = "505-666-6666",
                    InternshipType = InternshipType.SummerInternship,
                    Workplace = "GM Büro",
                    CompanyEmployeeNumber = "STJ001",
                    EmployeeNumber = "STJ001",
                    IsFromCloudoffix = true,
                    InternshipStartDate = DateTime.Now.AddDays(-30),
                    InternshipEndDate = DateTime.Now.AddDays(30),
                    IsActive = true
                },
                new Intern
                {
                    FullName = "Mehmet YILMAZ",
                    Address = "Ankara/Çankaya",
                    PhoneNumber = "533-444-3333",
                    Email = "mehmet.yilmaz@student.edu.tr",
                    Company = "MEDAŞ",
                    Department = "Bilgi İşlem Müdürlüğü",
                    ResponsiblePerson = "Ahmet DEMİR",
                    ResponsiblePersonId = supervisorUser?.Id,
                    School = "Selçuk Üniversitesi",
                    Major = "Bilgisayar Mühendisliği",
                    WorkDays = "Pazartesi-Salı-Çarşamba-Perşembe-Cuma",
                    EmergencyContactName = "Fatma YILMAZ",
                    EmergencyContactPhone = "544-333-2222",
                    SchoolSupervisorName = "Prof. Dr. Ali KAYA",
                    SchoolSupervisorPhone = "505-777-8888",
                    InternshipType = InternshipType.Talentern,
                    Workplace = "GM Büro",
                    CompanyEmployeeNumber = "STJ002",
                    EmployeeNumber = "STJ002",
                    IsFromCloudoffix = false,
                    InternshipStartDate = DateTime.Now.AddDays(-45),
                    InternshipEndDate = DateTime.Now.AddDays(45),
                    IsActive = true
                },
                new Intern
                {
                    FullName = "Zeynep KAYA",
                    Address = "İstanbul/Kadıköy",
                    PhoneNumber = "542-123-4567",
                    Email = "zeynep.kaya@student.edu.tr",
                    Company = "MEDAŞ",
                    Department = "İnsan Kaynakları ve Organizasyon Metot Müdürlüğü",
                    ResponsiblePerson = "Ayşe KOZAN",
                    ResponsiblePersonId = hrUser?.Id,
                    School = "Boğaziçi Üniversitesi",
                    Major = "İşletme",
                    WorkDays = "Pazartesi-Çarşamba-Cuma",
                    EmergencyContactName = "Mehmet KAYA",
                    EmergencyContactPhone = "532-987-6543",
                    SchoolSupervisorName = "Dr. Elif ÖZKAN",
                    SchoolSupervisorPhone = "505-111-2233",
                    InternshipType = InternshipType.KozaProject,
                    Workplace = "İşletme",
                    CompanyEmployeeNumber = "STJ003",
                    EmployeeNumber = "STJ003",
                    IsFromCloudoffix = true,
                    InternshipStartDate = DateTime.Now.AddDays(-20),
                    InternshipEndDate = DateTime.Now.AddDays(40),
                    IsActive = true
                }
            };

            context.Interns.AddRange(interns);
        }

        private static async Task CreateLeaveRequests(ApplicationDbContext context)
        {
            if (context.LeaveRequests.Any())
            {
                return; // Zaten izin talebi var
            }

            var intern = context.Interns.FirstOrDefault();
            if (intern != null)
            {
                var leaveRequests = new List<LeaveRequest>
                {
                    new LeaveRequest
                    {
                        InternId = intern.Id,
                        LeaveType = LeaveType.PersonalLeave,
                        StartDateTime = DateTime.Now.AddDays(-5).Date.AddHours(9),
                        EndDateTime = DateTime.Now.AddDays(-5).Date.AddHours(12),
                        Reason = "Sağlık Kontrolü",
                        TotalDays = 0,
                        TotalHours = 3,
                        Status = ApprovalStatus.Approved,
                        ApproverName = "Ayşe KOZAN",
                        ApprovalDate = DateTime.Now.AddDays(-4),
                        ShouldReflectToTimesheet = true,
                        IsManualEntry = false
                    },
                    new LeaveRequest
                    {
                        InternId = intern.Id,
                        LeaveType = LeaveType.ExamLeave,
                        StartDateTime = DateTime.Now.AddDays(2).Date.AddHours(8),
                        EndDateTime = DateTime.Now.AddDays(2).Date.AddHours(17),
                        Reason = "Final Sınavı",
                        TotalDays = 1,
                        TotalHours = 8,
                        Status = ApprovalStatus.Pending,
                        ShouldReflectToTimesheet = false,
                        IsManualEntry = false
                    }
                };

                context.LeaveRequests.AddRange(leaveRequests);
            }
        }
    }
}