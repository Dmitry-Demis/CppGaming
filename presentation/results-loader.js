// Results Loader — comparison donut charts (Сайт vs ВУЗ vs Онлайн)
// Reads from "Опрос. Cpp Quest" CSV — Cmp1-Cmp4 columns contain "ВУЗ;Онлайн;Сайт" triples

function csvRowRes(line) {
    const r = []; let c = '', q = false;
    for (let i = 0; i < line.length; i++) {
        const ch = line[i];
        if (ch === '"') q = !q;
        else if (ch === ',' && !q) { r.push(c); c = ''; }
        else c += ch;
    }
    r.push(c);
    return r.map(s => s.replace(/^"|"$/g, '').trim());
}

function parseCSVRes(txt) {
    const lines = txt.trim().split(/\r?\n/);
    const hdr = csvRowRes(lines[0]);
    return lines.slice(1).map(l => {
        const v = csvRowRes(l), o = {};
        hdr.forEach((h, i) => o[h.trim()] = (v[i] || '').trim());
        return o;
    }).filter(r => Object.values(r).some(x => x));
}

function avgNums(arr) {
    const n = arr.map(Number).filter(x => !isNaN(x) && x >= 0);
    return n.length ? +(n.reduce((a, b) => a + b, 0) / n.length).toFixed(1) : null;
}

function parseCmpTriple(val) {
    if (!val) return null;
    const parts = val.split(';').map(s => parseFloat(s.trim()));
    if (parts.length === 3 && parts.every(x => !isNaN(x))) {
        return { uni: parts[0], online: parts[1], site: parts[2] };
    }
    return null;
}

function compDonut(segments, centerVal, size = 76) {
    const r = 36, cx = 50, cy = 50, sw = 14;
    let angle = -90, paths = '';
    const total = segments.reduce((s, seg) => s + seg.val, 0);
    segments.forEach(seg => {
        const pct = total > 0 ? seg.val / total : 1 / segments.length;
        const a = pct * 360;
        const end = angle + a;
        const x1 = cx + r * Math.cos(angle * Math.PI / 180);
        const y1 = cy + r * Math.sin(angle * Math.PI / 180);
        const x2 = cx + r * Math.cos(end * Math.PI / 180);
        const y2 = cy + r * Math.sin(end * Math.PI / 180);
        const large = a > 180 ? 1 : 0;
        paths += `<path d="M${cx},${cy} L${x1},${y1} A${r},${r} 0 ${large} 1 ${x2},${y2} Z" fill="${seg.color}" opacity="0.9"/>`;
        angle = end;
    });
    const scoreColor = centerVal >= 8 ? '#10b981' : centerVal >= 6 ? '#f59e0b' : '#ef4444';
    return `<svg viewBox="0 0 100 100" style="width:${size}px;height:${size}px;flex-shrink:0">
        <circle cx="${cx}" cy="${cy}" r="${r + sw / 2}" fill="#111827"/>
        ${paths}
        <circle cx="${cx}" cy="${cy}" r="${r - sw}" fill="#111827"/>
        <text x="${cx}" y="${cy - 4}" text-anchor="middle" fill="${scoreColor}" font-size="15" font-weight="900" font-family="Inter,sans-serif">${centerVal}</text>
        <text x="${cx}" y="${cy + 9}" text-anchor="middle" fill="#6b7280" font-size="7" font-weight="600" font-family="Inter,sans-serif">avg</text>
    </svg>`;
}

// Compact card for the 4-column grid
function compCard(title, siteVal, uniVal, onlineVal) {
    const segments = [
        { label: 'Сайт', val: siteVal, color: '#10b981' },
        { label: 'ВУЗ', val: uniVal, color: '#6366f1' },
        { label: 'Онлайн', val: onlineVal, color: '#f59e0b' },
    ];
    const overallAvg = ((siteVal + uniVal + onlineVal) / 3).toFixed(1);
    const donutSVG = compDonut(segments, overallAvg, 72);

    const rows = segments.map(seg => {
        const w = Math.round((seg.val / 10) * 100);
        return `<div style="display:flex;align-items:center;gap:5px;margin-bottom:4px">
            <span style="font-size:0.6rem;color:#9ca3af;width:38px;flex-shrink:0">${seg.label}</span>
            <div style="flex:1;height:3px;background:rgba(255,255,255,.06);border-radius:2px;overflow:hidden">
                <div style="height:100%;width:${w}%;background:${seg.color};border-radius:2px"></div>
            </div>
            <span style="font-size:0.62rem;font-weight:700;color:${seg.color};width:24px;text-align:right;flex-shrink:0">${seg.val.toFixed(1)}</span>
        </div>`;
    }).join('');

    return `<div style="background:#1a1d29;padding:10px;border-radius:8px;border:1px solid rgba(255,255,255,.07)">
        <div style="font-size:0.65rem;font-weight:700;color:#e2e8f0;margin-bottom:8px;line-height:1.3;min-height:2.6em">${title}</div>
        <div style="display:flex;gap:8px;align-items:center">
            ${donutSVG}
            <div style="flex:1;min-width:0">${rows}</div>
        </div>
    </div>`;
}

// Wide overall card shown below the 4-column grid
function overallCard(siteVal, uniVal, onlineVal, n, malePct, betterPct) {
    const segments = [
        { label: 'Сайт', val: siteVal, color: '#10b981' },
        { label: 'ВУЗ', val: uniVal, color: '#6366f1' },
        { label: 'Онлайн', val: onlineVal, color: '#f59e0b' },
    ];
    const overallAvg = ((siteVal + uniVal + onlineVal) / 3).toFixed(1);
    const donutSVG = compDonut(segments, overallAvg, 82);

    const rows = segments.map(seg => {
        const w = Math.round((seg.val / 10) * 100);
        return `<div style="display:flex;align-items:center;gap:8px;margin-bottom:5px">
            <span style="font-size:0.68rem;color:#9ca3af;width:44px;flex-shrink:0">${seg.label}</span>
            <div style="flex:1;height:5px;background:rgba(255,255,255,.06);border-radius:3px;overflow:hidden">
                <div style="height:100%;width:${w}%;background:${seg.color};border-radius:3px"></div>
            </div>
            <span style="font-size:0.7rem;font-weight:700;color:${seg.color};width:28px;text-align:right;flex-shrink:0">${seg.val.toFixed(1)}</span>
        </div>`;
    }).join('');

    return `<div style="background:#1a1d29;padding:12px 16px;border-radius:10px;border:2px solid rgba(99,102,241,0.35);display:flex;align-items:center;gap:14px;margin-top:7px">
        ${donutSVG}
        <div style="flex:1;min-width:0">
            <div style="font-size:0.72rem;font-weight:700;color:#a5b4fc;margin-bottom:8px;text-transform:uppercase;letter-spacing:0.05em">Итоговое среднее по всем критериям</div>
            ${rows}
        </div>
        <div style="display:flex;gap:8px;flex-shrink:0">
            <div style="text-align:center;padding:8px 12px;background:rgba(255,255,255,0.05);border-radius:8px;min-width:52px">
                <div style="font-size:1.3rem;font-weight:900;color:#6366f1;line-height:1">${n}</div>
                <div style="font-size:0.52rem;color:#6b7280;text-transform:uppercase;letter-spacing:.06em;margin-top:3px">Ответов</div>
            </div>
            <div style="text-align:center;padding:8px 12px;background:rgba(255,255,255,0.05);border-radius:8px;min-width:52px">
                <div style="font-size:1.3rem;font-weight:900;color:#10b981;line-height:1">${malePct}%</div>
                <div style="font-size:0.52rem;color:#6b7280;text-transform:uppercase;letter-spacing:.06em;margin-top:3px">Мужчин</div>
            </div>
            <div style="text-align:center;padding:8px 12px;background:rgba(255,255,255,0.05);border-radius:8px;min-width:52px">
                <div style="font-size:1.3rem;font-weight:900;color:#f59e0b;line-height:1">${betterPct}%</div>
                <div style="font-size:0.52rem;color:#6b7280;text-transform:uppercase;letter-spacing:.06em;margin-top:3px">Лучше ВУЗа</div>
            </div>
        </div>
    </div>`;
}

async function loadResults() {
    const container = document.getElementById('results-comparison-container');
    if (!container) return;

    try {
        const response = await fetch('Опрос. Cpp Quest - Опрос. Cpp Quest.csv');
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        const text = await response.text();
        const rows = parseCSVRes(text);

        const cmp1 = rows.map(r => parseCmpTriple(r['Cmp1_Структура(ВУЗ;Онлайн;Сайт)'])).filter(Boolean);
        const cmp2 = rows.map(r => parseCmpTriple(r['Cmp2_Пробелы(ВУЗ;Онлайн;Сайт)'])).filter(Boolean);
        const cmp3 = rows.map(r => parseCmpTriple(r['Cmp3_Повторение(ВУЗ;Онлайн;Сайт)'])).filter(Boolean);
        const cmp4 = rows.map(r => parseCmpTriple(r['Cmp4_Фидбек(ВУЗ;Онлайн;Сайт)'])).filter(Boolean);

        const avg3 = (arr, key) => avgNums(arr.map(x => x[key]));

        const c1 = { site: avg3(cmp1,'site'), uni: avg3(cmp1,'uni'), online: avg3(cmp1,'online') };
        const c2 = { site: avg3(cmp2,'site'), uni: avg3(cmp2,'uni'), online: avg3(cmp2,'online') };
        const c3 = { site: avg3(cmp3,'site'), uni: avg3(cmp3,'uni'), online: avg3(cmp3,'online') };
        const c4 = { site: avg3(cmp4,'site'), uni: avg3(cmp4,'uni'), online: avg3(cmp4,'online') };

        const allSite   = avgNums([c1.site,   c2.site,   c3.site,   c4.site  ].map(String));
        const allUni    = avgNums([c1.uni,     c2.uni,    c3.uni,    c4.uni   ].map(String));
        const allOnline = avgNums([c1.online,  c2.online, c3.online, c4.online].map(String));

        const n = rows.length;
        const male = rows.filter(r => r['Пол'] === 'Мужской').length;
        const malePct = Math.round(male / n * 100);

        const betterCount = rows.filter(r => {
            const v = r['R2_Эффективность'] || '';
            return v.includes('заметно лучше') || v.includes('Идеально');
        }).length;
        const betterPct = Math.round(betterCount / n * 100);

        container.innerHTML = `
            <div style="display:grid;grid-template-columns:repeat(4,1fr);gap:7px">
                ${compCard('Структура и полнота материала', c1.site, c1.uni, c1.online)}
                ${compCard('Закрытие пробелов в знаниях', c2.site, c2.uni, c2.online)}
                ${compCard('Удобство повторения', c3.site, c3.uni, c3.online)}
                ${compCard('Качество обратной связи', c4.site, c4.uni, c4.online)}
            </div>
            ${overallCard(allSite, allUni, allOnline, n, malePct, betterPct)}`;

    } catch (error) {
        console.error('Error loading results:', error);
        container.innerHTML = `<div style="color:#ef4444;text-align:center;padding:20px">❌ Ошибка загрузки данных: ${error.message}</div>`;
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', loadResults);
} else {
    loadResults();
}
