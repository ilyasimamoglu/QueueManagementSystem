using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueManagementSystem.DataAccess;
using System;
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
            string busiestDay = "Pazartesi";
            string avgWaitTime = "03:45";
            string busiestHour = "13:00";

            switch (period.ToLower())
            {
                case "günlük":
                    busiestDay = DateTime.Now.ToString("dddd");
                    break;
                case "haftalık":
                    busiestDay = "Pazartesi";
                    break;
                case "aylık":
                    busiestDay = "Cuma";
                    break;
                case "yıllık":
                    busiestDay = "Pazartesi";
                    break;
            }

            var reportData = new
            {
                BusiestDay = busiestDay,
                AverageWaitTime = avgWaitTime,
                BusiestHour = busiestHour
            };

            return Ok(reportData);
        }

        [HttpGet("performance")]
        public async Task<IActionResult> GetStaffPerformance()
        {
            try
            {
                // 1. AŞAMA: Entity Framework üzerinden veriyi int (sayısal) olarak güvenle çekiyoruz
                var rawData = await _context.Users
                    .Where(u => u.Role != null && u.Role.Contains("Gişe"))
                    .Select(u => new
                    {
                        Name = u.FullName ?? u.Username,
                        ServedCount = _context.QueueTickets.Count(t => t.CounterId == u.Id),
                        u.IsActive
                    })
                    .ToListAsync();

                // 2. AŞAMA: WPF tarafı 'Served' alanını string beklediği için bellekte ToString() ile dönüştürüyoruz
                var staffPerformance = rawData.Select(u => new
                {
                    Name = u.Name,
                    Served = u.ServedCount.ToString(), // Sorunu çözen kritik dönüştürme noktası
                    AvgTime = "03:15",
                    BreakTime = "15 dk",
                    Status = u.IsActive ? "🟢 Aktif" : "🔴 İzinli"
                }).ToList();

                return Ok(staffPerformance);
            }
            catch (Exception ex)
            {
                return BadRequest("Performans verileri alınamadı: " + ex.Message);
            }
        }
    }
}