FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/VexCash.SmsService/VexCash.SmsService.csproj", "src/VexCash.SmsService/"]
RUN dotnet restore "src/VexCash.SmsService/VexCash.SmsService.csproj"
COPY . .
WORKDIR "/src/src/VexCash.SmsService"
RUN dotnet build "./VexCash.SmsService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VexCash.SmsService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "VexCash.SmsService.dll"]
