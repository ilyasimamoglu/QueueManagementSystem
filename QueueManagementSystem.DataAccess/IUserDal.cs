using System.Collections.Generic;
using QueueManagementSystem.Entities;

namespace QueueManagementSystem.DataAccess
{
    public interface IUserDal
    {
        List<User> GetAll();
        User GetById(int id);
        void Add(User user);
        void Update(User user);
        void Delete(int id);
    }
}