// Тип: matching — перетаскивание карточек в слоты

import { fill } from '../ui/templates.js';

export function buildNodes(q, quizId, _g, _escape, md, shuffle) {
    const shuffledPairs = shuffle([...q.pairs]);
    const shuffledRight = shuffle(shuffledPairs.map(p => p.right));

    const pool = fill('tpl-matching-pool', { poolId: `matching-cards-${quizId}` });
    shuffledRight.forEach(item => {
        pool.appendChild(fill('tpl-matching-card', { value: item, text: md(item) }));
    });

    const slots = fill('tpl-matching-slots');
    shuffledPairs.forEach((pair, i) => {
        slots.appendChild(fill('tpl-matching-row', {
            letter:   i + 1,
            left:     md(pair.left),
            slot:     i,
            expected: pair.right,
            slotId:   `mslot-${quizId}-${i}`,
        }));
    });

    const wrap = document.createElement('div');
    wrap.className = 'quiz-matching';
    wrap.append(pool, slots);
    return [wrap];
}

export function attachListeners(q, container, checkBtn, quizId, onCorrect) {
    initDnd(container, quizId);
    if (!checkBtn) return;
    checkBtn.disabled = true;

    container.querySelectorAll('.quiz-matching-slot').forEach(slot => {
        slot.addEventListener('drop', () => { checkBtn.disabled = false; }, { once: true });
    });

    checkBtn.addEventListener('click', () => {
        checkBtn.hidden = true;
        onCorrect(null);
    });
}

export function initDnd(container, quizId) {
    let dragged  = null;  // для drag-and-drop
    let selected = null;  // для tap-to-place

    const pool = () => container.querySelector(`#matching-cards-${quizId}`);

    const deselect = () => {
        selected?.classList.remove('quiz-matching-card--selected');
        selected = null;
    };

    const placeCard = (card, slot) => {
        const existing = slot.querySelector('.quiz-matching-card');
        if (existing) pool()?.appendChild(existing);
        slot.querySelector('.quiz-matching-slot-hint')?.remove();
        slot.appendChild(card);
        slot.dispatchEvent(new Event('drop', { bubbles: true }));  // чтобы checkBtn разблокировался
    };

    const bindCard = card => {
        // --- drag ---
        card.addEventListener('dragstart', e => {
            dragged = card;
            deselect();
            card.classList.add('quiz-matching-card--dragging');
            e.dataTransfer.effectAllowed = 'move';
        });
        card.addEventListener('dragend', () => {
            card.classList.remove('quiz-matching-card--dragging');
            dragged = null;
        });

        // --- tap ---
        card.addEventListener('click', e => {
            e.stopPropagation();
            if (selected === card) { deselect(); return; }
            deselect();
            selected = card;
            card.classList.add('quiz-matching-card--selected');
        });
    };

    container.querySelectorAll('.quiz-matching-card').forEach(bindCard);

    container.querySelectorAll('.quiz-matching-slot').forEach(slot => {
        // --- drag ---
        slot.addEventListener('dragover', e => { e.preventDefault(); slot.classList.add('quiz-matching-slot--over'); });
        slot.addEventListener('dragleave', () => slot.classList.remove('quiz-matching-slot--over'));
        slot.addEventListener('drop', e => {
            e.preventDefault();
            slot.classList.remove('quiz-matching-slot--over');
            if (!dragged) return;
            const existing = slot.querySelector('.quiz-matching-card');
            if (existing) { pool()?.appendChild(existing); bindCard(existing); }
            slot.querySelector('.quiz-matching-slot-hint')?.remove();
            slot.appendChild(dragged);
        });

        // --- tap ---
        slot.addEventListener('click', () => {
            if (!selected) return;
            placeCard(selected, slot);
            deselect();
        });
    });

    // тык на пул — возвращает карточку обратно если выбрана
    pool()?.addEventListener('click', e => {
        if (!selected) return;
        if (e.target.closest('.quiz-matching-card')) return;  // клик по карточке обработает сама карточка
        pool()?.appendChild(selected);
        deselect();
    });

    pool()?.addEventListener('dragover', e => e.preventDefault());
    pool()?.addEventListener('drop', e => { e.preventDefault(); if (dragged) pool()?.appendChild(dragged); });
}

export function submit(q, _value, container, quizId, _g) {
    const slots = container.querySelectorAll('.quiz-matching-slot');
    let correct = 0;

    slots.forEach(slot => {
        const placed   = slot.querySelector('.quiz-matching-card')?.dataset.value ?? null;
        const expected = slot.dataset.expected;
        const ok       = placed === expected;
        if (ok) correct++;
        slot.classList.add(ok ? 'quiz-matching-slot--correct' : 'quiz-matching-slot--wrong');
        if (!ok) slot.appendChild(fill('tpl-matching-hint', { expected }));
    });

    const isRight = correct === slots.length;
    const earned  = Math.round((correct / slots.length) * 10);
    const extra   = isRight ? null : `Правильно: ${correct} из ${slots.length}`;
    return { isRight, earned, extra };
}
