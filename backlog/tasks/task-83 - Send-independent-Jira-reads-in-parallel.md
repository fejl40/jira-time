---
id: TASK-83
title: Send independent Jira reads in parallel
status: Done
assignee: []
created_date: '2026-10-30 10:05'
updated_date: '2026-10-30 10:34'
labels: []
dependencies:
  - TASK-81
ordinal: 132000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Period load runs sprint issues, period issues, worklogs and statuses one after another, and the per-issue worklog reads too. Run independent reads at the same time; the resilience pipeline's limiter from TASK-81 keeps at most 4 in flight per Jira host and queues the rest. Writes stay one at a time, so per-issue write limits and the changed-in-Jira check keep working.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Sprint issues, period issues and worklogs are read at the same time when a period opens
- [x] #2 Writes to Jira stay sequential
- [x] #3 Tests show the reads overlap and the cap holds
- [x] #4 Per-issue worklog reads are sent at the same time and the limiter keeps at most 4 in flight
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
PeriodLoader sends the worklog read, the sprint issues and the period issues at the same time and waits for all three before loading the sheet (days off, statuses and the store stay sequential). JiraWorklogClient reads the issues whose worklogs the search didn't send all at once in parallel; the resilience pipeline's limiter keeps at most 4 in flight per host and queues the rest. Writes are unchanged and stay one at a time. Checked thread safety: ExpiringCache and RecentWorklogIssues are concurrent collections, results are combined only after Task.WhenAll, IssueStatuses is filled after the reads, and FakeJira now locks. JiraAction (AsyncLocal) flows into the parallel requests, and a test checks they share the action id. Tests hold the fake Jira to show the three reads overlap, the per-issue reads cap at four, and saves send one POST at a time. Mock Jira with 100 ms latency: Open period 499 -> 264 ms (4 requests, 3 at once); without embedded worklogs 8 issue reads take about 310 ms with 4 in flight.
<!-- SECTION:FINAL_SUMMARY:END -->
