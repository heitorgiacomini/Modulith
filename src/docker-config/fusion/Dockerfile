FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /workspace
COPY .config/dotnet-tools.json .config/dotnet-tools.json
RUN dotnet tool restore
COPY docker-config/fusion/compose-gateway.sh /workspace/compose-gateway.sh
ENTRYPOINT ["bash", "/workspace/compose-gateway.sh"]
