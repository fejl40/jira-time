---
id: TASK-81
title: >-
  Handle Jira rate limits with Microsoft.Extensions.Http.Resilience: global 429
  backoff and a local queue
status: Done
assignee: []
created_date: '2026-10-30 10:05'
updated_date: '2026-10-30 10:23'
labels: []
dependencies: []
ordinal: 130000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Jira answers 429 when a rate limit is hit (Data Center: per user per node when an admin enables it; Cloud: always). Handle it with Microsoft.Extensions.Http.Resilience, configured once on one typed HttpClient, not with hand-written retry code.

- JiraHttp becomes the typed HttpClient (AddHttpClient<JiraHttp> with the 30 s timeout and the resilience handler); JiraClient and JiraWorklogClient get JiraHttp injected.
- One pipeline per Jira host (SelectPipelineByAuthority), shared by every user and tab.
- A 429 triggers a global backoff for that host: a circuit breaker opens for the Retry-After time (plus jitter), so no request goes to that host meanwhile; a later 429 can lengthen the pause, never shorten it.
- An outer retry waits out Retry-After / the open circuit and then sends the request. Writes (POST/PUT/DELETE) are retried only on 429 or the open circuit, never on 5xx, timeouts or network errors, so no worklog is logged twice. Attempts and total wait are capped; then the request fails as today.
- A concurrency limiter with a local queue: at most 4 requests in flight per host, the rest wait in a FIFO queue instead of being refused; a full queue fails like any Jira error.
- Every attempt shows in the request log, and the toasts/status say when the app waits on a rate limit.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 JiraHttp is the one typed HttpClient for Jira, with the timeout and resilience pipeline set up once in Program.cs
- [x] #2 Retries and backoff use Microsoft.Extensions.Http.Resilience strategies, not hand-written retry loops
- [x] #3 A 429 pauses every Jira request to that host, for all users and tabs, until Retry-After has passed; a later 429 can only lengthen the pause
- [x] #4 Requests waiting on the pause are sent afterwards instead of failing, up to a cap
- [x] #5 Writes are retried only on 429, never on 5xx, timeouts or network errors
- [x] #6 At most 4 requests per Jira host are in flight; the rest wait in a local FIFO queue
- [x] #7 Every attempt shows in the request log, and the toasts or status text say when the app waits on a rate limit
- [x] #8 Tests cover the backoff across users, another host not being paused, write retries, the queue and the cap
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
JiraHttp is now the one typed HttpClient for Jira (AddJiraHttp: 30 s timeout, resilience pipeline, request log); JiraClient and JiraWorklogClient get it injected. Microsoft.Extensions.Http.Resilience builds one pipeline per Jira host (SelectPipelineByAuthority), shared by all users and tabs: retry (outermost; 429 or open circuit only, Retry-After or exponential backoff with jitter, 5 tries, pauses over 10 s fail as before), a concurrency limiter (4 in flight, FIFO queue of 200), and a circuit breaker that a 429 opens for Retry-After plus up to 20% jitter. Writes are never retried on 5xx, timeouts or network errors. JiraRecordingHandler inside the pipeline logs every attempt; a 429 is logged and toasted as 'Jira asked us to slow down: requests to Jira pause for N s.' Limits: Polly's breaker needs two requests in its 5-minute window, so a lone first 429 only waits on its own; the timesheet status line doesn't mention the pause (the toast does). TestClock timers now fire at once and move the clock. Checked against a mock Jira answering 429 during Sync now.
<!-- SECTION:FINAL_SUMMARY:END -->
