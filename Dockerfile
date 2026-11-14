# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, in its own layer, so it is cached until a project file changes.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/BookingHub.Domain/BookingHub.Domain.csproj src/BookingHub.Domain/
COPY src/BookingHub.Application/BookingHub.Application.csproj src/BookingHub.Application/
COPY src/BookingHub.Infrastructure/BookingHub.Infrastructure.csproj src/BookingHub.Infrastructure/
COPY src/BookingHub.Api/BookingHub.Api.csproj src/BookingHub.Api/
RUN dotnet restore src/BookingHub.Api/BookingHub.Api.csproj

COPY src/ src/
RUN dotnet publish src/BookingHub.Api/BookingHub.Api.csproj -c Release -o /app/publish --no-restore

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# SQLite database lives on a volume owned by the non-root app user.
RUN mkdir -p /data && chown "$APP_UID" /data
VOLUME /data

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__Default="Data Source=/data/bookinghub.db"
EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "BookingHub.Api.dll"]
