using Microsoft.EntityFrameworkCore;
using Parkmeter.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parkmeter.Data.EF
{
    public class ParkmeterContext : DbContext
    {
        public DbSet<Parking> Parkings { get; set; }
        public DbSet<Space> Spaces { get; set; }

        public ParkmeterContext(DbContextOptions<ParkmeterContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //VehicleAccess will be managed in a NoSql db
            modelBuilder.Ignore<VehicleAccess>();
        }
    }
}
