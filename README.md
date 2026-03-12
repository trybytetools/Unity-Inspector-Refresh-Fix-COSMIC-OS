Unity COSMIC OS Fix — Editor Utility for Linux
A Unity Editor script that resolves common UI refresh issues when running Unity on Linux with the COSMIC desktop environment.
Problems This Fixes

Inspector panel not updating when selecting a different object or asset
Hierarchy panel not reflecting changes or selection state correctly


More fixes may be added over time as issues are discovered.

Installation

In your Unity project, create a folder at Assets/Editor/ (if it doesn't already exist)
Download the .cs script from this repository
Place it inside Assets/Editor/
That's it — Unity will pick it up automatically as an Editor script

Requirements

Unity (any recent LTS version should work)
Linux with COSMIC DE

Known Limitations
This script does not fix every Unity-on-Linux issue — only the specific panel refresh problems listed above. If you run into something else, see below.
Reporting Issues
Not super familiar with GitHub's issue tracker yet, but feel free to open an Issue on this repo and I'll do my best to look into it and push a fix when I can.
Contributing
PRs are welcome if you've found a fix for something else Unity breaks on COSMIC. The more the merrier.
