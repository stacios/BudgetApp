-- Stored Procedure: sp_LockMonth
-- Purpose: Locks a month to prevent edits/deletes of transactions
-- Uses SQL transaction to ensure atomicity
-- Inserts into LockedMonth and writes to ActivityLog

IF OBJECT_ID('dbo.sp_LockMonth', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_LockMonth;
GO

CREATE PROCEDURE dbo.sp_LockMonth
    @Year INT,
    @Month INT,
    @UserId NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @LockedMonthId INT;
    DECLARE @MonthName NVARCHAR(20);
    DECLARE @TransactionCount INT;
    
    -- Validate parameters
    IF @Year < 2000 OR @Year > 2100
    BEGIN
        SET @ErrorMessage = 'Year must be between 2000 and 2100.';
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN;
    END
    
    IF @Month < 1 OR @Month > 12
    BEGIN
        SET @ErrorMessage = 'Month must be between 1 and 12.';
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN;
    END
    
    -- Get month name for logging
    SET @MonthName = DATENAME(MONTH, DATEFROMPARTS(@Year, @Month, 1));
    
    -- Check if already locked
    IF EXISTS (SELECT 1 FROM dbo.LockedMonths WHERE [Year] = @Year AND [Month] = @Month)
    BEGIN
        SET @ErrorMessage = CONCAT(@MonthName, ' ', @Year, ' is already locked.');
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN;
    END
    
    -- Get transaction count for this month
    SELECT @TransactionCount = COUNT(*)
    FROM dbo.Transactions
    WHERE YEAR(Date) = @Year AND MONTH(Date) = @Month;
    
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Insert into LockedMonth
        INSERT INTO dbo.LockedMonths ([Year], [Month], LockedAt, LockedByUserId)
        VALUES (@Year, @Month, GETUTCDATE(), @UserId);
        
        SET @LockedMonthId = SCOPE_IDENTITY();
        
        -- Insert into ActivityLog
        INSERT INTO dbo.ActivityLogs 
            (EntityName, EntityId, ActionType, [Description], OldValuesJson, NewValuesJson, UserId, [Timestamp])
        VALUES 
            ('LockedMonth', 
             @LockedMonthId, 
             'Lock', 
             CONCAT('Locked ', @MonthName, ' ', @Year, ' (', @TransactionCount, ' transactions)'),
             NULL,
             CONCAT('{"Year":', @Year, ',"Month":', @Month, ',"TransactionCount":', @TransactionCount, '}'),
             @UserId,
             GETUTCDATE());
        
        COMMIT TRANSACTION;
        
        -- Return success message
        SELECT 
            @LockedMonthId AS LockedMonthId,
            CONCAT(@MonthName, ' ', @Year, ' has been locked.') AS Message,
            @TransactionCount AS TransactionCount;
            
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        SET @ErrorMessage = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END
GO

-- Example usage:
-- EXEC sp_LockMonth @Year = 2024, @Month = 1, @UserId = 'user-guid-here';

-- Grant execute permission (adjust as needed)
-- GRANT EXECUTE ON dbo.sp_LockMonth TO [ApplicationRole];
