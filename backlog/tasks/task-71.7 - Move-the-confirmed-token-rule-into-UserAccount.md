---
id: TASK-71.7
title: Move the confirmed-token rule into UserAccount
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:03'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 82000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
JiraIdentity.SignInAsync is about 45 lines and does account rules by hand (profile, token order, trimming to RememberedTokens). (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 SignInAsync is 30 lines or fewer
- [x] #2 UserAccount.Confirm holds token ordering and trimming and is tested directly
- [x] #3 Tests pass
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
UserAccount.Confirm records Jira's profile, the verification time and the token order and trimming (RememberedTokens moved to UserAccount), with direct tests; SignInAsync is 28 lines with the not-confirmed paths in NotConfirmedAsync.
<!-- SECTION:FINAL_SUMMARY:END -->
