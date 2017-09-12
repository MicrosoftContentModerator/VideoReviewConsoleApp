# VideoReviewConsoleApp
This repo contains the Windows Console application for Video reviews.
# To Use the VideoReviewConsole Application:

Download the code:

Starting in the folder where you clone the repository (this folder).

Change the app.config value “###” to the Content moderator account specific values:

<appSettings>  
  <add key="AzureMediaServiceAccountKey" value="###" />
  <add key="AzureMediaServiceAccountName" value="###" />
  <add key="StreamingUrlActiveDays" value="365" />
  <add key="ContentModeratorReviewApiSubscriptionKey" value="###" />
  <add key="ContentModeratorApiEndpoint" value="###" />
  <add key="ContentModeratorTeamId" value="###" />  
</appSettings>

Build the Application:
	
Start Microsoft Visual Studio 2015 and select File > Open > Project/Solution. Double-click the Visual Studio 2015 Solution (.sln) file. Press Ctrl+Shift+B, or select Build > Build Solution.

Run the Application:	
	
Click F5 and run the application OR go to bin>debug and double click on the executable file. 

This application can be used to process a Single Video or multiple videos stored in a shared folder.

Single Video process:

Download the application and run after changing the app.conig values as mentioned above. 
A command window will appear and prompt to enter the video path.
  
Enter the video path here and the application will create the video review.

Multiple Videos process:

Open command prompt and give the AMS exe path and Video Folder path. (C:\[AMSConsole.exe path][C:\VideofolderPath])
Application will process each video sequentially.
