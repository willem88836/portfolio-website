
The included scripts are part of the project IntoReality, an editing tool with which interaction (e.g. multiple-choice questions, or a branching storyline) can be added to 360-degree videos.
Inside The "Screenshots" folder the mock-up of the tool, and a screenshot of the delivered project can be found.

The scripts Timeline, TimelineChapter, and TimelineEvent are the core of the application.
Timeline represents a complete interactive video.
TimelineChapter represents one video segment (an mp4 file combined with a few events).
TimelineEvent is the base script of any implemented event (see "Events" folder for some implementations).

In the application, the interactive 360-degree video can be exported to be played on (among other things) an Oculus Go.
TimelineSaveLoad saves and loads the timeline, SimpleZipper is used to zip and unzip multiple files into one file.

For the full repo, check out: https://github.com/willem88836/IWP_CanIHelp
For a cool little video of the project, check out: https://www.youtube.com/watch?v=BK0ODXdY_8M&feature=youtu.be
