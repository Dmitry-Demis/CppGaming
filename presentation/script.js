// Presentation Navigation Script

let currentSlide = 1;
const totalSlides = 13;

// Get elements
const slides = document.querySelectorAll('.slide');
const prevBtn = document.getElementById('prevBtn');
const nextBtn = document.getElementById('nextBtn');
const slideCounter = document.getElementById('slideCounter');

// Initialize
updateSlide();

// Event listeners
prevBtn.addEventListener('click', () => {
    if (currentSlide > 1) {
        currentSlide--;
        updateSlide();
    }
});

nextBtn.addEventListener('click', () => {
    if (currentSlide < totalSlides) {
        currentSlide++;
        updateSlide();
    }
});

// Keyboard navigation
document.addEventListener('keydown', (e) => {
    if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') {
        if (currentSlide > 1) {
            currentSlide--;
            updateSlide();
        }
    } else if (e.key === 'ArrowRight' || e.key === 'ArrowDown' || e.key === ' ') {
        e.preventDefault();
        if (currentSlide < totalSlides) {
            currentSlide++;
            updateSlide();
        }
    } else if (e.key === 'Home') {
        currentSlide = 1;
        updateSlide();
    } else if (e.key === 'End') {
        currentSlide = totalSlides;
        updateSlide();
    }
});

// Touch/swipe support
let touchStartX = 0;
let touchEndX = 0;

document.addEventListener('touchstart', (e) => {
    touchStartX = e.changedTouches[0].screenX;
});

document.addEventListener('touchend', (e) => {
    touchEndX = e.changedTouches[0].screenX;
    handleSwipe();
});

function handleSwipe() {
    const swipeThreshold = 50;
    const diff = touchStartX - touchEndX;
    
    if (Math.abs(diff) > swipeThreshold) {
        if (diff > 0 && currentSlide < totalSlides) {
            // Swipe left - next slide
            currentSlide++;
            updateSlide();
        } else if (diff < 0 && currentSlide > 1) {
            // Swipe right - previous slide
            currentSlide--;
            updateSlide();
        }
    }
}

// Update slide display
function updateSlide() {
    // Hide all slides
    slides.forEach(slide => {
        slide.classList.remove('active');
    });
    
    // Show current slide
    const currentSlideElement = document.getElementById(`slide-${currentSlide}`);
    if (currentSlideElement) {
        currentSlideElement.classList.add('active');
    }
    
    // Update counter
    slideCounter.textContent = `${currentSlide} / ${totalSlides}`;
    
    // Update button states
    prevBtn.disabled = currentSlide === 1;
    nextBtn.disabled = currentSlide === totalSlides;
    
    // Update URL hash (for bookmarking)
    window.location.hash = `slide-${currentSlide}`;
}

// Handle direct URL navigation
window.addEventListener('load', () => {
    const hash = window.location.hash;
    if (hash) {
        const slideNum = parseInt(hash.replace('#slide-', ''));
        if (slideNum >= 1 && slideNum <= totalSlides) {
            currentSlide = slideNum;
            updateSlide();
        }
    }
});

// Fullscreen toggle (F key)
document.addEventListener('keydown', (e) => {
    if (e.key === 'f' || e.key === 'F') {
        toggleFullscreen();
    }
});

function toggleFullscreen() {
    if (!document.fullscreenElement) {
        document.documentElement.requestFullscreen().catch(err => {
            console.log('Fullscreen error:', err);
        });
    } else {
        if (document.exitFullscreen) {
            document.exitFullscreen();
        }
    }
}

// Print all slides (P key)
document.addEventListener('keydown', (e) => {
    if (e.key === 'p' || e.key === 'P') {
        e.preventDefault();
        window.print();
    }
});

// Slide overview (O key)
document.addEventListener('keydown', (e) => {
    if (e.key === 'o' || e.key === 'O') {
        showOverview();
    }
});

function showOverview() {
    alert('Slide Overview:\n\n' +
        '1. Титульный слайд\n' +
        '2. Актуальность и цель\n' +
        '3. Анализ и постановка задачи\n' +
        '4. Архитектура — Контекст\n' +
        '5. Архитектура — Контейнеры\n' +
        '6. Результаты — Эффективность\n' +
        '7. Результаты — UX и Интерактивность\n' +
        '8. Отзывы пользователей\n' +
        '9. Текущая реализация\n' +
        '10. План работ\n' +
        '11. Заключение\n' +
        '12. [Запасной] Технологический стек\n' +
        '13. [Запасной] Детальная статистика\n\n' +
        'Горячие клавиши:\n' +
        '← → : Навигация\n' +
        'Home/End : Первый/последний слайд\n' +
        'F : Полноэкранный режим\n' +
        'P : Печать\n' +
        'O : Обзор слайдов'
    );
}

// Show keyboard shortcuts on ? key
document.addEventListener('keydown', (e) => {
    if (e.key === '?') {
        showOverview();
    }
});

console.log('Cpp Quest Presentation loaded');
console.log('Keyboard shortcuts: ← → (navigate), F (fullscreen), P (print), O (overview)');
