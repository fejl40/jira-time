---
id: TASK-71.5
title: Read epics with typed records instead of JsonElement
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:01'
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
- [x] #1 No TryGetProperty left in JiraClient.cs except the Epic Link extension-data lookup
- [x] #2 JiraClient.cs about 150 lines
- [x] #3 EpicOf and IssueEpic are private
- [x] #4 EpicLookupTests pass unchanged
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Epic search and the field list are read into typed records in Jira/JiraDtos.cs (the Epic Link value through [JsonExtensionData]); EpicOf and IssueEpic are private. JiraClient.cs is 168 lines (it still holds the sprint, period and key searches).
<!-- SECTION:FINAL_SUMMARY:END -->
