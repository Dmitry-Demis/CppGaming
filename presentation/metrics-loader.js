// Metrics Loader for Traditional Learning Dashboard

// Function to get rating label based on score
function getRatingLabel(score) {
    if (score >= 10.0) return 'Превосходно';
    if (score >= 9.0) return 'Отлично';
    if (score >= 8.0) return 'Очень хорошо';
    if (score >= 7.0) return 'Хорошо';
    if (score >= 6.0) return 'Выше среднего';
    if (score >= 5.0) return 'Средне';
    if (score >= 4.0) return 'Ниже среднего';
    if (score >= 3.0) return 'Посредственно';
    if (score >= 2.0) return 'Плохо';
    return 'Ужасно';
}

// Function to get rating class based on score
function getRatingClass(score) {
    if (score >= 9.0) return 'excellent';
    if (score >= 8.0) return 'good';
    if (score >= 7.0) return 'above-average';
    if (score >= 6.0) return 'average';
    if (score >= 5.0) return 'below-average';
    if (score >= 4.0) return 'poor';
    return 'very-poor';
}

// Function to get color based on score
function getScoreColor(score) {
    if (score >= 9.0) return '#10b981'; // bright green
    if (score >= 8.0) return '#34d399'; // light green
    if (score >= 7.0) return '#6ee7b7'; // pale green
    if (score >= 6.0) return '#84cc16'; // lime
    if (score >= 5.0) return '#eab308'; // yellow
    if (score >= 4.0) return '#fbbf24'; // light yellow
    if (score >= 3.0) return '#f59e0b'; // orange
    if (score >= 2.0) return '#f97316'; // orange-red
    if (score >= 1.0) return '#ef4444'; // red
    return '#dc2626'; // dark red
}

// Function to create donat circle SVG
function createDonatCircle(score) {
    const percentage = (score / 10) * 100;
    const radius = 27.5;
    const circumference = 2 * Math.PI * radius;
    const offset = circumference - (percentage / 100) * circumference;
    
    // Get color for the score number
    const scoreColor = getScoreColor(score);
    
    // Create gradient colors based on score segments
    const colors = [
        { stop: 0, color: '#ef4444' },    // 0-2: red
        { stop: 20, color: '#f97316' },   // 2-4: orange-red
        { stop: 40, color: '#f59e0b' },   // 4-6: orange
        { stop: 50, color: '#eab308' },   // 5-6: yellow
        { stop: 60, color: '#84cc16' },   // 6-7: lime
        { stop: 70, color: '#6ee7b7' },   // 7-8: pale green
        { stop: 80, color: '#34d399' },   // 8-9: light green
        { stop: 100, color: '#10b981' }   // 9-10: bright green
    ];
    
    const gradientId = `gradient-${Math.random().toString(36).substring(2, 11)}`;
    
    return `
        <div class="donat-circle-container">
            <svg class="donat-circle" viewBox="0 0 100 100">
                <defs>
                    <linearGradient id="${gradientId}" x1="0%" y1="0%" x2="100%" y2="100%">
                        ${colors.map(c => `<stop offset="${c.stop}%" stop-color="${c.color}" />`).join('')}
                    </linearGradient>
                </defs>
                <circle class="donat-circle-bg" cx="50" cy="50" r="${radius}" />
                <circle 
                    class="donat-circle-progress" 
                    cx="50" 
                    cy="50" 
                    r="${radius}"
                    stroke="url(#${gradientId})"
                    stroke-dasharray="${circumference}"
                    stroke-dashoffset="${offset}"
                />
            </svg>
            <div class="donat-center-text">
                <div class="donat-score" style="color: ${scoreColor};">${score.toFixed(1)}</div>
                <div class="donat-label">AVG</div>
            </div>
        </div>
    `;
}

// Function to create metric card HTML
function createMetricCard(metric, isWorst = false) {
    const score = parseFloat(metric.avg);
    const ratingClass = getRatingClass(score);
    const donatHTML = createDonatCircle(score);
    const worstClass = isWorst ? 'worst-metric' : '';
    const ratingLabel = getRatingLabel(score);
    
    return `
        <div class="metric-card-donat ${ratingClass} ${worstClass}">
            <div class="metric-text-content">
                <div class="metric-title-text">${metric.title}</div>
                <div class="metric-description-text">${metric.description}</div>
            </div>
            <div class="metric-donat-wrapper">
                ${donatHTML}
                <div class="metric-rating-label">${ratingLabel}</div>
            </div>
        </div>
    `;
}

// Proper CSV parser — handles quoted fields containing commas/semicolons
function csvRowMetrics(line) {
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

function parseCSV(text) {
    const lines = text.trim().split(/\r?\n/);
    const hdr = csvRowMetrics(lines[0]);
    return lines.slice(1).map(l => {
        const v = csvRowMetrics(l), o = {};
        hdr.forEach((h, i) => o[h.trim()] = (v[i] || '').trim());
        return o;
    }).filter(r => Object.values(r).some(x => x));
}

// Load metrics from CSV
async function loadTraditionalMetrics() {
    const loader = document.getElementById('csv-loader');
    const container = document.getElementById('traditional-metrics');
    
    if (!container) {
        console.error('Container #traditional-metrics not found');
        return;
    }
    
    // Show loading state
    if (loader) {
        loader.innerHTML = '<p style="color: #9ca3af; font-size: 0.9rem;">Загрузка данных...</p>';
    }
    
    try {
        const response = await fetch('Результаты Опроса - Результаты Опроса.csv');
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const text = await response.text();
        const rows = parseCSV(text);
        
        // Define metrics to extract from CSV
        const metricsConfig = [
        { key: 'T1_Материалы', title: 'Доступность материалов', description: 'Были ли все лекции, презентации и методички доступны в одном удобном месте?' },
            { key: 'T2_Понятность', title: 'Понятность подачи', description: 'Было ли понятно из лекций, что именно нужно записывать и учить?' },
            { key: 'T3_Преподаватель', title: 'Компетентность преподавателя', description: 'Насколько хорошо преподаватель объяснял материал?' },
            { key: 'T4_Интерес', title: 'Интерес к предмету', description: 'Насколько интересно и увлекательно проходили лекции?' },
            { key: 'T5_Записи', title: 'Наличие записей', description: 'Были ли доступны видеозаписи лекций для повторения?' },
            { key: 'T6_Подготовка', title: 'Подготовка к экзамену', description: 'Насколько легко было готовиться к экзамену на основе материалов?' },
            { key: 'P2_Полезность', title: 'Полезность задач', description: 'Насколько задачи соответствуют реальным требованиям разработки?' },
            { key: 'P3_Сложность', title: 'Сложность задач', description: 'Насколько адекватна сложность задач вашему уровню?' },
            { key: 'P4_ИнтересЗадачи', title: 'Интерес к задачам', description: 'Насколько интересно было решать лабораторные работы?' },
            { key: 'P5_Настройка', title: 'Настройка окружения', description: 'Насколько легко было настроить IDE и компилятор?' },
            { key: 'P8_ОбратнаяСвязь', title: 'Обратная связь', description: 'Оцените качество комментариев от преподавателя к коду' },
            { key: 'P9_Скорость', title: 'Скорость проверки', description: 'Как быстро проверялись работы?' },
            { key: 'D2_Стресс', title: 'Стресс при защите', description: 'Насколько стрессовой была процедура защиты?', inverted: true },
            { key: 'D3_Объективность', title: 'Объективность оценки', description: 'Насколько объективно выставлялась оценка?' },
            { key: 'Прогресс', title: 'Отслеживание прогресса', description: 'Насколько хорошо вы могли отследить свой прогресс?' }
        ];
        
        // Calculate averages for each metric
        const metrics = metricsConfig.map(config => {
            const values = rows.map(row => parseFloat(row[config.key])).filter(v => !isNaN(v) && v >= 0);
            let avg = values.length > 0 ? values.reduce((sum, v) => sum + v, 0) / values.length : 0;
            
            // Invert score if needed (for stress - lower is better)
            if (config.inverted) {
                avg = 10 - avg;
            }
            
            return {
                title: config.title,
                description: config.description,
                avg: avg.toFixed(2)
            };
        });
        
        // Sort metrics from worst to best (ascending by avg score)
        metrics.sort((a, b) => parseFloat(a.avg) - parseFloat(b.avg));
        
        // Calculate overall average
        const overallAvg = metrics.reduce((sum, m) => sum + parseFloat(m.avg), 0) / metrics.length;
        
        // Hide loader button
        if (loader) {
            loader.style.display = 'none';
        }
        
        // Create overall rating card
        const overallCard = {
            title: 'Общий рейтинг',
            description: 'Среднее значение по всем метрикам',
            avg: overallAvg.toFixed(2)
        };
        
        // Show all metrics - 4 per row, with overall card at the end
        let html = '';
        const allCards = [...metrics, overallCard];
        
        for (let i = 0; i < allCards.length; i += 4) {
            html += '<div class="metric-row-simple">';
            for (let j = i; j < Math.min(i + 4, allCards.length); j++) {
                // Mark first 3 metrics as worst (but not the overall card)
                const isWorst = j < 3 && j < metrics.length;
                // Mark overall card specially
                const isOverall = j === allCards.length - 1;
                const card = allCards[j];
                
                if (isOverall) {
                    // Special styling for overall card
                    const score = parseFloat(card.avg);
                    const donatHTML = createDonatCircle(score);
                    const ratingLabel = getRatingLabel(score);
                    html += `
                        <div class="metric-card-donat overall-metric">
                            <div class="metric-text-content">
                                <div class="metric-title-text">${card.title}</div>
                                <div class="metric-description-text">${card.description}</div>
                            </div>
                            <div class="metric-donat-wrapper">
                                ${donatHTML}
                                <div class="metric-rating-label">${ratingLabel}</div>
                            </div>
                        </div>
                    `;
                } else {
                    html += createMetricCard(card, isWorst);
                }
            }
            html += '</div>';
        }
        
        container.innerHTML = html;
        console.log(`Loaded ${metrics.length} metrics + overall average (sorted from worst to best)`);
    } catch (error) {
        console.error('Error loading metrics:', error);
        if (loader) {
            loader.innerHTML = `
                <div style="color: #ef4444; text-align: center; padding: 15px;">
                    <p style="font-size: 0.9rem;">❌ Ошибка загрузки данных</p>
                    <p style="font-size: 0.8rem; margin-top: 8px;">Проверьте файл Результаты Опроса - Результаты Опроса.csv</p>
                    <button class="load-csv-btn" onclick="loadTraditionalMetrics()" style="margin-top: 12px;">
                        🔄 Попробовать снова
                    </button>
                </div>
            `;
        }
    }
}

// Auto-load on page load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', loadTraditionalMetrics);
} else {
    loadTraditionalMetrics();
}
