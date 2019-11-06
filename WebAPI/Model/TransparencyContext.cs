using Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI.Model
{
    public class TransparencyContext : DbContext
    {
        public DbSet<MemberOfParliament> MemberOfParliaments { get; set; }
        
        public DbSet<Person> Persons { get; set; }
        
        public DbSet<Proposal> Proposals { get; set; }

        public DbSet<Proposer> Proposer { get; set; }

        public DbSet<Vote> Vote { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=blogging.db");
    }
}
