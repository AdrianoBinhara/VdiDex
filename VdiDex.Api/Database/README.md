# VdiDex Database

Scripts to provision the VdiDex SQL Server 2022 database for local dev.

## Prerequisites

- Docker running
- `sqlcmd` (via `mssql-tools` or the `go-sqlcmd` binary)
- `.env` file at repo root (copy from `.env.example`) defining `MSSQL_SA_PASSWORD`

## Bring up SQL Server

From repo root:

```bash
cp .env.example .env   # first time only
docker compose up -d
```

Container: `vdidex-sqlserver`, port `1433`, sa password sourced from `.env` (`MSSQL_SA_PASSWORD`).

## Apply schema + procs

```bash
source .env   # load MSSQL_SA_PASSWORD into shell

sqlcmd -S localhost,1433 -U sa -P "$MSSQL_SA_PASSWORD" -C -i VdiDex.Api/Database/01_schema.sql
sqlcmd -S localhost,1433 -U sa -P "$MSSQL_SA_PASSWORD" -C -i VdiDex.Api/Database/02_procedures.sql
```

## Create .bak

```bash
docker exec vdidex-sqlserver mkdir -p /var/opt/mssql/backup
sqlcmd -S localhost,1433 -U sa -P "$MSSQL_SA_PASSWORD" -C -i VdiDex.Api/Database/03_backup.sql
docker cp vdidex-sqlserver:/var/opt/mssql/backup/VdiDex.bak VdiDex.Api/Database/VdiDex.bak
```

## Restore .bak (fresh container)

```bash
docker cp VdiDex.Api/Database/VdiDex.bak vdidex-sqlserver:/var/opt/mssql/backup/VdiDex.bak
sqlcmd -S localhost,1433 -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "RESTORE DATABASE VdiDex FROM DISK = N'/var/opt/mssql/backup/VdiDex.bak' WITH REPLACE, MOVE 'VdiDex' TO '/var/opt/mssql/data/VdiDex.mdf', MOVE 'VdiDex_log' TO '/var/opt/mssql/data/VdiDex_log.ldf';"
```
