# Run Microsoft SQL Server in Docker (local development)

This project uses a local SQL Server for development. The repo includes a `docker-compose.yml` to run SQL Server 2022.

## Prerequisites

- Docker and Docker Compose installed.

## Quick start

### 1. Set up user secrets (secure method)

Store your MSSQL password in user secrets instead of environment files:

```bash
dotnet user-secrets set "MSSQL:SA_PASSWORD" "<YOUR_SECURE_PASSWORD>"
```

### 2. Start the container

**Option A: Using the helper script (recommended)**

```bash
./start-docker.sh
```

This script automatically loads your password from user secrets and starts the containers.

**Option B: Manual with environment variable**

```bash
export SA_PASSWORD=$(dotnet user-secrets list | grep MSSQL:SA_PASSWORD | cut -d' ' -f3)
docker compose up -d
```

**Option C: Manual with .env file (less secure)**

Create a local `.env` file (which is .gitignored):

```bash
echo "SA_PASSWORD=<YOUR_SECURE_PASSWORD>" > .env
docker compose up -d
```

### 3. Check the container health

```bash
docker compose ps
```

## Connection strings

Use this connection string in your .NET application:

```
Server=localhost,1433;Database=ECommerceDb;User Id=sa;Password=<YOUR_SECURE_PASSWORD>;TrustServerCertificate=True;
```

Note: The password should come from user secrets or environment variables, never hardcoded.

## Security Notes

- ✅ **Recommended**: Use `dotnet user-secrets set` to store sensitive credentials
- ❌ **Avoid**: Committing `.env` files with real passwords (they're .gitignored for security)
- ✅ Use `TrustServerCertificate=True` for local dev to avoid TLS issues
- ✅ Data is persisted in a named Docker volume `ecommerce-mssql-data`

## Cleanup

Stop and remove the container and volume:

```bash
docker compose down -v
```

To remove all secrets:

```bash
dotnet user-secrets clear
```
