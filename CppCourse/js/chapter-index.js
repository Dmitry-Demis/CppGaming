/**
 * chapter-index.js — строит список параграфов главы с бейджами усвоения.
 * Прогрессивная загрузка: скелетоны → карточки → прогресс.
 */

function chapterNumFromId(id) {
    const m = id.match(/^chapter-(.+)$/);
    return m ? m[1] : id;
}

async function buildChapterIndex(chapterId) {
    const grid = document.getElementById('sections-grid');
    if (!grid) return;

    // ── Фаза 1: скелетоны ────────────────────────────────────────────────
    grid.innerHTML = Array.from({ length: 5 }, () => `
        <div class="section-card section-card--skeleton">
            <div class="skeleton sk-num"></div>
            <div class="skeleton sk-text"></div>
        </div>`).join('');

    try {
        // ── Фаза 2: загружаем структуру ──────────────────────────────────
        const data = await loadCourseStructure('../../../');
        const chapter = data.chapters.find(c => c.id === chapterId);
        if (!chapter) { grid.innerHTML = '<p style="color:var(--text-secondary)">Глава не найдена.</p>'; return; }
        const paragraphs = chapter.paragraphs || [];
        if (!paragraphs.length) { grid.innerHTML = '<p style="color:var(--text-secondary)">Параграфы не найдены.</p>'; return; }
        const chapterNum = chapterNumFromId(chapterId);

        const badge = document.querySelector('.section-badge');
        if (badge) badge.textContent = `Глава ${chapterNum}`;
        const navActive = document.querySelector('.header-nav a.active');
        if (navActive) navActive.textContent = `Глава ${chapterNum}`;

        // ── Фаза 3: рендерим карточки (без прогресса) ────────────────────
        grid.innerHTML = paragraphs.map((p, i) => {
            const href = p.id + '.html';
            const testCount = (p.tests || []).length;
            const testBadge = testCount > 0
                ? `<span class="sci-test-badge">${testCount > 1 ? testCount + '\u202fтеста' : 'Тест'}</span>`
                : '';
            return `<a href="${href}" class="section-card section-card--loaded" data-para-id="${p.id}" style="animation-delay:${i * 0.03}s">
                <div class="section-card__number">§${chapterNum}.${i + 1}</div>
                <div class="section-card__body"><div class="section-card__title">${p.title}</div></div>
                ${testBadge}
                <span class="sci-mastery-badge para-badge para-badge--none para-badge--loading" data-mastery-badge>&nbsp;</span>
                <span class="section-card__arrow">&rsaquo;</span>
            </a>`;
        }).join('');

        if (window.ParagraphLock) await window.ParagraphLock.applyLocksToChapterIndex(data);

        // ── Фаза 4: прогресс (только если залогинен) ─────────────────────
        const user = JSON.parse(localStorage.getItem('cpp_user') || 'null');
        if (!user?.isuNumber) {
            // Убираем анимацию загрузки у бейджей
            document.querySelectorAll('[data-mastery-badge]').forEach(b => {
                b.classList.remove('para-badge--loading');
                b.className = 'sci-mastery-badge para-badge para-badge--none';
                b.textContent = '';
            });
            return;
        }

        const progressMap = await fetch(`/api/progress/${user.isuNumber}/all`, {
            headers: { 'X-Isu-Number': user.isuNumber },
            cache: 'no-store'
        }).then(r => r.ok ? r.json() : null).catch(() => null);

        paragraphs.forEach(p => {
            const card  = grid.querySelector(`[data-para-id="${p.id}"]`);
            const badge = card?.querySelector('[data-mastery-badge]');
            if (!badge) return;

            badge.classList.remove('para-badge--loading');

            const prog = progressMap?.[p.id];
            if (!prog) { badge.className = 'sci-mastery-badge para-badge para-badge--none'; badge.textContent = ''; return; }

            const attempted = Object.values(prog.tests || {}).filter(t => t.attemptsCount > 0);
            if (!attempted.length) { badge.className = 'sci-mastery-badge para-badge para-badge--none'; badge.textContent = ''; return; }

            const seenSum  = attempted.reduce((s, t) => s + (t.seenCount || 0), 0);
            const totalSum = attempted.reduce((s, t) => s + (t.totalQuestionsInBank || 0), 0);
            const mastery  = totalSum > 0 ? Math.min(seenSum / totalSum * 100, 100) : 0;
            const pct      = mastery.toFixed(1) + '%';

            if (mastery >= 100)     { badge.className = 'sci-mastery-badge para-badge para-badge--gold';   badge.textContent = `🥇 ${pct}`; }
            else if (mastery >= 90) { badge.className = 'sci-mastery-badge para-badge para-badge--silver'; badge.textContent = `🥈 ${pct}`; }
            else if (mastery >= 80) { badge.className = 'sci-mastery-badge para-badge para-badge--bronze'; badge.textContent = `🥉 ${pct}`; }
            else if (mastery >= 70) { badge.className = 'sci-mastery-badge para-badge para-badge--pass';   badge.textContent = `✓ ${pct}`; }
            else                    { badge.className = 'sci-mastery-badge para-badge para-badge--fail';   badge.textContent = pct; }
        });

    } catch (e) {
        grid.innerHTML = '<p style="color:var(--text-secondary)">Не удалось загрузить секции: ' + e.message + '</p>';
    }
}
