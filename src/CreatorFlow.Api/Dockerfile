# Build context: repository root. No secrets, DB access or migration during build.
FROM mcr.microsoft.com/dotnet/sdk:10.0.401-noble AS build
WORKDIR /source
COPY src/CreatorFlow.Api/CreatorFlow.Api.csproj src/CreatorFlow.Api/
COPY src/CreatorFlow.Contracts/CreatorFlow.Contracts.csproj src/CreatorFlow.Contracts/
RUN dotnet restore src/CreatorFlow.Api/CreatorFlow.Api.csproj -r linux-x64
COPY src/CreatorFlow.Api/ src/CreatorFlow.Api/
COPY src/CreatorFlow.Contracts/ src/CreatorFlow.Contracts/
RUN dotnet publish src/CreatorFlow.Api/CreatorFlow.Api.csproj -c Release -r linux-x64 --self-contained false --no-restore -o /out

# Portable backend tests, including real native image decoding; opt-in build target.
FROM build AS tests
COPY tests/CreatorFlow.Api.Tests/ tests/CreatorFlow.Api.Tests/
RUN dotnet test tests/CreatorFlow.Api.Tests/CreatorFlow.Api.Tests.csproj -c Release --filter "TestCategory!=AuthApiDatabase" --logger "trx;LogFileName=auth-linux.trx" --results-directory /test-results

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12-noble AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /out/ ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "CreatorFlow.Api.dll"]
