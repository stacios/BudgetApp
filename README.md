# BudgetManager

A comprehensive .NET 8 ASP.NET Core MVC application for personal budget management with transaction tracking, budget planning, and financial reporting. Features a modern UI with dark mode, animated visualizations, and interactive dashboards.

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

### Reporting & Visualizations
- **Budget vs Actual**: Visual comparison with charts and tables
- **Month over Month**: Track spending trends across the year
- **Top Expenses**: Identify largest spending items
- **Interactive Dashboard**: Animated donut charts, daily spending trends, spending calendar heatmap, cash flow Sankey diagrams

### Data Protection
- **Lock Month**: Prevent editing/deleting transactions in closed months
- **Adjustment Transactions**: Special transactions that bypass month locks
- **Activity Log**: Complete audit trail of all changes

### UI/UX
- **Dark Mode**: Toggle between light and dark themes
- **Animated Counters**: Summary cards with number animations
- **Gradient Cards**: Modern design with hover effects
- **Responsive Design**: Works on desktop, tablet, and mobile

## Technology Stack

- **.NET 8** with ASP.NET Core MVC
- **Entity Framework Core 8** with SQLite (cross-platform)
- **ASP.NET Core Identity** for authentication
- **Bootstrap 5.3** with responsive design
- **Chart.js 4.4** for charts and visualizations
- **Google Charts** for Sankey diagrams
- **xUnit** with Moq for testing

## Getting Started

### Prerequisites
- .NET 8 SDK
- No database server required (uses SQLite)

### Local Development

1. Clone the repository:
   ```bash
   git clone https://github.com/stacios/BudgetApp.git
   cd BudgetApp
   ```

2. Run the application:
   ```bash
   cd src/BudgetManager.Web
   dotnet run
   ```

3. Open http://localhost:5000 in your browser

The database is created automatically with seed data on first run.

### Default Demo User
- **Email**: demo@budgetmanager.com
- **Password**: Demo123!

## Project Structure

```
BudgetManager/
├── src/
│   └── BudgetManager.Web/
│       ├── Controllers/          # MVC Controllers
│       ├── Data/                 # DbContext & Migrations
│       ├── Models/               # EF Core Entities
│       ├── Services/             # Business Logic
│       ├── ViewModels/           # View Models
│       ├── Views/                # Razor Views
│       └── wwwroot/              # CSS, JS, Static Files
├── tests/
│   └── BudgetManager.Tests/      # xUnit Tests
├── sample-data/
│   └── sample-transactions.csv   # Sample Import File
└── README.md
```

## CSV Import Format

| Column | Format | Required | Description |
|--------|--------|----------|-------------|
| Date | MM/DD/YYYY or YYYY-MM-DD | Yes | Transaction date |
| Description | Text | Yes | Transaction description |
| Amount | Decimal | Yes | Negative for expenses, positive for income |
| Category | Text | No | Category name (must match existing) |

### Example
```csv
Date,Description,Amount,Category
01/15/2026,Walmart Groceries,-85.42,Groceries
01/16/2026,Direct Deposit,2600.00,Income
01/17/2026,Netflix Subscription,-15.99,Subscriptions
```

## Running Tests

```bash
cd tests/BudgetManager.Tests
dotnet test
```

## License

MIT License
