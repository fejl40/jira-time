// Animations that need script; the styles and speeds live in Styles/motion.css. The page calls these
// through Components/MotionScript.cs:
//   collapse(selector)            rows shrink away; resolves when done, so the page can then remove them
//   glide(grid)                   moves the grid's selected-day highlight to the selected column
//   flyTicks(from, to)            a check mark flies from each saved cell to the period status
//   celebrate(key, from)          confetti, at most once per key in this browser
// Numbers need no call: an element with data-count="7.5" shows that many hours (data-zero for 0,
// data-unit after the number) and counts to a new value when the attribute changes.
(() => {
  const root = document.documentElement;

  // Speeds come from the CSS, so reduced motion (speed 0) turns the script's animations off too.
  // The CSS is minified, so 150ms may arrive as .15s.
  const ms = name => {
    const value = getComputedStyle(root).getPropertyValue(name).trim();
    const n = parseFloat(value) || 0;
    return value.endsWith('ms') ? n : n * 1000;
  };
  const speed = () => ms('--motion');
  const stagger = () => ms('--motion-stagger');
  const ease = () => getComputedStyle(root).getPropertyValue('--motion-ease').trim() || 'ease-out';
  const ready = () => root.classList.contains('motion-ready');

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
    const duration = speed();
    cancelAnimationFrame(el._frame);
    if (from === undefined || from === to || !duration || !ready()) {
      show(el, to);
      return;
    }
    const start = performance.now();
    const step = now => {
      const t = Math.min(1, Math.max(0, (now - start) / duration)); // a frame can be stamped before start
      show(el, t < 1 ? from + (to - from) * (1 - Math.pow(1 - t, 3)) : to);
      if (t < 1) el._frame = requestAnimationFrame(step);
    };
    el._frame = requestAnimationFrame(step);
  };

  // ---- Collapse ----
  const collapse = selector => {
    const rows = [...document.querySelectorAll(selector)];
    if (!rows.length || !speed()) return Promise.resolve();
    return Promise.all(rows.map(el => {
      el.style.overflow = 'hidden';
      return el.animate(
        [{ height: `${el.offsetHeight}px`, opacity: 1 }, { height: '0px', paddingTop: '0px', paddingBottom: '0px', opacity: 0 }],
        { duration: speed(), easing: ease(), fill: 'forwards' }
      ).finished;
    })).then(() => undefined);
  };

  // ---- Glide ----
  const glide = grid => {
    const highlight = grid?.querySelector('[data-day-glide]');
    if (!highlight) return;
    const column = grid.querySelector('[data-selected-day]');
    highlight.style.opacity = column ? '' : '0';
    if (!column) return;
    highlight.style.width = `${column.offsetWidth}px`;
    highlight.style.transform = `translateX(${column.offsetLeft}px)`;
    // The first placement jumps; later ones glide.
    if (!('placed' in highlight.dataset)) requestAnimationFrame(() => highlight.dataset.placed = '');
  };
  addEventListener('resize', () => document.querySelectorAll('[data-day-glide]').forEach(h => glide(h.parentElement)));

  // ---- Flying pieces ----
  const center = r => ({ x: r.left + r.width / 2, y: r.top + r.height / 2 });
  // A fixed element at a point on screen, removed when its animation ends.
  const fly = (className, text, at, keyframes, options) => {
    const el = document.createElement('span');
    el.className = className;
    el.textContent = text;
    el.style.left = `${at.x}px`;
    el.style.top = `${at.y}px`;
    document.body.append(el);
    el.animate(keyframes, { easing: ease(), fill: 'both', ...options }).finished.then(() => el.remove());
    return el;
  };
  const restart = (el, className) => {
    el.classList.remove(className);
    void el.offsetWidth;
    el.classList.add(className);
  };

  const flyTicks = (fromSelector, toSelector) => {
    const target = document.querySelector(toSelector);
    const duration = speed();
    if (!target || !duration) return;
    const to = target.getBoundingClientRect();
    const end = { x: to.left + 12, y: to.top + to.height / 2 };
    const sources = [...document.querySelectorAll(fromSelector)].slice(0, 24);
    sources.forEach((cell, i) => {
      const at = center(cell.getBoundingClientRect());
      const dx = end.x - at.x, dy = end.y - at.y;
      fly('motion-tick', '✓', at,
        [{ transform: 'translate(-50%, -50%) scale(0.6)', opacity: 0 },
         { transform: 'translate(-50%, -50%) scale(1.2)', opacity: 1, offset: 0.25 },
         { transform: `translate(calc(-50% + ${dx}px), calc(-50% + ${dy}px)) scale(0.8)`, opacity: 0.2 }],
        { duration, delay: i * stagger() });
    });
    if (sources.length) setTimeout(() => restart(target, 'motion-received'), duration + sources.length * stagger());
  };

  const COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#8b5cf6', '#f43f5e', '#06b6d4'];
  // True the first time a key is seen in this browser.
  const firstTime = key => {
    try {
      const k = `jira-time.celebrated.${key}`;
      if (localStorage.getItem(k)) return false;
      localStorage.setItem(k, new Date().toISOString());
    } catch { /* no storage: celebrate anyway */ }
    return true;
  };
  const celebrate = (key, fromSelector) => {
    if (!speed() || !firstTime(key)) return;
    const from = document.querySelector(fromSelector)?.getBoundingClientRect();
    const at = from ? center(from) : { x: innerWidth / 2, y: 120 };
    for (let i = 0; i < 48; i++) {
      const angle = Math.random() * Math.PI * 2, distance = 60 + Math.random() * 120;
      const dx = Math.cos(angle) * distance, dy = Math.sin(angle) * distance - 40;
      fly('motion-confetti', '', at,
        [{ transform: 'translate(-50%, -50%)', opacity: 1 },
         { transform: `translate(calc(-50% + ${dx}px), calc(-50% + ${dy}px)) rotate(${Math.random() * 360}deg)`, opacity: 1, offset: 0.6 },
         { transform: `translate(calc(-50% + ${dx * 1.1}px), calc(-50% + ${dy + 80}px)) rotate(${Math.random() * 720}deg)`, opacity: 0 }],
        { duration: speed() * 4 }).style.background = COLORS[i % COLORS.length];
    }
  };

  // ---- Start ----
  const start = () => {
    new MutationObserver(records => {
      for (const r of records) {
        if (r.type === 'attributes') { count(r.target); continue; }
        for (const node of r.addedNodes) {
          if (!(node instanceof Element)) continue;
          if (node.matches('[data-count]')) count(node);
          node.querySelectorAll('[data-count]').forEach(count);
        }
      }
    }).observe(document.body, { subtree: true, childList: true, attributes: true, attributeFilter: ['data-count'] });
    document.querySelectorAll('[data-count]').forEach(count);

    // Let the first screen draw still, then animate what changes from then on.
    setTimeout(() => {
      document.querySelectorAll('[class*="motion-"]').forEach(el => el.dataset.settled = '');
      root.classList.add('motion-ready');
    }, 800);
  };
  if (document.body) start(); else addEventListener('DOMContentLoaded', start);

  window.jiraMotion = { collapse, glide, flyTicks, celebrate };
})();
