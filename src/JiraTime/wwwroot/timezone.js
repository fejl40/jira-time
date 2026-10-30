// The browser's time zone, so the server can tell today and worklog start times on the user's clock.
window.jiraTimeZone = {
  // The IANA name, such as Europe/Copenhagen; empty when the browser doesn't say.
  name: () => {
    try {
      return Intl.DateTimeFormat().resolvedOptions().timeZone || '';
    } catch {
      return '';
    }
  }
};
