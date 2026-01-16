# Budget Manager - Project Description

**Full-stack web app for tracking expenses, budgets, and insights (.NET 8, ASP.NET Core MVC, EF Core, SQLite, ASP.NET Identity, Bootstrap 5, Chart.js, Google Charts, xUnit, CsvHelper, Python, pandas, matplotlib, Plotly, Jupyter)**

Built a polished personal finance app using .NET 8, ASP.NET Core MVC, Razor Views, Entity Framework Core 8, SQLite, and ASP.NET Core Identity, featuring integrated data visualizations and supplementary Python-based analysis

• Designed a normalized data model (Transactions, Budgets, Categories, Accounts, LockedMonths) with EF Core migrations, decimal-precision financial calculations, and cross-platform SQLite compatibility (migrated from SQL Server LocalDB for macOS/Linux support)

• Implemented monthly budgeting logic with smart pacing insights (e.g., "Safe to spend today"), category status indicators (OK/WATCH/OVER), expected-to-date calculations, and month locking to prevent edits to finalized periods

• Developed a CSV import engine using CsvHelper library with preview functionality, duplicate detection using normalized date/amount/description/account matching, and rule-based auto-categorization with prioritized text-matching (case-insensitive pattern matching)

• Optimized LINQ queries for SQLite compatibility by replacing `Math.Abs()` calls with arithmetic negation operations across services and controllers, ensuring database-level query translation and maintaining performance for expense aggregations

• Used ASP.NET Core Identity for authentication and created secure workflows for editing, importing, and locking financial data with user attribution and activity logging

• Created xUnit tests with Moq for mocking and EF Core InMemory provider for business rules: duplicate detection logic, budget status calculations, locked month restrictions, and categorization rule priority handling

• Generated reports with Chart.js 4.4.1 and Bootstrap 5.3.2 integration showing month-over-month spending trends, top expenses analysis, and budget vs. actual comparisons with interactive filtering and responsive design

• Integrated advanced financial visualizations into the web dashboard including: spending-by-category horizontal bar charts, 14-day daily spending trend analysis with budget target and average lines, interactive spending calendar heatmap with intensity-based coloring, and Google Charts Sankey diagrams showing cash flow from income through categories to individual merchants

• Built RESTful API endpoints serving JSON chart data for real-time dashboard visualizations, enabling dynamic updates without page reloads and supporting responsive chart rendering across device sizes

• Created a supplementary Jupyter notebook data analysis pipeline using Python, pandas, NumPy, matplotlib, and Plotly for offline exploratory analysis, prototyping visualizations, and generating exportable financial reports

• Implemented goal tracking analytics with configurable savings targets (monthly/annual), spending limits (daily/weekly), and progress indicators comparing actual performance against financial goals with percentage-based status reporting

• Deployed-ready structure with environment-based configuration (Development/Production), automated database seeding with demo data, EF Core migrations, and SQLite file-based database for zero-configuration local development across Windows, macOS, and Linux
