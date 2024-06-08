using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Threading.Tasks;
using PassionProject.Models;

namespace PassionProject.Services
{
    public class BaseService<TEntity> where TEntity : class
    {
        protected readonly ApplicationDbContext _context;

        public BaseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(TEntity entity)
        {
            _context.Set<TEntity>().Add(entity);
            await _context.SaveChangesAsync();
        }

        public IEnumerable<TEntity> GetAll()
        {
            return _context.Set<TEntity>();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
