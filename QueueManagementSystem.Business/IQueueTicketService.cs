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
        // Müşteri sıramatikten yeni bir bilet aldığında bu çalışacak
        QueueTicket TakeTicket();

        // Gişe görevlisi butona basıp sıradaki kişiyi çağırdığında bu çalışacak
        QueueTicket CallNextTicket(int counterId);

        void CompleteTicket(int ticketId);

        // Bekleyen biletleri getirecek metot
        List<QueueTicket> GetWaitingTickets();

        // Şu an gişede işlem gören biletleri getirecek metot
        List<QueueTicket> GetCalledTickets();
    }
}
