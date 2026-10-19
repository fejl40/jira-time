# Jira Time

A local timesheet for logging hours on Jira issues, in semi-monthly periods like myTE.
Each entry records *when* the work happened (date, from, to), not just how long.

Blazor (Interactive Server, .NET 10) + Tailwind CSS v4.

## Run

Requires the .NET 10 SDK and Node.js (Node is only used to build the Tailwind CSS).

```sh
dotnet run --project src/JiraTime
```

Opens http://localhost:5115. The first build runs `npm install` and compiles Tailwind
automatically. While editing styles, `npm run watch:css` alongside `dotnet watch` gives live CSS rebuilds.

## First start

1. **Connect**: Jira URL + token.
   - *Server / Data Center*: Personal Access Token, sent as `Bearer`.
   - *Cloud*: Atlassian email + API token, sent as Basic auth.
   The token is checked against Jira (`/myself`) before it is saved, and the user it belongs to
   becomes your profile. To log hours as someone else, connect with their token.

## Where data lives

| What | Where |
|---|---|
| Jira URL, auth type, email, **token** | Browser `localStorage` (`jira-time.connection`), encrypted by the server |
| Encryption keys | `data/keys/` on the server (DPAPI-protected on Windows) |
| Profile | `data/documents/settings/app.json` |
| Days off | `data/documents/days-off/<date>.json` |
| Time entries | `data/documents/time-entries/<id>.json`, one file per entry |

**Token encryption:** the server encrypts the connection with ASP.NET Core Data Protection
(`ProtectedLocalStorage`) before handing it to the browser. The browser only ever stores
ciphertext; the key never leaves `data/keys`. Copying the browser's storage gives nothing usable.
Deleting `data/keys` makes the stored token unreadable and the app asks you to connect again.

`data/` is created next to the project (`src/JiraTime/data/`). **Back up** the `data/` folder to keep your history. `data/` is git-ignored.

## Jira worklogs

Every entry is sent to Jira as a worklog as soon as it is saved; edits update it, moving an entry
to another issue moves the worklog, and deleting an entry deletes the worklog. If Jira can't be
reached the entry is still saved and shown as **Not in Jira** with the reason; retry it from the
day view or use **Sync now** in the period header.

## My sprint

The entry editor shows the issues assigned to you in an open sprint (not Done) as
quick picks, next to recently used issues. Tasks and stories both count. On Jira without sprints it
falls back to all open issues assigned to you. The list is cached for five minutes.

## Fill day

Time is logged per day: an entry is an issue, a day and a number of hours (`1.5`, `1,5` or `1:30`),
plus an optional comment. When during the day you did it doesn't matter. Jira needs a start time on
each worklog, so the app uses 09:00 on the entry's day.

**Fill day** in the day view suggests an entry with the hours the workday is still missing (up to
today), on a best-guess issue: the one you last logged that day, then your In Progress sprint issue,
then any sprint issue. Suggestions are drafts: change the issue or hours, remove them, then
**Accept** to save them (and send them to Jira) or **Discard**.

**Fill period** in the period header does the same for every short workday so far (never future
days). Suggested hours show dashed in the grid with a **Suggested** row; accept them all from the
header, or open a day to review and accept just that day.

## Theme

Light, dark or system (follows the OS). Switch with the icon in the header; the choice is kept in the browser.

## Storage abstraction

The app only uses `IDocumentStore` / `IDocumentCollection<T>` (`Storage/IDocumentStore.cs`):
`GetAsync`, `QueryAsync(Expression<...>)`, `UpsertAsync`, `DeleteAsync`. Filters are
expressions so a database implementation can translate them into server-side queries.

The current implementation, `Storage/FileSystem/FileDocumentStore`, writes one JSON file per
document (atomic temp-file + move), caches each collection in memory, and reloads if files change
on disk. To move to a document database (MongoDB, Cosmos DB, RavenDB, ...), implement the two
interfaces and replace `AddFileDocumentStore(...)` in `Program.cs`.

## Periods

Periods are semi-monthly: the 1st to the 15th, and the 16th to the last day of the month.

## Workdays

Monday to Friday need 8 hours. Danish public holidays are skipped automatically, and you can mark
a day off (vacation, sick, other) from the day view. The grid shows the hours still missing on each
workday up to today.

## Tests

```sh
dotnet test
```

xUnit tests in `tests/JiraTime.Tests` cover periods, the file document store, time entry rules, workdays, worklog sync, sprint issues and fill suggestions.

## Contributing

Tasks are tracked with [Backlog.md](https://github.com/MrLesk/Backlog.md) in `backlog/`
(`npm i -g backlog.md`, then `backlog board` or `backlog browser`). The git policy is in `CLAUDE.md`.

## Layout

Paths below are relative to `src/JiraTime/`.

```
Program.cs                         DI: storage, data protection, Jira client
Domain/Documents.cs                AppSettings, TimeEntry, JiraProfile, Period, ...
Domain/WorkCalendar.cs             day kinds, Danish holidays, days off
Domain/DayFiller.cs                suggestions for a day's missing hours
Storage/                           document store interfaces + file-system implementation
Services/ConnectionState.cs        encrypted connection in browser localStorage (per tab/circuit)
Services/SettingsService.cs        profile
Services/TimeEntryService.cs       entries: query, save (with validation), delete
Services/JiraClient.cs             Jira REST calls (myself, issue picker, sprint search, worklogs)
Services/JiraSetup.cs              connect: check the token, take its user as the profile
Services/WorklogSync.cs            keeps Jira worklogs in step with entries
Services/SprintIssues.cs           my open-sprint issues, cached briefly
Components/Layout/MainLayout.razor header + redirect to setup when not configured
Components/Pages/Setup/            Connect
Components/Pages/Timesheet.razor   period grid + day view
Components/Pages/SettingsPage.razor
Components/Shared/                 Autocomplete, EntryEditor, QuickPicks, DraftList, UserRow
Styles/app.css                     Tailwind input (compiled to wwwroot/app.css)
```

## Not done yet

See `backlog/tasks/`.
