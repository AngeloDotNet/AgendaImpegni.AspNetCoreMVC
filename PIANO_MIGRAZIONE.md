# Piano di migrazione: Agenda MVC → .NET Minimal API + Angular

## Context
Il progetto attuale (`src/AgendaWithAspNetCoreMVC`, net10.0) è un prototipo: ASP.NET Core MVC + FullCalendar 6 (vanilla JS), dati in memoria (`MockCalendarService`), nessun DB, auth, test o logica di promemoria (esiste solo l'enum `CalendarReminderType`). Si vuole una nuova soluzione con backend **Minimal API**, frontend **Angular**, **notifiche email in stile Google Calendar** e un **pannello impostazioni** (inizialmente pubblico, proteggibile in seguito).

Decisioni confermate: **PostgreSQL** (EF Core), **SMTP configurabile + MailKit**, destinatari = **email predefinita nelle impostazioni + partecipanti per evento**, ricorrenze **RRULE + eccezioni per occorrenza**.

**Prerequisito:** le regole di sviluppo verranno aggiunte in `.claude/rules` (ora la cartella non esiste) prima di iniziare. Prima della Fase 0 leggerle e adeguare questo piano (naming, struttura, test, stile). Le convenzioni attuali in `.github/copilot.instructions.md` (file-scoped namespace, primary constructors, collection expressions, `ThrowIfNull`, pochi commenti) valgono come default.

## Struttura soluzione target
```
AgendaImpegni.slnx
Directory.Build.props                  (esistente, ArtifactsPath)
src/
  Agenda.Api/                          Minimal API (host, endpoint, DI, Program.cs)
  Agenda.Domain/                       Entità, enum, value object (nessuna dipendenza)
  Agenda.Application/                  Servizi, DTO, validatori, interfacce (IEmailSender, IClock)
  Agenda.Infrastructure/               EF Core/Npgsql, migrazioni, MailKit, job Coravel
  agenda-web/                          Angular (workspace CLI)
tests/
  Agenda.Domain.Tests/ Agenda.Application.Tests/ Agenda.Api.Tests/ (xUnit + Testcontainers PostgreSQL)
docker-compose.yml                     postgres + mailpit (dev)
```
`src/AgendaWithAspNetCoreMVC` resta fino al cut-over (Fase 7), poi viene rimosso.

## Modello dati (EF Core, UTC ovunque)
- `Event`: Id, Title, Description, Location, Notes, StartUtc, EndUtc, AllDay, TimeZoneId (IANA), Color, CategoryId?, `RecurrenceRule` (RRULE string, null se singolo), RecurrenceUntil derivato, CreatedAt/UpdatedAt.
- `EventException`: Id, EventId, OriginalStartUtc, IsCancelled, override campi (Title, StartUtc, EndUtc, ecc.) → "solo questo evento".
- `Category`: Id, Name, Color (+ CRUD, oggi solo lettura).
- `Reminder`: Id, EventId, `MinutesBefore`, `Method` (Email). Sostituisce l'enum singolo `ReminderType` (migrazione dati: AtTime=0, 5, 15, 30, 60, 1440); consente promemoria multipli come Google.
- `Attendee`: Id, EventId, Email, Name?, ResponseStatus (NeedsAction/Accepted/Declined).
- `NotificationLog`: Id, EventId, OccurrenceStartUtc, ReminderId, Recipient, SentAtUtc, Status, Error — garantisce idempotenza (indice univoco su evento+occorrenza+reminder+destinatario).
- `AppSettings` (riga singola chiave/valore tipizzata): SMTP (host, port, security, user, password cifrata con Data Protection, from name/address), email predefinita destinatario, promemoria predefiniti, fuso orario predefinito, primo giorno settimana, vista iniziale, orari lavorativi, invio inviti on/off, riepilogo giornaliero on/off + ora.

## API (Minimal API, gruppi + `MapGroup`, un file di endpoint per risorsa)
Prefisso `/api/v1`, OpenAPI (`Microsoft.AspNetCore.OpenApi` + Scalar), ProblemDetails, validazione con filtro endpoint + FluentValidation (sostituisce `IValidatableObject` di `CalendarEventRequest`, riportando le stesse regole: titolo ≤200, End≥Start, intervallo 1–999, ecc., messaggi in italiano).
- `GET /events?from&to&query&categoryId` → occorrenze espanse nella finestra [from,to] (non più "oggi+6 mesi"), con `eventId` + `occurrenceStart` per distinguere le istanze della serie.
- `GET /events/{id}`, `POST /events`, `PUT /events/{id}?scope=this|following|all&occurrenceStart=`, `DELETE /events/{id}?scope=…`, `POST /events/{id}/move` (drag/resize con scope).
- `GET/POST/PUT/DELETE /categories`.
- `GET/PUT /settings`, `POST /settings/smtp/test` (invia mail di prova), `GET /settings/timezones`.
- `GET /health`.
- CORS per origine Angular in dev; in produzione Angular servito dallo stesso host (`UseStaticFiles` + fallback `index.html`) o reverse proxy.
- Gruppo `/settings` esposto tramite un gruppo dedicato così da applicare `.RequireAuthorization()` in un colpo solo in futuro; `AddAuthentication/Authorization` già predisposti con policy `Admin` che oggi consente tutti (feature flag `Auth:Enabled=false`).
- Espansione RRULE: libreria `Ical.Net` (o `Rrule`) incapsulata in `IRecurrenceExpander` in Application; correggere i difetti noti (ultimo giorno escluso con `until` senza ora, deriva di `AddMonths`).

## Notifiche email (stile Google Calendar)
- Job Coravel `SendRemindersJob : IInvocable` (scheduler `EveryMinute()`, `PreventOverlapping`; licenza MIT e compatibilità .NET 10 da verificare al primo uso): per i prossimi N minuti calcola le occorrenze con reminder in scadenza (via `IRecurrenceExpander`), esclude quelle già in `NotificationLog`, invia e registra. Resiliente a riavvii (recupero finestra mancata, limite retry con backoff).
- Tipi di email: **invito** (alla creazione/aggiunta partecipante, con allegato `.ics` METHOD:REQUEST), **aggiornamento**, **annullamento** (METHOD:CANCEL), **promemoria** prima dell'evento, **riepilogo giornaliero** opzionale.
- `IEmailSender` (MailKit) legge le impostazioni SMTP a ogni invio (modificabili da pannello senza riavvio); template HTML responsive (Razor/Scriban) in italiano con pulsanti riepilogo evento, luogo, data nel fuso corretto.
- Dev: Mailpit in docker-compose (SMTP 1025, UI 8025).

## Frontend Angular (`agenda-web`)
- Angular ultima LTS, standalone components, signals, lazy routes, Reactive Forms, `HttpClient` con interceptor errori (ProblemDetails), Bootstrap 5.3 (coerente con l'UI attuale) , i18n italiano.
- Client API tipizzato generato da OpenAPI (es. `ng-openapi-gen`), così i DTO restano allineati.
- Routing: `/agenda` (calendario + sidebar) e `/impostazioni` (nessun guard ora; punto unico `canActivate` pronto per il futuro).
- Porting da `wwwroot/js/agenda.js`, `Views/Agenda/Index.cshtml`, `_Sidebar.cshtml`, `_EventModal.cshtml`:
  - `CalendarComponent` con `@fullcalendar/angular` 6.x (stesse opzioni: locale `it`, `firstDay:1`, viste mese/settimana/giorno/lista, weekNumbers, nowIndicator, businessHours — ora lette da `/settings`), fetch eventi tramite `datesSet` con from/to.
  - `SidebarComponent` (ricerca, filtro categoria, eventi del giorno).
  - `EventDialogComponent` a 3 tab (Dettagli, Note, Promemoria) con form reattivo; nuovi: editor ricorrenza (stile Google: giornaliera/settimanale con giorni/mensile/annuale/personalizzata, fine mai/data/N volte), promemoria multipli, partecipanti (chip email), dialog scelta scope ("Solo questo / Questo e successivi / Tutti").
  - Fix dei difetti attuali: `recurrenceInterval/Until` e colore ora tornano nel DTO e vengono ripristinati in modifica.
- Pannello Impostazioni: sezioni Generali, Email/SMTP (con "Invia mail di prova"), Notifiche e promemoria predefiniti, Categorie (CRUD), Calendario (vista, orari, fuso).

## Fasi
0. **Prerequisiti**: leggere `.claude/rules`, scaffold soluzione, docker-compose (postgres+mailpit), CI (estendere `.github/workflows`: build+test .NET e `ng build/test`).
1. **Domain/Infrastructure**: entità, DbContext, migrazione iniziale, seed categorie/eventi demo (equivalenti ai 3 eventi del mock), test di persistenza con Testcontainers.
2. **Application**: servizi eventi/categorie/impostazioni, `IRecurrenceExpander` con test estensivi (DST, fine mese, until, eccezioni, scope following).
3. **Minimal API**: endpoint, validazione, OpenAPI, ProblemDetails, test di integrazione (WebApplicationFactory).
4. **Frontend base**: workspace Angular, client generato, calendario + sidebar + dialog evento a parità di funzioni con l'MVC.
5. **Notifiche**: MailKit, template, `.ics`, worker, `NotificationLog`, impostazioni SMTP + test mail.
6. **Pannello impostazioni** completo + ricorrenze/partecipanti/promemoria avanzati nella UI.
7. **Cut-over**: verifica parità funzionale, rimozione del progetto MVC e dei residui (`HomeController`, PageModel, `CalendarEventDto`, codice commentato, `agenda.css` mancante), README con istruzioni di avvio, aggiornamento `.slnx`.
8. **(Futuro, fuori scope)** Autenticazione (OIDC/Identity) su `/settings` e `UserId` sulle entità.

## File esistenti da riusare come riferimento
- `Services/ICalendarService.cs` → contratto dei casi d'uso da mantenere.
- `Services/MockCalendarService.cs` → regole di ricerca (Title/Description/Location/Notes, categoria, giorno) e logica colore (richiesta → categoria → `#0d6efd`) e seed.
- `Models/CalendarEventRequest.cs` → regole di validazione da portare in FluentValidation.
- `Controllers/CalendarApiController.cs` → rotte e proiezione FullCalendar (`ToFullCalendarResult`).
- `wwwroot/js/agenda.js`, `Views/Agenda/*` → comportamento UI da replicare in Angular.

## Verifica
- `dotnet build` e `dotnet test` verdi (unit + integrazione con Testcontainers); `ng build` e `ng test` verdi.
- `docker compose up -d`, avvio API + `ng serve`: creare/modificare/trascinare/eliminare eventi singoli e ricorrenti (scope this/following/all), verifica filtro e ricerca.
- Impostare SMTP verso Mailpit dal pannello, "Invia mail di prova"; creare evento con partecipante → invito con `.ics` visibile in Mailpit; evento con promemoria a 1 min → email ricevuta una sola volta (anche dopo riavvio del worker).
- Controllo parità funzionale con l'app MVC prima del cut-over.
