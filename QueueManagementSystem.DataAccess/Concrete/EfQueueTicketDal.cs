using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QueueManagementSystem.Entities;

namespace QueueManagementSystem.DataAccess
{
    // Sınıfımız IQueueTicketDal arayüzündeki kuralları uygulayacağını taahhüt ediyor
    public class EfQueueTicketDal : IQueueTicketDal
    {
        // Entity Framework Core'un veritabanı bağlantı sınıfı (DbContext)
        // Senin Solution Explorer'daki ismine uygun olarak 'QueueDbContext' yapıldı.
        private readonly QueueDbContext _context;

        // Dependency Injection ile DbContext'i içeri alıyoruz
        public EfQueueTicketDal(QueueDbContext context)
        {
            _context = context;
        }

        public List<QueueTicket> GetAll()
        {
            // Veritabanındaki tüm biletleri liste olarak döner
            return _context.QueueTickets.ToList();
        }

        public void Add(QueueTicket queueTicket)
        {
            // Yeni bileti EF Core'un takip listesine ekler
            _context.QueueTickets.Add(queueTicket);
            // Değişiklikleri SQL Server'a kaydeder (INSERT)
            _context.SaveChanges();
        }

        public void Update(QueueTicket queueTicket)
        {
            // Mevcut biletin güncellendiğini EF Core'a bildirir
            _context.QueueTickets.Update(queueTicket);
            // Değişiklikleri SQL Server'a kaydeder (UPDATE)
            _context.SaveChanges();
        }
    }
}