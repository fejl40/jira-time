---
id: TASK-94
title: Keep the issue cache per user
status: To Do
assignee: []
created_date: '2026-10-30 13:06'
labels:
  - bug
dependencies: []
priority: medium
ordinal: 159000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Carl decided the issue cache must not be shared between users. JiraIssueLookup is a singleton: the sprint and period lists are already keyed by host and user, but issue statuses, epics and the Epic Link field are keyed by host and issue only, so one user's lookups answer another user's on the same Jira. Key every cached value by the signed-in user (UserKey: host and Jira user id), so each user only ever gets what Jira told them.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Every JiraIssueLookup cache key includes the user (host and Jira user id)
- [ ] #2 A test shows two users on the same Jira each get their own statuses and epics from Jira, not the other's cached values
- [ ] #3 One user's repeated lookups are still cached as before (five minutes, the Epic Link field twelve hours)
- [ ] #4 AGENTS.md says the issue caches are per user
- [ ] #5 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
