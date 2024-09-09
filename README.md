# Anonymous Photo Bin

https://github.com/IsaacSchemm/AnonymousPhotoBin

This is a web app (ASP.NET Core) designed for a group of people to easily upload photos to a central location without logging in. All they need to know is the URL.

Anonymous Photo Bin is designed to be used for a short period of time (e.g. during a weeklong event.) After the event is over, an administrator can sort and download the pictures, and then shut down the website.

## Deploying on Azure

1. Create a Cosmos DB database. Add a container with database ID "AnonymousPhotoBin", container ID "PhotoBinDbContext", and partition key "/__partitionKey".

2. Create a web app. Anyone with the URL will be able to upload files.

3. Set up an Azure storage account with public blob access allowed. This will hold the actual data of the uploaded files.

4. Set up the application settings:

    a. FileManagementPassword (app setting)
	
    b. CosmosDB (connection string)
    
    c. AzureStorageConnectionString (connection string)

Users will need to enter the FileManagementPassword to view the "list" page and to edit or delete uploaded files. However, note that the URLs to download pictures are *not* password-protected (although they each contain a randomly generated GUID).
