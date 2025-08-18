// ============================================================================
// Controllers/InternsController.cs - Stajyer Yönetim Controller'ı
// ============================================================================

using ClosedXML.Excel;
using MedasStajyerYonetimSistemi.Data;
using MedasStajyerYonetimSistemi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MedasStajyerYonetimSistemi.Controllers
{
    public class InternsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InternsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ========================================================================
        // GET: Interns - Stajyer Listesi
        // ========================================================================
        public async Task<IActionResult> Index(string sortBy = "FullName", string searchTerm = "", int page = 1)
        {
            var query = _context.Interns
                .Include(i => i.ResponsibleUser)
                .Where(i => i.IsActive);

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

            return View(interns);
        }

        // ========================================================================
        // GET: Interns/Details/5 - Stajyer Detayları
        // ========================================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var intern = await _context.Interns
                .Include(i => i.ResponsibleUser)
                .Include(i => i.LeaveRequests)
                .Include(i => i.Timesheets)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (intern == null)
            {
                return NotFound();
            }

            return View(intern);
        }

        // ========================================================================
        // GET: Interns/Create - Yeni Stajyer Formu
        // ========================================================================
        //[Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropDowns();
            return View();
        }

        // ========================================================================
        // POST: Interns/Create - Yeni Stajyer Kaydet
        // ========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create([Bind("FullName,Address,PhoneNumber,Email,Company,Department,ResponsiblePerson,ResponsiblePersonId,School,Major,WorkDays,EmergencyContactName,EmergencyContactPhone,SchoolSupervisorName,SchoolSupervisorPhone,InternshipType,Workplace,CompanyEmployeeNumber,EmployeeNumber,IsFromCloudoffix,InternshipStartDate,InternshipEndDate")] Intern intern)
        {
            if (ModelState.IsValid)
            {
                intern.CreatedDate = DateTime.Now;
                intern.IsActive = true;

                // Email kontrolü
                var existingIntern = await _context.Interns
                    .FirstOrDefaultAsync(i => i.Email == intern.Email && i.IsActive);

                if (existingIntern != null)
                {
                    ModelState.AddModelError("Email", "Bu email adresi zaten kayıtlı.");
                    await PopulateDropDowns(intern.ResponsiblePersonId);
                    return View(intern);
                }

                // Sicil no kontrolü
                if (!string.IsNullOrEmpty(intern.EmployeeNumber))
                {
                    var existingEmployeeNumber = await _context.Interns
                        .FirstOrDefaultAsync(i => i.EmployeeNumber == intern.EmployeeNumber && i.IsActive);

                    if (existingEmployeeNumber != null)
                    {
                        ModelState.AddModelError("EmployeeNumber", "Bu sicil numarası zaten kullanılıyor.");
                        await PopulateDropDowns(intern.ResponsiblePersonId);
                        return View(intern);
                    }
                }

                _context.Add(intern);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Stajyer başarıyla kaydedildi.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropDowns(intern.ResponsiblePersonId);
            return View(intern);
        }

        // ========================================================================
        // GET: Interns/Edit/5 - Stajyer Düzenle
        // ========================================================================
        //[Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var intern = await _context.Interns.FindAsync(id);
            if (intern == null)
            {
                return NotFound();
            }

            // await PopulateDropDowns(intern.ResponsiblePersonId); // Şimdilik yorum satırına alın
            return View(intern);
        }

        // ========================================================================
        // POST: Interns/Edit/5 - Stajyer Güncelle
        // ========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Address,PhoneNumber,Email,Company,Department,ResponsiblePerson,ResponsiblePersonId,School,Major,WorkDays,EmergencyContactName,EmergencyContactPhone,SchoolSupervisorName,SchoolSupervisorPhone,InternshipType,Workplace,CompanyEmployeeNumber,EmployeeNumber,IsFromCloudoffix,InternshipStartDate,InternshipEndDate,IsActive,CreatedDate")] Intern intern)
        {
            if (id != intern.Id)
            {
                return NotFound();
            }

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
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropDowns(intern.ResponsiblePersonId);
            return View(intern);
        }

        // ========================================================================
        // GET: Interns/Delete/5 - Stajyer Sil (Soft Delete)
        // ========================================================================
        //[Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var intern = await _context.Interns
                .Include(i => i.ResponsibleUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (intern == null)
            {
                return NotFound();
            }

            return View(intern);
        }

        // ========================================================================
        // POST: Interns/Delete/5 - Stajyer Sil (Onay)
        // ========================================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin,HR")]
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

                // Model state'i temizle
                ModelState.Clear();

                _context.Update(intern);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Stajyer kaydı başarıyla silindi.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                // Veritabanı güncelleme hatası
                TempData["ErrorMessage"] = $"Veritabanı hatası: {dbEx.InnerException?.Message ?? dbEx.Message}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Genel hata
                TempData["ErrorMessage"] = $"Silme işlemi sırasında hata oluştu: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ========================================================================
        // Excel Export - Stajyer Listesi Dışa Aktar
        // ========================================================================
        //[Authorize(Roles = "Admin,HR,Supervisor")]
        public async Task<IActionResult> ExportToExcel()
        {
            var interns = await _context.Interns
                .Include(i => i.ResponsibleUser)
                .Where(i => i.IsActive)
                .OrderBy(i => i.FullName)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Stajyer Listesi");

            // Başlık satırı
            worksheet.Cell(1, 1).Value = "Adı Soyadı";
            worksheet.Cell(1, 2).Value = "Şirket";
            worksheet.Cell(1, 3).Value = "Departman";
            worksheet.Cell(1, 4).Value = "Email";
            worksheet.Cell(1, 5).Value = "Telefon";
            worksheet.Cell(1, 6).Value = "Okul";
            worksheet.Cell(1, 7).Value = "Bölüm";
            worksheet.Cell(1, 8).Value = "Sorumlu Kişi";
            worksheet.Cell(1, 9).Value = "Staj Türü";
            worksheet.Cell(1, 10).Value = "Çalışma Günleri";
            worksheet.Cell(1, 11).Value = "İşyeri";
            worksheet.Cell(1, 12).Value = "Staj Başlangıç";
            worksheet.Cell(1, 13).Value = "Staj Bitiş";
            worksheet.Cell(1, 14).Value = "Sicil No";

            // Başlık satırını kalın yap
            var headerRange = worksheet.Range(1, 1, 1, 14);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;

            // Veri satırları
            for (int i = 0; i < interns.Count; i++)
            {
                var intern = interns[i];
                var row = i + 2; // Başlık satırından sonra

                worksheet.Cell(row, 1).Value = intern.FullName;
                worksheet.Cell(row, 2).Value = intern.Company;
                worksheet.Cell(row, 3).Value = intern.Department;
                worksheet.Cell(row, 4).Value = intern.Email;
                worksheet.Cell(row, 5).Value = intern.PhoneNumber;
                worksheet.Cell(row, 6).Value = intern.School;
                worksheet.Cell(row, 7).Value = intern.Major;
                worksheet.Cell(row, 8).Value = intern.ResponsiblePerson;
                worksheet.Cell(row, 9).Value = GetInternshipTypeDisplayName(intern.InternshipType);
                worksheet.Cell(row, 10).Value = intern.WorkDays;
                worksheet.Cell(row, 11).Value = intern.Workplace;
                worksheet.Cell(row, 12).Value = intern.InternshipStartDate?.ToString("dd.MM.yyyy") ?? "";
                worksheet.Cell(row, 13).Value = intern.InternshipEndDate?.ToString("dd.MM.yyyy") ?? "";
                worksheet.Cell(row, 14).Value = intern.EmployeeNumber ?? "";
            }

            // Sütun genişliklerini ayarla
            worksheet.Columns().AdjustToContents();

            // Tablo stili ekle
            var dataRange = worksheet.Range(1, 1, interns.Count + 1, 14);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Excel dosyasını memory stream'e kaydet
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Stajyer_Listesi_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // Helper method for InternshipType display name
        private string GetInternshipTypeDisplayName(InternshipType internshipType)
        {
            return internshipType switch
            {
                InternshipType.SummerInternship => "Yaz Stajı",
                InternshipType.Talentern => "Talentern Staj",
                InternshipType.KozaProject => "Koza Projesi",
                InternshipType.HighSchoolInternship => "Lise Stajı",
                _ => internshipType.ToString()
            };
        }

        // ========================================================================
        // API: Stajyer Arama (AutoComplete için)
        // ========================================================================
        [HttpGet]
        public async Task<JsonResult> SearchInterns(string term)
        {
            var interns = await _context.Interns
                .Where(i => i.IsActive && i.FullName.Contains(term))
                .Select(i => new
                {
                    id = i.Id,
                    label = i.FullName,
                    department = i.Department,
                    responsiblePerson = i.ResponsiblePerson
                })
                .Take(10)
                .ToListAsync();

            return Json(interns);
        }

        // ========================================================================
        // Private Helper Methods
        // ========================================================================
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