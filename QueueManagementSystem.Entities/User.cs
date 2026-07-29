using System;

namespace QueueManagementSystem.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; }     // Ad Soyad
        public string Username { get; set; }     // Sisteme giriş adı
        public string Password { get; set; }     // Şifre (Şimdilik düz metin tutabiliriz)
        public string Role { get; set; }         // Örn: "Admin" veya "GiseGorevlisi"
        public bool IsActive { get; set; }       // Kullanıcı aktif mi?
        public DateTime CreatedAt { get; set; }  // Eklenme tarihi
    }
}