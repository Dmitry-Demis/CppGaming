# План реализации: CppQuest API Gateway

## Обзор

Реализация API Gateway на базе YARP (.NET 8) с централизованной JWT-валидацией, Rate Limiting через Redis, отказоустойчивостью через Polly, метриками Prometheus и аудитом через Kafka. Реализация ведётся инкрементально: сначала ядро маршрутизации, затем слои безопасности, наблюдаемости и инфраструктуры.

## Задачи

- [x] 1. Инициализация проекта и базовая структура
  - Создать проект `CppQuest.Gateway` (.NET 8, ASP.NET Core) с файлами `Program.cs`, `appsettings.json`, `CppQuest.Gateway.csproj`
  - Добавить NuGet-зависимости: `Yarp.ReverseProxy`, `Microsoft.Extensions.Http.Resilience`, `StackExchange.Redis`, `prometheus-net.AspNetCore`, `Confluent.Kafka`, `FsCheck.Xunit`, `Microsoft.AspNetCore.Mvc.Testing`
  - Создать директории: `Middleware/`, `Services/`, `Models/`, `Configuration/`, `Tests/`
  - Определить конфигурационные record-модели: `GatewayConfiguration`, `JwtConfig`, `RateLimitingConfig`, `RateLimitPolicy`, `CorsConfig`, `KafkaConfig` в `Configuration/` — с Doxygen-комментариями на русском для каждого типа и свойства
  - Определить модели событий аудита: `AccessAuditEvent`, `SecurityAuditEvent` в `Models/` — с Doxygen-комментариями на русском
  - Определить модели результатов: `JwtValidationResult`, `RateLimitResult` в `Models/` — с Doxygen-комментариями на русском
  - _Требования: 1.1, 1.5, 11.1, 12.1, 12.2_

- [x] 2. YARP — базовая маршрутизация и Hot Reload
  - [x] 2.1 Настроить YARP Reverse Proxy с маршрутами auth-route, profile-route, legacy-fallback в `appsettings.json`
    - Зарегистрировать YARP через `builder.Services.AddReverseProxy().LoadFromConfig()`
    - Настроить `IOptionsMonitor<T>` для Hot Reload конфигурации в течение 5 секунд
    - _Требования: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_

  - [ ]* 2.2 Написать property-тест для round-trip конфигурации маршрутизации
    - **Свойство 3: Round-trip конфигурации маршрутизации**
    - **Validates: Requirements 1.5, 11.4**

  - [ ]* 2.3 Написать интеграционный тест Hot Reload конфигурации
    - Проверить, что изменение `appsettings.json` применяется без перезапуска процесса
    - _Требования: 1.6_

- [ ] 3. Middleware: трансформация заголовков
  - [ ] 3.1 Реализовать `HeaderTransformMiddleware` в `Middleware/HeaderTransformMiddleware.cs`
    - Входящие трансформации: добавить `X-Forwarded-For`, удалить `Authorization`, добавить `X-User-Id` и `X-User-Claims` из `HttpContext.Items`
    - Исходящие трансформации: добавить `Strict-Transport-Security`, `X-Content-Type-Options`, `Content-Security-Policy`; удалить `Server`, `X-Powered-By`, `X-AspNet-Version`
    - Добавить Doxygen-комментарии на русском для класса и метода `InvokeAsync`
    - _Требования: 2.1, 2.2, 2.3, 2.4, 12.1, 12.2_

  - [ ]* 3.2 Написать unit-тесты для `HeaderTransformMiddleware`
    - Тест: добавление `X-Forwarded-For` с корректным IP
    - Тест: удаление `Authorization` из проксируемого запроса
    - Тест: добавление security-заголовков во все ответы
    - Тест: удаление `Server` и `X-Powered-By` из ответа
    - _Требования: 2.1, 2.2, 2.3, 2.4_

  - [ ]* 3.3 Написать property-тест для трансформации заголовков
    - **Свойство 4: Трансформация заголовков при проксировании**
    - **Validates: Requirements 2.1, 2.2**

  - [ ]* 3.4 Написать property-тест для security-заголовков
    - **Свойство 5: Security-заголовки присутствуют во всех ответах**
    - **Validates: Requirements 2.3, 2.4**

- [ ] 4. Middleware: JWT-валидация
  - [ ] 4.1 Реализовать интерфейс `IJwtValidator` и класс `JwtValidator` в `Services/JwtValidator.cs`
    - Загрузка публичного RSA-ключа из конфигурации при старте (без обращения к Auth_Service)
    - Проверка подписи, срока действия, issuer и audience
    - Возврат `JwtValidationResult` с claims при успехе или `FailureReason` при неудаче
    - Добавить Doxygen-комментарии на русском для интерфейса, класса и метода `Validate`
    - _Требования: 3.1, 3.2, 3.3, 3.4, 3.5, 12.1, 12.2_

  - [ ] 4.2 Реализовать `JwtValidationMiddleware` в `Middleware/JwtValidationMiddleware.cs`
    - Для защищённых маршрутов: вызвать `IJwtValidator.Validate()`, при успехе — записать claims в `HttpContext.Items`, при неудаче — вернуть 401 в формате `{"error": "UNAUTHORIZED", ...}`
    - Добавить Doxygen-комментарии на русском для класса и метода `InvokeAsync`
    - _Требования: 3.1, 3.2, 3.3, 3.4, 12.1, 12.2_

  - [ ]* 4.3 Написать unit-тесты для `JwtValidator`
    - Тест: валидный токен → `IsValid = true`, claims заполнены
    - Тест: истёкший токен → `IsValid = false`, `FailureReason` не null
    - Тест: неверная подпись → `IsValid = false`
    - Тест: отсутствующий токен → `IsValid = false`
    - _Требования: 3.1, 3.2, 3.3, 11.1_

  - [ ]* 4.4 Написать property-тест: невалидный JWT возвращает 401
    - **Свойство 6: Невалидный JWT возвращает 401 и не проксирует запрос**
    - **Validates: Requirements 3.1, 3.2, 3.3**

  - [ ]* 4.5 Написать property-тест: claims из валидного JWT передаются во внутренний сервис
    - **Свойство 7: Claims из валидного JWT передаются во внутренний сервис**
    - **Validates: Requirements 3.4**

- [ ] 5. Checkpoint — проверка базового конвейера
  - Убедиться, что все тесты проходят. Уточнить у пользователя, если возникнут вопросы.

- [ ] 6. Middleware: Rate Limiting через Redis
  - [ ] 6.1 Реализовать интерфейс `IRateLimiterService` и класс `RedisRateLimiterService` в `Services/RedisRateLimiterService.cs`
    - Алгоритм Sliding Window через `StackExchange.Redis`
    - Ключи Redis: `rl:ip:{ip}:{window_ts}` и `rl:user:{user_id}:{window_ts}`
    - Возврат `RateLimitResult` с `RetryAfterSeconds` и `RemainingRequests`
    - Добавить Doxygen-комментарии на русском для интерфейса, класса и метода `CheckLimitAsync`
    - _Требования: 4.1, 4.2, 4.3, 4.4, 12.1, 12.2_

  - [ ] 6.2 Реализовать `RateLimiterMiddleware` в `Middleware/RateLimiterMiddleware.cs`
    - Выбор политики: `PublicRoutePolicy` для публичных маршрутов, `AuthenticatedPolicy` для аутентифицированных, `DefaultPolicy` для остальных
    - При превышении лимита: вернуть 429 с заголовком `Retry-After`
    - Добавить Doxygen-комментарии на русском для класса и метода `InvokeAsync`
    - _Требования: 4.1, 4.2, 4.4, 4.5, 12.1, 12.2_

  - [ ]* 6.3 Написать unit-тесты для `RedisRateLimiterService`
    - Тест: первый запрос разрешён
    - Тест: N+1 запрос отклонён, `RetryAfterSeconds > 0`
    - Тест: сброс окна — запрос снова разрешён
    - _Требования: 4.1, 4.2, 4.3, 11.1_

  - [ ]* 6.4 Написать property-тест: превышение лимита возвращает 429 с Retry-After
    - **Свойство 8: Превышение лимита запросов возвращает 429 с заголовком Retry-After**
    - **Validates: Requirements 4.1, 4.2, 4.4**

- [ ] 7. Отказоустойчивость: Polly Retry и Circuit Breaker
  - [ ] 7.1 Настроить `ResiliencePipeline` через `Microsoft.Extensions.Http.Resilience` в `Program.cs`
    - Retry: 3 попытки, экспоненциальная задержка от 200 мс, обработка `HttpRequestException`
    - Circuit Breaker: `FailureRatio = 0.5`, `MinimumThroughput = 5`, `SamplingDuration = 30s`, `BreakDuration = 10s`, `HalfOpenMaxAttempts = 1`
    - Таймаут: 3 секунды на запрос (возврат 504)
    - Добавить inline-комментарии на русском к каждому параметру политики
    - _Требования: 5.1, 5.2, 5.3, 5.4, 5.5, 12.1, 12.2_

  - [ ] 7.2 Реализовать Fallback-логику при недоступном сервисе в конфигурации YARP
    - При открытом Circuit Breaker и наличии Fallback — перенаправить на Legacy
    - Добавить Doxygen-комментарии на русском если логика выносится в отдельный класс
    - _Требования: 5.6, 12.1, 12.2_

  - [ ]* 7.3 Написать unit-тесты для Circuit Breaker
    - Тест: переход Closed → Open после 5 ошибок за 30 секунд
    - Тест: состояние Open → возврат 503 без обращения к сервису
    - Тест: успешный пробный запрос → переход Open → Closed
    - _Требования: 5.3, 5.4, 5.5, 11.3_

  - [ ]* 7.4 Написать property-тест: Retry Policy не более 3 повторных попыток
    - **Свойство 9: Retry Policy выполняет не более 3 повторных попыток**
    - **Validates: Requirements 5.2**

  - [ ]* 7.5 Написать property-тест: Circuit Breaker переходит в Open после порогового числа ошибок
    - **Свойство 10: Circuit Breaker переходит в Open после порогового числа ошибок**
    - **Validates: Requirements 5.3**

  - [ ]* 7.6 Написать property-тест: round-trip состояния Circuit Breaker (Open → Closed)
    - **Свойство 11: Round-trip состояния Circuit Breaker (Open → Closed)**
    - **Validates: Requirements 5.5**

  - [ ]* 7.7 Написать property-тест: Fallback при недоступном сервисе
    - **Свойство 12: Fallback при недоступном сервисе**
    - **Validates: Requirements 5.6**

- [ ] 8. Checkpoint — проверка безопасности и отказоустойчивости
  - Убедиться, что все тесты проходят. Уточнить у пользователя, если возникнут вопросы.

- [ ] 9. Наблюдаемость: метрики Prometheus
  - [ ] 9.1 Реализовать `MetricsMiddleware` в `Middleware/MetricsMiddleware.cs`
    - Зарегистрировать метрики через `prometheus-net`: `gateway_requests_total`, `gateway_request_duration_seconds`, `gateway_errors_total`, `circuit_breaker_state`
    - Обновлять метрики после каждого запроса с метками `route`, `method`, `status_code`, `error_type`, `service`
    - Подключить эндпоинт `/metrics` через `app.MapMetrics()`
    - Добавить Doxygen-комментарии на русском для класса, полей метрик и метода `InvokeAsync`
    - _Требования: 6.1, 6.2, 6.3, 6.4, 6.5, 12.1, 12.2_

  - [ ] 9.2 Реализовать обновление метрики `circuit_breaker_state` при изменении состояния Circuit Breaker
    - Подписаться на события Polly `OnClosed`, `OnOpened`, `OnHalfOpened` и обновлять Gauge
    - Добавить inline-комментарии на русском к каждому обработчику события
    - _Требования: 6.5, 12.1, 12.2_

  - [ ]* 9.3 Написать property-тест: метрики RED корректно обновляются для каждого запроса
    - **Свойство 13: Метрики RED корректно обновляются для каждого запроса**
    - **Validates: Requirements 6.1, 6.2, 6.3**

  - [ ]* 9.4 Написать property-тест: состояние Circuit Breaker отражается в метрике
    - **Свойство 14: Состояние Circuit Breaker отражается в метрике**
    - **Validates: Requirements 6.5**

- [ ] 10. Наблюдаемость: аудит событий через Kafka
  - [ ] 10.1 Реализовать интерфейс `IAuditEventPublisher` и класс `KafkaAuditEventPublisher` в `Services/KafkaAuditEventPublisher.cs`
    - Использовать `Channel<T>` как in-memory буфер (ограниченная ёмкость из `KafkaConfig.LocalBufferCapacity`)
    - Middleware записывает событие в `Channel` (non-blocking)
    - Добавить Doxygen-комментарии на русском для интерфейса, класса и методов `PublishAccessEventAsync`, `PublishSecurityEventAsync`
    - _Требования: 7.1, 7.2, 7.3, 7.4, 12.1, 12.2_

  - [ ] 10.2 Реализовать `KafkaPublisherHostedService` в `Services/KafkaPublisherHostedService.cs`
    - Фоновый `IHostedService`, читающий из `Channel` и публикующий в Kafka через `Confluent.Kafka`
    - При недоступности Kafka: логировать с уровнем `Warning`, события остаются в буфере
    - Добавить Doxygen-комментарии на русском для класса и методов `StartAsync`, `StopAsync`
    - _Требования: 7.3, 7.4, 12.1, 12.2_

  - [ ] 10.3 Интегрировать `IAuditEventPublisher` в `JwtValidationMiddleware` и в финальный Middleware конвейера
    - После успешного проксирования: публиковать `AccessAuditEvent`
    - После неудачной JWT-валидации: публиковать `SecurityAuditEvent`
    - _Требования: 7.1, 7.2_

  - [ ]* 10.4 Написать unit-тесты для `KafkaAuditEventPublisher`
    - Тест: при недоступной Kafka события буферизируются, обработка запроса не блокируется
    - _Требования: 7.4, 11.1_

  - [ ]* 10.5 Написать property-тест: событие аудита содержит все обязательные поля
    - **Свойство 15: Событие аудита содержит все обязательные поля**
    - **Validates: Requirements 7.1**

  - [ ]* 10.6 Написать property-тест: событие безопасности публикуется при неудачной JWT-валидации
    - **Свойство 16: Событие безопасности публикуется при неудачной JWT-валидации**
    - **Validates: Requirements 7.2**

- [ ] 11. Middleware: CORS
  - [ ] 11.1 Настроить CORS Middleware в `Program.cs`
    - Загрузить список доверенных доменов из `CorsConfig.AllowedOrigins` через `IOptionsMonitor` с поддержкой Hot Reload
    - Preflight OPTIONS-запрос от доверенного Origin → 204 с CORS-заголовками
    - Запрос от недоверенного Origin → ответ без CORS-заголовков
    - Добавить inline-комментарии на русском к логике проверки Origin
    - _Требования: 8.1, 8.2, 8.3, 12.1, 12.2_

  - [ ]* 11.2 Написать property-тест: доверенный Origin получает корректные CORS-заголовки
    - **Свойство 17: CORS — доверенный Origin получает корректные заголовки**
    - **Validates: Requirements 8.1**

  - [ ]* 11.3 Написать property-тест: недоверенный Origin не получает CORS-заголовки
    - **Свойство 18: CORS — недоверенный Origin не получает CORS-заголовки**
    - **Validates: Requirements 8.2**

- [ ] 12. Сборка конвейера и интеграционные тесты
  - [ ] 12.1 Собрать Middleware Pipeline в `Program.cs` в корректном порядке
    - Порядок: CORS → Rate Limiter → JWT Validation → Header Transform → YARP (с Polly + Metrics)
    - Зарегистрировать все сервисы в DI-контейнере
    - Добавить inline-комментарии на русском к каждому шагу регистрации и порядку middleware
    - _Требования: 1.1, 2.1, 3.1, 4.1, 5.1, 6.1, 7.1, 8.1, 12.1, 12.2_

  - [ ]* 12.2 Написать интеграционный тест: сквозное прохождение запроса с проверкой заголовков
    - Использовать `WebApplicationFactory<Program>` с mock-сервисами
    - Проверить корректность заголовков у mock-сервиса (Свойство 4)
    - _Требования: 11.2_

  - [ ]* 12.3 Написать интеграционный тест: таймаут внутреннего сервиса → 504
    - _Требования: 5.1, 11.3_

  - [ ]* 12.4 Написать интеграционный тест: срабатывание Circuit Breaker → 503
    - _Требования: 5.3, 11.3_

  - [ ]* 12.5 Написать property-тест: маршрутизация сохраняет метод, путь и тело запроса
    - **Свойство 1: Маршрутизация сохраняет метод, путь и тело запроса**
    - **Validates: Requirements 1.2, 1.3**

  - [ ]* 12.6 Написать property-тест: Fallback-маршрутизация на Legacy
    - **Свойство 2: Fallback-маршрутизация на Legacy**
    - **Validates: Requirements 1.4**

- [ ] 13. Инфраструктура как код (Docker Compose)
  - [ ] 13.1 Создать `docker-compose.yml` с сервисами: Gateway, Kafka, Redis, Prometheus, Grafana, PostgreSQL (Auth + Profile), Legacy
    - Настроить health checks и порядок запуска через `depends_on`
    - Использовать именованные Docker volumes для PostgreSQL и Kafka
    - _Требования: 9.1, 9.2, 9.4_

  - [ ] 13.2 Создать конфигурационные файлы Prometheus (`prometheus.yml`) и Grafana (дашборды) в репозитории
    - Монтировать файлы через `volumes` в `docker-compose.yml`
    - Настроить scrape-конфигурацию для эндпоинта `/metrics` Gateway
    - _Требования: 9.3_

- [ ] 14. Финальный checkpoint — все тесты проходят
  - Убедиться, что все тесты проходят. Уточнить у пользователя, если возникнут вопросы.

- [ ] 15. README
  - [ ] 15.1 Создать `README.md` в корне проекта `CppQuest.Gateway/`
    - Раздел «Структура проекта» — дерево директорий с кратким описанием каждой папки и ключевых файлов
    - Раздел «Требования» — версии .NET, Docker, Docker Compose
    - Раздел «Быстрый старт» — команды для запуска всего окружения через `docker compose up`
    - Раздел «Запуск только Gateway» — команды `dotnet run` для локальной разработки
    - Раздел «Запуск тестов» — команды `dotnet test` для unit/property/интеграционных тестов
    - Раздел «Проверка работоспособности» — примеры curl-запросов к `/metrics`, `/api/auth/`, `/api/profile/` и проверка заголовков ответа
    - Раздел «Конфигурация» — описание ключевых секций `appsettings.json` (YARP, JWT, RateLimiting, Cors, Kafka)

## Примечания

- Задачи, помеченные `*`, являются опциональными и могут быть пропущены для ускорения MVP
- Каждая задача ссылается на конкретные требования для обеспечения трассируемости
- Property-тесты используют FsCheck (интеграция с xUnit через `FsCheck.Xunit`), минимум 100 итераций
- Нагрузочное тестирование (Требования 10.1, 10.2) проводится отдельно с использованием `k6` или `NBomber` и не включено в данный план как задача кодирования
- Все комментарии в коде пишутся на русском языке в формате Doxygen (`/// <summary>`, `/// <param>`, `/// <returns>`, `/// <exception>`)
