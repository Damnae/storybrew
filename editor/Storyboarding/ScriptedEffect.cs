using StorybrewCommon.Scripting;
using StorybrewEditor.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Remoting;

namespace StorybrewEditor.Storyboarding
{
    public class ScriptedEffect : Effect
    {
        private ScriptContainer<StoryboardObjectGenerator> scriptContainer;
        private List<EditorStoryboardLayer> layers;
        private EditorStoryboardLayer placeHolderLayer;
        private Stopwatch statusStopwatch = new Stopwatch();

        private string name;
        public override string Name
        {
            get { return name; }
            set
            {
                if (name == value) return;
                name = value;
                RaiseChanged();
                refreshLayerNames();
            }
        }

        public override string BaseName => scriptContainer.Name;
        public override string Path => scriptContainer.SourcePath;

        private EffectStatus status = EffectStatus.Initializing;
        public override EffectStatus Status => status;
        private string statusMessage = string.Empty;
        public override string StatusMessage => statusMessage;

        public ScriptedEffect(Project project, ScriptContainer<StoryboardObjectGenerator> scriptContainer) : base(project)
        {
            statusStopwatch.Start();

            this.scriptContainer = scriptContainer;
            name = project.GetUniqueEffectName();

            layers = new List<EditorStoryboardLayer>();
            layers.Add(placeHolderLayer = new EditorStoryboardLayer(string.Empty, this));
            refreshLayerNames();

            Project.LayerManager.Add(placeHolderLayer);

            scriptContainer.OnScriptChanged += scriptContainer_OnScriptChanged;
        }

        public override void AddPlaceholder(EditorStoryboardLayer layer)
        {
            if (layers == null)
                throw new InvalidOperationException();

            if (placeHolderLayer != null)
            {
                layers.Remove(placeHolderLayer);
                Project.LayerManager.Remove(placeHolderLayer);
                placeHolderLayer = null;
            }
            layers.Add(layer);
            refreshLayerNames();

            Project.LayerManager.Add(layer);
        }

        /// <summary>
        /// Should only be called by Project.QueueEffectUpdate(Effect).
        /// Doesn't run on the main thread.
        /// </summary>
        public override void Update()
        {
            if (!scriptContainer.HasScript) return;

            var context = new EditorGeneratorContext(this, Project.MainBeatmap);
            try
            {
                changeStatus(EffectStatus.Loading);
                var script = scriptContainer.Script;

                changeStatus(EffectStatus.Configuring);
                Program.RunMainThread(() =>
                {
                    if (script.Configure(Config))
                        RaiseConfigFieldsChanged();
                });

                changeStatus(EffectStatus.Updating);
                script.Generate(context);
            }
            catch (RemotingException e)
            {
                Debug.Print($"Script execution failed with RemotingException, reloading {BaseName} ({e.Message})");
                changeStatus(EffectStatus.ReloadPending);
                Program.Schedule(() =>
                {
                    if (Project.IsDisposed) return;
                    scriptContainer.ReloadScript();
                });
                return;
            }
            catch (ScriptCompilationException e)
            {
                Debug.Print($"Script compilation failed for {BaseName}\n{e.Message}");
                changeStatus(EffectStatus.CompilationFailed, e.Message);
                return;
            }
            catch (ScriptLoadingException e)
            {
                Debug.Print($"Script load failed for {BaseName}\n{e.ToString()}");
                changeStatus(EffectStatus.LoadingFailed, e.InnerException != null ? $"{e.Message}: {e.InnerException.Message}" : e.Message);
                return;
            }
            catch (Exception e)
            {
                changeStatus(EffectStatus.ExecutionFailed, $"Unexpected error during {status}:\n{e.ToString()}");
                return;
            }
            finally
            {
                context.DisposeResources();
            }
            changeStatus(EffectStatus.Ready);

            Program.Schedule(() =>
            {
                if (Project.IsDisposed || layers == null)
                    return;

                if (placeHolderLayer != null)
                {
                    Project.LayerManager.Replace(placeHolderLayer, context.EditorLayers);
                    placeHolderLayer = null;
                }
                else Project.LayerManager.Replace(layers, context.EditorLayers);
                layers = context.EditorLayers;
                refreshLayerNames();
            });
        }

        public override void Clear()
        {
            if (layers == null)
                return;

            scriptContainer.OnScriptChanged -= scriptContainer_OnScriptChanged;

            foreach (var layer in layers)
                Project.LayerManager.Remove(layer);
            layers = null;
        }

        public override void Refresh()
            => Project.QueueEffectUpdate(this);

        private void scriptContainer_OnScriptChanged(object sender, EventArgs e)
            => Refresh();

        private void refreshLayerNames()
        {
            if (layers == null)
                return;

            foreach (var layer in layers)
                layer.Name = string.IsNullOrWhiteSpace(layer.Identifier) ? $"{name}" : $"{name} ({layer.Identifier})";
        }

        private void changeStatus(EffectStatus status, string message = null)
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
                            break;
                        default:
                            Debug.Print($"{BaseName}'s {this.status} status took {duration}ms");
                            break;
                    }

                this.status = status;
                statusMessage = message ?? string.Empty;
                RaiseChanged();

                statusStopwatch.Restart();
            });
        }
    }
}
