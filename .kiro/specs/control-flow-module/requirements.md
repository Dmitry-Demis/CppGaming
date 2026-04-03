# Требования: Модуль «Управление потоком выполнения программы»

## Введение

Модуль расширяет главу 5 («Управление потоком») веб-курса «Современный C++» (cppQuest).
Текущая глава содержит только три параграфа (введение, `if`-операторы, типичные ошибки `if`).
Необходимо добавить полный набор параграфов, охватывающих `switch`, циклы, прыжки и завершение программы,
с локализацией на русский язык, примерами кода, тестами и gated-контентом для стандартов C++17/20/23.

---

## Глоссарий

- **Course_System** — веб-приложение cppQuest (фронтенд CppCourse + бэкенд cppQuest.Server).
- **Theory_Page** — HTML-страница теории по пути `CppCourse/theory/chapter-N/groupId/paragraphId.html`.
- **Gated_Content** — платный HTML-контент в `cppQuest.Server/gated/`, доступный после покупки стандарта C++ в магазине.
- **Example_File** — файл `.cpp` в `cppQuest.Server/examples/chapter-N/.../*.cpp`, отображаемый на Theory_Page через `data-example`.
- **Test_File** — JSON-файл в `cppQuest.Server/tests/chapter-N/.../*.json` с вопросами для Quiz_Widget.
- **Quiz_Widget** — компонент `<div class="quiz-widget" data-final="...">` на Theory_Page, загружающий Test_File.
- **Std_Badge** — визуальная метка `<span class="std-badge std-badge--NN">C++NN</span>` рядом с gated-материалом.
- **Paragraph** — учебная единица: один параграф = одна Theory_Page + один или несколько Example_File + один Test_File.
- **Chapter_5** — глава «Управление потоком», groupId = `control-flow`, путь `chapter-5/control-flow/`.
- **course-meta.json** — файл структуры курса, определяющий порядок параграфов в навигации.
- **Brace_Init** — стиль инициализации переменных через фигурные скобки: `int x {42};`.
- **Fallthrough** — поведение `switch`, при котором выполнение переходит в следующий `case` без `break`.
- **Constexpr_If** — конструкция `if constexpr (...)`, вычисляемая на этапе компиляции (C++17).

---

## Требования

### Требование 1: Расширение структуры курса

**User Story:** Как студент, я хочу видеть полный набор тем по управлению потоком в навигации курса, чтобы последовательно изучить все конструкции C++.

#### Acceptance Criteria

1. THE Course_System SHALL добавить в `course-meta.json` для `chapter-5` параграфы в следующем порядке: `introduction-to-control-flow`, `if-statements`, `if-common-problems`, `switch-basics`, `switch-fallthrough`, `goto-statements`, `while-loops`, `do-while-loops`, `for-loops`, `break-and-continue`, `halts`.
2. WHEN студент открывает любую Theory_Page главы 5, THE Course_System SHALL отображать в боковой навигации все параграфы из пункта 1 в указанном порядке.
3. THE Course_System SHALL сохранить существующие параграфы `introduction-to-control-flow`, `if-statements`, `if-common-problems` без изменения их идентификаторов.

---

### Требование 2: Параграф «Оператор switch — основы»

**User Story:** Как студент, я хочу понять синтаксис и семантику `switch`, чтобы писать читаемый код ветвления по целочисленным значениям.

#### Acceptance Criteria

1. THE Course_System SHALL создать Theory_Page `chapter-5/control-flow/switch-basics.html` с объяснением синтаксиса `switch`, меток `case`, метки `default` и оператора `break`.
2. WHEN студент читает параграф, THE Theory_Page SHALL объяснять, почему `switch` может быть эффективнее цепочки `if-else` (jump table), используя строгий инженерный стиль без вводных слов «просто» и «легко».
3. THE Theory_Page SHALL содержать не менее одного Example_File с демонстрацией `switch` с `break`, `return` и `default`, использующего Brace_Init и не использующего `using namespace std` в глобальной области.
4. THE Theory_Page SHALL содержать Quiz_Widget, ссылающийся на Test_File `chapter-5/control-flow/switch-basics.json` с не менее чем 3 вопросами.
5. IF значение выражения `switch` не совпадает ни с одним `case` и отсутствует `default`, THEN THE Theory_Page SHALL объяснять, что выполнение продолжается после блока `switch`.
6. THE Theory_Page SHALL объяснять ограничение типов в `switch`: только целочисленные и перечислимые типы.

---

### Требование 3: Параграф «Fallthrough и область видимости в switch»

**User Story:** Как студент, я хочу понять поведение fallthrough и правила объявления переменных в `switch`, чтобы избегать скрытых ошибок.

#### Acceptance Criteria

1. THE Course_System SHALL создать Theory_Page `chapter-5/control-flow/switch-fallthrough.html` с объяснением механизма Fallthrough, его опасности и намеренного использования.
2. THE Theory_Page SHALL содержать Example_File, демонстрирующий непреднамеренный Fallthrough с выводом нескольких строк при совпадении одного `case`.
3. THE Theory_Page SHALL объяснять технику «стекирования» меток `case` (sequential case labels) для группировки значений без Fallthrough.
4. THE Theory_Page SHALL объяснять правила объявления и инициализации переменных внутри `case`: объявление допустимо, инициализация в не-последнем `case` — нет; рекомендуется использовать явный блок `{ }`.
5. THE Theory_Page SHALL содержать Quiz_Widget, ссылающийся на Test_File `chapter-5/control-flow/switch-fallthrough.json` с не менее чем 3 вопросами.
6. THE Theory_Page SHALL содержать Gated_Content-секцию для C++17 с объяснением атрибута `[[fallthrough]]` и примером его использования, помеченную Std_Badge `C++17`.

---

### Требование 4: Параграф «Оператор goto»

**User Story:** Как студент, я хочу знать, что такое `goto` и почему его избегают, чтобы понимать исторический контекст и не использовать его в новом коде.

#### Acceptance Criteria

1. THE Course_System SHALL создать Theory_Page `chapter-5/control-flow/goto-statements.html` с объяснением синтаксиса `goto`, меток операторов и области видимости меток (function scope).
2. THE Theory_Page SHALL объяснять ограничения `goto`: только в пределах одной функции, нельзя перепрыгивать через инициализацию переменных, остающихся в области видимости.
3. THE Theory_Page SHALL содержать Example_File с единственным обоснованным применением `goto` — выходом из вложенных циклов — и сравнением с альтернативой через флаг или функцию.
4. THE Theory_Page SHALL содержать Quiz_Widget, ссылающийся на Test_File `chapter-5/control-flow/goto-statements.json` с не менее чем 2 вопросами.
5. THE Theory_Page SHALL явно рекомендовать избегать `goto` и объяснять понятие «спагетти-код».

---

### Требование 5: Параграф «Цикл while»

**User Story:** Как студент, я хочу освоить цикл `while`, чтобы писать программы с повторяющимися действиями при неизвестном заранее числе итераций.

#### Acceptance Criteria

1. THE Course_System SHALL создать Theory_Page `chapter-5/control-flow/while-loops.html` с объяснением синтаксиса `while`, условия завершения и понятия итерации.
2. THE Theory_Page SHALL объяснять случай, когда условие ложно с самого начала (тело цикла не выполняется ни разу).
3. THE Theory_Page SHALL объяснять намеренный бесконечный цикл `while (true)` и способы выхода из него.
4. THE Theory_Page SHALL предупреждать об опасности беззнаковых переменных-счётчиков (unsigned loop variable overflow) с примером.
5. THE Theory_Page SHALL содержать не менее одного Example_File, использующего Brace_Init для переменных цикла.
6. THE Theory_Page SHALL содержать Quiz_Widget, ссылающийся на Test_File `chapter-5/control-flow/while-loops.json` с не менее чем 3 вопросами.

---

### Требование 6: Параграф «Цикл do-while»

**User Story:** Как студент, я хочу понять отличие `do-while` от `while`, чтобы выбирать подходящую конструкцию для задач с гарантированным первым выполнением.

#### Acceptance Criteria

1. THE Course_System SHALL создать Theory_Page `chapter-5/control-flow/do-while-loops.html` с объяснением синтаксиса `do-while` и гарантии хотя бы одного выполнения тела.
2. THE Theory_Page SHALL содержать Example_File с практическим применением `do-while` — валидацией ввода пользователя.
3. THE Theory_Page SHALL сравнивать `do-while` и `while`, объясняя, когда предпочтителен каждый вариант.
4. THE Theory_Page SHALL содержать Quiz_Widget, ссылающийся на Test_File `chapter-5/control-flow/do-while-loops.json` с не менее чем 2 вопросами.

---

### Требование 7: Параграф «Цикл for»

**User Story:** Как студент, я хочу освоить цикл `for` и его варианты, чтобы писать компактный и читаемый код для итерации по диапазонам.

#### Acceptance Criteria

1. THE Course_System SHALL создать Theory_Page `chapter-5/control-flow/for-loops.html` с объяснением трёх частей заголовка `for` (инициализация, условие, итерация) и их необязательности.
2. THE Theory_Page SHALL объяснять range-based `for` (for-each) и его применение для перебора последовательностей.
3. THE Theory_Page SHALL объяснять вложенные циклы `for` с примером таблицы умножения.
4. THE Theory_Page SHALL рекомендовать знаковые типы для переменных-счётчиков цикла.
5. THE Theory_Page SHALL содержать не менее двух Example_File: один для классического `for`, один для range-based `for`.
6. THE Theory_Page SHALL содержать Quiz_Widget, ссылающийся на Test_File `chapter-5/control-flow/for-loops.json` с не менее чем 4 вопросами.

---

### Требование 8: Параграф «break и continue»

**User Story:** Как студент, я хочу понять операторы `break` и `continue`, чтобы управлять ходом цикла без введения лишних флаговых переменных.

#### Acceptance Criteria

1. THE Course_System SHALL создать Theory_Page `chapter-5/control-flow/break-and-continue.html` с объяснением семантики `break` (завершение цикла или `switch`) и `continue` (переход к следующей итерации).
2. THE Theory_Page SHALL объяснять разницу между `break` и `return` в контексте цикла.
3. THE Theory_Page SHALL предупреждать об опасности `continue` в `while`/`do-while` при пропуске инкремента счётчика.
4. THE Theory_Page SHALL содержать Example_File, демонстрирующий `break` для выхода из бесконечного цикла и `continue` для пропуска итераций.
5. THE Theory_Page SHALL содержать Quiz_Widget, ссылающийся на Test_File `chapter-5/control-flow/break-and-continue.json` с не менее чем 3 вопросами.

---

### Требование 9: Параграф «Завершение программы (halts)»

**User Story:** Как студент, я хочу знать о функциях завершения программы, чтобы понимать разницу между нормальным и аварийным завершением и правильно использовать `std::exit`.

#### Acceptance Criteria

1. THE Course_System SHALL создать Theory_Page `chapter-5/control-flow/halts.html` с объяснением `std::exit()`, `std::atexit()`, `std::abort()` и `std::terminate()`.
2. THE Theory_Page SHALL объяснять, что `std::exit()` не уничтожает локальные переменные, и предупреждать об опасности явного вызова.
3. THE Theory_Page SHALL объяснять разницу между нормальным завершением (`std::exit`) и аварийным (`std::abort`).
4. THE Theory_Page SHALL содержать Example_File с демонстрацией `std::atexit` для регистрации функции очистки.
5. THE Theory_Page SHALL содержать Quiz_Widget, ссылающийся на Test_File `chapter-5/control-flow/halts.json` с не менее чем 2 вопросами.
6. THE Theory_Page SHALL рекомендовать использовать исключения вместо явных вызовов halt-функций.

---

### Требование 10: Gated-контент для C++17 — constexpr if

**User Story:** Как студент с доступом к расширенной версии, я хочу изучить `if constexpr`, чтобы писать эффективный шаблонный код с ветвлением на этапе компиляции.

#### Acceptance Criteria

1. THE Course_System SHALL создать Gated_Content-файл `cppQuest.Server/gated/chapter-5/control-flow/if-statements.html` с секцией `data-slot="1" data-std="17"`, содержащей объяснение Constexpr_If.
2. THE Gated_Content SHALL объяснять разницу между `if constexpr` и обычным `if` с `constexpr`-условием: гарантия вычисления на этапе компиляции и исключение мёртвого кода.
3. THE Gated_Content SHALL содержать Example_File `chapter-5/control-flow/if-constexpr.cpp` с демонстрацией `if constexpr` в шаблонной функции.
4. WHEN студент не приобрёл стандарт C++17, THE Course_System SHALL отображать заглушку с меткой «Доступно в расширенной версии» и ценой на месте Gated_Content.
5. THE Gated_Content SHALL быть помечен Std_Badge `C++17`.

---

### Требование 11: Gated-контент для C++17 — switch с инициализатором и [[fallthrough]]

**User Story:** Как студент с доступом к расширенной версии, я хочу изучить расширения `switch` в C++17, чтобы писать более выразительный код.

#### Acceptance Criteria

1. THE Course_System SHALL создать Gated_Content-файл `cppQuest.Server/gated/chapter-5/control-flow/switch-fallthrough.html` с секцией `data-slot="1" data-std="17"`, содержащей объяснение атрибута `[[fallthrough]]`.
2. THE Course_System SHALL добавить в тот же файл секцию `data-slot="2" data-std="17"` с объяснением синтаксиса `switch(init; expr)` (инициализатор в `switch`).
3. THE Gated_Content SHALL содержать Example_File для каждой секции, использующий Brace_Init.
4. WHEN студент не приобрёл стандарт C++17, THE Course_System SHALL отображать заглушки с меткой «Доступно в расширенной версии».

---

### Требование 12: Стиль и качество кода в примерах

**User Story:** Как студент, я хочу видеть примеры кода в современном стиле C++, чтобы сразу усваивать правильные практики.

#### Acceptance Criteria

1. THE Course_System SHALL использовать Brace_Init во всех Example_File главы 5: `int x {42};`, `char c {'A'};`.
2. THE Course_System SHALL не использовать `using namespace std;` в глобальной области ни в одном Example_File.
3. THE Course_System SHALL добавлять комментарии в Example_File только для нетривиальной логики; самодокументируемый код комментариев не требует.
4. IF Example_File содержит устаревший синтаксис (например, `int x = 42;` вместо Brace_Init), THEN THE Course_System SHALL обновить его перед включением в параграф.
5. THE Course_System SHALL использовать букву «ё» во всех русскоязычных текстах Theory_Page там, где она требуется по правилам русского языка.

---

### Требование 13: Система тестирования

**User Story:** Как студент, я хочу проверять знания после каждого параграфа, чтобы закреплять изученный материал.

#### Acceptance Criteria

1. THE Course_System SHALL создать Test_File для каждого нового параграфа по соглашению об именовании: `{ChapterID}_{ParagraphID}_final.json` в директории `cppQuest.Server/tests/chapter-5/control-flow/`.
2. WHEN студент завершает тест, THE Quiz_Widget SHALL отображать результат (количество правильных ответов, итоговый статус) и сохранять попытку через `POST /api/quiz/attempt`.
3. THE Course_System SHALL включать в каждый Test_File не менее одного вопроса на понимание поведения кода (predict-the-output) и не менее одного вопроса на выбор правильной конструкции.
4. IF студент отвечает неправильно, THEN THE Quiz_Widget SHALL показывать объяснение правильного ответа.

---

### Требование 14: Навигация и прогресс

**User Story:** Как студент, я хочу переходить между параграфами через кнопки «Предыдущий» / «Следующий», чтобы изучать материал последовательно.

#### Acceptance Criteria

1. THE Course_System SHALL отображать кнопки навигации «Предыдущий параграф» и «Следующий параграф» на каждой Theory_Page главы 5 в соответствии с порядком из `course-meta.json`.
2. WHEN студент завершает чтение Theory_Page (прокрутка до конца), THE Course_System SHALL фиксировать сессию чтения через `POST /api/reading/complete`.
3. THE Course_System SHALL отображать индикатор прогресса чтения (полоса вверху страницы) на каждой Theory_Page.
