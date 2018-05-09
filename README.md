---
services: media-services,functions
platforms: dotnet
author: johndeu
---

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fshigeyf%2Fmedia-services-v3-handson%2Fmaster%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>

# Media Services v3 API: Integrating Azure Media Services with Azure Functions and Logic Apps
This project contains examples of using Azure Functions with Azure Media Services.


## Deploying to Azure
It is **REQUIRED** that you first fork the project and update the "sourceCodeRepositoryURL" in the [azuredeploy.json](azuredeploy.json) template parameters
when deploying to your own Azure account.  That way you can more easily update, experiment and edit the code and see changes
reflected quickly in your own Functions deployment.

We are doing this to save you from our future updates that could break your functions due to continuous integration.

**WARNING**: If you attempt to deploy from the public samples Github repo, and not your own fork, you will see an Error during deployment with a "BadRequest" and an OAuth exception. 


## How to run the sample

To run the samples:
+ Make sure that you have a Media Services Account created, and configure a Service Principal to access it ([Follow this article](https://docs.microsoft.com/en-us/azure/media-services/media-services-portal-get-started-with-aad#service-principal-authentication))
+ first fork this project into your own repository, and then deploy the Functions with the [azuredeploy.json](azuredeploy.json) template
+ Make sure to update the path to point to your github fork
+ Set the **Project** app setting to the desired folder name for the solution sample that you wish to deploy.  
+ If you wish to switch sample projects after deployment, you can simple update the **Project** app setting and then force a GIT Sync through the Continuous Integration settings of your deployed Functions App

The deployment template will automatically create the following Azure resources:
* This Azure Functions application with your source code configured for continuous integration.
* A storage account to run with the functions.
* The required function's application settings will be updated to point to the new resources automatically. You can modify any of these settings after deployment.

Note : if you never provided your GitHub account in the Azure portal before, the continous integration probably will probably fail and you won't see the functions. In that case, you need to setup it manually. Go to your azure functions deployment / Functions app settings / Configure continous integration. Select GitHub as a source and configure it to use your fork.


### License
This sample project is licensed under [the MIT License](LICENSE.txt)
