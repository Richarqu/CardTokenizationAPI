using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAppTools.Infrastructure
{
    internal class BaseRepository<T> : Repository<T>
       where T : class
    {
        public BaseRepository(IUnitOfWork unitOfWork)
            : base((unitOfWork as UnitOfWork).Context)
        {
        }
    }
}
