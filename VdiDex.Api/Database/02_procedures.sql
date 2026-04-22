USE VdiDex;
GO

IF OBJECT_ID(N'dbo.SaveDEXMeter', N'P') IS NOT NULL DROP PROCEDURE dbo.SaveDEXMeter;
GO

CREATE PROCEDURE dbo.SaveDEXMeter
    @Machine              NVARCHAR(16),
    @DEXDateTime          DATETIME2(0),
    @MachineSerialNumber  NVARCHAR(64),
    @ValueOfPaidVends     BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.DEXMeter (Machine, DEXDateTime, MachineSerialNumber, ValueOfPaidVends)
    OUTPUT INSERTED.Id
    VALUES (@Machine, @DEXDateTime, @MachineSerialNumber, @ValueOfPaidVends);
END
GO

IF OBJECT_ID(N'dbo.SaveDEXLaneMeters', N'P') IS NOT NULL DROP PROCEDURE dbo.SaveDEXLaneMeters;
GO

CREATE PROCEDURE dbo.SaveDEXLaneMeters
    @Lanes dbo.DEXLaneMeterType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.DEXLaneMeter (DEXMeterId, ProductIdentifier, Price, NumberOfVends, ValueOfPaidSales)
    SELECT DEXMeterId, ProductIdentifier, Price, NumberOfVends, ValueOfPaidSales
    FROM @Lanes;
END
GO

IF OBJECT_ID(N'dbo.ClearDexTables', N'P') IS NOT NULL DROP PROCEDURE dbo.ClearDexTables;
GO

CREATE PROCEDURE dbo.ClearDexTables
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRAN;
        DELETE FROM dbo.DEXLaneMeter;
        DELETE FROM dbo.DEXMeter;
        DBCC CHECKIDENT ('dbo.DEXLaneMeter', RESEED, 0) WITH NO_INFOMSGS;
        DBCC CHECKIDENT ('dbo.DEXMeter', RESEED, 0) WITH NO_INFOMSGS;
    COMMIT TRAN;
END
GO
