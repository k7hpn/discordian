# DiscordIan

Ian is a bot for Discord written in [C#](https://docs.microsoft.com/en-us/dotnet/csharp/)
using [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) designed to run
successfully in [Docker](https://www.docker.com) and be easy to customize/extend.

# Running the bot

## Getting a key

The core of the bot uses [Discord.Net](https://github.com/discord-net/Discord.Net) and their
[instructions for getting a Discord bot key](https://discord.foxbot.me/stable/guides/getting_started/first-bot.html#creating-a-discord-bot) are easy to follow. Go through the _Creating a Discord Bot_ section until you have a token from the "Bot" settings pane. You will configure this as the `IanLoginToken` below

## Running the bot in Docker

The easiest way to run the bot without having to worry about .NET Core framework is to load
it up in Docker.

### Using a pre-built image (e.g. x86)

1. `docker pull docker.pkg.github.com/k7hpn/discordian/discordian:latest`
2. `docker run --name bot --restart unless-stopped -e IanLoginToken=<token> discordian:latest`

### Building an image and using it (e.g. Raspberry Pi)

1. `git clone git@github.com:k7hpn/discordian.git && cd discordian`
2. `./docker-build.bash`
3. `docker run --name bot --restart unless-stopped -e IanLoginToken=<token> discordian:latest`

*Note*: `docker-build.bash` only performs the build step unless the branch is named
`master`, `develop`, or in a `release/0.0.0` format. If you want it to build the runtime
Docker image and your branch isn't named one of those, use the environment variable
`BLD_PUBLISH` to tell the script you want the publish stage as well, e.g.: `BLD_PUBLISH=true ./docker-build.bash`. You need the publish stage to run the bot.

# Configuring the bot

The following configuration items can be set in the `settings.json` file or passed in as
environment variables:

- IanCatFactEndpoint - URL to the cat facts service
- IanCommandChar - _optional_ - a command character for the bot to respond to in public
- IanImdbIdUrl - URL to an IMDB movie
- IanLoginToken - Discord bot token (see _Getting a key_ above)
- IanMapQuestEndpoint - URL to the MapQuest geocoding service, `{0}` is the location and `{1}` is the API key
- IanMapQuestKey - A [MapQuest API key](https://developer.mapquest.com)
- IanOmdbEndpoint - URL to the OMDB API endpoint
- IanOmdbKey - An [OMDB API key](http://www.omdbapi.com/apikey.aspx)
- IanOpenWeatherKey - An [OpenWeather API key](https://openweathermap.org/api)
- IanOpenWeatherMapEndpointCoords - OpenWeather API endpoint to query by coordinates, `{0}` is latitude, `{1}` is longitude, `{2}` is the OpenWeather API key
- IanOpenWeatherMapEndpointForecast - OpenWeather OneCall API endpoint, `{0}` is latitude, `{1}` is longitude, `{2}` is the OpenWeather API key
- IanOpenWeatherMapEndpointQ - OpenWeather API endpoint to query by city name, `{0}` is the city name, `{1}` is the OpenWeather API key
- IanUrbanDictionaryEndpoint - URL to the UrbanDictionary API, `{0}` is the search term
- IanUrbanDictionarySwap - _optional_ - a comma-separated list of search terms where the
first and second definitions should be swapped

# License

DiscordIan source code is distributed under The MIT License.

