/**
 * csrf.js — утилита для получения CSRF-токена из куки XSRF-TOKEN.
 * Подключается на всех страницах перед другими скриптами.
 *
 * Сервер устанавливает куку XSRF-TOKEN при каждом GET-запросе.
 * Клиент читает её и кладёт в заголовок X-CSRF-Token при POST/PUT/DELETE.
 */

/**
 * Читает значение куки по имени.
 * @param {string} name
 * @returns {string|null}
 */
function getCookie(name) {
    const match = document.cookie.match(new RegExp('(?:^|;\\s*)' + name + '=([^;]*)'));
    return match ? decodeURIComponent(match[1]) : null;
}

/**
 * Возвращает заголовок X-CSRF-Token для fetch-запросов.
 * @returns {{ 'X-CSRF-Token': string }}
 */
function csrfHeader() {
    return { 'X-CSRF-Token': getCookie('XSRF-TOKEN') || '' };
}
