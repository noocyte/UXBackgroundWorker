mkdir release

@call msbuild src\AzureWorkers\AzureWorkers.csproj /t:clean
@call msbuild src\AzureWorkers\AzureWorkers.csproj /p:Configuration=Release

src\.nuget\NuGet.exe pack -sym src\AzureWorkers\AzureWorkers.csproj -OutputDirectory release