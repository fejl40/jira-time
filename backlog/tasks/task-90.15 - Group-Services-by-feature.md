---
id: TASK-90.15
title: Group Services by feature
status: To Do
assignee: []
created_date: '2026-10-30 11:32'
labels:
  - refactor
dependencies:
  - TASK-90.14
parent_task_id: TASK-90
priority: low
ordinal: 154000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
21 files sit flat in Services/ while the REST clients live outside Services/Jira/. Move files keeping the namespace JiraTime.Services: Services/Jira/ gets JiraClient, JiraWorklogClient and JiraIssueLookup; Services/Worklogs/ gets TimeLog, RetryQueue, WorklogSync, WorklogWriter and RecentWorklogIssues; Services/SignIn/ gets CurrentUser, JiraIdentity, SignInSession and LegacyDataMigration. Moves only, no code changes.

Files:
- src/JiraTime/Services/*.cs (moves only, listed above)

Rules for this subtask: edit only the files listed. Do not edit AGENTS.md, README.md, backlog docs or wwwroot/app.css; write any doc changes needed into the final summary so the Post-refactor cleanup applies them.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Services/ has about 7 top-level files and every new folder has at least three
- [ ] #2 No code changes besides the moves
- [ ] #3 Only the files listed under Files are created, edited, moved or deleted
- [ ] #4 Behaviour and what the user sees stay the same
- [ ] #5 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
