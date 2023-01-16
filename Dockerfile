FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80


FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY SignalTrader/. .
RUN dotnet restore "SignalTrader.csproj" && \
    dotnet build "SignalTrader.csproj" -c Release -o /app/build


FROM build AS publish
RUN dotnet publish "SignalTrader.csproj" -c Release -o /app/publish


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SignalTrader.dll"]
