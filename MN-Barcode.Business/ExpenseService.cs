using MN_Barcode.DataAccess;
using MN_Barcode.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MN_Barcode.Business
{
    public class ExpenseService
    {
        private BarcodeContext _context;

        public ExpenseService()
        {
            _context = new BarcodeContext();
        }

        // Gider Ekle
        public void Add(Expense expense)
        {
            _context.Expenses.Add(expense);
            _context.SaveChanges();
        }

        // Gider Sil
        public void Delete(int id)
        {
            var expense = _context.Expenses.Find(id);
            if (expense != null)
            {
                _context.Expenses.Remove(expense);
                _context.SaveChanges();
            }
        }

        // Gider Listele (Tarih Aralığına Göre)
        public List<Expense> GetExpenses(DateTime startDate, DateTime endDate)
        {
            return _context.Expenses.ToList()
                .Where(x => x.ExpenseDate.Date >= startDate.Date && x.ExpenseDate.Date <= endDate.Date)
                .OrderByDescending(x => x.ExpenseDate)
                .ToList();
        }

        // Toplam Gider (Tarih Aralığına Göre)
        public decimal GetTotalExpense(DateTime startDate, DateTime endDate)
        {
            return _context.Expenses.ToList()
                .Where(x => x.ExpenseDate.Date >= startDate.Date && x.ExpenseDate.Date <= endDate.Date)
                .Sum(x => x.Amount);
        }
    }
}
