// Demographics Loader — uses Результаты Опроса CSV

const PAL_DEMO = ['#6366f1','#8b5cf6','#ec4899','#f59e0b','#10b981','#3b82f6','#ef4444','#14b8a6','#f97316','#a855f7'];

function csvRowDemo(line) {
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

function parseCSVDemo(txt) {
    const lines = txt.trim().split(/\r?\n/);
    const hdr = csvRowDemo(lines[0]);
    return lines.slice(1).map(l => {
        const v = csvRowDemo(l), o = {};
        hdr.forEach((h, i) => o[h.trim()] = (v[i] || '').trim());
        return o;
    }).filter(r => Object.values(r).some(x => x));
}

function multiDemo(v) { return v ? v.split(';').map(s => s.trim()).filter(Boolean) : []; }

function countVDemo(arr) {
    const m = {};
    arr.forEach(v => { if (v && v.trim()) m[v.trim()] = (m[v.trim()] || 0) + 1; });
    return m;
}

function countMDemo(arr) {
    const m = {};
    arr.forEach(cell => multiDemo(cell).forEach(v => { m[v] = (m[v] || 0) + 1; }));
    return m;
}

function svgDonut(segments, total, size = 88) {
    const r = 38, cx = 50, cy = 50, sw = 16;
    let angle = -90, paths = '';
    segments.forEach(seg => {
        const a = (seg.pct / 100) * 360;
        const end = angle + a;
        const x1 = cx + r * Math.cos(angle * Math.PI / 180);
        const y1 = cy + r * Math.sin(angle * Math.PI / 180);
        const x2 = cx + r * Math.cos(end * Math.PI / 180);
        const y2 = cy + r * Math.sin(end * Math.PI / 180);
        const large = a > 180 ? 1 : 0;
        paths += `<path d="M${cx},${cy} L${x1},${y1} A${r},${r} 0 ${large} 1 ${x2},${y2} Z" fill="${seg.color}" opacity="0.92"/>`;
        angle = end;
    });
    return `<svg viewBox="0 0 100 100" style="width:${size}px;height:${size}px;flex-shrink:0">
        <circle cx="${cx}" cy="${cy}" r="${r + sw / 2}" fill="#1a1d29"/>
        ${paths}
        <circle cx="${cx}" cy="${cy}" r="${r - sw}" fill="#1a1d29"/>
        <text x="${cx}" y="${cy - 5}" text-anchor="middle" fill="white" font-size="16" font-weight="800" font-family="Inter,sans-serif">${total}</text>
        <text x="${cx}" y="${cy + 10}" text-anchor="middle" fill="#6b7280" font-size="8" font-weight="600" font-family="Inter,sans-serif">ответов</text>
    </svg>`;
}

function donutCard(title, counts, total) {
    const entries = Object.entries(counts).sort((a, b) => b[1] - a[1]);
    if (!entries.length) return '';
    const segments = entries.map(([, v], i) => ({ pct: Math.round(v / total * 100), color: PAL_DEMO[i % PAL_DEMO.length] }));
    const legend = entries.map(([k, v], i) => `
        <div style="display:flex;align-items:center;gap:4px;margin-bottom:4px">
            <div style="width:7px;height:7px;border-radius:50%;background:${PAL_DEMO[i % PAL_DEMO.length]};flex-shrink:0"></div>
            <span style="font-size:0.6rem;color:#d1d5db;flex:1;line-height:1.2;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${k}</span>
            <span style="font-size:0.6rem;font-weight:700;color:#e2e8f0;flex-shrink:0;margin-left:2px">${v}</span>
            <span style="font-size:0.58rem;color:#6b7280;width:26px;text-align:right;flex-shrink:0">${Math.round(v / total * 100)}%</span>
        </div>`).join('');
    return `<div style="background:#1a1d29;padding:10px;border-radius:10px;border:1px solid rgba(255,255,255,.07);overflow:hidden">
        <div style="font-size:0.75rem;font-weight:700;color:white;margin-bottom:8px">${title}</div>
        <div style="display:flex;gap:8px;align-items:center">
            ${svgDonut(segments, total)}
            <div style="flex:1;min-width:0;overflow:hidden">${legend}</div>
        </div>
    </div>`;
}

function multiBarCard(title, counts, total) {
    const entries = Object.entries(counts).sort((a, b) => b[1] - a[1]);
    if (!entries.length) return '';
    const maxV = entries[0][1];
    const RANK = ['#f59e0b', '#94a3b8', '#cd7c3a'];
    const rows = entries.map(([k, v], i) => {
        const pct = Math.round(v / total * 100);
        const w = Math.round(v / maxV * 100);
        const c = PAL_DEMO[i % PAL_DEMO.length];
        const rb = i < 3 ? RANK[i] : 'rgba(255,255,255,.06)';
        const rc = i < 3 ? '#000' : '#6b7280';
        return `<div style="margin-bottom:7px">
            <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:3px">
                <span style="font-size:0.67rem;color:#d1d5db;display:flex;align-items:center;gap:5px">
                    <span style="display:inline-flex;align-items:center;justify-content:center;width:16px;height:16px;border-radius:4px;font-size:0.58rem;font-weight:800;background:${rb};color:${rc};flex-shrink:0">${i + 1}</span>
                    ${k}
                </span>
                <span style="font-size:0.67rem;font-weight:800;color:${c};flex-shrink:0;margin-left:6px">${v} <span style="color:#6b7280;font-weight:400;font-size:0.6rem">${pct}%</span></span>
            </div>
            <div style="height:5px;background:rgba(255,255,255,.05);border-radius:3px;overflow:hidden">
                <div style="height:100%;width:${w}%;background:linear-gradient(90deg,${c}66,${c});border-radius:3px"></div>
            </div>
        </div>`;
    }).join('');
    return `<div style="background:#1a1d29;padding:12px;border-radius:10px;border:1px solid rgba(255,255,255,.07)">
        <div style="font-size:0.8rem;font-weight:700;color:white;margin-bottom:4px">${title}</div>
        <div style="font-size:0.6rem;color:#6b7280;margin-bottom:10px">% от ${total} респондентов · можно несколько</div>
        ${rows}
    </div>`;
}

async function loadDemographics() {
    const container = document.getElementById('demographics-container');
    if (!container) return;

    try {
        // Load aprobation CSV for ВУЗ, Пол, C++ course
        const respAprob = await fetch('Апробация ВКР (Ответы) - Ответы на форму (1).csv');
        if (!respAprob.ok) throw new Error(`HTTP ${respAprob.status}`);
        const textAprob = await respAprob.text();
        const rowsAprob = parseCSVDemo(textAprob).filter(r => r['Университет'] || r['Пол']);

        const nAprob = rowsAprob.length;
        const uniCounts    = countVDemo(rowsAprob.map(r => r['Университет']).filter(Boolean));
        const genderCounts = countVDemo(rowsAprob.map(r => r['Пол']).filter(Boolean));
        const cppRaw       = rowsAprob.map(r => r['Был ли у вас предмет C++?']).filter(Boolean);
        const cppCounts    = countVDemo(cppRaw);
        const nCpp         = cppRaw.length; // только те, кто ответил

        // Load old CSV for Курс и Опыт C++ (from Результаты Опроса)
        const respOld = await fetch('Результаты Опроса - Результаты Опроса.csv');
        if (!respOld.ok) throw new Error(`HTTP ${respOld.status}`);
        const textOld = await respOld.text();
        const rowsOld = parseCSVDemo(textOld);
        const nOld = rowsOld.length;
        const courseCounts   = countVDemo(rowsOld.map(r => r['Курс']).filter(Boolean));
        const stdCounts      = countVDemo(rowsOld.map(r => r['P1_Стандарты']).filter(Boolean));
        const gitCounts      = countVDemo(rowsOld.map(r => r['P6_Git']).filter(Boolean));
        const cicdCounts     = countVDemo(rowsOld.map(r => r['P7_CICD']).filter(Boolean));
        const prefCounts     = countMDemo(rowsOld.map(r => r['Предпочтения']));
        const ageCounts      = countVDemo(rowsOld.map(r => r['Возраст']).filter(Boolean));
        const defCounts      = countVDemo(rowsOld.map(r => r['D1_Формат']).filter(Boolean));
        const nDef  = rowsOld.filter(r => r['D1_Формат']).length;
        const nStd  = rowsOld.filter(r => r['P1_Стандарты']).length;
        const nGit  = rowsOld.filter(r => r['P6_Git']).length;
        const nCicd = rowsOld.filter(r => r['P7_CICD']).length;

        const html = `
            <div style="display:grid;grid-template-columns:2fr 1.5fr 1.5fr 2fr;gap:10px;margin-bottom:10px">
                ${donutCard('ВУЗ', uniCounts, nAprob)}
                ${donutCard('Пол', genderCounts, nAprob)}
                ${donutCard('Был ли предмет C++?', cppCounts, nCpp)}
                ${donutCard('Курс и программа', courseCounts, nOld)}
            </div>
            <div style="display:grid;grid-template-columns:2fr 2fr 1fr;gap:10px;margin-bottom:10px">
                ${donutCard('Стандарты языка', stdCounts, nStd)}
                ${donutCard('Использование Git', gitCounts, nGit)}
                ${donutCard('Возраст', ageCounts, nOld)}
            </div>
            <div style="display:grid;grid-template-columns:2fr 1fr;gap:10px">
                ${multiBarCard('Какой формат обучения наиболее мотивирующий?', prefCounts, nOld)}
                <div style="display:flex;flex-direction:column;gap:10px">
                    ${donutCard('Проверка кода (CI/CD)', cicdCounts, nCicd)}
                    ${donutCard('Формат защиты', defCounts, nDef)}
                </div>
            </div>`;

        container.innerHTML = html;
    } catch (error) {
        console.error('Error loading demographics:', error);
        container.innerHTML = `<div style="color:#ef4444;text-align:center;padding:20px">❌ Ошибка загрузки данных</div>`;
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', loadDemographics);
} else {
    loadDemographics();
}
