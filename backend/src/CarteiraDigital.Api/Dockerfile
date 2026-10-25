FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/CarteiraDigital.Core/*.csproj src/CarteiraDigital.Core/
COPY src/CarteiraDigital.Api/*.csproj src/CarteiraDigital.Api/
RUN dotnet restore src/CarteiraDigital.Api/CarteiraDigital.Api.csproj
COPY src/ src/
RUN dotnet publish src/CarteiraDigital.Api/CarteiraDigital.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__Default="Data Source=/data/carteira.db"
RUN mkdir -p /data && chown app:app /data
VOLUME /data
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "CarteiraDigital.Api.dll"]
