// ============================================================================
// Controllers/LeaveRequestsController.cs - İzin Talepleri Controller'ı (Gerçek Model'e Uygun)
// ============================================================================

using MedasStajyerYonetimSistemi.Data;
using MedasStajyerYonetimSistemi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MedasStajyerYonetimSistemi.Controllers
{
    public class LeaveRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeaveRequestsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ========================================================================
        // GET: LeaveRequests - İzin Talepleri Listesi
        // ========================================================================
        public async Task<IActionResult> Index(string status = "All", int page = 1)
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

            return View(leaveRequests);
        }

        // ========================================================================
        // GET: LeaveRequests/Details/5 - İzin Talebi Detayları
        // ========================================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Intern)
                .Include(lr => lr.Approver)
                .Include(lr => lr.ApprovalHistories)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (leaveRequest == null) return NotFound();

            return View(leaveRequest);
        }

        // ========================================================================
        // GET: LeaveRequests/Create - Yeni İzin Talebi
        // ========================================================================
        public async Task<IActionResult> Create()
        {
            await PopulateDropDowns();

            var model = new LeaveRequest
            {
                StartDateTime = DateTime.Today.AddHours(9), // 09:00
                EndDateTime = DateTime.Today.AddHours(17),  // 17:00
                CreatedDate = DateTime.Now
            };

            return View(model);
        }

        // ========================================================================
        // POST: LeaveRequests/Create - Yeni İzin Talebi Kaydet
        // ========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveRequest leaveRequest)
        {
            Console.WriteLine("=== CREATE POST ACTION CALLED ===");
            Console.WriteLine($"InternId: {leaveRequest.InternId}");
            Console.WriteLine($"LeaveType: {leaveRequest.LeaveType}");
            ModelState.Remove("Intern");
            ModelState.Remove("Approver");
            ModelState.Remove("ApprovalHistories");
            Console.WriteLine($"ModelState IsValid: {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }

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
                // Toplam gün ve saat hesaplama
                var timeSpan = leaveRequest.EndDateTime - leaveRequest.StartDateTime;
                leaveRequest.TotalDays = (int)Math.Ceiling(timeSpan.TotalDays);
                leaveRequest.TotalHours = (decimal)timeSpan.TotalHours;

                // Çakışan izin kontrolü
                var conflictingLeave = await _context.LeaveRequests
                    .Where(lr => lr.InternId == leaveRequest.InternId)
                    .Where(lr => lr.Status == ApprovalStatus.Approved || lr.Status == ApprovalStatus.Pending)
                    .Where(lr =>
                        (lr.StartDateTime <= leaveRequest.EndDateTime && lr.EndDateTime >= leaveRequest.StartDateTime))
                    .FirstOrDefaultAsync();

                if (conflictingLeave != null)
                {
                    ModelState.AddModelError("", $"Bu tarih aralığında zaten bir izin talebiniz bulunmaktadır. ({conflictingLeave.StartDateTime:dd.MM.yyyy HH:mm} - {conflictingLeave.EndDateTime:dd.MM.yyyy HH:mm})");
                    await PopulateDropDowns(leaveRequest.InternId);
                    return View(leaveRequest);
                }

                // Manuel entry kontrolü
                if (leaveRequest.IsManualEntry)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
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

        // ========================================================================
        // GET: LeaveRequests/Edit/5 - İzin Talebini Düzenle (Sadece Pending olanlar)
        // ========================================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest == null) return NotFound();

            if (leaveRequest.Status != ApprovalStatus.Pending)
            {
                TempData["ErrorMessage"] = "Sadece beklemede olan izin talepleri düzenlenebilir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await PopulateDropDowns(leaveRequest.InternId);
            return View(leaveRequest);
        }

        // ========================================================================
        // POST: LeaveRequests/Edit/5 - İzin Talebini Güncelle
        // ========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LeaveRequest leaveRequest)
        {
            Console.WriteLine("=== EDIT POST ACTION CALLED ===");
            Console.WriteLine($"ID: {id}");
            Console.WriteLine($"LeaveRequest ID: {leaveRequest.Id}");
            Console.WriteLine($"InternId: {leaveRequest.InternId}");
            Console.WriteLine($"LeaveType: {leaveRequest.LeaveType}");

            if (id != leaveRequest.Id) return NotFound();

            // Navigation property hatalarını temizle
            ModelState.Remove("Intern");
            ModelState.Remove("Approver");
            ModelState.Remove("ApprovalHistories");

            Console.WriteLine($"ModelState IsValid: {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }
            {
                if (id != leaveRequest.Id) return NotFound();

                // Tarih kontrolü
                if (leaveRequest.StartDateTime > leaveRequest.EndDateTime)
                {
                    ModelState.AddModelError("EndDateTime", "Dönüş tarihi çıkış tarihinden önce olamaz.");
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        // Toplam gün ve saat hesaplama
                        var timeSpan = leaveRequest.EndDateTime - leaveRequest.StartDateTime;
                        leaveRequest.TotalDays = (int)Math.Ceiling(timeSpan.TotalDays);
                        leaveRequest.TotalHours = (decimal)timeSpan.TotalHours;

                        // Çakışan izin kontrolü (kendisi hariç)
                        var conflictingLeave = await _context.LeaveRequests
                            .Where(lr => lr.InternId == leaveRequest.InternId && lr.Id != leaveRequest.Id)
                            .Where(lr => lr.Status == ApprovalStatus.Approved || lr.Status == ApprovalStatus.Pending)
                            .Where(lr =>
                                (lr.StartDateTime <= leaveRequest.EndDateTime && lr.EndDateTime >= leaveRequest.StartDateTime))
                            .FirstOrDefaultAsync();

                        if (conflictingLeave != null)
                        {
                            ModelState.AddModelError("", $"Bu tarih aralığında zaten bir izin talebiniz bulunmaktadır. ({conflictingLeave.StartDateTime:dd.MM.yyyy HH:mm} - {conflictingLeave.EndDateTime:dd.MM.yyyy HH:mm})");
                            await PopulateDropDowns(leaveRequest.InternId);
                            return View(leaveRequest);
                        }

                        leaveRequest.UpdatedDate = DateTime.Now;
                        _context.Update(leaveRequest);
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "İzin talebi başarıyla güncellendi.";
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
                    return RedirectToAction(nameof(Index));
                }

                await PopulateDropDowns(leaveRequest.InternId);
                return View(leaveRequest);
            }
        }

        // ========================================================================
        // POST: LeaveRequests/Approve/5 - İzin Talebini Onayla
        // ========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? approvalNote = null)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Intern)
                .FirstOrDefaultAsync(lr => lr.Id == id);

            if (leaveRequest == null) return NotFound();

            if (leaveRequest.Status != ApprovalStatus.Pending)
            {
                TempData["ErrorMessage"] = "Sadece beklemede olan izin talepleri onaylanabilir.";
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

            // ============================================================================
            // YENİ: İzin onaylandıktan sonra puantaja yansıt
            // ============================================================================
            if (leaveRequest.ShouldReflectToTimesheet)
            {
                await ReflectLeaveToTimesheet(leaveRequest);
            }

            TempData["SuccessMessage"] = $"{leaveRequest.Intern.FullName} adlı stajyerin izin talebi onaylandı.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ========================================================================
        // YENİ HELPER METHOD: İzni Puantaja Yansıt
        // ========================================================================
        private async Task ReflectLeaveToTimesheet(LeaveRequest leaveRequest)
        {
            try
            {
                // İzin tarihlerini al
                var leaveStartDate = leaveRequest.StartDateTime.Date;
                var leaveEndDate = leaveRequest.EndDateTime.Date;

                // İzin kapsamındaki tarihlerde puantaj detaylarını bul
                var affectedTimesheets = await _context.Timesheets
                    .Include(t => t.TimesheetDetails)
                    .Where(t => t.InternId == leaveRequest.InternId)
                    .Where(t => t.PeriodDate.Year >= leaveStartDate.Year &&
                               t.PeriodDate.Month >= leaveStartDate.Month)
                    .ToListAsync();

                foreach (var timesheet in affectedTimesheets)
                {
                    bool timesheetUpdated = false;
                    string leaveDescription = GetLeaveTypeDescription(leaveRequest.LeaveType);

                    // Bu puantajdaki izin kapsamına giren günleri bul
                    var affectedDetails = timesheet.TimesheetDetails
                        .Where(d => d.WorkDate >= leaveStartDate && d.WorkDate <= leaveEndDate)
                        .ToList();

                    foreach (var detail in affectedDetails)
                    {
                        // İzin saatini hesapla (o gün için)
                        var leaveHoursForDay = CalculateLeaveHoursForDay(leaveRequest, detail.WorkDate);

                        if (leaveHoursForDay > 0)
                        {
                            // İzin bilgilerini ekle/güncelle
                            detail.LeaveInfo = string.IsNullOrEmpty(detail.LeaveInfo)
                                ? leaveDescription
                                : $"{detail.LeaveInfo}, {leaveDescription}";

                            detail.LeaveHours += leaveHoursForDay;

                            // Eğer tam gün izinse devamsızlık olarak işaretle
                            if (leaveHoursForDay >= 8)
                            {
                                detail.IsPresent = false;
                                detail.StartTime = null;
                                detail.EndTime = null;
                                detail.HasMealAllowance = false;
                            }

                            timesheetUpdated = true;
                        }
                    }

                    if (timesheetUpdated)
                    {
                        // Puantaj onaylanmışsa "Revision" durumuna çevir
                        if (timesheet.Status == ApprovalStatus.Approved)
                        {
                            timesheet.Status = ApprovalStatus.Revision;
                            timesheet.ApprovalNote = $"İzin onayı nedeniyle güncellendi: {leaveDescription}";
                            timesheet.UpdatedDate = DateTime.Now;
                        }

                        // Puantaj toplamlarını yeniden hesapla
                        await UpdateTimesheetTotals(timesheet.Id);

                        _context.Update(timesheet);
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't break the approval process
                Console.WriteLine($"Error reflecting leave to timesheet: {ex.Message}");
            }
        }

        // ========================================================================
        // HELPER: İzin türü açıklaması
        // ========================================================================
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

        // ========================================================================
        // HELPER: O gün için izin saati hesapla
        // ========================================================================
        private decimal CalculateLeaveHoursForDay(LeaveRequest leaveRequest, DateTime workDate)
        {
            var leaveStart = leaveRequest.StartDateTime;
            var leaveEnd = leaveRequest.EndDateTime;
            var workDay = workDate.Date;

            // İzin bu günü kapsıyor mu?
            if (workDay < leaveStart.Date || workDay > leaveEnd.Date)
                return 0;

            // O gün için çalışma saatleri (08:30 - 17:30)
            var workDayStart = workDay.AddHours(8).AddMinutes(30);
            var workDayEnd = workDay.AddHours(17).AddMinutes(30);

            // İzin saatlerini o günün çalışma saatleri ile kesiştir
            var effectiveLeaveStart = leaveStart > workDayStart ? leaveStart : workDayStart;
            var effectiveLeaveEnd = leaveEnd < workDayEnd ? leaveEnd : workDayEnd;

            // Geçerli aralık var mı?
            if (effectiveLeaveStart >= effectiveLeaveEnd)
                return 0;

            var leaveHours = (decimal)(effectiveLeaveEnd - effectiveLeaveStart).TotalHours;

            // Maksimum 8 saat
            return Math.Min(Math.Round(leaveHours, 1), 8);
        }

        // ========================================================================
        // HELPER: Puantaj toplamlarını güncelle (TimesheetsController'dan kopyala)
        // ========================================================================
        private async Task UpdateTimesheetTotals(int timesheetId)
        {
            var timesheet = await _context.Timesheets
                .Include(t => t.TimesheetDetails)
                .FirstOrDefaultAsync(t => t.Id == timesheetId);

            if (timesheet != null)
            {
                timesheet.TotalWorkDays = timesheet.TimesheetDetails.Count(d => d.IsPresent);
                timesheet.TotalLeaveDays = timesheet.TimesheetDetails
                    .Where(d => d.LeaveHours > 0)
                    .Sum(d => (int)Math.Ceiling(d.LeaveHours / 8));
                timesheet.TotalTrainingHours = timesheet.TimesheetDetails.Sum(d => d.TrainingHours);
                timesheet.UpdatedDate = DateTime.Now;

                _context.Update(timesheet);
            }
        }

        // ========================================================================
        // POST: LeaveRequests/Reject/5 - İzin Talebini Reddet
        // ========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string rejectionReason)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Intern)
                .FirstOrDefaultAsync(lr => lr.Id == id);

            if (leaveRequest == null) return NotFound();

            if (leaveRequest.Status != ApprovalStatus.Pending)
            {
                TempData["ErrorMessage"] = "Sadece beklemede olan izin talepleri reddedilebilir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["ErrorMessage"] = "Red nedeni belirtilmelidir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUser = await _userManager.GetUserAsync(User);

            leaveRequest.Status = ApprovalStatus.Rejected;
            leaveRequest.ApproverId = currentUser?.Id;
            leaveRequest.ApproverName = currentUser?.FullName ?? currentUser?.UserName;
            leaveRequest.ApprovalDate = DateTime.Now;
            leaveRequest.ApprovalNote = rejectionReason;
            leaveRequest.UpdatedDate = DateTime.Now;

            _context.Update(leaveRequest);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{leaveRequest.Intern.FullName} adlı stajyerin izin talebi reddedildi.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ========================================================================
        // GET: LeaveRequests/Delete/5 - İzin Talebini Sil
        // ========================================================================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Intern)
                .Include(lr => lr.Approver)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (leaveRequest == null) return NotFound();

            return View(leaveRequest);
        }

        // ========================================================================
        // POST: LeaveRequests/Delete/5 - İzin Talebini Sil (Onay)
        // ========================================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var leaveRequest = await _context.LeaveRequests.FindAsync(id);
                if (leaveRequest != null)
                {
                    _context.LeaveRequests.Remove(leaveRequest);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "İzin talebi başarıyla silindi.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Silinecek izin talebi bulunamadı.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Silme işlemi sırasında hata oluştu: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // ========================================================================
        // Helper Methods
        // ========================================================================
        private bool LeaveRequestExists(int id)
        {
            return _context.LeaveRequests.Any(e => e.Id == id);
        }

        private async Task PopulateDropDowns(int? selectedInternId = null)
        {
            var interns = await _context.Interns
                .Where(i => i.IsActive)
                .OrderBy(i => i.FullName)
                .ToListAsync();

            ViewBag.InternId = new SelectList(interns, "Id", "FullName", selectedInternId);

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
    }
}