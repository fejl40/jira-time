// Clipboard and file downloads, for the month export and the Jira request log.
window.jiraFiles = {
  // Puts the text on the clipboard; true when it worked.
  copy: async text => {
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch {
      // Older browsers or no clipboard permission: copy from a hidden text area.
      const area = document.createElement('textarea');
      area.value = text;
      area.style.position = 'fixed';
      area.style.opacity = '0';
      document.body.appendChild(area);
      area.select();
      const ok = document.execCommand('copy');
      area.remove();
      return ok;
    }
  },

  // Saves text, or a stream from the server (DotNetStreamReference), as a file of the given type.
  download: async (name, type, content) => {
    const data = typeof content === 'string' ? content : await content.arrayBuffer();
    const url = URL.createObjectURL(new Blob([data], { type }));
    const a = document.createElement('a');
    a.href = url;
    a.download = name;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  },
};
