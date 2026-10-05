FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["RiotAPIProxy.csproj", "./"]
RUN dotnet restore "RiotAPIProxy.csproj"
COPY . .
RUN dotnet publish "RiotAPIProxy.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RiotAPIProxy.dll"]
