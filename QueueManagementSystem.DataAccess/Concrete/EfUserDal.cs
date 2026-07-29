using System.Collections.Generic;
using System.Linq;
using QueueManagementSystem.Entities;

namespace QueueManagementSystem.DataAccess.Concrete
{
    public class EfUserDal : IUserDal
    {
        private readonly QueueDbContext _context;

        // Dependency Injection ile DbContext'i alıyoruz
        public EfUserDal(QueueDbContext context)
        {
            _context = context;
        }

        public List<User> GetAll()
        {
            // Veritabanındaki tüm kullanıcıları listele
            return _context.Users.ToList();
        }

        public User GetById(int id)
        {
            // İlgili ID'ye sahip kullanıcıyı bul
            return _context.Users.FirstOrDefault(u => u.Id == id);
        }

        public void Add(User user)
        {
            // Yeni kullanıcıyı ekle ve veritabanına kaydet
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        public void Update(User user)
        {
            // Kullanıcı bilgilerini güncelle ve kaydet
            _context.Users.Update(user);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            // Önce silinecek kullanıcıyı bul, varsa sil
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
        }
    }
}