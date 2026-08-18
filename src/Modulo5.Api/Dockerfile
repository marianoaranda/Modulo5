FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Modulo5.Domain/Modulo5.Domain.csproj src/Modulo5.Domain/
COPY src/Modulo5.Data/Modulo5.Data.csproj src/Modulo5.Data/
COPY src/Modulo5.Api/Modulo5.Api.csproj src/Modulo5.Api/
RUN dotnet restore src/Modulo5.Api/Modulo5.Api.csproj

COPY src/Modulo5.Domain/ src/Modulo5.Domain/
COPY src/Modulo5.Data/ src/Modulo5.Data/
COPY src/Modulo5.Api/ src/Modulo5.Api/
RUN dotnet publish src/Modulo5.Api/Modulo5.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Modulo5.Api.dll"]
