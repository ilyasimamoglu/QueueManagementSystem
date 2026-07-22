using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QueueManagementSystem.Entities;


namespace QueueManagementSystem.DataAccess
{
    public interface IQueueTicketDal
    {
        // Veritabanındaki tüm biletleri getirecek sözleşme
        List<QueueTicket> GetAll();

        // Veritabanına yeni bilet ekleyecek sözleşme
        void Add(QueueTicket queueTicket);

        // Veritabanındaki mevcut bir bileti güncelleyecek sözleşme
        void Update(QueueTicket queueTicket);
    }
}