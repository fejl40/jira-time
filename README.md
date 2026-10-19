# Jira Time

A local timesheet for logging hours on Jira issues, in two-week periods like myTE.
Each entry records *when* the work happened (date, from, to), not just how long.

Blazor (Interactive Server, .NET 10) + Tailwind CSS v4.

## Run

Requires the .NET 10 SDK and Node.js (Node is only used to build the Tailwind CSS).

```sh
dotnet run
```

Opens http://localhost:5115. The first build runs `npm install` and compiles Tailwind
automatically. While editing styles, `npm run watch:css` alongside `dotnet watch` gives live CSS rebuilds.

## First start

1. **Connect**: Jira URL + token.
   - *Server / Data Center*: Personal Access Token, sent as `Bearer`.
   - *Cloud*: Atlassian email + API token, sent as Basic auth.
   The token is checked against Jira (`/myself`) before it is saved.
2. **Profile**: search for your Jira user. The token's owner is offered as a shortcut.

## Where data lives

| What | Where |
|---|---|
| Jira URL, auth type, email, **token** | Browser `localStorage` (`jira-time.connection`), encrypted by the server |
| Encryption keys | `data/keys/` on the server (DPAPI-protected on Windows) |
| Profile, period start | `data/documents/settings/app.json` |
| Time entries | `data/documents/time-entries/<id>.json`, one file per entry |

**Token encryption:** the server encrypts the connection with ASP.NET Core Data Protection
(`ProtectedLocalStorage`) before handing it to the browser. The browser only ever stores
ciphertext; the key never leaves `data/keys`. Copying the browser's storage gives nothing usable.
Deleting `data/keys` makes the stored token unreadable and the app asks you to connect again.

**Back up** the `data/` folder to keep your history. `data/` is git-ignored.

## Storage abstraction

The app only uses `IDocumentStore` / `IDocumentCollection<T>` (`Storage/IDocumentStore.cs`):
`GetAsync`, `QueryAsync(Expression<...>)`, `UpsertAsync`, `DeleteAsync`. Filters are
expressions so a database implementation can translate them into server-side queries.

The current implementation, `Storage/FileSystem/FileDocumentStore`, writes one JSON file per
document (atomic temp-file + move), caches each collection in memory, and reloads if files change
on disk. To move to a document database (MongoDB, Cosmos DB, RavenDB, ...), implement the two
interfaces and replace `AddFileDocumentStore(...)` in `Program.cs`.

## Periods

Periods are 14 days, repeating from an anchor date (default Monday 5 Jan 2026). Change it under
Settings → Timesheet periods so it lines up with your myTE periods.

## Layout

```
Program.cs                         DI: storage, data protection, Jira client
Domain/Documents.cs                AppSettings, TimeEntry, JiraProfile, Period, ...
Storage/                           document store interfaces + file-system implementation
Services/ConnectionState.cs        encrypted connection in browser localStorage (per tab/circuit)
Services/SettingsService.cs        profile + period settings
Services/TimeEntryService.cs       entries: query, save (with overlap check), delete
Services/JiraClient.cs             Jira REST calls (myself, user search, issue picker)
Components/Layout/MainLayout.razor header + redirect to setup when not configured
Components/Pages/Setup/            Connect, Profile
Components/Pages/Timesheet.razor   two-week grid + day view
Components/Pages/SettingsPage.razor
Components/Shared/                 Autocomplete, EntryEditor, DayTimeline, UserRow
Styles/app.css                     Tailwind input (compiled to wwwroot/app.css)
```

## Not done yet

- Pushing entries to Jira as worklogs (`TimeEntry.JiraWorklogId` is reserved for it).
- Importing existing worklogs from Jira.
