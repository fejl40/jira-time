---
id: TASK-6
title: Add dark mode
status: Done
assignee: []
created_date: '2026-10-19 09:15'
updated_date: '2026-10-19 10:04'
labels: []
dependencies: []
modified_files:
  - src/JiraTime/wwwroot/theme.js
  - src/JiraTime/Components/Shared/ThemeToggle.razor
  - src/JiraTime/Components/App.razor
  - src/JiraTime/Styles/app.css
  - src/JiraTime/Components/Layout/MainLayout.razor
  - src/JiraTime/Components/Layout/ReconnectModal.razor.css
type: feature
ordinal: 6000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Add a dark theme to the whole app using Tailwind v4's dark variant. Follow the OS setting by default, with a toggle in the header (light / dark / system) remembered in the browser.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 All pages (setup, timesheet, day view, entry editor, settings) are readable in dark mode
- [x] #2 Defaults to the OS preference
- [x] #3 Header toggle switches light / dark / system and the choice survives a reload
- [x] #4 No flash of the wrong theme on page load
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Class-based Tailwind dark variant (.dark on <html>) with dark: styles on every page and shared component. wwwroot/theme.js loads in <head>, applies the theme before first paint, follows the OS in System mode, and re-applies it after Blazor's enhanced navigation resets <html> attributes. Header ThemeToggle cycles Light / Dark / System, stored in localStorage. Verified in a browser: OS dark is picked up on load, the toggle cycles and survives a reload, and setup, timesheet, entry editor and settings render in dark.
<!-- SECTION:FINAL_SUMMARY:END -->
