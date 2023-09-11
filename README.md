# Anonymous Photo Bin

https://github.com/IsaacSchemm/AnonymousPhotoBin

This is a web app (ASP.NET Core) designed for a group of people to easily upload photos to a central location without logging in. All they need to know is the URL.

Anonymous Photo Bin is designed to be used for a short period of time (e.g. during a weeklong event.) After the event is over, an administrator can sort and download the pictures, and then shut down the website.

## Deploying on Azure

To use this app, you have to deploy it yourself. The most straightforward way is by creating a Microsoft Azure account.

1. Create a Microsoft SQL database. A Basic database should be fine, because it's only storing metadata about the uploaded files.

2. Create a web app. Give it a URL that's hard to guess or hit on by accident. Anyone with the URL will be able to upload files.

3. Set up an Azure storage account. This will be used for blob storage - it will hold the actual data of the uploaded files.

4. Set up the application settings:

    a. FileManagementPassword (app setting)
	
    b. SqlConnectionString (SQL connection string)
    
    c. AzureStorageConnectionString (Azure storage connection string)

Users will need to enter the FileManagementPassword to view the "list" page and to edit or delete uploaded files. However, note that the URLs to download pictures are *not* password-protected (although they each contain a randomly generated GUID).
