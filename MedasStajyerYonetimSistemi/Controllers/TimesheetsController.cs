// ============================================================================
// Controllers/TimesheetsController.cs - Puantaj Yönetim Controller'ı
// ============================================================================

using ClosedXML.Excel;
using MedasStajyerYonetimSistemi.Data;
using MedasStajyerYonetimSistemi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace MedasStajyerYonetimSistemi.Controllers
{
    public class TimesheetsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TimesheetsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ========================================================================
        // GET: Timesheets - Puantaj Listesi
        // ========================================================================
        public async Task<IActionResult> Index(string status = "All", int page = 1)
        {
            var query = _context.Timesheets
                .Include(t => t.Intern)
                .Include(t => t.Approver)
                .AsQueryable();

            // Status filtresi
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
            ViewBag.TotalCount = totalCount;

            // Status options for filter dropdown
            ViewBag.StatusOptions = new List<SelectListItem>
            {
                new() { Value = "All", Text = "Tümü" },
                new() { Value = "Pending", Text = "Beklemede" },
                new() { Value = "Approved", Text = "Onaylandı" },
                new() { Value = "Rejected", Text = "Reddedildi" },
                new() { Value = "Revision", Text = "Revize" }
            };

            return View(timesheets);
        }

        // ========================================================================
        // GET: Timesheets/Details/5 - Puantaj Detayları
        // ========================================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var timesheet = await _context.Timesheets
                .Include(t => t.Intern)
                .Include(t => t.Approver)
                .Include(t => t.TimesheetDetails.OrderBy(td => td.WorkDate))
                .Include(t => t.ApprovalHistories)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (timesheet == null) return NotFound();

            return View(timesheet);
        }

        // ========================================================================
        // GET: Timesheets/Create - Yeni Puantaj Oluştur
        // ========================================================================
        public async Task<IActionResult> Create()
        {
            await PopulateDropDowns();

            var model = new Timesheet
            {
                PeriodDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                CreatedDate = DateTime.Now
            };

            return View(model);
        }

        // ========================================================================
        // POST: Timesheets/Create - Yeni Puantaj Kaydet
        // ========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Timesheet timesheet)
        {
            // Navigation property hatalarını temizle
            ModelState.Remove("Intern");
            ModelState.Remove("Approver");
            ModelState.Remove("TimesheetDetails");
            ModelState.Remove("ApprovalHistories");

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

                // Manuel entry kontrolü
                if (timesheet.IsManualEntry)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    timesheet.ManualEntryBy = currentUser?.UserName;
                }

                timesheet.CreatedDate = DateTime.Now;
                timesheet.Status = ApprovalStatus.Pending;

                // Dönem için günlük detayları oluştur
                await CreateMonthlyTimesheetDetails(timesheet);

                _context.Add(timesheet);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Puantaj başarıyla oluşturuldu. Günlük detayları girebilirsiniz.";
                return RedirectToAction(nameof(Edit), new { id = timesheet.Id });
            }

            await PopulateDropDowns(timesheet.InternId);
            return View(timesheet);
        }

        // ========================================================================
        // GET: Timesheets/Edit/5 - Puantaj Düzenle
        // ========================================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var timesheet = await _context.Timesheets
                .Include(t => t.Intern)
                .Include(t => t.TimesheetDetails.OrderBy(td => td.WorkDate))
                .FirstOrDefaultAsync(t => t.Id == id);

            if (timesheet == null) return NotFound();

            if (timesheet.Status == ApprovalStatus.Approved)
            {
                TempData["ErrorMessage"] = "Onaylanmış puantajlar düzenlenemez.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await PopulateDropDowns(timesheet.InternId);
            return View(timesheet);
        }

        // ========================================================================
        // POST: Timesheets/UpdateDetail - Günlük Detay Güncelle
        // ========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDetail(int detailId, bool isPresent,
            string startTime, string endTime, string leaveInfo, decimal leaveHours,
            string trainingInfo, decimal trainingHours, bool hasMealAllowance,
            string notes, WorkLocation workLocation)
        {

            Console.WriteLine("=== UPDATE DETAIL ACTION CALLED ===");
            Console.WriteLine($"DetailId: {detailId}");
            Console.WriteLine($"TrainingInfo: {trainingInfo}");
            Console.WriteLine($"TrainingHours: {trainingHours}");

            var detail = await _context.TimesheetDetails
                .Include(td => td.Timesheet)
                .FirstOrDefaultAsync(td => td.Id == detailId);

            if (detail == null) return NotFound();

            if (detail.Timesheet.Status == ApprovalStatus.Approved)
            {
                TempData["ErrorMessage"] = "Onaylanmış puantaj güncellenemez.";
                return RedirectToAction(nameof(Edit), new { id = detail.TimesheetId });
            }

            detail.IsPresent = isPresent;
            detail.WorkLocation = workLocation;
            detail.LeaveInfo = leaveInfo ?? "";
            detail.LeaveHours = leaveHours;
            detail.TrainingInfo = trainingInfo ?? "";
            detail.TrainingHours = trainingHours;
            detail.HasMealAllowance = hasMealAllowance;
            detail.Notes = notes ?? "";

            if (isPresent && !string.IsNullOrEmpty(startTime) && !string.IsNullOrEmpty(endTime))
            {
                if (TimeSpan.TryParse(startTime, out var start) && TimeSpan.TryParse(endTime, out var end))
                {
                    detail.StartTime = start;
                    detail.EndTime = end;
                }
            }
            else
            {
                detail.StartTime = null;
                detail.EndTime = null;
            }

            // Puantaj toplamlarını güncelle
            await UpdateTimesheetTotals(detail.TimesheetId);

            _context.Update(detail);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{detail.WorkDate:dd.MM.yyyy} günü güncellendi.";
            return RedirectToAction(nameof(Edit), new { id = detail.TimesheetId });
        }

        // ========================================================================
        // POST: Timesheets/Approve/5 - Puantaj Onayla
        // ========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? approvalNote = null)
        {
            var timesheet = await _context.Timesheets
                .Include(t => t.Intern)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (timesheet == null) return NotFound();

            if (timesheet.Status != ApprovalStatus.Pending && timesheet.Status != ApprovalStatus.Revision)
            {
                TempData["ErrorMessage"] = "Sadece beklemede veya revize durumundaki puantajlar onaylanabilir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUser = await _userManager.GetUserAsync(User);

            timesheet.Status = ApprovalStatus.Approved;
            timesheet.ApproverId = currentUser?.Id;
            timesheet.ApproverName = currentUser?.FullName ?? currentUser?.UserName;
            timesheet.ApprovalDate = DateTime.Now;
            timesheet.ApprovalNote = approvalNote;
            timesheet.UpdatedDate = DateTime.Now;

            _context.Update(timesheet);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{timesheet.Intern.FullName} adlı stajyerin puantajı onaylandı.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ========================================================================
        // POST: Timesheets/Reject/5 - Puantaj Reddet
        // ========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string rejectionReason)
        {
            var timesheet = await _context.Timesheets
                .Include(t => t.Intern)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (timesheet == null) return NotFound();

            if (timesheet.Status != ApprovalStatus.Pending && timesheet.Status != ApprovalStatus.Revision)
            {
                TempData["ErrorMessage"] = "Sadece beklemede veya revize durumundaki puantajlar reddedilebilir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["ErrorMessage"] = "Red nedeni belirtilmelidir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUser = await _userManager.GetUserAsync(User);

            timesheet.Status = ApprovalStatus.Rejected;
            timesheet.ApproverId = currentUser?.Id;
            timesheet.ApproverName = currentUser?.FullName ?? currentUser?.UserName;
            timesheet.ApprovalDate = DateTime.Now;
            timesheet.ApprovalNote = rejectionReason;
            timesheet.UpdatedDate = DateTime.Now;

            _context.Update(timesheet);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{timesheet.Intern.FullName} adlı stajyerin puantajı reddedildi.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ========================================================================
        // Helper Methods
        // ========================================================================
        private async Task CreateMonthlyTimesheetDetails(Timesheet timesheet)
        {
            var startDate = new DateTime(timesheet.PeriodDate.Year, timesheet.PeriodDate.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Bu dönemde onaylı izinleri getir
            var approvedLeaves = await _context.LeaveRequests
                .Where(lr => lr.InternId == timesheet.InternId)
                .Where(lr => lr.Status == ApprovalStatus.Approved)
                .Where(lr => lr.ShouldReflectToTimesheet)
                .Where(lr => lr.StartDateTime.Date <= endDate && lr.EndDateTime.Date >= startDate)
                .ToListAsync();

            var details = new List<TimesheetDetail>();
            var turkishCulture = new CultureInfo("tr-TR");

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // Bu günde izin var mı kontrol et
                var dayLeaves = approvedLeaves
                    .Where(lr => lr.StartDateTime.Date <= date && lr.EndDateTime.Date >= date)
                    .ToList();

                var detail = new TimesheetDetail
                {
                    TimesheetId = timesheet.Id,
                    WorkDate = date,
                    DayName = turkishCulture.DateTimeFormat.GetDayName(date.DayOfWeek),
                    DayNumber = date.Day,
                    WorkLocation = WorkLocation.HeadOffice,
                    IsPresent = date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday,
                    StartTime = date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday ? new TimeSpan(8, 30, 0) : null,
                    EndTime = date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday ? new TimeSpan(17, 30, 0) : null,
                    HasMealAllowance = date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday
                };

                // İzinleri uygula
                if (dayLeaves.Any())
                {
                    var totalLeaveHours = 0m;
                    var leaveInfos = new List<string>();

                    foreach (var leave in dayLeaves)
                    {
                        var leaveHoursForDay = CalculateLeaveHoursForDay(leave, date);
                        totalLeaveHours += leaveHoursForDay;

                        var leaveDescription = GetLeaveTypeDescription(leave.LeaveType);
                        leaveInfos.Add($"{leaveDescription} ({leaveHoursForDay}h)");
                    }

                    detail.LeaveHours = totalLeaveHours;
                    detail.LeaveInfo = string.Join(", ", leaveInfos);

                    // Tam gün izin ise devamsız olarak işaretle
                    if (totalLeaveHours >= 8)
                    {
                        detail.IsPresent = false;
                        detail.StartTime = null;
                        detail.EndTime = null;
                        detail.HasMealAllowance = false;
                    }
                }

                details.Add(detail);
            }

            timesheet.TimesheetDetails = details;
        }

        // TimesheetsController.cs'e ekleyin

        // ========================================================================
        // GET: Timesheets/ExportToExcel - Puantaj Excel Export
        // ========================================================================
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

            // ========================================================================
            // Worksheet 1: Puantaj Özeti
            // ========================================================================
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
                summaryWorksheet.Cell(summaryRow, 6).Value = timesheet.PeriodDate.ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
                summaryWorksheet.Cell(summaryRow, 7).Value = timesheet.TotalWorkDays;
                summaryWorksheet.Cell(summaryRow, 8).Value = timesheet.TotalLeaveDays;
                summaryWorksheet.Cell(summaryRow, 9).Value = mealAllowanceDays;
                summaryWorksheet.Cell(summaryRow, 10).Value = timesheet.TotalTrainingHours.ToString("F1");
                summaryWorksheet.Cell(summaryRow, 11).Value = GetApprovalStatusDisplayName(timesheet.Status);
                summaryWorksheet.Cell(summaryRow, 12).Value = timesheet.ApproverName ?? "";

                // Durum renklandırması
                var statusCell = summaryWorksheet.Cell(summaryRow, 11);
                switch (timesheet.Status)
                {
                    case ApprovalStatus.Approved:
                        statusCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        break;
                    case ApprovalStatus.Rejected:
                        statusCell.Style.Fill.BackgroundColor = XLColor.LightPink;
                        break;
                    case ApprovalStatus.Pending:
                        statusCell.Style.Fill.BackgroundColor = XLColor.LightYellow;
                        break;
                    case ApprovalStatus.Revision:
                        statusCell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                        break;
                }

                summaryRow++;
            }

            // Summary styling
            summaryWorksheet.Columns().AdjustToContents();
            var summaryDataRange = summaryWorksheet.Range(1, 1, summaryRow - 1, 12);
            summaryDataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            summaryDataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // ========================================================================
            // Worksheet 2: Detaylı Puantaj (Günlük)
            // ========================================================================
            var detailWorksheet = workbook.Worksheets.Add("Günlük Detaylar");

            // Detail Headers
            detailWorksheet.Cell(1, 1).Value = "Stajyer Adı";
            detailWorksheet.Cell(1, 2).Value = "Dönem";
            detailWorksheet.Cell(1, 3).Value = "Tarih";
            detailWorksheet.Cell(1, 4).Value = "Gün";
            detailWorksheet.Cell(1, 5).Value = "Devam Durumu";
            detailWorksheet.Cell(1, 6).Value = "Görev Yeri";
            detailWorksheet.Cell(1, 7).Value = "Başlangıç Saati";
            detailWorksheet.Cell(1, 8).Value = "Bitiş Saati";
            detailWorksheet.Cell(1, 9).Value = "İzin Bilgisi";
            detailWorksheet.Cell(1, 10).Value = "İzin Saati";
            detailWorksheet.Cell(1, 11).Value = "Eğitim Bilgisi";
            detailWorksheet.Cell(1, 12).Value = "Eğitim Saati";
            detailWorksheet.Cell(1, 13).Value = "Yemek Yardımı";
            detailWorksheet.Cell(1, 14).Value = "Açıklama";

            // Detail header styling
            var detailHeaderRange = detailWorksheet.Range(1, 1, 1, 14);
            detailHeaderRange.Style.Font.Bold = true;
            detailHeaderRange.Style.Fill.BackgroundColor = XLColor.DarkGreen;
            detailHeaderRange.Style.Font.FontColor = XLColor.White;
            detailHeaderRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;

            // Detail data
            int detailRow = 2;
            foreach (var timesheet in timesheets)
            {
                foreach (var detail in timesheet.TimesheetDetails)
                {
                    detailWorksheet.Cell(detailRow, 1).Value = timesheet.Intern.FullName;
                    detailWorksheet.Cell(detailRow, 2).Value = timesheet.PeriodDate.ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
                    detailWorksheet.Cell(detailRow, 3).Value = detail.WorkDate.ToString("dd.MM.yyyy");
                    detailWorksheet.Cell(detailRow, 4).Value = detail.DayName;
                    detailWorksheet.Cell(detailRow, 5).Value = detail.IsPresent ? "Var" : "Yok";
                    detailWorksheet.Cell(detailRow, 6).Value = detail.WorkLocation == WorkLocation.HeadOffice ? "Genel Müdürlük" : "İşletme";
                    detailWorksheet.Cell(detailRow, 7).Value = detail.StartTime?.ToString(@"hh\:mm") ?? "";
                    detailWorksheet.Cell(detailRow, 8).Value = detail.EndTime?.ToString(@"hh\:mm") ?? "";
                    detailWorksheet.Cell(detailRow, 9).Value = detail.LeaveInfo;
                    detailWorksheet.Cell(detailRow, 10).Value = detail.LeaveHours > 0 ? detail.LeaveHours.ToString("F1") : "";
                    detailWorksheet.Cell(detailRow, 11).Value = detail.TrainingInfo;
                    detailWorksheet.Cell(detailRow, 12).Value = detail.TrainingHours > 0 ? detail.TrainingHours.ToString("F1") : "";
                    detailWorksheet.Cell(detailRow, 13).Value = detail.HasMealAllowance ? "Evet" : "Hayır";
                    detailWorksheet.Cell(detailRow, 14).Value = detail.Notes;

                    // Hafta sonu renklandırması
                    if (detail.WorkDate.DayOfWeek == DayOfWeek.Saturday || detail.WorkDate.DayOfWeek == DayOfWeek.Sunday)
                    {
                        var weekendRange = detailWorksheet.Range(detailRow, 1, detailRow, 14);
                        weekendRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                    }

                    detailRow++;
                }
            }

            // Detail styling
            detailWorksheet.Columns().AdjustToContents();
            var detailDataRange = detailWorksheet.Range(1, 1, detailRow - 1, 14);
            detailDataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            detailDataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Excel dosyasını oluştur
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Puantaj_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private string GetApprovalStatusDisplayName(ApprovalStatus status)
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

        // Helper method'ları da ekleyin (aynı LeaveRequestsController'da olduğu gibi)
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

        private async Task UpdateTimesheetTotals(int timesheetId)
        {
            var timesheet = await _context.Timesheets
                .Include(t => t.TimesheetDetails)
                .FirstOrDefaultAsync(t => t.Id == timesheetId);

            if (timesheet != null)
            {
                timesheet.TotalWorkDays = timesheet.TimesheetDetails.Count(d => d.IsPresent);
                timesheet.TotalLeaveDays = timesheet.TimesheetDetails.Where(d => d.LeaveHours > 0).Sum(d => (int)Math.Ceiling(d.LeaveHours / 8));
                timesheet.TotalTrainingHours = timesheet.TimesheetDetails.Sum(d => d.TrainingHours);
                timesheet.UpdatedDate = DateTime.Now;
            }
        }

        private async Task PopulateDropDowns(int? selectedInternId = null)
        {
            var interns = await _context.Interns
                .Where(i => i.IsActive)
                .OrderBy(i => i.FullName)
                .ToListAsync();

            ViewBag.InternId = new SelectList(interns, "Id", "FullName", selectedInternId);

            ViewBag.WorkLocations = new SelectList(
                Enum.GetValues(typeof(WorkLocation))
                    .Cast<WorkLocation>()
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