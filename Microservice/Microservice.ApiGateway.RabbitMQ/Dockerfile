FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Microservice.ApiGateway.RabbitMQ/Microservice.ApiGateway.RabbitMQ.csproj", "Microservice.ApiGateway.RabbitMQ/"]
COPY ["Microservice.Common/Microservice.Common.csproj", "Microservice.Common/"]
RUN dotnet restore "Microservice.ApiGateway.RabbitMQ/Microservice.ApiGateway.RabbitMQ.csproj"
COPY . .
WORKDIR "/src/Microservice.ApiGateway.RabbitMQ"
RUN dotnet build "Microservice.ApiGateway.RabbitMQ.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Microservice.ApiGateway.RabbitMQ.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Microservice.ApiGateway.RabbitMQ.dll"]

