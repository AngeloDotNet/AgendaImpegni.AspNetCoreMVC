# CLAUDE.md — Agenda Impegni

Questo file PREVALE sulle regole globali in caso di conflitto. Tienilo breve: solo ciò che è specifico del progetto.

## Regole
I moduli sono in `.claude/rules/` e Claude Code li carica da solo: `core.md`, `git.md` e `security.md` all'avvio; `frontend.md` (`**/*.{ts,html,css,scss}`), `backend.md` (`**/*.{cs,csproj,props}`) e `docker.md` (Dockerfile, compose, `docker/**`) quando si legge o modifica un file corrispondente (`paths:` nell'intestazione). I moduli opzionali stanno in `.claude/rules-optional/` (non si caricano): si attivano spostandoli in `.claude/rules/` e annotandolo qui. Per verificare cosa è caricato: `/context`. Non aggiungere altri `.md` in `.claude/rules/` se non sono regole: vengono caricati tutti.

- **Versione delle regole copiate:** `core.md` v3.10 del 2026-10-29
- **Moduli rimossi o non attivati (fuori ambito):** `feature-workflow` (da confermare), `backend-infra` (nessuna messaggistica né cache). `backend-email` è **da attivare** (spostare in `.claude/rules/`) prima di sviluppare l'invio email.

## Progetto
- **Scopo:** agenda personale con calendario, ricorrenze, notifiche email stile Google Calendar e pannello impostazioni.
- **Tipo:** fullstack
- **Stato:** esistente — migrazione dal prototipo ASP.NET Core MVC (`src/AgendaWithAspNetCoreMVC`, rimosso al cut-over, vedi `PIANO_MIGRAZIONE.md`) a Minimal API + Angular. Non migrare a zoneless né ristrutturare senza chiedere (per il nuovo frontend Angular vale invece zoneless).

## Scelte architetturali (decise una volta per progetto)
- **Architettura backend:** Clean Architecture
- **Livelli Clean Architecture:** Domain, Application, Infrastructure, Api (prefisso `Agenda.`)
- **Entità di riferimento:** nessuna ancora — `feature-workflow.md` non attivo
- **Modello del dominio:** ricco per `Event` (aggregato con ricorrenze, eccezioni, promemoria, partecipanti); anemico per le altre entità (`Category`, impostazioni)
- **Repository:** no (default)
- **Specification pattern:** no (default)
- **Gestione errori/risultati:** OperationResults (Marco Minerva)
- **Validazione backend:** FluentValidation
- **Esposizione del documento OpenAPI:** solo Development (UI Swagger solo in Development)
- **Tecnologia frontend:** Angular + PrimeNG
- **Sviluppo mobile:** no
- **ORM:** EF Core (default)
- **Database:** PostgreSQL — versione [da decidere]
- **Database nei test:** progetto `IntegrationTests` con database reale di prova PostgreSQL su istanza indicata dall'utente (un database per esecuzione); connection string da User Secrets o `ConnectionStrings__Test`. No Testcontainers.
- **Multi-tenancy:** no
- **Soft delete:** [da decidere]
- **Sicurezza backend:** nessuna per ora (endpoint pubblici, anche `/settings`); l'autenticazione verrà aggiunta in seguito, il gruppo `/settings` è predisposto per `RequireAuthorization()`. Meccanismo e granularità da decidere allora.
- **Livello ASVS:** 1 — da rivalutare quando verrà aggiunta l'autenticazione
- **Token JWT / Granularità / Sessione SPA:** n/a finché non c'è autenticazione
- **Cache:** nessuna per ora
- **File storage:** non usato
- **Invio email:** MailKit (da confermare all'attivazione di `backend-email.md`) — sezione `Smtp` in appsettings con valori segnaposto, valori reali solo da env/secret. Dal pannello sono modificabili host, porta, sicurezza e mittente (su DB); la password SMTP viene solo da variabile d'ambiente/secret (`Smtp__Password`), mai salvata su DB e mai esposta dall'API. Nessuna deroga alle regole.
- **Messaggistica:** non usata
- **Job in background:** Coravel (MIT, da verificare su .NET 10) — tipo: ricorrente (promemoria ogni minuto, `PreventOverlapping`) — retry: sì, con backoff (tentativi e tempi [da decidere])
- **Disaccoppiamento invio email:** job in background (Coravel)
- **Outbox/retry email:** outbox + retry (tentativi e backoff [da decidere])
- **Logging:** Serilog dietro `ILogger`, sink console (stdout); template testuale in Development e JSON compatto in produzione; livelli minimi per namespace in `appsettings.json`; arricchimento con `traceId`. Nessuna rotazione file (non c'è sink su file).
- **SMTP di prova (test/sviluppo):** Mailtrap (confermato) — credenziali da secret
- **Preferenze utente (tema, fuso orario, consenso cookie):** [da decidere: senza utenti non c'è profilo a database]
- **Lingue supportate:** [it, en] — lingua di default: [it]
- **Temi disponibili:** [light, dark]
- **Tailwind:** non usato
- **SSR / PWA:** SSR no; PWA sì (`@angular/service-worker`, passo 5.7 di `PIANO.md`)
- **Browser supportati:** default delle regole globali

## Nomi e struttura
- **Solution:** `AgendaImpegni.slnx` (formato esistente)
- **Namespace radice:** `Agenda`
- **Cartelle:** backend in `src/Agenda.Api` (più `Agenda.Domain`, `Agenda.Application`, `Agenda.Infrastructure`), frontend in `src/agenda-web`, test in `tests/`
- **File di configurazione .NET:** `.editorconfig` e `NuGet.config` [da decidere: tenuto | rimosso]
- **Prefisso selettori Angular:** `app` (da confermare)
- **Naming file Angular:** come da `frontend.md` §2 (nessuna deroga)
- **Micro frontend:** no

## Comandi
| Azione | Comando |
|---|---|
| Avvio ambiente locale (DB) | `docker compose up -d` |
| Build backend | `dotnet build` |
| Test backend | `dotnet test` |
| Avvio backend | `dotnet run --project src/Agenda.Api` |
| Installazione frontend | `npm ci` (in `src/agenda-web`) |
| Avvio frontend | `npm start` |
| Lint frontend | `npm run lint` |
| Test unitari frontend | `npm test` |
| Test E2E | `npx playwright test` |
| Generazione client API | [da definire: NSwag / OpenAPI Generator] |
| Nuova migrazione EF | `dotnet ef migrations add [Nome] --project src/Agenda.Infrastructure --startup-project src/Agenda.Api` |

## Operatività
- **Deploy:** sempre Dockerfile + docker-compose (regole in `docker.md`). Server gestiti dall'utente.
- **Ambienti:** [da decidere]
- **Reverse proxy / TLS:** [da decidere]
- **Porta applicazione nel container:** 8080
- **Servizi nel compose:** PostgreSQL (SMTP di prova: Mailtrap, esterno)
- **Osservabilità:** [da decidere]
- **Backup:** [da decidere]
- **RPO / RTO e retention backup:** [da decidere]
- **Scadenza/rotazione segreti:** [da decidere]
- **Soglie di performance diverse dal default:** nessuna

## Git
- **Modello branch (git flow):** git flow completo con `develop` (da creare a cura dell'utente) — Claude conferma il branch con te a ogni lavoro
- **Branch principale:** main
- **Strategia di merge:** come da `git.md` (squash verso `develop`, merge commit per ogni PR che coinvolge `main`)
- **Ticket nei branch/commit:** [da decidere]
- **Scope Conventional Commits ammessi:** [da decidere]
- **File della versione e changelog:** `Directory.Build.props` + `CHANGELOG.md` — `CHANGELOG.md` e `README.md` si aggiornano a ogni step del piano (`core.md` §5b)
- **Lingua della documentazione (README.md, CHANGELOG.md):** italiano
- **Fine riga:** [da decidere]
- **Hook pre-commit:** [da decidere]
- **Push, PR, tag, release ed esecuzione delle pipeline:** a cura dell'utente (Claude fa solo commit locali; può scrivere i file di pipeline)
- **Attribuzione Claude nei commit:** trailer Co-Authored-By

## Eccezioni alle regole globali
- Nessuna al momento.

## Note per Claude
- Il piano di migrazione è in `PIANO_MIGRAZIONE.md`; va riallineato a queste scelte e convertito in `PIANO.md` (formato di `core.md` §6.15).
- Dominio: un `Event` può essere singolo o ricorrente (RRULE iCalendar) con eccezioni per singola occorrenza (modifica "solo questo / questo e successivi / tutti"). Promemoria multipli per evento, partecipanti con email, notifiche email stile Google Calendar (inviti `.ics`, aggiornamenti, annullamenti, promemoria, riepilogo giornaliero opzionale).
- Il pannello impostazioni (SMTP, destinatario predefinito, promemoria predefiniti, fuso orario, categorie, opzioni calendario) è oggi pubblico.
- Date in UTC; fuso IANA per evento e fuso predefinito nelle impostazioni.
