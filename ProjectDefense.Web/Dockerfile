#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ProjectDefense.Web/ProjectDefense.Web.csproj", "ProjectDefense.Web/"]
COPY ["ProjectDefense.Shared/ProjectDefense.Shared.csproj", "ProjectDefense.Shared/"]
RUN dotnet restore "./ProjectDefense.Web/ProjectDefense.Web.csproj"
COPY . .
WORKDIR "/src/ProjectDefense.Web"
RUN dotnet build "./ProjectDefense.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ProjectDefense.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER root
RUN chown -R app:app /app
USER app
ENTRYPOINT ["dotnet", "ProjectDefense.Web.dll"]
