---
id: TASK-90.14
title: Share test builders and name test files after what they test
status: To Do
assignee: []
created_date: '2026-10-30 11:32'
labels:
  - refactor
dependencies:
  - TASK-90.13
  - TASK-90.3
parent_task_id: TASK-90
priority: medium
ordinal: 153000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Fifteen test files define their own TimeEntry builders and the PeriodSheet-with-days setup is copied four times; DayWorkEditTests uses static members of DayWorkTests; several test files are named after things that don't exist (SprintIssues, PeriodIssues, EpicLookup, RetireLocalTime, SaveDay, DataSeparation, DayWorkEdit) or split one class over two files. Add tests/Entries.cs (TestEntries.Saved/Draft/Waiting/Row, TestSheets.For, TestUsers.CarlsProfile) and use it; merge RetireLocalTimeTests into LegacyDataMigrationTests and IssueFilterTests into IssueChoicesTests; rename the issue-lookup, save and data-separation test files after their types (or say why in a class summary); write the two method-named tests as sentences.

Files:
- tests/JiraTime.Tests/Entries.cs (new)
- tests/JiraTime.Tests/*.cs (test files only: builders, renames and merges; no file under src/)

Rules for this subtask: edit only the files listed. Do not edit AGENTS.md, README.md, backlog docs or wwwroot/app.css; write any doc changes needed into the final summary so the Post-refactor cleanup applies them.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 No test file declares its own TimeEntry factory except for fields nobody else uses
- [ ] #2 No test file uses using static on another *Tests class and the PeriodSheet-with-days setup exists once
- [ ] #3 Every *Tests.cs name is a type or concept in src/, and no production class has two test files without a documented split
- [ ] #4 All test names are sentences
- [ ] #5 Same number of tests, all green
- [ ] #6 Only the files listed under Files are created, edited, moved or deleted
- [ ] #7 Behaviour and what the user sees stay the same
- [ ] #8 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
