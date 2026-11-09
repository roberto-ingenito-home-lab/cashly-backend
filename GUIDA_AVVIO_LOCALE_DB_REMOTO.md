# Guida: Esecuzione Locale con Database Remoto

Questa guida descrive i passaggi per avviare l'applicazione in locale (Frontend Angular + Backend .NET) puntando al database PostgreSQL remoto.

---

## 1. Prerequisiti

- Il database PostgreSQL remoto deve essere raggiungibile (es. sulla rete locale all'indirizzo `192.168.1.100:5116`).
- Nessun processo precedente bloccato sulla porta `8080`. Se necessario, termina le istanze precedenti di `cashly`:
  ```powershell
  Get-Process -Name cashly -ErrorAction SilentlyContinue | Stop-Process
  ```

---

## 2. Avvio Backend (.NET)

Apri una finestra di PowerShell e posizionati nella cartella del backend:

```powershell
cd C:\Users\roberto\Documents\repos-server\cashly-backend\cashly
```

Imposta la variabile d'ambiente per sovrascrivere temporaneamente la Connection String senza modificare i file di configurazione git:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=192.168.1.100;Port=5116;Database=DATABASE;Username=USERNAME;Password=PASSWORD"
```

Avvia il backend sulla porta `8080` (utilizzata dal proxy frontend):

```powershell
dotnet run --urls http://localhost:8080
```

> [!NOTE]
> La variabile d'ambiente è attiva solo per la sessione del terminale PowerShell corrente, quindi non sporca il repository e non lascia credenziali salvate nei commit git.

---

## 3. Avvio Frontend (Angular)

Apri un secondo terminale PowerShell e posizionati nella cartella del frontend:

```powershell
cd C:\Users\roberto\Documents\repos-server\cashly-frontend
```

Avvia l'applicazione:

```powershell
npm start
```

- Il server di sviluppo si avvia su **`http://localhost:3000`**.
- Tramite il file `proxy.conf.json`, tutte le richieste verso `/cashly-api/*` vengono automaticamente inoltrate a `http://localhost:8080`.

---

## 4. Riepilogo Porte e Flusso

```
Browser (http://localhost:3000)
    │
    ▼
Frontend Angular Dev Server (:3000)
    │ (Proxy: /cashly-api)
    ▼
Backend ASP.NET Core (:8080)
    │ (Npgsql EF Core)
    ▼
PostgreSQL Remoto (192.168.1.100:5116)
```
