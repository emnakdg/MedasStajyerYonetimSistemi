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

            // Veritabanını oluştur yoksa
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

            // ik kullanıcısı
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



            // Stajyer 1 - Hatice BAKICI
            var intern1Email = "hatice.bakici@medas.com.tr";
            if (await userManager.FindByEmailAsync(intern1Email) == null)
            {
                var intern1User = new ApplicationUser
                {
                    UserName = intern1Email,
                    Email = intern1Email,
                    FullName = "Hatice BAKICI",
                    Department = "İnsan Kaynakları ve Organizasyon Metot Müdürlüğü",
                    Title = "Stajyer",
                    EmployeeNumber = "STJ001",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(intern1User, "Staj123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(intern1User, "Intern");
                }
            }

            // Stajyer 2 - Ali KAYA
            var intern2Email = "ali.kaya@medas.com.tr";
            if (await userManager.FindByEmailAsync(intern2Email) == null)
            {
                var intern2User = new ApplicationUser
                {
                    UserName = intern2Email,
                    Email = intern2Email,
                    FullName = "Ali KAYA",
                    Department = "Bilgi İşlem Müdürlüğü",
                    Title = "Stajyer",
                    EmployeeNumber = "STJ002",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(intern2User, "Staj123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(intern2User, "Intern");
                }
            }

            // Stajyer 3 - Zehra ÖZKAN
            var intern3Email = "zehra.ozkan@medas.com.tr";
            if (await userManager.FindByEmailAsync(intern3Email) == null)
            {
                var intern3User = new ApplicationUser
                {
                    UserName = intern3Email,
                    Email = intern3Email,
                    FullName = "Zehra ÖZKAN",
                    Department = "Teknik İşler Müdürlüğü",
                    Title = "Stajyer",
                    EmployeeNumber = "STJ003",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(intern3User, "Staj123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(intern3User, "Intern");
                }
            }
        }




        // Stajyer verilerini güncelle
        private static async Task CreateInterns(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            if (context.Interns.Any())
            {
                return;
            }

            // Sorumlu kişiyi bul
            var responsibleUser = await userManager.FindByEmailAsync("ayskozan@medas.com.tr");

            var interns = new List<Intern>
    {
        new Intern
        {
            FullName = "Hatice BAKICI",
            Address = "Konya/Selçuklu",
            PhoneNumber = "555-555-5555",
            Email = "hatice.bakici@medas.com.tr",
            Company = "MEDAŞ",
            Department = "İnsan Kaynakları ve Organizasyon Metot Müdürlüğü",
            ResponsiblePerson = "Ayşe KOZAN",
            ResponsiblePersonId = responsibleUser?.Id,
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
            FullName = "Ali KAYA",
            Address = "Konya/Meram",
            PhoneNumber = "555-666-7777",
            Email = "ali.kaya@medas.com.tr",
            Company = "MEDAŞ",
            Department = "Bilgi İşlem Müdürlüğü",
            ResponsiblePerson = "Ahmet DEMİR",
            ResponsiblePersonId = responsibleUser?.Id,
            School = "Selçuk Üniversitesi",
            Major = "Bilgisayar Mühendisliği",
            WorkDays = "Pazartesi-Salı-Çarşamba-Perşembe-Cuma",
            EmergencyContactName = "Mehmet KAYA",
            EmergencyContactPhone = "505-777-8888",
            SchoolSupervisorName = "Dr. Ahmet YİĞİT",
            SchoolSupervisorPhone = "505-888-9999",
            InternshipType = InternshipType.Talentern,
            Workplace = "İşletme",
            CompanyEmployeeNumber = "STJ002",
            EmployeeNumber = "STJ002",
            IsFromCloudoffix = true,
            InternshipStartDate = DateTime.Now.AddDays(-25),
            InternshipEndDate = DateTime.Now.AddDays(35),
            IsActive = true
        },
        new Intern
        {
            FullName = "Zehra ÖZKAN",
            Address = "Konya/Karatay",
            PhoneNumber = "555-777-8888",
            Email = "zehra.ozkan@medas.com.tr",
            Company = "MEDAŞ",
            Department = "Teknik İşler Müdürlüğü",
            ResponsiblePerson = "Elif ÖZKAN",
            ResponsiblePersonId = responsibleUser?.Id,
            School = "KTO Karatay Üniversitesi",
            Major = "Elektrik Mühendisliği",
            WorkDays = "Pazartesi-Salı-Çarşamba-Perşembe-Cuma",
            EmergencyContactName = "Fatma ÖZKAN",
            EmergencyContactPhone = "505-888-9999",
            SchoolSupervisorName = "Prof. Dr. Mehmet ÖZKAN",
            SchoolSupervisorPhone = "505-999-1111",
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

        // İzin taleplerini oluştur
        private static async Task CreateLeaveRequests(ApplicationDbContext context)
        {
            if (context.LeaveRequests.Any())
            {
                return;
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