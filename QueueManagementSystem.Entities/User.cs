using System;

namespace QueueManagementSystem.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; }     // Ad Soyad
        public string Username { get; set; }     // Sisteme giriş adı
        public string Password { get; set; }     // Şifre (Şimdilik düz metin tutuluyor)
        public string Role { get; set; }         // Örn: "Admin" veya "GiseGorevlisi"

        // YENİ EKLENEN SÜTUNLAR
        public string AssignedCounter { get; set; }      // Kullanıcının atandığı gişe (Örn: "Gişe 1", "-")
        public double TotalBreakTimeInMinutes { get; set; } 

        public bool IsActive { get; set; }       // Kullanıcı aktif mi?
        public DateTime CreatedAt { get; set; }  // Eklenme tarihi
    }
}