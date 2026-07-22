using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QueueManagementSystem.DataAccess;
using QueueManagementSystem.Entities;


namespace QueueManagementSystem.Business
{
    // Aşçımız, hazırladığımız IQueueTicketService menüsündeki sözleşmelere uymak zorunda.
    public class QueueTicketManager : IQueueTicketService  
    {
        // Aşçının (Business) malzemeleri alıp koyabilmesi için Kiler Görevlisine (DataAccess) ihtiyacı var.
        private readonly IQueueTicketDal _queueTicketDal;

        // Dependency Injection: "Bana kiler görevlisini ver ki işimi yapabileyim" diyoruz.
        public QueueTicketManager(IQueueTicketDal queueTicketDal)
        {
            _queueTicketDal = queueTicketDal;
        }

        public QueueTicket TakeTicket()
        {
            // 1. Veritabanındaki tüm biletleri getir (Şimdilik basit mantıkla yapıyoruz)
            var allTickets = _queueTicketDal.GetAll();

            // 2. Mevcut en büyük bilet numarasını bul. Eğer hiç bilet yoksa 0 kabul et.
            int lastTicketNumber = allTickets.Any() ? allTickets.Max(t => t.TicketNumber) : 0;

            // 3. Yeni bilet objesini oluştur (Senin Entity'ndeki kurallara göre)
            QueueTicket newTicket = new QueueTicket
            {
                TicketNumber = lastTicketNumber + 1, // Sıradaki numara
                Status = 0,                          // 0 = Bekliyor
                CreatedAt = DateTime.Now             // Alındığı anın saati
                // CounterId ve CompletedAt boş (null) bırakılıyor çünkü henüz işlem görmedi.
            };

            // 4. Kiler görevlisine (DataAccess) "Bunu veritabanına kaydet" komutunu ver
            _queueTicketDal.Add(newTicket);

            // 5. Oluşturulan bileti garsona (Controller/API) geri gönder
            return newTicket;
        }

        public QueueTicket CallNextTicket(int counterId)
        {
            // 1. Durumu 0 (Bekliyor) olan tüm biletleri bul ve tarihe göre en eskisini (ilk geleni) seç.
            var nextTicket = _queueTicketDal.GetAll()
                                            .Where(t => t.Status == 0)
                                            .OrderBy(t => t.CreatedAt)
                                            .FirstOrDefault();

            // 2. Eğer bekleyen bilet varsa güncellemeleri yap
            if (nextTicket != null)
            {
                nextTicket.Status = 1;               // 1 = Çağrıldı
                nextTicket.CounterId = counterId;    // Hangi gişe çağırdıysa onu kaydet

                // Kiler görevlisine "Bu biletin bilgilerini güncelle" de
                _queueTicketDal.Update(nextTicket);
            }

            // 3. Bileti geri gönder (Eğer bilet yoksa null dönecektir, API tarafında "Bekleyen bilet yok" uyarısı veririz)
            return nextTicket;
        }
        public void CompleteTicket(int ticketId)
        {
            // GetAll() ile biletleri getirip LINQ (FirstOrDefault) yardımıyla istediğimiz ID'yi buluyoruz
            var ticket = _queueTicketDal.GetAll().FirstOrDefault(t => t.Id == ticketId);

            if (ticket != null)
            {
                // 2 = Tamamlandı olarak kabul ediliyor
                ticket.Status = 2;

                _queueTicketDal.Update(ticket);
            }
        }
        public List<QueueTicket> GetWaitingTickets()
        {
            // Status değeri 0 (Bekleyen) olanları filtreleyip liste olarak döndürüyoruz
            return _queueTicketDal.GetAll().Where(t => t.Status == 0).ToList();
        }

        public List<QueueTicket> GetCalledTickets()
        {
            // Status değeri 1 (Gişeye Çağrılmış / İşlemde) olanları filtreleyip liste olarak döndürüyoruz
            return _queueTicketDal.GetAll().Where(t => t.Status == 1).ToList();
        }
    }
}