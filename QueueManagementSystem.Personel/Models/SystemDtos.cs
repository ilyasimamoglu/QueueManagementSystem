using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueueManagementSystem.Counter.Models
{
    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }

        // YENİ EKLENEN ÖZELLİKLER
        public string AssignedCounter { get; set; }
        public double TotalBreakTimeInMinutes { get; set; }

        public string Icon => !string.IsNullOrEmpty(Role) && Role.Contains("Admin") ? "👑" : "👤";
        public string RoleColor => !string.IsNullOrEmpty(Role) && Role.Contains("Admin") ? "#F59E0B" : "#10B981";
    }

    public class ReportSummaryDto
    {
        public string BusiestDay { get; set; }
        public string AverageWaitTime { get; set; }
        public string BusiestHour { get; set; }
    }

    public class QueueTicketDto { public int Id { get; set; } }

    public class CounterDto { public int Id { get; set; } }

    public class StaffPerformanceDto
    {
        public string Name { get; set; }
        public string Served { get; set; }
        public string AvgTime { get; set; }
        public string BreakTime { get; set; }
        public string Status { get; set; }
    }
}