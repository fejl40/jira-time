---
id: TASK-16
title: Verify who each user is against their Jira host
status: Done
assignee: []
created_date: '2026-10-19 13:34'
updated_date: '2026-10-19 13:40'
labels: []
dependencies: []
priority: high
type: feature
ordinal: 16000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Several people can use the same Jira Time server, each from their own browser with their own token. On every visit the server asks the chosen Jira host who the token belongs to (/myself) and uses that Jira user id and host as the user's identity; nobody can claim to be someone else. If Jira can't be reached, a token that was verified before for that host is still accepted so offline work keeps going. A rejected token sends the user back to connect.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Identity is the Jira host plus the user id Jira returns for the token, checked on each visit
- [x] #2 A token Jira rejects is not accepted, and the user is sent to connect again
- [x] #3 When Jira can't be reached, a token verified before for that host still works; an unknown token does not
- [x] #4 A new connection is only accepted after Jira confirms it
- [x] #5 The profile shown is the verified user; the global profile setting is gone
- [x] #6 Covered by tests
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
New JiraIdentity signs each browser tab in: it calls /myself on the stored connection's host and sets CurrentUser (scoped per circuit) to UserKey(normalized host, Jira user id). The layout does this on every visit; a 401/403 clears the stored connection and sends the user to Connect, other failures show a "Can't check who you are" panel with Try again. Confirmed tokens are remembered per user as hashes (last 5) in the users collection, so when Jira is unreachable (network error, timeout, 5xx) a token confirmed before for that host still signs in, shown as Offline in the header; unknown tokens are refused, and Connect always requires Jira to confirm. The global SettingsService/profile and JiraSetup were removed; the profile shown comes from Jira. Tests in MultiUserTests (UserKeyTests, JiraIdentityTests); checked in the browser with two tokens and with the mock Jira stopped.
<!-- SECTION:FINAL_SUMMARY:END -->
