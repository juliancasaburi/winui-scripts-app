# Script Runner - WinUI Application Design Document

## Overview

A .NET WinUI 3 application that provides a user-friendly interface for managing and executing VBScript files. Users can place VB script files in a designated folder, and the application will automatically discover, list, and allow execution of these scripts while tracking their execution history.

## Features

### Core Functionality
- **Script Discovery**: Automatically scan a designated folder for `.vbs` files
- **Script Listing**: Display all discovered scripts in a clean, organized UI
- **Script Execution**: Run selected script with a single click
- **Script Removal**: Delete scripts directly from the application
- **Execution Tracking**: Display the last execution time for each script
- **Real-time Updates**: Automatically refresh the script list when files are added/removed

### User Interface Components
- Main window with script list view
- Execution status indicators
- Script management controls (Run, Delete)
- File system watcher for automatic updates

## Architecture

### Technology Stack
- **Framework**: .NET 8+ with WinUI 3
- **Language**: C#
- **Pattern**: MVVM (Model-View-ViewModel)
- **Script Execution**: System.Diagnostics.Process with cscript.exe
- **File Monitoring**: FileSystemWatcher
- **Data Persistence**: JSON file for execution history

### Project Structure
```
VBScriptRunner/
├── Models/
│   ├── ScriptInfo.cs
│   └── ExecutionHistory.cs
├── ViewModels/
│   ├── MainViewModel.cs
│   └── ScriptItemViewModel.cs
├── Views/
│   ├── MainWindow.xaml
│   └── MainWindow.xaml.cs
├── Services/
│   ├── IScriptService.cs
│   ├── ScriptService.cs
│   ├── IExecutionHistoryService.cs
│   └── ExecutionHistoryService.cs
├── Helpers/
│   ├── RelayCommand.cs
│   └── FileSystemHelper.cs
└── Assets/
    └── Icons/
```

## Data Models

### ScriptInfo Model
```csharp
public class ScriptInfo
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public DateTime? LastExecuted { get; set; }
    public long FileSize { get; set; }
    public bool IsExecuting { get; set; }
}
```

### ExecutionHistory Model
```csharp
public class ExecutionHistory
{
    public Dictionary<string, DateTime> ScriptExecutions { get; set; } = new();
    
    public void UpdateExecution(string scriptPath)
    {
        ScriptExecutions[scriptPath] = DateTime.Now;
    }
    
    public DateTime? GetLastExecution(string scriptPath)
    {
        return ScriptExecutions.TryGetValue(scriptPath, out var lastExec) ? lastExec : null;
    }
}
```

## Services

### IScriptService Interface
```csharp
public interface IScriptService
{
    Task<List<ScriptInfo>> GetScriptsAsync();
    Task<bool> ExecuteScriptAsync(string scriptPath);
    Task<bool> DeleteScriptAsync(string scriptPath);
    event EventHandler<ScriptInfo> ScriptAdded;
    event EventHandler<string> ScriptRemoved;
    void StartWatching();
    void StopWatching();
}
```

### IExecutionHistoryService Interface
```csharp
public interface IExecutionHistoryService
{
    Task SaveExecutionAsync(string scriptPath, DateTime executionTime);
    Task<DateTime?> GetLastExecutionAsync(string scriptPath);
    Task<ExecutionHistory> LoadHistoryAsync();
}
```

## User Interface Design

### Main Window Layout
```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
        <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>
    
    <!-- Header -->
    <StackPanel Grid.Row="0" Orientation="Horizontal" Padding="16">
        <TextBlock Text="Script Runner" Style="{StaticResource HeaderTextBlockStyle}"/>
        <Button Content="Refresh" Command="{Binding RefreshCommand}" Margin="16,0,0,0"/>
        <Button Content="Open Scripts Folder" Command="{Binding OpenFolderCommand}" Margin="8,0,0,0"/>
    </StackPanel>
    
    <!-- Script List -->
    <ListView Grid.Row="1" ItemsSource="{Binding Scripts}" SelectedItem="{Binding SelectedScript}">
        <ListView.ItemTemplate>
            <DataTemplate>
                <Grid Margin="8" Padding="12" Background="{ThemeResource CardBackgroundFillColorDefaultBrush}">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    
                    <StackPanel Grid.Column="0">
                        <TextBlock Text="{Binding Name}" FontWeight="SemiBold" FontSize="16"/>
                        <TextBlock Text="{Binding LastExecutedDisplay}" Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
                        <TextBlock Text="{Binding FileSizeDisplay}" Foreground="{ThemeResource TextFillColorTertiaryBrush}" FontSize="12"/>
                    </StackPanel>
                    
                    <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center">
                        <Button Content="Run" Command="{Binding DataContext.RunScriptCommand, RelativeSource={RelativeSource AncestorType=ListView}}" 
                                CommandParameter="{Binding}" IsEnabled="{Binding IsNotExecuting}" Margin="0,0,8,0"/>
                        <Button Content="Delete" Command="{Binding DataContext.DeleteScriptCommand, RelativeSource={RelativeSource AncestorType=ListView}}" 
                                CommandParameter="{Binding}" Style="{StaticResource AccentButtonStyle}"/>
                        <ProgressRing IsActive="{Binding IsExecuting}" Width="24" Height="24" Margin="8,0,0,0"/>
                    </StackPanel>
                </Grid>
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
    
    <!-- Status Bar -->
    <StatusBar Grid.Row="2">
        <StatusBarItem>
            <TextBlock Text="{Binding StatusMessage}"/>
        </StatusBarItem>
        <StatusBarItem HorizontalAlignment="Right">
            <TextBlock Text="{Binding ScriptCount, StringFormat='Scripts: {0}'}"/>
        </StatusBarItem>
    </StatusBar>
</Grid>
```

## Configuration

### Application Settings
```json
{
  "ScriptsFolder": "%USERPROFILE%\\Documents\\Scripts",
  "HistoryFile": "execution_history.json",
  "AutoRefreshInterval": 5000,
  "MaxExecutionTimeout": 300000
}
```

### Scripts Folder Structure
```
%USERPROFILE%\Documents\Scripts\
├── script1.vbs
├── script2.vbs
├── utilities\
│   ├── helper.vbs
│   └── cleanup.vbs
└── archived\
    └── old_script.vbs
```

## Implementation Details

### Script Execution Process
1. Validate script file exists and has `.vbs` extension
2. Update UI to show execution in progress
3. Execute using `cscript.exe` with proper parameters
4. Capture output and error streams
5. Update execution history with timestamp
6. Display results to user
7. Reset UI state

### File System Monitoring
- Use `FileSystemWatcher` to monitor the scripts folder
- React to Created, Deleted, and Renamed events
- Debounce rapid file system changes
- Update UI on the main thread

### Error Handling
- Script execution failures with detailed error messages
- File access permission issues
- Invalid script file formats
- Network drive availability (if scripts folder is on network)