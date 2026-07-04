# TIPL Travel Expense & Advance Management Portal — Complete Source Code

Yeh ek single file hai jisme project ka **saara code** hai — har file ka path heading ke roop me diya gaya hai.

**Kisi developer ko dene ke liye:** Unhe bolo ki har section ke upar likhe path pe wahi file banaye apne folder me (jaisa likha hai), phir README wale setup steps follow karein.

**Technology**: ASP.NET Core 8 MVC, Entity Framework Core, SQL Server, Bootstrap 5, jQuery/AJAX

---


## FILE: `README.md`

```markdown
# TIPL Travel Expense & Advance Management Portal

ASP.NET Core 8 MVC + EF Core + SQL Server + Bootstrap 5 + jQuery/AJAX.

## What's included in this build

**Unified Travel Dashboard** (`/Dashboard/Index`) — combines the three legacy screens you shared into one tabbed page:
1. **View Open Tours** — live grid of all open tours
2. **Advance Against Travelling/Expenses** — auto-calculated Debit/Credit balance per employee (no manual entry, ever)
3. **Employee Tour/DR Status search** — search by name, see requested/approved/taken advance, expense amount, status, and exactly which DR dates are missing

**Calculation engines** (`Services/`)
- `BalanceCalculationService` — Debit Balance, Credit Balance, Pending Tour Expense, Pending DR: all computed live from the `AdvanceLedgerEntries` ledger table + `Tours` + `DailyExpenseReports`. Nobody types a balance in by hand; it's always `SUM(Debit) - SUM(Credit)`.
- `TeRateCalculationService` — implements the actual TIPL TE Rules (w.e.f. 1 Apr 2025) you uploaded: designation × city-type lodging/boarding lookup, Mumbai/female additions, joint-tour ×1.3, minimum/refreshing lodging, 30/30/30/10 boarding split with meal-time windows and customer-provided-meal handling.

**Rate Master** (`RateMasters` table) — seeded directly from the policy PDF (all 11 designation tiers × 3 city types), editable from the DB/future Admin UI so policy revisions don't need code changes.

## Prerequisites to run

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB is fine for dev — comes with Visual Studio) or full SQL Server/Azure SQL
- Visual Studio 2022 (17.8+) or VS Code with C# Dev Kit

## Setup

```bash
cd src/TravelExpensePortal.Web

# restore & build
dotnet restore
dotnet build

# create the initial migration (first time only)
dotnet ef migrations add InitialCreate

# apply to your DB (connection string in appsettings.json)
dotnet ef database update

# run
dotnet run
```

On first run in Development, `DbSeeder` automatically seeds demo data shaped like the screens you shared (same tour numbers, employee names, balances) so you can visually compare against the legacy system immediately.

Navigate to `https://localhost:xxxx/Dashboard/Index`. You'll need to register a user first (Identity is wired up) — for a quick start, use `dotnet ef` or a small seed script to create an Admin login, or extend `DbSeeder` to call `userManager.CreateAsync(...)`.

## Project layout

```
TravelExpensePortal.sln
src/TravelExpensePortal.Web/
  Controllers/       DashboardController (3-in-1 AJAX endpoints), AccountController (login)
  Models/             Employee, Designation, RateMaster, Tour, DailyExpenseReport, AdvanceLedgerEntry, ViewModels
  Services/           BalanceCalculationService, TeRateCalculationService
  Data/               ApplicationDbContext (+ seed data from TE policy), DbSeeder (demo data)
  Views/              Dashboard/Index.cshtml (unified tabbed view), Account/Login.cshtml, Shared/_Layout.cshtml
  wwwroot/            js/dashboard.js (AJAX + DataTables wiring), css/site.css
database/
  schema_reference.sql   Plain SQL reference schema (EF migrations are the real source of truth)
```

## What's next (not yet built — tell me which to prioritize)

- Tour creation/approval workflow (Reporting Authority approval chain)
- Daily Expense (DR) entry form with the boarding/lodging auto-calc wired into the UI
- Tour Settlement screen (close out a tour, post the final ledger entries)
- DSIC day-slab conveyance calculation UI
- Foreign tour per-diem calculator
- Admin screens to edit Rate Master / Cities without touching code
- Role-based approval routing (Employee → RA → Accounts/Audit → CEO Office exceptions)
- Reports/exports (Excel/PDF) for the ledger and settlement history
```

---

## FILE: `TravelExpensePortal.sln`

```text
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.9.0.0
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "TravelExpensePortal.Web", "src\TravelExpensePortal.Web\TravelExpensePortal.Web.csproj", "{A1B2C3D4-0001-4000-8000-000000000001}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{A1B2C3D4-0001-4000-8000-000000000001}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A1B2C3D4-0001-4000-8000-000000000001}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A1B2C3D4-0001-4000-8000-000000000001}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A1B2C3D4-0001-4000-8000-000000000001}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal
```

---

## FILE: `database/schema_reference.sql`

```sql
-- =========================================================================
-- TIPL Travel Expense & Advance Management Portal
-- Reference schema (EF Core Migrations will generate the actual DB - this
-- file is for DBA review / manual environments where migrations aren't run)
-- =========================================================================

CREATE TABLE Departments (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL
);

CREATE TABLE Designations (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Rank INT NOT NULL,
    IsDsicEngineer BIT NOT NULL DEFAULT 0
);

CREATE TABLE Employees (
    Id INT IDENTITY PRIMARY KEY,
    EmpCode NVARCHAR(20) NOT NULL UNIQUE,
    Name NVARCHAR(150) NOT NULL,
    DesignationId INT NOT NULL REFERENCES Designations(Id),
    DepartmentId INT NOT NULL REFERENCES Departments(Id),
    ReportingAuthorityId INT NULL REFERENCES Employees(Id),
    Gender INT NOT NULL,
    HomeCity NVARCHAR(150) NULL,
    Email NVARCHAR(150) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    IdentityUserId NVARCHAR(450) NULL
);

CREATE TABLE Cities (
    Id INT IDENTITY PRIMARY KEY,
    CityName NVARCHAR(150) NOT NULL,
    CityType INT NOT NULL,   -- 0=Metro, 1=StateCapital, 2=Other
    IsMumbai BIT NOT NULL DEFAULT 0,
    IsForeign BIT NOT NULL DEFAULT 0,
    Country NVARCHAR(100) NULL,
    IsSaarcOrChina BIT NOT NULL DEFAULT 0
);

-- Rate Master: Designation x City Type -> Lodging/Boarding (from TE Rules w.e.f. 1 Apr 2025)
CREATE TABLE RateMasters (
    Id INT IDENTITY PRIMARY KEY,
    DesignationId INT NOT NULL REFERENCES Designations(Id),
    CityType INT NOT NULL,
    MaxLodgingPerDay DECIMAL(10,2) NOT NULL,
    BoardingPerDay DECIMAL(10,2) NOT NULL,
    TravelClass NVARCHAR(200) NOT NULL,
    BillsNeededForTravel BIT NOT NULL DEFAULT 1,
    EffectiveFrom DATETIME2 NOT NULL,
    EffectiveTo DATETIME2 NULL
);

CREATE TABLE DsicRateSlabs (
    Id INT IDENTITY PRIMARY KEY,
    MinDay INT NOT NULL,
    MaxDay INT NOT NULL,
    MaxLodgingPerDay DECIMAL(10,2) NOT NULL,
    MaxConveyancePerDay DECIMAL(10,2) NOT NULL,
    ConveyanceAsPerActual BIT NOT NULL DEFAULT 0,
    EffectiveFrom DATETIME2 NOT NULL
);

CREATE TABLE Tours (
    Id INT IDENTITY PRIMARY KEY,
    TourNo NVARCHAR(30) NOT NULL UNIQUE,
    EmployeeId INT NOT NULL REFERENCES Employees(Id),
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    DestinationCityId INT NULL REFERENCES Cities(Id),
    RequestedTourAdvance DECIMAL(12,2) NOT NULL DEFAULT 0,
    ApprovedTourAdvance DECIMAL(12,2) NOT NULL DEFAULT 0,
    TourAdvanceTaken DECIMAL(12,2) NOT NULL DEFAULT 0,
    ExpenseAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
    AdvanceApprovalDate DATETIME2 NULL,
    Status INT NOT NULL DEFAULT 0,     -- 0=Open,1=PendingForDocumentReceived,2=ToBeAudited,3=DrCompleted,4=Settled,5=Rejected
    IsJointTour BIT NOT NULL DEFAULT 0,
    JointTourSeniorEmployeeId INT NULL,
    CreatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE DailyExpenseReports (
    Id INT IDENTITY PRIMARY KEY,
    DrNo NVARCHAR(30) NOT NULL,
    TourId INT NOT NULL REFERENCES Tours(Id),
    ReportDate DATETIME2 NOT NULL,
    IsSubmitted BIT NOT NULL DEFAULT 0,
    SubmittedOn DATETIME2 NULL,
    Amount DECIMAL(12,2) NOT NULL DEFAULT 0
);

-- Core ledger - EVERY balance figure in the app is derived from this table.
-- Balance(Employee) = SUM(DebitAmount) - SUM(CreditAmount)
CREATE TABLE AdvanceLedgerEntries (
    Id INT IDENTITY PRIMARY KEY,
    EmployeeId INT NOT NULL REFERENCES Employees(Id),
    TourId INT NULL REFERENCES Tours(Id),
    EntryType INT NOT NULL,   -- 0=AdvanceDisbursed,1=TourExpenseSettled,2=CashReturnedByEmployee,3=AdHocAdjustment
    DebitAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
    CreditAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
    TransactionDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Remarks NVARCHAR(300) NULL,
    ScheduledDisbursementDate DATETIME2 NULL,   -- advances processed only Tue & Fri per policy
    IsDisbursed BIT NOT NULL DEFAULT 0
);

CREATE INDEX IX_AdvanceLedgerEntries_EmployeeId ON AdvanceLedgerEntries(EmployeeId);
CREATE INDEX IX_Tours_EmployeeId_Status ON Tours(EmployeeId, Status);

-- Sample view mirroring the "Advance Against Travelling/Expenses" grid (Panel 2)
-- so it can also be consumed directly by SQL Reporting Services if needed.
CREATE OR ALTER VIEW vw_EmployeeBalances AS
SELECT
    e.Id AS EmployeeId,
    e.EmpCode AS AnalysisCode,
    e.EmpCode,
    e.Name,
    d.Name AS Dept,
    ra.Name AS RA,
    ISNULL(SUM(l.DebitAmount) - SUM(l.CreditAmount), 0) AS NetBalance
FROM Employees e
LEFT JOIN Departments d ON d.Id = e.DepartmentId
LEFT JOIN Employees ra ON ra.Id = e.ReportingAuthorityId
LEFT JOIN AdvanceLedgerEntries l ON l.EmployeeId = e.Id
WHERE e.IsActive = 1
GROUP BY e.Id, e.EmpCode, e.Name, d.Name, ra.Name;
GO
```

---

## FILE: `src/TravelExpensePortal.Web/Controllers/AccountController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TravelExpensePortal.Web.Data;

namespace TravelExpensePortal.Web.Controllers
{
    public class LoginVm
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, isPersistent: true, lockoutOnFailure: true);
            if (result.Succeeded)
                return !string.IsNullOrEmpty(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Dashboard");

            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}
```

---

## FILE: `src/TravelExpensePortal.Web/Controllers/DashboardController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelExpensePortal.Web.Data;
using TravelExpensePortal.Web.Models;
using TravelExpensePortal.Web.Services;

namespace TravelExpensePortal.Web.Controllers
{
    /// <summary>
    /// Unified Travel Dashboard - combines:
    ///   1) View Open Tours grid
    ///   2) Advance Against Travelling/Expenses (DR/CR) ledger grid
    ///   3) Employee-wise Tour/DR status search
    /// into a single tabbed page, all data-driven from live calculations (no manual entry of balances).
    /// </summary>
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBalanceCalculationService _balanceService;

        public DashboardController(ApplicationDbContext db, IBalanceCalculationService balanceService)
        {
            _db = db;
            _balanceService = balanceService;
        }

        public IActionResult Index() => View();

        // ---------- Panel 1: Open Tours ----------
        [HttpGet]
        public async Task<IActionResult> OpenTours()
        {
            var tours = await _db.Tours
                .Include(t => t.Employee)
                .Where(t => t.Status == TourStatus.Open)
                .OrderByDescending(t => t.CreatedOn)
                .Select(t => new OpenTourRowVm
                {
                    TourId = t.Id,
                    TourNo = t.TourNo,
                    EmployeeName = t.Employee!.Name,
                    EmpCode = t.Employee!.EmpCode,
                    StartDate = t.StartDate.ToString("dd/MM/yyyy"),
                    EndDate = t.EndDate.ToString("dd/MM/yyyy"),
                    AmountApprovalDate = t.AdvanceApprovalDate.HasValue ? t.AdvanceApprovalDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "",
                    Status = t.Status.ToString(),
                    AdvanceAmount = t.ApprovedTourAdvance
                })
                .ToListAsync();

            return Json(new { data = tours });
        }

        // ---------- Panel 2: Advance Against Travelling/Expenses (auto-calculated DR/CR) ----------
        [HttpGet]
        public async Task<IActionResult> LedgerBalances()
        {
            var balances = await _balanceService.GetAllEmployeeBalancesAsync();

            int sr = 1;
            var rows = balances.Select(b => new LedgerRowVm
            {
                SrNo = sr++,
                AnalysisCode = b.AnalysisCode,
                EmpCode = b.EmpCode,
                Name = b.Name,
                Dept = b.Department,
                RA = b.ReportingAuthority,
                Balance = b.Balance,
                DrCr = b.BalanceType.ToString(),
                Flagged = b.PendingDrCount > 5 // example business flag: too many missing DRs -> highlight
            }).ToList();

            return Json(new { data = rows });
        }

        // ---------- Panel 3: Employee-wise Tour/DR status search ----------
        [HttpGet]
        public async Task<IActionResult> EmployeeStatus(string employeeName)
        {
            if (string.IsNullOrWhiteSpace(employeeName))
                return Json(new { data = Array.Empty<object>() });

            var tours = await _db.Tours
                .Include(t => t.Employee)
                .Where(t => t.Employee!.Name.Contains(employeeName))
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();

            var rows = new List<EmployeeTourStatusRowVm>();
            foreach (var t in tours)
            {
                var submittedDates = await _db.DailyExpenseReports
                    .Where(d => d.TourId == t.Id && d.IsSubmitted)
                    .Select(d => d.ReportDate.Date)
                    .ToListAsync();

                var missing = new List<string>();
                for (var d = t.StartDate.Date; d <= t.EndDate.Date && d <= DateTime.Today; d = d.AddDays(1))
                {
                    if (!submittedDates.Contains(d)) missing.Add(d.ToString("yyyy-MM-dd"));
                }

                rows.Add(new EmployeeTourStatusRowVm
                {
                    TourOrDrNo = t.TourNo,
                    StartDate = t.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = t.EndDate.ToString("yyyy-MM-dd"),
                    RequestedTourAdvance = t.RequestedTourAdvance,
                    ApprovedTourAdvance = t.ApprovedTourAdvance,
                    TourAdvanceTaken = t.TourAdvanceTaken,
                    ExpenseAmount = t.ExpenseAmount == 0 ? null : t.ExpenseAmount,
                    Status = t.Status.ToString(),
                    MissingDrDates = missing,
                    Report = missing.Count == 0 ? "Dr Completed" : "On These Dates Dr Not Submitted"
                });
            }

            return Json(new { data = rows });
        }

        // ---------- Summary cards (Debit/Credit/Pending Expense/Pending DR at a glance) ----------
        [HttpGet]
        public async Task<IActionResult> Summary()
        {
            var balances = await _balanceService.GetAllEmployeeBalancesAsync();

            var vm = new DashboardSummaryVm
            {
                TotalDebitBalance = balances.Where(b => b.BalanceType == DrCr.DR).Sum(b => b.Balance),
                TotalCreditBalance = balances.Where(b => b.BalanceType == DrCr.CR).Sum(b => b.Balance),
                TotalPendingTourExpense = balances.Sum(b => b.PendingTourExpense),
                TotalPendingDrCount = balances.Sum(b => b.PendingDrCount),
                OpenTourCount = await _db.Tours.CountAsync(t => t.Status == TourStatus.Open)
            };

            return Json(vm);
        }
    }
}
```

---

## FILE: `src/TravelExpensePortal.Web/Data/ApplicationDbContext.cs`

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TravelExpensePortal.Web.Models;

namespace TravelExpensePortal.Web.Data
{
    public class ApplicationUser : IdentityUser
    {
        public int? EmployeeId { get; set; }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Designation> Designations => Set<Designation>();
        public DbSet<RateMaster> RateMasters => Set<RateMaster>();
        public DbSet<DsicRateSlab> DsicRateSlabs => Set<DsicRateSlab>();
        public DbSet<CityMaster> Cities => Set<CityMaster>();
        public DbSet<Tour> Tours => Set<Tour>();
        public DbSet<DailyExpenseReport> DailyExpenseReports => Set<DailyExpenseReport>();
        public DbSet<AdvanceLedgerEntry> AdvanceLedgerEntries => Set<AdvanceLedgerEntry>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Employee>()
                .HasOne(e => e.ReportingAuthority)
                .WithMany()
                .HasForeignKey(e => e.ReportingAuthorityId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Employee>()
                .HasIndex(e => e.EmpCode).IsUnique();

            builder.Entity<Tour>()
                .HasIndex(t => t.TourNo).IsUnique();

            builder.Entity<RateMaster>()
                .HasIndex(r => new { r.DesignationId, r.CityType, r.EffectiveFrom });

            builder.Entity<AdvanceLedgerEntry>()
                .HasOne(l => l.Employee).WithMany().HasForeignKey(l => l.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<AdvanceLedgerEntry>()
                .HasOne(l => l.Tour).WithMany().HasForeignKey(l => l.TourId).OnDelete(DeleteBehavior.SetNull);

            SeedStaticData(builder);
        }

        /// <summary>
        /// Seeds designation tiers + rate master directly from TIPL TE Rules (w.e.f. 1 Apr 2025).
        /// Ids are fixed so relationships can be seeded predictably.
        /// </summary>
        private static void SeedStaticData(ModelBuilder builder)
        {
            var designations = new[]
            {
                new Designation { Id = 1, Name = "Workmen", Rank = 1 },
                new Designation { Id = 2, Name = "Trainees/Jr. Executive/Executive/Jr. Tech. Assistant/Tech. Asst./Jr Engineer", Rank = 2 },
                new Designation { Id = 3, Name = "Sr. Executive/Asst. Team Lead/Asst. Engineer", Rank = 3 },
                new Designation { Id = 4, Name = "Team Lead/Sr. Team Lead/Engineer/Sr. Engineer", Rank = 4 },
                new Designation { Id = 5, Name = "Asst. Managers/Deputy Managers", Rank = 5 },
                new Designation { Id = 6, Name = "Managers/Sr. Managers", Rank = 6 },
                new Designation { Id = 7, Name = "AGM", Rank = 7 },
                new Designation { Id = 8, Name = "DGM", Rank = 8 },
                new Designation { Id = 9, Name = "GM", Rank = 9 },
                new Designation { Id = 10, Name = "Sr. GM & above", Rank = 10 },
                new Designation { Id = 11, Name = "Directors", Rank = 11 },
            };
            builder.Entity<Designation>().HasData(designations);

            // Rate table: (DesignationId, CityType, Lodging, Boarding, TravelClass)
            var rates = new List<RateMaster>();
            int id = 1;
            void Add(int desigId, CityType city, decimal lodging, decimal boarding, string travelClass)
            {
                rates.Add(new RateMaster
                {
                    Id = id++,
                    DesignationId = desigId,
                    CityType = city,
                    MaxLodgingPerDay = lodging,
                    BoardingPerDay = boarding,
                    TravelClass = travelClass,
                    EffectiveFrom = new DateTime(2025, 4, 1)
                });
            }

            Add(1, CityType.Metro, 550, 330, "Sleeper/Chair Car-2S or Non-AC bus");
            Add(1, CityType.StateCapital, 500, 305, "Sleeper/Chair Car-2S or Non-AC bus");
            Add(1, CityType.Other, 450, 305, "Sleeper/Chair Car-2S or Non-AC bus");

            Add(2, CityType.Metro, 900, 415, "III AC/AC Chair car or AC bus");
            Add(2, CityType.StateCapital, 800, 390, "III AC/AC Chair car or AC bus");
            Add(2, CityType.Other, 700, 390, "III AC/AC Chair car or AC bus");

            Add(3, CityType.Metro, 950, 475, "III AC/AC Chair car or AC bus/Volvo");
            Add(3, CityType.StateCapital, 850, 450, "III AC/AC Chair car or AC bus/Volvo");
            Add(3, CityType.Other, 750, 450, "III AC/AC Chair car or AC bus/Volvo");

            Add(4, CityType.Metro, 1050, 510, "III AC/AC Chair car or AC bus/Volvo");
            Add(4, CityType.StateCapital, 950, 485, "III AC/AC Chair car or AC bus/Volvo");
            Add(4, CityType.Other, 850, 485, "III AC/AC Chair car or AC bus/Volvo");

            Add(5, CityType.Metro, 1200, 550, "III AC/AC Chair car or AC bus/Volvo");
            Add(5, CityType.StateCapital, 1100, 525, "III AC/AC Chair car or AC bus/Volvo");
            Add(5, CityType.Other, 1000, 525, "III AC/AC Chair car or AC bus/Volvo");

            Add(6, CityType.Metro, 1350, 600, "II AC/AC Chair car");
            Add(6, CityType.StateCapital, 1250, 575, "II AC/AC Chair car or AC bus/Volvo");
            Add(6, CityType.Other, 1150, 575, "II AC/AC Chair car or AC bus/Volvo");

            Add(7, CityType.Metro, 1500, 700, "II AC/AC Chair car or AC bus/Volvo");
            Add(7, CityType.StateCapital, 1400, 675, "II AC/AC Chair car or AC bus/Volvo");
            Add(7, CityType.Other, 1300, 675, "II AC/AC Chair car or AC bus/Volvo");

            Add(8, CityType.Metro, 1600, 725, "II AC/AC Chair car or AC bus/Volvo/Flight");
            Add(8, CityType.StateCapital, 1500, 700, "II AC/AC Chair car or AC bus/Volvo/Flight");
            Add(8, CityType.Other, 1400, 700, "II AC/AC Chair car or AC bus/Volvo/Flight");

            Add(9, CityType.Metro, 1700, 850, "II AC/AC Chair car or AC bus/Volvo/Flight");
            Add(9, CityType.StateCapital, 1600, 825, "II AC/AC Chair car or AC bus/Volvo/Flight");
            Add(9, CityType.Other, 1500, 825, "II AC/AC Chair car or AC bus/Volvo/Flight");

            Add(10, CityType.Metro, 1800, 900, "II AC/AC Chair car or AC bus/Volvo/Flight/One Way Drop Ajmer-Jaipur");
            Add(10, CityType.StateCapital, 1700, 875, "II AC/AC Chair car or AC bus/Volvo/Flight/One Way Drop Ajmer-Jaipur");
            Add(10, CityType.Other, 1600, 875, "II AC/AC Chair car or AC bus/Volvo/Flight/One Way Drop Ajmer-Jaipur");

            // Directors: on actuals - store 0 as flag, handled specially in calc service
            Add(11, CityType.Metro, 0, 0, "As per requirement (On Actuals)");
            Add(11, CityType.StateCapital, 0, 0, "As per requirement (On Actuals)");
            Add(11, CityType.Other, 0, 0, "As per requirement (On Actuals)");

            builder.Entity<RateMaster>().HasData(rates);

            // DSIC Engineer day-range slabs (Revised applicable from 01.01.2023)
            builder.Entity<DsicRateSlab>().HasData(
                new DsicRateSlab { Id = 1, MinDay = 0, MaxDay = 5, MaxLodgingPerDay = 950, MaxConveyancePerDay = 0, ConveyanceAsPerActual = true },
                new DsicRateSlab { Id = 2, MinDay = 6, MaxDay = 12, MaxLodgingPerDay = 800, MaxConveyancePerDay = 300 },
                new DsicRateSlab { Id = 3, MinDay = 13, MaxDay = 25, MaxLodgingPerDay = 600, MaxConveyancePerDay = 300 },
                new DsicRateSlab { Id = 4, MinDay = 26, MaxDay = 30, MaxLodgingPerDay = 10000, MaxConveyancePerDay = 6000 }
            );

            // Metro cities per policy note 1
            var metros = new[] { "Mumbai", "Kolkata", "Chennai", "Delhi", "NCR", "Bangalore", "Hyderabad" };
            int cid = 1;
            var cities = new List<CityMaster>();
            foreach (var m in metros)
            {
                cities.Add(new CityMaster { Id = cid++, CityName = m, CityType = CityType.Metro, IsMumbai = m == "Mumbai" });
            }
            cities.Add(new CityMaster { Id = cid++, CityName = "Jaipur", CityType = CityType.StateCapital });
            cities.Add(new CityMaster { Id = cid++, CityName = "Ajmer", CityType = CityType.Other });
            builder.Entity<CityMaster>().HasData(cities);
        }
    }
}
```

---

## FILE: `src/TravelExpensePortal.Web/Data/DbSeeder.cs`

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TravelExpensePortal.Web.Models;

namespace TravelExpensePortal.Web.Data
{
    /// <summary>
    /// One-time demo/reference data loader - illustrates records shaped like the
    /// existing legacy screens (Open Tours / Advance Ledger / Employee status) so the
    /// new portal can be visually validated against them.
    /// Call DbSeeder.SeedAsync(app) once at startup in Development, or run manually.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await db.Database.MigrateAsync();

            foreach (var role in new[] { "Admin", "Accounts", "ReportingAuthority", "Employee" })
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            if (!await db.Departments.AnyAsync())
            {
                db.Departments.AddRange(
                    new Department { Name = "Production-Instruments" },
                    new Department { Name = "Management" },
                    new Department { Name = "Product" },
                    new Department { Name = "Service-DSIC" },
                    new Department { Name = "Research & Development" },
                    new Department { Name = "Sales-NBD" },
                    new Department { Name = "Pre Sales" }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.Employees.AnyAsync())
            {
                var deptLookup = await db.Departments.ToDictionaryAsync(d => d.Name, d => d.Id);

                var ra1 = new Employee { EmpCode = "E100000", Name = "Ajeet Kumar Gupta", DesignationId = 6, DepartmentId = deptLookup["Service-DSIC"], Gender = Gender.Male };
                var ra2 = new Employee { EmpCode = "E100001", Name = "Naveen Kumar Sharma", DesignationId = 6, DepartmentId = deptLookup["Product"], Gender = Gender.Male };
                db.Employees.AddRange(ra1, ra2);
                await db.SaveChangesAsync();

                var employees = new List<Employee>
                {
                    new() { EmpCode = "E100530", Name = "Anuj Ajmera", DesignationId = 3, DepartmentId = deptLookup["Product"], ReportingAuthorityId = ra2.Id, Gender = Gender.Male },
                    new() { EmpCode = "E101441", Name = "Mayank Pratap Verma", DesignationId = 2, DepartmentId = deptLookup["Service-DSIC"], ReportingAuthorityId = ra1.Id, Gender = Gender.Male },
                    new() { EmpCode = "E101260", Name = "Vineet Bansal", DesignationId = 2, DepartmentId = deptLookup["Product"], ReportingAuthorityId = ra2.Id, Gender = Gender.Male },
                    new() { EmpCode = "E101449", Name = "Golu Agrahari", DesignationId = 1, DepartmentId = deptLookup["Service-DSIC"], ReportingAuthorityId = ra1.Id, Gender = Gender.Male },
                    new() { EmpCode = "E101395", Name = "Rupesh Modak", DesignationId = 3, DepartmentId = deptLookup["Sales-NBD"], ReportingAuthorityId = ra2.Id, Gender = Gender.Male },
                };
                db.Employees.AddRange(employees);
                await db.SaveChangesAsync();

                var byCode = await db.Employees.ToDictionaryAsync(e => e.EmpCode, e => e.Id);

                var tours = new List<Tour>
                {
                    new() { TourNo = "TR/14209/26-27", EmployeeId = byCode["E101441"], StartDate = new DateTime(2026,7,1), EndDate = new DateTime(2026,7,3), ApprovedTourAdvance = 4000, TourAdvanceTaken = 4000, RequestedTourAdvance = 4000, AdvanceApprovalDate = new DateTime(2026,7,2,5,24,38), Status = TourStatus.Open },
                    new() { TourNo = "TR/14208/26-27", EmployeeId = byCode["E101260"], StartDate = new DateTime(2026,7,2), EndDate = new DateTime(2026,7,3), ApprovedTourAdvance = 3000, TourAdvanceTaken = 3000, RequestedTourAdvance = 3000, AdvanceApprovalDate = new DateTime(2026,7,2,10,8,26), Status = TourStatus.Open },
                    new() { TourNo = "TR/14207/26-27", EmployeeId = byCode["E100530"], StartDate = new DateTime(2026,7,1), EndDate = new DateTime(2026,7,6), ApprovedTourAdvance = 20000, TourAdvanceTaken = 20000, RequestedTourAdvance = 20000, AdvanceApprovalDate = new DateTime(2026,7,2,10,8,53), Status = TourStatus.Open },
                    new() { TourNo = "TR/14205/26-27", EmployeeId = byCode["E101449"], StartDate = new DateTime(2026,7,1), EndDate = new DateTime(2026,7,7), ApprovedTourAdvance = 6000, TourAdvanceTaken = 6000, RequestedTourAdvance = 6000, AdvanceApprovalDate = new DateTime(2026,7,1,10,35,20), Status = TourStatus.Open },

                    // Rupesh Modak history (matches Screenshot 3)
                    new() { TourNo = "TR/14003/26-27", EmployeeId = byCode["E101395"], StartDate = new DateTime(2026,4,20), EndDate = new DateTime(2026,4,24), RequestedTourAdvance = 8300, ApprovedTourAdvance = 8300, TourAdvanceTaken = 8300, ExpenseAmount = 7585, Status = TourStatus.PendingForDocumentReceived },
                    new() { TourNo = "TR/14150/26-27", EmployeeId = byCode["E101395"], StartDate = new DateTime(2026,6,16), EndDate = new DateTime(2026,6,19), RequestedTourAdvance = 9100, ApprovedTourAdvance = 9100, TourAdvanceTaken = 9100, ExpenseAmount = 6825, Status = TourStatus.ToBeAudited },
                    new() { TourNo = "TR/14176/26-27", EmployeeId = byCode["E101395"], StartDate = new DateTime(2026,6,22), EndDate = new DateTime(2026,6,27), RequestedTourAdvance = 9300, ApprovedTourAdvance = 9000, TourAdvanceTaken = 9000, ExpenseAmount = 10592, Status = TourStatus.ToBeAudited },
                    new() { TourNo = "TR/14196/26-27", EmployeeId = byCode["E101395"], StartDate = new DateTime(2026,6,30), EndDate = new DateTime(2026,7,3), RequestedTourAdvance = 7400, ApprovedTourAdvance = 7400, TourAdvanceTaken = 7400, Status = TourStatus.Open },
                };
                db.Tours.AddRange(tours);
                await db.SaveChangesAsync();

                // Post matching ledger (advance = Debit) entries so balances/panel-2 populate automatically
                foreach (var t in tours)
                {
                    db.AdvanceLedgerEntries.Add(new AdvanceLedgerEntry
                    {
                        EmployeeId = t.EmployeeId,
                        TourId = t.Id,
                        EntryType = LedgerEntryType.AdvanceDisbursed,
                        DebitAmount = t.TourAdvanceTaken,
                        TransactionDate = t.AdvanceApprovalDate ?? t.StartDate,
                        Remarks = $"Advance for {t.TourNo}"
                    });

                    if (t.ExpenseAmount > 0)
                    {
                        db.AdvanceLedgerEntries.Add(new AdvanceLedgerEntry
                        {
                            EmployeeId = t.EmployeeId,
                            TourId = t.Id,
                            EntryType = LedgerEntryType.TourExpenseSettled,
                            CreditAmount = t.ExpenseAmount,
                            TransactionDate = t.EndDate,
                            Remarks = $"Expense settled for {t.TourNo}"
                        });
                    }
                }
                await db.SaveChangesAsync();
            }
        }
    }
}
```

---

## FILE: `src/TravelExpensePortal.Web/Models/AdvanceLedgerEntry.cs`

```csharp
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelExpensePortal.Web.Models
{
    public enum LedgerEntryType
    {
        AdvanceDisbursed,      // Debit  (money given to employee)
        TourExpenseSettled,    // Credit (approved expense adjusted against advance)
        CashReturnedByEmployee,// Credit (excess advance returned in cash)
        AdHocAdjustment
    }

    public enum DrCr { DR, CR }

    /// <summary>
    /// Every advance disbursement / expense settlement / cash-return posts an entry here.
    /// Running balance per employee = Sum(Debits) - Sum(Credits).
    /// Balance > 0  => employee owes company (shown as "DR", matches Advance Against Travel/Expenses screen)
    /// Balance < 0  => company owes employee (shown as "CR")
    /// This removes ALL manual calculation - balance is always a live aggregate query.
    /// </summary>
    public class AdvanceLedgerEntry
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public int? TourId { get; set; }
        public Tour? Tour { get; set; }

        public LedgerEntryType EntryType { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal DebitAmount { get; set; } = 0; // advance given

        [Column(TypeName = "decimal(12,2)")]
        public decimal CreditAmount { get; set; } = 0; // expense approved / cash returned

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "nvarchar(300)")]
        public string? Remarks { get; set; }

        // Advances/credit balances are processed only Tue & Fri per policy - flag for the disbursement batch job
        public DateTime? ScheduledDisbursementDate { get; set; }
        public bool IsDisbursed { get; set; } = false;
    }

    /// <summary>
    /// Read-only projection used by the "Advance Against Travelling/Expenses" grid.
    /// Computed by BalanceCalculationService, never stored/hand-edited.
    /// </summary>
    public class EmployeeBalanceSummary
    {
        public int EmployeeId { get; set; }
        public string AnalysisCode { get; set; } = string.Empty; // = EmpCode
        public string EmpCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string ReportingAuthority { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public DrCr BalanceType { get; set; }
        public decimal PendingTourExpense { get; set; }
        public int PendingDrCount { get; set; }
    }
}
```

---

## FILE: `src/TravelExpensePortal.Web/Models/DashboardViewModels.cs`

```csharp
namespace TravelExpensePortal.Web.Models
{
    // ---- Panel 1: "View Open Tours" (Screenshot 1) ----
    public class OpenTourRowVm
    {
        public string TourNo { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string EmpCode { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string AmountApprovalDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal AdvanceAmount { get; set; }
        public int TourId { get; set; }
    }

    // ---- Panel 2: "Advance Against Travelling/Expenses" (Screenshot 2) ----
    public class LedgerRowVm
    {
        public int SrNo { get; set; }
        public string AnalysisCode { get; set; } = string.Empty;
        public string EmpCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Dept { get; set; } = string.Empty;
        public string RA { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string DrCr { get; set; } = string.Empty;
        public bool Flagged { get; set; } // highlight rows in red, like legacy screen (e.g. overdue)
    }

    // ---- Panel 3: Employee-wise Tour/DR status search (Screenshot 3) ----
    public class EmployeeTourStatusRowVm
    {
        public string TourOrDrNo { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public decimal RequestedTourAdvance { get; set; }
        public decimal ApprovedTourAdvance { get; set; }
        public decimal TourAdvanceTaken { get; set; }
        public decimal? ExpenseAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Report { get; set; } = string.Empty; // "Dr Completed" or list of missing dates
        public List<string> MissingDrDates { get; set; } = new();
    }

    // ---- Summary cards shown at top of the unified dashboard ----
    public class DashboardSummaryVm
    {
        public decimal TotalDebitBalance { get; set; }
        public decimal TotalCreditBalance { get; set; }
        public decimal TotalPendingTourExpense { get; set; }
        public int TotalPendingDrCount { get; set; }
        public int OpenTourCount { get; set; }
    }
}
```

---

## FILE: `src/TravelExpensePortal.Web/Models/Employee.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelExpensePortal.Web.Models
{
    public enum Gender { Male, Female, Other }
    public enum CityType { Metro, StateCapital, Other }

    public class Department
    {
        public int Id { get; set; }
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;
    }

    public class Designation
    {
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Ordering used for entitlement tier comparisons (1 = Workmen ... 11 = Directors)
        public int Rank { get; set; }

        public bool IsDsicEngineer { get; set; } = false;

        public ICollection<RateMaster> Rates { get; set; } = new List<RateMaster>();
    }

    public class Employee
    {
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string EmpCode { get; set; } = string.Empty; // e.g. E101441

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public int DesignationId { get; set; }
        public Designation? Designation { get; set; }

        public int DepartmentId { get; set; }
        public Department? Department { get; set; }

        // Reporting Authority (RA) - self referencing
        public int? ReportingAuthorityId { get; set; }
        public Employee? ReportingAuthority { get; set; }

        public Gender Gender { get; set; }

        [MaxLength(150)]
        public string? HomeCity { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;

        // ASP.NET Identity linkage
        public string? IdentityUserId { get; set; }
    }
}
```

---

## FILE: `src/TravelExpensePortal.Web/Models/RateMaster.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelExpensePortal.Web.Models
{
    /// <summary>
    /// Encodes the TIPL TE Rules rate table (Designation x City Type -> Lodging/Boarding limits).
    /// Editable from Admin UI so policy revisions (e.g. "w.e.f. 1 April 2025") don't need code changes.
    /// </summary>
    public class RateMaster
    {
        public int Id { get; set; }

        public int DesignationId { get; set; }
        public Designation? Designation { get; set; }

        public CityType CityType { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal MaxLodgingPerDay { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal BoardingPerDay { get; set; }

        [MaxLength(200)]
        public string TravelClass { get; set; } = string.Empty; // e.g. "III AC/AC Chair car"

        public bool BillsNeededForTravel { get; set; } = true;

        public DateTime EffectiveFrom { get; set; } = new DateTime(2025, 4, 1);
        public DateTime? EffectiveTo { get; set; }
    }

    /// <summary>
    /// DSIC Engineer slabs are day-range based rather than city based.
    /// </summary>
    public class DsicRateSlab
    {
        public int Id { get; set; }
        public int MinDay { get; set; }
        public int MaxDay { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal MaxLodgingPerDay { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal MaxConveyancePerDay { get; set; }

        public bool ConveyanceAsPerActual { get; set; } = false;
        public DateTime EffectiveFrom { get; set; } = new DateTime(2023, 1, 1);
    }

    /// <summary>
    /// City master used to classify a tour destination as Metro / State Capital / Other,
    /// and to flag Mumbai (extra lodging allowance).
    /// </summary>
    public class CityMaster
    {
        public int Id { get; set; }
        [Required, MaxLength(150)]
        public string CityName { get; set; } = string.Empty;
        public CityType CityType { get; set; }
        public bool IsMumbai { get; set; } = false;
        public bool IsForeign { get; set; } = false;
        [MaxLength(100)]
        public string? Country { get; set; }
        public bool IsSaarcOrChina { get; set; } = false;
    }
}
```

---

## FILE: `src/TravelExpensePortal.Web/Models/Tour.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelExpensePortal.Web.Models
{
    public enum TourStatus
    {
        Open,
        PendingForDocumentReceived,
        ToBeAudited,
        DrCompleted,
        Settled,
        Rejected
    }

    /// <summary>
    /// Represents one Tour record (TR/xxxxx/26-27) - matches "View Open Tours" screen.
    /// </summary>
    public class Tour
    {
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string TourNo { get; set; } = string.Empty; // e.g. TR/14209/26-27

        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int? DestinationCityId { get; set; }
        public CityMaster? DestinationCity { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal RequestedTourAdvance { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal ApprovedTourAdvance { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal TourAdvanceTaken { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal ExpenseAmount { get; set; } // Filled once TE bill / DR is submitted & totalled

        public DateTime? AdvanceApprovalDate { get; set; }

        public TourStatus Status { get; set; } = TourStatus.Open;

        public bool IsJointTour { get; set; } = false;
        public int? JointTourSeniorEmployeeId { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Daily Expense / DR (Daily Report) submission tracking - matches employee-wise tracker screen
    /// which shows "On These Dates DR Not Submitted".
    /// </summary>
    public class DailyExpenseReport
    {
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string DrNo { get; set; } = string.Empty; // e.g. DE/xxxxx/26-27

        public int TourId { get; set; }
        public Tour? Tour { get; set; }

        public DateTime ReportDate { get; set; }

        public bool IsSubmitted { get; set; } = false;
        public DateTime? SubmittedOn { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }
    }
}
```

---

## FILE: `src/TravelExpensePortal.Web/Program.cs`

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TravelExpensePortal.Web.Data;
using TravelExpensePortal.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- Database ----
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---- Identity (simple username/password auth) ----
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// ---- Application services (calculation engines) ----
builder.Services.AddScoped<IBalanceCalculationService, BalanceCalculationService>();
builder.Services.AddScoped<ITeRateCalculationService, TeRateCalculationService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
```

---

## FILE: `src/TravelExpensePortal.Web/Services/BalanceCalculationService.cs`

```csharp
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

                result.Add(new EmployeeBalanceSummary
                {
                    EmployeeId = emp.Id,
                    AnalysisCode = emp.EmpCode,
                    EmpCode = emp.EmpCode,
                    Name = emp.Name,
                    Department = emp.Department?.Name ?? "",
                    ReportingAuthority = emp.ReportingAuthority?.Name ?? "",
                    Balance = Math.Abs(bal),
                    BalanceType = bal >= 0 ? DrCr.DR : DrCr.CR,
                    PendingTourExpense = await GetPendingTourExpenseAsync(emp.Id),
                    PendingDrCount = await GetPendingDrCountAsync(emp.Id)
                });
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

            var debit = await _db.AdvanceLedgerEntries.Where(l => l.EmployeeId == employeeId).SumAsync(l => l.DebitAmount);
            var credit = await _db.AdvanceLedgerEntries.Where(l => l.EmployeeId == employeeId).SumAsync(l => l.CreditAmount);
            var bal = debit - credit;

            return new EmployeeBalanceSummary
            {
                EmployeeId = emp.Id,
                AnalysisCode = emp.EmpCode,
                EmpCode = emp.EmpCode,
                Name = emp.Name,
                Department = emp.Department?.Name ?? "",
                ReportingAuthority = emp.ReportingAuthority?.Name ?? "",
                Balance = Math.Abs(bal),
                BalanceType = bal >= 0 ? DrCr.DR : DrCr.CR,
                PendingTourExpense = await GetPendingTourExpenseAsync(employeeId),
                PendingDrCount = await GetPendingDrCountAsync(employeeId)
            };
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
        /// </summary>
        public async Task<int> GetPendingDrCountAsync(int employeeId)
        {
            var tours = await _db.Tours
                .Where(t => t.EmployeeId == employeeId && t.Status == TourStatus.Open)
                .ToListAsync();

            int pending = 0;
            foreach (var tour in tours)
            {
                var submittedDates = await _db.DailyExpenseReports
                    .Where(d => d.TourId == tour.Id && d.IsSubmitted)
                    .Select(d => d.ReportDate.Date)
                    .ToListAsync();

                for (var d = tour.StartDate.Date; d <= tour.EndDate.Date; d = d.AddDays(1))
                {
                    if (!submittedDates.Contains(d)) pending++;
                }
            }
            return pending;
        }
    }
}
```

---

## FILE: `src/TravelExpensePortal.Web/Services/TeRateCalculationService.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using TravelExpensePortal.Web.Data;
using TravelExpensePortal.Web.Models;

namespace TravelExpensePortal.Web.Services
{
    public class BoardingSplit
    {
        public decimal Breakfast { get; set; }
        public decimal Lunch { get; set; }
        public decimal Dinner { get; set; }
        public decimal Miscellaneous { get; set; }
        public decimal Total => Breakfast + Lunch + Dinner + Miscellaneous;
    }

    public class LodgingResult
    {
        public decimal EntitledAmount { get; set; }
        public string Basis { get; set; } = string.Empty; // explains how the figure was derived
    }

    public interface ITeRateCalculationService
    {
        Task<LodgingResult> CalculateLodgingAsync(int employeeId, CityType cityType, bool isMumbai, bool stayedInHotel,
            bool hasHotelBill, decimal hotelHalt_Hours, bool isJointTourSenior, decimal? actualBillAmount);

        BoardingSplit CalculateBoarding(decimal totalBoardingEntitlement, DateTime tourStart, DateTime tourEnd,
            bool breakfastByCustomer, bool lunchByCustomer, bool dinnerByCustomer);

        Task<decimal> GetBoardingEntitlementPerDayAsync(int employeeId, CityType cityType);
    }

    /// <summary>
    /// Implements the auto-calculation rules straight from TIPL TE Rules (w.e.f. 1 Apr 2025)
    /// so no employee/auditor ever has to hand-calculate lodging/boarding entitlement.
    /// </summary>
    public class TeRateCalculationService : ITeRateCalculationService
    {
        private readonly ApplicationDbContext _db;
        public TeRateCalculationService(ApplicationDbContext db) => _db = db;

        public async Task<LodgingResult> CalculateLodgingAsync(int employeeId, CityType cityType, bool isMumbai,
            bool stayedInHotel, bool hasHotelBill, decimal haltHours, bool isJointTourSenior, decimal? actualBillAmount)
        {
            var emp = await _db.Employees.Include(e => e.Designation).FirstOrDefaultAsync(e => e.Id == employeeId)
                       ?? throw new InvalidOperationException("Employee not found");

            var rate = await _db.RateMasters
                .Where(r => r.DesignationId == emp.DesignationId && r.CityType == cityType)
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (rate is null)
                return new LodgingResult { EntitledAmount = 0, Basis = "No rate configured for this designation/city" };

            // Directors: On actuals
            if (emp.Designation!.Name.Contains("Directors"))
            {
                return new LodgingResult
                {
                    EntitledAmount = actualBillAmount ?? 0,
                    Basis = "Director - reimbursed on actuals"
                };
            }

            decimal max = rate.MaxLodgingPerDay;

            // Rule: not staying overnight in hotel -> refreshing/minimum lodging rules apply instead
            if (!stayedInHotel)
            {
                if (haltHours > 4)
                {
                    if (hasHotelBill)
                        return new LodgingResult { EntitledAmount = max * 0.5m, Basis = "Halt > 4 hrs, hotel receipt produced: 50% of max lodging" };

                    var refreshing = emp.Designation.Rank >= 3 ? 250m : 150m; // Sr Supervisor & above = 250
                    return new LodgingResult { EntitledAmount = refreshing, Basis = "Refreshing charges (no overnight stay)" };
                }

                // Minimum lodging: 40% of max, capped at Rs 400
                var minLodging = Math.Min(max * 0.4m, 400m);
                return new LodgingResult { EntitledAmount = minLodging, Basis = "Minimum lodging: 40% of max lodging, capped at Rs 400" };
            }

            // Overnight hotel stay: pay MAX amount irrespective of actual bill (per policy note 1),
            // unless accommodation was free/paid by customer (handled by caller not calling this method).
            decimal entitlement = max;
            var basisNotes = new List<string> { "Full max lodging (paid irrespective of actual bill per policy)" };

            if (isMumbai)
            {
                entitlement += 200;
                basisNotes.Add("+Rs 200 Mumbai lodging allowance");
            }

            if (emp.Gender == Gender.Female)
            {
                entitlement += 200;
                basisNotes.Add("+Rs 200 female employee allowance");
            }

            if (isJointTourSenior)
            {
                entitlement *= 1.3m;
                basisNotes.Add("x1.3 joint tour (claimed only by senior employee)");
            }

            return new LodgingResult { EntitledAmount = Math.Round(entitlement, 2), Basis = string.Join("; ", basisNotes) };
        }

        public async Task<decimal> GetBoardingEntitlementPerDayAsync(int employeeId, CityType cityType)
        {
            var emp = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId)
                      ?? throw new InvalidOperationException("Employee not found");

            var rate = await _db.RateMasters
                .Where(r => r.DesignationId == emp.DesignationId && r.CityType == cityType)
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync();

            return rate?.BoardingPerDay ?? 0;
        }

        /// <summary>
        /// Splits the day's boarding entitlement into Breakfast(30%)/Lunch(30%)/Dinner(30%)/Misc(10%)
        /// based on whether the tour window covers each meal's applicable time band, and whether
        /// the customer already provided that meal.
        /// </summary>
        public BoardingSplit CalculateBoarding(decimal totalBoardingEntitlement, DateTime tourStart, DateTime tourEnd,
            bool breakfastByCustomer, bool lunchByCustomer, bool dinnerByCustomer)
        {
            bool CoversWindow(TimeSpan winStart, TimeSpan winEnd)
            {
                // true if the [tourStart.Time, tourEnd.Time] window overlaps the meal window on any day of tour
                var s = tourStart.TimeOfDay;
                var e = tourEnd.TimeOfDay;
                // Simplification: applicable if tour was ongoing (across any day) during the meal window
                return tourStart.Date < tourEnd.Date || (s <= winEnd && e >= winStart);
            }

            var result = new BoardingSplit
            {
                Miscellaneous = totalBoardingEntitlement * 0.10m // always applicable
            };

            bool breakfastApplicable = CoversWindow(new TimeSpan(8, 0, 0), new TimeSpan(10, 0, 0));
            bool lunchApplicable = CoversWindow(new TimeSpan(12, 0, 0), new TimeSpan(14, 0, 0));
            bool dinnerApplicable = CoversWindow(new TimeSpan(19, 0, 0), new TimeSpan(21, 0, 0));

            // "All three meals by customer" -> total boarding capped at Rs 100 (policy 5a)
            if (breakfastByCustomer && lunchByCustomer && dinnerByCustomer)
            {
                return new BoardingSplit { Breakfast = 0, Lunch = 0, Dinner = 0, Miscellaneous = 100m };
            }

            if (breakfastApplicable && !breakfastByCustomer) result.Breakfast = totalBoardingEntitlement * 0.30m;
            if (lunchApplicable && !lunchByCustomer) result.Lunch = totalBoardingEntitlement * 0.30m;
            if (dinnerApplicable && !dinnerByCustomer) result.Dinner = totalBoardingEntitlement * 0.30m;

            return result;
        }
    }
}
```

---

## FILE: `src/TravelExpensePortal.Web/TravelExpensePortal.Web.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>travel-expense-portal-secrets</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.8" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.8" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.8">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.8">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

---

## FILE: `src/TravelExpensePortal.Web/Views/Account/Login.cshtml`

```html
@model TravelExpensePortal.Web.Controllers.LoginVm
@{
    ViewData["Title"] = "Login";
    Layout = null;
}
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <title>Login - TIPL Travel Expense Portal</title>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/twitter-bootstrap/5.3.3/css/bootstrap.min.css" />
</head>
<body class="bg-light">
    <div class="d-flex align-items-center justify-content-center vh-100">
        <div class="card shadow-sm" style="width: 380px;">
            <div class="card-body p-4">
                <h4 class="text-center mb-3"><i class="fa-solid fa-plane-departure"></i> TIPL Travel Expense Portal</h4>
                <form asp-action="Login" method="post">
                    <input type="hidden" name="returnUrl" value="@ViewData["ReturnUrl"]" />
                    <div asp-validation-summary="All" class="text-danger small"></div>
                    <div class="mb-3">
                        <label class="form-label">Username / Emp Code</label>
                        <input asp-for="UserName" class="form-control" />
                    </div>
                    <div class="mb-3">
                        <label class="form-label">Password</label>
                        <input asp-for="Password" type="password" class="form-control" />
                    </div>
                    <button type="submit" class="btn btn-primary w-100">Login</button>
                </form>
            </div>
        </div>
    </div>
</body>
</html>
```

---

## FILE: `src/TravelExpensePortal.Web/Views/Dashboard/Index.cshtml`

```html
@{
    ViewData["Title"] = "Travel Dashboard";
}

<!-- Summary Cards: auto-calculated, zero manual entry -->
<div class="row g-3 mb-4" id="summaryCards">
    <div class="col-6 col-md-3">
        <div class="card border-0 shadow-sm h-100">
            <div class="card-body">
                <div class="text-muted small">Total Debit Balance (DR)</div>
                <div class="fs-4 fw-bold text-danger" id="cardDebit">₹0</div>
            </div>
        </div>
    </div>
    <div class="col-6 col-md-3">
        <div class="card border-0 shadow-sm h-100">
            <div class="card-body">
                <div class="text-muted small">Total Credit Balance (CR)</div>
                <div class="fs-4 fw-bold text-success" id="cardCredit">₹0</div>
            </div>
        </div>
    </div>
    <div class="col-6 col-md-3">
        <div class="card border-0 shadow-sm h-100">
            <div class="card-body">
                <div class="text-muted small">Pending Tour Expense</div>
                <div class="fs-4 fw-bold text-warning" id="cardPendingExpense">₹0</div>
            </div>
        </div>
    </div>
    <div class="col-6 col-md-3">
        <div class="card border-0 shadow-sm h-100">
            <div class="card-body">
                <div class="text-muted small">Pending DR / Open Tours</div>
                <div class="fs-4 fw-bold text-info" id="cardPendingDr">0 / 0</div>
            </div>
        </div>
    </div>
</div>

<div class="card border-0 shadow-sm">
    <div class="card-header bg-white">
        <ul class="nav nav-tabs card-header-tabs" id="dashboardTabs" role="tablist">
            <li class="nav-item" role="presentation">
                <button class="nav-link active" id="tab-open-tours-btn" data-bs-toggle="tab" data-bs-target="#tab-open-tours" type="button">
                    <i class="fa-solid fa-route me-1"></i> View Open Tours
                </button>
            </li>
            <li class="nav-item" role="presentation">
                <button class="nav-link" id="tab-ledger-btn" data-bs-toggle="tab" data-bs-target="#tab-ledger" type="button">
                    <i class="fa-solid fa-scale-balanced me-1"></i> Advance Against Travelling/Expenses
                </button>
            </li>
            <li class="nav-item" role="presentation">
                <button class="nav-link" id="tab-emp-status-btn" data-bs-toggle="tab" data-bs-target="#tab-emp-status" type="button">
                    <i class="fa-solid fa-magnifying-glass me-1"></i> Employee Tour/DR Status
                </button>
            </li>
        </ul>
    </div>
    <div class="card-body">
        <div class="tab-content">

            <!-- PANEL 1: Open Tours (Screenshot 1) -->
            <div class="tab-pane fade show active" id="tab-open-tours">
                <table id="openToursTable" class="table table-striped table-hover table-bordered w-100">
                    <thead class="table-light">
                        <tr>
                            <th>Tour No.</th>
                            <th>Employee Name</th>
                            <th>Emp Code</th>
                            <th>Start Date</th>
                            <th>End Date</th>
                            <th>Amount Approval Date</th>
                            <th>Status</th>
                            <th>Adv. Amount</th>
                            <th>Advance Detail</th>
                        </tr>
                    </thead>
                    <tbody></tbody>
                </table>
            </div>

            <!-- PANEL 2: Advance Against Travelling/Expenses ledger (Screenshot 2) -->
            <div class="tab-pane fade" id="tab-ledger">
                <table id="ledgerTable" class="table table-striped table-hover table-bordered w-100">
                    <thead class="table-light">
                        <tr>
                            <th>Sr.No.</th>
                            <th>Analysis Code</th>
                            <th>Emp Code</th>
                            <th>Name</th>
                            <th>Dept</th>
                            <th>RA</th>
                            <th>Balance</th>
                            <th>DR/CR</th>
                        </tr>
                    </thead>
                    <tbody></tbody>
                </table>
            </div>

            <!-- PANEL 3: Employee-wise Tour/DR status search (Screenshot 3) -->
            <div class="tab-pane fade" id="tab-emp-status">
                <div class="row mb-3">
                    <div class="col-md-4">
                        <label class="form-label fw-semibold">Employee Name:</label>
                        <div class="input-group">
                            <input type="text" id="empNameInput" class="form-control" placeholder="e.g. Rupesh Modak" />
                            <button class="btn btn-primary" id="empSearchBtn">Go</button>
                        </div>
                    </div>
                </div>
                <table id="empStatusTable" class="table table-striped table-hover table-bordered w-100">
                    <thead class="table-light">
                        <tr>
                            <th>Tour/Daily Exp No.</th>
                            <th>Start Date</th>
                            <th>End Date</th>
                            <th>Requested Tour Advance</th>
                            <th>Approved Tour Advance</th>
                            <th>Tour Advance Taken</th>
                            <th>Expense Amount</th>
                            <th>Status</th>
                            <th>Report</th>
                        </tr>
                    </thead>
                    <tbody></tbody>
                </table>
            </div>

        </div>
    </div>
</div>

@section Scripts {
    <script src="~/js/dashboard.js"></script>
}
```

---

## FILE: `src/TravelExpensePortal.Web/Views/Shared/_Layout.cshtml`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>@ViewData["Title"] - TIPL Travel Expense Portal</title>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/twitter-bootstrap/5.3.3/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/datatables/1.13.8/css/dataTables.bootstrap5.min.css" />
    <link rel="stylesheet" href="~/css/site.css" />
</head>
<body>
    <nav class="navbar navbar-expand-lg navbar-dark bg-primary shadow-sm">
        <div class="container-fluid">
            <a class="navbar-brand fw-bold" href="/Dashboard/Index"><i class="fa-solid fa-plane-departure me-2"></i>TIPL Travel Expense Portal</a>
            <div class="d-flex align-items-center text-white">
                <span class="me-3 small">@(User?.Identity?.Name ?? "Guest")</span>
                <form asp-controller="Account" asp-action="Logout" method="post" class="d-inline">
                    <button class="btn btn-sm btn-outline-light" type="submit"><i class="fa-solid fa-right-from-bracket"></i> Logout</button>
                </form>
            </div>
        </div>
    </nav>

    <div class="container-fluid py-4">
        @RenderBody()
    </div>

    <footer class="text-center text-muted small py-3">
        &copy; @DateTime.Now.Year TIPL - Travel Expense &amp; Advance Management Portal
    </footer>

    <script src="https://cdnjs.cloudflare.com/ajax/libs/jquery/3.7.1/jquery.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/twitter-bootstrap/5.3.3/js/bootstrap.bundle.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/datatables/1.13.8/js/jquery.dataTables.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/datatables/1.13.8/js/dataTables.bootstrap5.min.js"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

---

## FILE: `src/TravelExpensePortal.Web/Views/_ViewImports.cshtml`

```html
@using TravelExpensePortal.Web
@using TravelExpensePortal.Web.Models
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

---

## FILE: `src/TravelExpensePortal.Web/Views/_ViewStart.cshtml`

```html
@{
    Layout = "_Layout";
}
```

---

## FILE: `src/TravelExpensePortal.Web/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TravelExpensePortalDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## FILE: `src/TravelExpensePortal.Web/wwwroot/css/site.css`

```css
body {
    background-color: #f4f6f9;
    font-family: 'Segoe UI', Roboto, Arial, sans-serif;
}

.card {
    border-radius: 0.6rem;
}

.nav-tabs .nav-link.active {
    font-weight: 600;
    border-bottom: 3px solid #0d6efd;
}

table.dataTable thead th {
    white-space: nowrap;
}

.badge {
    font-size: 0.8rem;
}
```

---

## FILE: `src/TravelExpensePortal.Web/wwwroot/js/dashboard.js`

```javascript
$(function () {
    const fmtMoney = (v) => '₹' + Number(v || 0).toLocaleString('en-IN', { minimumFractionDigits: 2 });

    // ---------- Summary Cards ----------
    function loadSummary() {
        $.getJSON('/Dashboard/Summary', function (data) {
            $('#cardDebit').text(fmtMoney(data.totalDebitBalance));
            $('#cardCredit').text(fmtMoney(data.totalCreditBalance));
            $('#cardPendingExpense').text(fmtMoney(data.totalPendingTourExpense));
            $('#cardPendingDr').text(data.totalPendingDrCount + ' / ' + data.openTourCount);
        });
    }

    // ---------- Panel 1: Open Tours ----------
    const openToursTable = $('#openToursTable').DataTable({
        ajax: { url: '/Dashboard/OpenTours', dataSrc: 'data' },
        columns: [
            { data: 'tourNo', render: (d, t, row) => `<a href="/Tour/Details/${row.tourId}">${d}</a>` },
            { data: 'employeeName' },
            { data: 'empCode' },
            { data: 'startDate' },
            { data: 'endDate' },
            { data: 'amountApprovalDate' },
            { data: 'status' },
            { data: 'advanceAmount', render: (d) => fmtMoney(d) },
            {
                data: null, orderable: false,
                render: (d, t, row) => `<a href="/Tour/AdvanceDetail/${row.tourId}" class="text-warning"><i class="fa-solid fa-pen-to-square"></i></a>`
            }
        ],
        order: [[0, 'desc']],
        pageLength: 10
    });

    // ---------- Panel 2: Advance Against Travelling/Expenses (auto DR/CR) ----------
    const ledgerTable = $('#ledgerTable').DataTable({
        ajax: { url: '/Dashboard/LedgerBalances', dataSrc: 'data' },
        columns: [
            { data: 'srNo' },
            { data: 'analysisCode' },
            { data: 'empCode' },
            { data: 'name' },
            { data: 'dept' },
            { data: 'ra' },
            { data: 'balance', render: (d) => Number(d).toLocaleString('en-IN', { minimumFractionDigits: 2 }) },
            {
                data: 'drCr',
                render: (d) => d === 'DR'
                    ? '<span class="badge bg-danger">DR</span>'
                    : '<span class="badge bg-success">CR</span>'
            }
        ],
        createdRow: function (row, data) {
            if (data.flagged) $(row).addClass('table-danger');
        },
        pageLength: 15
    });

    // ---------- Panel 3: Employee-wise Tour/DR status search ----------
    let empStatusTable = $('#empStatusTable').DataTable({
        data: [],
        columns: [
            { data: 'tourOrDrNo', render: (d) => `<a href="#">${d}</a>` },
            { data: 'startDate' },
            { data: 'endDate' },
            { data: 'requestedTourAdvance', render: (d) => fmtMoney(d) },
            { data: 'approvedTourAdvance', render: (d) => fmtMoney(d) },
            { data: 'tourAdvanceTaken', render: (d) => fmtMoney(d) },
            { data: 'expenseAmount', render: (d) => d ? fmtMoney(d) : '' },
            { data: 'status' },
            {
                data: null,
                render: (d, t, row) => {
                    if (row.missingDrDates && row.missingDrDates.length > 0) {
                        return `${row.missingDrDates.join(' ')}<br><span class="text-danger fw-semibold">On These Dates Dr Not Submitted</span>`;
                    }
                    return `<span class="text-success">Dr Completed</span>`;
                }
            }
        ]
    });

    function searchEmployee() {
        const name = $('#empNameInput').val().trim();
        if (!name) return;
        $.getJSON('/Dashboard/EmployeeStatus', { employeeName: name }, function (resp) {
            empStatusTable.clear();
            empStatusTable.rows.add(resp.data);
            empStatusTable.draw();
        });
    }

    $('#empSearchBtn').on('click', searchEmployee);
    $('#empNameInput').on('keypress', function (e) {
        if (e.which === 13) searchEmployee();
    });

    loadSummary();
    // Refresh summary + ledger periodically so balances always reflect the latest ledger entries
    setInterval(loadSummary, 60000);
});
```

---
