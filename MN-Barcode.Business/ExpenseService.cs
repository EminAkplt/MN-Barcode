using MN_Barcode.DataAccess;
using MN_Barcode.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MN_Barcode.Business
{
    /// <summary>
    /// Gider iş mantığı servisi.
    /// Her metot kendi kısa ömürlü context'ini kullanır (bkz. CategoryService notu).
    /// </summary>
    public class ExpenseService
    {
        // Gider Ekle
        public void Add(Expense expense)
        {
            using var context = new BarcodeContext();
            context.Expenses.Add(expense);
            context.SaveChanges();
        }

        // Gider Sil
        public void Delete(int id)
        {
            using var context = new BarcodeContext();
            var expense = context.Expenses.Find(id);
            if (expense != null)
            {
                context.Expenses.Remove(expense);
                context.SaveChanges();
            }
        }

        public List<Expense> GetExpenses(DateTime startDate, DateTime endDate, string search = "")
        {
            using var context = new BarcodeContext();

            DateTime baslangic = startDate.Date;
            DateTime bitisHaric = endDate.Date.AddDays(1);

            var query = context.Expenses.Where(x => x.ExpenseDate >= baslangic && x.ExpenseDate < bitisHaric);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(x => x.Description.Contains(search));

            return query.OrderByDescending(x => x.ExpenseDate).ToList();
        }

        // Toplam Gider (Tarih Aralığına Göre)
        public decimal GetTotalExpense(DateTime startDate, DateTime endDate)
        {
            using var context = new BarcodeContext();

            DateTime baslangic = startDate.Date;
            DateTime bitisHaric = endDate.Date.AddDays(1);

            // Toplam doğrudan veritabanında hesaplanır.
            return context.Expenses
                .Where(x => x.ExpenseDate >= baslangic && x.ExpenseDate < bitisHaric)
                .Sum(x => (decimal?)x.Amount) ?? 0m; // Kayıt yoksa 0
        }
    }
}
