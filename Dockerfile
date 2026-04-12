# Stage 1: Build Vue frontend
FROM node:22-alpine AS vue-build
WORKDIR /app/src/Web/vue-app
COPY src/Web/vue-app/package*.json ./
RUN npm ci
COPY src/Web/vue-app/ ./
COPY src/Web/vue-app/.env.main .env
COPY src/Web/vue-app/.env.main .env.production
RUN npm run build

# Stage 2: Build .NET app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-build
WORKDIR /app
COPY . .
COPY --from=vue-build /app/src/Web/wwwroot/vue ./src/Web/wwwroot/vue/
RUN dotnet publish src/Web -c Release -o /publish

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=dotnet-build /publish .
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "Web.dll"]
