// Animations that need script. Styles live in Styles/motion.css.
//   Numbers: an element with data-count="7.5" shows the number (as decimal hours, like the server) and
//   counts to a new value when the attribute changes. data-zero is shown for 0, data-unit after the number.
(() => {
  const reduced = () => matchMedia('(prefers-reduced-motion: reduce)').matches;
  const SLOW = 150;

  // ---- Numbers ----
  const format = (v, zero) => {
    const r = Math.round(v * 100) / 100;
    return r === 0 ? zero : String(r);
  };
  const show = (el, v) => {
    el._current = v;
    const text = format(v, el.dataset.zero ?? '');
    el.textContent = text === '' ? '' : text + (el.dataset.unit ?? '');
  };
  const count = el => {
    const to = parseFloat(el.dataset.count) || 0;
    const from = el._current; // what is on screen, also halfway through an earlier count
    cancelAnimationFrame(el._frame);
    if (from === undefined || from === to || reduced() || !document.documentElement.classList.contains('motion-ready')) {
      show(el, to);
      return;
    }
    const start = performance.now();
    const step = now => {
      const t = Math.min(1, (now - start) / SLOW);
      const eased = 1 - Math.pow(1 - t, 3);
      show(el, from + (to - from) * Math.min(1, Math.max(0, eased)));
      if (t < 1) el._frame = requestAnimationFrame(step);
      else show(el, to);
    };
    el._frame = requestAnimationFrame(step);
  };

  // Watches the page for numbers that appear or change.
  const observer = new MutationObserver(records => {
    for (const r of records) {
      if (r.type === 'attributes') { count(r.target); continue; }
      for (const node of r.addedNodes) {
        if (!(node instanceof Element)) continue;
        if (node.matches('[data-count]')) count(node);
        node.querySelectorAll('[data-count]').forEach(count);
      }
    }
  });
  const start = () => {
    observer.observe(document.body, { subtree: true, childList: true, attributes: true, attributeFilter: ['data-count'] });
    document.querySelectorAll('[data-count]').forEach(count);
    // Let the first screen draw without animations, then allow them.
    setTimeout(() => document.documentElement.classList.add('motion-ready'), 800);
  };
  if (document.body) start(); else addEventListener('DOMContentLoaded', start);

  window.jiraMotion = {};
})();
