using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueManagementSystem.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QueueManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly QueueDbContext _context;

        public ReportsController(QueueDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetReportSummary([FromQuery] string period = "Günlük")
        {
            try
            {
                var now = DateTime.Now;
                DateTime startDate = now.Date;

                if (period.Equals("Haftalık", StringComparison.OrdinalIgnoreCase))
                    startDate = now.Date.AddDays(-7);
                else if (period.Equals("Aylık", StringComparison.OrdinalIgnoreCase))
                    startDate = now.Date.AddMonths(-1);
                else if (period.Equals("Yıllık", StringComparison.OrdinalIgnoreCase))
                    startDate = now.Date.AddYears(-1);

                var tickets = await _context.QueueTickets
                    .Where(t => t.CreatedAt >= startDate)
                    .ToListAsync();

                // 1. En Yoğun Gün
                string busiestDay = "-";
                if (tickets.Any())
                {
                    busiestDay = tickets.GroupBy(t => t.CreatedAt.Date)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key.ToString("dddd") ?? "-";
                }

                // 2. GERÇEK Ortalama Bekleme Süresi (Kiosktan alınma saati ile Gişeye çağrılma saati arası)
                string avgWaitTime = "00:00";
                var calledTickets = tickets.Where(t => t.CalledAt.HasValue).ToList();
                if (calledTickets.Any())
                {
                    double avgSeconds = calledTickets.Average(t => (t.CalledAt.Value - t.CreatedAt).TotalSeconds);
                    TimeSpan avgTimeSpan = TimeSpan.FromSeconds(Math.Max(0, avgSeconds));
                    avgWaitTime = $"{avgTimeSpan.Minutes:00}:{avgTimeSpan.Seconds:00}";
                }

                // 3. En Yoğun Saat
                string busiestHour = "-";
                if (tickets.Any())
                {
                    var hour = tickets.GroupBy(t => t.CreatedAt.Hour)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key ?? 0;
                    busiestHour = $"{hour:00}:00";
                }

                return Ok(new { BusiestDay = busiestDay, AverageWaitTime = avgWaitTime, BusiestHour = busiestHour });
            }
            catch (Exception ex)
            {
                return BadRequest("Özet veriler alınamadı: " + ex.Message);
            }
        }

        [HttpGet("performance")]
        public async Task<IActionResult> GetStaffPerformance([FromQuery] string period = "Günlük")
        {
            try
            {
                DateTime startDate = DateTime.Today;

                if (period.Equals("Haftalık", StringComparison.OrdinalIgnoreCase))
                    startDate = DateTime.Today.AddDays(-7);
                else if (period.Equals("Aylık", StringComparison.OrdinalIgnoreCase))
                    startDate = DateTime.Today.AddMonths(-1);
                else if (period.Equals("Yıllık", StringComparison.OrdinalIgnoreCase))
                    startDate = DateTime.Today.AddYears(-1);

                var users = await _context.Users
                    .Where(u => u.Role != null && u.Role.Contains("Gişe"))
                    .ToListAsync();

                var tickets = await _context.QueueTickets
                    .Where(t => t.Status == 2)
                    .ToListAsync();

                var staffPerformance = users.Select(u =>
                {
                    string counterStr = new String(u.AssignedCounter?.Where(Char.IsDigit).ToArray() ?? Array.Empty<char>());
                    int currentCounterNo = int.TryParse(counterStr, out int val) ? val : -1;

                    var userTickets = tickets.Where(t => t.CounterId == currentCounterNo || t.CounterId == u.Id).ToList();

                    var periodTickets = userTickets.Where(t =>
                        (t.CompletedAt.HasValue && t.CompletedAt.Value >= startDate) ||
                        (!t.CompletedAt.HasValue && t.CreatedAt >= startDate)
                    ).ToList();

                    double avgSeconds = 0;
                    var validProcessedTickets = periodTickets.Where(t => t.CalledAt.HasValue && t.CompletedAt.HasValue).ToList();

                    if (validProcessedTickets.Any())
                    {
                        avgSeconds = validProcessedTickets.Average(t => (t.CompletedAt.Value - t.CalledAt.Value).TotalSeconds);
                    }

                    TimeSpan avgTimeSpan = TimeSpan.FromSeconds(Math.Max(0, avgSeconds));

                    return new
                    {
                        Name = u.FullName ?? u.Username,
                        Served = periodTickets.Count.ToString(),
                        AvgTime = $"{avgTimeSpan.Minutes:00}:{avgTimeSpan.Seconds:00}",
                        BreakTime = $"{Math.Round(u.TotalBreakTimeInMinutes, 2)} dk",
                        Status = u.IsActive ? "🟢 Aktif" : "🔴 İzinli"
                    };
                }).ToList();

                return Ok(staffPerformance);
            }
            catch (Exception ex)
            {
                return BadRequest("Performans verileri alınamadı: " + ex.Message);
            }
        }

        [HttpGet("dashboard-real")]
        public async Task<IActionResult> GetRealDashboardData()
        {
            try
            {
                var result = new RealDashboardDto();
                var now = DateTime.Now;

                var allTickets = await _context.QueueTickets.ToListAsync();

                // 1. HAFTALIK GRAFİK
                string[] colors = { "#6B468D", "#3B82F6", "#06B6D4", "#10B981", "#F59E0B", "#334155", "#334155" };
                var last7Days = Enumerable.Range(0, 7).Select(i => now.Date.AddDays(-6 + i)).ToList();

                for (int i = 0; i < last7Days.Count; i++)
                {
                    var day = last7Days[i];
                    int count = allTickets.Count(t => t.CreatedAt.Date == day);
                    string dayName = day.ToString("ddd").Substring(0, 1).ToUpper() + day.ToString("ddd").Substring(1, 2);
                    result.WeeklyChart.Add(new ChartBarDto { Label = dayName, Count = count, Color = colors[i] });
                }

                // 2. SAATLİK GRAFİK
                var todayTickets = allTickets.Where(t => t.CreatedAt.Date == now.Date).ToList();
                string[] hourColors = { "#3B82F6", "#3B82F6", "#10B981", "#10B981", "#F59E0B", "#3B82F6", "#3B82F6", "#334155", "#334155", "#334155" };

                for (int i = 8; i <= 17; i++)
                {
                    int count = todayTickets.Count(t => t.CreatedAt.Hour == i);
                    result.HourlyChart.Add(new ChartBarDto { Label = $"{i:00}:00", Count = count, Color = hourColors[i - 8] });
                }

                // 3. GİŞE DURUMLARI
                var users = await _context.Users.Where(u => u.Role != null && u.Role.Contains("Gişe")).ToListAsync();

                for (int i = 1; i <= 5; i++)
                {
                    string currentCounterName = $"Gişe {i}";
                    var counterUser = users.FirstOrDefault(u => u.AssignedCounter == currentCounterName);

                    if (counterUser != null && counterUser.IsActive)
                    {
                        int servedToday = todayTickets.Count(t => (t.CounterId == i || t.CounterId == counterUser.Id) && t.Status == 2);
                        result.CounterStatuses.Add(new CounterStatusDto
                        {
                            CounterName = $"🖥️ {currentCounterName}",
                            OperatorName = $"Opr: {counterUser.FullName ?? counterUser.Username}",
                            ServedCount = servedToday,
                            IsActive = true
                        });
                    }
                    else
                    {
                        result.CounterStatuses.Add(new CounterStatusDto
                        {
                            CounterName = $"🖥️ {currentCounterName}",
                            OperatorName = "Opr: - Yok -",
                            ServedCount = 0,
                            IsActive = false
                        });
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public class RealDashboardDto
    {
        public List<ChartBarDto> WeeklyChart { get; set; } = new List<ChartBarDto>();
        public List<ChartBarDto> HourlyChart { get; set; } = new List<ChartBarDto>();
        public List<CounterStatusDto> CounterStatuses { get; set; } = new List<CounterStatusDto>();
    }

    public class ChartBarDto
    {
        public string Label { get; set; }
        public int Count { get; set; }
        public string Color { get; set; }
    }

    public class CounterStatusDto
    {
        public string CounterName { get; set; }
        public string OperatorName { get; set; }
        public int ServedCount { get; set; }
        public bool IsActive { get; set; }
    }
}