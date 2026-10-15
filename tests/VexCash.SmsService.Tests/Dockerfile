FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["tests/VexCash.SmsService.Tests/VexCash.SmsService.Tests.csproj", "tests/VexCash.SmsService.Tests/"]
COPY ["src/VexCash.SmsService/VexCash.SmsService.csproj", "src/VexCash.SmsService/"]
RUN dotnet restore "tests/VexCash.SmsService.Tests/VexCash.SmsService.Tests.csproj"
COPY . .
WORKDIR "/src/tests/VexCash.SmsService.Tests"
RUN dotnet build "./VexCash.SmsService.Tests.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VexCash.SmsService.Tests.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "VexCash.SmsService.Tests.dll"]
