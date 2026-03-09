FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Weather/Weather.csproj", "Weather/"]
RUN dotnet restore "Weather/Weather.csproj"

COPY . .
WORKDIR "/src/Weather"
RUN dotnet build "./Weather.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "./Weather.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

RUN mkdir -p /app/data && chown -R $APP_UID /app/data

COPY --from=publish /app/publish .


EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "Weather.dll"]