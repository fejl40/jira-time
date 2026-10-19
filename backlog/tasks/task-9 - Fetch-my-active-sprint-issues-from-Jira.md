---
id: TASK-9
title: Fetch my active-sprint issues from Jira
status: Done
assignee: []
created_date: '2026-10-19 11:44'
updated_date: '2026-10-19 12:02'
labels: []
dependencies: []
priority: high
type: feature
ordinal: 9000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Load the issues tied to the user: assigned to the selected profile, in an open sprint, not Done (JQL along the lines of assignee = currentUser() AND sprint in openSprints() AND statusCategory != Done). Include tasks and stories, with key, summary and status. Used as the source for Fill suggestions and as quick picks in the entry editor.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Returns key, summary, type and status for assigned open-sprint issues that are not Done
- [x] #2 Works on Server/DC and Cloud
- [x] #3 Results are cached briefly so Fill period does not call Jira per day
- [x] #4 The entry editor shows these issues as quick picks next to recent ones
- [x] #5 Covered by tests against a fake Jira
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
JiraClient.GetSprintIssuesAsync searches `assignee = "<username or account id>" AND statusCategory != Done AND sprint in openSprints()` via /rest/api/2/search (Server/DC) or /rest/api/3/search/jql (Cloud), and falls back to the query without the sprint clause on a 400 (Jira without Jira Software). SprintIssues caches results per connection and profile for five minutes. The entry editor shows them as "My sprint" quick-pick chips above "Recent". Tests in SprintIssuesTests cover both APIs, the JQL, mapping, fallback, error propagation and caching; checked in the browser against the mock Jira.
<!-- SECTION:FINAL_SUMMARY:END -->
