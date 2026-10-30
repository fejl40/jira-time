# Jira Time

A timesheet for logging your hours on Jira issues.

Your hours are saved as Jira worklogs, so they show up in Jira and WorklogPRO.

## Start it

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and [Node.js](https://nodejs.org).

```sh
dotnet run --project src/JiraTime
```

Open http://localhost:5115 and connect to Jira:

- **Jira Server / Data Center:** your Jira URL and a Personal Access Token.
- **Jira Cloud:** your Jira URL, your email and an API token.

## Use it

1. Pick a day. If it is short of hours, it comes with suggested rows.
2. Set the issue, the hours and an optional comment.
3. Press **Save day**.

To do a whole period at once, press **Fill period** and then **Save all**.

Some other things you can do:

- **Time off…** marks a day, or part of it, as vacation, sick or other time off. Days off stand out in the timesheet with their own icon and colour.
- **Export** gives you a month in the agency's format, ready to paste into Excel.
- **Settings** sets your hours per weekday.
- Keys: **←** and **→** change the day, **N** adds a row, **Ctrl+S** saves.

## Your data

- **Hours** are kept in Jira.
- **Days off, work hours and export values** are kept on this computer in `src/JiraTime/data/`. Back that folder up.
- **Your token** is encrypted and kept in your browser.

If Jira is down, your changes wait and are sent when you press **Sync now**.

## For developers

How to build, test and change the app is in [AGENTS.md](AGENTS.md). How the code is written is in [the coding principles](<backlog/docs/doc-1 - Coding-principles.md>). Tasks are in `backlog/`.
