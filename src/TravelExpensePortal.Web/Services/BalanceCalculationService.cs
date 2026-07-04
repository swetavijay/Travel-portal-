using Microsoft.EntityFrameworkCore;
using TravelExpensePortal.Web.Data;
using TravelExpensePortal.Web.Models;

namespace TravelExpensePortal.Web.Services
{
    public interface IBalanceCalculationService
    {
        Task<List<EmployeeBalanceSummary>> GetAllEmployeeBalancesAsync();
        Task<EmployeeBalanceSummary?> GetEmployeeBalanceAsync(int employeeId);
        Task<decimal> GetPendingTourExpenseAsync(int employeeId);
        Task<int> GetPendingDrCountAsync(int employeeId);
    }

    /// <summary>
    /// Single source of truth for Debit Balance / Credit Balance / Pending Tour Expense / Pending DR.
    /// Every figure is a live aggregate over AdvanceLedgerEntry + Tour + DailyExpenseReport -
    /// nobody ever types a balance in by hand.
    ///
    /// Balance = SUM(DebitAmount) - SUM(CreditAmount) for the employee.
    ///   > 0  -> employee owes company  -> "DR" (matches legacy "Advance Against Travelling/Expenses" grid)
    ///   < 0  -> company owes employee  -> "CR"
    /// </summary>
    public class BalanceCalculationService : IBalanceCalculationService
    {
        private readonly ApplicationDbContext _db;

        public BalanceCalculationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<EmployeeBalanceSummary>> GetAllEmployeeBalancesAsync()
        {
            var grouped = await _db.AdvanceLedgerEntries
                .GroupBy(l => l.EmployeeId)
                .Select(g => new
                {
                    EmployeeId = g.Key,
                    Balance = g.Sum(x => x.DebitAmount) - g.Sum(x => x.CreditAmount)
                })
                .ToListAsync();

            var balanceLookup = grouped.ToDictionary(g => g.EmployeeId, g => g.Balance);

            var employees = await _db.Employees
                .Include(e => e.Department)
                .Include(e => e.ReportingAuthority)
                .Where(e => e.IsActive)
                .ToListAsync();

            var result = new List<EmployeeBalanceSummary>();
            foreach (var emp in employees)
            {
                balanceLookup.TryGetValue(emp.Id, out var bal);
                if (bal == 0) continue; // only non-zero balances show on the ledger grid, like legacy screen

                result.Add(await MapEmployeeToBalanceSummaryAsync(emp, bal));
            }

            return result.OrderBy(r => r.EmpCode).ToList();
        }

        public async Task<EmployeeBalanceSummary?> GetEmployeeBalanceAsync(int employeeId)
        {
            var emp = await _db.Employees
                .Include(e => e.Department)
                .Include(e => e.ReportingAuthority)
                .FirstOrDefaultAsync(e => e.Id == employeeId);
            if (emp is null) return null;

            var bal = await CalculateEmployeeNetBalanceAsync(employeeId);
            return await MapEmployeeToBalanceSummaryAsync(emp, bal);
        }

        /// <summary>
        /// Extracts common logic for mapping an Employee to EmployeeBalanceSummary.
        /// Reusable across GetAllEmployeeBalancesAsync and GetEmployeeBalanceAsync.
        /// </summary>
        private async Task<EmployeeBalanceSummary> MapEmployeeToBalanceSummaryAsync(Employee emp, decimal balance)
        {
            return new EmployeeBalanceSummary
            {
                EmployeeId = emp.Id,
                AnalysisCode = emp.EmpCode,
                EmpCode = emp.EmpCode,
                Name = emp.Name,
                Department = emp.Department?.Name ?? "",
                ReportingAuthority = emp.ReportingAuthority?.Name ?? "",
                Balance = Math.Abs(balance),
                BalanceType = balance >= 0 ? DrCr.DR : DrCr.CR,
                PendingTourExpense = await GetPendingTourExpenseAsync(emp.Id),
                PendingDrCount = await GetPendingDrCountAsync(emp.Id)
            };
        }

        /// <summary>
        /// Extracts the net balance calculation logic: SUM(Debit) - SUM(Credit).
        /// Reusable across GetAllEmployeeBalancesAsync and GetEmployeeBalanceAsync.
        /// </summary>
        private async Task<decimal> CalculateEmployeeNetBalanceAsync(int employeeId)
        {
            var debit = await _db.AdvanceLedgerEntries
                .Where(l => l.EmployeeId == employeeId)
                .SumAsync(l => l.DebitAmount);
            var credit = await _db.AdvanceLedgerEntries
                .Where(l => l.EmployeeId == employeeId)
                .SumAsync(l => l.CreditAmount);
            return debit - credit;
        }

        /// <summary>
        /// Pending Tour Expense = advance taken on tours that are not yet Settled, minus whatever
        /// expense amount has already been recorded against them (i.e. still to be adjusted/audited).
        /// </summary>
        public async Task<decimal> GetPendingTourExpenseAsync(int employeeId)
        {
            var openTours = await _db.Tours
                .Where(t => t.EmployeeId == employeeId && t.Status != TourStatus.Settled && t.Status != TourStatus.Rejected)
                .ToListAsync();

            return openTours.Sum(t => Math.Max(t.TourAdvanceTaken - t.ExpenseAmount, 0));
        }

        /// <summary>
        /// Pending DR = count of Daily Expense Report dates within a tour's date range
        /// for which no DR has been submitted yet (mirrors "On These Dates DR Not Submitted").
        /// Extracted into a helper to avoid code duplication when counting missing DRs.
        /// </summary>
        public async Task<int> GetPendingDrCountAsync(int employeeId)
        {
            var tours = await _db.Tours
                .Where(t => t.EmployeeId == employeeId && t.Status == TourStatus.Open)
                .ToListAsync();

            int pending = 0;
            foreach (var tour in tours)
            {
                pending += await CountMissingDrDatesForTourAsync(tour.Id);
            }
            return pending;
        }

        /// <summary>
        /// Extracts the logic for counting missing DR dates for a specific tour.
        /// Reusable when calculating pending DRs or when displaying missing dates in UI.
        /// </summary>
        private async Task<int> CountMissingDrDatesForTourAsync(int tourId)
        {
            var tour = await _db.Tours.FirstOrDefaultAsync(t => t.Id == tourId);
            if (tour is null) return 0;

            var submittedDates = await _db.DailyExpenseReports
                .Where(d => d.TourId == tourId && d.IsSubmitted)
                .Select(d => d.ReportDate.Date)
                .ToListAsync();

            int missingCount = 0;
            for (var d = tour.StartDate.Date; d <= tour.EndDate.Date; d = d.AddDays(1))
            {
                if (!submittedDates.Contains(d)) missingCount++;
            }
            return missingCount;
        }
    }
}
