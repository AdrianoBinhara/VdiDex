# VdiDex

DEX (Data Exchange) ingestion system for unattended retail devices (vending machines). A .NET MAUI client posts hard-coded DEX reports for two machines (A and B) to an ASP.NET Core API that parses and persists them into SQL Server via stored procedures.

## Solution Layout

```
VdiDex/
├── VdiDex.sln
├── docker-compose.yml              # SQL Server 2022 (Linux) for local dev
├── VdiDex.Maui/                    # .NET MAUI 10 client (MVVM)
│   ├── Auth / Models / Views / ViewModels / Services / Interfaces
│   └── Resources/Raw/
│       ├── appsettings.json        # API base URL + basic auth creds
│       ├── dex_machine_a.txt       # Hard-coded DEX for Machine A
│       └── dex_machine_b.txt       # Hard-coded DEX for Machine B
└── VdiDex.Api/                     # ASP.NET Core 9 Minimal API
    ├── Auth/                       # Custom BasicAuthHandler
    ├── Endpoints/                  # DexEndpoints.cs (POST/DELETE /vdi-dex)
    ├── Interfaces/                 # IDexParser, IDexRepository
    ├── Services/                   # DexParser, DexRepository (Dapper + TVP)
    ├── Models/                     # DexMeter, DexLaneMeter, DexSaveResult, BasicAuthCredentials
    ├── Database/                   # SQL scripts + .bak
    │   ├── 01_schema.sql
    │   ├── 02_procedures.sql
    │   ├── 03_backup.sql
    │   └── VdiDex.bak              # Generated full backup
    ├── Program.cs
    └── appsettings.json
```

## Architecture

```
┌─────────────────────┐     HTTPS + Basic Auth      ┌────────────────────────┐
│  MAUI App           │  POST /vdi-dex?machine=A    │  ASP.NET Core 9 API    │
│  (MVVM, DI, typed   │  body = DEX text            │                        │
│   HttpClient)       │────────────────────────────▶│  - BasicAuthHandler    │
│                     │                             │  - DexParser (C#)      │
│  Buttons:           │  DELETE /vdi-dex            │  - DexRepository       │
│   [Send Machine A]  │────────────────────────────▶│    (Dapper + SqlClient)│
│   [Send Machine B]  │                             │                        │
│   [Clear Tables]    │◀────────────────────────────│                        │
└─────────────────────┘                             └───────────┬────────────┘
                                                                │ Stored procs
                                                                │ SaveDEXMeter
                                                                │ SaveDEXLaneMeters
                                                                │ ClearDexTables
                                                                ▼
                                                    ┌────────────────────────┐
                                                    │ SQL Server 2022        │
                                                    │  DEXMeter (parent)     │
                                                    │  DEXLaneMeter (child)  │
                                                    │  UQ(Machine,DEXDateTime│
                                                    │  CHECK Machine∈{A,B}   │
                                                    └────────────────────────┘
```

### DEX Parsing

Input format: segment-based, `*`-delimited, `/` terminated. Fields extracted:

| Segment      | Field  | Mapped to                     |
|--------------|--------|-------------------------------|
| `ID1*<sn>..` | 1      | `DEXMeter.MachineSerialNumber`|
| `ID5*YYYYMMDD*HHMM*..` | 1+2 | `DEXMeter.DEXDateTime`    |
| `VA1*<n>..`  | 1      | `DEXMeter.ValueOfPaidVends`   |
| `PA1*<id>*<price>` | 1,2 | Lane product + price        |
| `PA2*<vends>*<sales>` | 1,2 | Lane counters (paired with preceding PA1) |

Each `PA1`/`PA2` pair produces one `DEXLaneMeter` row.

### Database Schema

**DEXMeter** (parent)
- `Id` BIGINT IDENTITY PK
- `Machine` NVARCHAR(16) — CHECK IN ('A','B')
- `DEXDateTime` DATETIME2
- `MachineSerialNumber` NVARCHAR(64)
- `ValueOfPaidVends` BIGINT
- `ReceivedAt` DATETIME2 DEFAULT SYSUTCDATETIME()
- UNIQUE (`Machine`, `DEXDateTime`)

**DEXLaneMeter** (child)
- `Id` BIGINT IDENTITY PK
- `DEXMeterId` BIGINT FK → `DEXMeter.Id` ON DELETE CASCADE
- `ProductIdentifier` NVARCHAR(32)
- `Price` BIGINT
- `NumberOfVends` BIGINT
- `ValueOfPaidSales` BIGINT

**User-defined TVP**: `dbo.DEXLaneMeterType` — used by `SaveDEXLaneMeters`.

### Stored Procedures

- `dbo.SaveDEXMeter` — inserts parent, returns new `Id` via `OUTPUT INSERTED.Id`.
- `dbo.SaveDEXLaneMeters` — bulk insert lane rows from TVP.
- `dbo.ClearDexTables` — deletes both tables + reseeds identities (transactional).

## Prerequisites

- .NET SDK **9.0** and **10.0** (MAUI on 10, API on 9)
- .NET MAUI workload: `dotnet workload install maui`
- Docker (for SQL Server 2022 Linux container)
- `sqlcmd` (from `mssql-tools` or [`go-sqlcmd`](https://github.com/microsoft/go-sqlcmd))
- macOS, Windows, or Linux

## Setup

### 1. Configure local secrets

```bash
cp .env.example .env
cp VdiDex.Api/appsettings.Development.example.json VdiDex.Api/appsettings.Development.json
```

Edit both copies if you want a different SA password. Defaults work for local dev.

### 2. Start SQL Server

From repo root:

```bash
docker compose up -d
```

- Container: `vdidex-sqlserver`
- Port: `1433`
- SA password: loaded from `.env` (`MSSQL_SA_PASSWORD`)

### 3. Apply schema and procedures

```bash
source .env
sqlcmd -S localhost,1433 -U sa -P "$MSSQL_SA_PASSWORD" -C -i VdiDex.Api/Database/01_schema.sql
sqlcmd -S localhost,1433 -U sa -P "$MSSQL_SA_PASSWORD" -C -i VdiDex.Api/Database/02_procedures.sql
```

### 4. (Optional) Restore from `.bak`

Alternative to running scripts:

```bash
source .env
docker cp VdiDex.Api/Database/VdiDex.bak vdidex-sqlserver:/var/opt/mssql/backup/VdiDex.bak
sqlcmd -S localhost,1433 -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "RESTORE DATABASE VdiDex FROM DISK = N'/var/opt/mssql/backup/VdiDex.bak' WITH REPLACE, MOVE 'VdiDex' TO '/var/opt/mssql/data/VdiDex.mdf', MOVE 'VdiDex_log' TO '/var/opt/mssql/data/VdiDex_log.ldf';"
```

### 5. Run the API

```bash
dotnet run --project VdiDex.Api
```

Default: `http://localhost:5251` / `https://localhost:7109`.

### 6. Run the MAUI app

```bash
# macOS (Mac Catalyst)
dotnet build -t:Run -f net10.0-maccatalyst VdiDex.Maui/VdiDex.Maui.csproj

# iOS simulator
dotnet build -t:Run -f net10.0-ios VdiDex.Maui/VdiDex.Maui.csproj

# Android emulator
dotnet build -t:Run -f net10.0-android VdiDex.Maui/VdiDex.Maui.csproj
```

MAUI config: `VdiDex.Maui/Resources/Raw/appsettings.json`.

> **Note**: Android emulator must reach host API via `http://10.0.2.2:5251`. Physical devices need host LAN IP.

## API Reference

Base URL: `http://localhost:5251`

All endpoints require **HTTP Basic Auth**:
- Username: `vendsys`
- Password: `NFsZGmHAGWJSZ#RuvdiV`

### `POST /vdi-dex?machine={A|B}`

Submit a DEX report.

- Content-Type: `text/plain`
- Body: raw DEX text
- Responses:
  - `201 Created` — saved
  - `400 Bad Request` — invalid machine, empty body, or parse error
  - `401 Unauthorized` — missing/invalid basic auth
  - `409 Conflict` — duplicate (Machine, DEXDateTime) already exists

### `DELETE /vdi-dex`

Clear both tables (dev/testing helper).

- Response: `204 No Content`

## Re-generating the `.bak`

```bash
source .env
docker exec vdidex-sqlserver mkdir -p /var/opt/mssql/backup
sqlcmd -S localhost,1433 -U sa -P "$MSSQL_SA_PASSWORD" -C -i VdiDex.Api/Database/03_backup.sql
docker cp vdidex-sqlserver:/var/opt/mssql/backup/VdiDex.bak VdiDex.Api/Database/VdiDex.bak
```

## Tech Choices

| Concern             | Choice                                           |
|---------------------|--------------------------------------------------|
| Client              | .NET MAUI 10 + CommunityToolkit.Mvvm             |
| API                 | ASP.NET Core 9 Minimal API                       |
| Data access         | Dapper + `Microsoft.Data.SqlClient`              |
| DEX parsing         | C# parser → stored procedures (TVP for lanes)    |
| Auth                | Custom `AuthenticationHandler<BasicAuthSchemeOptions>` with fixed-time credential compare |
| Concurrency         | ASP.NET Core async pipeline, connection-per-request, transaction wraps parent + lane inserts |
| Logging             | Default `ILogger` + console                      |
| DB                  | SQL Server 2022 on Linux (Docker)                |

## Security Notes

- **Basic auth credentials** (`vendsys` / `NFsZGmHAGWJSZ#RuvdiV`) are committed in `appsettings.json` — this follows the assignment spec, which explicitly fixed these values and allowed them to live in config.
- **SQL Server SA password** is kept out of source control: set in `.env` (ignored by git) and consumed by `docker-compose.yml`; the API reads its connection string from `appsettings.Development.json` (also ignored, generated from the `.example` template).
- The `DELETE /vdi-dex` endpoint is destructive. Remove or gate behind an admin role before production use.
