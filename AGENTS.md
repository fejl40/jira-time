# AGENTS.md

Notes for coding agents (and curious humans) working on Jira Time. The README is for people using the app;
everything about building it and how it works inside lives here.

Jira Time is a local Blazor (Interactive Server, .NET 10) + Tailwind CSS v4 timesheet. Jira is the source of
truth for logged time (the user's worklogs); days off, work hours and export values are kept in the app.

**Read [the coding principles](<backlog/docs/doc-1 - Coding-principles.md>) (Backlog.md document doc-1, `backlog doc view doc-1`) before changing code and follow it.** It sets the structure, file sizes and style.

## Build and test

```sh
dotnet build              # also compiles Tailwind (needs Node.js)
dotnet test
dotnet run --project src/JiraTime   # http://localhost:5115
```

- App: `src/JiraTime`. Tests: `tests/JiraTime.Tests` (xUnit). Solution: `JiraTime.slnx`.
- New features come with tests. Build and tests must pass before every push.
- Never commit `data/` (encryption keys and time entries).
- While editing styles, `npm run watch:css` alongside `dotnet watch` gives live CSS rebuilds.

## Task tracking

Tasks live in `backlog/` and are managed with Backlog.md through its MCP server (`backlog` in `.mcp.json`),
or the `backlog` CLI. Pick up work from the backlog, move a task to In Progress when you start it,
tick its acceptance criteria, and set it to Done in the commit that finishes it.
The `backlog/` folder is committed with the code.

## Git policy

- **One branch: `main`.** Do not create branches. For parallel work use `git worktree add --detach <path>`,
  then bring the commits onto `main` (rebase or cherry-pick) and remove the worktree.
- **Commit message format:** task ids first, comma separated, then a plain sentence:
  ```
  TASK-12 Add Jira worklog sync
  TASK-12, TASK-14 Sync worklogs and show sync status
  ```
- **No `Co-Authored-By` or other Claude trailers** in commit messages.
- **Author:** `Carl <fejl40@users.noreply.github.com>`.
- **Push** straight to `main` when a task is done and `dotnet build` and `dotnet test` pass locally.
  There is no CI and there are no pull requests.
- No signed commits, no release tags for now.

## Layout

Paths below are relative to `src/JiraTime/`.

```
Program.cs                         DI: storage, data protection, Jira clients
Domain/TimeEntry.cs                one block of work on an issue on a day
Domain/Period.cs                   semi-monthly periods, today from the clock
Domain/Jira.cs                     connection, profile, issues and worklogs as Jira has them
Domain/PeriodSheet.cs              a period being worked on: saved data, unsaved changes per day, rules
Domain/DayWork.cs                  one day's unsaved changes and the row edits that make them
Domain/TypedHours.cs               reading hours as the user types them (1.5, 1,5, 1:30)
Domain/DayFiller.cs                suggestions for a day's missing hours
Domain/PeriodSummary.cs            logged, missing and weekly hours of a period
Domain/IssueChoices.cs             what the issue picker offers, in which group, and filtering it as the user types
Domain/WorklogMatching.cs          worklogs as entries, waiting work laid over them, comparing the two, the day save plan
Domain/RecentWork.cs               recent issues and comments for quick picks
Domain/WorkCalendar.cs             day kinds, Danish holidays, days off, work hours
Domain/Milestones.cs               things reached while the user watches (a day becoming complete), to celebrate once
Domain/Users.cs                    UserKey (host + Jira user id), UserAccount, IOwnedDocument
Domain/MonthExport.cs              the month export: rows per day and issue, tab-separated text
Storage/IDocumentStore.cs          the document store interfaces
Storage/FileDocumentStore.cs       the file store: one JSON file per document
Storage/FileDocumentCollection.cs  one collection of the file store, cached in memory
Services/SignInSession.cs          restores, connects and signs out a tab; the encrypted connection in browser localStorage
Services/JiraIdentity.cs           signs a tab in as the user Jira says the token belongs to
Services/CurrentUser.cs            the verified user of this tab; data services filter by it
Services/LegacyDataMigration.cs    moves data saved by older versions (owners, entries Jira has); their documents and backups
Services/TimeLog.cs                logged time: Jira worklogs plus the retry queue, cached briefly
Services/RecentWorklogIssues.cs    issues just saved on, read directly while Jira's search catches up
Services/RetryQueue.cs             the retry queue: changes Jira couldn't take yet
Services/WorkCalendarService.cs    days and work hours of the signed-in user
Services/JiraClient.cs             Jira REST calls about issues (myself, issue picker, searches)
Services/JiraWorklogClient.cs      Jira REST calls that read and write worklogs
Services/Jira/                     HTTP transport, JQL quoting, comment format, raw JSON shapes
Services/WorklogSync.cs            saves a day or every unsaved day to Jira, sends the retry queue
Services/WorklogWriter.cs          writes or deletes one worklog in Jira, queues what fails
Services/PeriodLoader.cs           loads a period for the timesheet: time, days, picks, previous period
Services/IssueStatuses.cs          current status of the issues a timesheet shows
Services/JiraIssueLookup.cs        sprint and period issues and issue statuses, cached briefly
Services/EpicExportStore.cs        Fase, Forordning and Hovedleverance per epic for the export
Services/MonthExportLoader.cs      loads a month for the export: time, epics, values per epic
Services/ExpiringCache.cs          small cache whose values expire
Components/Layout/MainLayout.razor header + redirect to setup when not signed in
Components/Pages/Setup/            Connect
Components/Pages/Timesheet.razor   shows a period: wires PeriodLoader, WorklogSync and the parts below
Components/Format.cs               how hours, days and counts are written for the user
Components/Links.cs                builds and reads timesheet links (period, selected day) and export links
Components/Sheet/                  period header (with confetti), reminders, grid and its cells, day list (phones), day panel and header, day editor and its rows, issue picker, DayKeys (keyboard shortcuts)
Components/Motion.cs               animation classes (Styles/motion.css), MotionScript (calls wwwroot/motion.js), SaveMotion
Components/Pages/ExportPage.razor  a month in the agency's format: values per epic, Copy, Download .tsv (wwwroot/export.js)
Components/Pages/SettingsPage.razor
Components/Pages/Error.razor, NotFound.razor
Components/Shared/                 StepNav (‹ today ›), ErrorCard, Count, ThemeToggle, UserRow
Styles/app.css                     Tailwind input (compiled to wwwroot/app.css)
```

## Where data lives

| What | Where |
|---|---|
| Jira URL, auth type, email, **token** | Browser `localStorage` (`jira-time.connection`), encrypted by the server |
| Encryption keys | `data/keys/` on the server (DPAPI-protected on Windows) |
| Users Jira confirmed (profile, hashes of confirmed tokens) | `data/documents/users/<owner>.json` |
| Days off (whole or partial) | `data/documents/days-off/<owner>-<date>.json` |
| Work hours per weekday | `data/documents/user-settings/<owner>.json` |
| Export values per epic (Fase, Forordning, Hovedleverance) | `data/documents/epic-export-values/<owner>-<epic>.json` |
| Logged time | **Jira** (your worklogs) |
| Changes Jira couldn't take yet (the retry queue) | `data/documents/time-entries/<id>.json`, one file per change, with its owner; removed once Jira has it |
| Copy of the data from before multi-user support | `data/documents/backups/` |
| Copy of the time entries from before Jira became the source of truth | `data/documents/time-entry-backups/` |

**Token encryption:** the server encrypts the connection with ASP.NET Core Data Protection
(`ProtectedLocalStorage`) before handing it to the browser. The browser only ever stores
ciphertext; the key never leaves `data/keys`. Copying the browser's storage gives nothing usable.
Deleting `data/keys` makes the stored token unreadable and the app asks you to connect again.

`data/` is created next to the project (`src/JiraTime/data/`). **Back up** the `data/` folder to keep your days off and settings. `data/` is git-ignored.

## How it works

### Connecting

1. **Connect**: Jira URL + token.
   - *Server / Data Center*: Personal Access Token, sent as `Bearer`.
   - *Cloud*: Atlassian email + API token, sent as Basic auth.
   The token is checked against Jira (`/myself`) before it is saved, and the user it belongs to
   becomes your profile. To log hours as someone else, connect with their token.

### Several users

Several people can use one Jira Time server, each in their own browser with their own token. A
user is the Jira host plus the user id that host returns for the token: on every visit the server
asks Jira who the token belongs to, so nobody can see or change someone else's hours, and the same
user id on two Jira hosts counts as two users. If Jira can't be reached, a token Jira confirmed
before still works (the header shows **Offline**), so you can keep logging and sync later. A token
Jira rejects sends you back to Connect.

Data saved before this existed has no owner. The first time its user signs in (the user in the old
profile setting), it is given to them on the host they sign in to. A copy of the old data is stored
in `data/documents/backups/` first, and nothing is deleted.

### Jira worklogs

The timesheet shows your Jira worklogs, however they were logged: here, in Jira or in WorklogPRO.
They are read when a period opens, reused for a minute, and read again with **Sync now** or after a save.
Jira's search can take a while to find a new worklog, so for 15 minutes after a save the issues you
saved on are read directly.

Saving a day creates, updates and deletes its worklogs in Jira, then reads the day back. Moving a row
to another issue moves its worklog; removing a row deletes it. Before changing or deleting worklogs,
the app checks that Jira still has them as they were read. If someone changed one meanwhile nothing is
saved: the day shows Jira's version with your changes on top, to check and save again.

If Jira can't take a change, it waits in a local retry queue: it shows as **Not in Jira** with the
reason, counts in totals (a waiting removal hides the work), and is sent by **Sync now** or the next
save of that day. Once Jira has it, it leaves the queue.

Older versions kept every entry locally as well. The first time you sign in after updating, a copy
of your entries is stored in `data/documents/time-entry-backups/`, the entries Jira already has are
dropped, and the ones it doesn't have stay in the retry queue for **Sync now**.

If Jira can't be read at all, the period shows only the work waiting for Jira, with a **Try again**
button. Hours are unknown then, so nothing counts as missing, nothing is suggested and days can't be
edited, so nothing is logged twice.

### Issue picker

The issue picker lists the issues assigned to you in an open sprint (not Done, with their status),
then issues you logged on in the last 60 days (from your Jira worklogs). Typing filters both by key or summary and, from two characters, adds
Jira search results; arrow keys, Enter and Escape work. The comment field suggests the latest
comments you used on the row's issue in those worklogs. Tasks and stories both count. On Jira without sprints it
falls back to all open issues assigned to you. The list is cached for five minutes.

Under **Earlier this period** it also lists issues that were assigned to you and To Do or In Progress
at some point during the open period, even if they are Done now, so you can log time retroactively.
If Jira rejects those status names, it uses issues assigned to you during the period that weren't
resolved before it started. Any other issue can still be picked through Jira search. A row on an issue
that is Done in Jira shows a note saying so; the day saves as usual.

### Logging a day

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

#### Period header, grid and reminders

The header has ‹ **Today** › next to the period, one status line (hours logged, missing, unsaved,
not in Jira, with **Sync**) and one main action: **Save all** when something is unsaved, otherwise
**Fill period** when days are short. When nothing up to today is short, nothing is unsaved and
everything is in Jira, it says **Complete so far**. The grid marks cells whose work isn't in Jira
with an amber dot (the reason is in the tooltip) and shows each week's hours against what the week
needs. In the last three days of a period a banner sums up what is left, and when the previous
period still has missing hours or work not in Jira a banner links to it. On narrow screens a list of
days replaces the grid.

#### Keyboard and grid

Outside a field, **←** and **→** move to the previous and next day and **N** adds a row (focused on
its issue). **Enter** in an hours or comment field jumps to the next row's hours, **Esc** leaves a
field, and **Ctrl+S** (**Cmd+S**) saves the day, also while typing. Picking a day in the grid header
focuses its first hours field; clicking a cell opens that issue's row on that day, or adds an empty
row for the issue (ignored if you leave it empty).

### Periods

Periods are semi-monthly: the 1st to the 15th, and the 16th to the last day of the month.

### Workdays

Monday to Friday need 8 hours by default; set your own hours per weekday under **Settings → Work
hours** (a day with 0 hours is not a workday). Danish public holidays are skipped automatically.
**Time off…** in the day view marks a whole day or part of it off (vacation, sick, other); part of a
day lowers the hours that day needs. Marking a whole day off marks the time saved on it as removed, so
**Save day** deletes those worklogs in Jira; the day stays editable, and **Undo** brings the rows back before saving. The grid shows the hours still missing on each workday up to today.

### Month export

The Export page (`/export?month=yyyy-MM`) gives a calendar month in the agency's tab-separated format:
`Dato (yyyy-mm-dd)`, `Timer`, `Fase (ydelsesområde)`, `Forordning`, `Hovedleverance nr.`, `Hovedleverance link`,
`Epic`, `Beskrivelse`. Every day of the month gets a row (only the date when nothing is logged); otherwise one
row per day and issue with the hours added up, written with a decimal comma (7,5). `Epic` is a link to the
issue's epic (the parent when it is an epic, else the Epic Link field, a sub-task takes its parent's epic) and
is empty when there is none, epics included. Fase, Forordning and Hovedleverance are set once per epic by the
user and stay empty for issues without an epic. `Beskrivelse` is the issue key. **Copy** puts header and rows
on the clipboard; **Download .tsv** saves UTF-8 with a byte order mark so Excel reads æøå.

### Theme

Light, dark or system (follows the OS). Switch with the icon in the header; the choice is kept in the browser.

### Animations

Totals count to their new value, Fill period suggestions drop in day by day, day headers fill like a
progress bar, saved cells settle while a tick flies to the Jira status, removed rows turn red and
collapse, periods slide, the selected day glides, picker groups slide in, reminders pulse once, time off
fades to grey (with a sun for vacation), and a period that becomes complete gets confetti once. They
are quick (150–300 ms) and switch off when your system asks for reduced motion. Styles and speeds are in
`Styles/motion.css`, the script parts in `wwwroot/motion.js`, called through `MotionScript` in `Components/Motion.cs`.

### Storage abstraction

The app only uses `IDocumentStore` / `IDocumentCollection<T>` (`Storage/IDocumentStore.cs`):
`GetAsync`, `QueryAsync(Expression<...>)`, `UpsertAsync`, `DeleteAsync`. Filters are
expressions so a database implementation can translate them into server-side queries.

The current implementation, `Storage/FileDocumentStore`, writes one JSON file per
document (atomic temp-file + move), caches each collection in memory, and reloads if files change
on disk. To move to a document database (MongoDB, Cosmos DB, RavenDB, ...), implement the two
interfaces and replace the `IDocumentStore` registration in `Program.cs`.

### Tests

```sh
dotnet test
```

xUnit tests in `tests/JiraTime.Tests` cover periods, the period sheet, the file document store, time entry rules, workdays, reading time from Jira, loading a period, saving days to Jira and the retry queue, links, Jira queries, issue lookups and the picker, and fill suggestions.

