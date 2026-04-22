USE master;
GO

BACKUP DATABASE VdiDex
TO DISK = N'/var/opt/mssql/backup/VdiDex.bak'
WITH FORMAT, INIT, NAME = N'VdiDex-Full', COMPRESSION;
GO
