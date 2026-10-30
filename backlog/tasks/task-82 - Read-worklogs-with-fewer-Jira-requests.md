---
id: TASK-82
title: Read worklogs with fewer Jira requests
status: To Do
assignee: []
created_date: '2026-10-30 10:05'
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
- [ ] #1 Opening a period reads worklogs once, over one range covering the period, copy source and recent days
- [ ] #2 Issues whose worklogs fit in the search answer need no extra request
- [ ] #3 Issues with more worklogs than the search sent are read in full as today
- [ ] #4 Tests count the requests for a period with several issues
<!-- AC:END -->
