#   docker build --build-arg version=1.2.3 --build-arg gitsha=$(git rev-parse HEAD) -t myotis:1.2.3 .
FROM node:24 AS vuebuild
WORKDIR /src/RoushTech.Myotis.Vue
COPY RoushTech.Myotis.Vue/package.json RoushTech.Myotis.Vue/package-lock.json ./
RUN npm ci
COPY RoushTech.Myotis.Vue/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
ARG version=0.0.0
ARG gitsha
WORKDIR /src
COPY . .
RUN dotnet publish RoushTech.Myotis.Website -c Release -p:BuildSpa=false -p:HUSKY=0 -p:Version=$version -p:SourceRevisionId=$gitsha -p:DebugType=none -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=vuebuild /src/RoushTech.Myotis.Vue/build wwwroot
# The SQLite file lives here; mount a volume so mail survives a redeploy.
RUN mkdir -p /app/data
VOLUME /app/data
ENV ASPNETCORE_URLS=http://+:80
ENV ConnectionStrings__Default="Data Source=/app/data/myotis.db"
EXPOSE 80 2525
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=5 \
    CMD curl -fsS http://127.0.0.1:80/health || exit 1
ENTRYPOINT ["dotnet", "RoushTech.Myotis.Website.dll"]
