---
id: TASK-109
title: Allow more Jira requests in flight per host
status: Done
assignee: []
created_date: '2026-10-30 15:19'
updated_date: '2026-10-30 15:20'
labels:
  - jira
  - performance
dependencies: []
priority: medium
ordinal: 174000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The resilience pipeline allows 4 requests to a Jira host at once (TASK-83, the cautious end of Atlassian's advice). Carl wants the app aggressive with Jira: raise it to 10, the top of what Atlassian suggests for Cloud. The 429 handling (retry with Retry-After, the circuit breaker shared by every user) stays as it is and is what keeps this safe; writes stay one at a time.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 JiraResilience.MaxInFlight is 10
- [x] #2 Tests for the cap and the queue use the constant and pass
- [x] #3 AGENTS.md says 10 instead of 4
- [x] #4 Build has 0 warnings and all tests pass
<!-- AC:END -->
