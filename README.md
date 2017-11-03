# VideoReviewConsoleApp
Create Video Reviews in Content Moderator
## Getting Started
### For a Single Video File
Build and run the code, a console should be present with the following messege:
#### Enter the fully qualified local path for Uploading the video :
Format of the local path can be in quotes i.e. "C:\{path}" or without quotes i.e. C:\{path}
### Transcription Option
Choose y for transcription n for no transcription
#### Transcription Screentext Tagging
In order for transcription screentext result to work, a user must add Adult Text(at), Racy Text(rt), and Offensive Text(ot) tags to the team via Setting->Tags page.
### For Processing Multiple Videos
Build the executable file and run the .exe with parameters as follows
{ConsoleApp.exe path} {Video Folder Path} {Transcription option[y/n]}