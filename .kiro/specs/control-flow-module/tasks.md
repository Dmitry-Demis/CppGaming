# План реализации: Модуль «Управление потоком выполнения программы»

## Обзор

Расширение главы 5 курса cppQuest: добавление 8 новых параграфов, примеров кода на C++, тестов и gated-контента для C++17. Реализация ведётся на HTML/CSS/JavaScript (фронтенд) и C++ (примеры кода).

## Задачи

- [x] 1. Обновить структуру курса в `course-meta.json`
  - Добавить в массив `paragraphs` главы 5 новые идентификаторы в порядке: `introduction-to-control-flow`, `if-statements`, `if-common-problems`, `switch-basics`, `switch-fallthrough`, `goto-statements`, `while-loops`, `do-while-loops`, `for-loops`, `break-and-continue`, `halts`
  - Сохранить существующие три параграфа без изменения их идентификаторов
  - _Требования: 1.1, 1.2, 1.3_

- [x] 2. Параграф «Оператор switch — основы»
  - [x] 2.1 Создать `cppQuest.Server/examples/chapter-5/control-flow/switch-basic.cpp`
    - Демонстрация `switch` с `break`, `return` и `default`
    - Использовать Brace_Init, не использовать `using namespace std` в глобальной области
    - _Требования: 2.3, 12.1, 12.2_
  - [x] 2.2 Создать `cppQuest.Server/tests/chapter-5/control-flow/switch-basics.json`
    - Не менее 3 вопросов: минимум один predict-the-output и один на выбор конструкции
    - Каждый вопрос содержит поле `explanation` с объяснением правильного ответа
    - _Требования: 2.4, 13.1, 13.3, 13.4_
  - [x] 2.3 Создать `CppCourse/theory/chapter-5/control-flow/switch-basics.html`
    - Объяснение синтаксиса `switch`, меток `case`, `default`, оператора `break`
    - Объяснение эффективности `switch` через jump table (без слов «просто» и «легко»)
    - Объяснение поведения при отсутствии совпадения и `default`
    - Объяснение ограничения типов: только целочисленные и перечислимые
    - Подключить `data-example="chapter-5/control-flow/switch-basic.cpp"`
    - Quiz_Widget с `data-final="switch-basics"`
    - _Требования: 2.1, 2.2, 2.5, 2.6, 12.5_

- [x] 3. Параграф «Fallthrough и область видимости в switch»
  - [x] 3.1 Создать `cppQuest.Server/examples/chapter-5/control-flow/switch-fallthrough.cpp`
    - Демонстрация непреднамеренного fallthrough с выводом нескольких строк
    - Использовать Brace_Init
    - _Требования: 3.2, 12.1, 12.2_
  - [x] 3.2 Создать `cppQuest.Server/examples/chapter-5/control-flow/switch-fallthrough-cpp17.cpp`
    - Демонстрация `[[fallthrough]]` и `switch(init; expr)` (C++17)
    - Использовать Brace_Init
    - _Требования: 11.3, 12.1, 12.2_
  - [x] 3.3 Создать `cppQuest.Server/tests/chapter-5/control-flow/switch-fallthrough.json`
    - Не менее 3 вопросов: минимум один predict-the-output и один на выбор конструкции
    - Каждый вопрос содержит поле `explanation`
    - _Требования: 3.5, 13.1, 13.3, 13.4_
  - [x] 3.4 Создать `cppQuest.Server/gated/chapter-5/control-flow/switch-fallthrough.html`
    - Секция `data-slot="1" data-std="17"`: объяснение `[[fallthrough]]` с примером
    - Секция `data-slot="2" data-std="17"`: объяснение `switch(init; expr)` с примером
    - Std_Badge `C++17` в каждой секции
    - _Требования: 11.1, 11.2, 11.3, 11.4_
  - [x] 3.5 Создать `CppCourse/theory/chapter-5/control-flow/switch-fallthrough.html`
    - Объяснение механизма fallthrough, его опасности и намеренного использования
    - Объяснение «стекирования» меток `case` для группировки значений
    - Объяснение правил объявления/инициализации переменных внутри `case`, рекомендация блока `{ }`
    - Gated-секция для C++17 с Std_Badge `C++17` и заглушкой для неоплативших
    - Quiz_Widget с `data-final="switch-fallthrough"`
    - _Требования: 3.1, 3.2, 3.3, 3.4, 3.6, 10.4_

- [x] 4. Параграф «Оператор goto»
  - [x] 4.1 Создать `cppQuest.Server/examples/chapter-5/control-flow/goto-nested-loops.cpp`
    - Единственное обоснованное применение `goto` — выход из вложенных циклов
    - Сравнение с альтернативой через флаг или функцию
    - Использовать Brace_Init
    - _Требования: 4.3, 12.1, 12.2_
  - [x] 4.2 Создать `cppQuest.Server/tests/chapter-5/control-flow/goto-statements.json`
    - Не менее 2 вопросов: минимум один predict-the-output и один на выбор конструкции
    - Каждый вопрос содержит поле `explanation`
    - _Требования: 4.4, 13.1, 13.3, 13.4_
  - [x] 4.3 Создать `CppCourse/theory/chapter-5/control-flow/goto-statements.html`
    - Объяснение синтаксиса `goto`, меток операторов, области видимости (function scope)
    - Ограничения: только в пределах одной функции, нельзя перепрыгивать через инициализацию
    - Явная рекомендация избегать `goto`, объяснение «спагетти-кода»
    - Quiz_Widget с `data-final="goto-statements"`
    - _Требования: 4.1, 4.2, 4.5, 12.5_

- [x] 5. Параграф «Цикл while»
  - [x] 5.1 Создать `cppQuest.Server/examples/chapter-5/control-flow/while-basic.cpp`
    - Демонстрация `while` с Brace_Init для переменных цикла
    - Пример беззнакового счётчика с переполнением (unsigned overflow)
    - _Требования: 5.4, 5.5, 12.1, 12.2_
  - [x] 5.2 Создать `cppQuest.Server/tests/chapter-5/control-flow/while-loops.json`
    - Не менее 3 вопросов: минимум один predict-the-output и один на выбор конструкции
    - Каждый вопрос содержит поле `explanation`
    - _Требования: 5.6, 13.1, 13.3, 13.4_
  - [x] 5.3 Создать `CppCourse/theory/chapter-5/control-flow/while-loops.html`
    - Объяснение синтаксиса `while`, условия завершения, понятия итерации
    - Случай ложного условия с самого начала (тело не выполняется)
    - Намеренный бесконечный цикл `while (true)` и способы выхода
    - Предупреждение об опасности беззнаковых счётчиков с примером
    - Quiz_Widget с `data-final="while-loops"`
    - _Требования: 5.1, 5.2, 5.3, 5.4, 12.5_

- [x] 6. Параграф «Цикл do-while»
  - [x] 6.1 Создать `cppQuest.Server/examples/chapter-5/control-flow/do-while-input.cpp`
    - Практическое применение `do-while` — валидация ввода пользователя
    - Использовать Brace_Init
    - _Требования: 6.2, 12.1, 12.2_
  - [x] 6.2 Создать `cppQuest.Server/tests/chapter-5/control-flow/do-while-loops.json`
    - Не менее 2 вопросов: минимум один predict-the-output и один на выбор конструкции
    - Каждый вопрос содержит поле `explanation`
    - _Требования: 6.4, 13.1, 13.3, 13.4_
  - [x] 6.3 Создать `CppCourse/theory/chapter-5/control-flow/do-while-loops.html`
    - Объяснение синтаксиса `do-while` и гарантии хотя бы одного выполнения тела
    - Сравнение `do-while` и `while`: когда предпочтителен каждый вариант
    - Quiz_Widget с `data-final="do-while-loops"`
    - _Требования: 6.1, 6.3, 12.5_

- [x] 7. Параграф «Цикл for»
  - [x] 7.1 Создать `cppQuest.Server/examples/chapter-5/control-flow/for-classic.cpp`
    - Классический `for` с тремя частями заголовка, вложенные циклы (таблица умножения)
    - Знаковый тип для счётчика, Brace_Init
    - _Требования: 7.1, 7.3, 7.4, 12.1, 12.2_
  - [x] 7.2 Создать `cppQuest.Server/examples/chapter-5/control-flow/for-range.cpp`
    - Range-based `for` для перебора последовательности
    - Использовать Brace_Init
    - _Требования: 7.2, 12.1, 12.2_
  - [x] 7.3 Создать `cppQuest.Server/tests/chapter-5/control-flow/for-loops.json`
    - Не менее 4 вопросов: минимум один predict-the-output и один на выбор конструкции
    - Каждый вопрос содержит поле `explanation`
    - _Требования: 7.6, 13.1, 13.3, 13.4_
  - [x] 7.4 Создать `CppCourse/theory/chapter-5/control-flow/for-loops.html`
    - Объяснение трёх частей заголовка `for` и их необязательности
    - Объяснение range-based `for` и его применения
    - Вложенные циклы с примером таблицы умножения
    - Рекомендация знаковых типов для счётчиков
    - Подключить оба примера: `for-classic.cpp` и `for-range.cpp`
    - Quiz_Widget с `data-final="for-loops"`
    - _Требования: 7.1, 7.2, 7.3, 7.4, 7.5, 12.5_

- [x] 8. Параграф «break и continue»
  - [x] 8.1 Создать `cppQuest.Server/examples/chapter-5/control-flow/break-continue.cpp`
    - `break` для выхода из бесконечного цикла, `continue` для пропуска итераций
    - Использовать Brace_Init
    - _Требования: 8.4, 12.1, 12.2_
  - [x] 8.2 Создать `cppQuest.Server/tests/chapter-5/control-flow/break-and-continue.json`
    - Не менее 3 вопросов: минимум один predict-the-output и один на выбор конструкции
    - Каждый вопрос содержит поле `explanation`
    - _Требования: 8.5, 13.1, 13.3, 13.4_
  - [x] 8.3 Создать `CppCourse/theory/chapter-5/control-flow/break-and-continue.html`
    - Объяснение семантики `break` (завершение цикла или `switch`) и `continue` (следующая итерация)
    - Разница между `break` и `return` в контексте цикла
    - Предупреждение об опасности `continue` в `while`/`do-while` при пропуске инкремента
    - Quiz_Widget с `data-final="break-and-continue"`
    - _Требования: 8.1, 8.2, 8.3, 12.5_

- [x] 9. Параграф «Завершение программы (halts)»
  - [x] 9.1 Создать `cppQuest.Server/examples/chapter-5/control-flow/halts-atexit.cpp`
    - Демонстрация `std::atexit` для регистрации функции очистки
    - Использовать Brace_Init
    - _Требования: 9.4, 12.1, 12.2_
  - [x] 9.2 Создать `cppQuest.Server/tests/chapter-5/control-flow/halts.json`
    - Не менее 2 вопросов: минимум один predict-the-output и один на выбор конструкции
    - Каждый вопрос содержит поле `explanation`
    - _Требования: 9.5, 13.1, 13.3, 13.4_
  - [x] 9.3 Создать `CppCourse/theory/chapter-5/control-flow/halts.html`
    - Объяснение `std::exit()`, `std::atexit()`, `std::abort()`, `std::terminate()`
    - Предупреждение: `std::exit()` не уничтожает локальные переменные
    - Разница между нормальным (`std::exit`) и аварийным (`std::abort`) завершением
    - Рекомендация использовать исключения вместо halt-функций
    - Quiz_Widget с `data-final="halts"`
    - _Требования: 9.1, 9.2, 9.3, 9.6, 12.5_

- [x] 10. Контрольная точка — проверить структуру и навигацию
  - Убедиться, что все Theory_Page главы 5 отображают корректную боковую навигацию (все 11 параграфов в правильном порядке)
  - Убедиться, что кнопки «Предыдущий параграф» / «Следующий параграф» работают корректно на всех страницах
  - Убедиться, что индикатор прогресса чтения присутствует на каждой Theory_Page
  - _Требования: 1.2, 14.1, 14.3_

- [x] 11. Gated-контент для C++17 — `if constexpr`
  - [x] 11.1 Создать `cppQuest.Server/examples/chapter-5/control-flow/if-constexpr.cpp`
    - Демонстрация `if constexpr` в шаблонной функции
    - Использовать Brace_Init
    - _Требования: 10.3, 12.1, 12.2_
  - [x] 11.2 Создать `cppQuest.Server/gated/chapter-5/control-flow/if-statements.html`
    - Секция `data-slot="1" data-std="17"` с объяснением `if constexpr`
    - Разница между `if constexpr` и обычным `if` с `constexpr`-условием
    - Std_Badge `C++17`, подключить `if-constexpr.cpp`
    - _Требования: 10.1, 10.2, 10.5_
  - [x] 11.3 Убедиться, что в `CppCourse/theory/chapter-5/control-flow/if-statements.html` подключён gated-слот
    - Заглушка «Доступно в расширенной версии» отображается для неоплативших
    - _Требования: 10.4_

- [x] 12. Финальная контрольная точка
  - Убедиться, что все тесты (JSON) содержат корректный формат и загружаются Quiz_Widget
  - Убедиться, что все `data-example` ссылки в Theory_Page указывают на существующие `.cpp` файлы
  - Убедиться, что все примеры кода используют Brace_Init и не содержат `using namespace std` в глобальной области
  - Убедиться, что все русскоязычные тексты используют букву «ё» там, где она требуется
  - _Требования: 12.1, 12.2, 12.3, 12.5, 13.2_

## Примечания

- Задачи с `*` — опциональные, могут быть пропущены для ускорения MVP
- Каждая задача ссылается на конкретные требования для трассируемости
- Все примеры кода должны соответствовать стилю существующих файлов в `cppQuest.Server/examples/chapter-5/control-flow/`
- Gated-контент подключается через механизм `std-gate.js` аналогично главе 1
