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

        // Gider Listele (Tarih Aralığına Göre)
        public List<Expense> GetExpenses(DateTime startDate, DateTime endDate)
        {
            using var context = new BarcodeContext();

            // Aralığı gün bazına normalize et (başlangıç günü 00:00, bitiş gününün sonu).
            DateTime baslangic = startDate.Date;
            DateTime bitisHaric = endDate.Date.AddDays(1);

            // Filtre veritabanında uygulanır; tüm gider tablosu belleğe çekilmez.
            return context.Expenses
                .Where(x => x.ExpenseDate >= baslangic && x.ExpenseDate < bitisHaric)
                .OrderByDescending(x => x.ExpenseDate)
                .ToList();
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
