---
id: TASK-100
title: Write the export's hours with a decimal point
status: Done
assignee: []
created_date: '2026-10-30 13:58'
updated_date: '2026-10-30 14:03'
labels: []
dependencies: []
priority: medium
ordinal: 165000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Carl decided the decimal comma in numbers was a mistake and should be reversed. The only place the app writes one is the month export: the Timer column is written as 7,5 and 0,25 (MonthExport, used by Copy, Download .tsv and the table on the Export page). Write hours with a decimal point instead (7.5, 0.25), the same as everywhere else in the app. Typing hours as 1,5 on the timesheet and in Settings stays accepted, since that only reads what the user types.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 The export's Timer column is written with a decimal point (7.5, 0.25, 8) in Copy, Download .tsv and the table on the Export page
- [x] #2 No other number the app writes uses a decimal comma
- [x] #3 Typing 1,5 in an hours field still reads as 1.5 hours
- [x] #4 MonthExportTests cover the decimal point
- [x] #5 AGENTS.md (Month export) says hours are written with a decimal point
- [x] #6 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Besides the export, numbers written through the current culture (an interpolated or ToString'd number in markup) would show a decimal comma on a Danish Windows machine. Most formatting already passes InvariantCulture; to make sure no number gets a comma, the app runs with the invariant culture (CultureInfo.DefaultThreadCurrentCulture/UICulture and the request culture set in Program.cs).
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
MonthExport.Hours writes a decimal point (7.5, 0.25), so Copy, Download .tsv and the Export table do too. The app also runs with the invariant culture (DefaultThreadCurrentCulture/UICulture in Program.cs), so no number gets a comma on a Danish machine. Typing 1,5 still reads as 1.5 h. Tests: decimal point, and the same under da-DK. Browser check with the server and browser in Danish: the status line says 87.5 h and the export's Timer shows 7.5. AGENTS.md updated.
<!-- SECTION:FINAL_SUMMARY:END -->
