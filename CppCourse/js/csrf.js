/**
 * csrf.js — утилита для получения CSRF request token.
 *
 * ASP.NET Core Antiforgery использует пару токенов:
 *   - cookie token (XSRF-TOKEN) — устанавливается сервером автоматически
 *   - request token — должен передаваться в заголовке X-CSRF-Token
 *
 * Это НЕ одно и то же значение. Request token получаем через GET /api/csrf.
 */

let _csrfToken = sessionStorage.getItem('csrf_token') || null;

async function _refreshCsrfToken() {
    try {
        const r = await fetch('/api/csrf', { method: 'GET' });
        if (r.ok) {
            const data = await r.json();
            _csrfToken = data.token || null;
            if (_csrfToken) sessionStorage.setItem('csrf_token', _csrfToken);
        }
    } catch {}
}

function getCookie(name) {
    const match = document.cookie.match(new RegExp('(?:^|;\\s*)' + name + '=([^;]*)'));
    return match ? decodeURIComponent(match[1]) : null;
}

function csrfHeader() {
    return { 'X-CSRF-Token': _csrfToken || '' };
}

// Получаем свежий токен при загрузке страницы
_refreshCsrfToken();
