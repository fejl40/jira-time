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
    // Let the first screen draw without animations, then allow them for what changes from then on.
    setTimeout(() => {
      document.querySelectorAll('[class*="motion-"]').forEach(el => el.dataset.settled = '');
      document.documentElement.classList.add('motion-ready');
    }, 800);
  };
  if (document.body) start(); else addEventListener('DOMContentLoaded', start);

  // ---- Ticks: a small check mark flies from each saved cell to the place that says Jira has it ----
  const flyTicks = (fromSelector, toSelector) => {
    const target = document.querySelector(toSelector);
    if (!target || reduced()) return;
    const to = target.getBoundingClientRect();
    const sources = [...document.querySelectorAll(fromSelector)].slice(0, 24);
    sources.forEach((el, i) => {
      const from = el.getBoundingClientRect();
      const tick = document.createElement('span');
      tick.className = 'motion-tick';
      tick.textContent = '✓';
      tick.style.left = `${from.left + from.width / 2}px`;
      tick.style.top = `${from.top + from.height / 2}px`;
      document.body.append(tick);
      const dx = to.left + 12 - (from.left + from.width / 2), dy = to.top + to.height / 2 - (from.top + from.height / 2);
      tick.animate(
        [{ transform: 'translate(-50%, -50%) scale(0.6)', opacity: 0 },
         { transform: 'translate(-50%, -50%) scale(1.2)', opacity: 1, offset: 0.25 },
         { transform: `translate(calc(-50% + ${dx}px), calc(-50% + ${dy}px)) scale(0.8)`, opacity: 0.2 }],
        { duration: SLOW, delay: i * 15, easing: 'cubic-bezier(0.2, 0.8, 0.2, 1)', fill: 'both' }
      ).finished.then(() => tick.remove());
    });
    if (sources.length) {
      setTimeout(() => {
        target.classList.remove('motion-received');
        void target.offsetWidth; // restart the animation
        target.classList.add('motion-received');
      }, SLOW + sources.length * 15);
    }
  };

  // ---- Collapse: rows shrink away before the page removes them; resolves when done ----
  const collapse = selector => {
    const rows = [...document.querySelectorAll(selector)];
    if (!rows.length || reduced()) return Promise.resolve();
    return Promise.all(rows.map(el => {
      el.style.overflow = 'hidden';
      return el.animate(
        [{ height: `${el.offsetHeight}px`, opacity: 1 }, { height: '0px', paddingTop: '0px', paddingBottom: '0px', opacity: 0 }],
        { duration: SLOW, easing: 'cubic-bezier(0.2, 0.8, 0.2, 1)', fill: 'forwards' }
      ).finished;
    })).then(() => undefined);
  };

  // ---- Glide: the selected day's highlight moves to the selected column of a grid ----
  const glide = grid => {
    const highlight = grid?.querySelector('[data-day-glide]');
    const column = grid?.querySelector('[data-selected-day]');
    if (!highlight) return;
    if (!column) { highlight.style.opacity = '0'; return; }
    highlight.style.opacity = '';
    highlight.style.width = `${column.offsetWidth}px`;
    highlight.style.transform = `translateX(${column.offsetLeft}px)`;
    // The first placement jumps; later ones glide.
    if (!('placed' in highlight.dataset)) requestAnimationFrame(() => highlight.dataset.placed = '');
  };
  addEventListener('resize', () => document.querySelectorAll('[data-day-glide]').forEach(h => glide(h.parentElement)));

  // ---- Confetti: once per period and browser, from the element that says the period is complete ----
  const celebrated = key => {
    try {
      const k = `jira-time.celebrated.${key}`;
      if (localStorage.getItem(k)) return true;
      localStorage.setItem(k, new Date().toISOString());
    } catch { /* no storage: celebrate anyway */ }
    return false;
  };
  const COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#8b5cf6', '#f43f5e', '#06b6d4'];
  const celebrate = (key, fromSelector) => {
    if (celebrated(key) || reduced()) return;
    const from = document.querySelector(fromSelector)?.getBoundingClientRect();
    const x = from ? from.left + from.width / 2 : innerWidth / 2, y = from ? from.top + from.height / 2 : 120;
    for (let i = 0; i < 48; i++) {
      const bit = document.createElement('span');
      bit.className = 'motion-confetti';
      bit.style.left = `${x}px`;
      bit.style.top = `${y}px`;
      bit.style.background = COLORS[i % COLORS.length];
      document.body.append(bit);
      const angle = Math.random() * Math.PI * 2, distance = 60 + Math.random() * 120;
      const dx = Math.cos(angle) * distance, dy = Math.sin(angle) * distance - 40;
      bit.animate(
        [{ transform: 'translate(-50%, -50%) rotate(0deg)', opacity: 1 },
         { transform: `translate(calc(-50% + ${dx}px), calc(-50% + ${dy}px)) rotate(${Math.random() * 360}deg)`, opacity: 1, offset: 0.6 },
         { transform: `translate(calc(-50% + ${dx * 1.1}px), calc(-50% + ${dy + 80}px)) rotate(${Math.random() * 720}deg)`, opacity: 0 }],
        { duration: SLOW * 4, easing: 'cubic-bezier(0.2, 0.8, 0.4, 1)', fill: 'forwards' }
      ).finished.then(() => bit.remove());
    }
  };

  window.jiraMotion = { flyTicks, collapse, glide, celebrate };
})();
