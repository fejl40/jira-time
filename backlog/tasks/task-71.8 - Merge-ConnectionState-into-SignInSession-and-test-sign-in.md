---
id: TASK-71.8
title: Merge ConnectionState into SignInSession and test sign-in
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 83000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
SignInSession is the only user of ConnectionState, whose Connection property and Changed event are unused. Sign-in has no tests. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 ConnectionState.cs is gone; SignInSession.cs is 150 lines or fewer
- [ ] #2 Tests cover: rejected token cleared, unconfirmed connect not remembered, confirmed connect remembered, sign-out clears
- [ ] #3 AGENTS.md layout updated; build and tests pass
<!-- AC:END -->
