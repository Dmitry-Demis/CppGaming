// ============================================
// COURSE LOADER
// Fetches pre-built course structure from /api/course-structure.
// Titles are resolved server-side at startup — no per-paragraph HTML fetches.
// ============================================

let _courseCache = null;

/**
 * Render inline backtick code spans: `foo` → <code>foo</code>
 * @param {string} text
 * @returns {string}
 */
function renderInlineCode(text) {
    if (!text) return text;
    return text.replace(/`([^`]+)`/g, '<code>$1</code>');
}

/**
 * @param {string} [_base=''] - ignored, kept for API compatibility
 * @returns {Promise<{chapters: Array}>}
 */
async function loadCourseStructure(_base = '') {
    if (_courseCache) return _courseCache;

    const res = await fetch('/api/course-structure');
    if (!res.ok) throw new Error('course-structure unavailable');
    const data = await res.json();

    // Apply inline code rendering to titles/descriptions
    for (const ch of data.chapters || []) {
        if (ch.description) ch.description = renderInlineCode(ch.description);
        for (const p of ch.paragraphs || []) {
            if (p.title) p.title = renderInlineCode(p.title);
        }
    }

    _courseCache = data;
    return data;
}
