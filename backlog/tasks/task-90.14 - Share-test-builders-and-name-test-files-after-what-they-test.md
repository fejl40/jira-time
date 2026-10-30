---
id: TASK-90.14
title: Share test builders and name test files after what they test
status: Done
assignee: []
created_date: '2026-10-30 11:32'
updated_date: '2026-10-30 12:46'
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
- [x] #1 No test file declares its own TimeEntry factory except for fields nobody else uses
- [x] #2 No test file uses using static on another *Tests class and the PeriodSheet-with-days setup exists once
- [x] #3 Every *Tests.cs name is a type or concept in src/, and no production class has two test files without a documented split
- [x] #4 All test names are sentences
- [x] #5 Same number of tests, all green
- [x] #6 Only the files listed under Files are created, edited, moved or deleted
- [x] #7 Behaviour and what the user sees stay the same
- [x] #8 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Added tests/JiraTime.Tests/Entries.cs: TestEntries (Saved, Draft, Waiting, Row, Summary; Monday 19 Oct 2026 when no date), TestSheets (For(today, saved), Days(period, off)) and TestUsers (moved from TimeHarness.cs, plus CarlsProfile). Fifteen test files now build entries from it; the only local builders left set fields nobody else uses (UpdatedUtc in DayFillerTests, Id in DaySaveTests, a worklog on another issue in TimeEntryTests) or act on a DayWork/PeriodSheet. TestEntries.Saved sets JiraWorklogIssueKey to the entry's issue, as an entry read from Jira has it. DayWorkEditTests became DayWorkTests.Edits.cs (partial DayWorkTests, split documented), so no using static on another test class. RetireLocalTimeTests merged into LegacyDataMigrationTests, IssueFilterTests into IssueChoicesTests, DataSeparationTests into OwnedCollectionTests (now on TimeHarness). SprintIssues/PeriodIssues/EpicLookup tests renamed JiraIssueLookupSprintTests/PeriodTests/EpicTests with the split documented; SaveDayTests renamed WorklogSyncTests (SaveAllTests stays, split documented); ParallelReadsTests says why it is named after a behaviour. Non-sentence and method-named tests rewritten (Classify, Easter_sunday, Danish_holidays_2026, Reads_hours, Contains_..., Clear_..., Total_..., Matches_..., ToEntry_..., Merge_..., SameAsSynced_..., FileDocumentStore Upsert/Get/Query/Delete). JiraWorklogClientTests and the epic lookup tests use FakeJira's WorklogReads/Writes/Searches. 566 tests before and after, 0 warnings. Doc changes for cleanup: AGENTS.md Tests section could mention tests/JiraTime.Tests/Entries.cs (TestEntries, TestSheets, TestUsers) next to TimeHarness and FakeJira; doc-1 Tests section could add 'Build entries and sheets with TestEntries and TestSheets (Entries.cs); a class tested in more than one file says why in its summary (partial DayWorkTests, JiraIssueLookup*Tests).'
<!-- SECTION:FINAL_SUMMARY:END -->
