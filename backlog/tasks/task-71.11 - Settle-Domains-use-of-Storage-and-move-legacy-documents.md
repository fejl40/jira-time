---
id: TASK-71.11
title: Settle Domain's use of Storage and move legacy documents
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:06'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 86000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Five Domain files use Storage's IDocument and DocumentCollection markers, which PRINCIPLES.md does not allow; LegacyData.cs holds only types LegacyDataMigration uses. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 PRINCIPLES.md table allows only the two Storage markers in Domain, matching the code
- [x] #2 Domain/LegacyData.cs is gone; its types live with LegacyDataMigration
- [x] #3 AGENTS.md layout updated; tests pass
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
PRINCIPLES.md now lets Domain use only IDocument and [DocumentCollection] from Storage; AppSettings, LegacyBackup and TimeEntriesBackup moved into Services/LegacyDataMigration.cs (CreatedUtc set from the injected clock) and Domain/LegacyData.cs is gone.
<!-- SECTION:FINAL_SUMMARY:END -->
