// Keyboard shortcuts for the timesheet, off while a modal is open. The page attaches itself and gets called back:
//   Ctrl/Cmd+S  Save all (also while typing; the field is committed first)
//   ← / →       previous / next day (outside fields)
//   N           add a row (outside fields)
//   Enter       in an hours or comment field: next row's hours
//   Escape      in a field: leave it, so the keys above work again
(() => {
  let page = null;

  // The hours fields of the day editor's rows, in order.
  const HOURS = 'li[data-row] input[data-hours]';

  const isField = el => el instanceof HTMLElement && el.matches('input, textarea, select, [contenteditable]');

  /** Focuses a field and selects its text, so typing replaces it. */
  const focusAndSelect = el => { if (el) { el.focus(); el.select?.(); } };

  const onKeyDown = e => {
    if (!page) return;
    const target = e.target;
    const typing = isField(target);
    const save = (e.ctrlKey || e.metaKey) && !e.altKey && e.key.toLowerCase() === 's';

    // A modal is open (Components/Modals): its keys are its own, and Ctrl+S doesn't open the browser's Save.
    if (document.querySelector('dialog[open]')) {
      if (save) e.preventDefault();
      return;
    }

    if (save) {
      e.preventDefault();
      // Blur first so the field's change reaches the server before the save does.
      if (typing) target.blur();
      page.invokeMethodAsync('SaveFromKeyboard');
      return;
    }

    if (typing) {
      if (e.key === 'Escape' && !target.closest('[data-issue-picker]')) target.blur();
      if (e.key === 'Enter' && target.matches('[data-hours], [data-comment]')) {
        e.preventDefault();
        const rows = [...document.querySelectorAll(HOURS)];
        const row = target.closest('li[data-row]');
        const index = rows.findIndex(h => h.closest('li[data-row]') === row);
        const next = rows[index + 1];
        if (next) focusAndSelect(next);
        else target.blur();
      }
      return;
    }

    if (e.altKey || e.ctrlKey || e.metaKey || e.shiftKey) return;
    if (e.key === 'ArrowLeft' || e.key === 'ArrowRight') {
      e.preventDefault();
      page.invokeMethodAsync('MoveDay', e.key === 'ArrowLeft' ? -1 : 1);
    } else if (e.key === 'n' || e.key === 'N') {
      e.preventDefault();
      page.invokeMethodAsync('AddRowFromKeyboard');
    }
  };

  window.jiraKeys = {
    attach: ref => { page = ref; },
    detach: () => { page = null; },
    /** Focuses an element by id, or else the first hours field of the day. */
    focus: id => focusAndSelect((id && document.getElementById(id)) || document.querySelector(HOURS)),
  };
  document.addEventListener('keydown', onKeyDown);
})();
