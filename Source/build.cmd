@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
 echo Install the .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0
 pause
 exit /b 1
)
dotnet publish Island11.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish
if errorlevel 1 (
 echo Build failed. Read the errors above.
 pause
 exit /b 1
)
echo Ready: %CD%\publish\Island11.exe
pause
