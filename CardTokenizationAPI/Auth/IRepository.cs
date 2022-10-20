using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAppTools.Infrastructure
{
    public interface IRepository<T> : IDisposable
     where T : class
    {
        IQueryable<T> All { get; }
        void Add(T entity);
        void Delete(T entity);
        void Edit(T entity);
        T Find(params object[] keyValues);

    }

    internal partial class Repository<T> : IRepository<T>,
IDisposable
where T : class
    {
        #region Main Implementation
        private DbContext _context;

        public Repository(DbContext context)
        {
            this._context = context;
        }

        public virtual IQueryable<T> All
        {
            get
            {
                return GetAll();
            }
        }


        private IQueryable<T> GetAll()
        {
            IQueryable<T> query = _context.Set<T>();
            return query;
        }

        public virtual void Add(T entity)
        {
            _context.Set<T>().Add(entity);
        }

        public virtual void Edit(T entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
        }

        public virtual void Delete(T entity)
        {
            _context.Entry(entity).State = EntityState.Deleted;
        }

        public virtual T Find(params object[] keyValues)
        {
            return _context.Set<T>().Find(keyValues);
        }
        #endregion

        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }

            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }




}