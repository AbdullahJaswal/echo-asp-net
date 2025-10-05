FROM mcr.microsoft.com/dotnet/sdk:9.0 AS dev

WORKDIR /app

EXPOSE 5160

ENV ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_URLS=http://+:5160 \
    DOTNET_USE_POLLING_FILE_WATCHER=1 \
    DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1

COPY Echo/Echo.csproj ./Echo/
RUN dotnet restore "./Echo/Echo.csproj"

WORKDIR /app/Echo

CMD ["dotnet", "watch", "run", "--no-launch-profile", "--urls", "http://0.0.0.0:5160", "--non-interactive"]