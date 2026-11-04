// What follows Save all (Components/Sheet/SaveMotion.cs gives the steps, MotionScript.PlaySaveAsync calls land):
//   1. Lift: when Save is clicked in the Save all modal, each day's line is copied where it stands, before the
//      modal closes. The copies wait there, breathing, while Jira saves.
//   2. Land: each saved day's copy flies into its day ([data-day-cell], the grid's day header or the phone
//      list's row), following the cell every frame so it ends exactly on it, also when the page or the grid
//      scrolls; copies of days that weren't saved fade where they are.
//   3. Ripple: the cells of each saved week pulse one after another, then the ticks fly (wwwroot/motion.js).
// With reduced motion (speed 0) nothing is lifted, so nothing plays.
(() => {
  const motion = () => window.jiraMotion;
  const lifted = new Map(); // day (yyyy-MM-dd) → copy of its line
  let forget;

  const remove = el => el.animate([{ opacity: getComputedStyle(el).opacity }, { opacity: 0 }],
    { duration: 200, fill: 'forwards' }).finished.then(() => el.remove());

  const drop = () => {
    lifted.forEach(remove);
    lifted.clear();
  };

  // A copy of a line of the modal, fixed where the line is; it moves by transform only.
  const copy = line => {
    const r = line.getBoundingClientRect();
    const el = line.cloneNode(true);
    el.removeAttribute('id');
    el.className += ' save-flight';
    Object.assign(el.style, { left: '0px', top: '0px', width: `${r.width}px`, height: `${r.height}px` });
    el.dataset.saveFlight = '';
    el._from = { x: r.left, y: r.top, w: r.width, h: r.height };
    el.style.transform = `translate(${r.left}px, ${r.top}px)`;
    document.body.append(el);
    return el;
  };

  const lift = dialog => {
    drop();
    if (!motion()?.speed()) return;
    dialog.querySelectorAll('[data-save-all-day]').forEach(day => {
      const line = day.firstElementChild;
      if (line) lifted.set(day.dataset.saveAllDay, copy(line));
    });
    clearTimeout(forget);
    forget = setTimeout(drop, 30000); // the save never finished: don't leave copies behind
  };
  addEventListener('click', e => {
    const save = e.target.closest?.('[data-save-all-confirm]');
    if (save && !save.disabled) lift(save.closest('dialog'));
  }, true);

  const visibleCell = day => [...document.querySelectorAll(`[data-day-cell="${day}"]`)].find(el => el.offsetParent);

  // Scrolls the grid sideways so the cell shows; the flight follows it there.
  const reveal = cell => {
    const grid = cell.closest('[data-period-grid]');
    if (!grid) return;
    const g = grid.getBoundingClientRect(), c = cell.getBoundingClientRect();
    const sticky = grid.querySelector('th')?.offsetWidth ?? 0;
    if (c.left < g.left + sticky) grid.scrollBy({ left: c.left - g.left - sticky - 8, behavior: 'smooth' });
    else if (c.right > g.right) grid.scrollBy({ left: c.right - g.right + 8, behavior: 'smooth' });
  };

  const ease = t => (t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2);

  const fly = (el, cell, duration) => {
    const from = el._from, start = performance.now();
    el.classList.add('save-flight-going');
    const frame = now => {
      const t = Math.min(1, Math.max(0, (now - start) / duration)), k = ease(t);
      const to = cell.getBoundingClientRect(); // measured every frame: the page may scroll meanwhile
      const scale = 1 + (Math.min(to.width / from.w, to.height / from.h) - 1) * k;
      const x = from.x + (to.left + to.width / 2 - from.x - from.w / 2) * k + from.w * (1 - scale) / 2;
      const y = from.y + (to.top + to.height / 2 - from.y - from.h / 2) * k + from.h * (1 - scale) / 2;
      el.style.transform = `translate(${x}px, ${y}px) scale(${scale})`;
      el.style.opacity = String(t < 0.7 ? 1 : 1 - (t - 0.7) / 0.3);
      if (t < 1) requestAnimationFrame(frame); else el.remove();
    };
    requestAnimationFrame(frame);
  };

  const ripple = (cells, pulse) => cells.forEach(({ day, at }) => setTimeout(() => {
    const cell = visibleCell(day);
    if (!cell) return;
    cell.style.setProperty('--ripple-duration', `${pulse}ms`);
    motion().restart(cell, 'motion-ripple');
    setTimeout(() => cell.classList.remove('motion-ripple'), pulse);
  }, at));

  const land = (steps, flight, pulse, endsBy, ticksFrom, ticksTo) => {
    clearTimeout(forget);
    const speed = motion()?.speed() ?? 0;
    if (!speed) return drop();
    for (const [day, el] of lifted) {
      const cell = steps.fly.includes(day) ? visibleCell(day) : null;
      if (!cell) { remove(el); continue; }
      reveal(cell);
      fly(el, cell, flight);
    }
    lifted.clear();
    ripple(steps.ripple, pulse);
    if (steps.flyTicks)
      setTimeout(() => motion().flyTicks(ticksFrom, ticksTo, endsBy - steps.ticksAt - speed), steps.ticksAt);
  };

  window.jiraFlight = { land };
})();
