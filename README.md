C# Code Sharing Report
===============

Code sharing report for comparing percentage of shared code with *.csproj files.

Originally created by @praeclarum [here](https://gist.github.com/praeclarum/1608597/raw/66b9beee6024a892ef355e24134dad196e48a5f7/CodeShareReport.cs).

Changes from original include:

- Ignore case on paths - (don't know why this was an issue in our project)
- Added a list of Regex for ignoring certain files - (we had a few files that didn't need to count in the report)
