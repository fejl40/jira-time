---
id: TASK-39
title: Simplify worklog import
status: Done
assignee: []
created_date: '2026-10-27 09:03'
updated_date: '2026-10-27 09:06'
labels: []
dependencies: []
ordinal: 39000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
WorklogSync.ImportAsync compares and copies worklog fields in two places and does several jobs in one method.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Comparing an entry with a worklog and taking over its values each live in one place
- [x] #2 Import is split into small steps with names
- [x] #3 Existing import tests pass
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
WorklogMatching holds comparing an entry with a worklog, taking over its values, building an entry from a worklog and the save-day unchanged check. ImportAsync is split into LinkedEntriesAsync, ApplyRemoteAsync and ReconcileMissedAsync. Behaviour unchanged; 7 new tests.
<!-- SECTION:FINAL_SUMMARY:END -->
