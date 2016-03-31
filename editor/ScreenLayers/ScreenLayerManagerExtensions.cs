﻿using StorybrewEditor.ScreenLayers.Util;
using System;

namespace StorybrewEditor.ScreenLayers
{
    public static class ScreenLayerManagerExtensions
    {
        public static void OpenFolderPicker(this ScreenLayerManager screenLayerManager, string description, string initialValue, Action<string> callback)
        {
            screenLayerManager.AsyncLoading("Select a folder...", () =>
            {
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog()
                {
                    Description = description,
                    ShowNewFolderButton = true,
                    SelectedPath = initialValue,
                })
                    if (dialog.ShowDialog(screenLayerManager.Editor.FormsWindow) == System.Windows.Forms.DialogResult.OK)
                    {
                        var path = dialog.SelectedPath;
                        Program.Schedule(() => callback.Invoke(path));
                    }
            });
        }

        public static void OpenFilePicker(this ScreenLayerManager screenLayerManager, string description, string initialValue, string filter, Action<string> callback)
        {
            screenLayerManager.AsyncLoading("Select a file...", () =>
            {
                using (var dialog = new System.Windows.Forms.OpenFileDialog()
                {
                    Title = description,
                    RestoreDirectory = true,
                    ShowHelp = false,
                    FileName = initialValue,
                    Filter = filter,
                })
                    if (dialog.ShowDialog(screenLayerManager.Editor.FormsWindow) == System.Windows.Forms.DialogResult.OK)
                    {
                        var path = dialog.FileName;
                        Program.Schedule(() => callback.Invoke(path));
                    }
            });
        }

        public static void OpenSaveLocationPicker(this ScreenLayerManager screenLayerManager, string description, string initialValue, string extension, string filter, Action<string> callback)
        {
            screenLayerManager.AsyncLoading("Select a location...", () =>
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
                    if (dialog.ShowDialog(screenLayerManager.Editor.FormsWindow) == System.Windows.Forms.DialogResult.OK)
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

        public static void ShowName(this ScreenLayerManager screenLayerManager, string initialName, Action<string> renameAction)
            => screenLayerManager.Add(new NameBox(initialName, renameAction));
    }
}
