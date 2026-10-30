// Browser helpers for the Jira request log page.
window.jiraLogs = {
  // Scrolls a request into view, e.g. one opened from a toast.
  reveal: selector => document.querySelector(selector)?.scrollIntoView({ block: 'center' }),
};
