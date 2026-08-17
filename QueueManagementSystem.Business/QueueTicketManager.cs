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

        // --- 2. KIOSK İÇİN DETAYLI BİLET ALMA (GÜNCELLENDİ) ---
        public QueueTicket TakeTicket(string serviceType, string priority)
        {
            // 1. Sadece istenen hizmet türüne ait biletleri filtrele
            var serviceTickets = _queueTicketDal.GetAll().Where(t => t.ServiceType == serviceType);

            // 2. Başlangıç numaralarını hizmet türüne göre belirle
            int startNumber = 1;
            if (serviceType == "Öğrenci İşleri")
                startNumber = 1000;
            // BURASI DÜZELTİLDİ: E-Kampüs yerine Donanım Destek yazıldı
            else if (serviceType == "Donanım Destek")
                startNumber = 500;

            // 3. O hizmet türünde bilet varsa en büyüğünü al, yoksa belirlediğin başlangıç numarasından başlat
            int lastTicketNumber = serviceTickets.Any() ? serviceTickets.Max(t => t.TicketNumber) : (startNumber - 1);

            QueueTicket newTicket = new QueueTicket
            {
                TicketNumber = lastTicketNumber + 1,
                ServiceType = serviceType,           // Hangi hizmet olduğunu veritabanına yazıyoruz
                Status = 0,                          // 0 = Bekliyor
                CreatedAt = DateTime.Now
            };

            _queueTicketDal.Add(newTicket);
            return newTicket;
        }

        // --- 3. SIRADAKİ BİLETİ ÇAĞIR (GÜNCELLENDİ) ---
        public QueueTicket CallNextTicket(int counterId, string serviceType = null)
        {
            var query = _queueTicketDal.GetAll().Where(t => t.Status == 0); // Bekleyenler

            // Gişe spesifik bir hizmet türünü çağırıyorsa filtrele
            if (!string.IsNullOrEmpty(serviceType))
            {
                query = query.Where(t => t.ServiceType == serviceType);
            }

            var nextTicket = query.OrderBy(t => t.CreatedAt).FirstOrDefault();

            if (nextTicket != null)
            {
                nextTicket.Status = 1;               // 1 = Çağrıldı
                nextTicket.CounterId = counterId;
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
                ticket.CompletedAt = DateTime.Now;
                _queueTicketDal.Update(ticket);
            }
        }

        // --- 5. BİLETİ BEKLEMEYE AL ---
        public void HoldTicket(int ticketId)
        {
            var ticket = _queueTicketDal.GetAll().FirstOrDefault(t => t.Id == ticketId);
            if (ticket != null)
            {
                ticket.Status = 3; // 3 = Beklemeye Alındı
                _queueTicketDal.Update(ticket);
            }
        }

        // --- 6. BİLETİ İPTAL ET ---
        public void CancelTicket(int ticketId)
        {
            var ticket = _queueTicketDal.GetAll().FirstOrDefault(t => t.Id == ticketId);
            if (ticket != null)
            {
                ticket.Status = 4; // 4 = İptal Edildi
                ticket.CompletedAt = DateTime.Now;
                _queueTicketDal.Update(ticket);
            }
        }

        // --- 7. BİLETİ TRANSFER ET ---
        public void TransferTicket(int ticketId)
        {
            var ticket = _queueTicketDal.GetAll().FirstOrDefault(t => t.Id == ticketId);
            if (ticket != null)
            {
                ticket.Status = 0; // Tekrar 0 (Bekliyor) durumuna çekilip havuza atılır
                ticket.CounterId = null;
                ticket.CreatedAt = DateTime.Now;
                ticket.CalledAt = null;
                ticket.CompletedAt = null;

                _queueTicketDal.Update(ticket);
            }
        }

        // --- 8. LİSTELEME METOTLARI (GÜNCELLENDİ) ---
        public List<QueueTicket> GetWaitingTickets(string serviceType = null)
        {
            var query = _queueTicketDal.GetAll().Where(t => t.Status == 0);

            if (!string.IsNullOrEmpty(serviceType))
            {
                query = query.Where(t => t.ServiceType == serviceType);
            }

            return query.ToList();
        }

        public List<QueueTicket> GetCalledTickets()
        {
            return _queueTicketDal.GetAll().Where(t => t.Status == 1).ToList();
        }
    }
}