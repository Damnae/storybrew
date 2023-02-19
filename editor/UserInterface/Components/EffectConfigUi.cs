using BrewLib.UserInterface;
using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.OpenTKUtil;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Util;
using StorybrewEditor.Storyboarding;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace StorybrewEditor.UserInterface.Components
{
    public class EffectConfigUi : Widget
    {
        const string effectConfigFormat = "storybrewEffectConfig";

        readonly Label titleLabel;
        readonly LinearLayout layout, configFieldsLayout;

        public override Vector2 MinSize => layout.MinSize;
        public override Vector2 MaxSize => layout.MaxSize;
        public override Vector2 PreferredSize => layout.PreferredSize;

        Effect effect;
        public Effect Effect
        {
            get => effect;
            set
            {
                if (effect == value) return;
                if (effect != null)
                {
                    effect.OnChanged -= Effect_OnChanged;
                    effect.OnConfigFieldsChanged -= Effect_OnConfigFieldsChanged;
                }
                effect = value;
                if (effect != null)
                {
                    effect.OnChanged += Effect_OnChanged;
                    effect.OnConfigFieldsChanged += Effect_OnConfigFieldsChanged;
                }

                updateEffect();
                updateFields();
            }
        }
        public EffectConfigUi(WidgetManager manager) : base(manager)
        {
            Button copyButton, pasteButton, closeButton;

            Add(layout = new LinearLayout(manager)
            {
                StyleName = "panel",
                Padding = new FourSide(16),
                FitChildren = true,
                Fill = true,
                Children = new Widget[]
                {
                    new LinearLayout(manager)
                    {
                        Fill = true,
                        FitChildren = true,
                        Horizontal = true,
                        CanGrow = false,
                        Children = new Widget[]
                        {
                            titleLabel = new Label(manager)
                            {
                                Text = "Configuration"
                            },
                            copyButton = new Button(Manager)
                            {
                                StyleName = "icon",
                                Icon = IconFont.Copy,
                                Tooltip = "Copy all fields",
                                AnchorFrom = BoxAlignment.Centre,
                                AnchorTo = BoxAlignment.Centre,
                                CanGrow = false
                            },
                            pasteButton = new Button(Manager)
                            {
                                StyleName = "icon",
                                Icon = IconFont.Paste,
                                Tooltip = "Paste all fields",
                                AnchorFrom = BoxAlignment.Centre,
                                AnchorTo = BoxAlignment.Centre,
                                CanGrow = false
                            },
                            closeButton = new Button(Manager)
                            {
                                StyleName = "icon",
                                Icon = IconFont.TimesCircle,
                                AnchorFrom = BoxAlignment.Centre,
                                AnchorTo = BoxAlignment.Centre,
                                CanGrow = false
                            }
                        }
                    },
                    new ScrollArea(manager, configFieldsLayout = new LinearLayout(manager)
                    {
                        FitChildren = true
                    })
                }
            });

            copyButton.OnClick += (sender, e) => copyConfiguration();
            pasteButton.OnClick += (sender, e) => pasteConfiguration();
            closeButton.OnClick += (sender, e) =>
            {
                Effect = null;
                Displayed = false;
            };
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) Effect = null;
            base.Dispose(disposing);
        }
        protected override void Layout()
        {
            base.Layout();
            layout.Size = Size;
        }

        void Effect_OnChanged(object sender, EventArgs e) => updateEffect();
        void Effect_OnConfigFieldsChanged(object sender, EventArgs e) => updateFields();

        void updateEffect()
        {
            if (effect == null) return;
            titleLabel.Text = $"Configuration: {effect.Name} ({effect.BaseName})";
        }
        void updateFields()
        {
            configFieldsLayout.ClearWidgets();
            if (effect == null) return;

            var currentGroup = (string)null;
            foreach (var field in effect.Config.SortedFields)
            {
                if (!string.IsNullOrWhiteSpace(field.BeginsGroup))
                {
                    currentGroup = field.BeginsGroup;
                    configFieldsLayout.Add(new Label(Manager)
                    {
                        StyleName = "listGroup",
                        Text = field.BeginsGroup,
                        AnchorFrom = BoxAlignment.Centre,
                        AnchorTo = BoxAlignment.Centre
                    });
                }

                var displayName = field.DisplayName;
                if (currentGroup != null)
                    displayName = Regex.Replace(displayName, $@"^{Regex.Escape(currentGroup)}\s+", "");

                var description = $"Variable: {field.Name} ({field.Type.Name})";
                if (!string.IsNullOrWhiteSpace(field.Description))
                    description = "  " + description + "\n" + field.Description;

                configFieldsLayout.Add(new LinearLayout(Manager)
                {
                    AnchorFrom = BoxAlignment.Centre,
                    AnchorTo = BoxAlignment.Centre,
                    Horizontal = true,
                    Fill = true,
                    Children = new Widget[]
                    {
                        new Label(Manager)
                        {
                            StyleName = "listItem",
                            Text = displayName,
                            AnchorFrom = BoxAlignment.TopLeft,
                            AnchorTo = BoxAlignment.TopLeft,
                            Tooltip = description
                        },
                        buildFieldEditor(field)
                    }
                });
            }
        }
        Widget buildFieldEditor(EffectConfig.ConfigField field)
        {
            if (field.AllowedValues != null)
            {
                var widget = new Selectbox(Manager)
                {
                    Value = field.Value,
                    Options = field.AllowedValues,
                    AnchorFrom = BoxAlignment.Right,
                    AnchorTo = BoxAlignment.Right,
                    CanGrow = false
                };
                widget.OnValueChanged += (sender, e) => setFieldValue(field, widget.Value);
                return widget;
            }
            else if (field.Type == typeof(bool))
            {
                var widget = new Selectbox(Manager)
                {
                    Value = field.Value,
                    Options = new NamedValue[]
                    {
                        new NamedValue{ Name = true.ToString(), Value = true },
                        new NamedValue{ Name = false.ToString(), Value = false }
                    },
                    AnchorFrom = BoxAlignment.Right,
                    AnchorTo = BoxAlignment.Right,
                    CanGrow = false
                };
                widget.OnValueChanged += (sender, e) => setFieldValue(field, widget.Value);
                return widget;
            }
            else if (field.Type == typeof(string))
            {
                var widget = new Textbox(Manager)
                {
                    Value = field.Value?.ToString(),
                    AnchorFrom = BoxAlignment.Right,
                    AnchorTo = BoxAlignment.Right,
                    AcceptMultiline = true,
                    CanGrow = false
                };
                widget.OnValueCommited += (sender, e) =>
                {
                    setFieldValue(field, widget.Value);
                    widget.Value = effect.Config.GetValue(field.Name).ToString();
                };
                return widget;
            }
            else if (field.Type == typeof(Vector2))
            {
                var widget = new Vector2Picker(Manager)
                {
                    Value = (Vector2)field.Value,
                    AnchorFrom = BoxAlignment.Right,
                    AnchorTo = BoxAlignment.Right,
                    CanGrow = false
                };
                widget.OnValueCommited += (sender, e) =>
                {
                    setFieldValue(field, widget.Value);
                    widget.Value = (Vector2)effect.Config.GetValue(field.Name);
                };
                return widget;
            }
            else if (field.Type == typeof(Vector2d))
            {
                var widget = new Vector2dPicker(Manager)
                {
                    Value = (Vector2d)field.Value,
                    AnchorFrom = BoxAlignment.Right,
                    AnchorTo = BoxAlignment.Right,
                    CanGrow = false
                };
                widget.OnValueCommited += (sender, e) =>
                {
                    setFieldValue(field, widget.Value);
                    widget.Value = (Vector2d)effect.Config.GetValue(field.Name);
                };
                return widget;
            }
            else if (field.Type == typeof(Vector2i))
            {
                var widget = new Vector2iPicker(Manager)
                {
                    Value = (Vector2i)field.Value,
                    AnchorFrom = BoxAlignment.Right,
                    AnchorTo = BoxAlignment.Right,
                    CanGrow = false
                };
                widget.OnValueCommited += (sender, e) =>
                {
                    setFieldValue(field, widget.Value);
                    widget.Value = (Vector2i)effect.Config.GetValue(field.Name);
                };
                return widget;
            }
            else if (field.Type == typeof(Vector3))
            {
                var widget = new Vector3Picker(Manager)
                {
                    Value = (Vector3)field.Value,
                    AnchorFrom = BoxAlignment.Right,
                    AnchorTo = BoxAlignment.Right,
                    CanGrow = false
                };
                widget.OnValueCommited += (sender, e) =>
                {
                    setFieldValue(field, widget.Value);
                    widget.Value = (Vector3)effect.Config.GetValue(field.Name);
                };
                return widget;
            }
            else if (field.Type == typeof(Vector3d))
            {
                var widget = new Vector3dPicker(Manager)
                {
                    Value = (Vector3d)field.Value,
                    AnchorFrom = BoxAlignment.Right,
                    AnchorTo = BoxAlignment.Right,
                    CanGrow = false
                };
                widget.OnValueCommited += (sender, e) =>
                {
                    setFieldValue(field, widget.Value);
                    widget.Value = (Vector3d)effect.Config.GetValue(field.Name);
                };
                return widget;
            }
            else if (field.Type == typeof(Vector3i))
            {
                var widget = new Vector3iPicker(Manager)
                {
                    Value = (Vector3i)field.Value,
                    AnchorFrom = BoxAlignment.Right,
                    AnchorTo = BoxAlignment.Right,
                    CanGrow = false
                };
                widget.OnValueCommited += (sender, e) =>
                {
                    setFieldValue(field, widget.Value);
                    widget.Value = (Vector3i)effect.Config.GetValue(field.Name);
                };
                return widget;
            }
            else if (field.Type == typeof(Color4))
            {
                var widget = new HsbColorPicker(Manager)
                {
                    Value = (Color4)field.Value,
                    AnchorFrom = BoxAlignment.Right,
                    AnchorTo = BoxAlignment.Right,
                    CanGrow = false
                };
                widget.OnValueCommited += (sender, e) =>
                {
                    setFieldValue(field, widget.Value);
                    widget.Value = (Color4)effect.Config.GetValue(field.Name);
                };
                return widget;
            }
            else if (field.Type.GetInterface(nameof(IConvertible)) != null)
            {
                var widget = new Textbox(Manager)
                {
                    Value = Convert.ToString(field.Value, CultureInfo.InvariantCulture),
                    AnchorFrom = BoxAlignment.Right,
                    AnchorTo = BoxAlignment.Right,
                    CanGrow = false
                };
                widget.OnValueCommited += (sender, e) =>
                {
                    try
                    {
                        var value = Convert.ChangeType(widget.Value, field.Type, CultureInfo.InvariantCulture);
                        setFieldValue(field, value);
                    }
                    catch { }
                    widget.Value = Convert.ToString(effect.Config.GetValue(field.Name), CultureInfo.InvariantCulture);
                };
                return widget;
            }
            return new Label(Manager)
            {
                StyleName = "listItem",
                Text = field.Value.ToString(),
                Tooltip = $"Values of type {field.Type.Name} cannot be edited",
                AnchorFrom = BoxAlignment.Right,
                AnchorTo = BoxAlignment.Right,
                CanGrow = false
            };
        }

        void setFieldValue(EffectConfig.ConfigField field, object value)
        {
            if (effect.Config.SetValue(field.Name, value)) effect.Refresh();
        }
        void copyConfiguration()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(effect.Config.FieldCount);
                foreach (var field in effect.Config.Fields)
                {
                    writer.Write(field.Name);
                    ObjectSerializer.Write(writer, field.Value);
                    ClipboardHelper.SetData(effectConfigFormat, stream);
                }
            }
        }
        void pasteConfiguration()
        {
            var changed = false;
            try
            {
                using (var stream = (Stream)ClipboardHelper.GetData(effectConfigFormat)) using (var reader = new BinaryReader(stream))
                {
                    var fieldCount = reader.ReadInt32();
                    for (var i = 0; i < fieldCount; i++)
                    {
                        var name = reader.ReadString();
                        var value = ObjectSerializer.Read(reader);
                        try
                        {
                            var field = effect.Config.Fields.First(f => f.Name == name);
                            if (field.Value.Equals(value)) continue;

                            changed |= effect.Config.SetValue(name, value);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Cannot paste '{name}': {ex}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Cannot paste clipboard data: {ex}");
            }
            if (changed)
            {
                updateFields();
                effect.Refresh();
            }
        }
    }
}