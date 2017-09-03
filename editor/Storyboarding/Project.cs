using BrewLib.Audio;
using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using BrewLib.Graphics.Textures;
using BrewLib.Util;
using OpenTK;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Util;
using StorybrewEditor.Mapset;
using StorybrewEditor.Scripting;
using StorybrewEditor.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace StorybrewEditor.Storyboarding
{
    public class Project : IDisposable
    {
        public const string Extension = ".sbp";
        public const string DefaultFilename = "project" + Extension;
        public const string ProjectsFolder = "projects";

        public const string FileFilter = "project files (*" + Extension + ")|*" + Extension;

        private ScriptManager<StoryboardObjectGenerator> scriptManager;

        private string projectPath;
        public string ProjectFolderPath => Path.GetDirectoryName(projectPath);

        private string scriptsSourcePath;
        public string ScriptsPath => scriptsSourcePath;

        private string commonScriptsSourcePath;
        public string CommonScriptsPath => commonScriptsSourcePath;

        private string scriptsLibraryPath;
        public string ScriptsLibraryPath => scriptsLibraryPath;

        public string AudioPath
        {
            get
            {
                if (!Directory.Exists(MapsetPath))
                    return null;

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

        public string OsbPath
        {
            get
            {
                if (!Directory.Exists(MapsetPath))
                    return Path.Combine(ProjectFolderPath, "storyboard.osb");

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
        }

        private LayerManager layerManager = new LayerManager();
        public LayerManager LayerManager => layerManager;

        public Project(string projectPath, bool withCommonScripts)
        {
            this.projectPath = projectPath;

            reloadTextures();
            reloadAudio();

            scriptsSourcePath = Path.GetDirectoryName(projectPath);
            if (withCommonScripts)
            {
                commonScriptsSourcePath = Path.GetFullPath(Path.Combine("..", "..", "..", "scripts"));
                if (!Directory.Exists(commonScriptsSourcePath))
                {
                    commonScriptsSourcePath = Path.GetFullPath("scripts");
                    if (!Directory.Exists(commonScriptsSourcePath))
                        Directory.CreateDirectory(commonScriptsSourcePath);
                }
            }
            scriptsLibraryPath = Path.Combine(scriptsSourcePath, "scriptslibrary");
            if (!Directory.Exists(scriptsLibraryPath))
                Directory.CreateDirectory(scriptsLibraryPath);

            Trace.WriteLine($"Scripts path - project:{scriptsSourcePath}, common:{commonScriptsSourcePath}, library:{scriptsLibraryPath}");

            var compiledScriptsPath = Path.GetFullPath("cache/scripts");
            if (!Directory.Exists(compiledScriptsPath))
                Directory.CreateDirectory(compiledScriptsPath);
            else
            {
                cleanupFolder(compiledScriptsPath, "*.dll");
                cleanupFolder(compiledScriptsPath, "*.pdb");
            }

            scriptManager = new ScriptManager<StoryboardObjectGenerator>("StorybrewScripts", scriptsSourcePath, commonScriptsSourcePath, scriptsLibraryPath, compiledScriptsPath, ReferencedAssemblies);
            effectUpdateQueue.OnActionFailed += (effect, e) => Trace.WriteLine($"Action failed for '{effect}': {e.Message}");

            layerManager.OnLayersChanged +=
                (sender, e) => changed = true;

            OnMainBeatmapChanged += (sender, e) =>
            {
                foreach (var effect in effects)
                    if (effect.BeatmapDependant)
                        QueueEffectUpdate(effect);
            };
        }

        #region Audio and Display

        public static readonly OsbLayer[] OsbLayers = new OsbLayer[] { OsbLayer.Background, OsbLayer.Fail, OsbLayer.Pass, OsbLayer.Foreground, };

        public double DisplayTime;

        private TextureContainer textureContainer;
        public TextureContainer TextureContainer => textureContainer;

        private AudioContainer audioContainer;
        public AudioContainer AudioContainer => audioContainer;

        public void TriggerEvents(double startTime, double endTime)
        {
            layerManager.TriggerEvents(startTime, endTime);
        }

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity)
        {
            effectUpdateQueue.Enabled = true;
            layerManager.Draw(drawContext, camera, bounds, opacity);
        }

        private void reloadTextures()
        {
            textureContainer?.Dispose();
            textureContainer = new TextureContainerSeparate(null, TextureOptions.Default);
        }

        private void reloadAudio()
        {
            audioContainer?.Dispose();
            audioContainer = new AudioContainer(Program.AudioManager, null);
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
            => effectUpdateQueue.Queue(effect, effect.Path, (e) => e.Update());

        public IEnumerable<string> GetEffectNames()
            => scriptManager.GetScriptNames();

        public Effect GetEffectByName(string name)
            => effects.Find(e => e.Name == name);

        public Effect AddEffect(string effectName)
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(Project));

            var effect = new ScriptedEffect(this, scriptManager.Get(effectName));

            effects.Add(effect);
            changed = true;

            effect.OnChanged += effect_OnChanged;
            refreshEffectsStatus();

            OnEffectsChanged?.Invoke(this, EventArgs.Empty);
            QueueEffectUpdate(effect);
            return effect;
        }

        public void Remove(Effect effect)
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(Project));

            effects.Remove(effect);
            effect.Dispose();
            changed = true;

            refreshEffectsStatus();

            OnEffectsChanged?.Invoke(this, EventArgs.Empty);
        }

        public string GetUniqueEffectName(string baseName)
        {
            var count = 1;
            string name;
            do
                name = $"{baseName} {count++}";
            while (GetEffectByName(name) != null);
            return name;
        }

        private void effect_OnChanged(object sender, EventArgs e)
        {
            refreshEffectsStatus();
            changed = true;
        }

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

        #region Mapset

        private string mapsetPath;
        public string MapsetPath
        {
            get { return mapsetPath; }
            set
            {
                if (mapsetPath == value) return;
                mapsetPath = value;
                changed = true;

                OnMapsetPathChanged?.Invoke(this, EventArgs.Empty);
                refreshMapset();
            }
        }

        public event EventHandler OnMapsetPathChanged;

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
                changed = true;

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
            MainBeatmap = new EditorBeatmap(null);
        }

        public void SelectBeatmap(long id, string name)
        {
            foreach (var beatmap in MapsetManager.Beatmaps)
                if ((id > 0 && beatmap.Id == id) || (name.Length > 0 && beatmap.Name == name))
                {
                    MainBeatmap = beatmap;
                    break;
                }
        }

        private void refreshMapset()
        {
            var previousBeatmapId = mainBeatmap?.Id ?? -1;
            var previousBeatmapName = mainBeatmap?.Name;

            mainBeatmap = null;
            mapsetManager?.Dispose();

            mapsetManager = new MapsetManager(mapsetPath, mapsetManager != null);
            mapsetManager.OnFileChanged += mapsetManager_OnFileChanged;

            if (previousBeatmapName != null)
                SelectBeatmap(previousBeatmapId, previousBeatmapName);
        }

        private void mapsetManager_OnFileChanged(object sender, FileSystemEventArgs e)
        {
            var extension = Path.GetExtension(e.Name);
            if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
                reloadTextures();
            else if (extension == ".wav" || extension == ".mp3" || extension == ".ogg")
                reloadAudio();
            else if (extension == ".osu")
                refreshMapset();
        }

        #endregion

        #region Assemblies

        private static List<string> defaultAssemblies = new List<string>()
        {
            "System.dll",
            "System.Core.dll",
            "System.Drawing.dll",
            "OpenTK.dll",
            Assembly.GetAssembly(typeof(Script)).Location,
        };
        public static IEnumerable<string> DefaultAssemblies => defaultAssemblies;

        private List<string> importedAssemblies = new List<string>();
        public IEnumerable<string> ImportedAssemblies
        {
            get { return importedAssemblies; }
            set
            {
                if (disposedValue) throw new ObjectDisposedException(nameof(Project));

                importedAssemblies = new List<string>(value);
                scriptManager.ReferencedAssemblies = ReferencedAssemblies;
            }
        }

        public IEnumerable<string> ReferencedAssemblies
            => DefaultAssemblies.Concat(importedAssemblies);

        #endregion

        #region Save / Load / Export

        public const int Version = 5;

        private bool changed;
        public bool Changed => changed;

        private bool ownsOsb;
        public bool OwnsOsb
        {
            get { return ownsOsb; }
            set
            {
                if (ownsOsb == value) return;
                ownsOsb = value;
                changed = true;
            }
        }

        public void Save()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(Project));

            using (var stream = new SafeWriteStream(projectPath))
            using (var w = new BinaryWriter(stream, Encoding.UTF8))
            {
                w.Write(Version);
                w.Write(Program.FullName);

                w.Write(MapsetPath);
                w.Write(MainBeatmap.Id);
                w.Write(MainBeatmap.Name);

                w.Write(OwnsOsb);

                w.Write(effects.Count);
                foreach (var effect in effects)
                {
                    w.Write(effect.BaseName);
                    w.Write(effect.Name);

                    var config = effect.Config;
                    w.Write(config.FieldCount);
                    foreach (var field in config.SortedFields)
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

                w.Write(layerManager.LayersCount);
                foreach (var layer in layerManager.Layers)
                {
                    w.Write(layer.Identifier);
                    w.Write(effects.IndexOf(layer.Effect));
                    w.Write(layer.DiffSpecific);
                    w.Write((int)layer.OsbLayer);
                    w.Write(layer.Visible);
                }

                w.Write(importedAssemblies.Count);
                foreach (var assembly in importedAssemblies)
                    w.Write(assembly);

                stream.Commit();
                changed = false;
            }
        }

        public static Project Load(string projectPath, bool withCommonScripts)
        {
            var project = new Project(projectPath, withCommonScripts);
            using (var stream = new FileStream(projectPath, FileMode.Open))
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
                    project.SelectBeatmap(mainBeatmapId, mainBeatmapName);
                }

                project.OwnsOsb = version >= 4 ? r.ReadBoolean() : true;

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
                            effect.Config.UpdateField(fieldName, fieldDisplayName, fieldIndex, fieldValue?.GetType(), fieldValue, allowedValues);
                        }
                    }
                }

                var layerCount = r.ReadInt32();
                for (var layerIndex = 0; layerIndex < layerCount; layerIndex++)
                {
                    var identifier = r.ReadString();
                    var effectIndex = r.ReadInt32();
                    var diffSpecific = version >= 3 ? r.ReadBoolean() : false;
                    var osbLayer = version >= 2 ? (OsbLayer)r.ReadInt32() : OsbLayer.Background;
                    var visible = r.ReadBoolean();

                    var effect = project.effects[effectIndex];
                    effect.AddPlaceholder(new EditorStoryboardLayer(identifier, effect)
                    {
                        DiffSpecific = diffSpecific,
                        OsbLayer = osbLayer,
                        Visible = visible,
                    });
                }

                if (version >= 5)
                {
                    var assemblyCount = r.ReadInt32();
                    var importedAssemblies = new List<string>();
                    for (var assemblyIndex = 0; assemblyIndex < assemblyCount; assemblyIndex++)
                    {
                        var assembly = r.ReadString();
                        importedAssemblies.Add(assembly);
                    }
                    project.ImportedAssemblies = importedAssemblies;
                }
            }
            return project;
        }

        public static Project Create(string projectFolderName, string mapsetPath, bool withCommonScripts)
        {
            if (!Directory.Exists(ProjectsFolder))
                Directory.CreateDirectory(ProjectsFolder);

            var hasInvalidCharacters = false;
            foreach (var character in Path.GetInvalidFileNameChars())
                if (projectFolderName.Contains(character.ToString()))
                {
                    hasInvalidCharacters = true;
                    break;
                }

            if (hasInvalidCharacters || string.IsNullOrWhiteSpace(projectFolderName))
                throw new InvalidOperationException($"'{projectFolderName}' isn't a valid project folder name");

            var projectFolderPath = Path.Combine(ProjectsFolder, projectFolderName);
            if (Directory.Exists(projectFolderPath))
                throw new InvalidOperationException($"A project already exists at '{projectFolderPath}'");

            Directory.CreateDirectory(projectFolderPath);
            var project = new Project(Path.Combine(projectFolderPath, DefaultFilename), withCommonScripts)
            {
                MapsetPath = mapsetPath,
            };
            project.Save();

            return project;
        }

        /// <summary>
        /// Doesn't run in the main thread
        /// </summary>
        public void ExportToOsb()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(Project));

            string osuPath = null, osbPath = null;
            List<EditorStoryboardLayer> localLayers = null;
            Program.RunMainThread(() =>
            {
                osuPath = MainBeatmap.Path;
                osbPath = OsbPath;

                if (!OwnsOsb && File.Exists(osbPath))
                    File.Copy(osbPath, $"{osbPath}.bak");
                OwnsOsb = true;

                localLayers = new List<EditorStoryboardLayer>(layerManager.FindLayers(l => l.Visible));
            });

            var exportSettings = new ExportSettings();

            if (!string.IsNullOrEmpty(osuPath))
            {

                Debug.Print($"Exporting diff specific events to {osuPath}");
                using (var stream = new SafeWriteStream(osuPath))
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                using (var fileStream = new FileStream(osuPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    string line;
                    var inEvents = false;
                    var inStoryboard = false;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var trimmedLine = line.Trim();
                        if (!inEvents && trimmedLine == "[Events]")
                            inEvents = true;
                        else if (trimmedLine.Length == 0)
                            inEvents = false;

                        if (inEvents)
                        {
                            if (trimmedLine.StartsWith("//Storyboard Layer"))
                            {
                                if (!inStoryboard)
                                {
                                    foreach (var osbLayer in OsbLayers)
                                    {
                                        writer.WriteLine($"//Storyboard Layer {(int)osbLayer} ({osbLayer})");
                                        foreach (var layer in localLayers)
                                            if (layer.OsbLayer == osbLayer && layer.DiffSpecific)
                                                layer.WriteOsbSprites(writer, exportSettings);
                                    }
                                    inStoryboard = true;
                                }
                            }
                            else if (inStoryboard && trimmedLine.StartsWith("//"))
                                inStoryboard = false;

                            if (inStoryboard)
                                continue;
                        }
                        writer.WriteLine(line);
                    }
                    stream.Commit();
                }
            }

            Debug.Print($"Exporting osb to {osbPath}");
            using (var stream = new SafeWriteStream(osbPath))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.WriteLine("[Events]");
                writer.WriteLine("//Background and Video events");
                foreach (var osbLayer in OsbLayers)
                {
                    writer.WriteLine($"//Storyboard Layer {(int)osbLayer} ({osbLayer})");
                    foreach (var layer in localLayers)
                        if (layer.OsbLayer == osbLayer && !layer.DiffSpecific)
                            layer.WriteOsbSprites(writer, exportSettings);
                }
                writer.WriteLine("//Storyboard Sound Samples");
                stream.Commit();
            }
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
                    // Always dispose this first to ensure updates aren't happening while the project is being disposed
                    effectUpdateQueue.Dispose();

                    mapsetManager?.Dispose();
                    scriptManager.Dispose();
                    textureContainer.Dispose();
                    audioContainer.Dispose();
                }
                mapsetManager = null;
                effectUpdateQueue = null;
                scriptManager = null;
                textureContainer = null;
                audioContainer = null;
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
