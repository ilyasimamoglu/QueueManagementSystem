using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QueueManagementSystem.DataAccess;
using QueueManagementSystem.Entities;

namespace QueueManagementSystem.Business
{
    public class CounterManager : ICounterService
    {

        private readonly QueueDbContext _context;

        // Veritabanı bağlamımızı constructor (yapıcı metot) ile enjekte ediyoruz
        public CounterManager(QueueDbContext context)
        {
            _context = context;
        }

        public List<Counter> GetAll()
        {
            return _context.Counters.ToList();
        }

        public void Add(Counter counter)
        {
            _context.Counters.Add(counter);
            _context.SaveChanges();
        }

    }
}
