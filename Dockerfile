# Stage 1: build the React frontend
FROM node:24-alpine AS web-build
WORKDIR /src/Firesight.Web
COPY src/Firesight.Web/package.json src/Firesight.Web/package-lock.json ./
RUN npm ci
COPY src/Firesight.Web/ ./
RUN npm run build

# Stage 2: build and publish the .NET API (and everything it references)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src
COPY src/Firesight.Api/Firesight.Api.csproj src/Firesight.Api/
COPY src/Firesight.Application/Firesight.Application.csproj src/Firesight.Application/
COPY src/Firesight.Domain/Firesight.Domain.csproj src/Firesight.Domain/
COPY src/Firesight.Infrastructure/Firesight.Infrastructure.csproj src/Firesight.Infrastructure/
COPY src/Firesight.Mcp/Firesight.Mcp.csproj src/Firesight.Mcp/
RUN dotnet restore src/Firesight.Api/Firesight.Api.csproj
COPY src/Firesight.Api/ src/Firesight.Api/
COPY src/Firesight.Application/ src/Firesight.Application/
COPY src/Firesight.Domain/ src/Firesight.Domain/
COPY src/Firesight.Infrastructure/ src/Firesight.Infrastructure/
COPY src/Firesight.Mcp/ src/Firesight.Mcp/
RUN dotnet publish src/Firesight.Api/Firesight.Api.csproj -c Release -o /app/publish --no-restore

# Stage 3: runtime image — one container serves both the API and the built React app
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=api-build /app/publish ./
COPY --from=web-build /src/Firesight.Web/dist ./wwwroot
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Firesight.Api.dll"]
