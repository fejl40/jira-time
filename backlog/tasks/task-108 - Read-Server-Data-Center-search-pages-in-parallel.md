---
id: TASK-108
title: Read Server/Data Center search pages in parallel
status: Done
assignee: []
created_date: '2026-10-30 15:19'
updated_date: '2026-10-30 15:25'
labels:
  - jira
  - performance
dependencies: []
priority: medium
ordinal: 173000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Every search reads its pages one after another. On Server/DC the first page gives total and maxResults, so the remaining pages (startAt = 50, 100, ...) can all be asked at once. Cloud keeps reading page by page, since each page gives the token for the next. Large searches (many worklog issues, big sprints) then take about two round trips instead of one per page.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 On Server/DC, after the first page, the remaining pages of a search go out at the same time and are joined in order
- [x] #2 Cloud still follows nextPageToken page by page
- [x] #3 A page that comes back shorter or with a changed total still gives every issue once (no duplicates, none dropped compared to sequential reading)
- [x] #4 Tests show the requests overlap and the results are unchanged
- [x] #5 Build has 0 warnings and all tests pass
<!-- AC:END -->
