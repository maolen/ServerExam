using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public class ContextDb : DbContext
    {
        public ContextDb()
        {
            Database.EnsureCreated();
        }
        public DbSet<User> Users { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=A-104-06;Database=ServerDb;Trusted_Connection=true;");
        }
    }
}
