using System;
using System.Collections.Generic;
using System.Linq;
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

        // --- 1. MEVCUT BİLET ALMA ---
        public QueueTicket TakeTicket()
        {
            var allTickets = _queueTicketDal.GetAll();
            int lastTicketNumber = allTickets.Any() ? allTickets.Max(t => t.TicketNumber) : 0;

            QueueTicket newTicket = new QueueTicket
            {
                TicketNumber = lastTicketNumber + 1,
                Status = 0,                          // 0 = Bekliyor
                CreatedAt = DateTime.Now
            };

            _queueTicketDal.Add(newTicket);
            return newTicket;
        }

        // --- 2. KIOSK İÇİN DETAYLI BİLET ALMA (YENİ) ---
        public QueueTicket TakeTicket(string serviceType, string priority)
        {
            var allTickets = _queueTicketDal.GetAll();
            int lastTicketNumber = allTickets.Any() ? allTickets.Max(t => t.TicketNumber) : 0;

            QueueTicket newTicket = new QueueTicket
            {
                TicketNumber = lastTicketNumber + 1,
                Status = 0,                          // 0 = Bekliyor
                CreatedAt = DateTime.Now
            };

            _queueTicketDal.Add(newTicket);
            return newTicket;
        }

        // --- 3. SIRADAKİ BİLETİ ÇAĞIR ---
        public QueueTicket CallNextTicket(int counterId)
        {
            var nextTicket = _queueTicketDal.GetAll()
                                            .Where(t => t.Status == 0) // 0 = Bekliyor
                                            .OrderBy(t => t.CreatedAt)
                                            .FirstOrDefault();

            if (nextTicket != null)
            {
                nextTicket.Status = 1;               // 1 = Çağrıldı
                nextTicket.CounterId = counterId;

                // BURASI EKSİKTİ EKLENDİ: Çağrılma saatini veritabanına yazıyoruz!
                nextTicket.CalledAt = DateTime.Now;

                _queueTicketDal.Update(nextTicket);
            }

            return nextTicket;
        }

        // --- 4. BİLETİ TAMAMLA ---
        public void CompleteTicket(int ticketId)
        {
            var ticket = _queueTicketDal.GetAll().FirstOrDefault(t => t.Id == ticketId);
            if (ticket != null)
            {
                ticket.Status = 2; // 2 = Tamamlandı

                // KÖK ÇÖZÜM BURASI: İşlem bittiği an saati veritabanına işliyoruz
                ticket.CompletedAt = DateTime.Now;

                _queueTicketDal.Update(ticket);
            }
        }

        // --- 5. BİLETİ BEKLEMEYE AL (YENİ) ---
        public void HoldTicket(int ticketId)
        {
            var ticket = _queueTicketDal.GetAll().FirstOrDefault(t => t.Id == ticketId);
            if (ticket != null)
            {
                ticket.Status = 3; // 3 = Beklemeye Alındı
                _queueTicketDal.Update(ticket);
            }
        }

        // --- 6. BİLETİ İPTAL ET (YENİ) ---
        public void CancelTicket(int ticketId)
        {
            var ticket = _queueTicketDal.GetAll().FirstOrDefault(t => t.Id == ticketId);
            if (ticket != null)
            {
                ticket.Status = 4; // 4 = İptal Edildi

                // İptal edilen işlemlerin de süresini kapatıyoruz ki raporlarda açık kalmasın
                ticket.CompletedAt = DateTime.Now;

                _queueTicketDal.Update(ticket);
            }
        }

        // --- 7. BİLETİ TRANSFER ET (YENİ) ---
        public void TransferTicket(int ticketId)
        {
            var ticket = _queueTicketDal.GetAll().FirstOrDefault(t => t.Id == ticketId);
            if (ticket != null)
            {
                ticket.Status = 0; // Tekrar 0 (Bekliyor) durumuna çekilip havuza atılır
                ticket.CounterId = null; // Bağlı olduğu gişeden koparılır

                // Transfer olduğunda süreyi sıfırlıyoruz ki diğer gişe baştan alsın
                ticket.CreatedAt = DateTime.Now;

                // BURASI EKLENDİ: Transferde çağrılma süresini de sıfırlıyoruz.
                ticket.CalledAt = null;
                ticket.CompletedAt = null;

                _queueTicketDal.Update(ticket);
            }
        }

        // --- 8. LİSTELEME METOTLARI ---
        public List<QueueTicket> GetWaitingTickets()
        {
            return _queueTicketDal.GetAll().Where(t => t.Status == 0).ToList();
        }

        public List<QueueTicket> GetCalledTickets()
        {
            return _queueTicketDal.GetAll().Where(t => t.Status == 1).ToList();
        }
    }
}