// class ModalQuiz — тест внутри модального окна

import { get } from './templates.js';
import { buildNodes, attachListeners, submit } from './dispatcher.js';
import { quizMd, safeQuizMd, shuffle, shuffleAnswers, typeLabel, resultMood, rewardParts, saveResult, highlightCode } from './helpers.js';

function buildModalQuestions(data, pickedIds = [], isAdmin = false) {
    const all  = data.questions || [];
    const byId = Object.fromEntries(all.map(q => [String(q.id), q]));
    const ordered = pickedIds.length
        ? pickedIds.map(id => byId[String(id)]).filter(Boolean)
        : shuffle(all).slice(0, Math.min(data.pick || all.length, all.length));
    const questions = (ordered.length ? ordered : shuffle(all).slice(0, Math.min(data.pick || all.length, all.length)));
    // Для admin — не перемешиваем ни порядок вопросов, ни варианты ответов
    if (isAdmin) return questions;
    return shuffle(questions).map(q => shuffleAnswers(q, shuffle));
}

export class ModalQuiz {
    constructor(container, data, pickedIds = [], opts = {}) {
        this.container    = container;
        this.quizId       = data.quizId || data.id;
        this.title        = data.title;
        this.passingScore = data.passingScore ?? 70;
        this.isAdmin      = !!opts.isAdmin;
        this.questions    = buildModalQuestions(data, pickedIds, this.isAdmin);
        this.current      = 0;
        this.answers      = {};
        this.answered     = false;
        if (this.isAdmin) {
            this._renderAdmin();
        } else {
            this._render();
        }
    }

    _renderAdmin() {
        const esc = s => String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');

        const correctLabel = q => {
            if (q.type === 'single' || q.type === 'code') {
                return `<code>${esc(q.answers[q.correct])}</code>`;
            }
            if (q.type === 'multiple') {
                return q.correct.map(i => `<code>${esc(q.answers[i])}</code>`).join(', ');
            }
            if (q.type === 'fill') {
                const arr = Array.isArray(q.correct) ? q.correct : [q.correct];
                return arr.map(v => `<code>${esc(v)}</code>`).join(' / ');
            }
            if (q.type === 'matching') {
                return q.pairs.map(p => `<code>${esc(p.left)}</code> → <code>${esc(p.right)}</code>`).join(', ');
            }
            if (q.type === 'fill-code' || q.type === 'fill-code-drag') {
                const arr = Array.isArray(q.correct) ? q.correct : [q.correct];
                return arr.map(v => `<code>${esc(v)}</code>`).join(', ');
            }
            return '—';
        };

        // Шапка
        const header = document.createElement('div');
        header.style.cssText = 'padding:12px 48px 8px 16px;border-bottom:1px solid var(--border-primary);display:flex;align-items:center;gap:8px;position:sticky;top:0;z-index:2;background:var(--bg-card,#1e1c3a)';
        header.innerHTML = `
            <span style="font-size:.7rem;background:var(--accent-orange);color:#000;padding:2px 8px;border-radius:4px;font-weight:700">ADMIN</span>
            <span style="font-weight:700">${esc(this.title)}</span>
            <span style="color:var(--text-muted);font-size:.8rem;margin-left:auto">${this.questions.length} вопросов</span>`;

        // Скроллируемый список вопросов
        const scroll = document.createElement('div');
        scroll.style.cssText = 'padding:12px 16px;display:flex;flex-direction:column;gap:20px';

        this.questions.forEach((q, i) => {
            const card = document.createElement('div');
            card.style.cssText = 'border:1px solid var(--border-primary);border-radius:8px;overflow:hidden';

            // Заголовок карточки
            const cardHead = document.createElement('div');
            cardHead.style.cssText = 'display:flex;align-items:center;gap:8px;padding:8px 12px;background:var(--bg-secondary);border-bottom:1px solid var(--border-primary)';
            cardHead.innerHTML = `
                <span style="color:var(--text-muted);font-size:.72rem">#${q.id}</span>
                <span class="qm-type-badge qm-type--${q.type}" style="font-size:.7rem">${esc(typeLabel(q.type))}</span>
                <span style="color:var(--text-muted);font-size:.72rem;margin-left:auto">${i + 1} / ${this.questions.length}</span>`;

            // Тело карточки
            const cardBody = document.createElement('div');
            cardBody.style.cssText = 'padding:12px';

            const questionEl = document.createElement('div');
            questionEl.className = 'qm-question';
            questionEl.innerHTML = safeQuizMd(q.question);

            cardBody.appendChild(questionEl);

            if (q.code && !['fill-code', 'fill-code-drag'].includes(q.type)) {
                const pre = document.createElement('pre');
                pre.className = 'qm-code';
                pre.style.marginTop = '8px';
                const code = document.createElement('code');
                code.className = 'language-cpp';
                code.textContent = q.code;
                pre.appendChild(code);
                cardBody.appendChild(pre);
            }

            // Ответы через buildNodes (интерактивные, но disabled)
            const answersEl = document.createElement('div');
            answersEl.className = 'qm-answers';
            answersEl.id = `qm-answers-admin-${q.id}`;
            answersEl.style.marginTop = '10px';
            answersEl.replaceChildren(...buildNodes(q, this.quizId));
            // Отключаем интерактивность
            answersEl.querySelectorAll('input, button, [draggable]').forEach(el => {
                el.disabled = true;
                el.setAttribute('tabindex', '-1');
                if (el.draggable) el.draggable = false;
            });
            cardBody.appendChild(answersEl);

            // Правильный ответ
            const answerBadge = document.createElement('div');
            answerBadge.style.cssText = 'margin-top:10px;padding:6px 10px;background:color-mix(in srgb,var(--accent-green) 12%,transparent);border-left:3px solid var(--accent-green);border-radius:0 4px 4px 0;font-size:.82rem;color:var(--accent-green)';
            answerBadge.innerHTML = `✅ Правильный ответ: ${correctLabel(q)}`;
            cardBody.appendChild(answerBadge);

            // Пояснение
            if (q.explanation) {
                const expEl = document.createElement('div');
                expEl.style.cssText = 'margin-top:6px;padding:6px 10px;background:color-mix(in srgb,var(--accent-blue) 10%,transparent);border-left:3px solid var(--accent-blue);border-radius:0 4px 4px 0;font-size:.82rem;color:var(--text-secondary)';
                expEl.innerHTML = `💡 ${safeQuizMd(q.explanation)}`;
                cardBody.appendChild(expEl);
            }

            card.append(cardHead, cardBody);
            scroll.appendChild(card);
        });

        // В admin-режиме убираем padding у box — шапка прилипает к краю
        const box = this.container.closest('.quiz-modal-box');
        if (box) {
            box.style.padding = '0';
            box.dataset.adminMode = '1';
        }

        this.container.replaceChildren(header, scroll);

        highlightCode(this.container);
    }

    _render() {
        if (this.current >= this.questions.length) { this._showResults(); return; }

        const q         = this.questions[this.current];
        const pct       = Math.round(this.current / this.questions.length * 100);
        const earnedPts = Object.values(this.answers).reduce((s, a) => s + (a.pts ?? 0), 0);
        this.answered   = false;

        const tpl = get('tpl-qm-question');
        if (!tpl) { console.warn('[ModalQuiz] tpl-qm-question not found'); return; }

        const node = tpl.content.cloneNode(true);
        node.querySelector('.qm-title').textContent         = this.title;
        node.querySelector('.qm-counter').textContent       = `${this.current + 1} / ${this.questions.length}`;
        node.querySelector('.qm-progress-fill').style.width = pct + '%';
        node.querySelector('.qm-type-badge').textContent    = typeLabel(q.type);
        node.querySelector('.qm-type-badge').className      = `qm-type-badge qm-type--${q.type}`;
        node.querySelector('.qm-question').innerHTML        = safeQuizMd(q.question);
        node.querySelector('.qm-score-val').textContent     = earnedPts;

        const codeEl = node.querySelector('.qm-code');
        if (q.code && !['fill-code', 'fill-code-drag'].includes(q.type)) {
            codeEl.removeAttribute('hidden');
            codeEl.querySelector('code').textContent = q.code;
        }

        const checkBtn = node.querySelector('.js-qm-check');
        const nextBtn  = node.querySelector('.js-qm-next');
        checkBtn.removeAttribute('hidden');
        nextBtn.textContent = this.current + 1 < this.questions.length ? 'Далее →' : 'Завершить →';

        node.querySelector('.qm-feedback').id = 'qm-feedback';
        node.querySelector('.qm-answers').id  = 'qm-answers';

        this.container.replaceChildren(node);

        // Выносим footer из скроллируемой области наружу (в quiz-modal-box)
        const box = this.container.closest('.quiz-modal-box') || this.container.parentElement;
        const footer = this.container.querySelector('.qm-footer');
        if (footer && box && box !== this.container) {
            // Удаляем старый вынесенный footer если есть
            box.querySelector('.qm-footer--fixed')?.remove();
            footer.classList.add('qm-footer--fixed');
            box.appendChild(footer);
        }

        this.container.querySelector('#qm-answers')
            .replaceChildren(...buildNodes(q, this.quizId));

        nextBtn.hidden = true;
        nextBtn.addEventListener('click', () => { this.current++; this._render(); });

        // Enter: до ответа — проверить (если кнопка активна и помечена ready), после — далее
        // Ctrl+Shift+Enter — DEBUG: пропустить вопрос без ответа
        const onEnter = e => {
            if (e.key !== 'Enter') return;
            if (e.target.matches('input, textarea')) return;
            e.preventDefault();

            // DEBUG: Ctrl+Shift+Enter — пропуск вопроса
            if (e.ctrlKey && e.shiftKey) {
                this._clearEnterHandler();
                this.answers[this.current] = { isRight: true, pts: 10, qId: q.id };
                this.current++;
                this._render();
                return;
            }

            if (!nextBtn.hidden) {
                nextBtn.click();
            } else if (!checkBtn.hidden && !checkBtn.disabled) {
                checkBtn.click();
            }
        };
        this._enterHandler = onEnter;
        document.addEventListener('keydown', onEnter);
        // Убираем слушатель при переходе к следующему вопросу
        nextBtn.addEventListener('click', () => document.removeEventListener('keydown', onEnter), { once: true });

        attachListeners(q, this.container, checkBtn, this.quizId, value => {
            if (this.answered) return;
            this.answered = true;
            checkBtn.hidden = true;
            const { isRight, earned, extra } = submit(q, value, this.container, this.quizId);
            this.answers[this.current] = { isRight, pts: earned, qId: q.id };
            this._showFeedback(isRight, q.explanation, earned, extra);
            nextBtn.hidden = false;
        });

        highlightCode(this.container);
    }

    _showFeedback(isRight, explanation, earned, extra = null) {
        const fb = this.container.querySelector('#qm-feedback');
        if (!fb) return;

        const tpl = get('tpl-qm-feedback');
        if (tpl) {
            const node = tpl.content.cloneNode(true);
            const iconEl = node.querySelector('.qm-fb-icon');
            iconEl.textContent = isRight ? '✅' : '❌';
            iconEl.classList.add(isRight ? 'qm-fb-icon--ok' : 'qm-fb-icon--err');
            node.querySelector('.qm-fb-verdict').textContent   = isRight ? 'Правильно!' : 'Неверно';
            const expEl = node.querySelector('.qm-fb-explanation');
            if (explanation) {
                expEl.innerHTML = explanation;
            } else {
                expEl.hidden = true;
            }
            const extraEl = node.querySelector('.qm-fb-extra');
            if (extra) { extraEl.innerHTML = extra; extraEl.removeAttribute('hidden'); }
            const ptsEl = node.querySelector('.qm-fb-pts');
            if (earned) { ptsEl.textContent = `+${earned} очков`; ptsEl.removeAttribute('hidden'); }
            fb.replaceChildren(node);
        }

        fb.removeAttribute('hidden');
        fb.style.display = 'flex';
        fb.className = `qm-feedback qm-feedback--${isRight ? 'ok' : 'err'}`;
        if (earned && window.gameSystem) window.gameSystem.earnXP(earned, 'за правильный ответ');

        // Загружаем статистику сообщества для этого вопроса
        this._loadQuestionCommunityStats(fb);
    }

    _loadQuestionCommunityStats(fb) {
        const q = this.questions[this.current];
        if (!q?.id) return;
        const user = JSON.parse(localStorage.getItem('cpp_user') || 'null');
        const headers = user?.isuNumber ? { 'X-Isu-Number': user.isuNumber } : {};
        fetch(`/api/quiz/${this.quizId}/question-stats/${q.id}`, { headers })
            .then(r => r.ok ? r.json() : null)
            .then(data => {
                if (!data || data.totalAttempts < 1) return;
                const statEl = (this.container.closest('.quiz-modal-box') || this.container.parentElement)
                    ?.querySelector('.qm-community-stat');
                if (!statEl) return;
                statEl.textContent = `· ${data.correctPct}% решили верно`;
                statEl.removeAttribute('hidden');
            })
            .catch(() => {});
    }

    _clearEnterHandler() {
        if (this._enterHandler) {
            document.removeEventListener('keydown', this._enterHandler);
            this._enterHandler = null;
        }
    }

    _saveAbandoned() {
        this._clearEnterHandler();
        // Не сохраняем если не было ни одного ответа
        if (Object.keys(this.answers).length === 0) return;
        // Сохраняем попытку если тест не был завершён (закрыт крестиком)
        // Неотвеченные вопросы считаются неправильными
        if (this.current >= this.questions.length) return; // уже завершён нормально
        const answersArr = this.questions.map((q, i) => {
            const a = this.answers[i];
            return { isRight: a?.isRight ?? false, earned: a?.pts ?? 0, qId: q.id };
        });
        const earned = answersArr.reduce((s, a) => s + a.earned, 0);
        const pct    = Math.round((earned / (this.questions.length * 10)) * 100);
        saveResult({ quizId: this.quizId, title: this.title, questions: this.questions, answers: answersArr, pct })
            .then(() => {
                window.dispatchEvent(new CustomEvent('quizCompleted', { detail: { testId: this.quizId } }));
                window.ParagraphLock?.invalidateCache();
            });
    }

    _showResults() {
        this._clearEnterHandler();

        // Убираем вынесенный footer (кнопка «Завершить →»)
        const box = this.container.closest('.quiz-modal-box') || this.container.parentElement;
        box?.querySelector('.qm-footer--fixed')?.remove();

        const total   = this.questions.length;
        const earned  = Object.values(this.answers).reduce((s, a) => s + (a.pts ?? 0), 0);
        const pct     = Math.round((earned / (total * 10)) * 100);
        const correct = Object.values(this.answers).filter(a => a.isRight).length;
        const { emoji, msg: heading } = resultMood(pct);

        const answersArr = Object.values(this.answers).map(a => ({
            isRight: a.isRight, earned: a.pts, qId: a.qId,
        }));
        saveResult({ quizId: this.quizId, title: this.title, questions: this.questions, answers: answersArr, pct })
            .then(data => {
                const reward = data?.reward ?? data;
                if (reward?.coinsEarned !== undefined) this._showReward(reward);
                window.dispatchEvent(new CustomEvent('quizCompleted', { detail: { testId: this.quizId } }));
                window.ParagraphLock?.invalidateCache();
            });

        const tpl = get('tpl-qm-results');
        if (!tpl) return;

        const node = tpl.content.cloneNode(true);
        node.querySelector('.qm-results-emoji').textContent   = emoji;
        node.querySelector('.qm-results-heading').textContent = heading;
        node.querySelector('.qm-results-pct').textContent     = pct + '%';
        node.querySelector('.js-earned').textContent  = earned;
        node.querySelector('.js-correct').textContent = correct;
        node.querySelector('.js-total').textContent   = total;

        const arc = node.querySelector('.qm-results-arc');
        if (arc) {
            const passed = pct >= this.passingScore;
            arc.setAttribute('stroke', passed ? 'var(--accent-green)' : 'var(--accent-orange)');
            arc.setAttribute('stroke-dasharray', `${pct * 2.639} 263.9`);
        }

        const rewardsEl = node.querySelector('.qm-rewards');
        if (rewardsEl) rewardsEl.id = `qm-rewards-${this.quizId}`;

        node.querySelector('.js-qm-retry').addEventListener('click', () => {
            this.current = 0; this.answers = {}; this._render();
        });
        node.querySelector('.js-qm-close').addEventListener('click', () => window.closeQuizModal(null));

        this.container.replaceChildren(node);

        // Загружаем распределение результатов сообщества
        this._loadResultsDistribution(pct);
    }

    _loadResultsDistribution(myPct) {
        const user = JSON.parse(localStorage.getItem('cpp_user') || 'null');
        const headers = user?.isuNumber ? { 'X-Isu-Number': user.isuNumber } : {};
        fetch(`/api/quiz/${this.quizId}/score-distribution?score=${myPct}`, { headers })
            .then(r => r.ok ? r.json() : null)
            .then(data => {
                if (!data || data.totalAttempts < 1) return;
                const statsEl = this.container.querySelector('.qm-results-stats');
                if (!statsEl) return;
                const distEl = document.createElement('div');
                distEl.className = 'qm-score-dist';
                distEl.innerHTML = `
                    <div class="qm-dist-row">
                        <span class="qm-dist-label">Лучше тебя</span>
                        <span class="qm-dist-bar-wrap"><span class="qm-dist-bar qm-dist-bar--above" style="width:${data.pctAbove}%"></span></span>
                        <span class="qm-dist-val">${data.pctAbove}%</span>
                    </div>
                    <div class="qm-dist-row">
                        <span class="qm-dist-label">Столько же</span>
                        <span class="qm-dist-bar-wrap"><span class="qm-dist-bar qm-dist-bar--same" style="width:${Math.max(data.pctSame, 2)}%"></span></span>
                        <span class="qm-dist-val">${data.pctSame}%</span>
                    </div>
                    <div class="qm-dist-row">
                        <span class="qm-dist-label">Хуже тебя</span>
                        <span class="qm-dist-bar-wrap"><span class="qm-dist-bar qm-dist-bar--below" style="width:${data.pctBelow}%"></span></span>
                        <span class="qm-dist-val">${data.pctBelow}%</span>
                    </div>
                    <div class="qm-dist-my">Твой результат: <strong>${myPct}%</strong> · ${data.totalAttempts} попыток всего</div>`;
                statsEl.after(distEl);
            })
            .catch(() => {});
    }

    _showReward(reward) {
        const el = document.getElementById(`qm-rewards-${this.quizId}`);
        if (!el) return;
        const parts = rewardParts(reward);
        if (!parts.length) return;
        el.removeAttribute('hidden');
        el.style.display = 'flex';
        el.replaceChildren(...parts.map(p => Object.assign(document.createElement('span'), { className: 'reward-badge', textContent: p })));

        // Анимация: монетки/XP летят от reward-блока к хедеру
        requestAnimationFrame(() => {
            const rect = el.getBoundingClientRect();
            const cx = rect.left + rect.width / 2;
            const cy = rect.top  + rect.height / 2;
            if (reward.coinsEarned > 0) {
                window.dispatchEvent(new CustomEvent('coinsEarnAnim', { detail: { emoji: '🪙', count: Math.min(Math.max(1, Math.floor(reward.coinsEarned / 10)), 6), fromX: cx, fromY: cy } }));
            }
            if (reward.xpEarned > 0) {
                window.dispatchEvent(new CustomEvent('coinsEarnAnim', { detail: { emoji: '⭐', count: Math.min(Math.max(1, Math.floor(reward.xpEarned / 10)), 5), fromX: cx, fromY: cy } }));
            }
        });
    }
}
