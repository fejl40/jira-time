---
id: TASK-90.1
title: Rebuild day editing around one SheetDay and a single save plan
status: Done
assignee: []
created_date: '2026-10-30 11:32'
updated_date: '2026-10-30 11:51'
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
- [x] #1 No DayWork method takes a saved list and DayWork has no public mutable collections
- [x] #2 DayEditor and DayRow take one SheetDay parameter
- [x] #3 The time-off branching lives in PeriodSheet and is tested (partial day with only suggestions, whole day, undo)
- [x] #4 Exactly one place decides whether a day can be saved; WorklogSync never reads BadHours or walks PeriodSheet
- [x] #5 Computing errors or planning a save leaves DayWork and saved entries unchanged, proven by a test
- [x] #6 DaySave has its own file; WorklogSync has no test-only public method
- [x] #7 DayWork.cs and PeriodSheet.cs are <= 150 lines and every test file touched is <= 250 lines
- [x] #8 Only the files listed under Files are created, edited, moved or deleted
- [x] #9 Behaviour and what the user sees stay the same
- [x] #10 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Day editing is rebuilt around SheetDay (PeriodSheet.Day(date)): the date, DayInfo, saved rows and live unsaved changes read and edited together (Rows, RowsToSave, Total, Expected, Logged, Missing, Progress, IsComplete, HasChanges, HasOwnChanges, HasOnlySuggestions, Errors, Plan, PreviousWorkedDay; edits take the row only). Reading a day leaves nothing behind; the first edit makes its DayWork. DayWork now knows single entries only, its collections are private (Drafts is a read-only view, BadHourRows/UnreadHours for unreadable hours) and no method takes a saved list. DaySave moved to Domain/DaySave.cs and is the one place that decides whether a day can be saved (validation, more than 24 hours, unreadable hours via DaySave.BadHours, which replaces DayChanges.BadHours); it plans on copies and RowsToSend returns fresh stamped copies, so drafts and saved entries never change (tested). PeriodSheet.PlanSaveAll gives the plans; WorklogSync.SaveDaysAsync(connection, plans) is its one save entry point (SaveDayAsync and DayResult.DayError removed; SaveAllResult now holds every planned day's DayResult in Days, with Saved/NeedsWork/ChangedInJira derived). WorklogWriter is internal. The time-off branching moved from DayPanel into PeriodSheet.MarkDayOffAsync(day, wholeDay, saveAndReload) and UnmarkDayOffAsync(day, saveAndReload), tested for a partial day with only suggestions, a partial day with own rows, a whole day and undo. PeriodSheet.Day(date) now returns SheetDay; the DayInfo is PeriodSheet.Info(date); Missing/Progress/IsComplete/UnsavedDayTotal moved to SheetDay (CanFill and UnsavedFor stay on the sheet for PeriodHeader and PeriodCell). DayEditor and DayRow take one SheetDay. Tests: new SheetDayTests and DaySaveTests (DaySave tests moved out of WorklogMatchingTests); TimeHarness.SaveDayAsync plans the day and goes through SaveDaysAsync; SaveDayTests split (the check-against-Jira tests and the day-off save moved to SaveAllTests); every touched test file is at most 250 lines. Only listed files were touched.

Doc changes for the cleanup (AGENTS.md):
- Layout: add 'Domain/SheetDay.cs  one day of a period: saved rows and unsaved changes read and edited together (rows, totals, errors, fill, copy), the editor's row states'; add 'Domain/DaySave.cs  the day save plan: whether a day can be saved (errors include unreadable hours), what is removed, sent and checked; works on copies'; change DayWork.cs to 'one day's unsaved changes: edited copies, removals, new rows, suggestions, unreadable hours (single entries only)'; change WorklogMatching.cs to 'worklogs as entries, waiting work laid over them, comparing the two' (drop 'the day save plan'); PeriodSheet.cs: 'a period being worked on: saved data, each day's unsaved work (Day(date) gives a SheetDay), suggestions, days off, the Save all plans'.
- Logging a day / Save all: mention that Save all sends PeriodSheet.PlanSaveAll to WorklogSync.SaveDaysAsync (one changed-in-Jira search for every day) and that DaySave decides whether a day can be saved.
- Workdays: time off's effect on the day's work is PeriodSheet.MarkDayOffAsync/UnmarkDayOffAsync (around saving the day off and reloading).
- Tests paragraph: add the day (SheetDay) and the save plan (DaySave, always on copies), and days off on the sheet.
<!-- SECTION:FINAL_SUMMARY:END -->
