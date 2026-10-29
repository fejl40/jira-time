---
id: TASK-71.9
title: Flatten Storage and drop the options class and DI extension
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 84000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Storage/FileSystem/ holds only two files, FileDocumentStoreOptions wraps one string and StorageServiceCollectionExtensions.cs is a 14-line file. The store summary says 'single local user'. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Storage/ has 3 files and no subfolder; options type and extension class are gone
- [ ] #2 FileDocumentStore takes a root path; tests updated
- [ ] #3 Data still lands in data/documents/<collection>/; tests pass
<!-- AC:END -->
