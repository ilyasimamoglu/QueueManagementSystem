using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QueueManagementSystem.Entities;

namespace QueueManagementSystem.Business
{
    public interface IQueueTicketService
    {
        // --- MEVCUT METOTLARIN ---
        QueueTicket TakeTicket();
        QueueTicket CallNextTicket(int counterId);
        void CompleteTicket(int ticketId);
        List<QueueTicket> GetWaitingTickets();
        List<QueueTicket> GetCalledTickets();

        // --- YENİ EKLENEN METOTLAR (Gişe ve Kiosk İçin) ---

        // Kiosk'tan Hizmet Tipi ve Öncelik seçilerek bilet alındığında çalışacak
        QueueTicket TakeTicket(string serviceType, string priority);

        // Gişeden "Beklemeye Al" dendiğinde çalışacak
        void HoldTicket(int ticketId);

        // Gişeden "İptal Et" dendiğinde çalışacak
        void CancelTicket(int ticketId);

        // Gişeden "Transfer Et" dendiğinde çalışacak
        void TransferTicket(int ticketId);
    }
}