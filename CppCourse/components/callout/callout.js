/**
 * Callout Component
 * 
 * Универсальный компонент для отображения информационных блоков (callout).
 * Поддерживает различные типы: info, warning, danger, tip, important, hot, note, quote.
 * 
 * Функциональность:
 * 1. Инициализация .callout блоков с динамической загрузкой SVG иконок
 * 2. Fade-in анимация при появлении в viewport (IntersectionObserver)
 * 
 * @module CalloutComponent
 */
(function () {
    'use strict';

    // ============================================================================
    // КОНСТАНТЫ
    // ============================================================================

    /**
     * Базовый путь к компоненту (определяется автоматически по URL скрипта)
     * @type {string}
     */
    const COMPONENT_PATH = (() => {
        const src = (document.currentScript || {}).src || '';
        const match = src.match(/^(.*)\/callout\.js/);
        return match ? match[1] + '/' : './components/callout/';
    })();

    /**
     * Кэш загруженных SVG иконок
     * @type {Object.<string, string>}
     */
    const iconCache = {};

    // ============================================================================
    // ЗАГРУЗКА ИКОНОК
    // ============================================================================

    /**
     * Загружает SVG иконку из файла
     * Автоматически определяет путь по имени типа: type -> icons/type.svg
     * @param {string} type - Тип callout (info, warning, и т.д.)
     * @returns {Promise<string>} HTML содержимое SVG
     */
    async function loadIcon(type) {
        // Проверяем кэш
        if (iconCache[type]) {
            return iconCache[type];
        }

        // Автоматически формируем путь: icons/{type}.svg
        const iconPath = `icons/${type}.svg`;
        const fullPath = COMPONENT_PATH + iconPath;

        try {
            const response = await fetch(fullPath);
            if (!response.ok) {
                throw new Error(`Failed to load icon: ${fullPath}`);
            }
            const svgContent = await response.text();
            iconCache[type] = svgContent;
            return svgContent;
        } catch (error) {
            console.warn(`[Callout] Failed to load icon for type "${type}":`, error);
            
            // Fallback: пытаемся загрузить иконку info
            if (type !== 'info' && !iconCache.info) {
                return loadIcon('info');
            }
            
            return iconCache.info || '';
        }
    }

    // ============================================================================
    // ИНИЦИАЛИЗАЦИЯ CALLOUT
    // ============================================================================

    /**
     * Инициализирует .callout блок
     * @param {HTMLElement} element - DOM элемент .callout
     */
    async function initCallout(element) {
        // Проверяем, не был ли блок уже инициализирован
        if (element.dataset.calloutUpgraded) {
            return;
        }
        element.dataset.calloutUpgraded = '1';

        // Определяем тип callout из класса
        const type = Array.from(element.classList)
            .find(className => 
                className.startsWith('callout--') && 
                className !== 'callout--visible'
            )
            ?.replace('callout--', '') || 'info';

        const titleElement = element.querySelector('.callout__title');

        // Проверяем, не добавлена ли уже иконка
        if (element.querySelector('.callout__header') || element.querySelector('.callout__icon')) {
            return;
        }

        // Создаем и вставляем иконку
        const iconElement = document.createElement('span');
        iconElement.className = 'callout__icon';
        
        const iconSvg = await loadIcon(type);
        iconElement.innerHTML = iconSvg;

        if (titleElement) {
            // Создаем header и оборачиваем заголовок
            const header = document.createElement('div');
            header.className = 'callout__header';
            header.appendChild(iconElement);
            
            titleElement.parentNode.insertBefore(header, titleElement);
            header.appendChild(titleElement);
        } else {
            // Если нет заголовка, добавляем иконку в начало
            element.prepend(iconElement);
        }
    }

    // ============================================================================
    // АНИМАЦИЯ ПОЯВЛЕНИЯ
    // ============================================================================

    /**
     * Настраивает IntersectionObserver для fade-in анимации
     * Блоки становятся видимыми при появлении в viewport
     */
    function setupFadeInAnimation() {
        // Проверяем поддержку IntersectionObserver
        if (!('IntersectionObserver' in window)) {
            // Fallback: сразу показываем все блоки
            document.querySelectorAll('.callout').forEach(element => {
                element.classList.add('callout--visible');
            });
            return;
        }

        // Создаем observer
        const observer = new IntersectionObserver(
            (entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        // Блок появился в viewport - показываем его
                        entry.target.classList.add('callout--visible');
                        // Прекращаем наблюдение за этим элементом
                        observer.unobserve(entry.target);
                    }
                });
            },
            {
                threshold: 0.1,                      // Триггер при 10% видимости
                rootMargin: '0px 0px -40px 0px'      // Небольшой отступ снизу
            }
        );

        // Наблюдаем за всеми callout блоками
        document.querySelectorAll('.callout').forEach(element => {
            observer.observe(element);
        });
    }

    // ============================================================================
    // ИНИЦИАЛИЗАЦИЯ
    // ============================================================================

    /**
     * Главная функция инициализации компонента
     */
    async function initialize() {
        // Инициализируем все callout блоки
        const callouts = document.querySelectorAll('.callout:not([data-callout-upgraded])');
        for (const element of callouts) {
            await initCallout(element);
        }

        // Настраиваем анимацию появления
        setupFadeInAnimation();
    }

    // ============================================================================
    // ЗАПУСК
    // ============================================================================

    // Запускаем инициализацию после загрузки DOM
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        // DOM уже загружен - запускаем сразу
        initialize();
    }

})();
