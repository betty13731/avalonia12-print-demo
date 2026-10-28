# Avalonia 12 Native Print Demo

Готовый пример Avalonia 12 + CommunityToolkit.Mvvm:
- список принтеров
- выбор принтера
- печать PDF через нативные средства ОС
  - Windows: Shell `print/printto`
  - Linux/macOS: CUPS `lp`

## Запуск

```bash
dotnet restore
dotnet run --project src/AvaloniaNativePrintDemo
```

## Зависимости
- Avalonia 12
- CommunityToolkit.Mvvm
- System.Management (для списка принтеров на Windows)

## Примечания
- На Linux должен быть установлен CUPS client (`lp`, `lpstat`).
- На Windows печать зависит от ассоциации PDF-приложения в системе.
