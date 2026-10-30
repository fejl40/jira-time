// Native <dialog> modals (Components/Modals). The server owns which modals are open; this script opens
// and closes the dialogs ModalHost draws and tells it what the user asks for:
//   Esc, a click on the backdrop   Dismiss: cancels unless the modal must be answered or is busy
//   [data-modal-close]             CloseWith no result
//   [data-modal-result="x"]        CloseWith "x"
//   [data-modal-autofocus]         gets the focus when the modal opens
// A dialog without data-modal-dismiss, or busy (data-modal-busy on it or inside it), ignores all of these.
(() => {
  const find = id => document.getElementById(`modal-${id}`);
  const busy = dialog => dialog.matches('[data-modal-busy], :has([data-modal-busy])');
  const canDismiss = dialog => dialog.hasAttribute('data-modal-dismiss') && !busy(dialog);

  /** True when the pointer is outside the dialog's box, on the backdrop. */
  const outside = (dialog, e) => {
    const r = dialog.getBoundingClientRect();
    return e.clientX < r.left || e.clientX > r.right || e.clientY < r.top || e.clientY > r.bottom;
  };

  const focusFirst = dialog => dialog.querySelector('[data-modal-autofocus]')?.focus();

  const wire = (dialog, host, id) => {
    dialog.addEventListener('cancel', e => {
      e.preventDefault();
      if (canDismiss(dialog)) host.invokeMethodAsync('Dismiss', id);
    });

    // A press that starts and ends on the backdrop closes; dragging out of a field doesn't.
    let pressedOutside = false;
    dialog.addEventListener('pointerdown', e => { pressedOutside = e.target === dialog && outside(dialog, e); });
    dialog.addEventListener('click', e => {
      const fromBackdrop = pressedOutside && e.target === dialog && outside(dialog, e);
      pressedOutside = false;
      const button = e.target.closest('[data-modal-close], [data-modal-result]');
      if (button && !busy(dialog)) host.invokeMethodAsync('CloseWith', id, button.getAttribute('data-modal-result'));
      else if (fromBackdrop && canDismiss(dialog)) host.invokeMethodAsync('Dismiss', id);
    });

    // The browser may close a dialog by itself (Esc pressed twice quickly). The server decides: show it
    // again and ask it to dismiss.
    dialog.addEventListener('close', () => {
      if (dialog.hasAttribute('data-leaving')) return;
      dialog.showModal();
      focusFirst(dialog);
      if (canDismiss(dialog)) host.invokeMethodAsync('Dismiss', id);
    });
  };

  // Where the focus was before the first of a row of modals opened; it goes back there once all are
  // closed, also when the browser can't put it back itself (the element it remembered is gone).
  let opener = null;
  const anyOpen = () => document.querySelector('dialog[open]:not([data-leaving])');

  /** The length of the dialog's current animation in ms (0 with reduced motion). */
  const animationMs = dialog => parseFloat(getComputedStyle(dialog).animationDuration || '0') * 1000;

  window.jiraModals = {
    open: (id, host) => {
      const dialog = find(id);
      if (!dialog || dialog.open) return;
      if (!anyOpen()) opener = document.activeElement;
      wire(dialog, host, id);
      dialog.showModal();
      focusFirst(dialog);
    },

    // Plays the closing animation, then closes the dialog; the focus goes back to where it was.
    close: id => new Promise(resolve => {
      const dialog = find(id);
      if (!dialog?.open) return resolve();
      dialog.setAttribute('data-leaving', '');
      setTimeout(() => {
        dialog.close();
        if (!anyOpen() && opener?.isConnected && !document.activeElement?.closest('dialog[open]')) opener.focus();
        resolve();
      }, animationMs(dialog));
    }),
  };
})();
