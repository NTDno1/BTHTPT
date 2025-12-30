FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Microservice.Services.OrderService/Microservice.Services.OrderService.csproj", "Microservice.Services.OrderService/"]
COPY ["Microservice.Common/Microservice.Common.csproj", "Microservice.Common/"]
RUN dotnet restore "Microservice.Services.OrderService/Microservice.Services.OrderService.csproj"
COPY . .
WORKDIR "/src/Microservice.Services.OrderService"
RUN dotnet build "Microservice.Services.OrderService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Microservice.Services.OrderService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Microservice.Services.OrderService.dll"]

