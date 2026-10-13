FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/OrthoFlowAI.Domain/OrthoFlowAI.Domain.csproj src/OrthoFlowAI.Domain/
COPY src/OrthoFlowAI.Application/OrthoFlowAI.Application.csproj src/OrthoFlowAI.Application/
COPY src/OrthoFlowAI.Infrastructure/OrthoFlowAI.Infrastructure.csproj src/OrthoFlowAI.Infrastructure/
COPY src/OrthoFlowAI.Api/OrthoFlowAI.Api.csproj src/OrthoFlowAI.Api/
RUN dotnet restore src/OrthoFlowAI.Api/OrthoFlowAI.Api.csproj

COPY src/ src/
RUN dotnet publish src/OrthoFlowAI.Api/OrthoFlowAI.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

RUN addgroup --system --gid 1001 appgroup && \
    adduser --system --uid 1001 --ingroup appgroup appuser

COPY --from=build --chown=appuser:appgroup /app/publish .

USER appuser

ENTRYPOINT ["dotnet", "OrthoFlowAI.Api.dll"]
