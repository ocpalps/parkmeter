using Parkmeter.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Parkmeter.Core.Models;

namespace Parkmeter.Data.EF
{
    public class EFRepositoryFactory : IRepositoryFactory
    {
        public IRepository<T> CreateRepository<T>(string connectionString) where T : BaseEntity
        {
            ParkingContextFactory parkmeterFactory = new ParkingContextFactory();
            var dbContext = parkmeterFactory.CreateDbContext(new string[] { connectionString });
            return new EfRepository<T>(dbContext);
        }
       
    }
}
