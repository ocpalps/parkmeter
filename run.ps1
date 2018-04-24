
Set-Location ".\src\Backend\Parkmeter.API"
Start-Process -FilePath dotnet  -ArgumentList "run", "--project Parkmeter.Api.csproj"
Set-Location "..\..\..\"

Set-Location "src\FrontEnd\Parkmeter.Admin"
Start-Process -FilePath dotnet  -ArgumentList "run", "--project Parkmeter.Admin.csproj"
Set-Location "..\..\..\"
