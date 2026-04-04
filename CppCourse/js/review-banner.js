// ============================================
// REVIEW BANNER — показывает параграфы к повтору
// Вместо баннера-полосы — dropdown под колокольчиком в хедере
// ============================================

(function () {
    function run() {
        const user = JSON.parse(localStorage.getItem('cpp_user') || 'null');
        if (!user?.isuNumber) return;

        const navScript = document.querySelector('script[src*="navigation.js"]');
        const navSrc = navScript?.src || '';
        const m = navSrc.match(/^(.*\/)js\/navigation\.js/);
        const base = m ? m[1] : '/';

        fetch('/api/review/due', {
            headers: { 'X-Isu-Number': user.isuNumber }
        })
        .then(r => r.ok ? r.json() : null)
        .then(async dueItems => {
            if (!dueItems || dueItems.length === 0) {
                _updateBell([], {});
                return;
            }

            let paraMap = {};
            try {
                if (typeof loadCourseStructure === 'function') {
                    const structure = await loadCourseStructure(base);
                    if (structure?.chapters) {
                        let paraNum = 1;
                        for (const ch of structure.chapters) {
                            for (const para of ch.paragraphs || []) {
                                paraMap[para.id] = {
                                    title: para.title,
                                    href: `${base}theory/${ch.id}/${ch.groupId}/${para.id}.html`,
                                    num: paraNum++
                                };
                            }
                        }
                    }
                }
            } catch { /* без названий */ }

            _updateBell(dueItems, paraMap);
        })
        .catch(() => {});
    }

    function _updateBell(items, paraMap) {
        // Ждём пока колокольчик появится в DOM (streak.js рендерит его в bottom-nav)
        const tryAttach = (attempts) => {
            const bell = document.getElementById('bnb-bell-btn');
            if (!bell) {
                if (attempts < 20) setTimeout(() => tryAttach(attempts + 1), 300);
                return;
            }
            _attachDropdown(bell, items, paraMap);
        };
        tryAttach(0);
    }

    function _attachDropdown(bell, items, paraMap) {
        // Удаляем старый dropdown если есть
        document.getElementById('hpm-bell-dropdown')?.remove();

        if (items.length > 0) {
            bell.classList.add('has-items');
            const countEl = document.getElementById('bnb-bell-count');
            if (countEl) { countEl.textContent = items.length; countEl.style.display = ''; }
        } else {
            bell.classList.remove('has-items');
            const countEl = document.getElementById('bnb-bell-count');
            if (countEl) countEl.style.display = 'none';
            return;
        }

        const dropdown = document.createElement('div');
        dropdown.id = 'hpm-bell-dropdown';
        dropdown.className = 'hpm-bell-dropdown';

        const count = items.length;
        const label = count === 1 ? 'параграф' : count < 5 ? 'параграфа' : 'параграфов';
        dropdown.innerHTML = `<div class="hpm-bell-dropdown-title">Повторить ${count} ${label}</div>`;

        items.forEach(item => {
            const info = paraMap[item.paragraphId];
            const name = info?.title || item.paragraphId;
            const numBadge = info?.num ? `<span class="hpm-bell-item-num">§${info.num}</span>` : '';
            const score = item.wrongCount > 0
                ? `<span class="hpm-bell-item-score hpm-bell-item-score--wrong">${item.wrongCount} ошибок</span>`
                : `<span class="hpm-bell-item-score hpm-bell-item-score--due">повторить</span>`;

            const el = document.createElement('a');
            el.className = 'hpm-bell-item';
            el.href = info?.href || '#';
            el.innerHTML = `${numBadge}<span class="hpm-bell-item-name">📄 ${name}</span>${score}`;
            dropdown.appendChild(el);
        });

        // Позиционируем: открывается вверх над bottom-nav
        dropdown.style.top = 'auto';
        dropdown.style.bottom = 'calc(100% + 8px)';
        dropdown.style.left = '0';
        bell.appendChild(dropdown);

        // Закрываем при клике вне
        document.addEventListener('click', (e) => {
            if (!bell.contains(e.target)) {
                dropdown.classList.remove('hpm-bell-open');
            }
        }, { capture: true });

        // Слушаем завершение теста — убираем параграф из списка
        window.addEventListener('quizCompleted', () => {
            // paragraphId берём из URL текущей страницы (как делает helpers.js)
            const parts = location.pathname.replace(/\/$/, '').split('/').filter(Boolean);
            const paraId = (parts.at(-1) || '').replace(/\.html$/, '');
            if (!paraId) return;
            _removeItemFromDropdown(dropdown, bell, paraId, items);
        });
    }

    function _removeItemFromDropdown(dropdown, bell, paragraphId, items) {
        const idx = items.findIndex(i => i.paragraphId === paragraphId);
        if (idx !== -1) items.splice(idx, 1);

        const links = dropdown.querySelectorAll('.hpm-bell-item');
        links.forEach(link => {
            if (link.href && link.href.includes(paragraphId)) link.remove();
        });

        if (items.length === 0) {
            bell.classList.remove('has-items');
            const countEl = document.getElementById('bnb-bell-count');
            if (countEl) countEl.style.display = 'none';
            dropdown.classList.remove('hpm-bell-open');
            dropdown.remove();
            return;
        }

        const count = items.length;
        const label = count === 1 ? 'параграф' : count < 5 ? 'параграфа' : 'параграфов';
        const title = dropdown.querySelector('.hpm-bell-dropdown-title');
        if (title) title.textContent = `Повторить ${count} ${label}`;

        const countEl = document.getElementById('bnb-bell-count');
        if (countEl) countEl.textContent = count;
    }

    window.reviewBannerRemoveItem = function(paragraphId) {
        const items = window._reviewBannerItems || [];
        const dropdown = document.getElementById('hpm-bell-dropdown');
        const bell = document.getElementById('bnb-bell-btn');
        if (dropdown && bell) _removeItemFromDropdown(dropdown, bell, paragraphId, items);
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', run);
    } else {
        run();
    }
})();
