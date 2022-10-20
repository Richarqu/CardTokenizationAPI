using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAppTools.Infrastructure
{
    public interface IUnitOfWork : IDisposable
    {
        int SaveChanges();
    }

    internal class UnitOfWork : IUnitOfWork
    {
        private MyAppToolsDBContext _Context = new MyAppToolsDBContext();
        internal MyAppToolsDBContext Context { get { return this._Context; } }

        public int SaveChanges()
        {
            return this._Context.SaveChanges();
        }

        public void Dispose()
        {
            this._Context.Dispose();
        }
    }
}
