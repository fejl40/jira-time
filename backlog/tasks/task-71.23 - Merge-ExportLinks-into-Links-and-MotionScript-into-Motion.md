---
id: TASK-71.23
title: Merge ExportLinks into Links and MotionScript into Motion
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 07:57'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 98000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Each concept is split over two tiny files. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 ExportLinks.cs and MotionScript.cs are gone
- [x] #2 Export link tests live in LinksTests.cs
- [x] #3 Build and tests pass
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
ExportLinks now lives in Links.cs and MotionScript in Motion.cs (type names kept); the export link tests moved into LinksTests.cs.
<!-- SECTION:FINAL_SUMMARY:END -->
