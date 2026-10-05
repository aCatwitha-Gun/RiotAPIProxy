FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RiotProxy.csproj", "./"]
RUN dotnet restore "RiotProxy.csproj"
COPY . .
RUN dotnet publish "RiotProxy.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RiotProxy.dll"]
