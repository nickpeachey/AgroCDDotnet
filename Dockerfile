FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["AgroCDDotnet.Api/AgroCDDotnet.Api.csproj", "AgroCDDotnet.Api/"]
RUN dotnet restore "AgroCDDotnet.Api/AgroCDDotnet.Api.csproj"
COPY . .
WORKDIR "/src/AgroCDDotnet.Api"
RUN dotnet build "AgroCDDotnet.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AgroCDDotnet.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AgroCDDotnet.Api.dll"]
