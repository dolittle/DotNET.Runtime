# DotNet Source - filtered

FROM alpine AS dotnet-source
ARG CONFIGURATION=Release
WORKDIR /app
COPY Source ./Source/

# DotNet Build
FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS dotnet-build
ARG CONFIGURATION=Release
WORKDIR /app

COPY default.props ./
COPY versions.props ./
COPY Runtime.sln ./
COPY --from=dotnet-source /app/Source ./Source/

COPY Specifications ./Specifications/

WORKDIR /app/Source/Server
RUN dotnet restore
RUN dotnet publish -c ${CONFIGURATION} -o out

# Web Build
FROM node:14.2.0 AS web-build
WORKDIR /app

COPY Source/Management.Web ./Source/Management.Web

WORKDIR /app/Source/Management.Web

RUN yarn
RUN yarn build

# Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
ARG CONFIGURATION=Release

RUN echo Configuration = $CONFIGURATION

RUN if [ "$CONFIGURATION" = "Debug" ] ; then apt-get update && \
    apt-get install -y --no-install-recommends unzip procps curl && \
    rm -rf /var/lib/apt/lists/* \
    ; fi

RUN if [ "$CONFIGURATION" = "Debug" ] ; then curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l ~/vsdbg ; fi
RUN apt-get update && apt-get install -y libc-dev

WORKDIR /app
COPY --from=dotnet-build /app/Source/Server/out ./
COPY --from=dotnet-build /app/Source/Server/.dolittle ./.dolittle
COPY --from=web-build /app/Source/Management.Web/wwwroot ./wwwroot

EXPOSE 81
EXPOSE 9700

ENTRYPOINT ["dotnet", "Dolittle.Runtime.Server.dll"]
