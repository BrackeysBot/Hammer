FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Hammer/Hammer.csproj", "Hammer/"]
RUN dotnet restore "Hammer/Hammer.csproj"
COPY . .
WORKDIR "/src/Hammer"
RUN dotnet build "Hammer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Hammer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hammer.dll"]
