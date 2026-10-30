---
id: TASK-90.16
title: Post-refactor cleanup
status: To Do
assignee: []
created_date: '2026-10-30 11:32'
labels:
  - refactor
dependencies:
  - TASK-90.1
  - TASK-90.2
  - TASK-90.3
  - TASK-90.4
  - TASK-90.5
  - TASK-90.6
  - TASK-90.7
  - TASK-90.8
  - TASK-90.9
  - TASK-90.10
  - TASK-90.11
  - TASK-90.12
  - TASK-90.13
  - TASK-90.14
  - TASK-90.15
parent_task_id: TASK-90
priority: high
ordinal: 155000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
After every other refactor subtask is Done: remove leftover shims and dead code, fix usings, apply the doc changes the subtasks listed in their final summaries to AGENTS.md (Layout, How it works, Tests), README.md and backlog doc-1, check the folder structure against the coding principles (folders with fewer than three files, files over 250 lines), rebuild wwwroot/app.css with Node and commit it, run the full build (0 warnings) and test suite, and fix anything that fails. Browser-check the timesheet, Save all, time off, export and logs against the mock Jira.

Files:
- Any file, as needed for cleanup
- AGENTS.md
- README.md
- backlog/docs/doc-1 - Coding-principles.md
- src/JiraTime/wwwroot/app.css

Rules for this subtask: edit only the files listed. Do not edit AGENTS.md, README.md, backlog docs or wwwroot/app.css; write any doc changes needed into the final summary so the Post-refactor cleanup applies them.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 No leftover shims, unused members or stale usings (checked with grep and the compiler)
- [ ] #2 AGENTS.md Layout lists every file and folder under src/JiraTime and How it works matches the code
- [ ] #3 No folder under src/JiraTime has fewer than three files without a reason, and no file is over 250 lines
- [ ] #4 wwwroot/app.css is rebuilt and committed
- [ ] #5 Browser check of timesheet, Save all, time off, export and logs passes against the mock Jira
- [ ] #6 Only the files listed under Files are created, edited, moved or deleted
- [ ] #7 Behaviour and what the user sees stay the same
- [ ] #8 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
