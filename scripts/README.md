# Database Scripts

Эта папка содержит SQL скрипты для настройки базы данных PostgreSQL для приложения bankrupt-piterjust.

## Файлы

### 1. `add_employee_procedure.sql`
Содержит хранимые процедуры для работы с сотрудниками:
- `add_employee()` - добавление нового сотрудника с проверкой валидности данных
- `authenticate_employee()` - аутентификация сотрудника
- `find_employee_by_name_position()` - поиск сотрудника по имени и должности

### 2. `init_database.sql`
Полный скрипт инициализации базы данных:
- Включение расширения pgcrypto для хеширования паролей
- Создание схемы
- Создание таблицы employee
- Создание индексов для производительности
- Установка всех хранимых процедур
- Создание администратора по умолчанию

## Использование

### Для новой базы данных:
```sql
-- Подключитесь к базе данных piterjust и выполните:
\i /path/to/scripts/init_database.sql
```

### Для обновления существующей базы данных:
```sql
-- Подключитесь к базе данных piterjust и выполните:
\i /path/to/scripts/add_employee_procedure.sql
```

### Примеры использования хранимых процедур:

#### Добавление нового сотрудника:
```sql
SELECT * FROM add_employee(
    'Иванов', 
    'Иван', 
    'Иванович', 
    'Менеджер', 
    'ivanov', 
    crypt('password123', gen_salt('bf'))  -- PostgreSQL crypt функция
);
```

#### Прямое добавление сотрудника через SQL:
```sql
INSERT INTO employee (last_name, first_name, middle_name, position, login, password_hash, created_date, is_active)
VALUES (
    'Петров',
    'Петр', 
    'Петрович',
    'Специалист',
    'petrov',
    crypt('password123', gen_salt('bf')),
    NOW(),
    true
);
```

#### Проверка пароля:
```sql
SELECT 
    login,
    (password_hash = crypt('введенный_пароль', password_hash)) as password_correct
FROM employee 
WHERE login = 'admin';
```

## Примечания

1. **Безопасность**: Все пароли хешируются с использованием PostgreSQL функций `crypt()` и `gen_salt('bf')` (Blowfish алгоритм).

2. **Расширение pgcrypto**: Обязательно для работы с функциями хеширования. Устанавливается автоматически скриптом.

3. **Валидация**: Хранимые процедуры включают проверку обязательных полей и уникальности логина.

4. **Обработка ошибок**: Процедуры возвращают информацию об успехе операции и сообщения об ошибках.

5. **Производительность**: Созданы индексы для часто используемых запросов.

6. **Администратор по умолчанию**: 
   - Логин: `admin`
   - Пароль: `admin123`
   - Создается автоматически при первом запуске

## Структура таблицы employee

```sql
CREATE TABLE employee (
    employee_id SERIAL PRIMARY KEY,
    last_name VARCHAR(100) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    middle_name VARCHAR(100),
    position VARCHAR(200) NOT NULL,
    login VARCHAR(100) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    created_date TIMESTAMP NOT NULL DEFAULT NOW(),
    is_active BOOLEAN NOT NULL DEFAULT true
);
```

## Изменения в коде

После применения этих скриптов, код в `AuthenticationService.cs` был обновлен для использования PostgreSQL функций хеширования вместо BCrypt:

- **Хеширование**: `crypt(password, gen_salt('bf'))`
- **Проверка**: `password_hash = crypt(input_password, password_hash)`
- **Лучшую безопасность**: Использование встроенных функций PostgreSQL
- **Централизованную логику**: Все операции с паролями выполняются на стороне БД
- **Упрощенное обслуживание**: Нет зависимости от внешних библиотек .NET для хеширования
- **Улучшенную производительность**: Встроенные функции PostgreSQL оптимизированы

## Требования

- PostgreSQL 9.1 или выше (для поддержки pgcrypto)
- Права на создание расширений в базе данных
- Права на создание таблиц и функций