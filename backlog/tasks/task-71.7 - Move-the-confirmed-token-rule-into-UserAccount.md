---
id: TASK-71.7
title: Move the confirmed-token rule into UserAccount
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
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
- [ ] #1 SignInAsync is 30 lines or fewer
- [ ] #2 UserAccount.Confirm holds token ordering and trimming and is tested directly
- [ ] #3 Tests pass
<!-- AC:END -->
