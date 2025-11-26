using Microsoft.EntityFrameworkCore;
using MN_Barcode.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;


namespace MN_Barcode.DataAccess
{
    public class BarcodeContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleDetail> SaleDetails { get; set; }
        public DbSet<AppUser> Users { get; set; } // Kullanıcılar tablosu

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Veritabanı adını 'MNBarcodeLocal' yaptık ki eskisiyle karışmasın.
            optionsBuilder.UseSqlServer("Data Source=.\\SQLEXPRESS; Initial Catalog=MNBarcodeLocal; Integrated Security=True; TrustServerCertificate=True;");
        }
    }
}
