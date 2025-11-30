# EcommerceAPIDevelopment

A basic E‑Commerce API example with ASP.NET Core and SQL Server. This repo includes a Docker Compose service for SQL Server that you can run directly from JetBrains Rider.

## Run the SQL Server container in Rider

Prerequisites:
- Docker Engine available on your machine (Docker Desktop on Windows/macOS, Docker on Linux)
- Rider Docker plugin enabled (File → Settings → Plugins → Docker)

Steps:
1. Create a `.env` file from the provided example:
   - Copy `.env.example` to `.env` and set a strong password.
     ```bash
     cp .env.example .env
     # then edit .env and set SA_PASSWORD
     ```
2. In Rider, open the Services tool window (View → Tool Windows → Services) and add a Docker connection if you don’t have one yet (click + → Docker).
3. Create a Docker-Compose Run Configuration:
   - Run → Edit Configurations… → + → Docker-Compose
   - Compose files: select this repo’s `docker-compose.yml`
   - Ensure the configuration loads environment from the project `.env` (Rider does this automatically when the file is in the same directory as `docker-compose.yml`).
   - Optionally restrict Services to only `mssql`.
4. Run the Docker-Compose configuration. You should see a container named `ecommerce-mssql` in Services. Wait for the healthcheck to turn green.

Docker Compose details (already configured):
- Port mapping: `1433:1433`
- Volume: `ecommerce-mssql-data` for persistence
- Network: `ecommerce-network`

## Configure the API to use the container

The app resolves its connection string from, in order: User Secrets, appsettings, or environment variables. If nothing is configured, it will fall back to a local SQLite file database in Development so the API can start without storing any credentials on disk.

For development against SQL Server, use one of these options:

Option A — .NET User Secrets (recommended for local dev):
```bash
dotnet user-secrets set "ConnectionStrings:DatabaseConnection" \
  "Server=localhost,1433;Database=ECommerceDb;User Id=sa;Password=<YourStrong!Passw0rd>;Encrypt=True;TrustServerCertificate=True;"
```

Option B — Rider Run Configuration environment variable:
- Key: `ConnectionStrings__DatabaseConnection`
- Value: `Server=localhost,1433;Database=ECommerceDb;User Id=sa;Password=<YourStrong!Passw0rd>;Encrypt=True;TrustServerCertificate=True;`

Option C — Flat environment variables (useful in containers/CI):
- `DATABASE_CONNECTION_STRING` or `DB_CONNECTION_STRING` with the full connection string value

Notes:
- The application auto-selects the EF Core provider:
  - If the connection string looks like SQLite (e.g., starts with `Data Source=`), it uses SQLite.
  - Otherwise it uses SQL Server.
- If no connection string is configured, a local SQLite file `Data/ecommerce.dev.db` is created and used in Development.
- In Development, the database is recreated and seeded automatically by a hosted service.

Security tip:
- Prefer environment variables for secrets in shared machines/CI.
- If you no longer want secrets stored in User Secrets for this project, you can remove them:
  ```bash
  dotnet user-secrets list
  dotnet user-secrets remove "ConnectionStrings:DatabaseConnection"
  ```

## Run and debug the API in Rider
1. Start the Docker-Compose configuration (SQL Server) and wait until healthy.
2. Select the `ECommerceApp.RyanW84` run configuration and Run/Debug.
3. On first run in Development, you’ll see logs:
   - “Database migrations applied.”
   - “Database seeded with initial data.”
4. Open the Scalar UI: `https://localhost:<port>/scalar/v1`

## Troubleshooting
- Healthcheck failing: ensure `SA_PASSWORD` is strong (8+ chars, mixed case, number, symbol). Recreate the container:
  ```bash
  docker compose down -v
  docker compose up -d
  ```
- Port 1433 already in use: stop any local SQL Server instance, or change the host port in `docker-compose.yml` to `11433:1433` and update your connection string to `Server=localhost,11433;...`.
- SSL/Encryption errors: include `Encrypt=True;TrustServerCertificate=True;` in the connection string.
- User Secrets not found: run `dotnet user-secrets` from the project folder that contains `ECommerceApp.RyanW84.csproj` (it has a `UserSecretsId`).

## Optional: CLI verification inside the container
```bash
docker exec -it ecommerce-mssql /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" -Q "SELECT @@VERSION;" -C
```
