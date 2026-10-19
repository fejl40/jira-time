# Jira Time

Local Blazor (Interactive Server, .NET 10) + Tailwind v4 timesheet for logging hours on Jira issues.
See README.md for how it works and where data lives.

## Build and test

```sh
dotnet build              # also compiles Tailwind (needs Node.js)
dotnet test
dotnet run --project src/JiraTime   # http://localhost:5115
```

- App: `src/JiraTime`. Tests: `tests/JiraTime.Tests` (xUnit). Solution: `JiraTime.slnx`.
- New features come with tests. Build and tests must pass before every push.
- Never commit `data/` (encryption keys and time entries).

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
