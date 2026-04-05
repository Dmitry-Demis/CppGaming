# Callout Component

Универсальный компонент для отображения информационных блоков различных типов.

## Структура

```
callout/
├── callout.js          # Основной скрипт компонента
├── standalone.html     # Демо-страница со всеми типами
├── css/                # Стили компонента
│   ├── callout.css     # Базовые стили + импорт всех типов
│   └── types/          # Стили для каждого типа
│       ├── info.css
│       ├── warning.css
│       ├── danger.css
│       ├── tip.css
│       ├── important.css
│       ├── hot.css
│       ├── note.css
│       └── quote.css
├── icons/              # SVG иконки для разных типов
│   ├── info.svg
│   ├── warning.svg
│   ├── danger.svg
│   ├── tip.svg
│   ├── important.svg
│   ├── hot.svg
│   ├── note.svg
│   └── quote.svg
└── README.md           # Документация
```

## Использование

### Демо

Откройте `standalone.html` в браузере, чтобы увидеть все типы callout в действии с различными вариантами контента.

### Базовый синтаксис

```html
<div class="callout callout--info">
    <div class="callout__title">Заголовок</div>
    <div class="callout__body">
        <p>Содержимое блока</p>
    </div>
</div>
```

### Типы callout

- `callout--info` - Информационный блок (синий)
- `callout--warning` - Предупреждение (желтый)
- `callout--danger` - Опасность/ошибка (красный)
- `callout--tip` - Совет (зеленый)
- `callout--important` - Важное (фиолетовый)
- `callout--hot` - Горячая тема (оранжевый)
- `callout--note` - Заметка (серый)
- `callout--quote` - Цитата (голубой)

### Примеры

#### С заголовком

```html
<div class="callout callout--warning">
    <div class="callout__title">Внимание!</div>
    <div class="callout__body">
        <p>Это важное предупреждение.</p>
    </div>
</div>
```

#### Без заголовка

```html
<div class="callout callout--tip">
    <div class="callout__body">
        <p>Полезный совет без заголовка.</p>
    </div>
</div>
```

## Функциональность

### 1. Динамическая загрузка иконок

SVG иконки загружаются асинхронно из отдельных файлов и кэшируются для повторного использования.

### 2. Fade-in анимация

Блоки плавно появляются при прокрутке страницы (IntersectionObserver API).

### 3. Автоматическая инициализация

Компонент автоматически находит все `.callout` блоки на странице и добавляет к ним иконки.

## Технические детали

### Инициализация

Компонент автоматически инициализируется после загрузки DOM:

```html
<script src="components/callout/callout.js"></script>
```

### Кэширование иконок

Иконки загружаются один раз и кэшируются в памяти для повышения производительности.

### Поддержка браузеров

- Современные браузеры с поддержкой ES6+
- IntersectionObserver (с fallback для старых браузеров)
- Fetch API для загрузки SVG

## Стилизация

### Подключение стилей

Просто подключите один файл - он автоматически импортирует все типы:

```html
<link rel="stylesheet" href="components/callout/css/callout.css">
```

Файл `callout.css` содержит:
- Базовые стили для всех callout блоков
- Автоматический импорт всех типов из папки `types/`

### CSS переменные

Стили используют CSS переменные для цветов:

```css
/* Базовые стили */
.callout {
    --fs-sm: 0.875rem;
    --text-secondary: #a6adc8;
}

/* Цвета для типов */
--accent-cyan: #38bdf8;      /* info */
--accent-orange: #fb923c;    /* warning */
--accent-red: #f87171;       /* danger */
--accent-green: #a6e3a1;     /* tip */
--accent-purple: #cba6f7;    /* important */
--accent-blue: #60a5fa;      /* note */
--accent-violet: #8b5cf6;    /* quote */
```

## Разработка

### Добавление нового типа

Чтобы добавить новый тип callout:

1. **Создайте SVG иконку** в `icons/` с именем типа:
   ```
   icons/mynewtype.svg
   ```

2. **Создайте CSS файл** в `css/types/`:
   ```css
   /* css/types/mynewtype.css */
   .callout--mynewtype {
       background: rgba(...);
       border-color: rgba(...);
       border-left-color: var(--accent-color);
   }
   
   .callout--mynewtype .callout__icon svg {
       color: var(--accent-color);
   }
   
   .callout--mynewtype .callout__title {
       color: var(--accent-color);
   }
   ```

3. **Добавьте импорт** в `css/callout.css`:
   ```css
   @import url('./types/mynewtype.css');
   ```

4. **Готово!** Используйте новый тип:
   ```html
   <div class="callout callout--mynewtype">
       <div class="callout__title">Заголовок</div>
       <div class="callout__body">
           <p>Содержимое</p>
       </div>
   </div>
   ```

JavaScript автоматически найдет иконку по имени типа (`icons/{type}.svg`).

### Отладка

Компонент выводит предупреждения в консоль при ошибках загрузки иконок.
