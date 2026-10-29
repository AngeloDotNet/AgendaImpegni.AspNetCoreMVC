# PIANO — Migrazione Agenda Impegni a Minimal API + Angular

> Sostituisce nella pratica `PIANO_MIGRAZIONE.md` (copia storica, non più aggiornata). Scelte in `CLAUDE.md`; regole in `.claude/rules/`.
> Stati: `[ ]` da fare · `[~]` in corso · `[x]` completato (solo dopo build senza warning e test verdi). Un passo alla volta, con ok esplicito dell'utente prima del successivo (`core.md` §6.15).

## Obiettivo
Sostituire il prototipo ASP.NET Core MVC (`src/AgendaWithAspNetCoreMVC`, dati in memoria) con:
- backend **.NET 10 Minimal API**, Clean Architecture (`Agenda.Domain/Application/Infrastructure/Api`), EF Core + PostgreSQL;
- frontend **Angular + PrimeNG** con calendario FullCalendar;
- **notifiche email** stile Google Calendar (inviti `.ics`, aggiornamenti, annullamenti, promemoria, riepilogo giornaliero opzionale) via MailKit, Coravel, outbox + retry;
- **pannello impostazioni** pubblico (predisposto per `RequireAuthorization()` in futuro).

## Assunzioni
- Date in UTC; fuso IANA per evento e predefinito nelle impostazioni.
- Nessuna autenticazione, nessuna multi-tenancy, nessuna cache, ASVS livello 1.
- Ricorrenze RRULE (libreria da verificare: `Ical.Net` o `Rrule`) con eccezioni per occorrenza; promemoria multipli; partecipanti con email.
- SMTP: host/porta/sicurezza/mittente su DB e modificabili dal pannello; password solo da `Smtp__Password`. SMTP di prova: Mailtrap.
- Integration test su istanza PostgreSQL di prova indicata dall'utente (no Testcontainers).
- Git flow con `develop` (da creare a cura dell'utente); branch confermato con l'utente a ogni lavoro; commit solo dopo ok.

## Decisioni aperte (da chiedere al momento opportuno, una per volta)
- Versione di PostgreSQL; soft delete; sede delle preferenze utente (tema/fuso) senza profilo.
- `.editorconfig`/`NuGet.config`; prefisso selettori Angular; comando di generazione del client API.
- Tentativi e backoff di outbox/retry; ticket nei commit; scope Conventional Commits; fine riga; hook pre-commit.
- Ambienti, reverse proxy/TLS, osservabilità, backup, RPO/RTO, scadenza segreti, cifratura chiavi Data Protection (se usata), nonce/hash CSP per l'SPA statica, proxy fidati.
- Conferma di MailKit e attivazione di `backend-email.md`; conferma di `feature-workflow.md`.
- Strumento per i test di architettura (`NetArchTest.Rules` o `ArchUnitNET`) e verifica licenze/versioni di tutte le nuove dipendenze (`core.md` §3).

## Passi
Ogni passo che cambia comportamento, configurazione o struttura include l'aggiornamento di `README.md` e `CHANGELOG.md` (italiano).

### Fase 0 — Fondamenta
- [ ] **0.1 Branch e igiene repo** — conferma branch (es. `feature/migration-bootstrap` da `develop`), `.gitignore`, `.gitattributes` (decisione aperta), segnaposto `.env.example`. File: `.gitignore`, `.env.example`. Test: nessuno.
- [ ] **0.2 Scaffold soluzione** — `AgendaImpegni.slnx`, `Directory.Build.props` (esistente), `Directory.Packages.props`, `global.json`, lock file; progetti `Agenda.Domain/Application/Infrastructure/Api` con riferimenti Clean; `Program.cs` con `class Program` e `Main` esplicito + metodi di estensione. File: `src/Agenda.*`. Test: build senza warning (`TreatWarningsAsErrors`).
- [ ] **0.3 Test di base** — progetti `Agenda.UnitTests` e `Agenda.IntegrationTests` (xUnit, `WebApplicationFactory<Program>`); test di architettura sulla regola delle dipendenze. Test: test di architettura verdi.
- [ ] **0.4 Docker di sviluppo** — `docker-compose.yml` + `override` con PostgreSQL (volume nominato, healthcheck), `.dockerignore`, `.env.example`. Test: `docker compose config`, stack avviato e healthy.
- [ ] **0.5 CI** — workflow build+test .NET e `ng build/test/lint`, audit dipendenze (`dotnet list package --vulnerable --include-transitive`, `npm audit`), CodeQL, gitleaks, Dependabot. File: `.github/workflows/*`. (Esecuzione a cura dell'utente.)
- [ ] **0.6 Logging, errori, versionamento** — Serilog su console (template/JSON), `IExceptionHandler` + `ProblemDetails` con `traceId`, `OperationResults` (Minerva) con mappatura 400/404/409/412/422, `Asp.Versioning` (`/api/v1`), OpenAPI nativo + Swagger UI solo Development, health `/health/live` e `/health/ready`, CORS whitelist, security headers, rate limiting, `UseForwardedHeaders`. Test: unit + integration (header, CORS, health, mappatura errori, input malevoli).

### Fase 1 — Dominio e dati
- [ ] **1.1 Dominio** — `Event` (aggregato ricco: factory, ricorrenza RRULE, eccezioni per occorrenza, promemoria multipli, partecipanti, invarianti, eccezioni di dominio), `EventException`, `Reminder`, `Attendee`, `Category`, impostazioni; enum come stringhe. File: `src/Agenda.Domain`. Test: unit puri sul dominio.
- [ ] **1.2 Persistenza** — `IApplicationDbContext` (Application), `DbContext` + `IEntityTypeConfiguration` (Infrastructure), `snake_case`, UTC `timestamptz`, `xmin` come concurrency token, indici, outbox email (sostituisce il `NotificationLog`, con indice univoco evento+occorrenza+reminder+destinatario), migrazione iniziale, seed categorie di riferimento. Test: integration su PostgreSQL di prova.
- [ ] **1.3 Dizionario traduzioni** — tabella Chiave-Valore-Lingua (it/en), endpoint `/api/v1/localization/dictionary?lang=`, ETag, chiavi di errore del backend (seed idempotente). Test: integration.

### Fase 2 — Casi d'uso (Application)
- [ ] **2.1 Ricorrenze** — `IRecurrenceExpander` (finestra `[from,to]`, eccezioni, scope this/following/all, DST, fine mese, `until` senza ora). Test: unit estesi (vedi difetti noti del mock).
- [ ] **2.2 Eventi** — servizi CRUD/ricerca/move, DTO `record`, validatori FluentValidation con chiavi (regole di `CalendarEventRequest`: titolo ≤200, End≥Start, intervallo 1–999, ecc.), mapping con metodi di estensione, colore (richiesta → categoria → `#0d6efd`). Test: unit + integration.
- [ ] **2.3 Categorie e impostazioni** — CRUD categorie; impostazioni (SMTP senza password, destinatario predefinito, promemoria predefiniti, fuso, vista, orari, inviti, riepilogo giornaliero). Test: unit + integration.

### Fase 3 — API
- [ ] **3.1 Endpoint eventi e categorie** — `MapGroup`, commenti XML, `Produces/ProducesProblem`, paginazione/ordinamento dove serve. Test: integration, input malevoli, errori 400/404/409/412/422.
- [ ] **3.2 Endpoint impostazioni** — gruppo `/settings` (predisposto per autorizzazione), `POST /settings/smtp/test`, fusi orari. Test: integration (password mai restituita).
- [ ] **3.3 Schema OpenAPI versionato e test di contratto** — schema committato, test di incompatibilità.

### Fase 4 — Notifiche email
- [ ] **4.1 Attivazione modulo email** — sposta `backend-email.md` in `.claude/rules/`, conferma MailKit, sezione `Smtp` con segnaposto, `IEmailSender`/`MailKitEmailSender`, TLS valido, `App:PublicBaseUrl`. Test: integration con sender fittizio.
- [ ] **4.2 Template e `.ics`** — inviti (REQUEST), aggiornamenti, annullamenti (CANCEL), promemoria, riepilogo giornaliero; HTML localizzato con encoding dei valori utente. Test: unit sui template e sui `.ics`.
- [ ] **4.3 Outbox, job Coravel e retry** — verifica licenza/versione Coravel; `SendRemindersJob` (ogni minuto, `PreventOverlapping`, idempotente), outbox con retry/backoff e lock per più repliche. Test: integration (nessun doppio invio, ripresa dopo riavvio, retry).

### Fase 5 — Frontend
- [ ] **5.1 Workspace Angular** — `src/agenda-web` (zoneless, standalone, OnPush, PrimeNG, ESLint+Prettier, Vitest, Playwright), `.nvmrc`/`engines`, `config.json` a runtime, `TranslationService`, `ThemeService` (light/dark), `TimeZoneService`, interceptor errori e `traceId`, pagine di sistema 404/403/500. Test: unit.
- [ ] **5.2 Client API** — generazione dallo schema OpenAPI in `api/generated/` (script npm). Test: build.
- [ ] **5.3 Calendario** — FullCalendar (`@fullcalendar/angular`), fetch per finestra, drag/resize, sidebar (ricerca, categoria, eventi del giorno), responsive 360/768/1280. Test: unit + Playwright.
- [ ] **5.4 Dialog evento** — tab Dettagli/Note/Promemoria con form tipizzato, editor ricorrenza, promemoria multipli, partecipanti, scelta scope, mappatura errori server sui campi. Test: unit + Playwright + axe.
- [ ] **5.5 Pannello impostazioni** — Generali, Email/SMTP (mail di prova, password non modificabile), Notifiche, Categorie, Calendario; guard modifiche non salvate. Test: unit + Playwright + axe.
- [ ] **5.6 Traduzioni** — chiavi IT/EN per tutta la UI, test sulle chiavi mancanti.

### Fase 6 — Deploy
- [ ] **6.1 Dockerfile e compose di produzione** — multi-stage backend e frontend (nginx non root, `config.json` da env, header di sicurezza), `docker-compose.prod.yml`, servizio one-shot per le migrazioni (bundle EF), `db-backup` con `ops/backup.sh`/`restore.sh`, hardening, healthcheck. Test: `docker compose config`, build immagini, stack avviato.

### Fase 7 — Cut-over e chiusura
- [ ] **7.1 Parità funzionale** — confronto con l'app MVC, security review (`/security-review`), `nuget-license` e `license-checker`.
- [ ] **7.2 Rimozione del vecchio progetto** — elimina `src/AgendaWithAspNetCoreMVC` (`HomeController`, PageModel, `CalendarEventDto`, codice commentato) e aggiorna `.slnx`, `.github/copilot.instructions.md`.
- [ ] **7.3 Documentazione finale** — `README.md` (modello `SagreEventi.SQLServer`), `CHANGELOG.md`, ADR (`docs/adr/`) per le scelte che deviano dalle regole.

### Futuro (fuori scope)
Autenticazione su `/settings` e `UserId` sulle entità; rivalutazione del livello ASVS.

## Riferimenti dal prototipo
`Services/ICalendarService.cs` (casi d'uso), `Services/MockCalendarService.cs` (ricerca, colore, seed), `Models/CalendarEventRequest.cs` (validazione), `Controllers/CalendarApiController.cs` (rotte e proiezione FullCalendar), `wwwroot/js/agenda.js` e `Views/Agenda/*` (comportamento UI).

## Verifica end-to-end
`dotnet build` e `dotnet test` senza warning; `npm run lint`/`npm test`/`npx playwright test` verdi; `docker compose up -d`; creare, spostare, modificare ed eliminare eventi singoli e ricorrenti (scope this/following/all); invio invito con `.ics` e promemoria su Mailtrap, una sola volta anche dopo riavvio del job; mail di prova dal pannello.
