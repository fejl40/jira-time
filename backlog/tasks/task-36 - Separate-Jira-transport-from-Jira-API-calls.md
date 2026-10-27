---
id: TASK-36
title: Separate Jira transport from Jira API calls
status: Done
assignee: []
created_date: '2026-10-27 09:03'
updated_date: '2026-10-27 09:06'
labels: []
dependencies: []
ordinal: 36000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
JiraClient mixes HTTP, auth and error handling with the API calls, builds JQL by hand in several places and holds the Atlassian Document Format conversion.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 HTTP, auth and error mapping live in one transport class
- [x] #2 JQL is built through one helper that quotes values safely
- [x] #3 Atlassian Document Format conversion has its own class
- [x] #4 Existing Jira tests pass unchanged
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
JiraHttp holds HTTP, auth and error mapping; Jql quotes values (now also escaping backslashes, so DOMAIN\user works); AtlassianDocument converts comments; raw DTOs live in JiraDtos.cs. JiraClient keeps its public API and is about 210 lines. 13 new tests.
<!-- SECTION:FINAL_SUMMARY:END -->
