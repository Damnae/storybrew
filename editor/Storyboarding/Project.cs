using BrewLib.Audio;
using BrewLib.Data;
using BrewLib.Data.Tiny;
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
        public const string BinaryExtension = ".sbp";
        public const string TextExtension = ".yaml";
        public const string DefaultBinaryFilename = "project" + BinaryExtension;
        public const string DefaultTextFilename = "project.sbrew" + TextExtension;
        public const string DataFolder = ".sbrew";
        public const string ProjectsFolder = "projects";

        public const string FileFilter = "project files|" + DefaultBinaryFilename + ";" + DefaultTextFilename;

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

        public Project(string projectPath, bool withCommonScripts, ResourceContainer resourceContainer)
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

            scriptManager = new ScriptManager<StoryboardObjectGenerator>(resourceContainer, "StorybrewScripts", scriptsSourcePath, commonScriptsSourcePath, scriptsLibraryPath, compiledScriptsPath, ReferencedAssemblies);
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

        private AudioSampleContainer audioContainer;
        public AudioSampleContainer AudioContainer => audioContainer;

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
            audioContainer = new AudioSampleContainer(Program.AudioManager, null);
        }

        #endregion

        #region Effects

        private List<Effect> effects = new List<Effect>();
        public IEnumerable<Effect> Effects => effects;
        public event EventHandler OnEffectsChanged;

        private EffectStatus effectsStatus = EffectStatus.Initializing;
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

        public const int Version = 6;

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

        private static readonly Regex effectGuidRegex = new Regex("effect\\.([a-z0-9]{32})\\.yaml", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public void Save()
        {
            var binaryProjectPath = projectPath.Replace(DefaultTextFilename, DefaultBinaryFilename);
            if (File.Exists(binaryProjectPath))
                saveBinary(binaryProjectPath);

            saveText(projectPath.Replace(DefaultBinaryFilename, DefaultTextFilename));
        }

        public static Project Load(string projectPath, bool withCommonScripts, ResourceContainer resourceContainer)
        {
            var project = new Project(projectPath, withCommonScripts, resourceContainer);
            if (projectPath.EndsWith(BinaryExtension))
                project.loadBinary(projectPath);
            else project.loadText(projectPath.Replace(DefaultBinaryFilename, DefaultTextFilename));
            return project;
        }

        private void saveBinary(string path)
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(Project));

            using (var stream = new SafeWriteStream(path))
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
                    w.Write(effect.Guid.ToByteArray());
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
                    w.Write(layer.Guid.ToByteArray());
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

                    var effect = AddEffect(baseName);
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
                            effect.Config.UpdateField(fieldName, fieldDisplayName, fieldIndex, fieldValue?.GetType(), fieldValue, allowedValues);
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
            if (disposedValue) throw new ObjectDisposedException(nameof(Project));

            // Create the opener file
            if (!File.Exists(path))
                File.WriteAllText(path, "# This file is only used to open the project\n# Project data is contained in the .sbrew folder");

            var projectDirectory = Path.GetDirectoryName(path);

            var gitIgnorePath = Path.Combine(projectDirectory, ".gitignore");
            if (!File.Exists(gitIgnorePath))
                File.WriteAllText(gitIgnorePath, ".sbrew/user.yaml\n.sbrew.tmp\n.sbrew.bak\n.vs");

            var targetDirectory = Path.Combine(projectDirectory, DataFolder);
            using (var directoryWriter = new SafeDirectoryWriter(targetDirectory))
            {
                // Write the index
                {
                    var indexRoot = new TinyObject
                    {
                        { "FormatVersion", Version },
                        { "MapsetPath", MapsetPath },
                        { "BeatmapId", MainBeatmap.Id },
                        { "BeatmapName", MainBeatmap.Name },
                        { "Assemblies", importedAssemblies },
                        { "Layers", layerManager.Layers.Select(l => l.Guid.ToString("N")) },
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

                    foreach (var layer in layerManager.Layers.Where(l => l.Effect == effect))
                    {
                        var layerRoot = new TinyObject
                        {
                            { "Name", layer.Identifier },
                            { "OsbLayer", layer.OsbLayer.ToString() },
                            { "DiffSpecific", layer.DiffSpecific },
                            { "Visible", layer.Visible },
                        };
                        layersRoot.Add(layer.Guid.ToString("N"), layerRoot);
                    }

                    var effectPath = directoryWriter.GetPath("effect." + effect.Guid.ToString("N") + ".yaml");
                    effectRoot.Write(effectPath);
                }

                directoryWriter.Commit();
                changed = false;
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
                if (File.Exists(userPath))
                {
                    var userRoot = TinyToken.Read(userPath);

                    var userVersion = userRoot.Value<int>("FormatVersion");
                    if (userVersion > Version)
                        throw new InvalidOperationException("This project's user settings were saved with a more recent version, you need to update to open it");

                    var savedBy = userRoot.Value<string>("Editor");
                    Debug.Print($"Project saved by {savedBy}");

                    OwnsOsb = userRoot.Value<bool>("OwnsOsb");
                }

                MapsetPath = indexRoot.Value<string>("MapsetPath");
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

                    var effect = AddEffect(effectRoot.Value<string>("Script"));
                    effect.Guid = Guid.Parse(guidMatch.Groups[1].Value);
                    effect.Name = effectRoot.Value<string>("Name");

                    var configRoot = effectRoot.Value<TinyObject>("Config");
                    var fieldIndex = 0;
                    foreach (var fieldProperty in configRoot)
                    {
                        var fieldRoot = fieldProperty.Value;

                        var fieldTypeName = fieldRoot.Value<string>("Type");
                        var fieldContent = fieldRoot.Value<string>("Value");
                        var fieldValue = ObjectSerializer.FromString(fieldTypeName, fieldContent);

                        var allowedValues = fieldRoot
                                .Value<TinyObject>("AllowedValues")?
                                .Select(p => new NamedValue { Name = p.Key, Value = ObjectSerializer.FromString(fieldTypeName, p.Value.Value<string>()), })
                                .ToArray();

                        effect.Config.UpdateField(fieldProperty.Key, fieldRoot.Value<string>("DisplayName") ?? fieldProperty.Key, fieldIndex++, fieldValue?.GetType(), fieldValue, allowedValues);
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
                            OsbLayer = (OsbLayer)Enum.Parse(typeof(OsbLayer), layerRoot.Value<string>("OsbLayer")),
                            DiffSpecific = layerRoot.Value<bool>("DiffSpecific"),
                            Visible = layerRoot.Value<bool>("Visible"),
                        }));
                    }
                }

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
