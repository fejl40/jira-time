# Jira Time

A timesheet for logging your hours on Jira issues.

Your hours are saved as Jira worklogs, so they show up in Jira and WorklogPRO.

## Install

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and git.

1. Get the code:
   ```sh
   git clone https://github.com/fejl40/jira-time.git
   cd jira-time
   ```
2. Start it:
   ```sh
   dotnet run --project src/JiraTime
   ```
3. Open http://localhost:5115 and connect to Jira:
   - **Jira Server / Data Center:** your Jira URL and a Personal Access Token (in Jira: your profile → Personal Access Tokens → Create token).
   - **Jira Cloud:** your Jira URL, your email and an API token.

To update later, run `git pull` in the `jira-time` folder and start it again.

## Use it

1. Pick a day. If it is short of hours, it comes with suggested rows.
2. Set the issue, the hours and an optional comment.
3. Press **Save all**. It shows what will change in Jira; press **Save** to send it.

To do a whole period at once, press **Fill period** and then **Save all**.

Some other things you can do:

- **Time off…** marks a day, or part of it, as vacation, sick or other time off. Days off stand out in the timesheet with their own icon and colour.
- **Export** gives you a month in the agency's format, ready to paste into Excel.
- **Settings** sets your hours per weekday.
- Keys: **←** and **→** change the day, **N** adds a row, **Ctrl+S** opens Save all.

## Your data

- **Hours** are kept in Jira.
- **Days off, work hours and export values** are kept on this computer in `src/JiraTime/data/`. Back that folder up.
- **Your token** is encrypted and kept in your browser.
- **A log of every Jira request** (under **Logs**) is kept on this computer in `src/JiraTime/data/logs/` for 30 days. It holds what was sent and received, such as worklog comments and issue titles, but never your token. **Clear logs** deletes yours.

If Jira is down, your changes wait and are sent when you press **Sync now**.

## For developers

How to build, test and change the app is in [AGENTS.md](AGENTS.md). How the code is written is in [the coding principles](<backlog/docs/doc-1 - Coding-principles.md>). Tasks are in `backlog/`.
