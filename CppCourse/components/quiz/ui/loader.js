// QuizLoader — загрузка теста из JSON-файла

import { Quiz } from './quiz-inline.js';
import { shuffle } from './helpers.js';

export const QuizLoader = {
    async init(containerId, jsonPath) {
        const container = document.getElementById(containerId);
        if (!container) return;

        const loading = document.createElement('div');
        loading.className = 'quiz-loading';
        loading.textContent = '⏳ Загрузка теста…';
        container.replaceChildren(loading);

        let data;
        try {
            const res = await fetch(jsonPath);
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            data = await res.json();
        } catch (e) {
            loading.textContent = '❌ Не удалось загрузить тест';
            loading.style.color = 'var(--accent-red)';
            console.error('[QuizLoader]', jsonPath, e);
            return;
        }

        const wrongIds  = await this._fetchWrongIds(data.quizId);
        const questions = this._pickQuestions(data, wrongIds);
        new Quiz(containerId, questions, {
            quizId: data.quizId, title: data.title,
            type: data.type || 'mini', passingScore: data.passingScore ?? 70,
        });
    },

    async _fetchWrongIds(quizId) {
        try {
            const user = JSON.parse(localStorage.getItem('cpp_user') || 'null');
            if (!user?.isuNumber) return new Set();
            const res = await fetch(`/api/quiz/${quizId}/wrong-ids`, { headers: { 'X-Isu-Number': user.isuNumber } });
            return res.ok ? new Set(await res.json()) : new Set();
        } catch { return new Set(); }
    },

    _pickQuestions(data, wrongIds = new Set()) {
        const all    = data.questions || [];
        const pick   = Math.min(data.pick || all.length, all.length);

        // 1. Unseen — абсолютный приоритет
        const unseen = shuffle(all.filter(q => !wrongIds.has(q.id)));
        // 2. Wrong — не более 50% от оставшихся слотов после unseen
        const wrong  = shuffle(all.filter(q => wrongIds.has(q.id)));

        const result = [];

        // Сначала unseen
        for (const q of unseen) {
            if (result.length >= pick) break;
            result.push(q);
        }

        // Потом wrong (до 50% оставшихся слотов)
        const remaining = pick - result.length;
        const wrongCap  = Math.ceil(remaining * 0.5);
        const wrongAdded = [];
        for (const q of wrong.slice(0, wrongCap)) {
            if (result.length >= pick) break;
            result.push(q);
            wrongAdded.push(q);
        }

        // Если не хватает — добираем оставшимися wrong
        for (const q of wrong.filter(q => !wrongAdded.includes(q))) {
            if (result.length >= pick) break;
            result.push(q);
        }

        return shuffle(result);
    },
};
