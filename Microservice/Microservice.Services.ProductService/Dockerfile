FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Microservice.Services.ProductService/Microservice.Services.ProductService.csproj", "Microservice.Services.ProductService/"]
COPY ["Microservice.Common/Microservice.Common.csproj", "Microservice.Common/"]
RUN dotnet restore "Microservice.Services.ProductService/Microservice.Services.ProductService.csproj"
COPY . .
WORKDIR "/src/Microservice.Services.ProductService"
RUN dotnet build "Microservice.Services.ProductService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Microservice.Services.ProductService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Microservice.Services.ProductService.dll"]

