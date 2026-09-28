@echo off
echo Gerando o Miyagi Strap (1 exe so)...
echo.
dotnet publish MiyagiStrap.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o publish
echo.
echo Pronto! Seu exe esta em: %cd%\publish\MiyagiStrap.exe
pause
