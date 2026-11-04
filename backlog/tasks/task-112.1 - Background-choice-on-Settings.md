---
id: TASK-112.1
title: Background choice on Settings
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:19'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-112
priority: medium
ordinal: 181000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Settings gets Background: Aurora, Sky or Plain, stored in the user's settings document; the layout shows the chosen one.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 The choice is saved per user and survives reload
- [x] #2 Plain shows today's flat background
- [x] #3 Aurora is the default
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Settings → Background (Components/Shared/BackgroundPicker.razor): Aurora, Sky or Plain with a preview each, saved at once in UserSettings.Background through Services/BackgroundSettings.cs (default Aurora); BackdropLayer reloads on its Changed event, so the page switches without a reload, and the choice survives reloads (BackgroundSettingsTests).
<!-- SECTION:FINAL_SUMMARY:END -->
