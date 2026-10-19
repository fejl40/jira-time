---
id: TASK-9
title: Fetch my active-sprint issues from Jira
status: To Do
assignee: []
created_date: '2026-10-19 11:44'
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
- [ ] #1 Returns key, summary, type and status for assigned open-sprint issues that are not Done
- [ ] #2 Works on Server/DC and Cloud
- [ ] #3 Results are cached briefly so Fill period does not call Jira per day
- [ ] #4 The entry editor shows these issues as quick picks next to recent ones
- [ ] #5 Covered by tests against a fake Jira
<!-- AC:END -->
