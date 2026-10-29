---
id: TASK-71.33
title: Share one fake issue search and TestClock in lookup tests
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:41'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 108000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Three classes write their own search handlers, JiraIssueLookup instances, JQL helpers and private clocks. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 No test class defines its own TimeProvider
- [x] #2 Only FakeJira defines a search handler and Jql helper
- [x] #3 Lookup tests use TimeHarness.Issues
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
FakeJira has an issue table (Issues, AddIssue) served by both search endpoints, filtered by key in (...), plus FakeJira.Jql; EpicLookup, PeriodIssues, SprintIssues, PeriodLoader and MonthExportLoader tests use it through TimeHarness, and ExpiringCacheTests uses TestClock. Moving the tests onto the harness surfaced intermittent IOExceptions: every collection started a FileSystemWatcher (one inotify instance each, 128 per user here), so FileDocumentCollection now starts its watcher on the first read, with a regression test.
<!-- SECTION:FINAL_SUMMARY:END -->
