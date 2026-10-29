---
id: TASK-66.2
title: Remember export values per epic
status: Done
assignee: []
created_date: '2026-10-29 07:36'
updated_date: '2026-10-29 07:39'
labels:
  - feature
dependencies: []
parent_task_id: TASK-66
priority: high
ordinal: 68000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Fase, Forordning, Hovedleverance nr. and Hovedleverance link are set once per epic and kept per user.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 The four values can be saved and read back per epic
- [x] #2 Each user has their own values
- [x] #3 Tests cover saving and per-user separation
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
EpicExportValues document (epic-export-values, id = owner + epic) and EpicExportStore to read and save them; clearing all values removes the document. EpicExportStoreTests.
<!-- SECTION:FINAL_SUMMARY:END -->
