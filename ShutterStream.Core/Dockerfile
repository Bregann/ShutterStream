# See https://aka.ms/customizecontainer to learn how to customize your debug container
# and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["ShutterStream.Core/ShutterStream.Core.csproj", "ShutterStream.Core/"]
COPY ["ShutterStream.Domain/ShutterStream.Domain.csproj", "ShutterStream.Domain/"]

# Restore dependencies
RUN dotnet restore "ShutterStream.Core/ShutterStream.Core.csproj"

# Copy all source
COPY . .

# Build the project
WORKDIR "/src/ShutterStream.Core"
RUN dotnet build "ShutterStream.Core.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/ShutterStream.Core"
RUN dotnet publish "ShutterStream.Core.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShutterStream.Core.dll"]
