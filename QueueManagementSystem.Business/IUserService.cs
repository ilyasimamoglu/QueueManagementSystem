using System.Collections.Generic;
using QueueManagementSystem.Entities;

namespace QueueManagementSystem.Business
{
    public interface IUserService
    {
        List<User> GetAllUsers();
        User GetUserById(int id);
        void AddUser(User user);
        void UpdateUser(User user);
        void DeleteUser(int id);

        // Login (Giriş) ekranı için kullanılacak özel metot
        User AuthenticateUser(string username, string password);
    }
}