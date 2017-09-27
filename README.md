# Anonymous Photo Bin

https://github.com/IsaacSchemm/AnonymousPhotoBin

This is a web app (ASP.NET Core) designed for a group of people to easily upload photos to a central location without logging in. All they need to know is the URL.

Anonymous Photo Bin is designed to be used for a short period of time (e.g. during a weeklong event.) After the event is over, an administrator can sort and download the pictures, and then shut down the website.

## Deploying on Azure

To use this app, you have to deploy it yourself. The most straightforward way is by creating a Microsoft Azure account.

1. Create a Microsoft SQL database. The database should be big enough to hold all the files that people upload (up to 100 MB; video files are allowed) and fast enough to store them in a reasonable amount of time (under 2 minutes per file). A Basic database probably won't be sufficient.

2. Create a web app. Give it a URL that's hard to guess or hit on by accident. Anyone with the URL will be able to upload files.

3. Set up the application settings:

    a. FileManagementPassword (app setting): the password required to edit file metadata ("taken by" or "category") or delete files from the server.
	
	b. DefaultConnection (connection string): the SQL connection string for the database you just set up.
