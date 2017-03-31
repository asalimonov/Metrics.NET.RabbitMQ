mono .nuget/NuGet.exe restore Metrics.NET.RabbitMQ.sln 

xbuild Metrics.NET.RabbitMQ.sln /p:Configuration="Debug"
xbuild Metrics.NET.RabbitMQ.sln /p:Configuration="Release"