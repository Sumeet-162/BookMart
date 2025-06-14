# -------- Build Stage --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the .csproj file and restore dependencies
COPY BookMart.csproj ./
RUN dotnet restore

# Copy the rest of the app and publish it
COPY . ./
RUN dotnet publish -c Release -o out

# -------- Runtime Stage --------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app/out .

# Expose default port
EXPOSE 80

# Set the entry point
ENTRYPOINT ["dotnet", "BookMart.dll"]
