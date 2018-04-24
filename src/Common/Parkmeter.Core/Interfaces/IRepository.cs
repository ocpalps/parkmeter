using Parkmeter.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parkmeter.Core.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        T GetById(int id, string[] navigationProperties = null);
        List<T> List();
        List<T> List(ISpecification<T> spec);
        T Add(T entity);
        void Update(T entity);
        void Delete(T entity);
    }
}
