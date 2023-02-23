using BrewLib.ScreenLayers;
using BrewLib.Util;
using StorybrewEditor.ScreenLayers.Util;
using StorybrewEditor.Storyboarding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MessageBox = StorybrewEditor.ScreenLayers.Util.MessageBox;

namespace StorybrewEditor.ScreenLayers
{
    public static class ScreenLayerManagerExtensions
    {
        public static void OpenFolderPicker(this ScreenLayerManager screenLayer, string description, string initialValue, Action<string> callback)
        {
            screenLayer.AsyncLoading("Select a folder", () =>
            {
                using (var dialog = new FolderBrowserDialog
                {
                    Description = description,
                    ShowNewFolderButton = true,
                    SelectedPath = initialValue
                })
                if (dialog.ShowDialog(screenLayer.GetContext<Editor>().FormsWindow) == DialogResult.OK)
                {
                    var path = dialog.SelectedPath;
                    Program.Schedule(() => callback.Invoke(path));
                }
            });
        }
        public static void OpenFilePicker(this ScreenLayerManager screenLayer, string description, string initialValue, string initialDirectory, string filter, Action<string> callback)
        {
            screenLayer.AsyncLoading("Select a file", () =>
            {
                using (var dialog = new OpenFileDialog
                {
                    Title = description,
                    RestoreDirectory = true,
                    ShowHelp = false,
                    FileName = initialValue,
                    Filter = filter,
                    InitialDirectory = initialDirectory != null ? Path.GetFullPath(initialDirectory) : string.Empty
                })
                if (dialog.ShowDialog(screenLayer.GetContext<Editor>().FormsWindow) == DialogResult.OK)
                {
                    var path = dialog.FileName;
                    Program.Schedule(() => callback.Invoke(path));
                }
            });
        }
        public static void OpenSaveLocationPicker(this ScreenLayerManager screenLayer, string description, string initialValue, string extension, string filter, Action<string> callback)
        {
            screenLayer.AsyncLoading("Select a location", () =>
            {
                using (var dialog = new SaveFileDialog
                {
                    Title = description,
                    RestoreDirectory = true,
                    ShowHelp = false,
                    FileName = initialValue,
                    OverwritePrompt = true,
                    DefaultExt = extension,
                    Filter = filter
                })
                if (dialog.ShowDialog(screenLayer.GetContext<Editor>().FormsWindow) == DialogResult.OK)
                {
                    var path = dialog.FileName;
                    Program.Schedule(() => callback.Invoke(path));
                }
            });
        }

        public static void AsyncLoading(this ScreenLayerManager screenLayer, string message, Action action)
            => screenLayer.Add(new LoadingScreen(message, action));

        public static void ShowMessage(this ScreenLayerManager screenLayer, string message, Action ok = null)
            => screenLayer.Add(new MessageBox(message, ok));

        public static void ShowMessage(this ScreenLayerManager screenLayer, string message, Action ok, bool cancel)
            => screenLayer.Add(new MessageBox(message, ok, cancel));

        public static void ShowMessage(this ScreenLayerManager screenLayer, string message, Action yes, Action no, bool cancel)
            => screenLayer.Add(new MessageBox(message, yes, no, cancel));

        public static void ShowPrompt(this ScreenLayerManager screenLayer, string title, Action<string> action)
            => screenLayer.Add(new PromptBox(title, string.Empty, string.Empty, action));

        public static void ShowPrompt(this ScreenLayerManager screenLayer, string title, string description, Action<string> action)
            => screenLayer.Add(new PromptBox(title, description, string.Empty, action));

        public static void ShowPrompt(this ScreenLayerManager screenLayer, string title, string description, string text, Action<string> action)
            => screenLayer.Add(new PromptBox(title, description, text, action));

        public static void ShowContextMenu<T>(this ScreenLayerManager screenLayer, string title, Action<T> action, params ContextMenu<T>.Option[] options)
            => screenLayer.Add(new ContextMenu<T>(title, action, options));

        public static void ShowContextMenu<T>(this ScreenLayerManager screenLayer, string title, Action<T> action, params T[] options)
            => screenLayer.Add(new ContextMenu<T>(title, action, options));

        public static void ShowContextMenu<T>(this ScreenLayerManager screenLayer, string title, Action<T> action, IEnumerable<T> options)
            => screenLayer.Add(new ContextMenu<T>(title, action, options));

        public static void ShowOpenProject(this ScreenLayerManager screenLayer)
        {
            if (!Directory.Exists(Project.ProjectsFolder)) Directory.CreateDirectory(Project.ProjectsFolder);

            screenLayer.OpenFilePicker("", "", Project.ProjectsFolder, Project.FileFilter, (projectPath) =>
            {
                if (!PathHelper.FolderContainsPath(Project.ProjectsFolder, projectPath) || PathHelper.GetRelativePath(
                    Project.ProjectsFolder, projectPath).Count(c => c == '/') != 1)
                    screenLayer.ShowMessage("Projects must be placed in a folder directly inside the 'projects' folder.");

                else screenLayer.AsyncLoading("Loading project", () =>
                {
                    var resourceContainer = screenLayer.GetContext<Editor>().ResourceContainer;

                    var project = Project.Load(projectPath, true, resourceContainer);
                    Program.Schedule(() => screenLayer.Set(new ProjectMenu(project)));
                });
            });
        }
    }
}