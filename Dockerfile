FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/CareerCopilot.Domain/CareerCopilot.Domain.csproj src/CareerCopilot.Domain/
COPY src/CareerCopilot.Application/CareerCopilot.Application.csproj src/CareerCopilot.Application/
COPY src/CareerCopilot.Infrastructure/CareerCopilot.Infrastructure.csproj src/CareerCopilot.Infrastructure/
COPY src/CareerCopilot.AI/CareerCopilot.AI.csproj src/CareerCopilot.AI/
COPY src/CareerCopilot.Api/CareerCopilot.Api.csproj src/CareerCopilot.Api/
RUN dotnet restore src/CareerCopilot.Api/CareerCopilot.Api.csproj

COPY src/CareerCopilot.Domain/ src/CareerCopilot.Domain/
COPY src/CareerCopilot.Application/ src/CareerCopilot.Application/
COPY src/CareerCopilot.Infrastructure/ src/CareerCopilot.Infrastructure/
COPY src/CareerCopilot.AI/ src/CareerCopilot.AI/
COPY src/CareerCopilot.Api/ src/CareerCopilot.Api/
COPY CareerCopilot.slnx ./

RUN dotnet publish src/CareerCopilot.Api/CareerCopilot.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

RUN rm -f appsettings.json appsettings.*.json

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_contentRoot=/app
EXPOSE 8080

ENTRYPOINT ["dotnet", "CareerCopilot.Api.dll"]
