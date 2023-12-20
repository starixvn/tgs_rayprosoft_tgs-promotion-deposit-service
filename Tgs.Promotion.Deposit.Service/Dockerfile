#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Promotion.Deposit.Service/Tgs.Promotion.Deposit.Service.csproj", "Tgs.Promotion.Deposit.Service/"]
COPY ["Tgs.Promotion.Deposit.Repository/Tgs.Promotion.Deposit.Repository.csproj", "Tgs.Promotion.Deposit.Repository/"]
COPY ["Tgs.Promotion.Deposit.Core/Tgs.Promotion.Deposit.Core.csproj", "Tgs.Promotion.Deposit.Core/"]
RUN dotnet restore "./Tgs.Promotion.Deposit.Service/./Tgs.Promotion.Deposit.Service.csproj"
COPY . .
WORKDIR "/src/Tgs.Promotion.Deposit.Service"
RUN dotnet build "./Tgs.Promotion.Deposit.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tgs.Promotion.Deposit.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Promotion.Deposit.Service.dll"]