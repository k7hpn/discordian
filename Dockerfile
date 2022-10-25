# Get build image
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# Copy source
COPY . ./

# Publish
RUN dotnet publish -c Release -o "/app/publish/"

# Get runtime image
FROM mcr.microsoft.com/dotnet/runtime:6.0 AS publish
WORKDIR /app

# Bring in metadata via --build-arg
ARG BRANCH=unknown
ARG IMAGE_CREATED=unknown
ARG IMAGE_REVISION=unknown
ARG IMAGE_VERSION=unknown

# Configure image labels
LABEL branch=$branch \
    org.opencontainers.image.created=$IMAGE_CREATED \
    org.opencontainers.image.description="Discord Ian is a bot for Discord" \
    org.opencontainers.image.documentation="https://github.com/k7hpn/discordian/" \
    org.opencontainers.image.licenses="MIT" \
    org.opencontainers.image.revision=$IMAGE_REVISION \
    org.opencontainers.image.source="https://github.com/k7hpn/discordian/" \
    org.opencontainers.image.title="DiscordIan" \
    org.opencontainers.image.url="https://github.com/k7hpn/discordian/" \
    org.opencontainers.image.version=$IMAGE_VERSION

# Default image environment variable settings
ENV org.opencontainers.image.created=$IMAGE_CREATED \
    org.opencontainers.image.revision=$IMAGE_REVISION \
    org.opencontainers.image.version=$IMAGE_VERSION

# Copy source
COPY --from=build "/app/publish/" .

# Set entrypoint
ENTRYPOINT ["dotnet", "DiscordIan.dll"]
