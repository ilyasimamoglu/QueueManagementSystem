using System;
using System.Collections.Generic;
using System.Text;

namespace QueueManagementSystem.Entities
{
    public class Counter
    {
        public int Id { get; set; } // Gişenin veritabanındaki benzersiz numarası
        public string Name { get; set; } // Gişenin ekranda veya Arduino'da görünecek adı (Örn: "Gişe 1", "Vezne A")
        public bool IsActive { get; set; } // Gişe o an açık mı, memur çalışıyor mu? (true = açık, false = kapalı)  Böylece sadece aktif gişelere müşteri yönlendirebiliriz.


    }
}
