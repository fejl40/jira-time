---
id: TASK-81
title: Handle Jira rate limits (429) with Retry-After and a per-user request gate
status: To Do
assignee: []
created_date: '2026-10-30 10:05'
labels: []
dependencies: []
ordinal: 130000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Jira answers 429 when a rate limit is hit (Data Center: per user per node, token bucket, when an admin enables it; Cloud: burst per endpoint, per-issue writes, hourly points). Today JiraHttp treats 429 as any error: reads fail and writes go to the retry queue. Wait for Retry-After (with jitter, capped) and retry, honour X-RateLimit-Remaining, and send at most a few requests at a time per user through one shared gate so parallel work never bursts past the limit.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 A 429 is retried after Retry-After (plus jitter), up to a cap, then fails as today
- [ ] #2 Concurrent requests share one backoff per user instead of each retrying on its own
- [ ] #3 At most 4 Jira requests per user run at once
- [ ] #4 The waiting text and the request log say when the app is waiting on a rate limit
- [ ] #5 Tests cover retry, cap and the shared gate
<!-- AC:END -->
