FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Microservice.Services.UserService/Microservice.Services.UserService.csproj", "Microservice.Services.UserService/"]
COPY ["Microservice.Common/Microservice.Common.csproj", "Microservice.Common/"]
RUN dotnet restore "Microservice.Services.UserService/Microservice.Services.UserService.csproj"
COPY . .
WORKDIR "/src/Microservice.Services.UserService"
RUN dotnet build "Microservice.Services.UserService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Microservice.Services.UserService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Microservice.Services.UserService.dll"]

