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
    public class LeaveRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeaveRequestsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: LeaveRequests - İzin Talepleri Listesi
        // Stajyerler sadece kendi izinlerini, diğerleri herkesi görebilir
        public async Task<IActionResult> Index(string status = "All", int page = 1)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            var query = _context.LeaveRequests
                .Include(lr => lr.Intern)
                .Include(lr => lr.Approver)
                .AsQueryable();

            // Eğer stajyer ise sadece kendi izinlerini görsün
            if (userRoles.Contains("Intern"))
            {
                // Stajyer kendi email'i ile eşleşen izin taleplerini görebilir
                query = query.Where(lr => lr.Intern.Email == currentUser.Email);
            }
            // HR, Admin, Supervisor, PersonelIsleri herkesi görebilir

            // Status filtresi
            if (status != "All" && Enum.TryParse<ApprovalStatus>(status, out var statusEnum))
            {
                query = query.Where(lr => lr.Status == statusEnum);
            }

            // Sıralama (en yeni önce)
            query = query.OrderByDescending(lr => lr.CreatedDate);

            const int pageSize = 15;
            var totalCount = await query.CountAsync();
            var leaveRequests = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            // Status options for filter dropdown
            ViewBag.StatusOptions = new List<SelectListItem>
            {
                new() { Value = "All", Text = "Tümü" },
                new() { Value = "Pending", Text = "Beklemede" },
                new() { Value = "Approved", Text = "Onaylandı" },
                new() { Value = "Rejected", Text = "Reddedildi" }
            };

            // Kullanıcı rolü bilgisini view'a gönder
            ViewBag.IsIntern = userRoles.Contains("Intern");
            ViewBag.CanApprove = userRoles.Any(r => r == "Admin" || r == "HR" || r == "Supervisor");

            return View(leaveRequests);
        }

        // GET: LeaveRequests/Details/5 - İzin Talebi Detayları
        // Stajyerler sadece kendi izin detaylarını görebilir
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Intern)
                .Include(lr => lr.Approver)
                .Include(lr => lr.ApprovalHistories)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (leaveRequest == null) return NotFound();

            // Eğer stajyer ise sadece kendi izin detaylarını görebilir
            if (userRoles.Contains("Intern") && leaveRequest.Intern.Email != currentUser.Email)
            {
                TempData["ErrorMessage"] = "Bu izin talebine erişim yetkiniz bulunmamaktadır.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CanApprove = userRoles.Any(r => r == "Admin" || r == "HR" || r == "Supervisor");
            ViewBag.IsIntern = userRoles.Contains("Intern");

            return View(leaveRequest);
        }

        // GET: LeaveRequests/Create - Yeni İzin Talebi
        // Herkes izin talebi oluşturabilir ama giriş yapmış olmalı
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            // PersonelIsleri yeni izin talebi oluşturamaz
            if (userRoles.Contains("PersonelIsleri"))
            {
                TempData["ErrorMessage"] = "Personel İşleri kullanıcıları yeni izin talebi oluşturamaz.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropDowns();

            var model = new LeaveRequest
            {
                StartDateTime = DateTime.Today.AddHours(9), // 09:00
                EndDateTime = DateTime.Today.AddHours(17),  // 17:00
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

        // POST: LeaveRequests/Create - Yeni İzin Talebi Kaydet
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveRequest leaveRequest)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            // PersonelIsleri yeni izin talebi oluşturamaz
            if (userRoles.Contains("PersonelIsleri"))
            {
                TempData["ErrorMessage"] = "Personel İşleri kullanıcıları yeni izin talebi oluşturamaz.";
                return RedirectToAction(nameof(Index));
            }

            // Navigation property hatalarını temizle
            ModelState.Remove("Intern");
            ModelState.Remove("Approver");
            ModelState.Remove("ApprovalHistories");

            // Tarih kontrolü
            if (leaveRequest.StartDateTime > leaveRequest.EndDateTime)
            {
                ModelState.AddModelError("EndDateTime", "Dönüş tarihi çıkış tarihinden önce olamaz.");
            }

            // Geçmiş tarih kontrolü
            if (leaveRequest.StartDateTime < DateTime.Now.Date)
            {
                ModelState.AddModelError("StartDateTime", "Geçmiş tarihli izin talebi oluşturulamaz.");
            }

            // Eğer stajyer ise sadece kendi adına izin oluşturabilir
            if (userRoles.Contains("Intern"))
            {
                var internRecord = await _context.Interns
                    .FirstOrDefaultAsync(i => i.Email == currentUser.Email);

                if (internRecord == null)
                {
                    ModelState.AddModelError("", "Stajyer kaydınız bulunamadı. Lütfen yöneticinize başvurun.");
                }
                else if (leaveRequest.InternId != internRecord.Id)
                {
                    ModelState.AddModelError("", "Sadece kendi adınıza izin talebi oluşturabilirsiniz.");
                }
            }

            if (ModelState.IsValid)
            {
                // DÜZELTME: Toplam gün ve saat hesaplama
                var totalHours = CalculateLeaveHours(leaveRequest.StartDateTime, leaveRequest.EndDateTime);
                var totalDays = CalculateLeaveDays(leaveRequest.StartDateTime, leaveRequest.EndDateTime);

                leaveRequest.TotalHours = totalHours;
                leaveRequest.TotalDays = totalDays;

                // Çakışan izin kontrolü
                var conflictingLeave = await _context.LeaveRequests
                    .Where(lr => lr.InternId == leaveRequest.InternId)
                    .Where(lr => lr.Status == ApprovalStatus.Approved || lr.Status == ApprovalStatus.Pending)
                    .Where(lr =>
                        (lr.StartDateTime < leaveRequest.EndDateTime && lr.EndDateTime > leaveRequest.StartDateTime))
                    .FirstOrDefaultAsync();

                if (conflictingLeave != null)
                {
                    ModelState.AddModelError("", $"Bu tarih aralığında zaten bir izin talebiniz bulunmaktadır. ({conflictingLeave.StartDateTime:dd.MM.yyyy HH:mm} - {conflictingLeave.EndDateTime:dd.MM.yyyy HH:mm})");
                    await PopulateDropDowns(leaveRequest.InternId);
                    return View(leaveRequest);
                }

                // Manuel entry kontrolü (sadece HR/Admin manuel izin oluşturabilir)
                if (!userRoles.Contains("Intern"))
                {
                    leaveRequest.IsManualEntry = true;
                    leaveRequest.ManualEntryBy = currentUser?.UserName;
                }

                leaveRequest.CreatedDate = DateTime.Now;
                leaveRequest.Status = ApprovalStatus.Pending;

                _context.Add(leaveRequest);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "İzin talebiniz başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropDowns(leaveRequest.InternId);
            return View(leaveRequest);
        }


        // GET: LeaveRequests/Edit - İzin Düzenleme (Stajyer + Yetkili roller)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Intern)
                .FirstOrDefaultAsync(lr => lr.Id == id);

            if (leaveRequest == null) return NotFound();

            // Yetki kontrolü
            bool canEdit = userRoles.Any(r => r == "Admin" || r == "HR" || r == "Supervisor") ||
                           (userRoles.Contains("Intern") && leaveRequest.Intern.Email == currentUser.Email);

            if (!canEdit)
            {
                TempData["ErrorMessage"] = "Bu izin talebini düzenleme yetkiniz bulunmuyor.";
                return RedirectToAction(nameof(Index));
            }

            // Sadece beklemede veya revizyon durumundaki talepler düzenlenebilir
            if (leaveRequest.Status != ApprovalStatus.Pending && leaveRequest.Status != ApprovalStatus.Revision)
            {
                TempData["ErrorMessage"] = "Sadece beklemede veya revizyon durumundaki talepler düzenlenebilir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await PopulateDropDowns(leaveRequest.InternId);
            return View(leaveRequest);
        }

        // POST: LeaveRequests/RequestRevision - İzin Revizyon İsteme (Sadece yetkili roller)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR,Supervisor")]
        public async Task<IActionResult> RequestRevision(int id, string approvalNote)
        {
            if (string.IsNullOrWhiteSpace(approvalNote))
            {
                TempData["ErrorMessage"] = "Revizyon notu belirtilmelidir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Intern)
                .FirstOrDefaultAsync(lr => lr.Id == id);

            if (leaveRequest == null) return NotFound();

            if (leaveRequest.Status != ApprovalStatus.Pending)
            {
                TempData["ErrorMessage"] = "Sadece beklemede olan izin talepleri için revizyon istenebilir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUser = await _userManager.GetUserAsync(User);

            leaveRequest.Status = ApprovalStatus.Revision;
            leaveRequest.ApproverId = currentUser?.Id;
            leaveRequest.ApproverName = currentUser?.FullName ?? currentUser?.UserName;
            leaveRequest.ApprovalDate = DateTime.Now;
            leaveRequest.ApprovalNote = approvalNote;
            leaveRequest.UpdatedDate = DateTime.Now;

            _context.Update(leaveRequest);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{leaveRequest.Intern.FullName} adlı stajyerin izin talebi için revizyon istendi.";
            return RedirectToAction(nameof(Index));
        }

        // POST: LeaveRequests/Edit - İzin Güncelleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LeaveRequest leaveRequest)
        {
            if (id != leaveRequest.Id) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            // Yetki kontrolü
            var existingLeaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Intern)
                .FirstOrDefaultAsync(lr => lr.Id == id);

            if (existingLeaveRequest == null) return NotFound();

            bool canEdit = userRoles.Any(r => r == "Admin" || r == "HR" || r == "Supervisor") ||
                           (userRoles.Contains("Intern") && existingLeaveRequest.Intern.Email == currentUser.Email);

            if (!canEdit)
            {
                TempData["ErrorMessage"] = "Bu izin talebini düzenleme yetkiniz bulunmuyor.";
                return RedirectToAction(nameof(Index));
            }

            // Navigation property hatalarını temizle
            ModelState.Remove("Intern");
            ModelState.Remove("Approver");
            ModelState.Remove("ApprovalHistories");

            // Tarih kontrolü
            if (leaveRequest.StartDateTime > leaveRequest.EndDateTime)
            {
                ModelState.AddModelError("EndDateTime", "Dönüş tarihi çıkış tarihinden önce olamaz.");
            }

            // Geçmiş tarih kontrolü
            if (leaveRequest.StartDateTime < DateTime.Now.Date)
            {
                ModelState.AddModelError("StartDateTime", "Geçmiş tarihli izin talebi oluşturulamaz.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Sadece belirli alanları güncelle
                    existingLeaveRequest.LeaveType = leaveRequest.LeaveType;
                    existingLeaveRequest.StartDateTime = leaveRequest.StartDateTime;
                    existingLeaveRequest.EndDateTime = leaveRequest.EndDateTime;
                    existingLeaveRequest.Reason = leaveRequest.Reason;
                    existingLeaveRequest.ShouldReflectToTimesheet = leaveRequest.ShouldReflectToTimesheet;
                    existingLeaveRequest.UpdatedDate = DateTime.Now;

                    // Toplam süreyi hesapla
                    var totalHours = (decimal)(leaveRequest.EndDateTime - leaveRequest.StartDateTime).TotalHours;
                    existingLeaveRequest.TotalHours = totalHours;
                    existingLeaveRequest.TotalDays = totalHours >= 8 ? (int)Math.Ceiling(totalHours / 8) : 0;

                    // Eğer stajyer düzenliyorsa, durumu beklemede yap
                    if (userRoles.Contains("Intern") && existingLeaveRequest.Status == ApprovalStatus.Revision)
                    {
                        existingLeaveRequest.Status = ApprovalStatus.Pending;
                        existingLeaveRequest.ApprovalNote = null;
                        existingLeaveRequest.ApprovalDate = null;
                        existingLeaveRequest.ApproverId = null;
                        existingLeaveRequest.ApproverName = null;
                    }

                    // Çakışma kontrolü (düzenlenen kayıt hariç)
                    var conflictingLeave = await _context.LeaveRequests
                        .Where(lr => lr.InternId == existingLeaveRequest.InternId &&
                                    lr.Id != existingLeaveRequest.Id &&
                                    lr.Status == ApprovalStatus.Approved &&
                                    ((lr.StartDateTime <= leaveRequest.StartDateTime && lr.EndDateTime > leaveRequest.StartDateTime) ||
                                     (lr.StartDateTime < leaveRequest.EndDateTime && lr.EndDateTime >= leaveRequest.EndDateTime) ||
                                     (lr.StartDateTime >= leaveRequest.StartDateTime && lr.EndDateTime <= leaveRequest.EndDateTime)))
                        .FirstOrDefaultAsync();

                    if (conflictingLeave != null)
                    {
                        ModelState.AddModelError("",
                            $"Bu tarihler arasında onaylanmış başka bir izin bulunmaktadır. " +
                            $"({conflictingLeave.StartDateTime:dd.MM.yyyy HH:mm} - {conflictingLeave.EndDateTime:dd.MM.yyyy HH:mm})");
                        await PopulateDropDowns(leaveRequest.InternId);
                        return View(leaveRequest);
                    }

                    _context.Update(existingLeaveRequest);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "İzin talebi başarıyla güncellendi.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LeaveRequestExists(leaveRequest.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await PopulateDropDowns(leaveRequest.InternId);
            return View(leaveRequest);
        }

        // POST: LeaveRequests/Approve - İzin Onaylama (Sadece Yetkililer)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR,Supervisor")]
        public async Task<IActionResult> Approve(int id, string? approvalNote = null)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Intern)
                .FirstOrDefaultAsync(lr => lr.Id == id);

            if (leaveRequest == null) return NotFound();

            if (leaveRequest.Status != ApprovalStatus.Pending && leaveRequest.Status != ApprovalStatus.Revision)
            {
                TempData["ErrorMessage"] = "Sadece beklemede veya revize durumundaki talepler onaylanabilir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUser = await _userManager.GetUserAsync(User);

            leaveRequest.Status = ApprovalStatus.Approved;
            leaveRequest.ApproverId = currentUser?.Id;
            leaveRequest.ApproverName = currentUser?.FullName ?? currentUser?.UserName;
            leaveRequest.ApprovalDate = DateTime.Now;
            leaveRequest.ApprovalNote = approvalNote;
            leaveRequest.UpdatedDate = DateTime.Now;

            _context.Update(leaveRequest);
            await _context.SaveChangesAsync();

            // YENİ: İzin onaylandığında puantaja otomatik yansıt
            if (leaveRequest.ShouldReflectToTimesheet)
            {
                await ReflectLeaveToTimesheet(leaveRequest);
            }

            TempData["SuccessMessage"] = $"{leaveRequest.Intern.FullName} adlı stajyerin izin talebi onaylandı.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // DÜZELTİLMİŞ: İzni puantaja yansıtma
        private async Task ReflectLeaveToTimesheet(LeaveRequest leaveRequest)
        {
            try
            {
                // AYNI GÜN İZİNLERİ İÇİN ÖZEL KONTROL
                if (leaveRequest.StartDateTime.Date == leaveRequest.EndDateTime.Date)
                {
                    // Aynı gün içerisindeki izin
                    await ProcessSameDayLeave(leaveRequest);
                }
                else
                {
                    // Birden fazla gün süren izin
                    await ProcessMultiDayLeave(leaveRequest);
                }

                // Puantaj toplamlarını yeniden hesapla
                var affectedTimesheets = await _context.Timesheets
                    .Where(t => t.InternId == leaveRequest.InternId &&
                               t.PeriodDate >= new DateTime(leaveRequest.StartDateTime.Year, leaveRequest.StartDateTime.Month, 1) &&
                               t.PeriodDate <= new DateTime(leaveRequest.EndDateTime.Year, leaveRequest.EndDateTime.Month, 1))
                    .ToListAsync();

                foreach (var timesheet in affectedTimesheets)
                {
                    await RecalculateTimesheetTotals(timesheet.Id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"İzin puantaga yansıtılırken hata: {ex.Message}");
                // Hata logla ama approval process'i durdurmaTempData["ErrorMessage"] = $"İzin puantaja yansıtılırken hata: {ex.Message}";
            }
        }

        // AYNI GÜN İZİNLERİ İÇİN YENİ METOD
        private async Task ProcessSameDayLeave(LeaveRequest leaveRequest)
        {
            var date = leaveRequest.StartDateTime.Date;

            // Bu ay için puantaj var mı kontrol et
            var timesheet = await _context.Timesheets
                .Include(t => t.TimesheetDetails)
                .FirstOrDefaultAsync(t => t.InternId == leaveRequest.InternId &&
                                        t.PeriodDate.Year == date.Year &&
                                        t.PeriodDate.Month == date.Month);

            if (timesheet == null)
            {
                // Puantaj yoksa oluştur
                timesheet = new Timesheet
                {
                    InternId = leaveRequest.InternId,
                    PeriodDate = new DateTime(date.Year, date.Month, 1),
                    Status = ApprovalStatus.Pending,
                    CreatedDate = DateTime.Now,
                    IsManualEntry = true,
                    ManualEntryBy = "System_LeaveIntegration"
                };

                _context.Timesheets.Add(timesheet);
                await _context.SaveChangesAsync();

                // Aylık detayları oluştur
                await CreateMonthlyTimesheetDetailsForLeave(timesheet);

                // Yeniden yükle
                timesheet = await _context.Timesheets
                    .Include(t => t.TimesheetDetails)
                    .FirstOrDefaultAsync(t => t.Id == timesheet.Id);
            }

            // Bu tarihe ait puantaj detayını bul
            var timesheetDetail = timesheet.TimesheetDetails
                .FirstOrDefault(td => td.WorkDate.Date == date.Date);

            if (timesheetDetail != null)
            {
                // AYNI GÜN içinde başlayıp bitiyor (örn: 09:00-14:00 = 5 saat)
                var leaveHours = (decimal)(leaveRequest.EndDateTime - leaveRequest.StartDateTime).TotalHours;

                // İzin bilgilerini güncelle
                timesheetDetail.LeaveInfo = $"{GetLeaveTypeDescription(leaveRequest.LeaveType)} - Onaylandı";
                timesheetDetail.LeaveHours = leaveHours;

                // İŞ KURALI: 4.5 saatten fazla izin = yarım gün, 4.5 saat ve altı = tam gün
                if (leaveHours > 4.5m)
                {
                    // 4.5 saatten fazla izin - YARIM GÜN sayılır
                    timesheetDetail.IsPresent = false;
                    timesheetDetail.StartTime = null;
                    timesheetDetail.EndTime = null;
                    timesheetDetail.HasMealAllowance = false;
                }
                else
                {
                    // 4.5 saat ve altı izin - TAM GÜN sayılır
                    timesheetDetail.IsPresent = true;
                    // Çalışma saatleri varsayılan olarak ayarlanır
                    if (timesheetDetail.StartTime == null) timesheetDetail.StartTime = TimeSpan.FromHours(8.5); // 08:30
                    if (timesheetDetail.EndTime == null) timesheetDetail.EndTime = TimeSpan.FromHours(17.5); // 17:30
                    timesheetDetail.HasMealAllowance = true; // Tam gün geldi sayıldığı için yemek hakkı var
                }

                timesheetDetail.Notes = $"İzin Talebi #{leaveRequest.Id} - {leaveRequest.StartDateTime:dd.MM.yyyy HH:mm} / {leaveRequest.EndDateTime:dd.MM.yyyy HH:mm} ({leaveHours:F1} saat)";

                await _context.SaveChangesAsync();
            }
        }

        // ÇOK GÜNLÜK İZİNLER İÇİN MEVCUT METOD
        private async Task ProcessMultiDayLeave(LeaveRequest leaveRequest)
        {
            // Dönüş tarihi DAHİL EDİLMEZ - o gün işe dönüş günüdür
            for (var date = leaveRequest.StartDateTime.Date; date < leaveRequest.EndDateTime.Date; date = date.AddDays(1))
            {
                // Bu ay için puantaj var mı kontrol et
                var timesheet = await _context.Timesheets
                    .Include(t => t.TimesheetDetails)
                    .FirstOrDefaultAsync(t => t.InternId == leaveRequest.InternId &&
                                            t.PeriodDate.Year == date.Year &&
                                            t.PeriodDate.Month == date.Month);

                if (timesheet == null)
                {
                    // Puantaj yoksa oluştur
                    timesheet = new Timesheet
                    {
                        InternId = leaveRequest.InternId,
                        PeriodDate = new DateTime(date.Year, date.Month, 1),
                        Status = ApprovalStatus.Pending,
                        CreatedDate = DateTime.Now,
                        IsManualEntry = true,
                        ManualEntryBy = "System_LeaveIntegration"
                    };

                    _context.Timesheets.Add(timesheet);
                    await _context.SaveChangesAsync();

                    // Aylık detayları oluştur
                    await CreateMonthlyTimesheetDetailsForLeave(timesheet);

                    // Yeniden yükle
                    timesheet = await _context.Timesheets
                        .Include(t => t.TimesheetDetails)
                        .FirstOrDefaultAsync(t => t.Id == timesheet.Id);
                }

                // Bu tarihe ait puantaj detayını bul
                var timesheetDetail = timesheet.TimesheetDetails
                    .FirstOrDefault(td => td.WorkDate.Date == date.Date);

                if (timesheetDetail != null)
                {
                    // İzin bilgilerini güncelle
                    timesheetDetail.IsPresent = false;
                    timesheetDetail.LeaveInfo = $"{GetLeaveTypeDescription(leaveRequest.LeaveType)} - Onaylandı";

                    // Saatlik hesaplama
                    if (date.Date == leaveRequest.StartDateTime.Date)
                    {
                        // İLK GÜN - başlangıç saatinden gün sonuna kadar
                        var endOfWorkDay = date.Date.AddHours(17).AddMinutes(30); // 17:30
                        timesheetDetail.LeaveHours = (decimal)(endOfWorkDay - leaveRequest.StartDateTime).TotalHours;

                        if (leaveRequest.StartDateTime.TimeOfDay > TimeSpan.FromHours(17.5))
                        {
                            timesheetDetail.LeaveHours = 0; // O gün izin yok
                        }
                    }
                    else if (date.Date == leaveRequest.EndDateTime.Date.AddDays(-1))
                    {
                        // SON GÜN öncesi - tam gün izin
                        timesheetDetail.LeaveHours = 9; // Standart 9 saatlik çalışma günü
                    }
                    else
                    {
                        // ARA GÜNLER - tam gün izin
                        timesheetDetail.LeaveHours = 9; // Standart 9 saatlik çalışma günü
                    }

                    timesheetDetail.StartTime = null;
                    timesheetDetail.EndTime = null;
                    timesheetDetail.HasMealAllowance = false;
                    timesheetDetail.Notes = $"İzin Talebi #{leaveRequest.Id} - {leaveRequest.StartDateTime:dd.MM.yyyy HH:mm} / {leaveRequest.EndDateTime:dd.MM.yyyy HH:mm}";

                    await _context.SaveChangesAsync();
                }
            }
        }

        // POST: LeaveRequests/Reject - İzin Reddetme(Sadece yetkili roller)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR,Supervisor")]
        public async Task<IActionResult> Reject(int id, string approvalNote)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Intern)
                .FirstOrDefaultAsync(lr => lr.Id == id);

            if (leaveRequest == null) return NotFound();

            if (leaveRequest.Status != ApprovalStatus.Pending && leaveRequest.Status != ApprovalStatus.Revision)
            {
                TempData["ErrorMessage"] = "Sadece beklemede veya revize durumundaki talepler reddedilebilir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(approvalNote))
            {
                TempData["ErrorMessage"] = "Red nedeni belirtilmelidir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUser = await _userManager.GetUserAsync(User);

            leaveRequest.Status = ApprovalStatus.Rejected;
            leaveRequest.ApproverId = currentUser?.Id;
            leaveRequest.ApproverName = currentUser?.FullName ?? currentUser?.UserName;
            leaveRequest.ApprovalDate = DateTime.Now;
            leaveRequest.ApprovalNote = approvalNote;
            leaveRequest.UpdatedDate = DateTime.Now;

            _context.Update(leaveRequest);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{leaveRequest.Intern.FullName} adlı stajyerin izin talebi reddedildi.";
            return RedirectToAction(nameof(Index));
        }

        // GET: LeaveRequests/Delete - İzin Talebini Sil (Sadece Admin ve kendi talebi)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(currentUser);

            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Intern)
                .Include(lr => lr.Approver)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (leaveRequest == null) return NotFound();

            // Sadece Admin veya kendi talebi olan stajyer silebilir (ve sadece Pending durumunda)
            if (!userRoles.Contains("Admin"))
            {
                if (!userRoles.Contains("Intern") || leaveRequest.Intern.Email != currentUser.Email)
                {
                    TempData["ErrorMessage"] = "Bu izin talebini silme yetkiniz bulunmamaktadır.";
                    return RedirectToAction(nameof(Index));
                }

                if (leaveRequest.Status != ApprovalStatus.Pending)
                {
                    TempData["ErrorMessage"] = "Sadece beklemede olan izin talepleri silinebilir.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            return View(leaveRequest);
        }

        // POST: LeaveRequests/Delete - İzin Talebini Sil (Onay)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var userRoles = await _userManager.GetRolesAsync(currentUser);

                var leaveRequest = await _context.LeaveRequests
                    .Include(lr => lr.Intern)
                    .FirstOrDefaultAsync(lr => lr.Id == id);

                if (leaveRequest == null)
                {
                    TempData["ErrorMessage"] = "Silinecek izin talebi bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                // Yetki kontrolü
                if (!userRoles.Contains("Admin"))
                {
                    if (!userRoles.Contains("Intern") || leaveRequest.Intern.Email != currentUser.Email)
                    {
                        TempData["ErrorMessage"] = "Bu izin talebini silme yetkiniz bulunmamaktadır.";
                        return RedirectToAction(nameof(Index));
                    }

                    if (leaveRequest.Status != ApprovalStatus.Pending)
                    {
                        TempData["ErrorMessage"] = "Sadece beklemede olan izin talepleri silinebilir.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                _context.LeaveRequests.Remove(leaveRequest);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "İzin talebi başarıyla silindi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Silme işlemi sırasında hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Excel Export (Sadece PersonelIsleri)
        [Authorize(Roles = "PersonelIsleri")]
        public async Task<IActionResult> ExportToExcel(string status = "All", DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.LeaveRequests
                .Include(lr => lr.Intern)
                .Include(lr => lr.Approver)
                .AsQueryable();

            // Status filtresi
            if (status != "All" && Enum.TryParse<ApprovalStatus>(status, out var statusEnum))
            {
                query = query.Where(lr => lr.Status == statusEnum);
            }

            // Tarih filtresi
            if (startDate.HasValue)
            {
                query = query.Where(lr => lr.StartDateTime.Date >= startDate.Value.Date);
            }
            if (endDate.HasValue)
            {
                query = query.Where(lr => lr.EndDateTime.Date <= endDate.Value.Date);
            }

            var leaveRequests = await query
                .OrderByDescending(lr => lr.CreatedDate)
                .ToListAsync();

            // Excel dosyası oluştur
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("İzin Talepleri");

            // Başlık satırı
            var headers = new[]
            {
        "Sıra No", "Stajyer Adı", "Departman", "İzin Türü", "Çıkış Tarihi", "Çıkış Saati",
        "Dönüş Tarihi", "Dönüş Saati", "Toplam Gün", "Toplam Saat", "İzin Sebebi",
        "Durum", "Onaylayan", "Onay Tarihi", "Onay Notu", "Talep Tarihi", "Manuel Form", "Puantaja Yansıt"
    };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Veri satırları
            int row = 2;
            foreach (var leave in leaveRequests)
            {
                worksheet.Cell(row, 1).Value = row - 1; // Sıra no
                worksheet.Cell(row, 2).Value = leave.Intern.FullName;
                worksheet.Cell(row, 3).Value = leave.Intern.Department;
                worksheet.Cell(row, 4).Value = GetLeaveTypeDescription(leave.LeaveType);
                worksheet.Cell(row, 5).Value = leave.StartDateTime.ToString("dd.MM.yyyy");
                worksheet.Cell(row, 6).Value = leave.StartDateTime.ToString("HH:mm");
                worksheet.Cell(row, 7).Value = leave.EndDateTime.ToString("dd.MM.yyyy");
                worksheet.Cell(row, 8).Value = leave.EndDateTime.ToString("HH:mm");
                worksheet.Cell(row, 9).Value = leave.TotalDays;
                worksheet.Cell(row, 10).Value = leave.TotalHours.ToString("F1");
                worksheet.Cell(row, 11).Value = leave.Reason ?? "";
                worksheet.Cell(row, 12).Value = GetApprovalStatusDescription(leave.Status);
                worksheet.Cell(row, 13).Value = leave.ApproverName ?? "";
                worksheet.Cell(row, 14).Value = leave.ApprovalDate?.ToString("dd.MM.yyyy HH:mm") ?? "";
                worksheet.Cell(row, 15).Value = leave.ApprovalNote ?? "";
                worksheet.Cell(row, 16).Value = leave.CreatedDate.ToString("dd.MM.yyyy HH:mm");
                worksheet.Cell(row, 17).Value = leave.IsManualEntry ? "Evet" : "Hayır";
                worksheet.Cell(row, 18).Value = leave.ShouldReflectToTimesheet ? "Evet" : "Hayır";

                row++;
            }

            // Sütun genişliklerini ayarla
            worksheet.Columns().AdjustToContents();

            // Tablo formatı uygula
            var dataRange = worksheet.Range(1, 1, row - 1, headers.Length);
            var table = dataRange.CreateTable();
            table.Theme = XLTableTheme.TableStyleMedium2;

            // Dosya adı oluştur
            var fileName = $"Izin_Talepleri_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

            // Excel dosyasını gönder
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        // Helper Methods


        // İzin saatlerini hesaplar - standart çalışma saatleri 08:30-17:30 (9 saat)
        private decimal CalculateLeaveHours(DateTime startDateTime, DateTime endDateTime)
        {
            decimal totalHours = 0;

            // Aynı günde başlayıp bitiyor mu?
            if (startDateTime.Date == endDateTime.Date)
            {
                // Aynı gün içerisinde saat bazlı izin
                totalHours = (decimal)(endDateTime - startDateTime).TotalHours;
            }
            else
            {
                // Birden fazla gün
                var currentDate = startDateTime.Date;

                while (currentDate < endDateTime.Date) // Dönüş günü DAHİL EDİLMEZ
                {
                    if (currentDate == startDateTime.Date)
                    {
                        // İlk gün - başlangıç saatinden gün sonuna kadar
                        var endOfWorkDay = currentDate.AddHours(17).AddMinutes(30); // 17:30
                        var firstDayHours = (decimal)(endOfWorkDay - startDateTime).TotalHours;

                        // Eğer negatifse veya çok büyükse, standart çalışma saati ver
                        if (firstDayHours <= 0 || firstDayHours > 9)
                            firstDayHours = 9;

                        totalHours += firstDayHours;
                    }
                    else
                    {
                        // Ara günler - tam çalışma günü (9 saat)
                        totalHours += 9;
                    }

                    currentDate = currentDate.AddDays(1);
                }
            }

            return Math.Round(totalHours, 2);
        }

        // İzin günlerini hesaplar - dönüş günü dahil edilmez
        private int CalculateLeaveDays(DateTime startDateTime, DateTime endDateTime)
        {
            // Dönüş günü dahil edilmez - o gün işe dönüş günüdür
            var totalDays = (endDateTime.Date - startDateTime.Date).Days;

            // Minimum 0, eğer aynı günse 0 gün (sadece saatlik izin)
            return Math.Max(0, totalDays);
        }

        private bool LeaveRequestExists(int id)
        {
            return _context.LeaveRequests.Any(e => e.Id == id);
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

            ViewBag.LeaveTypes = new SelectList(
                Enum.GetValues(typeof(LeaveType))
                    .Cast<LeaveType>()
                    .Select(e => new
                    {
                        Value = (int)e,
                        Text = e.GetDisplayName()
                    }),
                "Value",
                "Text");
        }

        // Aylık puantaj detayları oluştur (sadece izin için)
        private async Task CreateMonthlyTimesheetDetailsForLeave(Timesheet timesheet)
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
                    IsPresent = false, // Başlangıçta tüm günler işaretsiz
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
            timesheet.TotalWorkDays = 0;
            timesheet.TotalLeaveDays = 0;
            timesheet.TotalTrainingHours = 0;
        }

        private string GetLeaveTypeDescription(LeaveType leaveType)
        {
            return leaveType switch
            {
                LeaveType.PersonalLeave => "Özel İzin",
                LeaveType.ExamLeave => "Sınav İzni",
                LeaveType.HealthLeave => "Sağlık İzni",
                LeaveType.ExcuseLeave => "Mazeret İzni",
                LeaveType.UnpaidLeave => "Ücretsiz İzin",
                _ => "İzin"
            };
        }

        // Puantaj toplamlarını yeniden hesapla (mevcut metoda eklenmeli)
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

