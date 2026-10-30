---
id: TASK-90.13
title: Build the test harness from the app's DI and split it by concept
status: Done
assignee: []
created_date: '2026-10-30 11:32'
updated_date: '2026-10-30 12:34'
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
- [x] #1 Adding a constructor parameter to an app service needs no harness change; TimeHarness has no new XxxService(...) for app services
- [x] #2 TimeHarness.cs holds only TimeHarness, UserData and TestUsers and is <= 150 lines
- [x] #3 No test registers /myself unless it needs a non-default user and no test declares its own MyselfAsync
- [x] #4 FakeJira.Client is gone and request-count assertions use the named queries
- [x] #5 Same number of tests, all green
- [x] #6 Only the files listed under Files are created, edited, moved or deleted
- [x] #7 Behaviour and what the user sees stay the same
- [x] #8 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Added services.AddJiraTime(dataDir) (Services/AppServices.cs): storage, data protection, TimeProvider, the scoped/singleton data services, the Jira request log and AddJiraHttp. Program.cs calls it and registers only the UI services (MotionScript, Modals, ShownPeriod). TimeHarness builds a ServiceProvider from AddJiraTime(Files.Path), swapping in TestClock (TimeProvider), the TempStore's store (IDocumentStore) and FakeJira (ConfigureHttpClientDefaults primary handler); Carl's tab is a DI scope whose CurrentUser is signed in via TestUsers.SignIn; services are read with Get<T>(); For(key) is another scope; Identity(tab) uses ActivatorUtilities; new MyselfAsync(connection?). TestClock and TempStore moved to their own files. FakeJira answers /myself with Myself (Carl) by default, has Writes and WorklogReads (FakeJira.cs) and Searches and WorklogSearches (FakeJiraSearch.cs); faults, 429s, offline and hold/in-flight knobs moved to FakeJiraFaults.cs; Client() deleted. Tests drop their /myself registrations and own MyselfAsync and count with the named queries. The request log of a harness now lives in <temp>/logs/jira as in the app. The test-only WorkCalendarReading.GetDayAsync extension (from TASK-90.12) stays at the end of TimeHarness.cs (149 lines): moving it would need an unlisted file. 566 tests, same as before. Doc changes for cleanup: AGENTS.md Layout add 'Services/AppServices.cs  AddJiraTime: registers the app's services, for Program.cs and the tests' and change Program.cs to 'AddJiraTime plus the UI's own services (modals, motion, shown period)'; the Tests section can say TimeHarness builds the app's services from AddJiraTime with a TestClock, a temp store and FakeJira (Myself, Writes, WorklogReads, Searches, WorklogSearches; faults in FakeJiraFaults.cs).
<!-- SECTION:FINAL_SUMMARY:END -->
