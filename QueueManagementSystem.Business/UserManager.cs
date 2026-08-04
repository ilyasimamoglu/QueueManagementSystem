using System;
using System.Collections.Generic;
using System.Linq;
using QueueManagementSystem.DataAccess;
using QueueManagementSystem.Entities;

namespace QueueManagementSystem.Business
{
    public class UserManager : IUserService
    {
        private readonly IUserDal _userDal;

        public UserManager(IUserDal userDal)
        {
            _userDal = userDal;
        }

        public List<User> GetAllUsers()
        {
            return _userDal.GetAll();
        }

        public User GetUserById(int id)
        {
            return _userDal.GetById(id);
        }

        public void AddUser(User user)
        {
            // İş Kuralı 1: Boş veri kontrolü
            if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password))
            {
                throw new Exception("Kullanıcı adı ve şifre boş bırakılamaz.");
            }

            // İş Kuralı 2: Aynı kullanıcı adından başka var mı? (Büyük/Küçük harf duyarsız)
            var isUsernameExists = _userDal.GetAll().Any(u => u.Username.Equals(user.Username, StringComparison.OrdinalIgnoreCase));
            if (isUsernameExists)
            {
                throw new Exception("Bu kullanıcı adı sistemde zaten kayıtlı. Lütfen başka bir kullanıcı adı belirleyin.");
            }

            // Otomatik atamalar
            user.CreatedAt = DateTime.Now;

            // Veritabanına gönder
            _userDal.Add(user);
        }

        public void UpdateUser(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Username))
            {
                throw new Exception("Kullanıcı adı boş bırakılamaz.");
            }

            var existingUser = _userDal.GetById(user.Id);
            if (existingUser == null)
            {
                throw new Exception("Güncellenmek istenen kullanıcı bulunamadı.");
            }

            // İş Kuralı 3: Eğer kullanıcı adını değiştiriyorsa, bu yeni ad başkası tarafından kullanılıyor mu?
            if (existingUser.Username != user.Username)
            {
                var isUsernameExists = _userDal.GetAll().Any(u => u.Username.Equals(user.Username, StringComparison.OrdinalIgnoreCase));
                if (isUsernameExists)
                {
                    throw new Exception("Bu kullanıcı adı başka bir personele ait.");
                }
            }

            // Bilgileri güncelle
            existingUser.FullName = user.FullName;
            existingUser.Username = user.Username;
            existingUser.Role = user.Role;
            existingUser.IsActive = user.IsActive;

            // ÇÖZÜM BURADA: Gişe atamasını API'den gelen yeni değere eşitliyoruz
            existingUser.AssignedCounter = user.AssignedCounter;

            // Eğer şifre alanı boş gönderilmemişse, şifreyi de güncelle
            if (!string.IsNullOrWhiteSpace(user.Password))
            {
                existingUser.Password = user.Password;
            }

            _userDal.Update(existingUser);
        }

        public void DeleteUser(int id)
        {
            _userDal.Delete(id);
        }

        public User AuthenticateUser(string username, string password)
        {
            // Kullanıcı adı ve şifre eşleşiyorsa ve kullanıcının hesabı Aktif (IsActive = true) ise girişine izin ver
            var user = _userDal.GetAll().FirstOrDefault(u => u.Username == username && u.Password == password && u.IsActive);

            return user; // Eşleşme yoksa null döner
        }
    }
}