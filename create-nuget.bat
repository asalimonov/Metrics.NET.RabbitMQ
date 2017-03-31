rd /S /Q .\publishing\lib

call build.bat
if %errorlevel% neq 0 exit /b %errorlevel%

md .\publishing\lib
md .\publishing\lib\net451

copy .\src\Metrics.NET.RabbitMQ\bin\Release\Metrics.RabbitMQ.dll .\publishing\lib\net451\
copy .\src\Metrics.NET.RabbitMQ\bin\Release\Metrics.RabbitMQ.xml .\publishing\lib\net451\
copy .\src\Metrics.NET.RabbitMQ\bin\Release\Metrics.RabbitMQ.pdb .\publishing\lib\net451\

.\.nuget\NuGet.exe pack .\Publishing\Metrics.NET.RabbitMQ.nuspec -OutputDirectory .\publishing
if %errorlevel% neq 0 exit /b %errorlevel%