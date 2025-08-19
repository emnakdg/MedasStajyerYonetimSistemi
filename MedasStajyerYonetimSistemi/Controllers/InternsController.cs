using ClosedXML.Excel;
using MedasStajyerYonetimSistemi.Data;
using MedasStajyerYonetimSistemi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MedasStajyerYonetimSistemi.Controllers
{
    [Authorize] // Temel authorization - giriş yapmış olmalı
    public class InternsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InternsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Interns - Stajyer Listesi 
        // Stajyerler sadece kendi kaydını görebilir, diğerleri herkesi görebilir
        public async Task<IActionResult> Index(string sortBy = "FullName", string searchTerm = "", int page = 1)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            var query = _context.Interns
                .Include(i => i.ResponsibleUser)
                .Where(i => i.IsActive);

            // Eğer stajyer ise sadece kendi kaydını görsün
            if (userRoles.Contains("Intern"))
            {
                query = query.Where(i => i.Email == currentUser.Email);
            }

            // Arama filtresi
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(i =>
                    i.FullName.Contains(searchTerm) ||
                    i.Company.Contains(searchTerm) ||
                    i.Department.Contains(searchTerm) ||
                    i.School.Contains(searchTerm) ||
                    i.ResponsiblePerson.Contains(searchTerm));
            }

            // Sıralama
            query = sortBy.ToLower() switch
            {
                "company" => query.OrderBy(i => i.Company),
                "department" => query.OrderBy(i => i.Department),
                "school" => query.OrderBy(i => i.School),
                "internshiptype" => query.OrderBy(i => i.InternshipType),
                _ => query.OrderBy(i => i.FullName)
            };

            const int pageSize = 10;
            var totalCount = await query.CountAsync();
            var interns = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentSort = sortBy;
            ViewBag.CurrentSearch = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            // Stajyer ise farklı bir view göster (opsiyonel)
            if (userRoles.Contains("Intern"))
            {
                ViewBag.IsInternView = true;
            }

            return View(interns);
        }

        // GET: Interns/Details - Stajyer Detayları 
        // Stajyerler sadece kendi detaylarını görebilir
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            var intern = await _context.Interns
                .Include(i => i.ResponsibleUser)
                .Include(i => i.LeaveRequests)
                .Include(i => i.Timesheets)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (intern == null) return NotFound();

            // Eğer stajyer ise sadece kendi detaylarını görebilir
            if (userRoles.Contains("Intern") && intern.Email != currentUser.Email)
            {
                TempData["ErrorMessage"] = "Bu bilgilere erişim yetkiniz bulunmamaktadır.";
                return RedirectToAction(nameof(Index));
            }

            return View(intern);
        }

        // GET: Interns/Create - Yeni Stajyer (Sadece HR ve Admin)
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropDowns();
            return View();
        }

        // POST: Interns/Create - Yeni Stajyer Kaydet (Sadece HR ve Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create([Bind("FullName,Address,PhoneNumber,Email,Company,Department,ResponsiblePerson,ResponsiblePersonId,School,Major,WorkDays,EmergencyContactName,EmergencyContactPhone,SchoolSupervisorName,SchoolSupervisorPhone,InternshipType,Workplace,CompanyEmployeeNumber,EmployeeNumber,IsFromCloudoffix,InternshipStartDate,InternshipEndDate")] Intern intern)
        {
            // Navigation property hatalarını temizle
            ModelState.Remove("ResponsibleUser");
            ModelState.Remove("LeaveRequests");
            ModelState.Remove("Timesheets");

            if (ModelState.IsValid)
            {
                // Email kontrolü
                var existingIntern = await _context.Interns
                    .FirstOrDefaultAsync(i => i.Email == intern.Email && i.IsActive);

                if (existingIntern != null)
                {
                    ModelState.AddModelError("Email", "Bu email adresi zaten kayıtlı.");
                    await PopulateDropDowns(intern.ResponsiblePersonId);
                    return View(intern);
                }

                intern.CreatedDate = DateTime.Now;
                intern.IsActive = true;

                _context.Add(intern);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Stajyer başarıyla kaydedildi.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropDowns(intern.ResponsiblePersonId);
            return View(intern);
        }

        // GET: Interns/Edit - Stajyer Düzenle (Sadece HR ve Admin)
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var intern = await _context.Interns.FindAsync(id);
            if (intern == null) return NotFound();

            await PopulateDropDowns(intern.ResponsiblePersonId);
            return View(intern);
        }

        // POST: Interns/Edit - Stajyer Güncelle (Sadece HR ve Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Address,PhoneNumber,Email,Company,Department,ResponsiblePerson,ResponsiblePersonId,School,Major,WorkDays,EmergencyContactName,EmergencyContactPhone,SchoolSupervisorName,SchoolSupervisorPhone,InternshipType,Workplace,CompanyEmployeeNumber,EmployeeNumber,IsFromCloudoffix,InternshipStartDate,InternshipEndDate,IsActive,CreatedDate")] Intern intern)
        {
            if (id != intern.Id) return NotFound();

            ModelState.Remove("ResponsibleUser");
            ModelState.Remove("LeaveRequests");
            ModelState.Remove("Timesheets");

            if (ModelState.IsValid)
            {
                try
                {
                    // Email kontrolü (kendisi hariç)
                    var existingIntern = await _context.Interns
                        .FirstOrDefaultAsync(i => i.Email == intern.Email && i.Id != intern.Id && i.IsActive);

                    if (existingIntern != null)
                    {
                        ModelState.AddModelError("Email", "Bu email adresi zaten kayıtlı.");
                        await PopulateDropDowns(intern.ResponsiblePersonId);
                        return View(intern);
                    }

                    intern.UpdatedDate = DateTime.Now;
                    _context.Update(intern);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Stajyer bilgileri başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InternExists(intern.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await PopulateDropDowns(intern.ResponsiblePersonId);
            return View(intern);
        }

        // GET: Interns/Delete - Stajyer Sil (Sadece Admin)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var intern = await _context.Interns
                .Include(i => i.ResponsibleUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (intern == null) return NotFound();

            return View(intern);
        }

        // POST: Interns/Delete - Stajyer Sil (Sadece Admin)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var intern = await _context.Interns
                    .Include(i => i.LeaveRequests)
                    .Include(i => i.Timesheets)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (intern == null)
                {
                    TempData["ErrorMessage"] = "Silinecek stajyer bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                // Soft delete
                intern.IsActive = false;
                intern.UpdatedDate = DateTime.Now;

                _context.Update(intern);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Stajyer kaydı başarıyla silindi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Silme işlemi sırasında hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Excel Export (Sadece HR ve Admin)
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> ExportToExcel(string? searchTerm = null)
        {
            var query = _context.Interns.Where(i => i.IsActive);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(i =>
                    i.FullName.Contains(searchTerm) ||
                    i.Company.Contains(searchTerm) ||
                    i.Department.Contains(searchTerm) ||
                    i.School.Contains(searchTerm));
            }

            var interns = await query.OrderBy(i => i.FullName).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Stajyerler");

            // Headers
            var headers = new[]
            {
                "Ad-Soyad", "Şirket", "Departman", "Okul", "Bölüm",
                "Sorumlu Kişi", "Telefon", "E-posta", "Staj Türü",
                "Başlangıç Tarihi", "Bitiş Tarihi", "Aktif"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Data
            int row = 2;
            foreach (var intern in interns)
            {
                worksheet.Cell(row, 1).Value = intern.FullName;
                worksheet.Cell(row, 2).Value = intern.Company;
                worksheet.Cell(row, 3).Value = intern.Department;
                worksheet.Cell(row, 4).Value = intern.School;
                worksheet.Cell(row, 5).Value = intern.Major;
                worksheet.Cell(row, 6).Value = intern.ResponsiblePerson;
                worksheet.Cell(row, 7).Value = intern.PhoneNumber;
                worksheet.Cell(row, 8).Value = intern.Email;
                worksheet.Cell(row, 9).Value = intern.InternshipType.ToString();
                worksheet.Cell(row, 10).Value = intern.InternshipStartDate?.ToString("dd.MM.yyyy") ?? "";
                worksheet.Cell(row, 11).Value = intern.InternshipEndDate?.ToString("dd.MM.yyyy") ?? "";
                worksheet.Cell(row, 12).Value = intern.IsActive ? "Evet" : "Hayır";
                row++;
            }

            worksheet.Columns().AdjustToContents();

            var fileName = $"Stajyerler_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // Helper methods
        private bool InternExists(int id)
        {
            return _context.Interns.Any(e => e.Id == id);
        }

        private async Task PopulateDropDowns(string? selectedResponsiblePersonId = null)
        {
            // Sorumlu kişiler (HR ve Supervisor rollerindeki kullanıcılar)
            var responsibleUsers = await _userManager.GetUsersInRoleAsync("HR");
            var supervisors = await _userManager.GetUsersInRoleAsync("Supervisor");

            var allResponsibleUsers = responsibleUsers.Union(supervisors)
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .ToList();

            ViewBag.ResponsiblePersonId = new SelectList(
                allResponsibleUsers,
                "Id",
                "FullName",
                selectedResponsiblePersonId);

            // Staj türleri
            ViewBag.InternshipTypes = new SelectList(
                Enum.GetValues(typeof(InternshipType))
                    .Cast<InternshipType>()
                    .Select(e => new
                    {
                        Value = (int)e,
                        Text = e.GetDisplayName()
                    }),
                "Value",
                "Text");

            // Şirketler
            ViewBag.Companies = new List<string> { "MEDAŞ" };

            // Departmanlar
            ViewBag.Departments = new List<string>
            {
                "İnsan Kaynakları ve Organizasyon Metot Müdürlüğü",
                "Bilgi İşlem Müdürlüğü",
                "Personel ve İdari İşler Müdürlüğü",
                "Teknik İşler Müdürlüğü"
            };

            // İşyerleri
            ViewBag.Workplaces = new List<string> { "GM Büro", "İşletme" };
        }
    }
}

// Görünen Ad için Enum Extensions (eğer yoksa)
public static class EnumExtensions
{
    public static string GetDisplayName(this Enum enumValue)
    {
        return enumValue.GetType()
            .GetMember(enumValue.ToString())
            .First()
            .GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false)
            .Cast<System.ComponentModel.DataAnnotations.DisplayAttribute>()
            .FirstOrDefault()?.Name ?? enumValue.ToString();
    }
}