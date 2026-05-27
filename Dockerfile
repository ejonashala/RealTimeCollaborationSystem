FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["RealTimeCollaborationSystem.sln", "./"]
COPY ["RealTimeCollaborationSystem/RealTimeCollaborationSystem.csproj", "RealTimeCollaborationSystem/"]
RUN dotnet restore "RealTimeCollaborationSystem/RealTimeCollaborationSystem.csproj"

COPY . .
RUN dotnet publish "RealTimeCollaborationSystem/RealTimeCollaborationSystem.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RealTimeCollaborationSystem.dll"]
