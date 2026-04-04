// ============================================
// STREAK MODAL + HEADER PROFILE MINI-CARD
// ============================================
(function () {
    'use strict';

    const FREEZE_MILESTONES = [3, 7, 14, 21, 35, 56, 84, 126, 189, 280, 420, 630];

    function nextMilestone(total) {
        return FREEZE_MILESTONES.find(m => m > total) ?? null;
    }

    // ── CSS ──────────────────────────────────────────────────────────────
    const CSS = `
/* ── Streak Modal ── */
.streak-overlay {
    position: fixed; inset: 0;
    background: rgba(0,0,0,.65);
    z-index: 1100;
    display: flex; align-items: center; justify-content: center;
    padding: 1rem;
    animation: streak-fade-in .25s ease both;
}
@keyframes streak-fade-in { from { opacity:0 } to { opacity:1 } }

.streak-modal {
    background: linear-gradient(160deg, #1e1e2e 0%, #181825 100%);
    border: 1px solid rgba(250,179,135,.25);
    border-radius: 20px;
    padding: 2rem 2.25rem 1.75rem;
    max-width: 360px; width: 100%;
    box-shadow: 0 24px 64px rgba(0,0,0,.6), 0 0 0 1px rgba(250,179,135,.08);
    text-align: center;
    animation: streak-pop .4s cubic-bezier(.34,1.56,.64,1) both;
    position: relative;
}
@keyframes streak-pop {
    from { opacity:0; transform: scale(.88) translateY(16px); }
    to   { opacity:1; transform: scale(1)   translateY(0); }
}

.streak-modal__fire {
    font-size: 3.5rem;
    display: block;
    margin-bottom: .5rem;
    animation: fire-pulse 1.2s ease-in-out infinite alternate;
    filter: drop-shadow(0 0 12px rgba(250,179,135,.8));
}
@keyframes fire-pulse {
    from { transform: scale(1)   rotate(-4deg); filter: drop-shadow(0 0 8px rgba(250,179,135,.6)); }
    to   { transform: scale(1.1) rotate(4deg);  filter: drop-shadow(0 0 18px rgba(250,179,135,1)); }
}
.streak-modal__fire--frozen {
    animation: none;
    filter: drop-shadow(0 0 12px rgba(137,180,250,.8));
}

.streak-modal__count {
    font-size: 3rem; font-weight: 900;
    color: #fab387;
    font-family: 'JetBrains Mono', monospace;
    line-height: 1;
    margin-bottom: .2rem;
}
.streak-modal__label {
    font-size: .8rem; color: rgba(205,214,244,.5);
    margin-bottom: 1.25rem;
    text-transform: uppercase; letter-spacing: .06em;
}

.streak-modal__rewards {
    display: flex; gap: .75rem; justify-content: center;
    margin-bottom: 1.25rem; flex-wrap: wrap;
}
.streak-reward-badge {
    display: inline-flex; align-items: center; gap: 5px;
    font-size: .8rem; font-weight: 700;
    padding: 5px 12px; border-radius: 8px;
}
.streak-reward-badge--coins {
    background: rgba(249,226,175,.12);
    color: #f9e2af;
    border: 1px solid rgba(249,226,175,.25);
}
.streak-reward-badge--xp {
    background: rgba(166,227,161,.1);
    color: #a6e3a1;
    border: 1px solid rgba(166,227,161,.22);
}
.streak-reward-badge--milestone {
    background: rgba(203,166,247,.12);
    color: #cba6f7;
    border: 1px solid rgba(203,166,247,.25);
    font-size: .72rem;
}

.streak-modal__notice {
    font-size: .78rem; line-height: 1.5;
    padding: 8px 12px; border-radius: 8px;
    margin-bottom: 1.25rem;
}
.streak-modal__notice--freeze {
    background: rgba(137,180,250,.1);
    border: 1px solid rgba(137,180,250,.22);
    color: #89b4fa;
}
.streak-modal__notice--broken {
    background: rgba(243,139,168,.1);
    border: 1px solid rgba(243,139,168,.22);
    color: #f38ba8;
}

.streak-modal__meta {
    font-size: .72rem; color: rgba(205,214,244,.35);
    margin-bottom: 1.25rem;
}
.streak-modal__meta span { color: #f9e2af; font-weight: 700; }

.streak-modal__badges {
    display: flex; gap: 6px; justify-content: center;
    flex-wrap: wrap; margin-bottom: 1.25rem;
}
.streak-sm-badge {
    font-size: .65rem; font-weight: 700;
    padding: 2px 8px; border-radius: 5px;
}
.streak-sm-badge--freeze {
    background: rgba(137,180,250,.15); color: #89b4fa;
    border: 1px solid rgba(137,180,250,.3);
}
.streak-sm-badge--snow {
    background: rgba(148,226,213,.12); color: #94e2d5;
    border: 1px solid rgba(148,226,213,.25);
}

.streak-modal__btn {
    width: 100%;
    padding: .7rem 1.5rem;
    background: linear-gradient(135deg, #fab387 0%, #f9e2af 100%);
    color: #1e1e2e;
    border: none; border-radius: 10px;
    font-size: .95rem; font-weight: 800;
    cursor: pointer;
    transition: opacity .15s, transform .15s;
}
.streak-modal__btn:hover { opacity: .9; transform: translateY(-1px); }
.streak-modal__btn:active { transform: translateY(0); }

/* ── Header Mini Profile ── */
.header-profile-mini {
    display: inline-flex; align-items: center; gap: 7px;
    background: rgba(255,255,255,.05);
    border: 1px solid rgba(255,255,255,.1);
    border-radius: 9px;
    padding: 4px 10px 4px 5px;
    text-decoration: none;
    color: var(--text-primary, #cdd6f4);
    transition: background .15s, border-color .15s;
    cursor: pointer;
    flex-shrink: 0;
    max-height: 36px;
}
.header-profile-mini:hover {
    background: rgba(255,255,255,.09);
    border-color: rgba(203,166,247,.4);
    text-decoration: none;
    color: var(--text-primary, #cdd6f4);
}
.hpm-avatar {
    width: 26px; height: 26px;
    border-radius: 6px;
    background: linear-gradient(135deg, #cba6f7, #89b4fa);
    display: flex; align-items: center; justify-content: center;
    font-size: .95rem; flex-shrink: 0;
}
/* Single-row layout */
.hpm-info {
    display: flex; align-items: center; gap: 6px;
    white-space: nowrap;
}
.hpm-name { font-size: .78rem; font-weight: 700; line-height: 1; }
.hpm-level-badge {
    font-size: .62rem; font-weight: 700;
    background: rgba(203,166,247,.15);
    color: #cba6f7;
    border: 1px solid rgba(203,166,247,.3);
    border-radius: 4px;
    padding: 1px 5px;
}
.hpm-xp-bar-wrap { display: flex; align-items: center; gap: 4px; }
.hpm-xp-bar {
    width: 50px; height: 3px;
    background: rgba(255,255,255,.1);
    border-radius: 99px; overflow: hidden;
}
.hpm-xp-fill {
    height: 100%;
    background: linear-gradient(90deg, #cba6f7, #89b4fa);
    border-radius: 99px;
    transition: width .4s;
}
.hpm-xp-val { font-size: .6rem; color: #a78bfa; font-weight: 700; }
.hpm-coins  { font-size: .68rem; font-weight: 700; color: #fde68a; }
.hpm-keys   { font-size: .68rem; font-weight: 700; color: rgba(205,214,244,.7); }
.hpm-streak { font-size: .68rem; font-weight: 700; color: #fab387; }
/* hide on small screens */
@media (max-width: 900px) {
    .hpm-xp-bar-wrap { display: none; }
    .hpm-xp-val { display: none; }
}
@media (max-width: 640px) {
    .hpm-name { display: none; }
    .hpm-keys { display: none; }
}
@media (max-width: 480px) {
    .streak-modal { padding: 1.5rem 1.25rem 1.25rem; }
    .streak-modal__count { font-size: 2.4rem; }
    .header-profile-mini { display: none; }
}

/* ── Header Auth Links ── */
.header-auth-link {
    font-size: .82rem;
    font-weight: 600;
    color: var(--text-secondary, rgba(205,214,244,.7));
    text-decoration: none;
    padding: 4px 10px;
    border-radius: 6px;
    border: 1px solid rgba(255,255,255,.1);
    transition: background .15s, color .15s, border-color .15s;
    white-space: nowrap;
}
.header-auth-link:hover {
    background: rgba(255,255,255,.07);
    color: var(--text-primary, #cdd6f4);
    border-color: rgba(203,166,247,.35);
    text-decoration: none;
}
@media (max-width: 480px) { .header-auth-link { display: none; } }

/* ── Bottom Nav Bar ── */
#bottom-nav-bar {
    position: fixed;
    bottom: 0; left: 0; right: 0;
    z-index: 900;
    display: flex;
    justify-content: center;
    gap: 12px;
    padding: 8px 16px 10px;
    background: rgba(15,14,30,.92);
    backdrop-filter: blur(12px);
    -webkit-backdrop-filter: blur(12px);
    border-top: 1px solid rgba(255,255,255,.08);
    pointer-events: auto;
}
.bnb-btn {
    display: inline-flex; align-items: center; gap: 6px;
    padding: 7px 18px;
    border-radius: 99px;
    font-size: .82rem; font-weight: 700;
    color: rgba(205,214,244,.7);
    background: rgba(255,255,255,.05);
    border: 1px solid rgba(255,255,255,.1);
    text-decoration: none;
    transition: background .15s, color .15s, border-color .15s;
    white-space: nowrap;
}
.bnb-btn:hover {
    background: rgba(203,166,247,.12);
    color: #cba6f7;
    border-color: rgba(203,166,247,.35);
    text-decoration: none;
}
.bnb-btn--active {
    background: rgba(203,166,247,.15);
    color: #cba6f7;
    border-color: rgba(203,166,247,.4);
}
/* Add bottom padding to body so content isn't hidden behind bar */
body { padding-bottom: 52px; }`;

    function injectStyle() {
        if (document.getElementById('streak-css')) return;
        const s = document.createElement('style');
        s.id = 'streak-css';
        s.textContent = CSS;
        document.head.appendChild(s);
    }

    // ── Header mini profile ───────────────────────────────────────────────
    function updateHeaderAuthLinks(loggedIn) {
        const nav = document.querySelector('.header-nav');
        if (!nav) return;

        // Убираем старые auth-ссылки
        nav.querySelectorAll('.header-auth-link').forEach(el => el.remove());

        const root = (typeof _siteRoot !== 'undefined' ? _siteRoot : '');

        if (loggedIn) {
            // Выйти (профиль доступен через плашку)
            const logoutLink = document.createElement('a');
            logoutLink.href = '#';
            logoutLink.className = 'header-auth-link';
            logoutLink.textContent = 'Выйти';
            logoutLink.addEventListener('click', e => {
                e.preventDefault();
                localStorage.removeItem('cpp_user');
                location.href = root + 'login.html';
            });

            nav.appendChild(logoutLink);
        } else {
            const regLink = document.createElement('a');
            regLink.href = root + 'register.html';
            regLink.className = 'header-auth-link';
            regLink.textContent = 'Регистрация';

            const loginLink = document.createElement('a');
            loginLink.href = root + 'login.html';
            loginLink.className = 'header-auth-link';
            loginLink.textContent = 'Вход';

            nav.appendChild(regLink);
            nav.appendChild(loginLink);
        }
    }

    function renderHeaderProfile(profile) {
        // Remove old
        document.querySelectorAll('.header-profile-mini').forEach(el => el.remove());
        document.querySelectorAll('.header-profile-link').forEach(el => el.remove());

        const LEVEL_ICONS  = ['','🌱','📘','⚙️','💻','🔬','👑','🧠','🚀','⚡','🎯',
                              '🏆','💎','🔮','🌟','🦾','🧬','🔥','💡','🎖️','🛸',
                              '🌌','⚔️','🧿','🏅','🎓','🦅','👾','🤖'];
        const LEVEL_TITLES = ['','Новичок','Ученик','Практикант','Программист','Эксперт',
                              'Мастер C++','Архитектор','Хакер','Ниндзя кода','Снайпер',
                              'Чемпион','Алмазный','Оракул','Звезда','Киборг',
                              'Генетик','Пирокод','Гений','Ветеран','Космонавт',
                              'Галактик','Рыцарь','Провидец','Легенда','Профессор',
                              'Орёл','Призрак','Терминатор'];
        const level   = profile.level  ?? 1;
        const xp      = profile.xp     ?? 0;
        const coins   = profile.coins  ?? 0;
        const keys    = profile.keys   ?? 0;
        const streak  = profile.currentStreak ?? 0;
        const needed  = 500;
        const inLevel = xp - (level - 1) * needed;
        const xpNext  = level * needed;
        const pct     = Math.min(xp / xpNext * 100, 100).toFixed(1);
        const icon    = LEVEL_ICONS[Math.min(level, LEVEL_ICONS.length - 1)] || '⭐';
        const title   = LEVEL_TITLES[Math.min(level, LEVEL_TITLES.length - 1)] || '';

        const mini = document.createElement('a');
        mini.href = (typeof _siteRoot !== 'undefined' ? _siteRoot : '') + 'profile.html';
        mini.className = 'header-profile-mini';
        mini.title = `${profile.firstName} ${profile.lastName} · Ур. ${level} · ${xp} XP`;
        mini.innerHTML = `
            <div class="hpm-avatar">${icon}</div>
            <div class="hpm-info">
                <span class="hpm-name">${profile.firstName} ${profile.lastName}</span>
                <span class="hpm-level-badge">Ур.${level} · ${title}</span>
                <div class="hpm-xp-bar-wrap">
                    <div class="hpm-xp-bar"><div class="hpm-xp-fill" style="width:${pct}%"></div></div>
                    <span class="hpm-xp-val">${xp} / ${xpNext} XP</span>
                </div>
                <span class="hpm-coins"><span class="coin-spin">🪙</span> ${coins}</span>
                ${streak > 0 ? `<span class="hpm-streak"><span class="fire-flicker">🔥</span> ${streak}</span>` : ''}
            </div>`;

        // Insert into header-inner, centered between logo and nav
        const inner = document.querySelector('.header-inner');
        if (!inner) return;
        // Remove existing
        inner.querySelectorAll('.header-profile-mini').forEach(el => el.remove());
        mini.style.cssText = 'position:absolute;left:50%;transform:translateX(-50%);';
        inner.style.position = 'relative';
        inner.appendChild(mini);
    }
    // Экспортируем глобально чтобы gamification.js мог вызвать после загрузки данных
    window.renderHeaderProfile = renderHeaderProfile;

    // ── Streak Modal ──────────────────────────────────────────────────────
    function renderModal(data, user) {
        injectStyle();

        const { currentStreak, freezeCount, snowflakeCount, usedFreeze, streakBroken,
                coinsReward, xpReward } = data;

        const isMilestone = currentStreak > 0 && currentStreak % 7 === 0;
        const fireEmoji = usedFreeze ? '🧊' : '🔥';
        const fireClass = `streak-modal__fire${usedFreeze ? ' streak-modal__fire--frozen' : ''}`;

        const totalDays = data.totalStreakDays ?? currentStreak;
        const next = nextMilestone(totalDays);
        const daysToNext = next ? next - totalDays : null;

        // Rewards row
        let rewardsHtml = '';
        if (coinsReward > 0 || xpReward > 0) {
            rewardsHtml = `<div class="streak-modal__rewards">`;
            if (coinsReward > 0) rewardsHtml += `<span class="streak-reward-badge streak-reward-badge--coins">🪙 +${coinsReward} монет</span>`;
            if (xpReward > 0)    rewardsHtml += `<span class="streak-reward-badge streak-reward-badge--xp">⭐ +${xpReward} XP</span>`;
            if (isMilestone)     rewardsHtml += `<span class="streak-reward-badge streak-reward-badge--milestone">🎉 ${currentStreak} дней!</span>`;
            rewardsHtml += `</div>`;
        }

        // Notice
        let noticeHtml = '';
        if (streakBroken) {
            noticeHtml = `<div class="streak-modal__notice streak-modal__notice--broken">💔 Стрик прерван — начинаем заново с 1 дня</div>`;
        } else if (usedFreeze) {
            noticeHtml = `<div class="streak-modal__notice streak-modal__notice--freeze">🧊 Заморозка использована — стрик сохранён!</div>`;
        }

        // Badges
        let badgesHtml = '';
        if (freezeCount > 0 || snowflakeCount > 0) {
            badgesHtml = `<div class="streak-modal__badges">`;
            if (freezeCount > 0)   badgesHtml += `<span class="streak-sm-badge streak-sm-badge--freeze">🧊 ×${freezeCount} заморозки</span>`;
            if (snowflakeCount > 0) badgesHtml += `<span class="streak-sm-badge streak-sm-badge--snow">❄️ ×${snowflakeCount} использовано</span>`;
            badgesHtml += `</div>`;
        }

        // Meta
        let metaHtml = '';
        if (daysToNext !== null) {
            metaHtml = `<div class="streak-modal__meta">До следующей заморозки: <span>${daysToNext} дн.</span></div>`;
        }

        const overlay = document.createElement('div');
        overlay.className = 'streak-overlay';
        overlay.id = 'streak-overlay';
        overlay.innerHTML = `
            <div class="streak-modal" role="dialog" aria-modal="true">
                <span class="${fireClass}">${fireEmoji}</span>
                <div class="streak-modal__count">${currentStreak}</div>
                <div class="streak-modal__label">дней подряд</div>
                ${rewardsHtml}
                ${noticeHtml}
                ${badgesHtml}
                ${metaHtml}
                <button class="streak-modal__btn" id="streak-accept-btn">Принять 🎯</button>
            </div>`;

        document.body.appendChild(overlay);

        const close = () => {
            overlay.style.animation = 'streak-fade-in .2s reverse both';
            setTimeout(() => overlay.remove(), 200);
            // Reload fresh data from server
            if (typeof window.renderHeaderProfile === 'function') {
                const user = JSON.parse(localStorage.getItem('cpp_user') || 'null');
                if (user?.isuNumber) {
                    fetch(`/api/profile/${user.isuNumber}`)
                        .then(r => r.ok ? r.json() : null)
                        .then(p => { if (p) window.renderHeaderProfile(p); })
                        .catch(() => {});
                }
            }
        };

        document.getElementById('streak-accept-btn').addEventListener('click', close);
        overlay.addEventListener('click', e => { if (e.target === overlay) close(); });
        document.addEventListener('keydown', function esc(e) {
            if (e.key === 'Escape') { close(); document.removeEventListener('keydown', esc); }
        });
    }

    // ── Admin Quiz List Modal ─────────────────────────────────────────────
    async function renderAdminQuizList() {
        let quizzes = [];
        try {
            const res = await fetch('/api/quiz/list');
            if (res.ok) quizzes = await res.json();
        } catch { /* offline */ }

        if (!quizzes.length) return;

        injectStyle();

        const overlay = document.createElement('div');
        overlay.className = 'streak-overlay';
        overlay.id = 'admin-quiz-overlay';
        overlay.style.cssText = 'z-index:10000;';

        const typeLabel = { mini: '🔹 Мини', paragraph: '⭐ Итог', standard: '📋 Стандарт', chapter: '🏆 Глава', final: '🏆 Финал' };

        const rows = quizzes.map(q => {
            const lbl = typeLabel[q.type] || q.type;
            const cnt = q.pick > 0 && q.pick < q.total ? `${q.pick}/${q.total}` : `${q.total}`;
            return `<tr style="border-bottom:1px solid rgba(255,255,255,.06);cursor:pointer" onclick="window.openQuizModal('${q.id}');document.getElementById('admin-quiz-overlay')?.remove();document.body.style.overflow=''">
                <td style="padding:6px 10px;font-size:.8rem;color:#94a3b8;font-family:monospace">${q.id}</td>
                <td style="padding:6px 10px;font-size:.85rem">${q.title}</td>
                <td style="padding:6px 10px;font-size:.75rem;white-space:nowrap">${lbl}</td>
                <td style="padding:6px 10px;font-size:.75rem;color:#94a3b8;text-align:right">${cnt} вопр.</td>
            </tr>`;
        }).join('');

        overlay.innerHTML = `
            <div class="streak-modal" role="dialog" aria-modal="true" style="max-width:720px;width:95vw;max-height:80vh;overflow:hidden;display:flex;flex-direction:column;padding:1.5rem">
                <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:1rem">
                    <div style="font-size:1.1rem;font-weight:700">📋 Все тесты курса <span style="font-size:.8rem;color:#94a3b8;font-weight:400">(${quizzes.length})</span></div>
                    <button id="admin-quiz-close" style="background:none;border:none;color:#94a3b8;font-size:1.2rem;cursor:pointer;padding:4px 8px">✕</button>
                </div>
                <div style="overflow-y:auto;flex:1">
                    <table style="width:100%;border-collapse:collapse">
                        <thead><tr style="border-bottom:1px solid rgba(255,255,255,.12)">
                            <th style="padding:4px 10px;text-align:left;font-size:.7rem;color:#64748b;font-weight:600">ID</th>
                            <th style="padding:4px 10px;text-align:left;font-size:.7rem;color:#64748b;font-weight:600">Название</th>
                            <th style="padding:4px 10px;text-align:left;font-size:.7rem;color:#64748b;font-weight:600">Тип</th>
                            <th style="padding:4px 10px;text-align:right;font-size:.7rem;color:#64748b;font-weight:600">Вопросы</th>
                        </tr></thead>
                        <tbody>${rows}</tbody>
                    </table>
                </div>
            </div>`;

        document.body.appendChild(overlay);
        document.body.style.overflow = 'hidden';

        const close = () => {
            overlay.remove();
            document.body.style.overflow = '';
        };
        document.getElementById('admin-quiz-close').addEventListener('click', close);
        overlay.addEventListener('click', e => { if (e.target === overlay) close(); });
        document.addEventListener('keydown', function esc(e) {
            if (e.key === 'Escape') { close(); document.removeEventListener('keydown', esc); }
        });
    }

    // ── Init ──────────────────────────────────────────────────────────────
    // ── Bottom nav bar (Achievements + Leaderboard) ──────────────────────
    function injectBottomNav() {
        if (document.getElementById('bottom-nav-bar')) return;
        if (location.pathname.includes('leaderboard')) return;
        const root = (typeof _siteRoot !== 'undefined' ? _siteRoot : '');
        const bar = document.createElement('div');
        bar.id = 'bottom-nav-bar';
        bar.innerHTML = `
            <a href="${root}achievements.html" class="bnb-btn" id="bnb-achievements">🏆 Достижения</a>
            <a href="${root}shop.html"         class="bnb-btn" id="bnb-shop">🛒 Магазин</a>
            <a href="${root}leaderboard.html"  class="bnb-btn" id="bnb-leaderboard">🥇 Лидеры</a>`;
        document.body.appendChild(bar);

        // Highlight active
        const path = location.pathname;
        if (path.includes('achievements')) document.getElementById('bnb-achievements')?.classList.add('bnb-btn--active');
        if (path.includes('shop'))         document.getElementById('bnb-shop')?.classList.add('bnb-btn--active');
        if (path.includes('leaderboard'))  document.getElementById('bnb-leaderboard')?.classList.add('bnb-btn--active');
    }

    async function init() {
        injectStyle();
        injectBottomNav();
        // Убираем ссылку «Главная» из header-nav — логотип уже ведёт на главную
        document.querySelectorAll('.header-nav a').forEach(a => {
            if (a.textContent.trim() === 'Главная') a.remove();
        });
        const user = JSON.parse(localStorage.getItem('cpp_user') || 'null');
        if (!user?.isuNumber) {
            updateHeaderAuthLinks(false);
            return;
        }

        // Validate session against server — no localStorage fallback
        let profile;
        try {
            const profileRes = await fetch(`/api/profile/${user.isuNumber}`, {
                headers: { 'X-Isu-Number': user.isuNumber }
            });
            if (!profileRes.ok) {
                localStorage.removeItem('cpp_user');
                updateHeaderAuthLinks(false);
                return;
            }
            profile = await profileRes.json();
        } catch {
            return; // offline — show nothing
        }

        renderHeaderProfile(profile);
        updateHeaderAuthLinks(true);

        if (user?.isAdmin) return;

        // Check streak once per calendar day
        const sessionKey = `streak_checked_${new Date().toDateString()}`;
        if (sessionStorage.getItem(sessionKey)) return;
        sessionStorage.setItem(sessionKey, '1');

        try {
            const res = await fetch(`/api/streak/${profile.isuNumber}`);
            if (!res.ok) return;
            const data = await res.json();

            // Only show modal if there's something to show
            const hasReward = (data.coinsReward ?? 0) > 0 || (data.xpReward ?? 0) > 0;
            const hasEvent  = data.usedFreeze || data.streakBroken;
            if (data.currentStreak > 0 && (hasReward || hasEvent)) {
                renderModal(data, profile);
            }
        } catch { /* offline */ }
    }

    // Guard: prevent double init if script loaded twice
    if (window._streakInitDone) return;
    window._streakInitDone = true;

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        setTimeout(init, 400);
    }
})();
