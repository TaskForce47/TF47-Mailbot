FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["TF47-Mailbot/TF47-Mailbot.csproj", "TF47-Mailbot/"]
RUN dotnet restore "TF47-Mailbot/TF47-Mailbot.csproj"
COPY . .
WORKDIR "/src/TF47-Mailbot"
RUN dotnet build "TF47-Mailbot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TF47-Mailbot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TF47-Mailbot.dll"]
