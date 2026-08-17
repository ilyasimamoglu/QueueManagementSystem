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
        // --- BİLET ALMA İŞLEMLERİ ---
        QueueTicket TakeTicket();

        // Kiosk'tan Hizmet Tipi (Örn: Öğrenci İşleri, E-Kampüs) seçilerek bilet alındığında çalışacak
        QueueTicket TakeTicket(string serviceType, string priority);

        // --- GİŞE ÇAĞRI İŞLEMLERİ ---
        // Gişenin hangi hizmet türüne baktığını belirtebilmesi için serviceType parametresi eklendi
        QueueTicket CallNextTicket(int counterId, string serviceType = null);

        void CompleteTicket(int ticketId);

        // Gişeden "Beklemeye Al" dendiğinde çalışacak
        void HoldTicket(int ticketId);

        // Gişeden "İptal Et" dendiğinde çalışacak
        void CancelTicket(int ticketId);

        // Gişeden "Transfer Et" dendiğinde çalışacak
        void TransferTicket(int ticketId);

        // --- LİSTELEME İŞLEMLERİ ---
        // Sadece ilgili hizmet türünün bekleyenlerini saymak için serviceType parametresi eklendi
        List<QueueTicket> GetWaitingTickets(string serviceType = null);

        List<QueueTicket> GetCalledTickets();
    }
}