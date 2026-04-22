IF DB_ID(N'VdiDex') IS NULL
BEGIN
    CREATE DATABASE VdiDex;
END
GO

USE VdiDex;
GO

IF OBJECT_ID(N'dbo.DEXLaneMeter', N'U') IS NOT NULL DROP TABLE dbo.DEXLaneMeter;
IF OBJECT_ID(N'dbo.DEXMeter', N'U') IS NOT NULL DROP TABLE dbo.DEXMeter;
GO

CREATE TABLE dbo.DEXMeter
(
    Id                   BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DEXMeter PRIMARY KEY,
    Machine              NVARCHAR(16)         NOT NULL
        CONSTRAINT CK_DEXMeter_Machine CHECK (Machine IN (N'A', N'B')),
    DEXDateTime          DATETIME2(0)         NOT NULL,
    MachineSerialNumber  NVARCHAR(64)         NOT NULL,
    ValueOfPaidVends     BIGINT               NOT NULL,
    ReceivedAt           DATETIME2(3)         NOT NULL CONSTRAINT DF_DEXMeter_ReceivedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_DEXMeter_Machine_DEXDateTime UNIQUE (Machine, DEXDateTime)
);
GO

CREATE TABLE dbo.DEXLaneMeter
(
    Id                 BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DEXLaneMeter PRIMARY KEY,
    DEXMeterId         BIGINT               NOT NULL,
    ProductIdentifier  NVARCHAR(32)         NOT NULL,
    Price              BIGINT               NOT NULL,
    NumberOfVends      BIGINT               NOT NULL,
    ValueOfPaidSales   BIGINT               NOT NULL,
    CONSTRAINT FK_DEXLaneMeter_DEXMeter
        FOREIGN KEY (DEXMeterId) REFERENCES dbo.DEXMeter(Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_DEXLaneMeter_DEXMeterId ON dbo.DEXLaneMeter(DEXMeterId);
GO

IF TYPE_ID(N'dbo.DEXLaneMeterType') IS NOT NULL DROP TYPE dbo.DEXLaneMeterType;
GO

CREATE TYPE dbo.DEXLaneMeterType AS TABLE
(
    DEXMeterId         BIGINT       NOT NULL,
    ProductIdentifier  NVARCHAR(32) NOT NULL,
    Price              BIGINT       NOT NULL,
    NumberOfVends      BIGINT       NOT NULL,
    ValueOfPaidSales   BIGINT       NOT NULL
);
GO
