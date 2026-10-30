---
id: TASK-90.3
title: Split the Export page and share one browser file script
status: Done
assignee: []
created_date: '2026-10-30 11:32'
updated_date: '2026-10-30 11:37'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-90
priority: medium
ordinal: 142000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
ExportPage.razor mixes the header, the per-epic values form with per-field saving, the preview table and copy/download; its four input cells are copy-paste, its labels duplicate MonthExport.Header and cell styling depends on column indexes. Move it to Components/Export/ with EpicValuesForm.razor and ExportTable.razor (route stays /export); add MonthExport.EpicColumns (label, getter, setter) used by Cells and the form, and named column alignment instead of i == n checks. export.js and logs.js duplicate the download code and Logs' JsonTree calls jiraExport.copy: add wwwroot/files.js (jiraFiles.copy, jiraFiles.download for text or streams), delete export.js, logs.js keeps reveal. Remove the unused .waiting-pending style from motion.css.

Files:
- src/JiraTime/Components/Pages/ExportPage.razor -> src/JiraTime/Components/Export/ExportPage.razor
- src/JiraTime/Components/Export/EpicValuesForm.razor (new)
- src/JiraTime/Components/Export/ExportTable.razor (new)
- src/JiraTime/Components/_Imports.razor
- src/JiraTime/Domain/MonthExport.cs
- src/JiraTime/wwwroot/files.js (new)
- src/JiraTime/wwwroot/export.js (delete)
- src/JiraTime/wwwroot/logs.js
- src/JiraTime/Components/App.razor
- src/JiraTime/Components/Pages/LogsPage.razor
- src/JiraTime/Components/Logs/JsonTree.razor
- src/JiraTime/Styles/motion.css
- tests/JiraTime.Tests/MonthExportTests.cs

Rules for this subtask: edit only the files listed. Do not edit AGENTS.md, README.md, backlog docs or wwwroot/app.css; write any doc changes needed into the final summary so the Post-refactor cleanup applies them.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 No file in Components/Export/ is over 150 lines
- [x] #2 Epic column labels exist only in MonthExport and the markup has no column-index checks
- [x] #3 MonthExportTests check EpicColumns match Header[2..5] and the setters round-trip
- [x] #4 One copy and one download implementation in wwwroot; no Logs file references jiraExport
- [x] #5 grep for waiting-pending finds nothing in src
- [x] #6 The .tsv keeps its byte order mark and the .jsonl download is unchanged
- [x] #7 Only the files listed under Files are created, edited, moved or deleted
- [x] #8 Behaviour and what the user sees stay the same
- [x] #9 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
_Imports.razor was not changed: the Export components are only used by ExportPage in the same namespace, so no @using is needed. The committed wwwroot/app.css still contains .waiting-pending until it is rebuilt with Node and committed (left to the cleanup, as app.css is not committed by subtasks).
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Moved the Export page to Components/Export/ (route /export unchanged) and split it into ExportPage (header, loading, copy/download, 103 lines), EpicValuesForm (values per epic with per-field saving, 93) and ExportTable (preview, 43). MonthExport now has EpicColumns (label, short field name, getter, setter) and Columns (label + ExportColumnKind: Date, Hours, EpicLink, Text); Header, Cells, EpicExportValues.IsEmpty, the form and the table use them, so the labels live only in MonthExport and cell alignment is by column kind. New wwwroot/files.js (jiraFiles.copy, jiraFiles.download(name, type, text or stream)) replaces export.js and the download in logs.js; logs.js keeps reveal; the .tsv byte order mark is added by ExportPage. Removed the unused .waiting-pending style and its keyframes from motion.css. Tests check EpicColumns match Header[2..5], Columns match Header, setters round-trip, and IsEmpty. Checked in Chromium against a mock Jira: values form, table, Copy, Download .tsv (BOM, same text as copied), .jsonl download and the JSON tree Copy work as before. Doc changes for cleanup: AGENTS.md layout - 'Components/Pages/ExportPage.razor' becomes 'Components/Export/ ExportPage (a month in the agency format, Copy, Download .tsv), EpicValuesForm (values per epic), ExportTable'; mention wwwroot/files.js (copy and downloads, used by Export and Logs) instead of wwwroot/export.js; LogsPage line '(wwwroot/logs.js)' still right (reveal only). Waiting for Jira section: drop '/ .waiting-pending' from '(aria-busy, .waiting-dim / .waiting-pending)'. Commit a rebuilt wwwroot/app.css so .waiting-pending leaves the compiled CSS.
<!-- SECTION:FINAL_SUMMARY:END -->
