# BUILD
Start-Process dotnet -ArgumentList "build .\src\ParkMeter.sln" -Wait

#cosmos db
$cosmos = Get-Process CosmosDB.Emulator -ErrorAction SilentlyContinue
if (-Not $cosmos) {
    Start-Process -FilePath "C:\Program Files\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe"
}


Set-Location ".\src\Backend\Parkmeter.API"
Start-Process -FilePath dotnet  -ArgumentList "run", "--project Parkmeter.Api.csproj"
Set-Location "..\..\..\"

Set-Location "src\FrontEnd\Parkmeter.Admin"
Start-Process -FilePath dotnet  -ArgumentList "run", "--project Parkmeter.Admin.csproj"
Set-Location "..\..\..\"

# FUNCTIONS
# DOWNLOAD LATEST RUNTIME
Start-Process npm -ArgumentList "install azure-functions-core-tools -g" -wait

Set-Location "src\BackEnd\Parkmeter.Functions\bin\Debug\netcoreapp2.1"
Start-Process func -ArgumentList "start"
Set-Location "..\..\..\"
