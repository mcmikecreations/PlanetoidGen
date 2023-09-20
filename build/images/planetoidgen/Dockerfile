FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base

RUN apt-get update && \
    apt-get install -y libc6-dev libassimp-dev && \
    rm -rf /var/lib/apt/lists/*

CMD ["/bin/bash"]
