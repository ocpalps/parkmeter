using Microsoft.EntityFrameworkCore;
using Parkmeter.Core.Interfaces;
using Parkmeter.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Parkmeter.Data.EF
{
    public class EfRepository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly ParkmeterContext _dbContext;

        public EfRepository(ParkmeterContext dbContext)
        {
            _dbContext = dbContext;
        }

        public T GetById(int id, string[] navigationProperties = null)
        {
            IQueryable<T> query = _dbContext.Set<T>().AsQueryable();
            if (navigationProperties != null && navigationProperties.Length > 0)
            {
                foreach (var navigationProperty in navigationProperties)
                {
                    query = query.Include(navigationProperty);
                }
            }
            
            return query.SingleOrDefault(e => e.ID == id);

        }

        public List<T> List()
        {
            return _dbContext.Set<T>().ToList();
        }

        public List<T> List(ISpecification<T> spec)
        {
            var queryableResultWithIncludes = spec.Includes
                .Aggregate(_dbContext.Set<T>().AsQueryable(),
                            (current, include) => current.Include(include));
            return queryableResultWithIncludes
                            .Where(spec.Criteria)
                            .ToList();
        }

        public T Add(T entity)
        {
            _dbContext.Set<T>().Add(entity);
            _dbContext.SaveChanges();

            return entity;
        }

        public void Delete(T entity)
        {
            _dbContext.Set<T>().Remove(entity);
            _dbContext.SaveChanges();
        }

        public void Update(T entity)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
            _dbContext.SaveChanges();
        }

    }
}
