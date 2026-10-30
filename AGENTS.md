# AGENTS.md

Notes for coding agents (and curious humans) working on Jira Time. The README is for people using the app;
everything about building it and how it works inside lives here.

Jira Time is a local Blazor (Interactive Server, .NET 10) + Tailwind CSS v4 timesheet. Jira is the source of
truth for logged time (the user's worklogs); days off, work hours and export values are kept in the app.

**Read [the coding principles](<backlog/docs/doc-1 - Coding-principles.md>) (Backlog.md document doc-1, `backlog doc view doc-1`) before changing code and follow it.** It sets the structure, file sizes and style.

## Build and test

```sh
dotnet build              # compiles Tailwind when Node.js is installed; otherwise uses the committed CSS
dotnet test
dotnet run --project src/JiraTime   # http://localhost:5115
```

- App: `src/JiraTime`. Tests: `tests/JiraTime.Tests` (xUnit). Solution: `JiraTime.slnx`.
- New features come with tests. Build and tests must pass before every push.
- The build has 0 warnings. `.editorconfig` and `Directory.Build.props` make it report a namespace that doesn't
  match its folder (IDE0130) and unused usings (IDE0005).
- Never commit `data/` (encryption keys and time entries).
- Colleagues restore packages from a company NuGet mirror that lags behind nuget.org. Don't move a package to
  its newest version: pick one that has been out a few months (Microsoft.Extensions.Http.Resilience 10.6.0 and
  Polly.Core 8.7.0 are known to be there).
- While editing styles, `npm run watch:css` alongside `dotnet watch` gives live CSS rebuilds.
- `wwwroot/app.css` (compiled Tailwind) is committed so the app builds with only the .NET SDK. After changing
  styles or Tailwind classes in markup, build with Node.js and commit `wwwroot/app.css` with the change.

## Task tracking

Tasks live in `backlog/` and are managed with Backlog.md through its MCP server (`backlog` in `.mcp.json`),
or the `backlog` CLI.

**Tasks come first.** Every change is described in a backlog task (description and acceptance criteria)
before any code for it is written. When the ask changes during the work, update the task first, then the code. Pick up work from the backlog, move a task to In Progress when you start it,
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
Program.cs                         AddJiraTime plus the UI's own services (modals, motion, the shown period)
Domain/TimeEntry.cs                one block of work on an issue on a day, and its named sync transitions
Domain/Hours.cs                    adding up hours (TimeSpanSum)
Domain/Period.cs                   semi-monthly periods, today from the clock
Domain/Jira.cs                     connection (and the typed Jira URL), profile, issues and worklogs as Jira has them
Domain/PeriodSheet.cs              a period being worked on: saved data, each day's unsaved work (Day(date) gives a SheetDay), suggestions, days off, the Save all plans
Domain/SheetDay.cs                 one day of a period: saved rows and unsaved changes read and edited together, row states
Domain/DayWork.cs                  one day's unsaved changes, single entries only
Domain/DaySave.cs                  the day save plan: whether a day can be saved, what is removed, sent and checked; works on copies
Domain/TypedHours.cs               reading hours as the user types them (1.5, 1,5, 1:30)
Domain/DayFiller.cs                suggestions for a day's missing hours
Domain/PeriodSummary.cs            logged, missing (per day too), weekly hours and work not in Jira of a period; nothing missing when Jira can't be read
Domain/IssueChoices.cs             what the issue picker offers, in which group, and filtering it as the user types
Domain/WorklogMatching.cs          worklogs as entries, waiting work laid over them, comparing the two
Domain/RecentWork.cs               recent issues and comments for quick picks
Domain/WorkCalendar.cs             day kinds, Danish holidays, days off, work hours (and as typed on Settings)
Domain/UnsavedChanges.cs           what Save all will change: rows added, changed and removed per day, totals, days that can't be saved
Domain/Milestones.cs               things reached while the user watches (a day becoming complete), to celebrate once
Domain/Users.cs                    UserKey (host + Jira user id), UserAccount, IOwnedDocument
Domain/ProfilePicture.cs           the user's Jira profile picture as kept: which avatar URL to read (not Jira's default), fresh for two days, its app URL
Domain/MonthExport.cs              the month export: rows per day and issue, tab-separated text
Domain/JiraLog/                    one logged Jira request (JiraLogEntry: state, short path, outcome), the Logs page's day, groups and filters, JsonBody (a body as a foldable JSON tree or raw text), RequestToasts (order, how long they stay, bursts)
Storage/IDocumentStore.cs          the document store interfaces
Storage/FileDocumentStore.cs       the file store: one JSON file per document
Storage/FileDocumentCollection.cs  one collection of the file store, cached in memory
Storage/SharedFile.cs              reads that let others write or delete meanwhile; reads, replaces and deletes wait briefly while Windows has the file open
Services/AppServices.cs            AddJiraTime: registers the app's services, for Program.cs and the tests
Services/OwnedCollection.cs        one collection of the signed-in user's documents: the only owner filter
Services/WorkCalendarService.cs    days and work hours of the signed-in user
Services/PeriodLoader.cs           loads a period for the timesheet: time, days, picks, previous period
Services/IssueStatuses.cs          current status of the issues a timesheet shows, and the issue picker's Jira search
Services/EpicExportStore.cs        Fase, Forordning and Hovedleverance per epic for the export
Services/MonthExportLoader.cs      loads a month for the export: time, epics, values per epic
Services/ProfilePictures.cs        the header's profile picture: read through JiraHttp at most once per two days per user, and the profile-picture/{key} endpoint
Services/ExpiringCache.cs          small cache whose values expire; expired values are dropped as new ones are stored
Services/SignIn/                   SignInSession (restores, connects and signs out a tab; the encrypted connection in browser localStorage), JiraIdentity (signs a tab in as the user Jira says the token belongs to), CurrentUser (the verified user of this tab and its Jira connection, CurrentUser.Jira), LegacyDataMigration (data saved by older versions: owners, entries Jira has, entries with start and end times; their backups)
Services/Worklogs/                 TimeLog (Jira worklogs plus the retry queue, cached briefly), RetryQueue (changes Jira couldn't take yet), WorklogSync (saves the Save all plans, sends the retry queue), WorklogWriter (writes or deletes one worklog, queues what fails), RecentWorklogIssues (issues just saved on, read directly while Jira's search catches up)
Services/Jira/                     JiraHttp (the one typed HttpClient for Jira), JiraResilience (429 retries, pause, queue), JiraSearch (pages, batches of keys, POST), JiraClient (issues: myself, picker, searches, epics), JiraWorklogClient (reads and writes worklogs), JiraIssueLookup (sprint and period issues, statuses and epics, cached briefly per user), JiraWorklogJson and JiraIssueJson (Jira's JSON mapped to the app's worklogs, issues, profile and epics), JQL quoting, comment format, raw JSON shapes
Services/JiraLog/                  the Jira request log: user action (JiraAction), JiraRecordingHandler (records each attempt for JiraHttp) and the recorder, JSONL files, live feed per user, MyJiraLog (the signed-in user's log)
Components/Layout/MainLayout.razor header + redirect to setup when not signed in
Components/Pages/Connect.razor     connecting to Jira (route /setup/connect)
Components/Pages/Timesheet.razor   shows a period: URL and selected day, modals, focus and animations around ShownPeriod
Components/Pages/SettingsPage.razor
Components/Pages/LogsPage.razor    the Jira request log of a day: filters, download, clear, live (wwwroot/logs.js)
Components/Pages/Error.razor, NotFound.razor
Components/Format.cs               how hours, days and counts are written for the user
Components/DayLook.cs              how a day looks: weekend and holiday grey and name, the grid's tooltip, time off icon, tint and label per reason
Components/Links.cs                builds and reads timesheet links (period, selected day), export links and log links
Components/Motion.cs               animation classes (Styles/motion.css), MotionScript (calls wwwroot/motion.js)
Components/Sheet/ShownPeriod.cs    the period on screen and its work with Jira: open, reload, Try again, Sync now, Save all, time off; SaveMotion
Components/Sheet/                  period header (with confetti), reminders, grid and its cells, day list (phones), day panel and header, day editor and its rows, issue picker, IssueColors (an issue's dot colour), DayKeys (keyboard shortcuts), JiraWaits (running Jira requests: what shows pending, which buttons wait, the status text), TimeOffModal, SaveAllModal
Components/Export/                 ExportPage (a month in the agency's format: Copy, Download .tsv), EpicValuesForm (values per epic), ExportTable
Components/Modals/                 the modal system: ModalService (injected as Modals), ModalQueue and ModalContext, ModalHost (in MainLayout), the Modal frame, ConfirmModal (wwwroot/modal.js)
Components/Logs/                   request toasts (in MainLayout), the log's action groups, a request's details and its status dot, the JSON tree
Components/Shared/                 StepNav (‹ today ›), ErrorCard, Count, ThemeToggle, UserRow, Avatar (the header's profile picture over the initials), Waiting (the waiting-for-Jira spinner)
Styles/app.css, motion.css         Tailwind input and animations (compiled to wwwroot/app.css)
wwwroot/files.js                   copy to the clipboard and file downloads (Export and Logs)
wwwroot/timezone.js                the browser's time zone, read at sign-in
wwwroot/favicon.svg                the app icon (apple-touch-icon.png is it as a 180 px square PNG)
```

## Where data lives

| What | Where |
|---|---|
| Jira URL, auth type, email, **token** | Browser `localStorage` (`jira-time.connection`), encrypted by the server |
| Encryption keys | `data/keys/` on the server (DPAPI-protected on Windows) |
| Users Jira confirmed (profile, hashes of confirmed tokens) | `data/documents/users/<owner>.json` |
| Days off (whole or partial) | `data/documents/days-off/<owner>-<date>.json` |
| Work hours per weekday | `data/documents/user-settings/<owner>.json` |
| Jira profile picture (as read from Jira, for two days) | `data/documents/profile-pictures/<owner>.json` |
| Export values per epic (Fase, Forordning, Hovedleverance) | `data/documents/epic-export-values/<owner>-<epic>.json` |
| Logged time | **Jira** (your worklogs) |
| Changes Jira couldn't take yet (the retry queue) | `data/documents/time-entries/<id>.json`, one file per change, with its owner; removed once Jira has it |
| Jira request log (every request and response, without the token) | `data/logs/jira/<owner>/yyyy-MM-dd.jsonl`, one JSON line per request; deleted after 30 days |
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

### Profile picture

The header shows the user's Jira profile picture (the 48 px one from `avatarUrls` in `/myself`, shown at 32 px) over
their initials. On Server/DC the picture needs the token, which the browser can't send, so the server reads it through
`JiraHttp` (rate limits and request log; logged under the sign-in it follows, or as **Profile picture**) and keeps it in
`data/documents/profile-pictures/<owner>.json`. The token is only sent when the picture is on the Jira host itself,
never to another host such as Gravatar. The picture is read again after two days, or at once when Jira's avatar URL
changes (`ProfilePictures`, rules in `Domain/ProfilePicture.cs`).

The browser loads it from `profile-picture/{key}` with `Cache-Control: private, max-age=172800, immutable`. That
request isn't part of a tab's circuit, so it has no signed-in user: the key is the permission. It is 128 random bits,
new on every read from Jira (so it is also the version), and only ever written into its owner's own pages; any other
key gets a 404. Jira's default avatar (no `ownerId` on Server/DC, Cloud's initials pictures, Gravatar without a picture,
asked with `d=404`), a failed read, an answer that isn't a PNG, JPEG, GIF or WebP (no SVG, which could run script from
the app's address) or no avatar keeps the initials. A failed read Jira answered is not retried for two days; when
Jira can't be reached the picture read before still shows.

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
One search reads them (`fields=summary,worklog`), and Jira sends each issue's first worklogs (20 on most Jiras) with
it; only an issue with more is read on its own. For a period near today that one search covers the period, the days
to copy from, the period before and the last 60 days for recent picks; an old period and the recent days are read apart.
Reads that don't depend on each other go out together: opening a period reads the worklogs, the sprint issues and the
period's issues at the same time, and issues read on their own are read in parallel; the Jira client's limiter keeps
at most 10 in flight per host (see Jira rate limits). Writes stay one at a time, so the changed-in-Jira check and
Jira's per-issue write limits keep working. `JiraAction` is AsyncLocal, so parallel requests still log under their action.
Jira's search can take a while to find a new worklog, so for 15 minutes after a save the issues you
saved on, when the worklog search doesn't return them, are read by key in one search with their worklogs
(`key in (...)`, `fields=summary,worklog`): field values come from the issue itself, not the worklog index.

Several issues are always read in one search, never one request each (`Services/Jira/JiraSearch.cs`): status and epic
lookups, the recently saved issues and the changed-in-Jira check all use `key in (...)`, 100 keys per search (the
batches go out together), and every search reads all its pages (`startAt`/`total` on Server/DC, `nextPageToken` on
Cloud). A search whose URL would be longer than 2000 characters is sent as `POST /search` with a JSON body. Key
searches on Server/DC send `validateQuery=warn`, so a deleted or unknown key is skipped with a warning instead of
failing the whole search; only a Jira that still answers 400 (Cloud, which has no `validateQuery`) is asked key by key.

Saving (with **Save all**) creates, updates and deletes each changed day's worklogs in Jira, then reads the period back. Moving a row
to another issue moves its worklog; removing a row deletes it. Before changing or deleting worklogs,
the app checks that Jira still has them as they were read: one search reads the issues of every worklog
Save all would change, on every day, with their worklogs (an issue with more worklogs than the search sends is
read on its own). If someone changed one meanwhile nothing is saved on that day: it shows Jira's version with your
changes on top, to check and save again. If Jira can't be asked, the save goes ahead.

If Jira can't take a change, it waits in a local retry queue: it shows as **Not in Jira** with the
reason, counts in totals (a waiting removal hides the work), and is sent by **Sync now**. Once Jira has it, it leaves the queue.

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
falls back to all open issues assigned to you. The list is cached for five minutes. Every issue cache (sprint and period lists,
statuses, epics, the Epic Link field) is kept per user (host and Jira user id), so one user never gets what Jira told
another.

Under **Earlier this period** it also lists issues that were assigned to you and To Do or In Progress
at some point during the open period, even if they are Done now, so you can log time retroactively.
If Jira rejects those status names, it uses issues assigned to you during the period that weren't
resolved before it started. Any other issue can still be picked through Jira search. A row on an issue
that is Done in Jira shows a note saying so; the day saves as usual.

### Logging a day

Time is logged per day: each row is an issue, a number of hours (`1.5`, `1,5` or `1:30`) and an
optional comment. When during the day you did it doesn't matter. Jira needs a start time on each
worklog, so the app uses 09:00 on the row's day, in your browser's time zone (read when the tab signs in, by
`wwwroot/timezone.js`; the server's zone when the browser gives none or one the server doesn't know). Today (which day
and period open, what counts as up to today, the export's month) is on the same clock (`Clock.Today` in `Domain/Period.cs`).

Pick a day in the grid to edit it; the day is kept in the URL (`?day=2026-10-21`), so reload and
back/forward keep it. A workday up to today that is short of its hours opens with suggested rows
for the missing hours. An empty day copies the previous day with work (up to two weeks back): same
issues and split, scaled to the missing hours in quarter hours, without comments. A partly logged day,
or one with nothing to copy, gets the rest on a best-guess issue: the one you last logged that day,
then your In Progress sprint issue, then any sprint issue. **Copy Mon 19 Oct** in the day view does
the copy on demand. Change the issue, hours or comment, **+ Add issue** for more rows and remove rows.
A removed row stays visible, struck through, with **Undo** until you save; saving deletes its worklog in
Jira. An added row left without hours or comment is ignored. A row that can't be saved (no issue, hours
that can't be read) says why right away. **Discard changes** drops a day's unsaved changes; a discarded
or saved day isn't suggested again until you reload.

**Save all** in the period header is the only way to save (there is no Save day). It opens a modal
(`SaveAllModal`, rules in `Domain/UnsavedChanges.cs`) listing every unsaved day with its rows added,
changed (hours or issue, before → after) and removed (worklog deleted in Jira), the day's hours before
and after, and the totals; a day that can't be saved yet says why and is left out. **Save** creates,
updates and deletes the worklogs in Jira to match, day by day (`PeriodSheet.PlanSaveAll()` gives one `DaySave` plan per
day and `WorklogSync.SaveDaysAsync` sends them, with one changed-in-Jira search); **Cancel** saves nothing. **Ctrl+S** opens
the same modal. Days that couldn't be saved stay unsaved and the first of them opens with the reason.

Unsaved changes are kept per day while the period is open, so you can move between days and come
back to them. Days with unsaved changes get a blue dot in the grid, and changed hours show dashed.
Changing period, going to Settings, reloading or closing the tab asks first when you changed
something (untouched suggestions don't count).

**Fill period** in the period header adds suggestions to every short workday so far (never future
days), in order, so each empty day copies the day before it as filled; check the suggestions in the
Save all modal, or open a day to adjust it first.

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
field, and **Ctrl+S** (**Cmd+S**) opens Save all, also while typing. Picking a day in the grid header
focuses its first hours field; clicking a cell opens that issue's row on that day, or adds an empty
row for the issue (ignored if you leave it empty).

### Periods

Periods are semi-monthly: the 1st to the 15th, and the 16th to the last day of the month.

### Workdays

Monday to Friday need 8 hours by default; set your own hours per weekday under **Settings → Work
hours** (a day with 0 hours is not a workday). Danish public holidays are skipped automatically.
**Time off…** in the day view opens a modal that marks a whole day or part of it off (vacation, sick, other); part of a
day lowers the hours that day needs. Marking a whole day off marks the time saved on it as removed, so
**Save all** deletes those worklogs in Jira (`PeriodSheet.MarkDayOffAsync` / `UnmarkDayOffAsync`); the day stays editable, and **Undo** brings the rows back before saving. The grid shows the hours still missing on each workday up to today.

Time off stands out without opening the day (`DayLook`, styles `.day-off-*` in `Styles/app.css`): each reason has
an icon and a tint (☀️ amber for vacation, 🤒 rose for sick, 🗓️ violet for other time off). A whole day off stripes
its column in the grid and its entry in the phone list, with the icon under the date; part of a day off tints them and
puts the icon next to the weekday. The phone list says "Vacation" or "Time off 3 h", the day header's badge "Vacation ·
whole day" or "Sick · 4 h off", and the grid header's tooltip the same. A whole day off keeps its saved hours readable,
struck through next to the 0 they become, until Save all removes them from Jira.

### Month export

The Export page (`/export?month=yyyy-MM`) gives a calendar month in the agency's tab-separated format:
`Dato (yyyy-mm-dd)`, `Timer`, `Fase (ydelsesområde)`, `Forordning`, `Hovedleverance nr.`, `Hovedleverance link`,
`Epic`, `Beskrivelse`. Every day of the month gets a row (only the date when nothing is logged); otherwise one
row per day and issue with the hours added up, written with a decimal point (7.5). `Epic` is a link to the
issue's epic (the parent when it is an epic, else the Epic Link field, a sub-task takes its parent's epic) and
is empty when there is none, epics included. Fase, Forordning and Hovedleverance are set once per epic by the
user and stay empty for issues without an epic. `Beskrivelse` is the issue key. **Copy** puts header and rows
on the clipboard; **Download .tsv** saves UTF-8 with a byte order mark so Excel reads æøå.

### Waiting for Jira

Every wait on Jira shows where the user is looking, with one shared indicator, `Components/Shared/Waiting.razor`
(a small spinner, with text where it helps). It stays hidden for the first 200 ms (`--waiting-delay` in
`Styles/motion.css`), so a fast answer doesn't flicker, and stands still with reduced motion. While a period opens,
the old one stays on screen dimmed, and the header says **Opening 1 Oct – 15 Oct…**; Sync, Save all and time off dim the
grid and the day's rows (time off is one wait from the click until the period is read again, shown at least 600 ms
so its spinner is seen) (`aria-busy`, `.waiting-dim`). The first load,
signing in and the export show a waiting line instead of empty content. The button that started a request gets the
spinner, and on the timesheet every button that sends a request to Jira (Sync, Save all, Try again, time
off) is off until the running one ends, so saves and reads never cross. `JiraWaits` (`Components/Sheet/JiraWaits.cs`) decides this and is tested;
a period left before it was read is never shown. Settings (Save hours), Connect, the issue picker's search and
the export's values per epic show the same spinner.

### Jira rate limits

`JiraHttp` is the one typed HttpClient for Jira (`AddJiraHttp` in `Services/Jira/JiraResilience.cs`): the 30 s
timeout, a Microsoft.Extensions.Http.Resilience pipeline and the request log are set up there once; `JiraClient` and
`JiraWorklogClient` get it injected. There is one pipeline per Jira host (`SelectPipelineByAuthority`), shared by every
user and tab, with three strategies, outermost first:

1. **Retry**: a 429 or the open circuit is waited out (Retry-After, in seconds or as a date, else exponential backoff
   from 1 s with jitter) and the request sent again, at most 5 times; a pause longer than 10 s isn't waited out and
   the request fails as before (a write goes to the retry queue). Only 429 and the open circuit are retried, never
   5xx, timeouts or network errors, so a worklog is never logged twice; Jira did nothing with a request it refused with 429.
2. **Concurrency limiter**: at most 10 requests to a host at once (the top of what Atlassian suggests; 429s are what slow it down); the rest wait in a FIFO queue (200 places; a full
   queue fails like any Jira error). It sits outside the breaker, so a request turned away by the open circuit gives
   its place back while the retry waits.
3. **Circuit breaker**: a 429 opens it for Retry-After plus up to 20% jitter, so no request goes to that host,
   for any user, until the pause is over. Polly needs two requests in its window (5 minutes) before it can open, so
   the very first request after a quiet spell that gets a 429 only waits on its own.

`JiraRecordingHandler` sits inside the pipeline, so every attempt is in the request log; a 429 is logged as
"Jira asked us to slow down: requests to Jira pause for N s.", which the request toast shows too. Tests use
`TestClock`, whose timers fire at once and move the clock on, so waits take no real time.

### Jira request log

Every Jira request goes through `JiraHttp`; each attempt is recorded by `JiraRecordingHandler` with `JiraRequestRecorder`: method, full URL,
status and reason, duration, headers (without `Authorization`, `Proxy-Authorization` and cookies), content
type, response size, request and response bodies (cut at 64 KB and flagged) and the error for failures,
timeouts and unreachable Jira. Each request is one JSON line in `data/logs/jira/<owner>/yyyy-MM-dd.jsonl`;
files older than 30 days are deleted. A picture's bytes aren't logged as text, only its type and size. The owner comes from the verified connection (`CurrentUser` stamps
`JiraConnection.Owner`).

Each request names the user action it was for (`JiraAction`, an AsyncLocal scope): the UI starts one
around Open period, Save all, Sync now, issue search, picking an issue, time off, the export and
signing in; a start inside a running action joins it, so the reload after a save belongs to the save.
Requests outside any action are **Background**. Requests made while signing in wait until Jira says whose
token it is and are logged under that user, or under `signed-out` when Jira refuses the token.

`JiraRequestFeed` tells each user's open tabs about their requests as they start and finish. Every
subscriber has its own queue read on a background task, so a slow or failing one never holds up a request.
The toasts in the bottom left (`JiraToasts`, rules in `RequestToasts`) and the **Logs** page use it. Logs
shows a day newest first, grouped by action, with filters (day, errors only, text), Download .jsonl and
Clear logs; a row expands into its details with the bodies as JSON trees. Clicking a toast opens
`logs?day=…&request=…` with that request expanded.

### Modals

All modals go through `Components/Modals` (namespace `JiraTime.Components.Modals`). `ModalService` (scoped, one per tab,
injected as `Modals`)
shows a modal component and waits for its result: `ShowAsync<TModal, TResult>(parameters, onTop)` returns what
the modal closed with, or null (the default) when it was cancelled, once its dialog has finished closing (so
what comes next, such as a new period sliding in, isn't hidden behind the fading backdrop); `ConfirmAsync(title, text, confirmLabel,
danger)` asks a yes-or-no question (Clear logs, leaving the timesheet with unsaved changes; closing the tab keeps
the browser's own warning). `ModalHost`, once in `MainLayout`, draws each open modal with `DynamicComponent` in a
native `<dialog>` that `wwwroot/modal.js` opens with `showModal()`: top layer, focus kept inside, the page inert,
the focus back on the opener when it closes. The backdrop is blurred and darkened (`.modal::backdrop` in
`Styles/app.css`), modals rise in and out quickly (`Styles/motion.css`, off with reduced motion), and on phones
they are a full-width sheet from the bottom.

One modal shows at a time: one asked for while another is open waits, and they open in the order asked for.
`onTop: true` puts a modal above the open one (for a modal opened from a modal); the one below comes back when it
closes. Going to another page cancels every open and waiting modal; another day on the same page doesn't. These
rules are `ModalQueue`, tested in `ModalsTests`. The server owns which modals are open: the script only reports.

A custom modal wraps its content in `<Modal Title="…">` (body, and a `Footer` for buttons) and gets a cascading
`ModalContext`: `Close(result)` (any value, such as a record), `Cancel()`, `Busy` and `WhileBusyAsync(work)`.
While busy the frame's controls are off, the footer shows the Waiting spinner and the modal can't be closed. In
markup: `data-modal-close` cancels, `data-modal-result="x"` closes with `x` (read as the result type: true, 4, an
enum name), `data-modal-autofocus` gets the focus on open, and `data-modal-busy` (set on the dialog while busy)
stops closing. `[Modal(Size = ModalSize.Small/Medium/Large, Dismiss = true)]` on the component sets its width and
whether Esc and a click on the backdrop cancel it. The timesheet's keyboard shortcuts are off while a modal is open.

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
expressions so a database implementation can translate them into server-side queries. Services reach the
signed-in user's documents through `OwnedCollection<T>` (`Services/OwnedCollection.cs`), which adds the owner to
every query expression, returns nothing of anyone else's and refuses to overwrite it.

The current implementation, `Storage/FileDocumentStore`, writes one JSON file per
document (atomic temp-file + move), caches each collection in memory, and reloads if files change
on disk. To move to a document database (MongoDB, Cosmos DB, RavenDB, ...), implement the two
interfaces and replace the `IDocumentStore` registration in `AddJiraTime` (`Services/AppServices.cs`).

### Tests

```sh
dotnet test
```

xUnit tests in `tests/JiraTime.Tests` sit in folders that mirror `src/JiraTime` (Domain, Storage, Services and its
subfolders, Components), with the shared helpers in `Support/`. They cover periods, the period sheet and its days (SheetDay, days off), the day
save plan (DaySave), the file document store, owned collections (others' documents are never read or written), time
entry rules and their sync transitions, workdays, reading hours as typed, reading and writing worklogs in Jira's
format (embedded worklogs, pages, the worklog body), loading a period, saving days to Jira and the retry queue, the
period summary (unknown hours, work not in Jira), links, Jira queries, issue lookups and the picker, fill
suggestions, the Save all change list, Settings work hours and the Jira URL from Connect, the expiring cache, modals
(queue, results, defaults), the waits on Jira (what shows pending and which buttons wait), Jira rate limits (retries
after 429, the pause across users and hosts, the queue), parallel reads (overlap, the cap of ten, writes one at a
time), the profile picture (read once per two days, logged, the default avatar and failures keep the initials, each
user only their own), and searches by key (batches, pages, POST, unknown keys).

`TimeHarness` builds the app's own services from `AddJiraTime` with a `TestClock` (timers fire at once), a temp
store and `FakeJira` (a fake Jira answering by URL, `Pictures` included; `Myself`, `Writes`, `WorklogReads`, `Searches` and
`WorklogSearches` count what was sent, faults are in `FakeJiraFaults.cs`). Entries and sheets are built with
`TestEntries` and `TestSheets`, signed-in users with `TestUsers` (all in `Support/Entries.cs`).

