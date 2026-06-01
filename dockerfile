# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia il file di soluzione (.sln) e i file di progetto (.csproj)
# per ottimizzare la cache di Docker.
COPY cashly.sln .
COPY cashly/cashly.csproj cashly/

# Ripristina le dipendenze per l'intero progetto.
RUN dotnet restore "cashly.sln"

# Copia il resto dei file del progetto.
COPY . .

# RUN dotnet tool install --global dotnet-ef
# ENV PATH="$PATH:/root/.dotnet/tools"
# RUN dotnet ef database update --project cashly/cashly.csproj

# Pubblica l'applicazione in modalità Release.
# La cartella di destinazione `publish` si troverà all'interno del container.
WORKDIR "/src/cashly"
RUN dotnet publish "cashly.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: final
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copia i file pubblicati dal primo stage.
COPY --from=build /app/publish .

# Imposta l'entrypoint per avviare l'applicazione.
ENTRYPOINT ["dotnet", "cashly.dll"]