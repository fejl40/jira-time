---
id: TASK-99
title: Put the tests into folders that mirror the app
status: To Do
assignee: []
created_date: '2026-10-30 13:30'
labels:
  - refactor
dependencies: []
priority: low
ordinal: 164000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
tests/JiraTime.Tests has 66 files in one folder, which makes it hard to get an overview. Group them into folders that mirror src/JiraTime, so a test sits where its code does:
- Domain/: Period, TimeEntry, PeriodSheet (both files), SheetDay, DayWork (both files), DaySave, DayFiller, PeriodSummary, IssueChoices, WorklogMatching, RecentWork, WorkCalendar, UnsavedChanges, Milestones, Users, MonthExport, TypedHours, JiraConnection
- Domain/JiraLog/: JiraLogDay, JsonBody, RequestToasts
- Storage/: FileDocumentStore
- Services/: OwnedCollection, WorkCalendarService, PeriodLoader, EpicExportStore, MonthExportLoader, ExpiringCache, UserTimeZone
- Services/SignIn/: SignInSession, JiraIdentity, LegacyDataMigration
- Services/Worklogs/: TimeLog, RetryQueue, WorklogSync, SaveAll, ParallelReads
- Services/Jira/: JiraWorklogClient, JiraIssueLookup* (three files), JiraSearch, JiraResilience, Jql, AtlassianDocument
- Services/JiraLog/: JiraAction, JiraRequestLog, JiraRequestFeed
- Components/: Format, DayLook, Links, Motion, Modals, JiraWaits, ShownPeriod, Favicon
- Support/: the shared test helpers (TimeHarness, TestClock, TempStore, FakeJira, FakeJiraSearch, FakeJiraFaults, Entries)
Move with git mv so history follows. Namespaces stay JiraTime.Tests, so no test or helper changes beyond the move. The exact split may be adjusted where a test clearly belongs elsewhere.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Every test file sits in the folder of the code it tests, mirroring src/JiraTime (Domain, Storage, Services and its subfolders, Components)
- [ ] #2 Shared test helpers are in one Support folder
- [ ] #3 Files are moved with git mv; namespaces and test names don't change
- [ ] #4 dotnet test finds and passes the same number of tests as before
- [ ] #5 AGENTS.md (Tests) and doc-1 (Tests follow the same concepts) say where tests and helpers live
- [ ] #6 dotnet build has 0 warnings
<!-- AC:END -->
