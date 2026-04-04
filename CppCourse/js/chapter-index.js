/**
 * chapter-index.js — строит список параграфов главы с бейджами усвоения.
 * Использование: вызвать buildChapterIndex('chapter-2') после загрузки DOM.
 */

/** Извлекает номер главы из id: 'chapter-1' → '1', 'chapter-1b' → '1b', 'chapter-9d' → '9d' */
function chapterNumFromId(id) {
    const m = id.match(/^chapter-(.+)$/);
    return m ? m[1] : id;
}

async function buildChapterIndex(chapterId) {
    const grid = document.getElementById('sections-grid');
    if (!grid) return;
    try {
        const data = await loadCourseStructure('../../../');
        const chapter = data.chapters.find(c => c.id === chapterId);
        if (!chapter) { grid.innerHTML = '<p style="color:var(--text-secondary)">Глава не найдена.</p>'; return; }
        const paragraphs = chapter.paragraphs || [];
        if (!paragraphs.length) { grid.innerHTML = '<p style="color:var(--text-secondary)">Параграфы не найдены.</p>'; return; }
        const chapterNum = chapterNumFromId(chapterId);

        // Динамически обновляем section-badge и nav-ссылку
        const badge = document.querySelector('.section-badge');
        if (badge) badge.textContent = `Глава ${chapterNum}`;
        const navActive = document.querySelector('.header-nav a.active');
        if (navActive) navActive.textContent = `Глава ${chapterNum}`;

        // Рендерим карточки сразу (без прогресса)
        grid.innerHTML = paragraphs.map((p, i) => {
            const href = p.id + '.html';
            const testCount = (p.tests || []).length;
            const testBadge = testCount > 0
                ? `<span class="sci-test-badge">${testCount > 1 ? testCount + '\u202fтеста' : 'Тест'}</span>`
                : '';
            return `<a href="${href}" class="section-card" data-para-id="${p.id}">
                <div class="section-card__number">§${chapterNum}.${i + 1}</div>
                <div class="section-card__body"><div class="section-card__title">${p.title}</div></div>
                ${testBadge}
                <span class="sci-mastery-badge" data-mastery-badge></span>
                <span class="section-card__arrow">&rsaquo;</span>
            </a>`;
        }).join('');

        if (window.ParagraphLock) await window.ParagraphLock.applyLocksToChapterIndex(data);

        // Загружаем прогресс если залогинен
        const user = JSON.parse(localStorage.getItem('cpp_user') || 'null');
        if (!user?.isuNumber) return;

        const progressMap = await fetch(`/api/progress/${user.isuNumber}/all`, {
            headers: { 'X-Isu-Number': user.isuNumber },
            cache: 'no-store'
        }).then(r => r.ok ? r.json() : null).catch(() => null);

        if (!progressMap) return;

        paragraphs.forEach(p => {
            const prog = progressMap[p.id];
            const card = grid.querySelector(`[data-para-id="${p.id}"]`);
            const badge = card?.querySelector('[data-mastery-badge]');
            if (!badge) return;

            if (!prog) return;
            const attempted = Object.values(prog.tests || {}).filter(t => t.attemptsCount > 0);
            if (!attempted.length) return;

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
