#!/usr/bin/env node
/**
 * build-course-meta.js
 * Пробегает по всем параграфам в course-meta.json,
 * вытаскивает <title> из соответствующих HTML-файлов
 * и записывает title прямо в JSON.
 *
 * Запуск: node scripts/build-course-meta.js
 */

const fs   = require('fs');
const path = require('path');

const ROOT      = path.resolve(__dirname, '..');
const META_PATH = path.join(ROOT, 'CppCourse', 'course-meta.json');
const THEORY    = path.join(ROOT, 'CppCourse', 'theory');

const meta = JSON.parse(fs.readFileSync(META_PATH, 'utf8'));

let updated = 0;
let missing = 0;

for (const chapter of meta.chapters) {
    const paras = chapter.paragraphs || [];
    chapter.paragraphs = paras.map((p, i) => {
        // Нормализуем: строка → объект
        const obj = typeof p === 'string' ? { id: p } : { ...p };

        // Если тайтл уже есть — не трогаем
        if (obj.title) return obj;

        const htmlPath = path.join(THEORY, chapter.id, chapter.groupId, obj.id + '.html');
        if (!fs.existsSync(htmlPath)) {
            console.warn(`  [MISS] ${htmlPath}`);
            missing++;
            return obj;
        }

        const html  = fs.readFileSync(htmlPath, 'utf8');
        const match = html.match(/<title>([\s\S]*?)<\/title>/i);
        if (!match) {
            console.warn(`  [NO TITLE] ${htmlPath}`);
            missing++;
            return obj;
        }

        // Убираем суффикс после — или –
        const title = match[1].replace(/\s*[—–].*$/, '').trim();
        console.log(`  [OK] ${obj.id} → "${title}"`);
        obj.title = title;
        updated++;
        return obj;
    });
}

fs.writeFileSync(META_PATH, JSON.stringify(meta, null, 2), 'utf8');
console.log(`\nГотово: обновлено ${updated}, не найдено ${missing}`);
