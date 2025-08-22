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
    public class TimesheetsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TimesheetsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Timesheets - Puantaj Listesi
        // Stajyerler sadece kendi puantajlarını, diğerleri herkesi görebilir
        public async Task<IActionResult> Index(string status = "All", int page = 1)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            var query = _context.Timesheets
                .Include(t => t.Intern)
                .Include(t => t.Approver)
                .AsQueryable();

            // Eğer stajyer ise sadece kendi puantajlarını görsün
            if (userRoles.Contains("Intern"))
            {
                query = query.Where(t => t.Intern.Email == currentUser.Email);
            }

            // İSTATİSTİKLER İÇİN TOPLAM SAYILAR (filtresiz)
            var allTimesheets = await query.ToListAsync(); // Tüm data'yı al
            ViewBag.TotalCount = allTimesheets.Count();
            ViewBag.PendingCount = allTimesheets.Count(t => t.Status == ApprovalStatus.Pending);
            ViewBag.ApprovedCount = allTimesheets.Count(t => t.Status == ApprovalStatus.Approved);
            ViewBag.RejectedCount = allTimesheets.Count(t => t.Status == ApprovalStatus.Rejected);
            ViewBag.RevisionCount = allTimesheets.Count(t => t.Status == ApprovalStatus.Revision);

            // Durum filtresi (sayfa gösterimi için)
            if (status != "All" && Enum.TryParse<ApprovalStatus>(status, out var statusEnum))
            {
                query = query.Where(t => t.Status == statusEnum);
            }

            // Sıralama (en yeni önce)
            query = query.OrderByDescending(t => t.PeriodDate).ThenByDescending(t => t.CreatedDate);

            const int pageSize = 15;
            var totalCount = await query.CountAsync();
            var timesheets = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Listeleme için durum seçenekleri
            ViewBag.StatusOptions = new List<SelectListItem>
    {
        new() { Value = "All", Text = "Tümü" },
        new() { Value = "Pending", Text = "Beklemede" },
        new() { Value = "Approved", Text = "Onaylandı" },
        new() { Value = "Rejected", Text = "Reddedildi" },
        new() { Value = "Revision", Text = "Revize" }
    };

            ViewBag.IsIntern = userRoles.Contains("Intern");
            ViewBag.CanApprove = userRoles.Any(r => r == "Admin" || r == "HR" || r == "Supervisor" || r == "PersonelIsleri");

            return View(timesheets);
        }

        // GET: Timesheets/Details - Puantaj Detayları
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            var timesheet = await _context.Timesheets
                .Include(t => t.Intern)
                .Include(t => t.Approver)
                .Include(t => t.TimesheetDetails.OrderBy(td => td.WorkDate))
                .Include(t => t.ApprovalHistories)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (timesheet == null) return NotFound();

            // Eğer stajyer ise sadece kendi puantaj detaylarını görebilir
            if (userRoles.Contains("Intern") && timesheet.Intern.Email != currentUser.Email)
            {
                TempData["ErrorMessage"] = "Bu puantaj kaydına erişim yetkiniz bulunmamaktadır.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CanApprove = userRoles.Any(r => r == "Admin" || r == "HR" || r == "Supervisor");
            ViewBag.IsIntern = userRoles.Contains("Intern");

            return View(timesheet);
        }

        // GET: Timesheets/Create - Yeni Puantaj Oluştur
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            await PopulateDropDowns();

            var model = new Timesheet
            {
                PeriodDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                CreatedDate = DateTime.Now
            };

            // Eğer stajyer ise kendi kaydını otomatik seç
            if (userRoles.Contains("Intern"))
            {
                var internRecord = await _context.Interns
                    .FirstOrDefaultAsync(i => i.Email == currentUser.Email);

                if (internRecord != null)
                {
                    model.InternId = internRecord.Id;
                    ViewBag.IsInternUser = true;
                    ViewBag.InternName = internRecord.FullName;
                }
            }

            return View(model);
        }

        // POST: Timesheets/Create - Yeni Puantaj Kaydet
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Timesheet timesheet)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            // Navigation property hatalarını temizle
            ModelState.Remove("Intern");
            ModelState.Remove("Approver");
            ModelState.Remove("TimesheetDetails");
            ModelState.Remove("ApprovalHistories");

            // Eğer stajyer ise sadece kendi adına puantaj oluşturabilir
            if (userRoles.Contains("Intern"))
            {
                var internRecord = await _context.Interns
                    .FirstOrDefaultAsync(i => i.Email == currentUser.Email);

                if (internRecord == null)
                {
                    ModelState.AddModelError("", "Stajyer kaydınız bulunamadı. Lütfen yöneticinize başvurun.");
                }
                else if (timesheet.InternId != internRecord.Id)
                {
                    ModelState.AddModelError("", "Sadece kendi adınıza puantaj oluşturabilirsiniz.");
                }
            }

            if (ModelState.IsValid)
            {
                // Aynı dönem için puantaj kontrolü
                var existingTimesheet = await _context.Timesheets
                    .FirstOrDefaultAsync(t => t.InternId == timesheet.InternId &&
                                            t.PeriodDate.Year == timesheet.PeriodDate.Year &&
                                            t.PeriodDate.Month == timesheet.PeriodDate.Month);

                if (existingTimesheet != null)
                {

                    ModelState.AddModelError("PeriodDate", "Bu stajyer için bu dönemde zaten puantaj kaydı bulunmaktadır.");
                    await PopulateDropDowns(timesheet.InternId);
                    return View(timesheet);
                }

                // Manuel giriş kontrolü
                if (!userRoles.Contains("Intern"))
                {
                    timesheet.IsManualEntry = true;
                    timesheet.ManualEntryBy = currentUser?.UserName;
                }

                timesheet.CreatedDate = DateTime.Now;
                timesheet.Status = ApprovalStatus.Pending;

                // Dönem için günlük detayları oluştur
                await CreateMonthlyTimesheetDetails(timesheet);

                _context.Add(timesheet);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Puantaj başarıyla oluşturuldu. Günlük detayları girebilirsiniz.";
                return RedirectToAction(nameof(Details), new { id = timesheet.Id });
            }

            await PopulateDropDowns(timesheet.InternId);
            return View(timesheet);
        }

        // GET: Timesheets/Edit - Puantaj Düzenleme (Stajyer + Yetkili roller)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            var timesheet = await _context.Timesheets
                .Include(t => t.Intern)
                .Include(t => t.TimesheetDetails)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (timesheet == null) return NotFound();

            // FORM DÜZENLEME YETKİ KONTROLÜ (Edit sayfası)
            bool canEditForm = false;

            // 1. Admin ve HR her zaman form düzenleyebilir
            if (userRoles.Any(r => r == "Admin" || r == "HR"))
            {
                canEditForm = true;
            }
            // 2. Supervisor sadece Pending ve Revision durumlarında form düzenleyebilir
            else if (userRoles.Contains("Supervisor"))
            {
                canEditForm = (timesheet.Status == ApprovalStatus.Pending || timesheet.Status == ApprovalStatus.Revision);
            }
            // 3. Stajyer SADECE Pending/Revision durumunda form düzenleyebilir
            // Onaylanmış puantajlarda sadece detay düzenlemesi için Details sayfasına yönlendir
            else if (userRoles.Contains("Intern") && timesheet.Intern.Email == currentUser.Email)
            {
                if (timesheet.Status == ApprovalStatus.Approved)
                {
                    TempData["InfoMessage"] = "Onaylanmış puantajlarda sadece günlük detayları düzenleyebilirsiniz.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                canEditForm = (timesheet.Status == ApprovalStatus.Pending || timesheet.Status == ApprovalStatus.Revision);
            }

            if (!canEditForm)
            {
                if (timesheet.Status == ApprovalStatus.Approved)
                {
                    TempData["ErrorMessage"] = "Onaylanmış puantajlar düzenlenemez.";
                }
                else if (timesheet.Status == ApprovalStatus.Rejected)
                {
                    TempData["ErrorMessage"] = "Reddedilmiş puantajlar düzenlenemez.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Bu puantajı düzenleme yetkiniz bulunmuyor.";
                }
                return RedirectToAction(nameof(Details), new { id });
            }

            await PopulateDropDowns(timesheet.InternId);
            return View(timesheet);
        }

        // POST: Timesheets/Edit - Puantaj Güncelleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Timesheet timesheet)
        {
            if (id != timesheet.Id) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            // Yetki kontrolü
            var existingTimesheet = await _context.Timesheets
                .Include(t => t.Intern)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (existingTimesheet == null) return NotFound();

            bool canEdit = userRoles.Any(r => r == "Admin" || r == "HR" || r == "Supervisor" || r == "PersonelIsleri") ||
                           (userRoles.Contains("Intern") && existingTimesheet.Intern.Email == currentUser.Email);

            if (!canEdit)
            {
                TempData["ErrorMessage"] = "Bu puantajı düzenleme yetkiniz bulunmuyor.";
                return RedirectToAction(nameof(Index));
            }

            // Navigation property hatalarını temizle
            ModelState.Remove("Intern");
            ModelState.Remove("Approver");
            ModelState.Remove("TimesheetDetails");
            ModelState.Remove("ApprovalHistories");

            if (ModelState.IsValid)
            {
                try
                {
                    // Sadece belirli alanları güncelle
                    existingTimesheet.PeriodDate = timesheet.PeriodDate;
                    existingTimesheet.UpdatedDate = DateTime.Now;

                    // Eğer stajyer düzenliyorsa, durumu beklemede yap
                    if (userRoles.Contains("Intern") && existingTimesheet.Status == ApprovalStatus.Revision)
                    {
                        existingTimesheet.Status = ApprovalStatus.Pending;
                        existingTimesheet.ApprovalNote = null;
                        existingTimesheet.ApprovalDate = null;
                        existingTimesheet.ApproverId = null;
                        existingTimesheet.ApproverName = null;
                    }

                    _context.Update(existingTimesheet);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Puantaj başarıyla güncellendi.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TimesheetExists(timesheet.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            await PopulateDropDowns(timesheet.InternId);
            return View(timesheet);
        }

        // POST: Timesheets/UpdateDetail - Puantaj Detay Güncelleme (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDetail(int detailId, bool isPresent, string startTime, string endTime,
    string leaveInfo, decimal leaveHours, string trainingInfo, decimal trainingHours, bool hasMealAllowance,
    WorkLocation workLocation, string notes)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var userRoles = await _userManager.GetRolesAsync(currentUser);

                var timesheetDetail = await _context.TimesheetDetails
                    .Include(td => td.Timesheet)
                    .ThenInclude(t => t.Intern)
                    .FirstOrDefaultAsync(td => td.Id == detailId);

                if (timesheetDetail == null)
                {
                    TempData["ErrorMessage"] = "Puantaj detayı bulunamadı.";
                    return RedirectToAction("Index");
                }

                // GENİŞLETİLMİŞ YETKİ KONTROLÜ
                bool canEditDetail = false;

                // 1. Admin ve HR her zaman detay düzenleyebilir
                if (userRoles.Any(r => r == "Admin" || r == "HR"))
                {
                    canEditDetail = true;
                }
                // 2. Supervisor: Pending, Revision ve Approved durumlarında düzenleyebilir
                else if (userRoles.Contains("Supervisor"))
                {
                    canEditDetail = (timesheetDetail.Timesheet.Status == ApprovalStatus.Pending ||
                                   timesheetDetail.Timesheet.Status == ApprovalStatus.Revision ||
                                   timesheetDetail.Timesheet.Status == ApprovalStatus.Approved);
                }
                // 3. Stajyer: Kendi puantajında tüm durumlarda detay düzenleyebilir (onaylanmış dahil)
                else if (userRoles.Contains("Intern") && timesheetDetail.Timesheet.Intern.Email == currentUser.Email)
                {
                    canEditDetail = true; // Stajyer her durumda günlük detayları düzenleyebilir
                }

                if (!canEditDetail)
                {
                    TempData["ErrorMessage"] = "Bu puantaj detayını düzenleme yetkiniz bulunmuyor.";
                    return RedirectToAction("Details", new { id = timesheetDetail.TimesheetId });
                }

                // Detay güncelleme
                timesheetDetail.IsPresent = isPresent;
                timesheetDetail.WorkLocation = workLocation;
                timesheetDetail.LeaveInfo = leaveInfo ?? "";
                timesheetDetail.LeaveHours = leaveHours;
                timesheetDetail.TrainingInfo = trainingInfo ?? "";
                timesheetDetail.TrainingHours = trainingHours;
                timesheetDetail.HasMealAllowance = hasMealAllowance;
                timesheetDetail.Notes = notes ?? "";

                // Saat alanları sadece devam varsa doldurulsun
                if (isPresent)
                {
                    if (TimeSpan.TryParse(startTime, out var parsedStartTime))
                        timesheetDetail.StartTime = parsedStartTime;
                    if (TimeSpan.TryParse(endTime, out var parsedEndTime))
                        timesheetDetail.EndTime = parsedEndTime;
                }
                else
                {
                    timesheetDetail.StartTime = null;
                    timesheetDetail.EndTime = null;
                }

                // ÖNEMLI: Revizyon durumundaysa ve stajyer değişiklik yaparsa Pending'e çevir
                if (timesheetDetail.Timesheet.Status == ApprovalStatus.Revision && userRoles.Contains("Intern"))
                {
                    timesheetDetail.Timesheet.Status = ApprovalStatus.Pending;
                    timesheetDetail.Timesheet.ApprovalNote = null;
                    timesheetDetail.Timesheet.ApprovalDate = null;
                    timesheetDetail.Timesheet.ApproverId = null;
                    timesheetDetail.Timesheet.ApproverName = null;
                    timesheetDetail.Timesheet.UpdatedDate = DateTime.Now;

                    TempData["SuccessMessage"] = "Puantaj detayı güncellendi ve revizyon tamamlanarak onay için gönderildi.";
                }
                else
                {
                    timesheetDetail.Timesheet.UpdatedDate = DateTime.Now;
                    TempData["SuccessMessage"] = "Puantaj detayı başarıyla güncellendi.";
                }

                await _context.SaveChangesAsync();

                // Puantaj toplamlarını yeniden hesapla
                await RecalculateTimesheetTotals(timesheetDetail.TimesheetId);

                return RedirectToAction("Details", new { id = timesheetDetail.TimesheetId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Güncelleme sırasında hata oluştu.";
                return RedirectToAction("Index");
            }
        }

        // POST: Timesheets/Approve - Puantaj Onaylama (Sadece yetkili roller)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR,Supervisor,PersonelIsleri")]
        public async Task<IActionResult> Approve(int id, string? approvalNote = null)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Geçersiz puantaj ID'si.";
                return RedirectToAction(nameof(Index));
            }

            var timesheet = await _context.Timesheets
                .Include(t => t.Intern)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (timesheet == null)
            {
                TempData["ErrorMessage"] = "Puantaj bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            if (timesheet.Status != ApprovalStatus.Pending &&
                timesheet.Status != ApprovalStatus.Revision &&
                timesheet.Status != ApprovalStatus.SupervisorApproved) // YENİ DURUM
            {
                TempData["ErrorMessage"] = "Bu puantaj onaylanamaz.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bilgisi alınamadı.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // YENİ İŞ AKIŞI: Staj türüne göre onay süreci
                if (userRoles.Contains("Supervisor"))
                {
                    // SUPERVISOR ONAYI
                    await ApproveBySuperviser(timesheet, currentUser, approvalNote);
                }
                else if (userRoles.Any(r => r == "Admin" || r == "HR"))
                {
                    // İK ONAYI (sadece Talentern için gerekli)
                    await ApproveByHR(timesheet, currentUser, approvalNote);
                }
                else
                {
                    TempData["ErrorMessage"] = "Bu işlemi gerçekleştirme yetkiniz bulunmuyor.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                _context.Update(timesheet);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Onaylama işlemi sırasında hata oluştu.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // METOD: Supervisor onayı
        private async Task ApproveBySuperviser(Timesheet timesheet, ApplicationUser currentUser, string? approvalNote)
        {
            // Staj türünü kontrol et
            if (timesheet.Intern.InternshipType == InternshipType.Talentern)
            {
                // Talentern ise İK onayına gönder
                timesheet.Status = ApprovalStatus.SupervisorApproved; // YENİ DURUM
                timesheet.SupervisorId = currentUser.Id;
                timesheet.SupervisorName = currentUser.FullName ?? currentUser.UserName;
                timesheet.SupervisorApprovalDate = DateTime.Now;
                timesheet.SupervisorNote = approvalNote;
                timesheet.UpdatedDate = DateTime.Now;

                TempData["SuccessMessage"] = $"{timesheet.Intern.FullName} adlı stajyerin puantajı supervisor tarafından onaylandı. İK onayı bekleniyor.";
            }
            else
            {
                // Diğer staj türleri direkt onaylanır
                timesheet.Status = ApprovalStatus.Approved;
                timesheet.ApproverId = currentUser.Id;
                timesheet.ApproverName = currentUser.FullName ?? currentUser.UserName;
                timesheet.ApprovalDate = DateTime.Now;
                timesheet.ApprovalNote = approvalNote;
                timesheet.UpdatedDate = DateTime.Now;

                TempData["SuccessMessage"] = $"{timesheet.Intern.FullName} adlı stajyerin puantajı onaylandı.";
            }
        }

        // YENİ METOD: İK onayı
        private async Task ApproveByHR(Timesheet timesheet, ApplicationUser currentUser, string? approvalNote)
        {
            if (timesheet.Status != ApprovalStatus.SupervisorApproved)
            {
                TempData["ErrorMessage"] = "Bu puantaj henüz supervisor tarafından onaylanmamış.";
                return;
            }

            // İK son onayı verir
            timesheet.Status = ApprovalStatus.Approved;
            timesheet.ApproverId = currentUser.Id;
            timesheet.ApproverName = currentUser.FullName ?? currentUser.UserName;
            timesheet.ApprovalDate = DateTime.Now;
            timesheet.ApprovalNote = approvalNote;
            timesheet.UpdatedDate = DateTime.Now;

            TempData["SuccessMessage"] = $"{timesheet.Intern.FullName} adlı stajyerin puantajı İK tarafından final onayı verildi.";
        }

        // POST: Timesheets/Reject - Puantaj Reddetme (Sadece yetkili roller)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR,Supervisor,PersonelIsleri")]
        public async Task<IActionResult> Reject(int id, string approvalNote)
        {
            if (string.IsNullOrWhiteSpace(approvalNote))
            {
                TempData["ErrorMessage"] = "Red sebebi belirtilmelidir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var timesheet = await _context.Timesheets
                .Include(t => t.Intern)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (timesheet == null) return NotFound();

            if (timesheet.Status != ApprovalStatus.Pending && timesheet.Status != ApprovalStatus.Revision)
            {
                TempData["ErrorMessage"] = "Sadece beklemede veya revize durumundaki puantajlar reddedilebilir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUser = await _userManager.GetUserAsync(User);

            timesheet.Status = ApprovalStatus.Rejected;
            timesheet.ApproverId = currentUser?.Id;
            timesheet.ApproverName = currentUser?.FullName ?? currentUser?.UserName;
            timesheet.ApprovalDate = DateTime.Now;
            timesheet.ApprovalNote = approvalNote;
            timesheet.UpdatedDate = DateTime.Now;

            _context.Update(timesheet);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{timesheet.Intern.FullName} adlı stajyerin puantajı reddedildi.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Timesheets/RequestRevision - Puantaj Revizyon İsteme (Sadece yetkili roller)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR,Supervisor,PersonelIsleri")]
        public async Task<IActionResult> RequestRevision(int id, string approvalNote)
        {
            if (string.IsNullOrWhiteSpace(approvalNote))
            {
                TempData["ErrorMessage"] = "Revizyon notu belirtilmelidir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var timesheet = await _context.Timesheets
                .Include(t => t.Intern)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (timesheet == null) return NotFound();

            if (timesheet.Status != ApprovalStatus.Pending)
            {
                TempData["ErrorMessage"] = "Sadece beklemede olan puantajlar için revizyon istenebilir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUser = await _userManager.GetUserAsync(User);

            timesheet.Status = ApprovalStatus.Revision;
            timesheet.ApproverId = currentUser?.Id;
            timesheet.ApproverName = currentUser?.FullName ?? currentUser?.UserName;
            timesheet.ApprovalDate = DateTime.Now;
            timesheet.ApprovalNote = approvalNote;
            timesheet.UpdatedDate = DateTime.Now;

            _context.Update(timesheet);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{timesheet.Intern.FullName} adlı stajyerin puantajı için revizyon istendi.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Timesheets/Delete - Puantaj Sil (Sadece Admin ve kendi puantajı)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            var timesheet = await _context.Timesheets
                .Include(t => t.Intern)
                .Include(t => t.Approver)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (timesheet == null) return NotFound();

            // Sadece Admin veya kendi puantajı olan stajyer silebilir (ve sadece Pending durumunda)
            if (!userRoles.Contains("Admin"))
            {
                if (!userRoles.Contains("Intern") || timesheet.Intern.Email != currentUser.Email)
                {
                    TempData["ErrorMessage"] = "Bu puantaj kaydını silme yetkiniz bulunmamaktadır.";
                    return RedirectToAction(nameof(Index));
                }

                if (timesheet.Status != ApprovalStatus.Pending)
                {
                    TempData["ErrorMessage"] = "Sadece beklemede olan puantajlar silinebilir.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            return View(timesheet);
        }

        // POST: Timesheets/Delete - Puantaj Sil (Onay)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var userRoles = await _userManager.GetRolesAsync(currentUser);

                var timesheet = await _context.Timesheets
                    .Include(t => t.Intern)
                    .Include(t => t.TimesheetDetails)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (timesheet == null)
                {
                    TempData["ErrorMessage"] = "Silinecek puantaj bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                // Yetki kontrolü
                if (!userRoles.Contains("Admin"))
                {
                    if (!userRoles.Contains("Intern") || timesheet.Intern.Email != currentUser.Email)
                    {
                        TempData["ErrorMessage"] = "Bu puantaj kaydını silme yetkiniz bulunmamaktadır.";
                        return RedirectToAction(nameof(Index));
                    }

                    if (timesheet.Status != ApprovalStatus.Pending)
                    {
                        TempData["ErrorMessage"] = "Sadece beklemede olan puantajlar silinebilir.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                _context.Timesheets.Remove(timesheet);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Puantaj kaydı başarıyla silindi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Silme işlemi sırasında hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Puantaj Excel Export (Sadece HR ve Admin)
        [Authorize(Roles = "Admin,HR,PersonelIsleri")]
        public async Task<IActionResult> ExportToExcel(string status = "All")
        {
            var query = _context.Timesheets
                .Include(t => t.Intern)
                .Include(t => t.Approver)
                .Include(t => t.TimesheetDetails.OrderBy(td => td.WorkDate))
                .AsQueryable();

            // Status filtresi
            if (status != "All" && Enum.TryParse<ApprovalStatus>(status, out var statusEnum))
            {
                query = query.Where(t => t.Status == statusEnum);
            }

            var timesheets = await query
                .OrderByDescending(t => t.PeriodDate)
                .ThenByDescending(t => t.CreatedDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            // Worksheet 1: Puantaj Özeti
            var summaryWorksheet = workbook.Worksheets.Add("Puantaj Özeti");

            // Summary Headers
            summaryWorksheet.Cell(1, 1).Value = "Stajyer Adı";
            summaryWorksheet.Cell(1, 2).Value = "Departman";
            summaryWorksheet.Cell(1, 3).Value = "Şirket Sicil No";
            summaryWorksheet.Cell(1, 4).Value = "Sicil No";
            summaryWorksheet.Cell(1, 5).Value = "İşyeri";
            summaryWorksheet.Cell(1, 6).Value = "Dönem";
            summaryWorksheet.Cell(1, 7).Value = "Toplam Çalışma Günü";
            summaryWorksheet.Cell(1, 8).Value = "Toplam İzin Günü";
            summaryWorksheet.Cell(1, 9).Value = "Yemek Yardımı Günü";
            summaryWorksheet.Cell(1, 10).Value = "Eğitim Saati";
            summaryWorksheet.Cell(1, 11).Value = "Durum";
            summaryWorksheet.Cell(1, 12).Value = "Onaylayan";

            // Header styling
            var summaryHeaderRange = summaryWorksheet.Range(1, 1, 1, 12);
            summaryHeaderRange.Style.Font.Bold = true;
            summaryHeaderRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            summaryHeaderRange.Style.Font.FontColor = XLColor.White;
            summaryHeaderRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;

            // Summary data
            int summaryRow = 2;
            foreach (var timesheet in timesheets)
            {
                var mealAllowanceDays = timesheet.TimesheetDetails.Count(d => d.HasMealAllowance);

                summaryWorksheet.Cell(summaryRow, 1).Value = timesheet.Intern.FullName;
                summaryWorksheet.Cell(summaryRow, 2).Value = timesheet.Intern.Department;
                summaryWorksheet.Cell(summaryRow, 3).Value = timesheet.Intern.CompanyEmployeeNumber ?? "";
                summaryWorksheet.Cell(summaryRow, 4).Value = timesheet.Intern.EmployeeNumber ?? "";
                summaryWorksheet.Cell(summaryRow, 5).Value = timesheet.Intern.Workplace ?? "";
                summaryWorksheet.Cell(summaryRow, 6).Value = timesheet.PeriodDate.ToString("yyyy-MM");
                summaryWorksheet.Cell(summaryRow, 7).Value = timesheet.TotalWorkDays;
                summaryWorksheet.Cell(summaryRow, 8).Value = timesheet.TotalLeaveDays;
                summaryWorksheet.Cell(summaryRow, 9).Value = mealAllowanceDays;
                summaryWorksheet.Cell(summaryRow, 10).Value = timesheet.TotalTrainingHours;
                summaryWorksheet.Cell(summaryRow, 11).Value = GetApprovalStatusDescription(timesheet.Status);
                summaryWorksheet.Cell(summaryRow, 12).Value = timesheet.ApproverName ?? "";

                summaryRow++;
            }

            summaryWorksheet.Columns().AdjustToContents();

            var fileName = $"Puantaj_Raporu_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // Helper Methods
        private bool TimesheetExists(int id)
        {
            return _context.Timesheets.Any(e => e.Id == id);
        }

        private async Task PopulateDropDowns(int? selectedInternId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            // Eğer stajyer ise sadece kendi kaydını göster
            if (userRoles.Contains("Intern"))
            {
                var internRecord = await _context.Interns
                    .Where(i => i.Email == currentUser.Email && i.IsActive)
                    .ToListAsync();

                ViewBag.InternId = new SelectList(internRecord, "Id", "FullName", selectedInternId);
            }
            else
            {
                // Diğer roller tüm aktif stajyerleri görebilir
                var interns = await _context.Interns
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.FullName)
                    .ToListAsync();

                ViewBag.InternId = new SelectList(interns, "Id", "FullName", selectedInternId);
            }
        }

        // Helper method: Puantaj toplamlarını yeniden hesapla
        private async Task RecalculateTimesheetTotals(int timesheetId)
        {
            var timesheet = await _context.Timesheets
                .Include(t => t.TimesheetDetails)
                .FirstOrDefaultAsync(t => t.Id == timesheetId);

            if (timesheet != null)
            {
                timesheet.TotalWorkDays = timesheet.TimesheetDetails.Count(td => td.IsPresent);
                timesheet.TotalLeaveDays = timesheet.TimesheetDetails.Count(td => !td.IsPresent && td.LeaveHours > 0);
                timesheet.TotalTrainingHours = timesheet.TimesheetDetails.Sum(td => td.TrainingHours);
                timesheet.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();
            }
        }

        private async Task CreateMonthlyTimesheetDetails(Timesheet timesheet)
        {
            var startDate = new DateTime(timesheet.PeriodDate.Year, timesheet.PeriodDate.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var details = new List<TimesheetDetail>();

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var detail = new TimesheetDetail
                {
                    TimesheetId = timesheet.Id,
                    WorkDate = date,
                    DayNumber = date.Day,
                    DayName = GetTurkishDayName(date.DayOfWeek),
                    WorkLocation = WorkLocation.HeadOffice,

                    // TÜM GÜNLER BAŞLANGIÇTA İŞARETSİZ OLSUN
                    IsPresent = false,
                    StartTime = null,
                    EndTime = null,
                    HasMealAllowance = false,

                    LeaveHours = 0,
                    LeaveInfo = "",
                    TrainingHours = 0,
                    TrainingInfo = "",
                    Notes = ""
                };

                details.Add(detail);
            }

            timesheet.TimesheetDetails = details;

            // Başlangıçta tüm sayıları sıfır yap
            timesheet.TotalWorkDays = 0;
            timesheet.TotalLeaveDays = 0;
            timesheet.TotalTrainingHours = 0;
        }

        private string GetTurkishDayName(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Pazartesi",
                DayOfWeek.Tuesday => "Salı",
                DayOfWeek.Wednesday => "Çarşamba",
                DayOfWeek.Thursday => "Perşembe",
                DayOfWeek.Friday => "Cuma",
                DayOfWeek.Saturday => "Cumartesi",
                DayOfWeek.Sunday => "Pazar",
                _ => "Bilinmeyen"
            };
        }

        private string GetApprovalStatusDescription(ApprovalStatus status)
        {
            return status switch
            {
                ApprovalStatus.Pending => "Beklemede",
                ApprovalStatus.Approved => "Onaylandı",
                ApprovalStatus.Rejected => "Reddedildi",
                ApprovalStatus.Revision => "Revize",
                ApprovalStatus.Cancelled => "İptal",
                _ => "Bilinmeyen"
            };
        }
    }
}