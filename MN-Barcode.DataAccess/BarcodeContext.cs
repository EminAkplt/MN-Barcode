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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {


            optionsBuilder.UseSqlServer("Data Source=.\\SQLEXPRESS; Initial Catalog=MNBarcodeDb; Integrated Security=True; TrustServerCertificate=True;");
        }
    }
}
