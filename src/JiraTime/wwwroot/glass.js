// The light that follows the mouse over cards and panels (Styles/glass.css). The pointer's place is written as
// --light-x / --light-y on the card under it, at most once per animation frame; that card gets data-lit.
// No light on touch-only devices (no hover, coarse pointer) or when the system asks for reduced motion.
(() => {
  const mouse = matchMedia('(hover: hover) and (pointer: fine)');
  const still = matchMedia('(prefers-reduced-motion: reduce)');
  const SURFACES = '.card, .panel';
  let lit = null, pointer = null, frame = 0;

  const off = () => {
    if (lit) delete lit.dataset.lit;
    lit = null;
  };

  const draw = () => {
    frame = 0;
    const card = pointer?.target instanceof Element ? pointer.target.closest(SURFACES) : null;
    if (card !== lit) {
      off();
      if (card) (lit = card).dataset.lit = '';
    }
    if (!card) return;
    const box = card.getBoundingClientRect();
    card.style.setProperty('--light-x', `${pointer.clientX - box.left}px`);
    card.style.setProperty('--light-y', `${pointer.clientY - box.top}px`);
  };

  addEventListener('pointermove', e => {
    if (e.pointerType !== 'mouse' || !mouse.matches || still.matches) { off(); return; }
    pointer = e;
    if (!frame) frame = requestAnimationFrame(draw);
  }, { passive: true });
  document.documentElement.addEventListener('pointerleave', () => { pointer = null; off(); });
})();
