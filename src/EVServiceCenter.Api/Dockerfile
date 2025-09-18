# Build & runtime cho .NET 8
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files trước để tối ưu cache
COPY ["src/EVServiceCenter.Api/EVServiceCenter.Api.csproj", "src/EVServiceCenter.Api/"]
COPY ["src/EVServiceCenter.Application/EVServiceCenter.Application.csproj", "src/EVServiceCenter.Application/"]
COPY ["src/EVServiceCenter.Infrastructure/EVServiceCenter.Infrastructure.csproj", "src/EVServiceCenter.Infrastructure/"]
COPY ["src/EVServiceCenter.Domain/EVServiceCenter.Domain.csproj", "src/EVServiceCenter.Domain/"]

# Restore dependencies
RUN dotnet restore "src/EVServiceCenter.Api/EVServiceCenter.Api.csproj"

# Copy toàn bộ source code
COPY . .

# Build và publish
WORKDIR "/src/src/EVServiceCenter.Api"
RUN dotnet publish -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Entry point
ENTRYPOINT ["dotnet", "EVServiceCenter.Api.dll"]
