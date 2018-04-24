using Parkmeter.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parkmeter.Core.Interfaces
{
    public interface IRepositoryFactory 
    {
        IRepository<T> CreateRepository<T>(string connectionString) where T : BaseEntity;
    }
}
