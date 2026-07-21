# 💰 Cashly Backend

API per **Cashly**, una moderna applicazione per la gestione delle finanze personali. Sviluppata per offrire un controllo completo su transazioni, categorie e abbonamenti con funzionalità di dashboard riassuntiva.

## 🚀 Panoramica Architetturale

Questo progetto espone un'API RESTful progettata per funzionare come backend della [Web App Angular](https://github.com/roberto-ingenito-home-lab/cashly-frontend).
È pensata per un'infrastruttura containerizzata e gira all'interno dell'ecosistema server homelab, configurata per operare dietro un reverse proxy (Nginx) al base path `/cashly-api/`.

## ⚙️ Stack Tecnologico

| Tecnologia                   | Descrizione                                  |
| ---------------------------- | -------------------------------------------- |
| **.NET 10**                  | Framework di sviluppo (ASP.NET Core Web API) |
| **Entity Framework Core 10** | ORM per l'interazione con il database        |
| **PostgreSQL**               | Database relazionale                         |
| **JWT (JSON Web Token)**     | Meccanismo di autenticazione                 |
| **BCrypt**                   | Hashing sicuro delle password                |
| **Swagger/OpenAPI**          | Documentazione automatica dell'API           |
| **Docker**                   | Containerizzazione (multi-stage build)       |

## 📦 Funzionalità Principali

- **Autenticazione & Utenti:** Registrazione, login (JWT), hashing delle password, recupero account (via email SMTP).
- **Gestione Transazioni:** Creazione, modifica, eliminazione e recupero delle entrate e uscite finanziarie.
- **Categorie:** Catalogazione delle transazioni per un'analisi finanziaria chiara.
- **Abbonamenti:** Tracciamento delle sottoscrizioni ricorrenti.
- **Dashboard:** Endpoints dedicati per metriche aggregate, statistiche e riepiloghi.
- **Mail Service:** Invio comunicazioni via SMTP.

## 📋 Prerequisiti

Per eseguire o sviluppare questo progetto, avrai bisogno di:

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [PostgreSQL](https://www.postgresql.org/) (o Docker per virtualizzarlo)
- [Docker](https://www.docker.com/) (opzionale, per deployment)

## 🛠️ Variabili d'Ambiente (.env)

Il progetto sfrutta il pacchetto `DotNetEnv` per caricare un file `.env` alla base. Le variabili chiave utilizzate includono:

```env
# Database PostgreSQL
CASHLY_POSTGRES_USER=tuouser
CASHLY_POSTGRES_PASSWORD=tuapassword
CASHLY_POSTGRES_DB=cashly_db

# Configurazione SMTP (per invio email)
SMTP_HOST=smtp.esempio.com
SMTP_PORT=587
SMTP_USERNAME=tua.email@esempio.com
SMTP_PASSWORD=password-smtp
SMTP_FROM_EMAIL=tua.email@esempio.com
SMTP_FROM_NAME=Cashly
```

_(Nota: I token JWT come `Jwt:Key`, `Jwt:Issuer` e `Jwt:Audience` sono configurati direttamente in `appsettings.json` o iniettati nell'ambiente)_

## 🏃 Setup & Esecuzione

### Esecuzione in Locale (Sviluppo)

1. Clona la repository:
   ```bash
   git clone https://github.com/roberto-ingenito-home-lab/cashly-backend.git
   ```
2. Ripristina le dipendenze:
   ```bash
   cd cashly-backend/cashly
   dotnet restore
   ```
3. Avvia il progetto (le migrazioni del database vengono applicate automaticamente all'avvio):
   ```bash
   dotnet run
   ```

### Esecuzione con Docker

Il progetto include un `Dockerfile` multi-stage pronto all'uso.

1. Costruisci l'immagine:
   ```bash
   docker build -t cashly-backend .
   ```
2. Avvia il container:
   ```bash
   docker run -p 5116:8080 --env-file .env cashly-backend
   ```

## 📖 Riferimento API

Se eseguito in ambiente di sviluppo, l'interfaccia **Swagger UI** sarà disponibile al seguente percorso (inclusivo del base path):
`http://localhost:<porta>/cashly-api/swagger`

Qui potrai esplorare gli endpoint protetti testando l'autenticazione tramite Bearer Token.

---

## 🔗 Progetti Correlati

- [Cashly Frontend](https://github.com/roberto-ingenito-home-lab/cashly-frontend) — Interfaccia utente Angular
- [Homelab Infrastructure](https://github.com/roberto-ingenito-home-lab/server-raspberry-pi) — Infrastruttura server e deployment Docker
