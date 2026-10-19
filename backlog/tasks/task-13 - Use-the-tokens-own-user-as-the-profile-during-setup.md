---
id: TASK-13
title: Use the token's own user as the profile during setup
status: Done
assignee: []
created_date: '2026-10-19 12:25'
updated_date: '2026-10-19 12:30'
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
- [x] #1 After connecting, the profile is set to the token's own user without a choice
- [x] #2 The profile picker step is gone from setup and Settings
- [x] #3 Changing the connection updates the profile to the new token's user
- [x] #4 Covered by tests
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
New JiraSetup service: ConnectAsync asks Jira who the token belongs to (/myself) and saves that user as the profile; a bad token throws and leaves the old profile. Connect is now the only setup step and goes straight to the timesheet. The profile picker page, the Change profile button, user search in JiraClient and the UserRow tag were removed; Settings explains the profile is the token's user. JiraSetupTests cover Server, Cloud, reconnecting with another token and a bad token; checked in the browser against the mock Jira.
<!-- SECTION:FINAL_SUMMARY:END -->
