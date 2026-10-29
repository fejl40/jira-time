---
id: TASK-71.5
title: Read epics with typed records instead of JsonElement
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 80000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
EpicOf, IsEpic, ReadEpicsAsync and GetEpicLinkFieldAsync hand-walk JsonElement while every other Jira answer uses records in Jira/JiraDtos.cs. EpicOf and IssueEpic are needlessly public. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 No TryGetProperty left in JiraClient.cs except the Epic Link extension-data lookup
- [ ] #2 JiraClient.cs about 150 lines
- [ ] #3 EpicOf and IssueEpic are private
- [ ] #4 EpicLookupTests pass unchanged
<!-- AC:END -->
