/* ============================================
   C++ SYNTAX HIGHLIGHTER
   ============================================ */

class CppHighlighter {
    constructor() {
        this.keywords = [
            'alignas', 'alignof', 'and', 'and_eq', 'asm', 'auto', 'bitand', 'bitor',
            'bool', 'break', 'case', 'catch', 'char', 'char8_t', 'char16_t', 'char32_t',
            'class', 'compl', 'concept', 'const', 'consteval', 'constexpr', 'constinit',
            'const_cast', 'continue', 'co_await', 'co_return', 'co_yield', 'decltype',
            'default', 'delete', 'do', 'double', 'dynamic_cast', 'else', 'enum', 'explicit',
            'export', 'extern', 'false', 'float', 'for', 'friend', 'goto', 'if', 'inline',
            'int', 'long', 'mutable', 'namespace', 'new', 'noexcept', 'not', 'not_eq',
            'nullptr', 'operator', 'or', 'or_eq', 'private', 'protected', 'public',
            'register', 'reinterpret_cast', 'requires', 'return', 'short', 'signed',
            'sizeof', 'static', 'static_assert', 'static_cast', 'struct', 'switch',
            'template', 'this', 'thread_local', 'throw', 'true', 'try', 'typedef',
            'typeid', 'typename', 'union', 'unsigned', 'using', 'virtual', 'void',
            'volatile', 'wchar_t', 'while', 'xor', 'xor_eq'
        ];

        this.types = [
            'std::string', 'std::vector', 'std::map', 'std::set', 'std::array',
            'std::cout', 'std::cin', 'std::endl', 'std::setprecision', 'std::numeric_limits',
            'size_t', 'int8_t', 'int16_t', 'int32_t', 'int64_t',
            'uint8_t', 'uint16_t', 'uint32_t', 'uint64_t'
        ];

        this.preprocessor = ['#include', '#define', '#ifdef', '#ifndef', '#endif', '#pragma'];
    }

    _esc(s) {
        return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    // Escape a string for use in RegExp
    _reEsc(s) {
        let out = '';
        for (const ch of s) {
            out += /[.*+?^${}()|[\]\\]/.test(ch) ? ('\\' + ch) : ch;
        }
        return out;
    }

    highlight(code) {
        // \uE000/\uE001 are Unicode private-use area chars — safe markers
        const store = [];
        const save = (html) => { store.push(html); return '\uE000' + (store.length - 1) + '\uE001'; };
        const restore = (s) => s.replace(/\uE000(\d+)\uE001/g, (_, i) => store[+i]);

        let h = code.replace(/\r\n/g, '\n').replace(/\r/g, '\n');

        // 1. Line comments (before HTML escaping so < > inside are raw)
        h = h.replace(/\/\/[^\n]*/g, m => save('<span class="token comment">' + this._esc(m) + '</span>'));

        // 2. Block comments
        h = h.replace(/\/\*[\s\S]*?\*\//g, m => save('<span class="token comment">' + this._esc(m) + '</span>'));

        // 3. String literals
        h = h.replace(/"(?:[^"\\\n]|\\.)*"/g, m => save('<span class="token string">' + this._esc(m) + '</span>'));

        // 4. Preprocessor directives (before escaping so #include <...> works)
        this.preprocessor.forEach(function(prep) {
            const re = new RegExp(prep.replace('#', '\\#') + '(?:\\s*<[^>]*>)?', 'g');
            h = h.replace(re, function(m) { return save('<span class="token preprocessor">' + this._esc(m) + '</span>'); }.bind(this));
        }.bind(this));

        // 5. Escape remaining HTML special chars
        h = h.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

        // 6. Numbers — replace digits not inside store markers
        h = h.replace(/\uE000\d+\uE001|(\b\d+\.?\d*[fFLuUlL]?\b)/g, function(m, num) {
            return num ? save('<span class="token number">' + num + '</span>') : m;
        });

        // 7. Types (longest first)
        var self = this;
        var sortedTypes = this.types.slice().sort(function(a, b) { return b.length - a.length; });
        sortedTypes.forEach(function(type) {
            var e = self._reEsc(type);
            h = h.replace(new RegExp('(?<![\\w:])' + e + '(?![\\w:])', 'g'), function(m) {
                return save('<span class="token type">' + m + '</span>');
            });
        });

        // 8. Keywords
        this.keywords.forEach(function(kw) {
            h = h.replace(new RegExp('\\b' + kw + '\\b', 'g'), function(m) {
                return save('<span class="token keyword">' + m + '</span>');
            });
        });

        // 9. Function calls
        h = h.replace(/\b([a-zA-Z_]\w*)\s*(?=\()/g, function(_, name) {
            return save('<span class="token function">' + name + '</span>');
        });

        // 10. Operators (after HTML escaping, so << appears as &lt;&lt;)
        h = h.replace(/(?:&lt;|&gt;|[+\-*/%=!&|^~])/g, function(m) {
            return save('<span class="token operator">' + m + '</span>');
        });

        return restore(h);
    }

    dedent(code) {
        const lines = code.split('\n');
        while (lines.length && !lines[0].trim()) lines.shift();
        while (lines.length && !lines[lines.length - 1].trim()) lines.pop();
        const minIndent = lines.reduce(function(min, line) {
            if (!line.trim()) return min;
            return Math.min(min, line.match(/^(\s*)/)[1].length);
        }, Infinity);
        const cut = minIndent === Infinity ? 0 : minIndent;
        return lines.map(function(line) { return line.slice(cut); }).join('\n');
    }

    addLineNumbers(codeEl) {
        const lines = codeEl.innerHTML.split('\n');
        const nums = lines.map(() => '<span></span>').join('');
        const wrapper = document.createElement('div');
        wrapper.className = 'line-numbers';
        wrapper.innerHTML = '<div class="line-numbers-rows">' + nums + '</div>';
        wrapper.appendChild(codeEl.cloneNode(true));
        codeEl.parentNode.replaceChild(wrapper, codeEl);
    }

    /**
     * highlighter.renderCode(label, code)
     * Call inline in a <script> tag — inserts code block at that exact position.
     * Use String.raw`...` to pass verbatim C++ code (so \n stays as \n).
     */
    renderCode(label, code) {
        var script = document.currentScript;
        if (!script) return;
        var highlighted = this.highlight(this.dedent(code.replace(/^\n+/, '').replace(/\n+$/, '')));
        var div = document.createElement('div');
        div.className = 'code-block';
        div.innerHTML =
            '<div class="code-header">' +
                '<span class="code-lang">' + label + '</span>' +
                '<div class="code-actions">' +
                    '<button class="btn-code" onclick="copyCode(this)">Копировать</button>' +
                '</div>' +
            '</div>' +
            '<div class="code-content">' +
                '<pre><code class="language-cpp highlighted">' + highlighted + '</code></pre>' +
            '</div>';
        script.replaceWith(div);
        var codeEl = div.querySelector('code');
        if (codeEl) this.addLineNumbers(codeEl);
    }

    /**
     * Renders <template data-label="..."> blocks.
     * Write code completely verbatim — no escaping needed.
     */
    renderScriptBlocks() {
        var self = this;
        document.querySelectorAll('template[data-label]').forEach(function(tmpl) {
            var raw = tmpl.content.textContent.replace(/\r\n/g, '\n').replace(/\r/g, '\n');
            var code = self.dedent(raw.replace(/^\n+/, '').replace(/\n+$/, ''));
            var label = tmpl.dataset.label || 'C++ — пример';
            var highlighted = self.highlight(code);

            var div = document.createElement('div');
            div.className = 'code-block';
            if (tmpl.dataset.example) div.dataset.example = tmpl.dataset.example;
            div.innerHTML =
                '<div class="code-header">' +
                    '<span class="code-lang">' + label + '</span>' +
                    '<div class="code-actions">' +
                        '<button class="btn-code" onclick="copyCode(this)">Копировать</button>' +
                    '</div>' +
                '</div>' +
                '<div class="code-content">' +
                    '<pre><code class="language-cpp highlighted">' + highlighted + '</code></pre>' +
                '</div>';

            var codeEl = div.querySelector('code');
            if (codeEl) self.addLineNumbers(codeEl);

            tmpl.replaceWith(div);
        });
    }

    highlightAll() {
        var self = this;
        document.querySelectorAll('pre code:not(.highlighted)').forEach(function(block) {
            var raw = block.textContent;
            var m = raw.match(/^\s*```\w*\n?([\s\S]*)```\s*$/);
            var code = m ? m[1].replace(/^\n/, '').replace(/\n$/, '') : self.dedent(raw);
            block.innerHTML = self.highlight(code);
            block.classList.add('highlighted');
            self.addLineNumbers(block);
        });
    }
}

function copyCode(btn) {
    var code = (btn.closest('.code-block') || btn.closest('pre'));
    var text = code ? (code.querySelector('code') || code).textContent : '';
    navigator.clipboard.writeText(text).then(function() {
        btn.textContent = 'Скопировано!';
        setTimeout(function() { btn.textContent = 'Копировать'; }, 2000);
    });
}

var highlighter = new CppHighlighter();
document.addEventListener('DOMContentLoaded', function() {
    highlighter.renderScriptBlocks();
    highlighter.highlightAll();
});
