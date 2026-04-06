// Survey Data Processor for C++ Quest Survey

// Parse CSV file
function parseCSV(text) {
    const lines = text.trim().split('\n');
    const headers = lines[0].split(',');
    const data = [];
    
    for (let i = 1; i < lines.length; i++) {
        const values = lines[i].split(',');
        const obj = {};
        headers.forEach((header, index) => {
            obj[header.trim()] = values[index] ? values[index].trim() : '';
        });
        data.push(obj);
    }
    
    return data;
}

// Calculate demographics
function calculateDemographics(data) {
    const demographics = {
        totalResponses: data.length,
        ageGroups: {},
        universities: {},
        gender: {},
        courses: {},
        cppExperience: {}
    };
    
    data.forEach(row => {
        // Age groups
        const age = parseInt(row['Возраст']);
        const ageGroup = age < 20 ? '< 20' : age < 25 ? '20-24' : '25+';
        demographics.ageGroups[ageGroup] = (demographics.ageGroups[ageGroup] || 0) + 1;
        
        // Universities
        const uni = row['ВУЗ'];
        demographics.universities[uni] = (demographics.universities[uni] || 0) + 1;
        
        // Gender
        const gender = row['Пол'];
        demographics.gender[gender] = (demographics.gender[gender] || 0) + 1;
        
        // Course
        const course = row['Курс'];
        demographics.courses[course] = (demographics.courses[course] || 0) + 1;
        
        // C++ Experience
        const exp = row['Опыт C++'];
        demographics.cppExperience[exp] = (demographics.cppExperience[exp] || 0) + 1;
    });
    
    return demographics;
}

// Calculate comparison metrics (ВУЗ vs Онлайн vs Сайт)
function calculateComparisons(data) {
    const comparisons = {
        structure: { vuz: [], online: [], site: [] },
        gaps: { vuz: [], online: [], site: [] },
        repetition: { vuz: [], online: [], site: [] },
        feedback: { vuz: [], online: [], site: [] }
    };
    
    data.forEach(row => {
        // Cmp1_Структура
        const structure = row['Cmp1_Структура(ВУЗ;Онлайн;Сайт)'].split(';').map(v => parseFloat(v.trim()));
        if (structure.length === 3) {
            comparisons.structure.vuz.push(structure[0]);
            comparisons.structure.online.push(structure[1]);
            comparisons.structure.site.push(structure[2]);
        }
        
        // Cmp2_Пробелы
        const gaps = row['Cmp2_Пробелы(ВУЗ;Онлайн;Сайт)'].split(';').map(v => parseFloat(v.trim()));
        if (gaps.length === 3) {
            comparisons.gaps.vuz.push(gaps[0]);
            comparisons.gaps.online.push(gaps[1]);
            comparisons.gaps.site.push(gaps[2]);
        }
        
        // Cmp3_Повторение
        const repetition = row['Cmp3_Повторение(ВУЗ;Онлайн;Сайт)'].split(';').map(v => parseFloat(v.trim()));
        if (repetition.length === 3) {
            comparisons.repetition.vuz.push(repetition[0]);
            comparisons.repetition.online.push(repetition[1]);
            comparisons.repetition.site.push(repetition[2]);
        }
        
        // Cmp4_Фидбек
        const feedback = row['Cmp4_Фидбек(ВУЗ;Онлайн;Сайт)'].split(';').map(v => parseFloat(v.trim()));
        if (feedback.length === 3) {
            comparisons.feedback.vuz.push(feedback[0]);
            comparisons.feedback.online.push(feedback[1]);
            comparisons.feedback.site.push(feedback[2]);
        }
    });
    
    // Calculate averages
    const avg = arr => arr.reduce((a, b) => a + b, 0) / arr.length;
    
    return {
        structure: {
            vuz: avg(comparisons.structure.vuz).toFixed(1),
            online: avg(comparisons.structure.online).toFixed(1),
            site: avg(comparisons.structure.site).toFixed(1)
        },
        gaps: {
            vuz: avg(comparisons.gaps.vuz).toFixed(1),
            online: avg(comparisons.gaps.online).toFixed(1),
            site: avg(comparisons.gaps.site).toFixed(1)
        },
        repetition: {
            vuz: avg(comparisons.repetition.vuz).toFixed(1),
            online: avg(comparisons.repetition.online).toFixed(1),
            site: avg(comparisons.repetition.site).toFixed(1)
        },
        feedback: {
            vuz: avg(comparisons.feedback.vuz).toFixed(1),
            online: avg(comparisons.feedback.online).toFixed(1),
            site: avg(comparisons.feedback.site).toFixed(1)
        }
    };
}

// Calculate theory metrics
function calculateTheoryMetrics(data) {
    const avg = arr => arr.reduce((a, b) => a + b, 0) / arr.length;
    
    const coverage = data.map(r => parseFloat(r['T1_Покрытие'])).filter(v => !isNaN(v));
    const goodBad = data.map(r => parseFloat(r['T2_ХорошоПлохо'])).filter(v => !isNaN(v));
    const visualizations = data.map(r => parseFloat(r['T3_Визуализации'])).filter(v => !isNaN(v));
    
    return {
        coverage: avg(coverage).toFixed(1),
        goodBad: avg(goodBad).toFixed(1),
        visualizations: avg(visualizations).toFixed(1)
    };
}

// Calculate gamification metrics
function calculateGamificationMetrics(data) {
    const avg = arr => arr.reduce((a, b) => a + b, 0) / arr.length;
    
    const coins = data.map(r => parseFloat(r['G1_ОпытМонетки'])).filter(v => !isNaN(v));
    const streaks = data.map(r => parseFloat(r['G2_Стрики'])).filter(v => !isNaN(v));
    const stats = data.map(r => parseFloat(r['G3_Статистика'])).filter(v => !isNaN(v));
    
    return {
        coins: avg(coins).toFixed(1),
        streaks: avg(streaks).toFixed(1),
        stats: avg(stats).toFixed(1)
    };
}

// Calculate satisfaction metrics
function calculateSatisfactionMetrics(data) {
    const avg = arr => arr.reduce((a, b) => a + b, 0) / arr.length;
    
    const satisfaction = data.map(r => parseFloat(r['R1_Удовлетворённость'])).filter(v => !isNaN(v));
    
    return {
        satisfaction: avg(satisfaction).toFixed(1)
    };
}

// Load and process survey
async function loadSurveyData() {
    try {
        const response = await fetch('Опрос. Cpp Quest - Опрос. Cpp Quest.csv');
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const text = await response.text();
        const data = parseCSV(text);
        
        const demographics = calculateDemographics(data);
        const comparisons = calculateComparisons(data);
        const theory = calculateTheoryMetrics(data);
        const gamification = calculateGamificationMetrics(data);
        const satisfaction = calculateSatisfactionMetrics(data);
        
        return {
            demographics,
            comparisons,
            theory,
            gamification,
            satisfaction,
            rawData: data
        };
    } catch (error) {
        console.error('Error loading survey data:', error);
        return null;
    }
}

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { loadSurveyData, parseCSV, calculateDemographics };
}
