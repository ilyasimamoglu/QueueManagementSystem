using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QueueManagementSystem.Entities;

namespace QueueManagementSystem.Business
{
    public interface ICounterService
    {
        List<Counter> GetAll();
        void Add(Counter counter);


    }
}
