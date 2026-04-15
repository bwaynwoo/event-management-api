# Event Management API

REST API для управления мероприятиями.
## Технологии

- .NET 10
- ASP.NET Core Web API
- Swagger/OpenAPI
- In-memory хранилище

## Требования

- [.NET 10](https://dotnet.microsoft.com/download) или выше

## Установка и запуск

### 1. Клонировать репозиторий

```bash
git clone https://github.com/bwaynwoo/event-management-api.git
cd event-management-api/EventManagementApi
```

### 2. Собрать проект
```bash
dotnet build
```

### 3. Запустить проект
```bash
dotnet run
```

### 4. Открыть Swagger
После запуска Swagger будет доступен по адресу:
```
http://localhost:5000/swagger/index.html
```

>Порт может отличаться. Точный адрес будет показан в терминале после запуска.

## API Endpoints

### Базовый URL: <https://localhost:5000/api/events>

| Метод |	Endpoint	| Описание |
| ------ | ------------ | -------- |
|GET	|/events	|Получить список всех мероприятий|
|GET	|/events/{id}	|Получить мероприятие по ID
|POST	|/events	|Создать новое мероприятие
|PUT	|/events/{id}	|Обновить мероприятие целиком|
|DELETE	|/events/{id}	|Удалить мероприятие

### Особенности реализации
* Данные хранятся в памяти приложения (List<Event>)
* При перезапуске сервера все данные сбрасываются
* ID генерируется автоматически (автоинкремент)
* Swagger доступен в режиме разработки

### Пример запроса для создания мероприятия
```json
{
  "title": "Конференция .NET",
  "description": "Ежегодная конференция разработчиков",
  "startAt": "2026-07-09T10:00:00",
  "endAt": "2026-07-09T18:00:00"
}
```
