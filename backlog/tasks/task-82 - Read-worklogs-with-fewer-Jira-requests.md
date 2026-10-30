---
id: TASK-82
title: Read worklogs with fewer Jira requests
status: Done
assignee: []
created_date: '2026-10-30 10:05'
updated_date: '2026-10-30 10:26'
labels: []
dependencies:
  - TASK-81
ordinal: 131000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Opening a period reads worklogs twice (the period range and the last 60 days for recent picks), each as one search plus one GET per issue, so N issues cost about 2 + 2N requests. Read one range that covers both, and ask the search for fields=summary,worklog so each issue's worklogs come with it; fetch an issue's worklogs separately only when Jira says there are more than it sent.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Opening a period reads worklogs once, over one range covering the period, copy source and recent days
- [x] #2 Issues whose worklogs fit in the search answer need no extra request
- [x] #3 Issues with more worklogs than the search sent are read in full as today
- [x] #4 Tests count the requests for a period with several issues
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
The worklog search asks for fields=summary,worklog and uses the worklogs Jira sends with each issue when they are all there (total <= sent); only an issue with more is read in full, as before. PeriodLoader reads one range covering the period, the copy source, the previous period and the last 60 days when they are close; an old period and the recent days are still read apart. The 15-minute direct read of just-saved issues and the one-minute TimeLog cache are unchanged. FakeJira embeds worklogs in searches (with a limit for the truncated case); tests count the requests for a period with five issues (4 in all, one worklog search, no per-issue reads). Mock Jira, 8 issues: Open period 21 -> 4 requests (18 -> 1 with issue lists cached), Sync now 18 -> 1.
<!-- SECTION:FINAL_SUMMARY:END -->
