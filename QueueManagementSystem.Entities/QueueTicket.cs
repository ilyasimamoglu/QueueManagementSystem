using System;
using System.Collections.Generic;
using System.Text;

namespace QueueManagementSystem.Entities
{
    public class QueueTicket
    {
        //Bilet tablosunun Kolonları
        public int Id { get; set; }                     // Veritabanındaki benzersiz numarası (Otomatik artacak)

        public int TicketNumber { get; set; }          // Kioskta ve Arduino ekranda yazacak olan numara (Örn: "A-101")
       
        public int Status { get; set; }               // Biletin durumu: 0 = Bekliyor, 1 = Çağrıldı, 2 = Bitti
        
        public int? CounterId { get; set; }          // İşlemi yapan gişenin numarası (Bilet ilk alındığında kimin çağıracağı belli olmadığı için sonuna '?' koyduk, yani boş kalabilir)

        public DateTime CreatedAt { get; set; }     // Biletin kiosktan alındığı ilk an

        public DateTime? CompletedAt { get; set; } // İşlemin bittiği an (Yine ilk başta boş kalacağı için '?')


    }
}
