using System.Collections.Generic;

namespace QueueManagementSystem.Counter.Models
{
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

        // WPF'de barın yüksekliğini/genişliğini otomatik çizmek için (Her bilet 2.5 birim yükseklik katsın)
        public double Height => Count * 2.5 > 0 ? Count * 2.5 : 2;
        public double Width => Count * 10 > 0 ? Count * 10 : 2;

        public string CountText => $"{Count} İşlem";
        public string Color { get; set; }
    }

    public class CounterStatusDto
    {
        public string CounterName { get; set; }
        public string OperatorName { get; set; }
        public int ServedCount { get; set; }
        public string ServedToday => $"{ServedCount} Müşteri";
        public bool IsActive { get; set; }

        public string Status => IsActive ? "🟢 Aktif" : "🔴 Pasif";
        public string StatusColor => IsActive ? "#10B981" : "#EF4444";
    }
}