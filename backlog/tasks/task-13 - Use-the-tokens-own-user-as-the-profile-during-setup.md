---
id: TASK-13
title: Use the token's own user as the profile during setup
status: To Do
assignee: []
created_date: '2026-10-19 12:25'
labels: []
dependencies:
  - TASK-12
priority: medium
type: enhancement
ordinal: 13000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A PAT (or Cloud API token) always belongs to one Jira user. During setup, take that user (GET /myself) as the profile instead of letting the user search for and pick someone. Remove the profile picker step and the Change profile option.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 After connecting, the profile is set to the token's own user without a choice
- [ ] #2 The profile picker step is gone from setup and Settings
- [ ] #3 Changing the connection updates the profile to the new token's user
- [ ] #4 Covered by tests
<!-- AC:END -->
