# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish UserManagementSystem.csproj -c Release -o out

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build-env /app/out .

# Expose the port Render expects
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Command to run the application
ENTRYPOINT ["dotnet", "UserManagementSystem.dll"]
