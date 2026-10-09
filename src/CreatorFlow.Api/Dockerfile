# Build context: repository root. No secrets, DB access or migration during build.
ARG RENDER_GIT_COMMIT=local
ARG SOURCE_REVISION=${RENDER_GIT_COMMIT}
FROM mcr.microsoft.com/dotnet/sdk:10.0.401-noble AS build
ARG SOURCE_REVISION
WORKDIR /source
COPY src/CreatorFlow.Api/CreatorFlow.Api.csproj src/CreatorFlow.Api/
COPY src/CreatorFlow.Contracts/CreatorFlow.Contracts.csproj src/CreatorFlow.Contracts/
RUN dotnet restore src/CreatorFlow.Api/CreatorFlow.Api.csproj -r linux-x64
COPY src/CreatorFlow.Api/ src/CreatorFlow.Api/
COPY src/CreatorFlow.Contracts/ src/CreatorFlow.Contracts/
RUN test "$SOURCE_REVISION" = local || (test "${#SOURCE_REVISION}" -eq 40 && echo "$SOURCE_REVISION" | LC_ALL=C grep -Eq '^[0-9a-fA-F]{40}$')
RUN dotnet publish src/CreatorFlow.Api/CreatorFlow.Api.csproj -c Release -r linux-x64 --self-contained false --no-restore -o /out \
    -p:InformationalVersion="$SOURCE_REVISION" -p:IncludeSourceRevisionInInformationalVersion=false

# Portable backend tests, including real native image decoding; opt-in build target.
FROM build AS tests
COPY tests/CreatorFlow.Api.Tests/ tests/CreatorFlow.Api.Tests/
RUN dotnet test tests/CreatorFlow.Api.Tests/CreatorFlow.Api.Tests.csproj -c Release --filter "TestCategory!=AuthApiDatabase" --logger "trx;LogFileName=auth-linux.trx" --results-directory /test-results

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12-noble AS runtime
ARG SOURCE_REVISION
LABEL org.opencontainers.image.revision=${SOURCE_REVISION}
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /out/ ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "CreatorFlow.Api.dll"]
