---
id: TASK-71.9
title: Flatten Storage and drop the options class and DI extension
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 07:54'
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
- [x] #1 Storage/ has 3 files and no subfolder; options type and extension class are gone
- [x] #2 FileDocumentStore takes a root path; tests updated
- [x] #3 Data still lands in data/documents/<collection>/; tests pass
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Moved FileDocumentStore and FileDocumentCollection into Storage/, gave the store a rootPath constructor, removed FileDocumentStoreOptions and the DI extension, and registered the store directly in Program.cs.
<!-- SECTION:FINAL_SUMMARY:END -->
