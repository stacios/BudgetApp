-- View: vw_MonthlyCategorySummary
-- Purpose: Provides a summary of budget vs actual spending per category per month
-- Includes: Budget, Actual, Remaining, ExpectedToDate calculations

IF OBJECT_ID('dbo.vw_MonthlyCategorySummary', 'V') IS NOT NULL
    DROP VIEW dbo.vw_MonthlyCategorySummary;
GO

CREATE VIEW dbo.vw_MonthlyCategorySummary
AS
WITH MonthlySpending AS (
    SELECT 
        YEAR(t.Date) AS [Year],
        MONTH(t.Date) AS [Month],
        t.CategoryId,
        SUM(CASE WHEN t.Amount < 0 THEN ABS(t.Amount) ELSE 0 END) AS ActualSpent,
        SUM(CASE WHEN t.Amount > 0 THEN t.Amount ELSE 0 END) AS ActualIncome,
        COUNT(*) AS TransactionCount
    FROM dbo.Transactions t
    GROUP BY YEAR(t.Date), MONTH(t.Date), t.CategoryId
),
MonthDays AS (
    SELECT 
        [Year],
        [Month],
        DAY(EOMONTH(DATEFROMPARTS([Year], [Month], 1))) AS DaysInMonth,
        CASE 
            WHEN [Year] = YEAR(GETDATE()) AND [Month] = MONTH(GETDATE()) 
            THEN DAY(GETDATE())
            ELSE DAY(EOMONTH(DATEFROMPARTS([Year], [Month], 1)))
        END AS CurrentDay
    FROM (
        SELECT DISTINCT YEAR(Date) AS [Year], MONTH(Date) AS [Month]
        FROM dbo.Transactions
        UNION
        SELECT DISTINCT Year AS [Year], Month AS [Month]
        FROM dbo.MonthlyBudgets
    ) AS months
)
SELECT 
    COALESCE(mb.[Year], ms.[Year]) AS [Year],
    COALESCE(mb.[Month], ms.[Month]) AS [Month],
    c.Id AS CategoryId,
    c.Name AS CategoryName,
    COALESCE(mb.BudgetAmount, 0) AS Budget,
    COALESCE(ms.ActualSpent, 0) AS Actual,
    COALESCE(mb.BudgetAmount, 0) - COALESCE(ms.ActualSpent, 0) AS Remaining,
    -- ExpectedToDate = Budget * (CurrentDay / DaysInMonth)
    CASE 
        WHEN md.DaysInMonth > 0 
        THEN COALESCE(mb.BudgetAmount, 0) * CAST(md.CurrentDay AS DECIMAL(18,2)) / CAST(md.DaysInMonth AS DECIMAL(18,2))
        ELSE 0 
    END AS ExpectedToDate,
    -- PercentUsed = (Actual / Budget) * 100
    CASE 
        WHEN COALESCE(mb.BudgetAmount, 0) > 0 
        THEN (COALESCE(ms.ActualSpent, 0) / mb.BudgetAmount) * 100 
        ELSE 0 
    END AS PercentUsed,
    -- Status: OK, WATCH, OVER
    CASE 
        WHEN COALESCE(ms.ActualSpent, 0) > COALESCE(mb.BudgetAmount, 0) AND COALESCE(mb.BudgetAmount, 0) > 0 
        THEN 'OVER'
        WHEN COALESCE(ms.ActualSpent, 0) > (COALESCE(mb.BudgetAmount, 0) * CAST(md.CurrentDay AS DECIMAL(18,2)) / NULLIF(CAST(md.DaysInMonth AS DECIMAL(18,2)), 0)) 
             AND COALESCE(mb.BudgetAmount, 0) > 0 
        THEN 'WATCH'
        ELSE 'OK'
    END AS [Status],
    md.DaysInMonth,
    md.CurrentDay,
    COALESCE(ms.TransactionCount, 0) AS TransactionCount
FROM dbo.Categories c
CROSS JOIN MonthDays md
LEFT JOIN dbo.MonthlyBudgets mb 
    ON c.Id = mb.CategoryId 
    AND mb.[Year] = md.[Year] 
    AND mb.[Month] = md.[Month]
LEFT JOIN MonthlySpending ms 
    ON c.Id = ms.CategoryId 
    AND ms.[Year] = md.[Year] 
    AND ms.[Month] = md.[Month]
WHERE c.IsActive = 1
  AND (COALESCE(mb.BudgetAmount, 0) > 0 OR COALESCE(ms.ActualSpent, 0) > 0);
GO

-- Example usage:
-- SELECT * FROM vw_MonthlyCategorySummary WHERE [Year] = 2024 AND [Month] = 1 ORDER BY CategoryName;
-- SELECT * FROM vw_MonthlyCategorySummary WHERE [Status] = 'OVER';
