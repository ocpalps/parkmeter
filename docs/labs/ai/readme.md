# Artificial Intelligence Lab

The goal of this lab is to improve the solution with some **Artifical Intelligence** functionalities:

- License Plate (text) recognition
- Chat Assistant (Bot)

# 0 - Getting started

- download the **stage 1** source code from [here](https://github.com/ocpalps/parkmeter/archive/1.zip)
- unzip the folder and execute **run.ps1**. The project must build successfully and launches two console applications. You might need to give missing persmissions to execute the powershell script:
```powershell
Set-ExecutionPolicy Unrestricted
```
- Browse to [http://localhost:50058/](http://localhost:50058/) and verify that the website works and allows to add a new parking

# 1 - License Plate (text) recognition


## New API for Vechicle Status

- In the project *Parkmeter.Functions*
    - Add a new function copying & pasting the content of **LastAccessFunction.txt** into *CosmosDBFunctions.cs* 
- In the project *Parkmeter.Data.NoSql*
    - Add a new method copying & pasting the content of **LastAccessMethod.txt** into *LedgerServerLess.cs*
- In the project *Parkmeter.Persistence*
    - Add a new method signature to *ILedger.cs*

    ```csharp
     Task<VehicleAccess> GetLastVehicleAccessAsync(int parkingId, string vehicleId);
    ```
- In the project *Parkmeter.Persistence*
    - Add a new method implementation copying & pasting the content of **LastAccessMethod2.txt** into *PersistenceManager.cs* 

## Cognitive Service

- Login to Azure portal and create a new **Cognitive Service** service of type **Computer Vision**
- Go to the **Keys** page and copy the value for **Key 1**
- Back to source code and open the solution. 
- In the project *Parkmeter.Api* add the copied value as a new setting in *appsettings.json*

    ```json
    "CognitiveServices": "XXXXXXXXXX"
    ```

- In the project *Parkmeter.Api*
    - add a new *Controller* adding the file **ServicesController.cs**
    - add Cognitive Service nuget: **Microsoft.Azure.CognitiveServices.Vision.ComputerVision** (v. 3.3.0)

Run the **run.ps1** script to build and launch the solution

# 2 - Chat Assistant (Bot)

