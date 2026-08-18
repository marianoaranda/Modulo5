FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Modulo5.Web/Modulo5.Web.csproj src/Modulo5.Web/
RUN dotnet restore src/Modulo5.Web/Modulo5.Web.csproj

COPY src/Modulo5.Web/ src/Modulo5.Web/
RUN dotnet publish src/Modulo5.Web/Modulo5.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Modulo5.Web.dll"]
