/* ============================================================================
   KAARIGAR — Dashboard JS
   Session-kick message relay, bar chart animation, KPI count-up
   ============================================================================ */

(function () {
    'use strict';

    // ── Bar chart: animate bars on load ──────────────────────────────────────

    const bars = document.querySelectorAll('.k-bar-chart__bar');
    if (bars.length) {
        // Store final heights then animate from 0
        const finalHeights = Array.from(bars).map(b => b.style.height);
        bars.forEach(b => { b.style.height = '4px'; b.style.transition = 'none'; });

        requestAnimationFrame(function () {
            requestAnimationFrame(function () {
                bars.forEach(function (b, i) {
                    b.style.transition = 'height 0.7s cubic-bezier(.4,0,.2,1)';
                    b.style.transitionDelay = (i * 0.08) + 's';
                    b.style.height = finalHeights[i];
                });
            });
        });
    }

    // ── KPI card count-up animation ───────────────────────────────────────────

    function animateCount(el, target, duration) {
        var start = 0;
        var step  = target / (duration / 16);
        var timer = setInterval(function () {
            start += step;
            if (start >= target) { start = target; clearInterval(timer); }
            el.textContent = Math.floor(start).toLocaleString();
        }, 16);
    }

    var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (!entry.isIntersecting) return;
            var el     = entry.target;
            var target = parseInt(el.dataset.count, 10);
            if (!isNaN(target)) animateCount(el, target, 800);
            observer.unobserve(el);
        });
    }, { threshold: 0.3 });

    document.querySelectorAll('.k-kpi-card__value').forEach(function (el) {
        var val = parseInt(el.textContent.trim(), 10);
        if (!isNaN(val) && val > 0) {
            el.dataset.count = val;
            el.textContent   = '0';
            observer.observe(el);
        }
    });

    // ── Session-kicked message: move from Session to TempData display ─────────
    // (Handled server-side via TempData in _DashboardLayout.cshtml)

})();
