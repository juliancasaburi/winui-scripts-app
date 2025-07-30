using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using winui_scripts_app.Services;
using winui_scripts_app.ViewModels;
using winui_scripts_app.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace winui_scripts_app
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();

            // Initialize services
            var historyService = new ExecutionHistoryService();
            var scriptService = new ScriptService(historyService);

            // Initialize ViewModel
            ViewModel = new MainViewModel(scriptService);

            // Set DataContext on the root element
            if (Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = ViewModel;
            }
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ScriptInfo script)
            {
                ViewModel.RunScriptCommand.Execute(script);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ScriptInfo script)
            {
                ViewModel.DeleteScriptCommand.Execute(script);
            }
        }
    }
}
