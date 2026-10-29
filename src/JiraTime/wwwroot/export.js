// Clipboard and file download for the month export page.
window.jiraExport = {
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

  // Saves the text as a UTF-8 file with a byte order mark, so Excel reads æ, ø and å right.
  download: (name, text) => {
    const blob = new Blob(['﻿' + text], { type: 'text/tab-separated-values;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = name;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  },
};
