---
id: TASK-90.8
title: Give TimeEntry named sync transitions and one same-work rule
status: Done
assignee: []
created_date: '2026-10-30 11:32'
updated_date: '2026-10-30 12:05'
labels:
  - refactor
dependencies:
  - TASK-90.1
  - TASK-90.6
parent_task_id: TASK-90
priority: medium
ordinal: 147000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
TimeEntry is the queue document, the edited row, the view of a Jira worklog and the sync state, and four classes set SyncPending, DeletePending, SyncError, JiraWorklogId and JiraWorklogIssueKey by hand. Add transitions (Sent, Failed, MarkRemovalPending, LeftOldIssue, TakeWorklogFrom) and make those setters private or init. Move TimeSpanSum to Domain/Hours.cs and the legacy start/end JSON handling to LegacyDataMigration. Rename Description/SetDescription to Comment/SetComment keeping [JsonPropertyName("description")]. Default timestamps no longer read DateTime.UtcNow inside TimeEntry (callers pass the clock time). Add TimeEntry.SameComment and SameWork and build DayWork.Differs, RowChange.CommentChanged, WorklogMatching.Matches and SameAsSynced on them (SameAsSynced stays exact, pinned by a test).

Files:
- src/JiraTime/Domain/TimeEntry.cs
- src/JiraTime/Domain/Hours.cs (new)
- src/JiraTime/Domain/WorklogMatching.cs
- src/JiraTime/Domain/DaySave.cs
- src/JiraTime/Domain/DayWork.cs
- src/JiraTime/Domain/SheetDay.cs
- src/JiraTime/Domain/DayFiller.cs
- src/JiraTime/Domain/RecentWork.cs
- src/JiraTime/Domain/UnsavedChanges.cs
- src/JiraTime/Services/WorklogWriter.cs
- src/JiraTime/Services/JiraWorklogClient.cs
- src/JiraTime/Services/Jira/JiraWorklogJson.cs
- src/JiraTime/Services/LegacyDataMigration.cs
- src/JiraTime/Components/Sheet/DayRow.razor
- src/JiraTime/Components/Sheet/SaveAllModal.razor
- tests/JiraTime.Tests/TimeEntryTests.cs
- tests/JiraTime.Tests/RetryQueueTests.cs
- tests/JiraTime.Tests/LegacyDataMigrationTests.cs
- tests/JiraTime.Tests/DayWorkEditTests.cs
- tests/JiraTime.Tests/WorklogMatchingTests.cs
- any other test file whose only change is .Description -> .Comment

Rules for this subtask: edit only the files listed. Do not edit AGENTS.md, README.md, backlog docs or wwwroot/app.css; write any doc changes needed into the final summary so the Post-refactor cleanup applies them.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 No code outside TimeEntry assigns SyncPending, DeletePending, SyncError, JiraWorklogId or JiraWorklogIssueKey
- [x] #2 Each transition has a unit test
- [x] #3 Comment normalisation exists once and the four comparisons use the shared helpers, with a test pinning each one's null versus empty behaviour
- [x] #4 Retry-queue files with a description field and legacy start/end files still load
- [x] #5 TimeEntry.cs holds only the entry; no DateTime.UtcNow in Domain/TimeEntry.cs
- [ ] #6 Only the files listed under Files are created, edited, moved or deleted
- [x] #7 Behaviour and what the user sees stay the same
- [x] #8 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Files touched outside the list (minimal, forced by the init-only setters): tests DaySaveTests, SaveDayTests, SheetDayTests, MonthExportTests now use transitions/initializers instead of setting sync fields by hand; UnsavedChangesTests and DayFillerTests got one new test each (CommentChanged null vs empty; unsaved rows count as newest in GuessIssue). AC #6 therefore not ticked. JiraWorklogClient.cs and SaveAllModal.razor needed no change. WorklogMatching.ToEntry still sets the sync fields through init in its object initializer. Failed() on a queued removal also sets SyncPending (it already was true for every removal).
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
TimeEntry now holds only the entry: sync state (JiraWorklogId, JiraWorklogIssueKey, SyncPending, DeletePending, SyncError) is init-only and changes through Sent, Failed, MarkRemovalPending, LeftOldIssue and TakeWorklogFrom, each unit-tested. Description/SetDescription became Comment/SetComment (still stored as "description"). CleanComment, SameComment and SameWork are the one comment rule; DayWork.IsChanged, RowChange.CommentChanged and Matches use SameComment (null = empty), SameAsSynced uses SameWork plus an exact comment check (pinned). TimeSpanSum moved to Domain/Hours.cs. CreatedUtc/UpdatedUtc have no UtcNow default: DaySave stamps new rows with the clock time on save, and DayFiller.GuessIssue treats unsaved rows as the latest work. Legacy start/end entries get their hours in LegacyDataMigration.ClaimAsync (TimedEntry document) instead of TimeEntry JSON setters. Doc changes for cleanup: AGENTS.md layout add 'Domain/Hours.cs  adding up hours (TimeSpanSum)'; Domain/TimeEntry.cs line could read 'one block of work on an issue on a day, and its named sync transitions'; Services/LegacyDataMigration.cs line add 'entries saved with start and end times'; Tests list may mention TimeEntry sync transitions.
<!-- SECTION:FINAL_SUMMARY:END -->
