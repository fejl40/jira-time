---
id: TASK-104
title: Don't block or fail on files replaced while the store reads them on Windows
status: Done
assignee: []
created_date: '2026-10-30 15:06'
updated_date: '2026-10-30 15:08'
labels: []
dependencies: []
priority: high
ordinal: 169000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
On Carl's Windows PC Writes_and_reads_at_the_same_time_while_files_change_on_disk_all_succeed fails with UnauthorizedAccessException in the test's hand edit (File.Move over a document). On Windows a file opened by File.ReadAllTextAsync (FileShare.Read) can't be replaced or deleted until it is closed, so the store's reads block anyone editing a document by hand, and the store's own temp-file move can fail the same way when another program (an editor, antivirus, backup) has the file open for a moment.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 The file store reads documents with FileShare.ReadWrite | FileShare.Delete, so it never stops another program from replacing or deleting a file
- [x] #2 The store's move into place is retried a few times, briefly, when Windows says the file is in use (IOException or UnauthorizedAccessException), then fails as before
- [x] #3 The test's simulated hand edit retries the same way a person or editor would when the file is busy for a moment
- [x] #4 Build has 0 warnings and all tests pass
<!-- AC:END -->
