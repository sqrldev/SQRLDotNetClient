using Microsoft.EntityFrameworkCore;
using SQRLDotNetClientUI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.DBContext
{
    public class SQLiteDBContext: DbContext
    {
        public DbSet<UserData> UserData { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=sqrl.db");
    }
}
