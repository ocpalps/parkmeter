# BUILD
Start-Process dotnet -ArgumentList "build .\src\ParkMeter.sln" -Wait -RedirectStandardError "dotnet_err_build.txt"

#cosmos db
$cosmos = Get-Process CosmosDB.Emulator -ErrorAction SilentlyContinue
if (-Not $cosmos) {
    Start-Process -FilePath "C:\Program Files\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe"
}


Set-Location ".\src\BackEnd\Parkmeter.API"
Start-Process -FilePath dotnet  -ArgumentList "run", "--project Parkmeter.Api.csproj" -RedirectStandardError "dotnet_err_api.txt"

Set-Location "..\..\..\"

Set-Location "src\FrontEnd\Parkmeter.Admin"
Start-Process -FilePath dotnet  -ArgumentList "run", "--project Parkmeter.Admin.csproj" -RedirectStandardError "dotnet_err_admin.txt"

Set-Location "..\..\..\"

# FUNCTIONS
# DOWNLOAD LATEST RUNTIME

Start-Process npm -ArgumentList "install" -wait

Set-Location "src\BackEnd\Parkmeter.Functions\bin\Debug\netcoreapp2.1"
Start-Process -FilePath "..\..\..\..\..\..\node_modules\azure-functions-core-tools\bin\func.exe" -ArgumentList "start"
Set-Location "..\..\..\"
