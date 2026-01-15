# BudgetManager

A comprehensive .NET 8 ASP.NET Core MVC application for personal budget management with transaction tracking, budget planning, and financial reporting.

## Features

### Core Features
- **Transaction Management**: Full CRUD operations for income and expenses
- **Category Management**: Organize transactions by customizable categories
- **Account Tracking**: Support for multiple accounts (checking, savings, credit cards)
- **Monthly Budgets**: Set and track spending limits per category
- **Budget Pacing**: Real-time tracking with "Safe to Spend Today" calculations

### Import & Automation
- **CSV Import**: Bulk import transactions from bank exports
  - Duplicate detection using (Date, Amount, Description, Account)
  - Preview before confirmation
  - Auto-categorization using rules
- **Categorization Rules**: Priority-based rules to auto-categorize by description text

### Reporting
- **Budget vs Actual**: Visual comparison with charts and tables
- **Month over Month**: Track spending trends across the year
- **Top Expenses**: Identify largest spending items

### Data Protection
- **Lock Month**: Prevent editing/deleting transactions in closed months
- **Adjustment Transactions**: Special transactions that bypass month locks
- **Activity Log**: Complete audit trail of all changes

## Technology Stack

- **.NET 8** with ASP.NET Core MVC
- **Entity Framework Core 8** with SQLite (cross-platform compatible)
- **ASP.NET Core Identity** for authentication
- **Bootstrap 5** with responsive design
- **Chart.js** for data visualization
- **xUnit** for testing

## Getting Started

### Prerequisites
- .NET 8 SDK
- No database server required (uses SQLite file-based database)

### Local Development Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/BudgetManager.git
   cd BudgetManager
   ```

2. Restore packages:
   ```bash
   dotnet restore
   ```

3. Run the application:
   ```bash
   cd src/BudgetManager.Web
   dotnet run
   ```

4. The database will be created automatically on first run with migrations applied and seed data loaded.

5. Open http://localhost:5000 (or the port shown in the console) in your browser

**Note:** The application uses SQLite, which creates a local database file (`BudgetManager.db`) in the project directory. This file is automatically created and does not require any additional setup. The database file is excluded from version control via `.gitignore`.

### Default Demo User
- **Email**: demo@budgetmanager.com
- **Password**: Demo123!

## Project Structure

```
BudgetManager/
├── src/
│   └── BudgetManager.Web/
│       ├── Controllers/          # MVC Controllers
│       ├── Data/                 # DbContext & Seed Data
│       ├── Models/               # EF Core Entities
│       ├── Services/             # Business Logic Layer
│       │   └── Interfaces/       # Service Interfaces
│       ├── ViewModels/           # View Models
│       ├── Views/                # Razor Views
│       └── wwwroot/              # Static Files
├── tests/
│   └── BudgetManager.Tests/      # xUnit Tests
├── sql/                          # SQL Scripts
│   ├── vw_MonthlyCategorySummary.sql
│   └── sp_LockMonth.sql
├── sample-data/
│   └── sample-transactions.csv   # Sample Import File
└── README.md
```

## CSV Import Format

### Required Columns
| Column | Format | Required | Description |
|--------|--------|----------|-------------|
| Date | MM/DD/YYYY or YYYY-MM-DD | Yes | Transaction date |
| Description | Text | Yes | Transaction description |
| Amount | Decimal | Yes | Negative for expenses, positive for income |
| Category | Text | No | Category name (must match existing) |

### Example CSV
```csv
Date,Description,Amount,Category
01/15/2024,Walmart Groceries,-85.42,Groceries
01/16/2024,Direct Deposit,2500.00,Income
01/17/2024,Netflix Subscription,-15.99,Subscriptions
```

### Duplicate Detection
Transactions are considered duplicates when they have the same:
- Date
- Rounded amount (to 2 decimal places)
- Normalized description (lowercase, extra spaces removed)
- Account

## Lock Month Behavior

### What Locking Does
- Prevents editing non-adjustment transactions in the locked month
- Prevents deleting non-adjustment transactions in the locked month
- Prevents importing new transactions to the locked month

### What's Still Allowed
- **Adjustment transactions**: Can be created, edited, and deleted even in locked months
- Viewing all transactions
- Running reports
- Managing budgets for the locked month

### Typical Workflow
1. Complete all transactions for the month
2. Review and categorize everything
3. Lock the month to finalize
4. Use adjustments for any corrections needed

## Deployment to Azure

### Azure App Service + Azure SQL

1. Create resources in Azure:
   ```bash
   # Create resource group
   az group create --name BudgetManagerRG --location eastus

   # Create App Service Plan
   az appservice plan create --name BudgetManagerPlan --resource-group BudgetManagerRG --sku B1 --is-linux

   # Create Web App
   az webapp create --resource-group BudgetManagerRG --plan BudgetManagerPlan --name your-budget-app --runtime "DOTNET|8.0"

   # Create Azure SQL Server
   az sql server create --name budgetmanager-sql --resource-group BudgetManagerRG --location eastus --admin-user sqladmin --admin-password YourStrong@Passw0rd

   # Create Database
   az sql db create --resource-group BudgetManagerRG --server budgetmanager-sql --name BudgetManagerDB --service-objective Basic
   ```

2. Configure connection string:
   ```bash
   az webapp config connection-string set --resource-group BudgetManagerRG --name your-budget-app --settings Default="Server=tcp:budgetmanager-sql.database.windows.net,1433;Database=BudgetManagerDB;User ID=sqladmin;Password=YourStrong@Passw0rd;Encrypt=True;TrustServerCertificate=False" --connection-string-type SQLAzure
   ```

3. Deploy:
   ```bash
   dotnet publish src/BudgetManager.Web -c Release -o ./publish
   cd publish
   zip -r ../deploy.zip .
   az webapp deployment source config-zip --resource-group BudgetManagerRG --name your-budget-app --src ../deploy.zip
   ```

### Environment Variables

Set the connection string via environment variable:
```bash
ConnectionStrings__Default="Server=...;Database=...;..."
```

## Running Tests

```bash
cd tests/BudgetManager.Tests
dotnet test
```

### Test Coverage
- **Locking Service**: Month locking, edit/delete prevention, adjustment bypass
- **Budget Service**: Budget status calculations (OK/WATCH/OVER), pacing
- **Rule Service**: Priority-based categorization, case-insensitive matching
- **Import Service**: Duplicate detection, description normalization

## SQL Scripts

### View: vw_MonthlyCategorySummary
Provides a comprehensive view of budget vs actual spending per category:
- Budget amount
- Actual spent
- Remaining
- Expected to date (prorated)
- Status (OK/WATCH/OVER)

### Stored Procedure: sp_LockMonth
Atomically locks a month with:
- Validation of year/month parameters
- Duplicate lock prevention
- Activity log entry
- Transaction safety

To deploy SQL scripts:
```sql
-- Run from SQL Server Management Studio or Azure Data Studio
:r sql/vw_MonthlyCategorySummary.sql
:r sql/sp_LockMonth.sql
```

## Development Notes

For details about the development process and challenges overcome during the project, including the migration from SQL Server to SQLite for cross-platform compatibility, see [DEVELOPMENT_SUMMARY.md](DEVELOPMENT_SUMMARY.md).

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Bootstrap for the UI framework
- Chart.js for data visualization
- CsvHelper for CSV parsing
