using BrewLib.Audio;
using BrewLib.Data;
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
using Tiny;

namespace StorybrewEditor.Storyboarding
{
    public class Project : IDisposable
    {
        public static readonly Encoding Encoding = new UTF8Encoding();

        public const string BinaryExtension = ".sbp";
        public const string TextExtension = ".yaml";
        public const string DefaultBinaryFilename = "project" + BinaryExtension;
        public const string DefaultTextFilename = "project.sbrew" + TextExtension;
        public const string DataFolder = ".sbrew";
        public const string ProjectsFolder = "projects";

        public const string FileFilter = "project files|" + DefaultBinaryFilename + ";" + DefaultTextFilename;

        private ScriptManager<StoryboardObjectGenerator> scriptManager;

        private readonly string projectPath;
        public string ProjectFolderPath => Path.GetDirectoryName(projectPath);
        public string ProjectAssetFolderPath => Path.Combine(ProjectFolderPath, "assetlibrary");

        public string ScriptsPath { get; }
        public string CommonScriptsPath { get; }
        public string ScriptsLibraryPath { get; }

        public string AudioPath
        {
            get
            {
                if (!Directory.Exists(MapsetPath))
                    return null;

                foreach (var beatmap in MapsetManager.Beatmaps)
                {
                    if (beatmap.AudioFilename == null)
                        continue;

                    var path = Path.Combine(MapsetPath, beatmap.AudioFilename);
                    if (!File.Exists(path))
                        continue;

                    return path;
                }

                return Directory.GetFiles(MapsetPath, "*.mp3", SearchOption.TopDirectoryOnly).FirstOrDefault();
            }
        }

        public string OsbPath
        {
            get
            {
                if (!MapsetPathIsValid)
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

        public readonly ExportSettings ExportSettings = new ExportSettings();

        public LayerManager LayerManager { get; } = new LayerManager();

        public Project(string projectPath, bool withCommonScripts, ResourceContainer resourceContainer)
        {
            this.projectPath = projectPath;

            reloadTextures();
            reloadAudio();

            ScriptsPath = Path.GetDirectoryName(projectPath);
            if (withCommonScripts)
            {
                CommonScriptsPath = Path.GetFullPath(Path.Combine("..", "..", "..", "scripts"));
                if (!Directory.Exists(CommonScriptsPath))
                {
                    CommonScriptsPath = Path.GetFullPath("scripts");
                    if (!Directory.Exists(CommonScriptsPath))
                        Directory.CreateDirectory(CommonScriptsPath);
                }
            }
            ScriptsLibraryPath = Path.Combine(ScriptsPath, "scriptslibrary");
            if (!Directory.Exists(ScriptsLibraryPath))
                Directory.CreateDirectory(ScriptsLibraryPath);

            Trace.WriteLine($"Scripts path - project:{ScriptsPath}, common:{CommonScriptsPath}, library:{ScriptsLibraryPath}");

            var compiledScriptsPath = Path.GetFullPath("cache/scripts");
            if (!Directory.Exists(compiledScriptsPath))
                Directory.CreateDirectory(compiledScriptsPath);
            else
            {
                cleanupFolder(compiledScriptsPath, "*.dll");
                cleanupFolder(compiledScriptsPath, "*.pdb");
            }

            initializeAssetWatcher();

            scriptManager = new ScriptManager<StoryboardObjectGenerator>(resourceContainer, "StorybrewScripts", ScriptsPath, CommonScriptsPath, ScriptsLibraryPath, compiledScriptsPath, ReferencedAssemblies);
            effectUpdateQueue.OnActionFailed += (effect, e) => Trace.WriteLine($"Action failed for '{effect}': {e.Message}");

            LayerManager.OnLayersChanged +=
                (sender, e) => Changed = true;

            OnMainBeatmapChanged += (sender, e) =>
            {
                foreach (var effect in effects)
                    if (effect.BeatmapDependant)
                        QueueEffectUpdate(effect);
            };
        }

        #region Audio and Display

        public static readonly OsbLayer[] OsbLayers = (OsbLayer[])Enum.GetValues(typeof(OsbLayer));

        public double DisplayTime;
        public float DimFactor;

        public TextureContainer TextureContainer { get; private set; }
        public AudioSampleContainer AudioContainer { get; private set; }

        public FrameStats FrameStats { get; private set; } = new FrameStats();

        public void TriggerEvents(double startTime, double endTime)
        {
            LayerManager.TriggerEvents(startTime, endTime);
        }

        public void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, bool updateFrameStats)
        {
            effectUpdateQueue.Enabled = allowEffectUpdates && MapsetPathIsValid;

            var newFrameStats = updateFrameStats ? new FrameStats() : null;
            LayerManager.Draw(drawContext, camera, bounds, opacity, newFrameStats);
            FrameStats = newFrameStats ?? FrameStats;
        }

        private void reloadTextures()
        {
            TextureContainer?.Dispose();
            TextureContainer = new TextureContainerSeparate(null, TextureOptions.Default);
        }

        private void reloadAudio()
        {
            AudioContainer?.Dispose();
            AudioContainer = new AudioSampleContainer(Program.AudioManager, null);
        }

        #endregion

        #region Effects

        private readonly List<Effect> effects = new List<Effect>();
        public IEnumerable<Effect> Effects => effects;
        public event EventHandler OnEffectsChanged;

        public EffectStatus EffectsStatus { get; private set; } = EffectStatus.Initializing;
        public event EventHandler OnEffectsStatusChanged;

        public double StartTime => effects.Count > 0 ? effects.Min(e => e.StartTime) : 0;
        public double EndTime => effects.Count > 0 ? effects.Max(e => e.EndTime) : 0;
        public event EventHandler OnEffectsContentChanged;

        private bool allowEffectUpdates = true;

        private AsyncActionQueue<Effect> effectUpdateQueue = new AsyncActionQueue<Effect>("Effect Updates", false, Program.Settings.EffectThreads);
        public void QueueEffectUpdate(Effect effect)
        {
            effectUpdateQueue.Queue(effect, effect.Path, (e) => e.Update(), effect.Multithreaded);
            refreshEffectsStatus();
        }
        public void CancelEffectUpdates(bool stopThreads) => effectUpdateQueue.CancelQueuedActions(stopThreads);
        public void StopEffectUpdates()
        {
            allowEffectUpdates = false;
            effectUpdateQueue.Enabled = false;
        }

        public IEnumerable<string> GetEffectNames()
            => scriptManager.GetScriptNames();

        public Effect GetEffectByName(string name)
            => effects.Find(e => e.Name == name);

        public Effect AddScriptedEffect(string scriptName, bool multithreaded = false)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(Project));

            var effect = new ScriptedEffect(this, scriptManager.Get(scriptName), multithreaded)
            {
                Name = GetUniqueEffectName(scriptName),
            };

            effects.Add(effect);
            Changed = true;

            effect.OnChanged += effect_OnChanged;
            refreshEffectsStatus();

            OnEffectsChanged?.Invoke(this, EventArgs.Empty);
            QueueEffectUpdate(effect);
            return effect;
        }

        public void Remove(Effect effect)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(Project));

            effects.Remove(effect);
            effect.Dispose();
            Changed = true;

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
            Changed = true;

            refreshEffectsStatus();
            OnEffectsContentChanged?.Invoke(this, EventArgs.Empty);
        }

        private void refreshEffectsStatus()
        {
            var previousStatus = EffectsStatus;
            var pendingTasks = effectUpdateQueue.TaskCount;
            var isUpdating = pendingTasks > 0;
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
            EffectsStatus = hasError ? EffectStatus.ExecutionFailed :
                isUpdating ? EffectStatus.Updating : EffectStatus.Ready;
            if (EffectsStatus != previousStatus)
                OnEffectsStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Mapset

        public bool MapsetPathIsValid { get; private set; }

        private string mapsetPath;
        public string MapsetPath
        {
            get { return mapsetPath; }
            set
            {
                if (mapsetPath == value) return;
                mapsetPath = value;
                MapsetPathIsValid = Directory.Exists(mapsetPath);
                Changed = true;

                OnMapsetPathChanged?.Invoke(this, EventArgs.Empty);
                refreshMapset();
            }
        }

        public event EventHandler OnMapsetPathChanged;

        public MapsetManager MapsetManager { get; private set; }

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
                Changed = true;

                OnMainBeatmapChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler OnMainBeatmapChanged;

        public void SwitchMainBeatmap()
        {
            var takeNextBeatmap = false;
            foreach (var beatmap in MapsetManager.Beatmaps)
            {
                if (takeNextBeatmap)
                {
                    MainBeatmap = beatmap;
                    return;
                }
                else if (beatmap == mainBeatmap)
                    takeNextBeatmap = true;
            }
            foreach (var beatmap in MapsetManager.Beatmaps)
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
            MapsetManager?.Dispose();

            MapsetManager = new MapsetManager(mapsetPath, MapsetManager != null);
            MapsetManager.OnFileChanged += mapsetManager_OnFileChanged;

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

        #region Asset library folder

        private FileSystemWatcher assetWatcher;

        private void initializeAssetWatcher()
        {
            var assetsFolderPath = Path.GetFullPath(ProjectAssetFolderPath);
            if (!Directory.Exists(assetsFolderPath))
                Directory.CreateDirectory(assetsFolderPath);

            assetWatcher = new FileSystemWatcher()
            {
                Path = assetsFolderPath,
                IncludeSubdirectories = true,
            };
            assetWatcher.Created += assetWatcher_OnFileChanged;
            assetWatcher.Changed += assetWatcher_OnFileChanged;
            assetWatcher.Renamed += assetWatcher_OnFileChanged;
            assetWatcher.Error += (sender, e) => Trace.WriteLine($"Watcher error (assets): {e.GetException()}");
            assetWatcher.EnableRaisingEvents = true;
            Trace.WriteLine($"Watching (assets): {assetsFolderPath}");
        }

        private void assetWatcher_OnFileChanged(object sender, FileSystemEventArgs e)
            => Program.Schedule(() =>
            {
                if (IsDisposed) return;

                var extension = Path.GetExtension(e.Name);
                if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
                    reloadTextures();
                else if (extension == ".wav" || extension == ".mp3" || extension == ".ogg")
                    reloadAudio();
            });

        #endregion

        #region Assemblies

        private static readonly List<string> defaultAssemblies = new List<string>()
        {
            typeof(object).Assembly.Location, // mscorlib
            typeof(System.Linq.Enumerable).Assembly.Location, // System.Core
            typeof(System.Drawing.Point).Assembly.Location, // System.Drawing
            typeof(OpenTK.Vector2).Assembly.Location, // OpenTK
            typeof(StorybrewCommon.Scripting.Script).Assembly.Location, // StorybrewCommon
        };
        public static IEnumerable<string> DefaultAssemblies => defaultAssemblies;

        private List<string> importedAssemblies = new List<string>();
        public IEnumerable<string> ImportedAssemblies
        {
            get { return importedAssemblies; }
            set
            {
                if (IsDisposed) throw new ObjectDisposedException(nameof(Project));

                importedAssemblies = new List<string>(value);
                scriptManager.ReferencedAssemblies = ReferencedAssemblies;
            }
        }

        public IEnumerable<string> ReferencedAssemblies
            => DefaultAssemblies.Concat(importedAssemblies);

        #endregion

        #region Save / Load / Export

        public const int Version = 7;

        public bool Changed { get; private set; }

        private bool ownsOsb;
        public bool OwnsOsb
        {
            get { return ownsOsb; }
            set
            {
                if (ownsOsb == value) return;
                ownsOsb = value;
                Changed = true;
            }
        }

        private static readonly Regex effectGuidRegex = new Regex("effect\\.([a-z0-9]{32})\\.yaml", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public void Save()
        {
            saveText(projectPath.Replace(DefaultBinaryFilename, DefaultTextFilename));
        }

        public static Project Load(string projectPath, bool withCommonScripts, ResourceContainer resourceContainer)
        {
            // Binary format isn't saved anymore and may be obsolete:
            // Load from the text format if possible even if the binary format has been selected.
            var textFormatPath = projectPath.Replace(DefaultBinaryFilename, DefaultTextFilename);
            if (projectPath.EndsWith(BinaryExtension) && File.Exists(textFormatPath))
                projectPath = textFormatPath;

            var project = new Project(projectPath, withCommonScripts, resourceContainer);
            if (projectPath.EndsWith(BinaryExtension))
                project.loadBinary(projectPath);
            else project.loadText(textFormatPath);
            return project;
        }

        private void loadBinary(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open))
            using (var r = new BinaryReader(stream, Encoding.UTF8))
            {
                var version = r.ReadInt32();
                if (version > Version)
                    throw new InvalidOperationException("This project was saved with a more recent version, you need to update to open it");

                var savedBy = r.ReadString();
                Debug.Print($"Loading project saved by {savedBy}");

                MapsetPath = r.ReadString();
                if (version >= 1)
                {
                    var mainBeatmapId = r.ReadInt64();
                    var mainBeatmapName = r.ReadString();
                    SelectBeatmap(mainBeatmapId, mainBeatmapName);
                }

                OwnsOsb = version >= 4 ? r.ReadBoolean() : true;

                var effectCount = r.ReadInt32();
                for (int effectIndex = 0; effectIndex < effectCount; effectIndex++)
                {
                    var guid = version >= 6 ? new Guid(r.ReadBytes(16)) : Guid.NewGuid();
                    var baseName = r.ReadString();
                    var name = r.ReadString();

                    var effect = AddScriptedEffect(baseName);
                    effect.Guid = guid;
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
                            effect.Config.UpdateField(fieldName, fieldDisplayName, null, fieldIndex, fieldValue?.GetType(), fieldValue, allowedValues, null);
                        }
                    }
                }

                var layerCount = r.ReadInt32();
                for (var layerIndex = 0; layerIndex < layerCount; layerIndex++)
                {
                    var guid = version >= 6 ? new Guid(r.ReadBytes(16)) : Guid.NewGuid();
                    var identifier = r.ReadString();
                    var effectIndex = r.ReadInt32();
                    var diffSpecific = version >= 3 ? r.ReadBoolean() : false;
                    var osbLayer = version >= 2 ? (OsbLayer)r.ReadInt32() : OsbLayer.Background;
                    var visible = r.ReadBoolean();

                    var effect = effects[effectIndex];
                    effect.AddPlaceholder(new EditorStoryboardLayer(identifier, effect)
                    {
                        Guid = guid,
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
                    ImportedAssemblies = importedAssemblies;
                }
            }
        }

        private void saveText(string path)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(Project));

            // Create the opener file
            if (!File.Exists(path))
                File.WriteAllText(path, "# This file is only used to open the project\n# Project data is contained in the .sbrew folder");

            var projectDirectory = Path.GetDirectoryName(path);

            var gitIgnorePath = Path.Combine(projectDirectory, ".gitignore");
            if (!File.Exists(gitIgnorePath))
                File.WriteAllText(gitIgnorePath, ".sbrew/user.yaml\n.sbrew.tmp\n.sbrew.bak\n.cache\n.vs");

            var targetDirectory = Path.Combine(projectDirectory, DataFolder);
            using (var directoryWriter = new SafeDirectoryWriter(targetDirectory))
            {
                // Write the index
                {
                    var indexRoot = new TinyObject
                    {
                        { "FormatVersion", Version },
                        { "BeatmapId", MainBeatmap.Id },
                        { "BeatmapName", MainBeatmap.Name },
                        { "Assemblies", importedAssemblies },
                        { "Layers", LayerManager.Layers.Select(l => l.Guid.ToString("N")) },
                    };

                    var indexPath = directoryWriter.GetPath("index.yaml");
                    indexRoot.Write(indexPath);
                }

                // Write user specific data
                {
                    var userRoot = new TinyObject
                    {
                        { "FormatVersion", Version },
                        { "Editor", Program.FullName },
                        { "MapsetPath", PathHelper.WithStandardSeparators(MapsetPath) },
                        { "ExportTimeAsFloatingPoint", ExportSettings.UseFloatForTime },
                        { "OwnsOsb", OwnsOsb },
                    };

                    var userPath = directoryWriter.GetPath("user.yaml");
                    userRoot.Write(userPath);
                }

                // Write each effect
                foreach (var effect in effects)
                {
                    var effectRoot = new TinyObject
                    {
                        { "FormatVersion", Version },
                        { "Name", effect.Name },
                        { "Script", effect.BaseName },
                        { "Multithreaded", effect.Multithreaded },
                    };

                    var configRoot = new TinyObject();
                    effectRoot.Add("Config", configRoot);

                    foreach (var field in effect.Config.SortedFields)
                    {
                        var fieldRoot = new TinyObject
                        {
                            { "Type", field.Type.FullName },
                            { "Value", ObjectSerializer.ToString(field.Type, field.Value)},
                        };
                        if (field.DisplayName != field.Name)
                            fieldRoot.Add("DisplayName", field.DisplayName);
                        if (!string.IsNullOrWhiteSpace(field.BeginsGroup))
                            fieldRoot.Add("BeginsGroup", field.BeginsGroup);
                        configRoot.Add(field.Name, fieldRoot);

                        if ((field.AllowedValues?.Length ?? 0) > 0)
                        {
                            var allowedValuesRoot = new TinyObject();
                            fieldRoot.Add("AllowedValues", allowedValuesRoot);

                            foreach (var allowedValue in field.AllowedValues)
                                allowedValuesRoot.Add(allowedValue.Name, ObjectSerializer.ToString(field.Type, allowedValue.Value));
                        }
                    }

                    var layersRoot = new TinyObject();
                    effectRoot.Add("Layers", layersRoot);

                    foreach (var layer in LayerManager.Layers.Where(l => l.Effect == effect))
                    {
                        var layerRoot = new TinyObject
                        {
                            { "Name", layer.Identifier },
                            { "OsbLayer", layer.OsbLayer },
                            { "DiffSpecific", layer.DiffSpecific },
                            { "Visible", layer.Visible },
                        };
                        layersRoot.Add(layer.Guid.ToString("N"), layerRoot);
                    }

                    var effectPath = directoryWriter.GetPath("effect." + effect.Guid.ToString("N") + ".yaml");
                    effectRoot.Write(effectPath);
                }

                directoryWriter.Commit(checkPaths: true);
                Changed = false;
            }
        }

        private void loadText(string path)
        {
            var targetDirectory = Path.Combine(Path.GetDirectoryName(path), DataFolder);
            using (var directoryReader = new SafeDirectoryReader(targetDirectory))
            {
                var indexPath = directoryReader.GetPath("index.yaml");
                var indexRoot = TinyToken.Read(indexPath);

                var indexVersion = indexRoot.Value<int>("FormatVersion");
                if (indexVersion > Version)
                    throw new InvalidOperationException("This project was saved with a more recent version, you need to update to open it");

                var userPath = directoryReader.GetPath("user.yaml");
                var userRoot = (TinyToken)null;
                if (File.Exists(userPath))
                {
                    userRoot = TinyToken.Read(userPath);

                    var userVersion = userRoot.Value<int>("FormatVersion");
                    if (userVersion > Version)
                        throw new InvalidOperationException("This project's user settings were saved with a more recent version, you need to update to open it");

                    var savedBy = userRoot.Value<string>("Editor");
                    Debug.Print($"Project saved by {savedBy}");

                    ExportSettings.UseFloatForTime = userRoot.Value<bool>("ExportTimeAsFloatingPoint");
                    OwnsOsb = userRoot.Value<bool>("OwnsOsb");
                }

                MapsetPath = userRoot?.Value<string>("MapsetPath") ?? indexRoot.Value<string>("MapsetPath") ?? "nul";
                SelectBeatmap(indexRoot.Value<long>("BeatmapId"), indexRoot.Value<string>("BeatmapName"));
                ImportedAssemblies = indexRoot.Values<string>("Assemblies");

                // Load effects
                var layerInserters = new Dictionary<string, Action>();
                foreach (var effectPath in Directory.EnumerateFiles(directoryReader.Path, "effect.*.yaml", SearchOption.TopDirectoryOnly))
                {
                    var guidMatch = effectGuidRegex.Match(effectPath);
                    if (!guidMatch.Success || guidMatch.Groups.Count < 2)
                        throw new InvalidDataException($"Could not parse effect Guid from '{effectPath}'");

                    var effectRoot = TinyToken.Read(effectPath);

                    var effectVersion = effectRoot.Value<int>("FormatVersion");
                    if (effectVersion > Version)
                        throw new InvalidOperationException("This project contains an effect that was saved with a more recent version, you need to update to open it");

                    var effect = AddScriptedEffect(effectRoot.Value<string>("Script"), effectRoot.Value<bool>("Multithreaded"));
                    effect.Guid = Guid.Parse(guidMatch.Groups[1].Value);
                    effect.Name = effectRoot.Value<string>("Name");

                    var configRoot = effectRoot.Value<TinyObject>("Config");
                    var fieldIndex = 0;
                    foreach (var fieldProperty in configRoot)
                    {
                        var fieldRoot = fieldProperty.Value;

                        var fieldTypeName = fieldRoot.Value<string>("Type");
                        var fieldContent = fieldRoot.Value<string>("Value");
                        var beginsGroup = fieldRoot.Value<string>("BeginsGroup");

                        var fieldValue = ObjectSerializer.FromString(fieldTypeName, fieldContent);

                        var allowedValues = fieldRoot
                                .Value<TinyObject>("AllowedValues")?
                                .Select(p => new NamedValue { Name = p.Key, Value = ObjectSerializer.FromString(fieldTypeName, p.Value.Value<string>()), })
                                .ToArray();

                        effect.Config.UpdateField(fieldProperty.Key, fieldRoot.Value<string>("DisplayName"), null, fieldIndex++, fieldValue?.GetType(), fieldValue, allowedValues, beginsGroup);
                    }

                    var layersRoot = effectRoot.Value<TinyObject>("Layers");
                    foreach (var layerProperty in layersRoot)
                    {
                        var layerEffect = effect;
                        var layerGuid = layerProperty.Key;
                        var layerRoot = layerProperty.Value;
                        layerInserters.Add(layerGuid, () => layerEffect.AddPlaceholder(new EditorStoryboardLayer(layerRoot.Value<string>("Name"), layerEffect)
                        {
                            Guid = Guid.Parse(layerGuid),
                            OsbLayer = layerRoot.Value<OsbLayer>("OsbLayer"),
                            DiffSpecific = layerRoot.Value<bool>("DiffSpecific"),
                            Visible = layerRoot.Value<bool>("Visible"),
                        }));
                    }
                }

                if (effects.Count == 0)
                    EffectsStatus = EffectStatus.Ready;

                // Insert layers defined in the index
                var layersOrder = indexRoot.Values<string>("Layers");
                if (layersOrder != null)
                    foreach (var layerGuid in layersOrder.Distinct())
                    {
                        if (layerInserters.TryGetValue(layerGuid, out var insertLayer))
                            insertLayer();
                    }

                // Insert all remaining layers
                foreach (var key in layersOrder == null ? layerInserters.Keys : layerInserters.Keys.Except(layersOrder))
                {
                    var insertLayer = layerInserters[key];
                    insertLayer();
                }
            }
        }

        public static Project Create(string projectFolderName, string mapsetPath, bool withCommonScripts, ResourceContainer resourceContainer)
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
            var project = new Project(Path.Combine(projectFolderPath, DefaultTextFilename), withCommonScripts, resourceContainer)
            {
                MapsetPath = mapsetPath,
            };
            project.Save();

            return project;
        }

        /// <summary>
        /// Doesn't run in the main thread
        /// </summary>
        public void ExportToOsb(bool exportOsb = true)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(Project));

            string osuPath = null, osbPath = null;
            List<EditorStoryboardLayer> localLayers = null;
            Program.RunMainThread(() =>
            {
                osuPath = MainBeatmap.Path;
                osbPath = OsbPath;

                if (!OwnsOsb && File.Exists(osbPath))
                    File.Copy(osbPath, $"{osbPath}.bak");
                OwnsOsb = true;

                localLayers = new List<EditorStoryboardLayer>(LayerManager.FindLayers(l => l.Visible));
            });

            var usesOverlayLayer = localLayers.Any(l => l.OsbLayer == OsbLayer.Overlay);

            if (!string.IsNullOrEmpty(osuPath))
            {
                Debug.Print($"Exporting diff specific events to {osuPath}");
                using (var stream = new SafeWriteStream(osuPath))
                using (var writer = new StreamWriter(stream, Encoding))
                using (var fileStream = new FileStream(osuPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new StreamReader(fileStream, Encoding))
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
                                        if (osbLayer == OsbLayer.Overlay && !usesOverlayLayer)
                                            continue;

                                        writer.WriteLine($"//Storyboard Layer {(int)osbLayer} ({osbLayer})");
                                        foreach (var layer in localLayers)
                                            if (layer.OsbLayer == osbLayer && layer.DiffSpecific)
                                                layer.WriteOsb(writer, ExportSettings);
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

            if (exportOsb)
            {
                Debug.Print($"Exporting osb to {osbPath}");
                using (var stream = new SafeWriteStream(osbPath))
                using (var writer = new StreamWriter(stream, Encoding))
                {
                    writer.WriteLine("[Events]");
                    writer.WriteLine("//Background and Video events");
                    foreach (var osbLayer in OsbLayers)
                    {
                        if (osbLayer == OsbLayer.Overlay && !usesOverlayLayer)
                            continue;

                        writer.WriteLine($"//Storyboard Layer {(int)osbLayer} ({osbLayer})");
                        foreach (var layer in localLayers)
                            if (layer.OsbLayer == osbLayer && !layer.DiffSpecific)
                                layer.WriteOsb(writer, ExportSettings);
                    }
                    writer.WriteLine("//Storyboard Sound Samples");
                    stream.Commit();
                }
            }
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

        public bool IsDisposed { get; private set; } = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // Always dispose this first to ensure updates aren't happening while the project is being disposed
                    effectUpdateQueue.Dispose();

                    assetWatcher.Dispose();
                    MapsetManager?.Dispose();
                    scriptManager.Dispose();
                    TextureContainer.Dispose();
                    AudioContainer.Dispose();
                }
                assetWatcher = null;
                MapsetManager = null;
                effectUpdateQueue = null;
                scriptManager = null;
                TextureContainer = null;
                AudioContainer = null;
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
