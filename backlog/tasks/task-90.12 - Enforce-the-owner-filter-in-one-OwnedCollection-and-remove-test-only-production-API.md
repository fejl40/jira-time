---
id: TASK-90.12
title: >-
  Enforce the owner filter in one OwnedCollection and remove test-only
  production API
status: To Do
assignee: []
created_date: '2026-10-30 11:32'
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
- [ ] #1 No service other than OwnedCollection compares .Owner or calls GetMineAsync
- [ ] #2 OwnedCollectionTests: Get of another owner's id is null, Query never returns others' documents, Upsert over another owner's id throws
- [ ] #3 CurrentUser has no public way to become signed in and no 'For tests' member
- [ ] #4 No production member exists only for tests; no static mutable state in Services/ besides JiraAction's AsyncLocal
- [ ] #5 DataSeparationTests and RetryQueueTests pass
- [ ] #6 Only the files listed under Files are created, edited, moved or deleted
- [ ] #7 Behaviour and what the user sees stay the same
- [ ] #8 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
