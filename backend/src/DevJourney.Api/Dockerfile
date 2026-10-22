# Build multi-stage: a imagem final contém apenas o runtime e os binários publicados (menor e mais segura).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/DevJourney.Domain/*.csproj src/DevJourney.Domain/
COPY src/DevJourney.Application/*.csproj src/DevJourney.Application/
COPY src/DevJourney.Infrastructure/*.csproj src/DevJourney.Infrastructure/
COPY src/DevJourney.Api/*.csproj src/DevJourney.Api/
# Restore em camada separada: só é refeito quando algum .csproj muda (cache do Docker).
RUN dotnet restore src/DevJourney.Api/DevJourney.Api.csproj
COPY src/ src/
RUN dotnet publish src/DevJourney.Api/DevJourney.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
# Imagens .NET 8+ já trazem um usuário sem privilégios de root.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DevJourney.Api.dll"]
