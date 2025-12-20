# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["GixatBackend.csproj", "./"]
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Create a non-root user 'appuser'
RUN useradd --create-home appuser && chown -R appuser:appuser /app
USER appuser

# Expose the port the app runs on
EXPOSE 8002
ENV ASPNETCORE_URLS=http://+:8002

ENTRYPOINT ["dotnet", "GixatBackend.dll"]
