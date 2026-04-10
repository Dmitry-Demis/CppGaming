/**
 * chapter-map.js
 * Рендерит карточки глав и модальную карту параграфов.
 * Заменяет buildChaptersGrid из index.html.
 */

(function () {
    'use strict';

    // ── Цвета глав (циклически) ──────────────────────────────────────────────
    const CH_COLORS = [
        '#58a6ff', '#f78166', '#7ee787', '#d2a8ff', '#ffa657',
        '#79c0ff', '#ff7b72', '#56d364', '#bc8cff', '#e3b341',
        '#38bdf8', '#fb923c', '#34d399', '#a78bfa', '#fbbf24',
        '#60a5fa', '#f87171', '#6ee7b7', '#c084fc', '#facc15',
        '#93c5fd', '#fca5a5', '#86efac', '#e879f9', '#fde68a',
        '#67e8f9', '#fdba74', '#bbf7d0', '#f0abfc', '#fef08a',
    ];

    // ── Иконки глав ─────────────────────────────────────────────────────────
    const CH_ICONS = [
        '⚡','🔢','🧮','🔬','🔧','🔄','🏷️','📦','🔁','🌿',
        '🎲','🔀','📐','🔗','🧩','🔤','📊','🏗️','📋','🎯',
        '🌀','⚙️','🔭','🗂️','💡','🔮','🧬','🛠️','🎓','🚀',
    ];

    // ── SVG паттерны для карточек (8 вариантов) ─────────────────────────────
    function getPattern(idx, color) {
        const t = idx % 8;
        const c = color;
        switch (t) {
            case 0: // Спираль
                return `<svg viewBox="0 0 200 200" xmlns="http://www.w3.org/2000/svg">
                    <path d="M100,100 m0,-60 a60,60 0 1,1 -0.1,0 m0,15 a45,45 0 1,1 -0.1,0 m0,15 a30,30 0 1,1 -0.1,0 m0,15 a15,15 0 1,1 -0.1,0" stroke="${c}" stroke-width="1.5" fill="none"/>
                </svg>`;
            case 1: // Сетка точек
                return `<svg viewBox="0 0 200 200" xmlns="http://www.w3.org/2000/svg">
                    ${Array.from({length:6},(_,r)=>Array.from({length:6},(_,c2)=>`<circle cx="${20+c2*32}" cy="${20+r*32}" r="2.5" fill="${c}"/>`).join('')).join('')}
                </svg>`;
            case 2: // Шестиугольники
                return `<svg viewBox="0 0 200 200" xmlns="http://www.w3.org/2000/svg">
                    <polygon points="60,20 80,20 90,37 80,54 60,54 50,37" stroke="${c}" stroke-width="1.5" fill="none"/>
                    <polygon points="100,20 120,20 130,37 120,54 100,54 90,37" stroke="${c}" stroke-width="1.5" fill="none"/>
                    <polygon points="140,20 160,20 170,37 160,54 140,54 130,37" stroke="${c}" stroke-width="1.5" fill="none"/>
                    <polygon points="80,54 100,54 110,71 100,88 80,88 70,71" stroke="${c}" stroke-width="1.5" fill="none"/>
                    <polygon points="120,54 140,54 150,71 140,88 120,88 110,71" stroke="${c}" stroke-width="1.5" fill="none"/>
                    <polygon points="60,88 80,88 90,105 80,122 60,122 50,105" stroke="${c}" stroke-width="1.5" fill="none"/>
                    <polygon points="100,88 120,88 130,105 120,122 100,122 90,105" stroke="${c}" stroke-width="1.5" fill="none"/>
                    <polygon points="140,88 160,88 170,105 160,122 140,122 130,105" stroke="${c}" stroke-width="1.5" fill="none"/>
                </svg>`;
            case 3: // Диагональные линии
                return `<svg viewBox="0 0 200 200" xmlns="http://www.w3.org/2000/svg">
                    ${Array.from({length:10},(_,i)=>`<line x1="${-20+i*24}" y1="0" x2="${i*24+60}" y2="200" stroke="${c}" stroke-width="1.2"/>`).join('')}
                </svg>`;
            case 4: // Концентрические окружности
                return `<svg viewBox="0 0 200 200" xmlns="http://www.w3.org/2000/svg">
                    <circle cx="100" cy="100" r="20" stroke="${c}" stroke-width="1.5" fill="none"/>
                    <circle cx="100" cy="100" r="40" stroke="${c}" stroke-width="1.2" fill="none"/>
                    <circle cx="100" cy="100" r="60" stroke="${c}" stroke-width="1" fill="none"/>
                    <circle cx="100" cy="100" r="80" stroke="${c}" stroke-width="0.8" fill="none"/>
                    <circle cx="100" cy="100" r="100" stroke="${c}" stroke-width="0.6" fill="none"/>
                </svg>`;
            case 5: // Лабиринт
                return `<svg viewBox="0 0 200 200" xmlns="http://www.w3.org/2000/svg">
                    <rect x="20" y="20" width="160" height="160" stroke="${c}" stroke-width="1.5" fill="none"/>
                    <polyline points="20,60 80,60 80,40" stroke="${c}" stroke-width="1.5" fill="none"/>
                    <polyline points="120,20 120,80 60,80 60,120 140,120 140,80 180,80" stroke="${c}" stroke-width="1.5" fill="none"/>
                    <polyline points="20,120 40,120 40,160" stroke="${c}" stroke-width="1.5" fill="none"/>
                    <polyline points="100,120 100,160 160,160 160,140" stroke="${c}" stroke-width="1.5" fill="none"/>
                    <polyline points="20,160 20,100" stroke="${c}" stroke-width="1.5" fill="none"/>
                </svg>`;
            case 6: // Звёздная сетка
                return `<svg viewBox="0 0 200 200" xmlns="http://www.w3.org/2000/svg">
                    ${Array.from({length:4},(_,r)=>Array.from({length:4},(_,c2)=>{
                        const cx=30+c2*47, cy=30+r*47;
                        return `<line x1="${cx-8}" y1="${cy}" x2="${cx+8}" y2="${cy}" stroke="${c}" stroke-width="1.5"/>
                                <line x1="${cx}" y1="${cy-8}" x2="${cx}" y2="${cy+8}" stroke="${c}" stroke-width="1.5"/>
                                <line x1="${cx-6}" y1="${cy-6}" x2="${cx+6}" y2="${cy+6}" stroke="${c}" stroke-width="1"/>
                                <line x1="${cx+6}" y1="${cy-6}" x2="${cx-6}" y2="${cy+6}" stroke="${c}" stroke-width="1"/>`;
                    }).join('')).join('')}
                </svg>`;
            case 7: // Волны
                return `<svg viewBox="0 0 200 200" xmlns="http://www.w3.org/2000/svg">
                    ${Array.from({length:7},(_,i)=>`<path d="M0,${20+i*26} Q25,${10+i*26} 50,${20+i*26} Q75,${30+i*26} 100,${20+i*26} Q125,${10+i*26} 150,${20+i*26} Q175,${30+i*26} 200,${20+i*26}" stroke="${c}" stroke-width="1.5" fill="none"/>`).join('')}
                </svg>`;
        }
    }

    // ── Вычислить медальки главы ─────────────────────────────────────────────
    function chapterMastery(chapter, progressMap) {
        if (!progressMap) return 0;
        const paras = chapter.paragraphs || [];
        if (!paras.length) return 0;
        let sum = 0;
        paras.forEach(p => {
            const prog = progressMap[p.id || p];
            if (!prog) return;
            const attempted = Object.values(prog.tests || {}).filter(t => t.attemptsCount > 0);
            if (!attempted.length) return;
            const seenSum  = attempted.reduce((s, t) => s + (t.seenCount || 0), 0);
            const totalSum = attempted.reduce((s, t) => s + (t.totalQuestionsInBank || 0), 0);
            if (totalSum > 0) sum += Math.min(seenSum / totalSum * 100, 100);
        });
        return paras.length > 0 ? sum / paras.length : 0;
    }

    // ── Бейдж прогресса параграфа ────────────────────────────────────────────
    function paraBadge(prog) {
        if (!prog) return { cls: 'none', label: '—' };
        const attempted = Object.values(prog.tests || {}).filter(t => t.attemptsCount > 0);
        if (!attempted.length) return { cls: 'none', label: '—' };
        const seenSum  = attempted.reduce((s, t) => s + (t.seenCount || 0), 0);
        const totalSum = attempted.reduce((s, t) => s + (t.totalQuestionsInBank || 0), 0);
        const m = totalSum > 0 ? Math.min(seenSum / totalSum * 100, 100) : 0;
        if (m >= 100) return { cls: 'gold',   label: `🥇 ${m.toFixed(0)}%` };
        if (m >= 90)  return { cls: 'silver', label: `🥈 ${m.toFixed(0)}%` };
        if (m >= 80)  return { cls: 'bronze', label: `🥉 ${m.toFixed(0)}%` };
        if (m >= 70)  return { cls: 'pass',   label: `✓ ${m.toFixed(0)}%` };
        if (m > 0)    return { cls: 'fail',   label: `${m.toFixed(0)}%` };
        return { cls: 'none', label: '—' };
    }

    // ── Номер главы из ID ────────────────────────────────────────────────────
    function chNum(id) {
        const m = id.match(/^chapter-(.+)$/);
        return m ? m[1] : id;
    }

    // ── Стандарт главы (из первого параграфа со стандартом) ─────────────────
    function chStd(chapter, slotsMap) {
        if (!slotsMap) return null;
        for (const p of (chapter.paragraphs || [])) {
            const pid = p.id || p;
            const key = `${chapter.id}/${chapter.groupId}/${pid}`;
            const slots = slotsMap[key];
            if (slots?.length) {
                const stds = [...new Set(slots.map(s => s.std))].sort((a,b)=>Number(a)-Number(b));
                return stds[0];
            }
        }
        return null;
    }

    // ── Модальное окно ───────────────────────────────────────────────────────
    let _overlay = null;

    function _ensureOverlay() {
        if (_overlay) return _overlay;
        _overlay = document.createElement('div');
        _overlay.className = 'chmap-overlay';
        _overlay.innerHTML = `<div class="chmap-modal" role="dialog" aria-modal="true"></div>`;
        document.body.appendChild(_overlay);
        _overlay.addEventListener('click', e => {
            if (e.target === _overlay) closeModal();
        });
        document.addEventListener('keydown', e => {
            if (e.key === 'Escape') closeModal();
        });
        return _overlay;
    }

    function closeModal() {
        if (!_overlay) return;
        _overlay.classList.remove('open');
    }

    function openModal(chapter, ci, color, icon, progressMap, unlockedSet, isAdmin, slotsMap) {
        const overlay = _ensureOverlay();
        const modal   = overlay.querySelector('.chmap-modal');
        const mastery = chapterMastery(chapter, progressMap);
        const paras   = chapter.paragraphs || [];
        const num     = chNum(chapter.id);
        const std     = chStd(chapter, slotsMap);

        const STD_COLORS = {
            '11':'rgba(56,189,248,.12)', '14':'rgba(251,191,36,.1)',
            '17':'rgba(52,211,153,.1)',  '20':'rgba(167,139,250,.12)',
            '23':'rgba(244,114,182,.1)', '26':'rgba(251,113,133,.1)'
        };
        const STD_TEXT = {
            '11':'#7dd3fc','14':'#fcd34d','17':'#6ee7b7',
            '20':'#c4b5fd','23':'#f9a8d4','26':'#fda4af'
        };
        const STD_BORDER = {
            '11':'rgba(56,189,248,.3)','14':'rgba(251,191,36,.25)',
            '17':'rgba(52,211,153,.25)','20':'rgba(167,139,250,.3)',
            '23':'rgba(244,114,182,.25)','26':'rgba(251,113,133,.25)'
        };

        // Строим узлы
        const chapterIndexHref = `theory/${chapter.id}/${chapter.groupId}/index.html`;

        let nodesHtml = '';
        paras.forEach((p, localIdx) => {
            const pid   = p.id || p;
            const title = p.title || pid;
            const href  = `theory/${chapter.id}/${chapter.groupId}/${pid}.html`;
            const prog  = progressMap?.[pid];
            const badge = paraBadge(prog);
            const isFirst = localIdx === 0;
            const prevId  = localIdx > 0 ? (paras[localIdx-1].id || paras[localIdx-1]) : null;
            const locked  = !isAdmin && !isFirst && prevId && !unlockedSet.has(prevId);
            const done    = badge.cls !== 'none';

            // Стандарты параграфа
            const pageKey = `${chapter.id}/${chapter.groupId}/${pid}`;
            const slots   = slotsMap?.[pageKey] || [];
            const stdSet  = {};
            slots.forEach(s => { if (!stdSet[s.std]) stdSet[s.std] = {total:0,purchased:0}; stdSet[s.std].total++; if(s.purchased) stdSet[s.std].purchased++; });
            const stdsHtml = Object.entries(stdSet).sort(([a],[b])=>Number(a)-Number(b))
                .map(([s,info]) => {
                    const bg = STD_COLORS[s]||'rgba(255,255,255,.06)';
                    const col = STD_TEXT[s]||'#aaa';
                    const border = STD_BORDER[s]||'rgba(255,255,255,.15)';
                    const done2 = info.purchased >= info.total;
                    return `<span style="background:${bg};color:${col};border:1px solid ${border};opacity:${done2?.5:1}">${done2?'✓ ':''}${s}</span>`;
                }).join('');

            // Иконка узла
            const nodeEmoji = done ? '✓' : locked ? '🔒' : (localIdx === 0 ? '▶' : `${localIdx+1}`);

            nodesHtml += `
            <a class="chmap-node${locked?' locked':''}${done?' done':''}"
               href="${locked ? '#' : href}"
               data-para-id="${pid}"
               ${locked ? 'onclick="return false"' : ''}>
                <div class="chmap-node-icon" style="--ch-color:${color}">
                    <span>${nodeEmoji}</span>
                    ${locked ? '<span class="chmap-node-lock">🔒</span>' : ''}
                </div>
                <div class="chmap-node-text">
                    <div class="chmap-node-num">§${num}.${localIdx+1}</div>
                    <div class="chmap-node-title">${title}</div>
                    ${stdsHtml ? `<div class="chmap-node-stds">${stdsHtml}</div>` : ''}
                </div>
                <span class="chmap-node-badge chmap-node-badge--${badge.cls}">${badge.label}</span>
            </a>`;
        });

        modal.style.setProperty('--ch-color', color);
        modal.innerHTML = `
            <div class="chmap-modal-header">
                <div class="chmap-modal-color-bar"></div>
                <div class="chmap-modal-icon">${icon}</div>
                <div class="chmap-modal-info">
                    <div class="chmap-modal-num">Глава ${num} · ${paras.length} параграфов</div>
                    <div class="chmap-modal-title">${chapter.title}</div>
                    <div class="chmap-modal-desc">${chapter.description || ''}</div>
                    <div class="chmap-modal-meta">
                        ${chapter.level ? `<span class="chmap-level chmap-level--${(chapter.level||'').replace('+','-plus')}">${chapter.level}</span>` : ''}
                        ${std ? `<span style="font-size:.65rem;font-weight:700;padding:2px 7px;border-radius:4px;background:${STD_COLORS[std]||'rgba(255,255,255,.06)'};color:${STD_TEXT[std]||'#aaa'};border:1px solid ${STD_BORDER[std]||'rgba(255,255,255,.15)'}">C++${std}</span>` : ''}
                        <div class="chmap-modal-medals">
                            <span class="chmap-modal-medal${mastery>=80?' earned':''}">🥉</span>
                            <span class="chmap-modal-medal${mastery>=90?' earned':''}">🥈</span>
                            <span class="chmap-modal-medal${mastery>=100?' earned':''}">🥇</span>
                        </div>
                    </div>
                </div>
                <button class="chmap-modal-close" aria-label="Закрыть">✕</button>
            </div>
            <div class="chmap-modal-body">
                <div class="chmap-map-wrap">
                    <div class="chmap-nodes">${nodesHtml}</div>
                </div>
            </div>
            <a class="chmap-modal-cta" href="${chapterIndexHref}" style="background:linear-gradient(135deg,${color}cc,${color}88)">
                Открыть страницу главы →
            </a>`;

        modal.querySelector('.chmap-modal-close').addEventListener('click', closeModal);

        // Тост для заблокированных
        modal.querySelectorAll('.chmap-node.locked').forEach(node => {
            node.addEventListener('click', e => {
                e.preventDefault();
                _showToast('🔒 Пройдите тесты предыдущего параграфа на ≥70%');
            });
        });

        overlay.classList.add('open');
    }

    function _showToast(msg) {
        let t = document.getElementById('chmap-toast');
        if (!t) {
            t = document.createElement('div');
            t.id = 'chmap-toast';
            t.style.cssText = 'position:fixed;bottom:80px;left:50%;transform:translateX(-50%);background:var(--bg-card);border:1px solid var(--border-secondary);color:var(--text-primary);padding:10px 18px;border-radius:12px;font-size:.85rem;font-weight:600;z-index:9999;box-shadow:var(--shadow-lg);opacity:0;transition:opacity .2s;pointer-events:none;white-space:nowrap';
            document.body.appendChild(t);
        }
        t.textContent = msg;
        t.style.opacity = '1';
        clearTimeout(t._t);
        t._t = setTimeout(() => { t.style.opacity = '0'; }, 2500);
    }

    // ── Главная функция рендера ──────────────────────────────────────────────
    window.buildChaptersGrid = async function () {
        const grid = document.getElementById('chapters-grid');
        const desc = document.getElementById('course-description');
        if (!grid) return;

        // Скелетоны
        grid.innerHTML = Array.from({length:8}, () => `
            <div class="chmap-card chmap-card--skeleton" style="background:var(--bg-card);border-radius:var(--radius-xl);min-height:180px;border:1px solid var(--border-primary)"></div>
        `).join('');

        let structure;
        try { structure = await loadCourseStructure(''); }
        catch {
            grid.innerHTML = '<p style="color:var(--text-secondary)">Не удалось загрузить структуру курса.</p>';
            return;
        }

        if (desc) desc.textContent = structure.course?.description || '';

        const chapters = structure.chapters || [];
        if (!chapters.length) { grid.innerHTML = ''; return; }

        // Загружаем прогресс и разблокированные параграфы параллельно
        const user = JSON.parse(localStorage.getItem('cpp_user') || 'null');
        let progressMap = null, unlockedSet = new Set(), isAdmin = false, slotsMap = {};

        if (user?.isuNumber) {
            const headers = { 'X-Isu-Number': user.isuNumber };
            const [progRes, unlockedRes, slotsRes] = await Promise.all([
                fetch(`/api/progress/${user.isuNumber}/all`, { headers, cache: 'no-store' }).then(r => r.ok ? r.json() : null).catch(() => null),
                fetch(`/api/progress/${user.isuNumber}/unlocked-paragraphs`, { headers }).then(r => r.ok ? r.json() : null).catch(() => null),
                fetch('/api/gated/all-slots', { headers }).then(r => r.ok ? r.json() : null).catch(() => null),
            ]);
            progressMap = progRes;
            unlockedSet = new Set(unlockedRes?.unlocked || []);
            isAdmin     = unlockedRes?.isAdmin || false;
            if (slotsRes?.length) {
                slotsRes.forEach(s => {
                    if (!slotsMap[s.page]) slotsMap[s.page] = [];
                    slotsMap[s.page].push(s);
                });
            }
        }

        // Рендерим карточки
        grid.innerHTML = '';
        chapters.forEach((chapter, ci) => {
            const color   = CH_COLORS[ci % CH_COLORS.length];
            const icon    = CH_ICONS[ci % CH_ICONS.length];
            const num     = chNum(chapter.id);
            const paras   = chapter.paragraphs || [];
            const mastery = chapterMastery(chapter, progressMap);
            const std     = chStd(chapter, slotsMap);
            const levelCls = (chapter.level || '').replace('+', '-plus');

            const card = document.createElement('div');
            card.className = 'chmap-card';
            card.style.setProperty('--ch-color', color);
            card.style.animationDelay = (ci * 0.04) + 's';

            card.innerHTML = `
                <div class="chmap-pattern">${getPattern(ci, color)}</div>
                ${std ? `<div class="chmap-std">C++${std}</div>` : ''}
                <div class="chmap-medals">
                    <span class="chmap-medal${mastery>=80?' earned':''}">🥉</span>
                    <span class="chmap-medal${mastery>=90?' earned':''}">🥈</span>
                    <span class="chmap-medal${mastery>=100?' earned':''}">🥇</span>
                </div>
                <div class="chmap-body">
                    <div class="chmap-num">Глава ${num}</div>
                    <div class="chmap-title">${chapter.title}</div>
                    <div class="chmap-footer">
                        ${chapter.level ? `<span class="chmap-level chmap-level--${levelCls}">${chapter.level}</span>` : '<span></span>'}
                        <span class="chmap-para-count">${paras.length} §§</span>
                    </div>
                </div>
                <div class="chmap-progress-bar">
                    <div class="chmap-progress-fill" style="width:${Math.min(mastery,100).toFixed(1)}%"></div>
                </div>`;

            card.addEventListener('click', () => {
                openModal(chapter, ci, color, icon, progressMap, unlockedSet, isAdmin, slotsMap);
            });

            grid.appendChild(card);
        });
    };

})();
