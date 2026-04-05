// Auth guard — redirect to login if not authenticated
// Include this script in all protected pages (theory, profile)
(async function () {
    console.log('[AUTH-GUARD] Starting auth check on:', location.href);
    
    const user = JSON.parse(localStorage.getItem('cpp_user') || 'null');
    console.log('[AUTH-GUARD] User from localStorage:', user);

    // Вычисляем корень сайта по URL скрипта
    const src = (document.currentScript || {}).src || '';
    const m = src.match(/^(.*\/)js\/auth-guard\.js/);
    const root = m ? m[1] : '/';
    console.log('[AUTH-GUARD] Root path:', root);

    if (!user || !user.isuNumber) {
        console.log('[AUTH-GUARD] No user or isuNumber, redirecting to login');
        sessionStorage.setItem('cpp_redirect_after_login', location.href);
        location.replace(root + 'login.html');
        return;
    }

    // ВСЕГДА проверяем в БД - не доверяем localStorage
    try {
        const url = `/api/profile/${encodeURIComponent(user.isuNumber)}`;
        console.log('[AUTH-GUARD] Validating user with API:', url);
        console.log('[AUTH-GUARD] Header X-Isu-Number:', user.isuNumber);
        
        const res = await fetch(url, {
            headers: { 'X-Isu-Number': user.isuNumber }
        });
        
        console.log('[AUTH-GUARD] API response status:', res.status);
        
        if (!res.ok) {
            // Любая ошибка (401, 404, 500) = выход
            console.log('[AUTH-GUARD] Validation failed (status ' + res.status + '), clearing localStorage and redirecting');
            
            // Логируем детали ошибки для отладки
            try {
                const errorData = await res.json();
                console.error('[AUTH-GUARD] Error response:', errorData);
            } catch (e) {
                console.error('[AUTH-GUARD] Could not parse error response:', e);
            }
            
            localStorage.removeItem('cpp_user');
            sessionStorage.setItem('cpp_redirect_after_login', location.href);
            location.replace(root + 'login.html');
        } else {
            console.log('[AUTH-GUARD] Validation successful, user is authenticated');
        }
    } catch (err) {
        // Сетевая ошибка - тоже выкидываем на логин
        console.error('[AUTH-GUARD] Network error during validation:', err);
        localStorage.removeItem('cpp_user');
        sessionStorage.setItem('cpp_redirect_after_login', location.href);
        location.replace(root + 'login.html');
    }
}());
