// Browser helpers for the Jira request log page.
window.jiraLogs = {
  // Saves a day's log, streamed from the server, as a .jsonl file.
  download: async (name, stream) => {
    const blob = new Blob([await stream.arrayBuffer()], { type: 'application/x-ndjson' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = name;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  },

  // Scrolls a request into view, e.g. one opened from a toast.
  reveal: selector => document.querySelector(selector)?.scrollIntoView({ block: 'center' }),
};
