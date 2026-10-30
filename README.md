# Island11

Компактная панель для Windows 11 в стиле Dynamic Island. Текущая версия: **0.4.2**.

## Возможности

- Плеер Windows/Spotify: обложка, управление, прогресс и перемотка при поддержке источника.
- Bluetooth-устройства и управление совместимыми аудиопрофилями.
- Системная громкость и выбор устройства вывода.
- Календарь, быстрый запуск, настраиваемое положение и оформление.
- Локальная история буфера обмена, отключена по умолчанию. **История хранится без шифрования и может содержать пароли.**

## Сборка

Нужны Windows 11 x64 и [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). Откройте `Source` в VS Code и запустите `build.cmd` либо выполните:

```powershell
dotnet publish Source/Island11.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish
```

Готовое приложение — `publish/Island11.exe`. На первом запуске восстановления зависимостей требуется доступ к NuGet.

Проверка логики:

```powershell
dotnet run --project Tests/Island11.Tests.csproj
```

## Ограничения

Совместимость Bluetooth и системного аудио зависит от Windows и драйверов. Исправление звука в версии 0.4.1 требует проверки на целевом ПК. Ползунок громкости меняет общую громкость Windows, не только Spotify. Приложение не подписано. Данные и настройки хранятся в `%LOCALAPPDATA%\Island11`.

Лицензии сторонних компонентов приведены в `Licenses`. Условия лицензирования самого проекта пока не указаны.
