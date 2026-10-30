---
id: TASK-80
title: Investigate Jira rate limits for PAT requests
status: Done
assignee: []
created_date: '2026-10-30 10:03'
updated_date: '2026-10-30 10:06'
labels:
  - spike
dependencies: []
ordinal: 129000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Find out whether Jira (Server/Data Center with a Personal Access Token, and Cloud with an API token) limits how many requests a user can send, how a limit shows (status, headers), and what that means for sending some of the app's requests in parallel or merging them into fewer requests. Inventory the requests the app sends per user action (Open period, Save all, Sync now) and recommend which to parallelize or batch, with follow-up tasks.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Rate limits for Data Center PATs and Cloud API tokens are described with sources
- [x] #2 How a limit shows (429, Retry-After, X-RateLimit headers) and how the app handles it today is described
- [x] #3 The requests per user action are listed with counts from the request log
- [x] #4 A recommendation says which requests to run in parallel and which to merge, with follow-up tasks created
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
## Findings

**Jira Data Center / Server (PAT):** rate limiting exists since 8.6 but is off until a system admin enables it (Administration > System > Rate limiting). It is per user and per node, a token bucket (requests allowed per interval, max bucket size), set by the admin, with per-user exemptions. Only external REST requests count. The docs list Basic auth, OAuth and session cookies; PATs are not named, so assume they count. Limited requests get 429 with `Retry-After`, `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-FillRate`, `X-RateLimit-Interval-Seconds`. No concurrency limit; Atlassian advises low concurrency.
Sources: https://confluence.atlassian.com/adminjiraserver105/improving-instance-stability-with-rate-limiting-1561364681.html, https://confluence.atlassian.com/adminjiraserver107/adjusting-your-code-for-rate-limiting-1595115640.html

**Jira Cloud (API token):** always on. Burst limit per endpoint and method (token bucket), per-issue writes (20 per 2 s, 100 per 30 s), and an hourly points quota. 429 with `Retry-After`, `RateLimit-Reason`, `X-RateLimit-Reset`, `RateLimit-Policy`. Atlassian suggests 5–10 requests in flight at most; more workers don't add throughput.
Source: https://developer.atlassian.com/cloud/jira/platform/build-an-app-that-respects-rate-limits/

**The app today:** every request is sequential. No 429 handling: a 429 read shows as a Jira error, a 429 write goes to the retry queue.

**Requests per action (request log, mock Jira, one issue):** Open period 4 (2 searches + 1 worklog GET per issue, twice: period range and last 60 days); Save all of 11 rows 15 (11 POSTs + read-back); Sync now 4. With N issues in the last 60 days, Open period is about 4 + 2N sequential requests (plus sprint/period issue searches when not cached), so 15 issues ≈ 34.

**Recommendation:** first handle 429 and add a per-user gate (TASK-81), then cut requests by reading worklogs once with embedded worklogs in the search (TASK-82, the biggest win: ≈ 34 → ≈ 4), then run the remaining independent reads in parallel, at most 4 at once (TASK-83). Keep writes sequential. Also found: the log files start with a BOM (TASK-84).
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Jira DC rate limits are opt-in per user per node (token bucket), Cloud always limits; both answer 429 with Retry-After. The app has no 429 handling and sends everything sequentially; Open period costs about 4 + 2N requests. Follow-ups: TASK-81 (429 + per-user gate), TASK-82 (fewer worklog reads), TASK-83 (parallel reads), TASK-84 (BOM in logs).
<!-- SECTION:FINAL_SUMMARY:END -->
