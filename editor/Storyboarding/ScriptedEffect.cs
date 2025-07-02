﻿using StorybrewCommon.Scripting;
using StorybrewEditor.Scripting;
using StorybrewEditor.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace StorybrewEditor.Storyboarding
{
    public class ScriptedEffect : Effect
    {
        private readonly ScriptContainer<StoryboardObjectGenerator> scriptContainer;

        private readonly Stopwatch statusStopwatch = new Stopwatch();
        private string configScriptIdentifier;
        private MultiFileWatcher dependencyWatcher;

        public override string BaseName => scriptContainer?.Name;
        public override string Path => scriptContainer?.MainSourcePath;

        private EffectStatus status = EffectStatus.Initializing;
        public override EffectStatus Status => status;
        private string statusMessage = string.Empty;
        public override string StatusMessage => statusMessage;

        private bool multithreaded;
        public override bool Multithreaded => multithreaded;

        private CancellationTokenSource cancellationTokenSource;

        private bool beatmapDependant = true;
        public override bool BeatmapDependant => beatmapDependant;

        public ScriptedEffect(Project project, ScriptContainer<StoryboardObjectGenerator> scriptContainer, bool multithreaded = false) : base(project)
        {
            statusStopwatch.Start();

            this.scriptContainer = scriptContainer;
            scriptContainer.OnScriptChanged += scriptContainer_OnScriptChanged;

            this.multithreaded = multithreaded;
        }

        /// <summary>
        /// Should only be called by Project.QueueEffectUpdate(Effect).
        /// Doesn't run on the main thread.
        /// </summary>
        public override void Update(CancellationTokenSource cancellationTokenSource)
        {
            if (!scriptContainer.HasScript) return;

            var newDependencyWatcher = new MultiFileWatcher();
            newDependencyWatcher.OnFileChanged += (sender, e) =>
            {
                if (IsDisposed) return;
                Refresh();
            };

            var context = new EditorGeneratorContext(this,
                Project.ProjectFolderPath, Project.ProjectAssetFolderPath,
                Project.MapsetPath, Project.MainBeatmap, Project.MapsetManager.Beatmaps,
                cancellationTokenSource.Token, newDependencyWatcher);
            var success = false;
            try
            {
                Program.RunMainThread(() => this.cancellationTokenSource = cancellationTokenSource);

                cancellationTokenSource.Token.ThrowIfCancellationRequested();

                changeStatus(EffectStatus.Loading);
                var script = scriptContainer.CreateScript();

                cancellationTokenSource.Token.ThrowIfCancellationRequested();

                changeStatus(EffectStatus.Configuring);
                Program.RunMainThread(() =>
                {
                    beatmapDependant = true;
                    if (script.Identifier != configScriptIdentifier)
                    {
                        script.UpdateConfiguration(Config);
                        configScriptIdentifier = script.Identifier;

                        RaiseConfigFieldsChanged();
                    }
                    else script.ApplyConfiguration(Config);
                });

                cancellationTokenSource.Token.ThrowIfCancellationRequested();

                changeStatus(EffectStatus.Updating);
                script.Generate(context);

                cancellationTokenSource.Token.ThrowIfCancellationRequested();

                foreach (var layer in context.EditorLayers)
                    layer.PostProcess(cancellationTokenSource.Token);

                success = true;
            }
            catch (ScriptCompilationException e)
            {
                Debug.Print($"Script compilation failed for {BaseName}\n{e.Message}");
                changeStatus(EffectStatus.CompilationFailed, e.Message, context.Log);
                return;
            }
            catch (ScriptLoadingException e)
            {
                Debug.Print($"Script load failed for {BaseName}\n{e}");
                changeStatus(EffectStatus.LoadingFailed, e.InnerException != null ? $"{e.Message}: {e.InnerException.Message}" : e.Message, context.Log);
                return;
            }
            catch (Exception e)
            {
                var inner = e;
                while (inner != null)
                {
                    if (inner is OperationCanceledException)
                    {
                        Debug.Print($"Script operation canceled for {BaseName}");
                        changeStatus(EffectStatus.UpdateCanceled);
                        return;
                    }
                    inner = e.InnerException;
                }

                changeStatus(EffectStatus.ExecutionFailed, getExecutionFailedMessage(e), context.Log);
                return;
            }
            finally
            {
                if (!success)
                {
                    if (dependencyWatcher != null)
                    {
                        dependencyWatcher.Watch(newDependencyWatcher.WatchedFilenames);
                        newDependencyWatcher.Dispose();
                        newDependencyWatcher = null;
                    }
                    else dependencyWatcher = newDependencyWatcher;
                }
                context.DisposeResources();
            }
            changeStatus(EffectStatus.Ready, null, context.Log);

            Program.Schedule(() =>
            {
                if (IsDisposed)
                {
                    newDependencyWatcher.Dispose();
                    return;
                }

                multithreaded = context.Multithreaded;
                beatmapDependant = context.BeatmapDependent;
                dependencyWatcher?.Dispose();
                dependencyWatcher = newDependencyWatcher;

                if (Project.IsDisposed)
                    return;

                UpdateLayers(context.EditorLayers);
            });
        }

        public override void CancelUpdate()
        {
            cancellationTokenSource?.Cancel();
        }

        private void scriptContainer_OnScriptChanged(object sender, EventArgs e)
            => Refresh();

        private void changeStatus(EffectStatus status, string message = null, string log = null)
        {
            Program.Schedule(() =>
            {
                var duration = statusStopwatch.ElapsedMilliseconds;
                if (duration > 0)
                    switch (this.status)
                    {
                        case EffectStatus.Ready:
                        case EffectStatus.CompilationFailed:
                        case EffectStatus.LoadingFailed:
                        case EffectStatus.ExecutionFailed:
                        case EffectStatus.UpdateCanceled:
                            break;
                        default:
                            Debug.Print($"{BaseName}'s {this.status} status took {duration}ms");
                            break;
                    }

                this.status = status;
                statusMessage = message ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(log))
                {
                    if (!string.IsNullOrWhiteSpace(statusMessage))
                        statusMessage += "\n\n";
                    statusMessage += $"Log:\n\n{log}";
                }
                RaiseChanged();

                statusStopwatch.Restart();
            });
        }

        private string getExecutionFailedMessage(Exception e)
        {
            if (e is FileNotFoundException)
                return $"File not found while {status}. Make sure this path is correct:\n{(e as FileNotFoundException).FileName}\n\nDetails:\n{e}";

            return $"Unexpected error during {status}:\n{e}";
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    dependencyWatcher?.Dispose();
                    scriptContainer.OnScriptChanged -= scriptContainer_OnScriptChanged;
                }
                dependencyWatcher = null;
                disposedValue = true;
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
