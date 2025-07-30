using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using winui_scripts_app.Services;
using winui_scripts_app.ViewModels;
using winui_scripts_app.Models;

namespace winui_scripts_app
{
    /// <summary>
    /// Main window for the Script Runner app.
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

            // Responsive layout handler
            this.SizeChanged += MainWindow_SizeChanged;
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

        // Responsive layout logic for script item buttons
        private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            double width = e.Size.Width;

            var root = this.Content as DependencyObject;
            if (root == null)
                return;

            foreach (var buttonsPanel in FindVisualChildren<StackPanel>(root))
            {
                if (buttonsPanel.Name == "ButtonsPanel")
                {
                    if (width < 500)
                    {
                        buttonsPanel.Orientation = Orientation.Vertical;
                        Grid.SetRow(buttonsPanel, 1);
                        Grid.SetColumn(buttonsPanel, 0);
                        buttonsPanel.Margin = new Thickness(0, 10, 0, 0);
                        buttonsPanel.HorizontalAlignment = HorizontalAlignment.Left;

                        foreach (var button in buttonsPanel.Children.OfType<Button>())
                        {
                            button.Margin = new Thickness(0, 10, 0, 0);
                        }
                    }
                    else
                    {
                        buttonsPanel.Orientation = Orientation.Horizontal;
                        Grid.SetRow(buttonsPanel, 0);
                        Grid.SetColumn(buttonsPanel, 1);

                        foreach (var button in buttonsPanel.Children.OfType<Button>())
                        {
                            button.Margin = new Thickness(10, 0, 0, 0);
                        }
                    }
                }
            }
        }

        private void MainNavView_SelectionChanged(object sender, NavigationViewSelectionChangedEventArgs e)
        {
            if (e.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            {
                switch (tag)
                {
                    case "Refresh":
                        ViewModel.RefreshCommand.Execute(null);
                        break;
                    case "OpenFolder":
                        ViewModel.OpenFolderCommand.Execute(null);
                        break;
                }
            }

            // Optionally, clear selection so the user can click the same item again
            MainNavView.SelectedItem = null;
        }

        // Helper to find all StackPanels in the visual tree
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                int count = VisualTreeHelper.GetChildrenCount(depObj);
                for (int i = 0; i < count; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child is T t)
                        yield return t;

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                        yield return childOfChild;
                }
            }
        }
    }
}