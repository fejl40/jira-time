---
id: TASK-90.13
title: Build the test harness from the app's DI and split it by concept
status: To Do
assignee: []
created_date: '2026-10-30 11:32'
labels:
  - refactor
dependencies:
  - TASK-90.12
  - TASK-90.11
parent_task_id: TASK-90
priority: medium
ordinal: 152000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
TimeHarness news up a dozen services by hand in the order Program.cs registers them, so every constructor change is made twice; TimeHarness.cs also holds UserData, TestUsers, TestClock and TempStore; six test classes paste the same /myself answer and count requests with ad-hoc URL predicates; FakeJira.Client() is dead. Extract services.AddJiraTime(dataDir) (Services/AppServices.cs) used by Program.cs and TimeHarness (which swaps in TestClock and FakeJira); move TestClock and TempStore to their own files; FakeJira answers /myself by default (Myself property), gets named request queries (Writes, WorklogReads, WorklogSearches, Searches) and its fault and concurrency knobs move to a partial FakeJiraFaults.cs; add TimeHarness.MyselfAsync(); tests use these.

Files:
- src/JiraTime/Services/AppServices.cs (new)
- src/JiraTime/Program.cs
- tests/JiraTime.Tests/TimeHarness.cs
- tests/JiraTime.Tests/TestClock.cs (new)
- tests/JiraTime.Tests/TempStore.cs (new)
- tests/JiraTime.Tests/FakeJira.cs
- tests/JiraTime.Tests/FakeJiraFaults.cs (new)
- tests/JiraTime.Tests/FakeJiraSearch.cs
- tests/JiraTime.Tests/JiraActionTests.cs
- tests/JiraTime.Tests/JiraResilienceTests.cs
- tests/JiraTime.Tests/LegacyDataMigrationTests.cs
- tests/JiraTime.Tests/SignInSessionTests.cs
- tests/JiraTime.Tests/JiraIdentityTests.cs
- tests/JiraTime.Tests/JiraRequestLogTests.cs
- tests/JiraTime.Tests/PeriodLoaderTests.cs
- tests/JiraTime.Tests/ParallelReadsTests.cs
- tests/JiraTime.Tests/SaveDayTests.cs
- tests/JiraTime.Tests/SaveAllTests.cs
- tests/JiraTime.Tests/TimeLogTests.cs
- tests/JiraTime.Tests/ExpiringCacheTests.cs
- tests/JiraTime.Tests/FileDocumentStoreTests.cs

Rules for this subtask: edit only the files listed. Do not edit AGENTS.md, README.md, backlog docs or wwwroot/app.css; write any doc changes needed into the final summary so the Post-refactor cleanup applies them.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Adding a constructor parameter to an app service needs no harness change; TimeHarness has no new XxxService(...) for app services
- [ ] #2 TimeHarness.cs holds only TimeHarness, UserData and TestUsers and is <= 150 lines
- [ ] #3 No test registers /myself unless it needs a non-default user and no test declares its own MyselfAsync
- [ ] #4 FakeJira.Client is gone and request-count assertions use the named queries
- [ ] #5 Same number of tests, all green
- [ ] #6 Only the files listed under Files are created, edited, moved or deleted
- [ ] #7 Behaviour and what the user sees stay the same
- [ ] #8 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
