// Script to calculate averages from survey data
// Run this in browser console to generate CSV from survey data

async function calculateAverages() {
    const response = await fetch('Результаты Опроса - Результаты Опроса.csv');
    const text = await response.text();
    const lines = text.trim().split('\n');
    const headers = lines[0].split(',');
    
    // Find column indices
    const metrics = {
        'T1_Материалы': 'Доступность материалов',
        'T2_Понятность': 'Понятность подачи',
        'T3_Преподаватель': 'Компетентность преподавателя',
        'T4_Интерес': 'Интерес к предмету',
        'T5_Записи': 'Наличие записей',
        'T6_Подготовка': 'Подготовка к экзамену',
        'P2_Полезность': 'Полезность задач',
        'P3_Сложность': 'Сложность задач',
        'P4_ИнтересЗадачи': 'Интерес к задачам',
        'P5_Настройка': 'Настройка окружения',
        'P8_ОбратнаяСвязь': 'Обратная связь',
        'P9_Скорость': 'Скорость проверки',
        'D3_Объективность': 'Объективность оценки',
        'D2_Стресс': 'Стресс при защите',
        'Прогресс': 'Отслеживание прогресса'
    };
    
    const descriptions = {
        'T1_Материалы': 'Были ли все лекции презентации и методички доступны в одном удобном месте',
        'T2_Понятность': 'Было ли понятно из лекций что именно нужно записывать и учить',
        'T3_Преподаватель': 'Насколько хорошо преподаватель объяснял материал',
        'T4_Интерес': 'Насколько интересно и увлекательно проходили лекции',
        'T5_Записи': 'Были ли доступны видеозаписи лекций для повторения',
        'T6_Подготовка': 'Насколько легко было готовиться к экзамену на основе материалов',
        'P2_Полезность': 'Насколько задачи соответствуют реальным требованиям разработки',
        'P3_Сложность': 'Насколько адекватна сложность задач вашему уровню',
        'P4_ИнтересЗадачи': 'Насколько интересно было решать лабораторные работы',
        'P5_Настройка': 'Насколько легко было настроить IDE и компилятор',
        'P8_ОбратнаяСвязь': 'Оцените качество комментариев от преподавателя к коду',
        'P9_Скорость': 'Как быстро проверялись работы',
        'D3_Объективность': 'Насколько объективно выставлялась оценка',
        'D2_Стресс': 'Насколько стрессовой была процедура защиты',
        'Прогресс': 'Насколько хорошо вы могли отследить свой прогресс и выявить пробелы в знаниях'
    };
    
    const results = {};
    
    // Calculate averages
    for (const [key, title] of Object.entries(metrics)) {
        const colIndex = headers.indexOf(key);
        if (colIndex === -1) continue;
        
        let sum = 0;
        let count = 0;
        
        for (let i = 1; i < lines.length; i++) {
            const values = lines[i].split(',');
            const value = parseFloat(values[colIndex]);
            if (!isNaN(value)) {
                sum += value;
                count++;
            }
        }
        
        if (count > 0) {
            let avg = sum / count;
            
            // Инвертируем значение для стресса (чем меньше стресс, тем лучше)
            if (key === 'D2_Стресс') {
                avg = 10 - avg;
            }
            
            results[key] = {
                title: title,
                description: descriptions[key],
                avg: avg.toFixed(1)
            };
        }
    }
    
    // Generate CSV
    let csv = 'title,description,avg\n';
    for (const data of Object.values(results)) {
        csv += `${data.title},${data.description},${data.avg}\n`;
    }
    
    console.log(csv);
    return csv;
}

// Run and copy result
calculateAverages().then(csv => {
    console.log('Copy this CSV to data/traditional-metrics.csv:');
    console.log(csv);
});
