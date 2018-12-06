using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DP.TwinRinksHelper.Web.Models
{
    public class TwinRinksHelperContext : DbContext
    {
        public TwinRinksHelperContext(DbContextOptions<TwinRinksHelperContext> options) : base(options)
        {
            
        }
        public DbSet<Models.ScheduleSyncSpec> ScheduleSyncSpecs { get; set; }
    }
}
