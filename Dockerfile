FROM mcr.microsoft.com/dotnet/sdk:6.0 AS builder
WORKDIR /app

COPY . .

RUN dotnet restore "OAuthExample/OAuthExample.csproj"
RUN dotnet publish "OAuthExample/OAuthExample.csproj" -c Release -o /dist 

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runner
WORKDIR /app

COPY --from=builder /dist /app

ENTRYPOINT ["dotnet", "OAuthExample.dll"]
