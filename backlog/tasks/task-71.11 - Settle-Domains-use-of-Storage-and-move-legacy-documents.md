---
id: TASK-71.11
title: Settle Domain's use of Storage and move legacy documents
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
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
- [ ] #1 PRINCIPLES.md table allows only the two Storage markers in Domain, matching the code
- [ ] #2 Domain/LegacyData.cs is gone; its types live with LegacyDataMigration
- [ ] #3 AGENTS.md layout updated; tests pass
<!-- AC:END -->
