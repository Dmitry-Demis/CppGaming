// Problems, Motivation & Preferences Loader

function csvRowProb(line) {
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

function parseCSVProb(txt) {
    const lines = txt.trim().split(/\r?\n/);
    const hdr = csvRowProb(lines[0]);
    return lines.slice(1).map(l => {
        const v = csvRowProb(l), o = {};
        hdr.forEach((h, i) => o[h.trim()] = (v[i] || '').trim());
        return o;
    }).filter(r => Object.values(r).some(x => x));
}

function multiProb(v) { return v ? v.split(';').map(s => s.trim()).filter(Boolean) : []; }

function countMProb(arr) {
    const m = {};
    arr.forEach(cell => multiProb(cell).forEach(v => { m[v] = (m[v] || 0) + 1; }));
    return m;
}

const PAL_PROB = ['#6366f1','#8b5cf6','#ec4899','#f59e0b','#10b981','#3b82f6','#ef4444','#14b8a6','#f97316','#a855f7','#06b6d4','#84cc16'];
const RANK_COLORS = ['#f59e0b', '#94a3b8', '#cd7c3a'];

function createBarChart(title, counts, total) {
    const entries = Object.entries(counts).sort((a, b) => b[1] - a[1]);
    if (!entries.length) return '';
    const maxV = entries[0][1];

    const rows = entries.map(([k, v], i) => {
        const pct = Math.round(v / total * 100);
        const w = Math.round(v / maxV * 100);
        const c = PAL_PROB[i % PAL_PROB.length];
        const rb = i < 3 ? RANK_COLORS[i] : 'rgba(255,255,255,.06)';
        const rc = i < 3 ? '#000' : '#6b7280';
        return `<div style="margin-bottom:7px">
            <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:3px">
                <span style="font-size:0.67rem;color:#d1d5db;display:flex;align-items:center;gap:5px;flex:1;min-width:0">
                    <span style="display:inline-flex;align-items:center;justify-content:center;width:16px;height:16px;border-radius:4px;font-size:0.58rem;font-weight:800;background:${rb};color:${rc};flex-shrink:0">${i + 1}</span>
                    <span style="overflow:hidden;text-overflow:ellipsis">${k}</span>
                </span>
                <span style="font-size:0.67rem;font-weight:800;color:${c};flex-shrink:0;margin-left:6px">${v} <span style="color:#6b7280;font-weight:400;font-size:0.6rem">${pct}%</span></span>
            </div>
            <div style="height:5px;background:rgba(255,255,255,.05);border-radius:3px;overflow:hidden">
                <div style="height:100%;width:${w}%;background:linear-gradient(90deg,${c}66,${c});border-radius:3px"></div>
            </div>
        </div>`;
    }).join('');

    return `<div style="background:#1a1d29;padding:14px;border-radius:10px;border:1px solid rgba(255,255,255,.07);flex:1">
        <div style="font-size:0.82rem;font-weight:700;color:white;margin-bottom:4px">${title}</div>
        <div style="font-size:0.6rem;color:#6b7280;margin-bottom:10px">% от ${total} респондентов · можно несколько</div>
        ${rows}
    </div>`;
}

async function loadProblemsMotivation() {
    const container = document.getElementById('difficulties-container');
    if (!container) return;

    try {
        const response = await fetch('Результаты Опроса - Результаты Опроса.csv');
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        const text = await response.text();
        const rows = parseCSVProb(text);
        const n = rows.length;

        const problemsCounts = countMProb(rows.map(r => r['Проблемы']));
        const motivatorsCounts = countMProb(rows.map(r => r['Мотиваторы']));

        const html = `<div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
            ${createBarChart('С какими трудностями столкнулись?', problemsCounts, n)}
            ${createBarChart('Что могло бы повысить мотивацию?', motivatorsCounts, n)}
        </div>`;

        container.innerHTML = html;
    } catch (error) {
        console.error('Error loading problems/motivation:', error);
        container.innerHTML = `<div style="color:#ef4444;text-align:center;padding:20px">❌ Ошибка загрузки данных</div>`;
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', loadProblemsMotivation);
} else {
    loadProblemsMotivation();
}
