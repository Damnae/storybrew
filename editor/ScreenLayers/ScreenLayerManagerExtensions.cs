using BrewLib.ScreenLayers;
using BrewLib.Util;
using StorybrewEditor.ScreenLayers.Util;
using StorybrewEditor.Storyboarding;
using System;
using System.Collections.Generic;
using System.IO;

namespace StorybrewEditor.ScreenLayers
{
    public static class ScreenLayerManagerExtensions
    {
        public static void OpenFolderPicker(this ScreenLayerManager screenLayerManager, string description, string initialValue, Action<string> callback)
        {
            screenLayerManager.AsyncLoading("Select a folder", () =>
            {
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog()
                {
                    Description = description,
                    ShowNewFolderButton = true,
                    SelectedPath = initialValue,
                })
                    if (dialog.ShowDialog(screenLayerManager.GetContext<Editor>().FormsWindow) == System.Windows.Forms.DialogResult.OK)
                    {
                        var path = dialog.SelectedPath;
                        Program.Schedule(() => callback.Invoke(path));
                    }
            });
        }

        public static void OpenFilePicker(this ScreenLayerManager screenLayerManager, string description, string initialValue, string initialDirectory, string filter, Action<string> callback)
        {
            screenLayerManager.AsyncLoading("Select a file", () =>
            {
                using (var dialog = new System.Windows.Forms.OpenFileDialog()
                {
                    Title = description,
                    RestoreDirectory = true,
                    ShowHelp = false,
                    FileName = initialValue,
                    Filter = filter,
                    InitialDirectory = initialDirectory != null ? Path.GetFullPath(initialDirectory) : string.Empty,
                })
                    if (dialog.ShowDialog(screenLayerManager.GetContext<Editor>().FormsWindow) == System.Windows.Forms.DialogResult.OK)
                    {
                        var path = dialog.FileName;
                        Program.Schedule(() => callback.Invoke(path));
                    }
            });
        }

        public static void OpenSaveLocationPicker(this ScreenLayerManager screenLayerManager, string description, string initialValue, string extension, string filter, Action<string> callback)
        {
            screenLayerManager.AsyncLoading("Select a location", () =>
            {
                using (var dialog = new System.Windows.Forms.SaveFileDialog()
                {
                    Title = description,
                    RestoreDirectory = true,
                    ShowHelp = false,
                    FileName = initialValue,
                    OverwritePrompt = true,
                    DefaultExt = extension,
                    Filter = filter,
                })
                    if (dialog.ShowDialog(screenLayerManager.GetContext<Editor>().FormsWindow) == System.Windows.Forms.DialogResult.OK)
                    {
                        var path = dialog.FileName;
                        Program.Schedule(() => callback.Invoke(path));
                    }
            });
        }

        public static void AsyncLoading(this ScreenLayerManager screenLayerManager, string message, Action action)
            => screenLayerManager.Add(new LoadingScreen(message, action));

        public static void ShowMessage(this ScreenLayerManager screenLayerManager, string message, Action okAction = null)
            => screenLayerManager.Add(new MessageBox(message, okAction));

        public static void ShowMessage(this ScreenLayerManager screenLayerManager, string message, Action okAction, bool cancelable)
            => screenLayerManager.Add(new MessageBox(message, okAction, cancelable));

        public static void ShowMessage(this ScreenLayerManager screenLayerManager, string message, Action yesAction, Action noAction, bool cancelable)
            => screenLayerManager.Add(new MessageBox(message, yesAction, noAction, cancelable));

        public static void ShowPrompt(this ScreenLayerManager screenLayerManager, string title, Action<string> action)
            => screenLayerManager.Add(new PromptBox(title, string.Empty, string.Empty, action));

        public static void ShowPrompt(this ScreenLayerManager screenLayerManager, string title, string description, Action<string> action)
            => screenLayerManager.Add(new PromptBox(title, description, string.Empty, action));

        public static void ShowPrompt(this ScreenLayerManager screenLayerManager, string title, string description, string initialText, Action<string> action)
            => screenLayerManager.Add(new PromptBox(title, description, initialText, action));

        public static void ShowContextMenu<T>(this ScreenLayerManager screenLayerManager, string title, Action<T> action, params ContextMenu<T>.Option[] options)
            => screenLayerManager.Add(new ContextMenu<T>(title, action, options));

        public static void ShowContextMenu<T>(this ScreenLayerManager screenLayerManager, string title, Action<T> action, params T[] options)
            => screenLayerManager.Add(new ContextMenu<T>(title, action, options));

        public static void ShowContextMenu<T>(this ScreenLayerManager screenLayerManager, string title, Action<T> action, IEnumerable<T> options)
            => screenLayerManager.Add(new ContextMenu<T>(title, action, options));

        public static void ShowOpenProject(this ScreenLayerManager screenLayerManager)
        {
            if (!Directory.Exists(Project.ProjectsFolder))
                Directory.CreateDirectory(Project.ProjectsFolder);

            screenLayerManager.OpenFilePicker("", "", Project.ProjectsFolder, Project.FileFilter, (projectPath) =>
            {
                if (!PathHelper.FolderContainsPath(Project.ProjectsFolder, projectPath))
                    screenLayerManager.ShowMessage("Projects must be placed in a folder inside the 'projects' folder.");
                else
                    screenLayerManager.AsyncLoading("Loading project", () =>
                    {
                        var project = Project.Load(projectPath, true);
                        Program.Schedule(() => screenLayerManager.Set(new ProjectMenu(project)));
                    });
            });
        }
    }
}
