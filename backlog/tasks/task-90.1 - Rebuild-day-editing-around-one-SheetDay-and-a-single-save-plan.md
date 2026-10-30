---
id: TASK-90.1
title: Rebuild day editing around one SheetDay and a single save plan
status: To Do
assignee: []
created_date: '2026-10-30 11:32'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-90
priority: high
ordinal: 140000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Domain: DayWork needs the saved rows passed into almost every method and exposes mutable collections (Removed, Drafts, Suggested, BadHours) that PeriodSheet, WorklogSync and DayRow reach into. Add SheetDay (PeriodSheet.Day(date)) binding the date, DayInfo, saved rows and that day's DayWork, with read members (Rows, Total, Expected, Missing, HasChanges, Errors, PreviousWorkedDay) and edits without a saved parameter. Make DayWork's collections private. Move the time-off branching (only suggestions / partial / whole day / undo) from DayPanel.razor into PeriodSheet with tests. Move DaySave out of WorklogMatching.cs into Domain/DaySave.cs; one place decides whether a day can be saved (plan errors include bad hours); planning works on copies and never mutates drafts or saved entries. WorklogSync takes plans from the Domain and keeps one public save entry point; delete the test-only SaveDayAsync (tests go through the harness), make WorklogWriter internal or injected. DayEditor and DayRow take one SheetDay instead of Work + Saved + Expected + Previous. Split SaveDayTests so every test file is <= 250 lines.

Files:
- src/JiraTime/Domain/SheetDay.cs (new)
- src/JiraTime/Domain/DaySave.cs (new)
- src/JiraTime/Domain/DayWork.cs
- src/JiraTime/Domain/PeriodSheet.cs
- src/JiraTime/Domain/UnsavedChanges.cs
- src/JiraTime/Domain/WorklogMatching.cs
- src/JiraTime/Services/WorklogSync.cs
- src/JiraTime/Services/WorklogWriter.cs
- src/JiraTime/Components/Pages/Timesheet.razor
- src/JiraTime/Components/Sheet/DayPanel.razor
- src/JiraTime/Components/Sheet/DayEditor.razor
- src/JiraTime/Components/Sheet/DayRow.razor
- src/JiraTime/Components/Sheet/PeriodGrid.razor
- src/JiraTime/Components/Sheet/DayList.razor
- src/JiraTime/Components/Sheet/SaveAllModal.razor
- tests/JiraTime.Tests/SheetDayTests.cs (new)
- tests/JiraTime.Tests/DaySaveTests.cs (new)
- tests/JiraTime.Tests/DayWorkTests.cs
- tests/JiraTime.Tests/DayWorkEditTests.cs
- tests/JiraTime.Tests/PeriodSheetTests.cs
- tests/JiraTime.Tests/UnsavedChangesTests.cs
- tests/JiraTime.Tests/SaveAllTests.cs
- tests/JiraTime.Tests/SaveDayTests.cs
- tests/JiraTime.Tests/RetryQueueTests.cs
- tests/JiraTime.Tests/WorklogMatchingTests.cs
- tests/JiraTime.Tests/TimeHarness.cs

Rules for this subtask: edit only the files listed. Do not edit AGENTS.md, README.md, backlog docs or wwwroot/app.css; write any doc changes needed into the final summary so the Post-refactor cleanup applies them.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 No DayWork method takes a saved list and DayWork has no public mutable collections
- [ ] #2 DayEditor and DayRow take one SheetDay parameter
- [ ] #3 The time-off branching lives in PeriodSheet and is tested (partial day with only suggestions, whole day, undo)
- [ ] #4 Exactly one place decides whether a day can be saved; WorklogSync never reads BadHours or walks PeriodSheet
- [ ] #5 Computing errors or planning a save leaves DayWork and saved entries unchanged, proven by a test
- [ ] #6 DaySave has its own file; WorklogSync has no test-only public method
- [ ] #7 DayWork.cs and PeriodSheet.cs are <= 150 lines and every test file touched is <= 250 lines
- [ ] #8 Only the files listed under Files are created, edited, moved or deleted
- [ ] #9 Behaviour and what the user sees stay the same
- [ ] #10 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
