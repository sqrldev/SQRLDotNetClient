using Microsoft.EntityFrameworkCore;
using SQRLDotNetClientUI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SQRLDotNetClientUI.DBContext
{
    public class SQLiteDBContext: DbContext
    {
        public DbSet<UserData> UserData { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var directory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            if (!File.Exists(Path.Combine(directory, "sqrl.db")))
                directory = "";
            Console.WriteLine($"{directory}: {File.Exists(Path.Combine(directory, "sqrl.db"))}");
            options.UseSqlite($"Data Source={Path.Combine(directory,"sqrl.db")}");
        }
    }
}
