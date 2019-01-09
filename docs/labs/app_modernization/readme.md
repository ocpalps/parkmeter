# App Modernization Lab

The goal of this lab is to modernize the solution adding serverless functionalities and securing the Frontend layer with **Azure AD B2C**.
The lab is divided in two parts: 
- Azure AD integration for login
- Azure Functions and Cosmos DB integration (serverless)

# 0 - Getting started

## Build the project
- download the **stage 0** source code from [here](https://github.com/ocpalps/parkmeter/archive/0.zip)
- unzip the folder and execute **run.ps1**. The project must build successfully and launches two console applications. You might need to give missing persmissions to execute the powershell script:
```powershell
Set-ExecutionPolicy Unrestricted
```
- Browse to [http://localhost:50058/](http://localhost:50058/) and verify that the website works and allows to add a new parking

## Create new Azure AD B2C tenant
- Login to Azure portal and create a new AD B2C resource
- Copy the domain name value
- Choose *"Local Account/Email"* as **Identity Provider**
- Create two new *"User Flow"*:
  - singin and signup with name *"susi"*
  - reset password with name *"pwdres"*
- Create a new Application of type Web (enable *"Implicit Flow"*) and copy the Application ID value


# 1 - Azure AD B2C to Parkmeter.Admin

- Update project target framework to **.NET Core 2.1**
- Update NuGet Packages:
  - Newtonsoft.Json (**latest stable**)
  - Microsoft.VisualStudio.Web.CodeGeneration.Design (**2.1.6**)
  - Microsoft.AspNetCore.All (**2.1.6**)
- Add NuGet Packages:
  - Microsoft.AspNetCore.Authentication.AzureADB2C.UI (**2.1.1**)
- Add Azure AD B2C settings in **appsettings.json** using the following table to match between portal and JSON names:

 Portal Name | .NET Name | Description | Example
--- | --- | --- | ---
Application ID | `ClientID` | |*41a7bd7f-3d45-44b7-98e8-b02303ed08e2*
Domain Name | `Domain` | FQDN of your tenant (with *.onmicrosoft.com*) | *parkmeter.onmicrosoft.com*
Resource Name  | `Instance` | Use the format *https://[TENANT_NAME].b2clogin.com/tfp/* where tenant name is your 3rd level domain name (without *.onmicrosoft.com*)  | *https://parkmeter.b2clogin.com/tfp/*



    ```json 
    "AzureAdB2C": {
    "Instance": "[XXXXXXXX]",
    "ClientId": "[YYYYYYYY]",
    "CallbackPath": "/signin-oidc",
    "Domain": "[ZZZZZZZZ]",
    "SignUpSignInPolicyId": "B2C_1_susi",
    "ResetPasswordPolicyId": "B2C_1_pwdres",
    "EditProfilePolicyId": ""
  },
    ```
- Add the using statements to **Startup.cs**
    ```csharp 
        using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
        using Microsoft.AspNetCore.Authentication;
    ```
- Add Authentication service inside **ConfigureServices** method above **services.AddMvc();** line
    ```csharp 
        services.AddAuthentication(AzureADB2CDefaults.AuthenticationScheme)
                .AddAzureADB2C(options => Configuration.Bind("AzureAdB2C", options));
    ```
- Add configurations to **Configure** method below **app.UseStaticFiles();** line
    ```csharp 
        app.UseHttpsRedirection();
        app.UseAuthentication();
    ```
- in the **HomeController.cs** add the using statement
    ```csharp 
        using Microsoft.AspNetCore.Authorization;
    ```
- Add the **[Authorize]** attibute to **HomeController** class
    ```csharp 
        [Authorize]
        public class HomeController : Controller
        {
            private async Task<IParkmeterApi> InitializeClient()
            {
                IParkmeterApi _apiClient = new ParkmeterApi();
                _apiClient.BaseUri = new Uri(Configuration["Parkmeter:ApiUrl"]);

                return _apiClient;
            }
    ```


# 2 - Azure Functions

- Create a new Azure Functions Project (**Empty template**)
- Add COSMOS DB url in **local.settings.json**
    ```json 
        "ConnectionStrings": {
          "CosmosDBEndpoint": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
        },
    ```
- Add reference to project **Parkmeter.Core**
- Add NuGet package
  - **Microsoft.Azure.WebJobs.Extensions.CosmosDB**
- Add the two source files **VehicleAccessDocument.cs** + **CosmosDBFunctions.cs**
- in the project **Parkmeter.Data.NoSql** add the File **LedgerServerLess.cs**
- in the project **Parkmeter.Persistence** change the dependency injection in **PersistenceManager.cs** (line 43):
    ```csharp 
        // AccessLedger = new Ledger(ledgerEndpoint, ledgerKey);
        AccessLedger = new LedgerServerLess(ledgerEndpoint);
    ```
    - Modify **Initialize** method signature as
        ```csharp        
            public void Initialize(Uri functionsEndpoint, string sqlConnectionString)
       ```
    - Remove **String.IsNullOrEmpty(ledgerKey)** from the first if condition
- in the project **Parkmeter.Api** we need to configure the new functions.
  - Add endpoint configuration in **appsettings.json**:
    ```json
        {
            ...
    
            "CosmosDBFunctions": {
                "Endpoint": "http://localhost:7071/api/"
            }
            ...
        }
    ```
  - in **Startup.cs** modify method **ConfigureServices** as
    ```csharp
        //store.Initialize(
        //new Uri(Configuration["DocumentDB:Endpoint"]),
        //Configuration["DocumentDB:Key"],
        //Configuration["ConnectionStrings:Default"]);

        store.Initialize(
        new Uri(Configuration["CosmosDBFunctions:Endpoint"]),
        Configuration["ConnectionStrings:Default"]);
    ```
- Copy **run.ps1** and **package.json** in the solution root folder (replacing existing files)
