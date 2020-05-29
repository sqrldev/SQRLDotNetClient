using Microsoft.EntityFrameworkCore;
using Serilog;
using SQRLCommon.Models;
using SQRLDotNetClientUI.DB.Models;
using System;
using System.IO;

namespace SQRLDotNetClientUI.DB.DBContext
{
    public sealed class SQRLDBContext: DbContext
    {
        private static SQRLDBContext _instance;

        /// <summary>
        /// The constructor is private, use the <Instance>property</Instance>
        /// to get the singletion class instance.
        /// </summary>
        private SQRLDBContext():base()
        {
            
        }

        /// <summary>
        /// Allows us to dispose the singleton instance if needed to reload the DB File
        /// </summary>
        public static void DisposeDB()
        {
            _instance.Dispose();
            _instance = null;
        }

        /// <summary>
        /// Gets the singleton <c>SQRLDBContext</c> instance.
        /// </summary>
        public static SQRLDBContext Instance
        {
            get 
            {
                if (_instance == null) _instance = new SQRLDBContext();
                    return _instance;
            }
        }

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
            if (!Directory.Exists(PathConf.ClientDBPath))
            {
                Log.Information("DB directory did not exist, creating");
                Directory.CreateDirectory(PathConf.ClientDBPath);
            }


            options.UseSqlite($"Data Source={PathConf.FullClientDbPath}");
        }
    }
}
