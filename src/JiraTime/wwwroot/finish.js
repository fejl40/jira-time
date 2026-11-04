// The finish when a period becomes complete (Components/MotionScript.cs, timing from Domain/PeriodFinish.cs):
// a light sweeps across the grid's day columns one after another (down the day list on phones) while the
// background glows, then the confetti from motion.js bursts. Once per user and period in this browser, and
// nothing at all with reduced motion. Styles: .finish-* in Styles/backdrop.css.
(() => {
  // The day columns of the grid, or the days of the phone list when the grid is hidden.
  const days = () => {
    const grid = document.querySelector('[data-day-glide]')?.parentElement;
    if (grid?.offsetParent) return [...grid.querySelectorAll('thead th')].slice(1, -1); // not Issue and Total
    return [...document.querySelectorAll('[data-day-list] [data-day-cell]')];
  };

  // One bright band over each day, lit a step after the one before; removed when it has faded.
  const sweep = (finish, ease) => {
    const table = document.querySelector('[data-day-glide]')?.parentElement;
    const column = table?.offsetParent ? table.getBoundingClientRect() : null;
    days().forEach((day, i) => {
      const box = day.getBoundingClientRect();
      const band = document.createElement('span');
      band.className = 'finish-sweep';
      band.dataset.finishSweep = '';
      Object.assign(band.style, column
        ? { left: `${box.left}px`, top: `${column.top}px`, width: `${box.width}px`, height: `${column.height}px` }
        : { left: `${box.left}px`, top: `${box.top}px`, width: `${box.width}px`, height: `${box.height}px` });
      document.body.append(band);
      band.animate(
        [{ opacity: 0, transform: 'scaleY(0.92)' }, { opacity: 1, transform: 'scaleY(1)', offset: 0.35 }, { opacity: 0, transform: 'scaleY(1)' }],
        { duration: finish.band, delay: i * finish.step, easing: ease, fill: 'both' }
      ).finished.then(() => band.remove());
    });
  };

  // The background brightens and settles back.
  const glow = finish => {
    const backdrop = document.querySelector('[data-backdrop]');
    if (!backdrop) return;
    backdrop.style.setProperty('--finish-glow', `${finish.glow}ms`);
    backdrop.dataset.glowing = '';
    jiraMotion.restart(backdrop, 'finish-glow');
    setTimeout(() => {
      backdrop.classList.remove('finish-glow');
      delete backdrop.dataset.glowing;
    }, finish.glow);
  };

  const celebrate = (key, fromSelector, finish) => {
    if (!jiraMotion.speed() || !jiraMotion.firstTime(key)) return;
    sweep(finish, jiraMotion.ease());
    glow(finish);
    setTimeout(() => jiraMotion.confetti(fromSelector), finish.confettiAt);
  };

  window.jiraFinish = { celebrate };
})();
