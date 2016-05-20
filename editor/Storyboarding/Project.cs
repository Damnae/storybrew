using OpenTK;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Util;
using StorybrewEditor.Graphics;
using StorybrewEditor.Graphics.Cameras;
using StorybrewEditor.Graphics.Textures;
using StorybrewEditor.Mapset;
using StorybrewEditor.Scripting;
using StorybrewEditor.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace StorybrewEditor.Storyboarding
{
    public class Project : IDisposable
    {
        public const string Extension = ".sbp";
        public const string DefaultFilename = "project" + Extension;
        public const string FileFilter = "project files (*" + Extension + ")|*" + Extension + "|All files (*.*)|*.*";

        private string projectFilename;
        private ScriptManager<StoryboardObjectGenerator> scriptManager;
        public string ScriptsPath => scriptManager.ScriptsPath;

        private TextureContainer textureContainer;
        public TextureContainer TextureContainer => textureContainer;

        public string AudioPath
        {
            get
            {
                checkMapsetPath();
                foreach (var beatmap in mapsetManager.Beatmaps)
                {
                    var path = Path.Combine(MapsetPath, beatmap.AudioFilename);
                    if (!File.Exists(path)) continue;
                    return path;
                }

                foreach (var mp3Path in Directory.GetFiles(MapsetPath, "*.mp3", SearchOption.TopDirectoryOnly))
                    return mp3Path;

                return null;
            }
        }

        public Project(string projectFilename)
        {
            this.projectFilename = projectFilename;

            reloadTextures();

            var scriptsSourcePath = Path.GetFullPath(Path.Combine("..", "..", "..", "scripts"));
            if (!Directory.Exists(scriptsSourcePath))
            {
                scriptsSourcePath = Path.GetFullPath("scripts");
                if (!Directory.Exists(scriptsSourcePath))
                    Directory.CreateDirectory(scriptsSourcePath);
            }
            Trace.WriteLine($"Scripts path: {scriptsSourcePath}");

            var compiledScriptsPath = Path.GetFullPath("cache/scripts");
            if (!Directory.Exists(compiledScriptsPath))
                Directory.CreateDirectory(compiledScriptsPath);
            else
            {
                cleanupFolder(compiledScriptsPath, "*.dll");
#if DEBUG
                cleanupFolder(compiledScriptsPath, "*.pdb");
#endif
            }
            var referencedAssemblies = new string[]
            {
                "System.dll",
                "OpenTK.dll",
                Assembly.GetAssembly(typeof(Script)).Location,
            };
            scriptManager = new ScriptManager<StoryboardObjectGenerator>("StorybrewScripts", scriptsSourcePath, compiledScriptsPath, referencedAssemblies);
            effectUpdateQueue.OnActionFailed += (effect, e) => Trace.WriteLine($"Action failed for '{effect}': {e.Message}");

            OnMainBeatmapChanged += (sender, e) =>
            {
                foreach (var effect in effects)
                    QueueEffectUpdate(effect);
            };
        }

        #region Display

        private static readonly OsbLayer[] osbLayers = new OsbLayer[] { OsbLayer.Background, OsbLayer.Fail, OsbLayer.Pass, OsbLayer.Foreground, };

        public double DisplayTime;

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity)
        {
            foreach (var osbLayer in osbLayers)
                foreach (var layer in layers)
                    layer.Draw(drawContext, camera, bounds, opacity, osbLayer);
        }

        private void reloadTextures()
        {
            textureContainer?.Dispose();
            textureContainer = new TextureContainerSeparate(false);
        }

        #endregion

        #region Effects

        private List<Effect> effects = new List<Effect>();
        public IEnumerable<Effect> Effects => effects;
        public event EventHandler OnEffectsChanged;

        public EffectStatus effectsStatus = EffectStatus.Initializing;
        public EffectStatus EffectsStatus => effectsStatus;
        public event EventHandler OnEffectsStatusChanged;

        private AsyncActionQueue<Effect> effectUpdateQueue = new AsyncActionQueue<Effect>("Effect Updates", false);
        public void QueueEffectUpdate(Effect effect)
            => effectUpdateQueue.Queue(effect, (e) => e.Update());

        public IEnumerable<string> GetEffectNames()
            => scriptManager.GetScriptNames();

        public Effect GetEffectByName(string name)
            => effects.Find(e => e.Name == name);

        public Effect AddEffect(string effectName)
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(Project));

            var effect = new ScriptedEffect(this, scriptManager.Get(effectName));

            effects.Add(effect);
            effect.OnChanged += Effect_OnChanged;
            refreshEffectsStatus();

            OnEffectsChanged?.Invoke(this, EventArgs.Empty);
            QueueEffectUpdate(effect);
            return effect;
        }

        public void Remove(Effect effect)
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(Project));

            effect.Clear();
            effects.Remove(effect);
            effect.OnChanged -= Effect_OnChanged;
            refreshEffectsStatus();

            OnEffectsChanged?.Invoke(this, EventArgs.Empty);
        }

        public string GetUniqueEffectName()
        {
            var count = 1;
            string name;
            do
                name = $"Effect {count++}";
            while (GetEffectByName(name) != null);
            return name;
        }

        private void Effect_OnChanged(object sender, EventArgs e)
            => refreshEffectsStatus();

        private void refreshEffectsStatus()
        {
            var previousStatus = effectsStatus;
            var isUpdating = false;
            var hasError = false;

            foreach (var effect in effects)
            {
                switch (effect.Status)
                {
                    case EffectStatus.Loading:
                    case EffectStatus.Configuring:
                    case EffectStatus.Updating:
                    case EffectStatus.ReloadPending:
                        isUpdating = true;
                        break;

                    case EffectStatus.CompilationFailed:
                    case EffectStatus.LoadingFailed:
                    case EffectStatus.ExecutionFailed:
                        hasError = true;
                        break;

                    case EffectStatus.Initializing:
                    case EffectStatus.Ready:
                        break;
                }
            }
            effectsStatus = hasError ? EffectStatus.ExecutionFailed :
                isUpdating ? EffectStatus.Updating : EffectStatus.Ready;
            if (effectsStatus != previousStatus)
                OnEffectsStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Layers

        private List<EditorStoryboardLayer> layers = new List<EditorStoryboardLayer>();
        public IEnumerable<EditorStoryboardLayer> Layers => layers;
        public int LayersCount => layers.Count;

        public event EventHandler OnLayersChanged;

        public void Add(EditorStoryboardLayer layer)
        {
            layers.Add(layer);
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Replace(EditorStoryboardLayer oldLayer, EditorStoryboardLayer newLayer)
        {
            var index = layers.IndexOf(oldLayer);
            if (index != -1)
            {
                newLayer.CopySettings(layers[index]);
                layers[index] = newLayer;
            }
            else throw new InvalidOperationException($"Cannot replace layer '{oldLayer.Name}' with '{newLayer.Name}', old layer not found");
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Replace(List<EditorStoryboardLayer> oldLayers, List<EditorStoryboardLayer> newLayers)
        {
            oldLayers = new List<EditorStoryboardLayer>(oldLayers);
            foreach (var newLayer in newLayers)
            {
                var oldLayer = oldLayers.Find(l => l.Identifier == newLayer.Identifier);
                if (oldLayer != null)
                {
                    var index = layers.IndexOf(oldLayer);
                    if (index != -1)
                    {
                        newLayer.CopySettings(layers[index]);
                        layers[index] = newLayer;
                    }
                    oldLayers.Remove(oldLayer);
                }
                else layers.Add(newLayer);
            }
            foreach (var oldLayer in oldLayers)
                layers.Remove(oldLayer);
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Replace(EditorStoryboardLayer oldLayer, List<EditorStoryboardLayer> newLayers)
        {
            var index = layers.IndexOf(oldLayer);
            if (index != -1)
            {
                foreach (var newLayer in newLayers)
                    newLayer.CopySettings(oldLayer);
                layers.InsertRange(index, newLayers);
                layers.Remove(oldLayer);
            }
            else throw new InvalidOperationException($"Cannot replace layer '{oldLayer.Name}' with multiple layers, old layer not found");
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Remove(EditorStoryboardLayer layer)
        {
            if (layers.Remove(layer))
                OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void MoveUp(EditorStoryboardLayer layer)
        {
            var index = layers.IndexOf(layer);
            if (index != -1)
            {
                var otherLayer = layers[index - 1];
                layers[index - 1] = layer;
                layers[index] = otherLayer;
            }
            else throw new InvalidOperationException($"Cannot move layer '{layer.Name}', not found");
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void MoveDown(EditorStoryboardLayer layer)
        {
            var index = layers.IndexOf(layer);
            if (index != -1)
            {
                var otherLayer = layers[index + 1];
                layers[index + 1] = layer;
                layers[index] = otherLayer;
            }
            else throw new InvalidOperationException($"Cannot move layer '{layer.Name}', not found");
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void MoveTop(EditorStoryboardLayer layer)
        {
            if (layers.Remove(layer))
                layers.Insert(0, layer);
            else throw new InvalidOperationException($"Cannot move layer '{layer.Name}', not found");
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void MoveBottom(EditorStoryboardLayer layer)
        {
            if (layers.Remove(layer))
                layers.Add(layer);
            else throw new InvalidOperationException($"Cannot move layer '{layer.Name}', not found");
            OnLayersChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Mapset

        private string mapsetPath;
        public string MapsetPath
        {
            get { return mapsetPath; }
            set
            {
                if (mapsetPath == value) return;
                mapsetPath = value;
                refreshMapset();
            }
        }

        private MapsetManager mapsetManager;
        public MapsetManager MapsetManager => mapsetManager;

        private EditorBeatmap mainBeatmap;
        public EditorBeatmap MainBeatmap
        {
            get
            {
                if (mainBeatmap == null)
                    SwitchMainBeatmap();

                return mainBeatmap;
            }
            set
            {
                if (mainBeatmap == value) return;
                mainBeatmap = value;
                OnMainBeatmapChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler OnMainBeatmapChanged;

        public void SwitchMainBeatmap()
        {
            var takeNextBeatmap = false;
            foreach (var beatmap in mapsetManager.Beatmaps)
            {
                if (takeNextBeatmap)
                {
                    MainBeatmap = beatmap;
                    return;
                }
                else if (beatmap == mainBeatmap)
                    takeNextBeatmap = true;
            }
            foreach (var beatmap in mapsetManager.Beatmaps)
            {
                MainBeatmap = beatmap;
                return;
            }
        }

        private void refreshMapset()
        {
            mainBeatmap = null;
            mapsetManager?.Dispose();
            mapsetManager = new MapsetManager(mapsetPath);
            mapsetManager.OnFileChanged += mapsetManager_OnFileChanged;
        }

        private void mapsetManager_OnFileChanged(object sender, FileSystemEventArgs e)
        {
            var extension = Path.GetExtension(e.Name);
            if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
                reloadTextures();
        }

        #endregion

        #region Save / Load / Export

        public const int Version = 1;

        public void Save()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(Project));

            using (var stream = new SafeWriteStream(projectFilename))
            using (var w = new BinaryWriter(stream, Encoding.UTF8))
            {
                w.Write(Version);
                w.Write(Program.FullName);

                w.Write(MapsetPath);
                w.Write(MainBeatmap.Id);
                w.Write(MainBeatmap.Name);

                w.Write(effects.Count);
                foreach (var effect in effects)
                {
                    w.Write(effect.BaseName);
                    w.Write(effect.Name);

                    var config = effect.Config;
                    w.Write(config.FieldCount);
                    foreach (var field in config.Fields)
                    {
                        w.Write(field.Name);
                        w.Write(field.DisplayName);
                        ObjectSerializer.Write(w, field.Value);

                        w.Write(field.AllowedValues?.Length ?? 0);
                        if (field.AllowedValues != null)
                            foreach (var allowedValue in field.AllowedValues)
                            {
                                w.Write(allowedValue.Name);
                                ObjectSerializer.Write(w, allowedValue.Value);
                            }
                    }
                }

                w.Write(layers.Count);
                foreach (var layer in layers)
                {
                    w.Write(layer.Identifier);
                    w.Write(effects.IndexOf(layer.Effect));
                    w.Write(layer.Visible);
                }
                stream.Commit();
            }
        }

        public static Project Load(string projectFilename)
        {
            var project = new Project(projectFilename);
            using (var stream = new FileStream(projectFilename, FileMode.Open))
            using (var r = new BinaryReader(stream, Encoding.UTF8))
            {
                var version = r.ReadInt32();
                if (version > Version)
                    throw new InvalidOperationException("This project was saved with a more recent version, you need to update to open it");

                var savedBy = r.ReadString();
                Debug.Print($"Loading project saved by {savedBy}");

                project.MapsetPath = r.ReadString();
                if (version >= 1)
                {
                    var mainBeatmapId = r.ReadInt64();
                    var mainBeatmapName = r.ReadString();

                    foreach (var beatmap in project.MapsetManager.Beatmaps)
                        if ((mainBeatmapId > 0 && beatmap.Id == mainBeatmapId) ||
                            (mainBeatmapName.Length > 0 && beatmap.Name == mainBeatmapName))
                        {
                            project.MainBeatmap = beatmap;
                            break;
                        }
                }

                var effectCount = r.ReadInt32();
                for (int effectIndex = 0; effectIndex < effectCount; effectIndex++)
                {
                    var baseName = r.ReadString();
                    var name = r.ReadString();

                    var effect = project.AddEffect(baseName);
                    effect.Name = name;

                    if (version >= 1)
                    {
                        var fieldCount = r.ReadInt32();
                        for (int fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
                        {
                            var fieldName = r.ReadString();
                            var fieldDisplayName = r.ReadString();
                            var fieldValue = ObjectSerializer.Read(r);

                            var allowedValueCount = r.ReadInt32();
                            var allowedValues = allowedValueCount > 0 ? new NamedValue[allowedValueCount] : null;
                            for (int allowedValueIndex = 0; allowedValueIndex < allowedValueCount; allowedValueIndex++)
                            {
                                var allowedValueName = r.ReadString();
                                var allowedValue = ObjectSerializer.Read(r);
                                allowedValues[allowedValueIndex] = new NamedValue()
                                {
                                    Name = allowedValueName,
                                    Value = allowedValue,
                                };
                            }
                            effect.Config.UpdateField(fieldName, fieldDisplayName, fieldValue.GetType(), fieldValue, allowedValues);
                        }
                    }
                }

                var layerCount = r.ReadInt32();
                for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
                {
                    var identifier = r.ReadString();
                    var effectIndex = r.ReadInt32();
                    var visible = r.ReadBoolean();

                    var effect = project.effects[effectIndex];
                    effect.AddPlaceholder(new EditorStoryboardLayer(identifier, effect)
                    {
                        Visible = visible,
                    });
                }
            }
            return project;
        }

        /// <summary>
        /// Doesn't run in the main thread
        /// </summary>
        public void ExportToOsb()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(Project));

            string osbPath = null;
            List<EditorStoryboardLayer> localLayers = null;
            Program.RunMainThread(() =>
            {
                osbPath = getOsbPath();
                localLayers = new List<EditorStoryboardLayer>(layers);
            });

            Debug.Print($"Exporting osb to {osbPath}");
            var exportSettings = new ExportSettings();

            using (var stream = new SafeWriteStream(osbPath))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.WriteLine("[Events]");
                writer.WriteLine("//Background and Video events");
                foreach (var osbLayer in osbLayers)
                {
                    writer.WriteLine($"//Storyboard Layer {(int)osbLayer} ({osbLayer})");
                    foreach (var layer in localLayers)
                        layer.WriteOsbSprites(writer, exportSettings, osbLayer);
                }
                writer.WriteLine("//Storyboard Sound Samples");
                stream.Commit();
            }
        }

        private string getOsbPath()
        {
            checkMapsetPath();

            // Find the correct osb filename from .osu files
            var regex = new Regex(@"^(.+ - .+ \(.+\)) \[.+\].osu$");
            foreach (var osuFilePath in Directory.GetFiles(MapsetPath, "*.osu", SearchOption.TopDirectoryOnly))
            {
                var osuFilename = Path.GetFileName(osuFilePath);

                Match match;
                if ((match = regex.Match(osuFilename)).Success)
                    return Path.Combine(MapsetPath, $"{match.Groups[1].Value}.osb");
            }

            // Use an existing osb
            foreach (var osbFilePath in Directory.GetFiles(MapsetPath, "*.osb", SearchOption.TopDirectoryOnly))
                return osbFilePath;

            // Whatever
            return Path.Combine(MapsetPath, "storyboard.osb");
        }

        private void checkMapsetPath()
        {
            if (!Directory.Exists(MapsetPath)) throw new InvalidOperationException($"Mapset directory doesn't exist.\n{MapsetPath}");
        }

        private static void cleanupFolder(string path, string searchPattern)
        {
            foreach (var filename in Directory.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly))
                try
                {
                    File.Delete(filename);
                    Debug.Print($"{filename} deleted");
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"{filename} couldn't be deleted: {e.Message}");
                }
        }

        #endregion

        #region IDisposable Support

        public bool IsDisposed => disposedValue;
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    mapsetManager?.Dispose();
                    effectUpdateQueue.Dispose();
                    scriptManager.Dispose();
                    textureContainer.Dispose();
                }
                mapsetManager = null;
                effectUpdateQueue = null;
                scriptManager = null;
                textureContainer = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
