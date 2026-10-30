---
id: TASK-90.3
title: Split the Export page and share one browser file script
status: To Do
assignee: []
created_date: '2026-10-30 11:32'
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
- [ ] #1 No file in Components/Export/ is over 150 lines
- [ ] #2 Epic column labels exist only in MonthExport and the markup has no column-index checks
- [ ] #3 MonthExportTests check EpicColumns match Header[2..5] and the setters round-trip
- [ ] #4 One copy and one download implementation in wwwroot; no Logs file references jiraExport
- [ ] #5 grep for waiting-pending finds nothing in src
- [ ] #6 The .tsv keeps its byte order mark and the .jsonl download is unchanged
- [ ] #7 Only the files listed under Files are created, edited, moved or deleted
- [ ] #8 Behaviour and what the user sees stay the same
- [ ] #9 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
