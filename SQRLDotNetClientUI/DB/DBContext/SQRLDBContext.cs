using Microsoft.EntityFrameworkCore;
using SQRLDotNetClientUI.DB.Models;
using System;
using System.IO;

namespace SQRLDotNetClientUI.DB.DBContext
{
    public class SQRLDBContext: DbContext
    {
        /// <summary>
        /// Used for saving user state like last loaded identity etc. 
        /// </summary>
        public DbSet<UserData> UserData { get; set; }

        /// <summary>
        /// The list of imported identities.
        /// </summary>
        public DbSet<Identity> Identities { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var directory = Path.GetDirectoryName(AppContext.BaseDirectory);
            if (!File.Exists(Path.Combine(directory, "sqrl.db")))
                directory = "";
            Console.WriteLine($"{directory}: {File.Exists(Path.Combine(directory, "sqrl.db"))}");
            options.UseSqlite($"Data Source={Path.Combine(directory,"sqrl.db")}");
        }
    }
}
