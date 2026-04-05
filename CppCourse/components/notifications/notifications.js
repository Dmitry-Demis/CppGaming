// ============================================
// NOTIFICATIONS COMPONENT
// Колокольчик с dropdown повторения параграфов
// Заменяет js/review-banner.js
// ============================================

(function () {
    'use strict';

    // ── Загрузка CSS компонента ───────────────────────────────────────────
    (function injectCSS() {
        if (document.getElementById('ntf-css')) return;
        const link = document.createElement('link');
        link.id   = 'ntf-css';
        link.rel  = 'stylesheet';
        // Определяем путь относительно navigation.js
        const navScript = document.querySelector('script[src*="navigation.js"]');
        const navSrc = navScript?.src || '';
        const m = navSrc.match(/^(.*\/)js\/navigation\.js/);
        const base = m ? m[1] : '/';
        link.href = base + 'components/notifications/css/notifications.css';
        document.head.appendChild(link);
    })();

    // ── Получение base URL ────────────────────────────────────────────────
    function getBase() {
        const navScript = document.querySelector('script[src*="navigation.js"]');
        const navSrc = navScript?.src || '';
        const m = navSrc.match(/^(.*\/)js\/navigation\.js/);
        return m ? m[1] : '/';
    }

    // ── Основная логика ───────────────────────────────────────────────────
    function run() {
        const user = JSON.parse(localStorage.getItem('cpp_user') || 'null');
        if (!user?.isuNumber) return;

        const base = getBase();

        fetch('/api/review/due', {
            headers: { 'X-Isu-Number': user.isuNumber }
        })
        .then(r => r.ok ? r.json() : null)
        .then(async dueItems => {
            if (!dueItems || dueItems.length === 0) {
                _updateBells([], {});
                return;
            }

            let paraMap = {};
            try {
                if (typeof loadCourseStructure === 'function') {
                    const structure = await loadCourseStructure(base);
                    if (structure?.chapters) {
                        for (const ch of structure.chapters) {
                            (ch.paragraphs || []).forEach((para, idx) => {
                                const id = typeof para === 'string' ? para : para.id;
                                const title = para.title || null;
                                paraMap[id] = {
                                    title,
                                    href: `${base}theory/${ch.id}/${ch.groupId}/${id}.html`,
                                    // Номер параграфа внутри главы (1-based)
                                    num: idx + 1,
                                    chapterTitle: ch.title
                                };
                            });
                        }
                    }
                }
            } catch { /* без названий */ }

            // Сохраняем для внешнего доступа
            window._ntfItems = dueItems;
            _updateBells(dueItems, paraMap);
        })
        .catch(() => {});
    }

    // ── Обновляем оба колокольчика (мобильный + десктопный) ──────────────
    function _updateBells(items, paraMap) {
        const tryAttach = (attempts) => {
            const mobileBell   = document.getElementById('ntf-bell-btn-mobile');
            const desktopBell  = document.getElementById('ntf-bell-btn-desktop');
            if (!mobileBell && !desktopBell) {
                if (attempts < 20) setTimeout(() => tryAttach(attempts + 1), 300);
                return;
            }
            if (mobileBell)  _attachToButton(mobileBell, items, paraMap, 'up');
            if (desktopBell) _attachToButton(desktopBell, items, paraMap, 'down');
        };
        tryAttach(0);
    }

    // ── Привязываем dropdown к кнопке ────────────────────────────────────
    function _attachToButton(btn, items, paraMap, direction) {
        // Удаляем старый dropdown
        btn.querySelector('.ntf-dropdown')?.remove();

        // Обновляем badge
        const countEl = btn.querySelector('.ntf-bell-count');
        if (items.length > 0) {
            btn.classList.add('has-items');
            btn.style.display = '';
            if (countEl) { countEl.textContent = items.length; countEl.style.display = ''; }
        } else {
            btn.classList.remove('has-items');
            if (countEl) countEl.style.display = 'none';
            btn.style.display = 'none';
            return;
        }

        const dropdown = _buildDropdown(items, paraMap);

        // Позиционирование
        if (direction === 'up') {
            dropdown.style.bottom = 'calc(100% + 8px)';
            dropdown.style.top    = 'auto';
            dropdown.style.left   = '0';
        } else {
            dropdown.style.top    = 'calc(100% + 8px)';
            dropdown.style.bottom = 'auto';
            dropdown.style.left   = '0';
        }

        btn.appendChild(dropdown);

        // Toggle по клику на кнопку
        btn.addEventListener('click', (e) => {
            e.stopPropagation();
            const expanded = dropdown.classList.toggle('ntf-open');
            btn.setAttribute('aria-expanded', expanded);
        });

        // Закрываем при клике вне
        document.addEventListener('click', (e) => {
            if (!btn.contains(e.target)) {
                dropdown.classList.remove('ntf-open');
                btn.setAttribute('aria-expanded', 'false');
            }
        }, { capture: true });

        // Убираем параграф после прохождения теста
        window.addEventListener('quizCompleted', () => {
            const parts = location.pathname.replace(/\/$/, '').split('/').filter(Boolean);
            const paraId = (parts.at(-1) || '').replace(/\.html$/, '');
            if (paraId) _removeItem(btn, dropdown, paraId, items);
        });
    }

    // ── Строим dropdown ───────────────────────────────────────────────────
    function _buildDropdown(items, paraMap) {
        const dropdown = document.createElement('div');
        dropdown.className = 'ntf-dropdown';
        dropdown.setAttribute('role', 'menu');

        const count = items.length;
        const label = count === 1 ? 'параграф' : count < 5 ? 'параграфа' : 'параграфов';
        dropdown.innerHTML = `<div class="ntf-dropdown-title">Повторить ${count} ${label}</div>`;

        items.forEach(item => {
            const info = paraMap[item.paragraphId];
            const name = info?.title
                || item.paragraphId.replace(/-/g, ' ').replace(/\b\w/g, c => c.toUpperCase());
            const numLabel = info?.num ? `§${info.num}` : '';
            const chLabel  = info?.chapterTitle || '';
            const subline  = [numLabel, chLabel].filter(Boolean).join(' · ');
            const score = item.wrongCount > 0
                ? `<span class="ntf-item-score ntf-item-score--wrong">${item.wrongCount} ошибок</span>`
                : `<span class="ntf-item-score ntf-item-score--due">повторить</span>`;

            const el = document.createElement('a');
            el.className = 'ntf-item';
            el.href = info?.href || '#';
            el.setAttribute('role', 'menuitem');
            el.innerHTML = `
                <span class="ntf-item-name">
                    <span class="ntf-item-name-text">📄 ${name}</span>
                    ${subline ? `<span class="ntf-item-chapter">${subline}</span>` : ''}
                </span>
                ${score}`;
            dropdown.appendChild(el);
        });

        return dropdown;
    }

    // ── Удаляем параграф из dropdown ─────────────────────────────────────
    function _removeItem(btn, dropdown, paragraphId, items) {
        const idx = items.findIndex(i => i.paragraphId === paragraphId);
        if (idx !== -1) items.splice(idx, 1);

        dropdown.querySelectorAll('.ntf-item').forEach(link => {
            if (link.href?.includes(paragraphId)) link.remove();
        });

        if (items.length === 0) {
            btn.classList.remove('has-items');
            const countEl = btn.querySelector('.ntf-bell-count');
            if (countEl) countEl.style.display = 'none';
            dropdown.classList.remove('ntf-open');
            btn.setAttribute('aria-expanded', 'false');
            btn.style.display = 'none';
            dropdown.remove();
            return;
        }

        const count = items.length;
        const label = count === 1 ? 'параграф' : count < 5 ? 'параграфа' : 'параграфов';
        const title = dropdown.querySelector('.ntf-dropdown-title');
        if (title) title.textContent = `Повторить ${count} ${label}`;

        const countEl = btn.querySelector('.ntf-bell-count');
        if (countEl) countEl.textContent = count;
    }

    // ── Публичный API ─────────────────────────────────────────────────────
    window.ntfRemoveItem = function(paragraphId) {
        const items = window._ntfItems || [];
        document.querySelectorAll('#ntf-bell-btn-mobile, #ntf-bell-btn-desktop').forEach(btn => {
            const dropdown = btn.querySelector('.ntf-dropdown');
            if (dropdown) _removeItem(btn, dropdown, paragraphId, items);
        });
    };

    // Обратная совместимость со старым API
    window.reviewBannerRemoveItem = window.ntfRemoveItem;

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', run);
    } else {
        run();
    }
})();
