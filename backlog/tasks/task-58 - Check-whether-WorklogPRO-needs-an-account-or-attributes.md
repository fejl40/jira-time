---
id: TASK-58
title: Check whether WorklogPRO needs an account or attributes
status: Done
assignee: []
created_date: '2026-10-27 14:02'
updated_date: '2026-10-30 08:00'
labels:
  - jira-source-of-truth
dependencies: []
priority: medium
ordinal: 58000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
WorklogPRO reads standard Jira worklogs, and stores its extras (account, attributes) in the worklogpro worklog property. Check on the real Jira whether worklogs saved here show in WorklogPRO and whether an account or attribute is required. If it is, let the user pick it and send it with each worklog.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Confirmed on the real Jira whether app worklogs show in WorklogPRO
- [x] #2 If WorklogPRO requires an account or attribute, the app lets the user set it and sends it in the worklogpro property, with tests
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Carl confirmed on the real Jira (2026-10-30) that worklogs saved by the app show in WorklogPRO and nothing extra is required, so no account or attribute support is needed.
<!-- SECTION:FINAL_SUMMARY:END -->
