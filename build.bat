.nuget\NuGet.exe restore Metrics.NET.RabbitMQ.sln

set MSBUILD="C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"

rd /S /Q .\bin\Debug
rd /S /Q .\bin\Release

%MSBUILD% Metrics.NET.RabbitMQ.sln /p:Configuration="Debug"
if %errorlevel% neq 0 exit /b %errorlevel%

%MSBUILD% Metrics.NET.RabbitMQ.sln /p:Configuration="Release"
if %errorlevel% neq 0 exit /b %errorlevel%