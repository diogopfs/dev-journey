FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/DevJourney.Domain/*.csproj src/DevJourney.Domain/
COPY src/DevJourney.Application/*.csproj src/DevJourney.Application/
COPY src/DevJourney.Infrastructure/*.csproj src/DevJourney.Infrastructure/
COPY src/DevJourney.Worker/*.csproj src/DevJourney.Worker/
RUN dotnet restore src/DevJourney.Worker/DevJourney.Worker.csproj
COPY src/ src/
RUN dotnet publish src/DevJourney.Worker/DevJourney.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "DevJourney.Worker.dll"]
