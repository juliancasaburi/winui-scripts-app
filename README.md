# Script Runner - WinUI 3 Application

A modern .NET 8 WinUI 3 application that provides a user-friendly interface for managing and executing VBScript files.

## Features

- **Script Discovery**: Automatically scans the `Documents\Scripts` folder.
- **Script Listing**: Displays all discovered scripts in a clean, organized UI
- **Script Execution**: Run selected scripts with a single click
- **Script Management**: Delete scripts directly from the application
- **Execution Tracking**: Displays the last execution time for each script
- **Real-time Updates**: Automatically refreshes the script list when files are added/removed
- **Modern UI**: Built with WinUI 3 featuring Mica backdrop and modern design elements

## Getting Started

### Prerequisites

- Windows 10 version 1809 (build 17763) or later
- .NET 8 Runtime
- Windows App SDK 1.7 or later

### Installation

1. Download and run the MSIX package from the releases
2. Or build from source using Visual Studio 2022

### Usage

1. **Launch the Application**: Start "Script Runner" from the Start Menu
2. **Add Scripts**: Place your scripts in the `Documents\Scripts` folder
   - You can click "Open Scripts Folder" to navigate directly to this location
3. **Run Scripts**: Click the "Run" button next to any script to execute it
4. **Monitor Execution**: Watch the status bar for execution feedback and progress
5. **View History**: See when each script was last executed
6. **Manage Scripts**: Use the "Delete" button to remove scripts you no longer need

### Script Folder Structure

The application monitors the following folder:
```
%USERPROFILE%\Documents\Scripts\
├── script1.vbs
├── script2.vbs
├── subfolder\
│   └── another_script.vbs
└── utilities\
    └── helper_script.vbs
```

## Sample VBScript

Here's a simple VBScript you can use to test the application:

```vbscript
' Sample VBScript - Hello World
Dim message
message = "Hello from Script Runner!"
MsgBox message, vbInformation, "Sample Script"
WScript.Echo "Script executed at " & Now
```

Save this as `hello_world.vbs` in your `Documents\Scripts` folder.

## Architecture

### Technology Stack
- **Framework**: .NET 8 with WinUI 3
- **Language**: C#
- **Pattern**: MVVM (Model-View-ViewModel)
- **Script Execution**: System.Diagnostics.Process with cscript.exe
- **File Monitoring**: FileSystemWatcher
- **Data Persistence**: JSON file for execution history

### Key Components

1. **Models**:
   - `ScriptInfo`: Represents a VBScript file with metadata
   - `ExecutionHistory`: Tracks when scripts were last executed

2. **Services**:
   - `IScriptService` / `ScriptService`: Manages script discovery, execution, and file monitoring
   - `IExecutionHistoryService` / `ExecutionHistoryService`: Handles execution history persistence

3. **ViewModels**:
   - `MainViewModel`: Main application logic and data binding

4. **Views**:
   - `MainWindow`: Primary application interface

## Execution History

The application automatically tracks when each script is executed and stores this information in:
```
%LOCALAPPDATA%\VBScriptRunner\execution_history.json
```

## File System Monitoring

The application uses `FileSystemWatcher` to automatically detect when scripts are:
- Added to the scripts folder
- Removed from the scripts folder  
- Renamed in the scripts folder

The UI updates in real-time without requiring manual refresh.

## Building from Source

### Requirements
- Visual Studio 2022 (17.8 or later)
- Windows App SDK 1.7 or later
- .NET 8 SDK

### Build Steps
1. Clone the repository
2. Open `winui-scripts-app.sln` in Visual Studio 2022
3. Restore NuGet packages
4. Build the solution (Ctrl+Shift+B)
5. Run the application (F5)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.