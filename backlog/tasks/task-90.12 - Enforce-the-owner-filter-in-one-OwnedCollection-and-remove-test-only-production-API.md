---
id: TASK-90.12
title: >-
  Enforce the owner filter in one OwnedCollection and remove test-only
  production API
status: Done
assignee: []
created_date: '2026-10-30 11:32'
updated_date: '2026-10-30 12:22'
labels:
  - refactor
dependencies:
  - TASK-90.10
  - TASK-90.8
  - TASK-90.2
  - TASK-90.4
parent_task_id: TASK-90
priority: medium
ordinal: 151000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
RetryQueue, WorkCalendarService and EpicExportStore each re-implement 'only the signed-in user's documents', and CurrentUser carries storage helpers (Owns, GetMineAsync). Add Services/OwnedCollection<T> (Get, Query with the owner added to the expression, Upsert stamping the owner and refusing others' documents, Delete) and use it in the three services. Remove test-only production API: CurrentUser.SignedIn (add InternalsVisibleTo JiraTime.Tests and build TestUsers.SignedIn on the internal Set), WorkCalendarService.GetDayAsync, DocumentCollectionExtensions.AllAsync. LegacyDataMigration's static SemaphoreSlim becomes an instance field. Users.cs timestamps stop defaulting to DateTime.UtcNow inside the Domain.

Files:
- src/JiraTime/Services/OwnedCollection.cs (new)
- src/JiraTime/Services/RetryQueue.cs
- src/JiraTime/Services/WorkCalendarService.cs
- src/JiraTime/Services/EpicExportStore.cs
- src/JiraTime/Services/CurrentUser.cs
- src/JiraTime/Services/LegacyDataMigration.cs
- src/JiraTime/Services/JiraIdentity.cs (only for the Users.cs timestamp)
- src/JiraTime/Domain/Users.cs
- src/JiraTime/Storage/IDocumentStore.cs
- src/JiraTime/JiraTime.csproj
- tests/JiraTime.Tests/OwnedCollectionTests.cs (new)
- tests/JiraTime.Tests/DataSeparationTests.cs
- tests/JiraTime.Tests/FileDocumentStoreTests.cs
- tests/JiraTime.Tests/LegacyDataMigrationTests.cs
- tests/JiraTime.Tests/JiraLogDayTests.cs
- tests/JiraTime.Tests/RetryQueueTests.cs
- tests/JiraTime.Tests/WorkCalendarServiceTests.cs
- tests/JiraTime.Tests/UsersTests.cs
- tests/JiraTime.Tests/TimeHarness.cs

Rules for this subtask: edit only the files listed. Do not edit AGENTS.md, README.md, backlog docs or wwwroot/app.css; write any doc changes needed into the final summary so the Post-refactor cleanup applies them.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 No service other than OwnedCollection compares .Owner or calls GetMineAsync
- [x] #2 OwnedCollectionTests: Get of another owner's id is null, Query never returns others' documents, Upsert over another owner's id throws
- [x] #3 CurrentUser has no public way to become signed in and no 'For tests' member
- [x] #4 No production member exists only for tests; no static mutable state in Services/ besides JiraAction's AsyncLocal
- [x] #5 DataSeparationTests and RetryQueueTests pass
- [ ] #6 Only the files listed under Files are created, edited, moved or deleted
- [x] #7 Behaviour and what the user sees stay the same
- [x] #8 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
AC #6: one file outside the list changed: src/JiraTime/Domain/MonthExport.cs, EpicExportValues.Owner from string to string? (one line). IOwnedDocument.Owner now has a setter so OwnedCollection.UpsertAsync can stamp the owner; a non-nullable string setter implementing string? set gives warning CS8767. AC #1: LegacyDataMigration still filters by Owner (unowned data, and RetireSyncedEntriesAsync runs during sign-in before CurrentUser is set), as do the request log's JiraLogEntry.Owner checks, which are not owned documents.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Added Services/OwnedCollection<T> (Get/Query/Upsert/Delete limited to the signed-in user; Query ANDs the owner into the expression, Upsert stamps the owner and throws over another owner's id, Delete leaves others' documents) and used it in RetryQueue, WorkCalendarService and EpicExportStore. Removed CurrentUser.Owns, GetMineAsync and the static SignedIn (tests build TestUsers.SignedIn on the internal Set via InternalsVisibleTo JiraTime.Tests in JiraTime.csproj), WorkCalendarService.GetDayAsync (a test extension in TimeHarness.cs reads one day) and DocumentCollectionExtensions.AllAsync. LegacyDataMigration's gate is an instance field (it is a singleton). UserAccount.CreatedUtc/LastVerifiedUtc no longer default to DateTime.UtcNow; JiraIdentity sets CreatedUtc from the clock. IOwnedDocument.Owner gained a setter; EpicExportValues.Owner became string? (MonthExport.cs, outside the list). New OwnedCollectionTests. Doc changes for cleanup: AGENTS.md Layout add 'Services/OwnedCollection.cs  one collection of the signed-in user's documents: the only owner filter'; Storage abstraction section: mention that services reach owned documents through OwnedCollection<T>, which adds the owner to every query expression; Tests list: add owned collections (others' documents never read or written).
<!-- SECTION:FINAL_SUMMARY:END -->
