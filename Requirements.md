I want a C# WPF application that will scan a directory to find files associated with an astrophotography project.
It will walk the tree of directories looking for a folder called `Data`, and within that folder, it will look for subfolders named in the format `NIGHT_YYYY-MM-DD`. Each of these night folders will contain a folder called `LIGHT`, which will hold FITS files named in a specific pattern.
Within the LIGHT folder files will be named like "2026-06-10_03-01-05_L_RMS_7.78_HFR_1.84_ECC_0.77_FWHM_5.78_Stars_479_100_120.00s_-10.00C_0202.fits".
With the filename there will be a letter for the filter that was used (L, R, G, B, S, H & O) with an underscore before and after the filter letter.
There will also be a part of the filename that indicates the length of the exposure in seconds (e.g., 120.00s).
The output should be a pretty summary of each filter and how many files and how many minutes of exposure time there are for each filter. The output should be sorted by filter and then by date. The date should be extracted from the filename and displayed in the format.  The date part will be hidden by default but can be toggled to show in the details. The summary should also include a total for each filter and a grand total for all filters combined.

## Clarifying Questions

1. What exact date format should be displayed in the final summary?
Filter | Date | Number of Files | Total Exposure Time (minutes)
L | 2026-06-10 | 5 | 600.00
L | 2026-06-11 | 3 | 360.00
L | Total | 8 | 960.00
R | 2026-06-10 | 4 | 480.00
...

A top levewl folder can contian many subdirectores for different telescopes and many subdirectories for different projects.  Each project will have a `Data` folder with subfolders for each night. The output should be the concatanationof the directories above the Data folder.  If the specified top level directory contained a folder called `Telescope1` and within that a folder called `ProjectA`, the output should show `Telescope1 - ProjectA` as the project name in the summary. The date format should be displayed as YYYY-MM-DD.  By default the UI should show the telescopes.  If the user clicks to expand a telescope, it should show the projects under that telescope.  If the user clicks to expand a project, it should show the nights under that project.  The summary should be displayed in a DataGrid with the ability to sort by any column.
3. Should exposure time be summed per filter per date, or should the output also include a grand total per filter across all dates?
The output should include both the total per filter per date and a grand total per filter across all dates.
4. When you say sorted by filter and then by date, do you want one section per filter with dates underneath it, or a single table sorted by those two keys?
A single table sorted by filter and then by date would be ideal for easy reading and comparison.
5. Should files that do not match the expected filename pattern be ignored, or should the program report them as warnings?
Ignore files that do not match the expected filename pattern. The program should focus on processing only the correctly formatted files to ensure accurate summaries.
6. Should the tool accept the top-level directory as a required command-line argument, or should it default to the current working directory?
The tool should accept the top-level directory as a required command-line argument to allow flexibility in specifying the location of the data. If no argument is provided, it can default to the current working directory.
UI & Interaction:

Should users be able to collapse/expand all items at once, or only individually? - Users should be able to collapse/expand all items at once for convenience, as well as individually for more granular control.
Should there be a search/filter capability to find specific telescopes, projects, or dates? - No
Do you want the ability to export the summary (CSV, Excel, etc.)? - No
Data Processing & Performance:
4. For large directory structures with many files, should the scan run asynchronously with a progress indicator? - Yes
5. Should there be a refresh button to re-scan the directory, and should results be cached between scans? - Yes
There should be an option to open a new top level directory.
The default top level directory should be the last one used.
The default should be a colapsed view of the telescopes, with the ability to expand to see projects and nights. The summary should be displayed in a DataGrid with the ability to sort by any column.
If there are no fits fiels in the NIGHT folder, the NIGHT folder should be shown but with no list of filters.
If the DATA folder has no NIGHT folders, the DATA folder should be shown but with no list of nights.
Ignore the contents of the fits files.

Error Handling & Logging:
6. How should errors be displayed (e.g., inaccessible directories, permission issues)? - Errors should be displayed in a user-friendly manner, such as a message box or a dedicated error panel within the application. The messages should clearly indicate the nature of the error and suggest possible actions to resolve it (e.g., check permissions, verify directory path).
7. Should there be logging capability for debugging purposes? - No

File Details:
8. When displaying exposure time in the summary, should it be shown only in minutes, or with options for hours as well?
- Exposure time should be shown only in whole minutes for simplicity and consistency in the summary.
9. Should the DataGrid allow copying/exporting of individual rows or the entire table? = No

Scope Edge Cases:
10. What if a single project has multiple Data folders at different levels in the hierarchy? Take only the first Data folder found in the directory tree for each project, and ignore any subsequent Data folders to avoid duplication and confusion in the summary.
11. Should the tool handle symlinks or only physical directories? - Physical only
I want the tool to be deployed using github actions to create a release package that can be downloaded and installed on Windows machines. The release package should include all necessary dependencies and a simple installer to guide users through the installation process.
Please generate a README.md file for this project that includes an overview of the application, installation instructions, usage guide, and any other relevant information for users. The README should be clear and concise, providing all necessary details to help users understand the purpose of the application and how to use it effectively.