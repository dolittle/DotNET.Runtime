# DotNet Build
FROM  mcr.microsoft.com/dotnet/sdk:5.0-alpine AS dotnet-build
ARG CONFIGURATION=Release

COPY default.props /app/
COPY versions.props /app/
COPY Runtime.sln /app/
COPY Source /app/Source/
COPY Specifications /app/Specifications/

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

RUN apt-get update && apt-get install -y libc-dev

WORKDIR /app
COPY --from=dotnet-build /app/Source/Server/out ./
COPY --from=dotnet-build /app/Source/Server/.dolittle ./.dolittle
COPY --from=web-build /app/Source/Management.Web/wwwroot ./wwwroot

# Install MongoDB dependencies
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        wget \
        gnupg \
    && rm -rf /var/lib/apt/lists/*

# Hack to get MongoDB to install even without systemctl
# See https://www.julienrouse.com/blog/systemctl_not_found_while_installing_mongodb_server/#spoiler-a-solution
RUN ln -s /bin/true /usr/local/sbin/systemctl
RUN ln -s /bin/true /usr/local/bin/systemctl
RUN ln -s /bin/true /usr/sbin/systemctl
RUN ln -s /bin/true /usr/bin/systemctl
RUN ln -s /bin/true /sbin/systemctl
RUN ln -s /bin/true /bin/systemctl

# Install MongoDB
RUN wget -qO - https://www.mongodb.org/static/pgp/server-4.2.asc | apt-key add - \
    && echo "deb [ arch=amd64,arm64 ] https://repo.mongodb.org/apt/ubuntu bionic/mongodb-org/4.2 multiverse" > /etc/apt/sources.list.d/mongodb-org-4.2.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends \
        mongodb-org-server \
        mongodb-org-shell \
    && rm -rf /var/lib/apt/lists/*

# Setup MongoDB as single-node replicaset
RUN mkdir -p /data/db /data/configdb \
    && chown -R mongodb:mongodb /data/db /data/configdb \
    && mongod --logpath /var/log/mongodb/initdb.log --replSet "rs0" --bind_ip 0.0.0.0 --fork \
    && mongo --eval 'rs.initiate({_id: "rs0", members: [{ _id: 0, host: "localhost:27017"}]})' \
    && mongod --shutdown

VOLUME /data/db /data/configdb

# Add Tini to get a real init process
ADD https://github.com/krallin/tini/releases/download/v0.19.0/tini /usr/bin/tini
RUN chmod +x /usr/bin/tini

# Create entrypoint that runs both MongoDB and Runtime
COPY Docker/Development/docker-entrypoint.sh /usr/bin/docker-entrypoint.sh
RUN chmod +x /usr/bin/docker-entrypoint.sh

# Expose all the ports that are usefull
EXPOSE 1880 27017 9007 50052 50053

ENTRYPOINT ["/usr/bin/tini", "--", "/bin/bash", "/usr/bin/docker-entrypoint.sh"]
