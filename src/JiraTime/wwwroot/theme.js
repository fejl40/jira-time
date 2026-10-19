// Light / dark / system theme. Loaded in <head> so the right theme is applied before first paint.
(() => {
  const key = 'jira-time.theme';
  const media = window.matchMedia('(prefers-color-scheme: dark)');
  const read = () => { try { return localStorage.getItem(key) || 'system'; } catch { return 'system'; } };
  const root = document.documentElement;
  const apply = () => {
    const mode = read();
    const dark = mode === 'dark' || (mode === 'system' && media.matches);
    if (root.classList.contains('dark') !== dark) root.classList.toggle('dark', dark);
    if (root.dataset.theme !== mode) root.dataset.theme = mode;
  };
  media.addEventListener('change', apply);
  // Blazor's enhanced navigation syncs <html> attributes with the server's copy, which has no theme.
  // Put the theme back whenever that happens.
  new MutationObserver(apply).observe(root, { attributes: true, attributeFilter: ['class', 'data-theme'] });
  window.jiraTheme = {
    get: read,
    set: mode => {
      try { mode === 'system' ? localStorage.removeItem(key) : localStorage.setItem(key, mode); } catch { }
      apply();
      return mode;
    },
  };
  apply();
})();
