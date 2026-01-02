# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["ECommerceApp.RyanW84.csproj", "./"]
COPY ["ConsoleClient/ECommerceApp.ConsoleClient.csproj", "ConsoleClient/"]

# Restore dependencies
RUN dotnet restore "ECommerceApp.RyanW84.csproj"

# Copy source code
COPY . .

# Build the application
RUN dotnet build "ECommerceApp.RyanW84.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ECommerceApp.RyanW84.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Expose ports
EXPOSE 80 443

ENTRYPOINT ["dotnet", "ECommerceApp.RyanW84.dll"]
