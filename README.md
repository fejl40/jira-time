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

## Several users

Several people can use one Jira Time server, each in their own browser with their own token. A
user is the Jira host plus the user id that host returns for the token: on every visit the server
asks Jira who the token belongs to, so nobody can see or change someone else's hours, and the same
user id on two Jira hosts counts as two users. If Jira can't be reached, a token Jira confirmed
before still works (the header shows **Offline**), so you can keep logging and sync later. A token
Jira rejects sends you back to Connect.

Data saved before this existed has no owner. The first time its user signs in (the user in the old
profile setting), it is given to them on the host they sign in to. A copy of the old data is stored
in `data/documents/backups/` first, and nothing is deleted.

## Where data lives

| What | Where |
|---|---|
| Jira URL, auth type, email, **token** | Browser `localStorage` (`jira-time.connection`), encrypted by the server |
| Encryption keys | `data/keys/` on the server (DPAPI-protected on Windows) |
| Users Jira confirmed (profile, hashes of confirmed tokens) | `data/documents/users/<owner>.json` |
| Days off (whole or partial) | `data/documents/days-off/<owner>-<date>.json` |
| Work hours per weekday | `data/documents/user-settings/<owner>.json` |
| Time entries | `data/documents/time-entries/<id>.json`, one file per entry, with its owner |
| Copy of the data from before multi-user support | `data/documents/backups/` |

**Token encryption:** the server encrypts the connection with ASP.NET Core Data Protection
(`ProtectedLocalStorage`) before handing it to the browser. The browser only ever stores
ciphertext; the key never leaves `data/keys`. Copying the browser's storage gives nothing usable.
Deleting `data/keys` makes the stored token unreadable and the app asks you to connect again.

`data/` is created next to the project (`src/JiraTime/data/`). **Back up** the `data/` folder to keep your history. `data/` is git-ignored.

## Jira worklogs

Every entry is sent to Jira as a worklog as soon as it is saved; edits update it, moving an entry
to another issue moves the worklog, and deleting an entry deletes the worklog. If Jira can't be
reached the entry is still saved and shown as **Not in Jira** with the reason; retry it from the
day view or use **Sync now** in the period header. Removing work deletes its worklog in Jira, so the issue's
logged hours go down. If Jira can't be reached then, the removal is remembered, counted under
"not in Jira" and retried by **Sync now**.

Worklogs you log in Jira directly (or with another tool) are imported when a period opens and with
**Sync now**: they show as **From Jira** and count in totals, missing hours and suggestions. For work
Jira already has, Jira wins: edits made there are copied here, and work deleted there is removed
here (after checking the worklog itself, so a slow Jira search index can't drop anything). Work with
local changes Jira doesn't have yet is left alone and sent to Jira instead.

## My sprint

The issue picker lists the issues assigned to you in an open sprint (not Done, with their status),
then recently used issues. Typing filters both by key or summary and, from two characters, adds
Jira search results; arrow keys, Enter and Escape work. The comment field suggests the latest
comments you used on the row's issue. Tasks and stories both count. On Jira without sprints it
falls back to all open issues assigned to you. The list is cached for five minutes.

## Logging a day

Time is logged per day: each row is an issue, a number of hours (`1.5`, `1,5` or `1:30`) and an
optional comment. When during the day you did it doesn't matter. Jira needs a start time on each
worklog, so the app uses 09:00 on the row's day.

Pick a day in the grid to edit it; the day is kept in the URL (`?day=2026-10-21`), so reload and
back/forward keep it. A workday up to today that is short of its hours opens with suggested rows
for the missing hours. An empty day copies the previous day with work (up to two weeks back): same
issues and split, scaled to the missing hours in quarter hours, without comments. A partly logged day,
or one with nothing to copy, gets the rest on a best-guess issue: the one you last logged that day,
then your In Progress sprint issue, then any sprint issue. **Copy Mon 19 Oct** in the day view does
the copy on demand. Change the issue, hours or comment, **+ Add issue** for more rows, remove rows, then **Save day**. A removed row stays visible,
struck through, with **Undo** until you save; saving deletes its worklog in Jira. Saving creates,
updates and removes the day's worklogs in Jira to match. An added row left without hours or comment
is ignored. **Discard changes** drops what you haven't saved; a discarded or saved day isn't
suggested again until you reload.

Unsaved changes are kept per day while the period is open, so you can move between days and come
back to them. Days with unsaved changes get a blue dot in the grid, and changed hours show dashed.
Changing period, going to Settings, reloading or closing the tab asks first when you changed
something (untouched suggestions don't count).

**Fill period** in the period header adds suggestions to every short workday so far (never future
days), in order, so each empty day copies the day before it as filled. **Save all** asks for confirmation (days, hours and worklogs deleted), then saves every day
with unsaved changes at once; or open a day to adjust it first.

### Keyboard and grid

Outside a field, **←** and **→** move to the previous and next day and **N** adds a row (focused on
its issue). **Enter** in an hours or comment field jumps to the next row's hours, **Esc** leaves a
field, and **Ctrl+S** (**Cmd+S**) saves the day, also while typing. Picking a day in the grid header
focuses its first hours field; clicking a cell opens that issue's row on that day, or adds an empty
row for the issue (ignored if you leave it empty).

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

Monday to Friday need 8 hours by default; set your own hours per weekday under **Settings → Work
hours** (a day with 0 hours is not a workday). Danish public holidays are skipped automatically.
**Time off…** in the day view marks a whole day or part of it off (vacation, sick, other); part of a
day lowers the hours that day needs. The grid shows the hours still missing on each workday up to today.

## Tests

```sh
dotnet test
```

xUnit tests in `tests/JiraTime.Tests` cover periods, the file document store, time entry rules, workdays, worklog sync and import, sprint issues and fill suggestions.

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
Domain/Users.cs                    UserKey (host + Jira user id), UserAccount
Storage/                           document store interfaces + file-system implementation
Services/ConnectionState.cs        encrypted connection in browser localStorage (per tab/circuit)
Services/CurrentUser.cs            the verified user of this tab; data services filter by it
Services/JiraIdentity.cs           signs a tab in as the user Jira says the token belongs to
Services/LegacyDataMigration.cs    gives data from before multi-user support to its owner
Services/TimeEntryService.cs       entries: query, save (with validation), delete
Services/JiraClient.cs             Jira REST calls (myself, issue picker, sprint search, worklogs)
Services/WorklogSync.cs            keeps Jira worklogs in step with entries
Services/SprintIssues.cs           my open-sprint issues, cached briefly
Components/Layout/MainLayout.razor header + redirect to setup when not configured
Components/Pages/Setup/            Connect
Components/Pages/Timesheet.razor   period grid + day view
Components/Pages/SettingsPage.razor
Components/Shared/                 Autocomplete, DayEditor, UserRow
Styles/app.css                     Tailwind input (compiled to wwwroot/app.css)
```

## Not done yet

See `backlog/tasks/`.
