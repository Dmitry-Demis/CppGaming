// Тип: classify — распределение карточек по категориям

export function buildNodes(q, quizId, tplGet, escape, md, shuffle) {
    if (!q.items || !q.categories) {
        const err = document.createElement('div');
        err.textContent = '[classify] Отсутствуют items или categories';
        return [err];
    }

    const wrap = document.createElement('div');
    wrap.className = 'quiz-classify';

    // Пул карточек (перемешанный)
    const itemsWithIdx = shuffle(q.items.map((item, i) => ({ item, i })));

    const pool = document.createElement('div');
    pool.className = 'quiz-classify-pool';
    pool.id = `classify-pool-${quizId}`;

    itemsWithIdx.forEach(({ item, i }) => {
        const card = document.createElement('div');
        card.className = 'quiz-classify-card';
        card.draggable = true;
        card.dataset.itemIdx = i;
        card.innerHTML = `<span class="quiz-matching-drag-handle">⠿</span><span>${md(item)}</span>`;
        pool.appendChild(card);
    });

    // Колонки категорий
    const cols = document.createElement('div');
    cols.className = 'quiz-classify-cols';

    q.categories.forEach((cat, ci) => {
        const col = document.createElement('div');
        col.className = 'quiz-classify-col';
        col.dataset.catIdx = ci;
        col.id = `classify-col-${quizId}-${ci}`;

        const header = document.createElement('div');
        header.className = 'quiz-classify-col-header';
        header.textContent = cat;

        const dropzone = document.createElement('div');
        dropzone.className = 'quiz-classify-dropzone';
        dropzone.dataset.catIdx = ci;
        dropzone.id = `classify-drop-${quizId}-${ci}`;

        col.append(header, dropzone);
        cols.appendChild(col);
    });

    wrap.append(pool, cols);
    return [wrap];
}

export function attachListeners(q, container, checkBtn, quizId, onCorrect) {
    _initDnd(container, quizId);
    if (!checkBtn) return;
    checkBtn.disabled = true;

    // Разблокировать при первом drop в dropzone
    const enableOnDrop = () => { checkBtn.disabled = false; };
    container.querySelectorAll('.quiz-classify-dropzone').forEach(zone => {
        zone.addEventListener('drop', enableOnDrop, { once: true });
    });

    checkBtn.addEventListener('click', () => {
        checkBtn.hidden = true;
        onCorrect(null);
    });
}

function _initDnd(container, quizId) {
    let dragged  = null;
    let selected = null;

    const pool = () => container.querySelector(`#classify-pool-${quizId}`);

    const deselect = () => {
        selected?.classList.remove('quiz-matching-card--selected');
        selected = null;
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

    container.querySelectorAll('.quiz-classify-card').forEach(bindCard);

    const zones = [...container.querySelectorAll('.quiz-classify-dropzone'),
                   pool()].filter(Boolean);

    zones.forEach(zone => {
        // --- drag ---
        zone.addEventListener('dragover', e => { e.preventDefault(); zone.classList.add('quiz-classify-over'); });
        zone.addEventListener('dragleave', () => zone.classList.remove('quiz-classify-over'));
        zone.addEventListener('drop', e => {
            e.preventDefault();
            zone.classList.remove('quiz-classify-over');
            if (!dragged) return;
            zone.appendChild(dragged);
            bindCard(dragged);
        });

        // --- tap ---
        zone.addEventListener('click', e => {
            if (!selected) return;
            if (e.target.closest('.quiz-classify-card')) return;
            zone.appendChild(selected);
            zone.dispatchEvent(new Event('drop', { bubbles: true }));
            deselect();
        });
    });
}

export function submit(q, _value, container, quizId) {
    const total = q.items.length;
    let correct = 0;

    q.categories.forEach((_, ci) => {
        const zone = container.querySelector(`#classify-drop-${quizId}-${ci}`);
        if (!zone) return;
        zone.querySelectorAll('.quiz-classify-card').forEach(card => {
            const itemIdx = parseInt(card.dataset.itemIdx);
            const ok = q.correct[itemIdx] === ci;
            if (ok) correct++;
            card.classList.add(ok ? 'quiz-classify-card--ok' : 'quiz-classify-card--err');
            if (!ok) {
                const hint = document.createElement('span');
                hint.className = 'quiz-classify-hint';
                hint.textContent = `→ ${q.categories[q.correct[itemIdx]]}`;
                card.appendChild(hint);
            }
        });
    });

    // Карточки оставшиеся в пуле — неправильно
    container.querySelector(`#classify-pool-${quizId}`)
        ?.querySelectorAll('.quiz-classify-card')
        .forEach(card => card.classList.add('quiz-classify-card--err'));

    const isRight = correct === total;
    const earned  = Math.round((correct / total) * 10);
    const extra   = isRight ? null : `Правильно: ${correct} из ${total}`;
    return { isRight, earned, extra };
}
