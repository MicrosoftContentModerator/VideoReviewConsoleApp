# VideoReviewConsoleApp
Create Video Reviews in Content Moderator
## Getting Started
### For a Single Video File
Build and run the code, a console should be present with the following messege:
#### Enter the fully qualified local path for Uploading the video :
Format of the local path can be in quotes i.e. "C:\{path}" or without quotes i.e. C:\{path}
### Video Transcription Option
Choosing Y will use your Azure Media Services account to generate the video transcript.

#### For Tagging video frames in the Video Review
The transcription is screened for Profanity using Content Moderator Text API. This API screens the text for known profanity terms in 100+ languages. For english, this API also returns classification scores for Adult, Racy and Offensive categories.

Video frames whose transcript are flagged by the Text Screen API can be automatically flagged in the video review.

To enable this, add the following Tags to your Content Moderator Team:

| Name           | Short Code  |
|:--------------:|:-----------:|
| Adult Text     | at          |
| Racy Text      | rt          |
| Offensive Text | ot          |

### For Processing Multiple Videos
Build the executable file and run the .exe with parameters as follows
{ConsoleApp.exe path} {Video Folder Path} {Transcription option[y/n]}