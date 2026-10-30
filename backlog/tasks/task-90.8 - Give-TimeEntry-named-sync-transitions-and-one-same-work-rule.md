---
id: TASK-90.8
title: Give TimeEntry named sync transitions and one same-work rule
status: To Do
assignee: []
created_date: '2026-10-30 11:32'
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
- [ ] #1 No code outside TimeEntry assigns SyncPending, DeletePending, SyncError, JiraWorklogId or JiraWorklogIssueKey
- [ ] #2 Each transition has a unit test
- [ ] #3 Comment normalisation exists once and the four comparisons use the shared helpers, with a test pinning each one's null versus empty behaviour
- [ ] #4 Retry-queue files with a description field and legacy start/end files still load
- [ ] #5 TimeEntry.cs holds only the entry; no DateTime.UtcNow in Domain/TimeEntry.cs
- [ ] #6 Only the files listed under Files are created, edited, moved or deleted
- [ ] #7 Behaviour and what the user sees stay the same
- [ ] #8 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
