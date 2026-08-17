using System;
using System.Collections.Generic;
using System.Text;

namespace QueueManagementSystem.Entities
{
    public class QueueTicket
    {
        public int Id { get; set; }
        public int TicketNumber { get; set; }
        public int Status { get; set; }
        public int? CounterId { get; set; }

        // YENİ EKLENDİ: Bu bilet Öğrenci İşleri mi yoksa E-Kampüs mü?
        public string ServiceType { get; set; }

        public DateTime CreatedAt { get; set; }

        // YENİ: Gişeye çağrıldığı an (Müşterinin gişe önüne geldiği an)
        public DateTime? CalledAt { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}