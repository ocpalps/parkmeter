using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Parkmeter.Data.EF
{
    public class ParkingContextFactory : IDesignTimeDbContextFactory<ParkmeterContext>
    {
        public ParkmeterContext CreateDbContext(string[] args)
        {

            string connectionString = "Server = (localdb)\\mssqllocaldb; Database = ParkmeterDb; Trusted_Connection = True; MultipleActiveResultSets = true";

            // LOCALDB
            // Server=(localdb)\\mssqllocaldb;Database=ParkmeterDb;Trusted_Connection=True;MultipleActiveResultSets=true

            // to connect locally with SQL management Studio use:
            // np:\\.\pipe\LOCALDB#103F486A\tsql\query

            // if LOCALDB does not start automatically you can run 
            // - sqllocaldb start MSSQLLocalDB
            // - sqllocaldb info MSSQLLocalDB

            if (args.Length == 1 && !String.IsNullOrEmpty(args[0]))
            {
                connectionString = args[0];
            }

            var optionsBuilder = new DbContextOptionsBuilder<ParkmeterContext>();
            optionsBuilder.UseSqlServer(connectionString);
            
            return new ParkmeterContext(optionsBuilder.Options);
        }
    }

}
