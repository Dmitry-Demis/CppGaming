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

        const pickedIds = await this._fetchPickedIds(data.quizId);
        const questions = this._pickQuestions(data, pickedIds);
        new Quiz(containerId, questions, {
            quizId: data.quizId, title: data.title,
            type: data.type || 'mini', passingScore: data.passingScore ?? 70,
        });
    },

    // Запрашивает у бэкенда SR-выборку ID вопросов (unseen → wrong → seenCorrect).
    // Для анонимных пользователей бэкенд вернёт случайную выборку.
    // При ошибке возвращает null — тогда _pickQuestions сделает случайную выборку на клиенте.
    async _fetchPickedIds(quizId) {
        try {
            const user = JSON.parse(localStorage.getItem('cpp_user') || 'null');
            const headers = user?.isuNumber ? { 'X-Isu-Number': user.isuNumber } : {};
            const res = await fetch(`/api/quiz/${quizId}/pick-questions`, { headers });
            return res.ok ? await res.json() : null;
        } catch { return null; }
    },

    _pickQuestions(data, pickedIds = null) {
        const all  = data.questions || [];
        const pick = Math.min(data.pick || all.length, all.length);

        // Если бэкенд вернул упорядоченный список ID — используем его порядок
        if (Array.isArray(pickedIds) && pickedIds.length > 0) {
            const byId = Object.fromEntries(all.map(q => [q.id, q]));
            const ordered = pickedIds.map(id => byId[id]).filter(Boolean);
            return shuffle(ordered);
        }

        // Fallback: случайная выборка (анонимный пользователь или ошибка сети)
        return shuffle(all).slice(0, pick);
    },
};
