using BrewLib.UserInterface;
using OpenTK;
using StorybrewCommon.OpenTKUtil;
using System;
using System.Globalization;

namespace StorybrewEditor.UserInterface
{
    public class Vector3Picker : Widget, Field
    {
        readonly LinearLayout layout;
        readonly Textbox xTextbox, yTextbox, zTextbox;

        public override Vector2 MinSize => layout.MinSize;
        public override Vector2 MaxSize => Vector2.Zero;
        public override Vector2 PreferredSize => layout.PreferredSize;

        Vector3 value;
        public Vector3 Value
        {
            get => value;
            set
            {
                if (this.value == value) return;
                this.value = value;

                updateWidgets();
                OnValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public object FieldValue
        {
            get => Value;
            set => Value = (Vector3)value;
        }

        public event EventHandler OnValueChanged, OnValueCommited;

        public Vector3Picker(WidgetManager manager) : base(manager)
        {
            Add(layout = new LinearLayout(manager)
            {
                FitChildren = true,
                Children = new Widget[]
                {
                    new LinearLayout(manager)
                    {
                        Horizontal = true,
                        FitChildren = true,
                        Fill = true,
                        Children = new Widget[]
                        {
                            new Label(Manager)
                            {
                                StyleName = "small",
                                Text = "X",
                                CanGrow = false
                            },
                            xTextbox = new Textbox(manager)
                            {
                                EnterCommits = true
                            }
                        }
                    },
                    new LinearLayout(manager)
                    {
                        Horizontal = true,
                        FitChildren = true,
                        Fill = true,
                        Children = new Widget[]
                        {
                            new Label(Manager)
                            {
                                StyleName = "small",
                                Text = "Y",
                                CanGrow = false
                            },
                            yTextbox = new Textbox(manager)
                            {
                                EnterCommits = true
                            }
                        }
                    },
                    new LinearLayout(manager)
                    {
                        Horizontal = true,
                        FitChildren = true,
                        Fill = true,
                        Children = new Widget[]
                        {
                            new Label(Manager)
                            {
                                StyleName = "small",
                                Text = "Z",
                                CanGrow = false
                            },
                            zTextbox = new Textbox(manager)
                            {
                                EnterCommits = true
                            }
                        }
                    }
                }
            });
            updateWidgets();

            xTextbox.OnValueCommited += xTextbox_OnValueCommited;
            yTextbox.OnValueCommited += yTextbox_OnValueCommited;
            zTextbox.OnValueCommited += zTextbox_OnValueCommited;
        }
        void xTextbox_OnValueCommited(object sender, EventArgs e)
        {
            var xCommit = xTextbox.Value;

            float x;
            try
            {
                x = float.Parse(xCommit, CultureInfo.InvariantCulture);
            }
            catch
            {
                updateWidgets();
                return;
            }
            Value = new Vector3(x, value.Y, value.Z);
            OnValueCommited?.Invoke(this, EventArgs.Empty);
        }
        void yTextbox_OnValueCommited(object sender, EventArgs e)
        {
            var yCommit = yTextbox.Value;

            float y;
            try
            {
                y = float.Parse(yCommit, CultureInfo.InvariantCulture);
            }
            catch
            {
                updateWidgets();
                return;
            }
            Value = new Vector3(value.X, y, value.Z);
            OnValueCommited?.Invoke(this, EventArgs.Empty);
        }
        void zTextbox_OnValueCommited(object sender, EventArgs e)
        {
            var zCommit = zTextbox.Value;

            float z;
            try
            {
                z = float.Parse(zCommit, CultureInfo.InvariantCulture);
            }
            catch
            {
                updateWidgets();
                return;
            }
            Value = new Vector3(value.X, value.Y, z);
            OnValueCommited?.Invoke(this, EventArgs.Empty);
        }
        void updateWidgets()
        {
            xTextbox.SetValueSilent(value.X.ToString(CultureInfo.InvariantCulture));
            yTextbox.SetValueSilent(value.Y.ToString(CultureInfo.InvariantCulture));
            zTextbox.SetValueSilent(value.Z.ToString(CultureInfo.InvariantCulture));
        }
        protected override void Layout()
        {
            base.Layout();
            layout.Size = Size;
        }
    }
    public class Vector3dPicker : Widget, Field
    {
        readonly LinearLayout layout;
        readonly Textbox xTextbox, yTextbox, zTextbox;

        public override Vector2 MinSize => layout.MinSize;
        public override Vector2 MaxSize => Vector2.Zero;
        public override Vector2 PreferredSize => layout.PreferredSize;

        Vector3d value;
        public Vector3d Value
        {
            get => value;
            set
            {
                if (this.value == value) return;
                this.value = value;

                updateWidgets();
                OnValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public object FieldValue
        {
            get => Value;
            set => Value = (Vector3d)value;
        }

        public event EventHandler OnValueChanged, OnValueCommited;

        public Vector3dPicker(WidgetManager manager) : base(manager)
        {
            Add(layout = new LinearLayout(manager)
            {
                FitChildren = true,
                Children = new Widget[]
                {
                    new LinearLayout(manager)
                    {
                        Horizontal = true,
                        FitChildren = true,
                        Fill = true,
                        Children = new Widget[]
                        {
                            new Label(Manager)
                            {
                                StyleName = "small",
                                Text = "X",
                                CanGrow = false
                            },
                            xTextbox = new Textbox(manager)
                            {
                                EnterCommits = true
                            }
                        }
                    },
                    new LinearLayout(manager)
                    {
                        Horizontal = true,
                        FitChildren = true,
                        Fill = true,
                        Children = new Widget[]
                        {
                            new Label(Manager)
                            {
                                StyleName = "small",
                                Text = "Y",
                                CanGrow = false
                            },
                            yTextbox = new Textbox(manager)
                            {
                                EnterCommits = true
                            }
                        }
                    },
                    new LinearLayout(manager)
                    {
                        Horizontal = true,
                        FitChildren = true,
                        Fill = true,
                        Children = new Widget[]
                        {
                            new Label(Manager)
                            {
                                StyleName = "small",
                                Text = "Z",
                                CanGrow = false
                            },
                            zTextbox = new Textbox(manager)
                            {
                                EnterCommits = true
                            }
                        }
                    }
                }
            });
            updateWidgets();

            xTextbox.OnValueCommited += xTextbox_OnValueCommited;
            yTextbox.OnValueCommited += yTextbox_OnValueCommited;
            zTextbox.OnValueCommited += zTextbox_OnValueCommited;
        }
        void xTextbox_OnValueCommited(object sender, EventArgs e)
        {
            var xCommit = xTextbox.Value;

            double x;
            try
            {
                x = double.Parse(xCommit, CultureInfo.InvariantCulture);
            }
            catch
            {
                updateWidgets();
                return;
            }
            Value = new Vector3d(x, value.Y, value.Z);
            OnValueCommited?.Invoke(this, EventArgs.Empty);
        }
        void yTextbox_OnValueCommited(object sender, EventArgs e)
        {
            var yCommit = yTextbox.Value;

            double y;
            try
            {
                y = double.Parse(yCommit, CultureInfo.InvariantCulture);
            }
            catch
            {
                updateWidgets();
                return;
            }
            Value = new Vector3d(value.X, y, value.Z);
            OnValueCommited?.Invoke(this, EventArgs.Empty);
        }
        void zTextbox_OnValueCommited(object sender, EventArgs e)
        {
            var zCommit = zTextbox.Value;

            double z;
            try
            {
                z = double.Parse(zCommit, CultureInfo.InvariantCulture);
            }
            catch
            {
                updateWidgets();
                return;
            }
            Value = new Vector3d(value.X, value.Y, z);
            OnValueCommited?.Invoke(this, EventArgs.Empty);
        }
        void updateWidgets()
        {
            xTextbox.SetValueSilent(value.X.ToString(CultureInfo.InvariantCulture));
            yTextbox.SetValueSilent(value.Y.ToString(CultureInfo.InvariantCulture));
            zTextbox.SetValueSilent(value.Z.ToString(CultureInfo.InvariantCulture));
        }
        protected override void Layout()
        {
            base.Layout();
            layout.Size = Size;
        }
    }
    public class Vector3iPicker : Widget, Field
    {
        readonly LinearLayout layout;
        readonly Textbox xTextbox, yTextbox, zTextbox;

        public override Vector2 MinSize => layout.MinSize;
        public override Vector2 MaxSize => Vector2.Zero;
        public override Vector2 PreferredSize => layout.PreferredSize;

        Vector3i value;
        public Vector3i Value
        {
            get => value;
            set
            {
                if (this.value == value) return;
                this.value = value;

                updateWidgets();
                OnValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public object FieldValue
        {
            get => Value;
            set => Value = (Vector3i)value;
        }

        public event EventHandler OnValueChanged, OnValueCommited;

        public Vector3iPicker(WidgetManager manager) : base(manager)
        {
            Add(layout = new LinearLayout(manager)
            {
                FitChildren = true,
                Children = new Widget[]
                {
                    new LinearLayout(manager)
                    {
                        Horizontal = true,
                        FitChildren = true,
                        Fill = true,
                        Children = new Widget[]
                        {
                            new Label(Manager)
                            {
                                StyleName = "small",
                                Text = "X",
                                CanGrow = false
                            },
                            xTextbox = new Textbox(manager)
                            {
                                EnterCommits = true
                            }
                        }
                    },
                    new LinearLayout(manager)
                    {
                        Horizontal = true,
                        FitChildren = true,
                        Fill = true,
                        Children = new Widget[]
                        {
                            new Label(Manager)
                            {
                                StyleName = "small",
                                Text = "Y",
                                CanGrow = false
                            },
                            yTextbox = new Textbox(manager)
                            {
                                EnterCommits = true
                            }
                        }
                    },
                    new LinearLayout(manager)
                    {
                        Horizontal = true,
                        FitChildren = true,
                        Fill = true,
                        Children = new Widget[]
                        {
                            new Label(Manager)
                            {
                                StyleName = "small",
                                Text = "Z",
                                CanGrow = false
                            },
                            zTextbox = new Textbox(manager)
                            {
                                EnterCommits = true
                            }
                        }
                    }
                }
            });
            updateWidgets();

            xTextbox.OnValueCommited += xTextbox_OnValueCommited;
            yTextbox.OnValueCommited += yTextbox_OnValueCommited;
            zTextbox.OnValueCommited += zTextbox_OnValueCommited;
        }
        void xTextbox_OnValueCommited(object sender, EventArgs e)
        {
            var xCommit = xTextbox.Value;

            int x;
            try
            {
                x = int.Parse(xCommit, CultureInfo.InvariantCulture);
            }
            catch
            {
                updateWidgets();
                return;
            }
            Value = new Vector3i(x, value.Y, value.Z);
            OnValueCommited?.Invoke(this, EventArgs.Empty);
        }
        void yTextbox_OnValueCommited(object sender, EventArgs e)
        {
            var yCommit = yTextbox.Value;

            int y;
            try
            {
                y = int.Parse(yCommit, CultureInfo.InvariantCulture);
            }
            catch
            {
                updateWidgets();
                return;
            }
            Value = new Vector3i(value.X, y, value.Z);
            OnValueCommited?.Invoke(this, EventArgs.Empty);
        }
        void zTextbox_OnValueCommited(object sender, EventArgs e)
        {
            var zCommit = zTextbox.Value;

            int z;
            try
            {
                z = int.Parse(zCommit, CultureInfo.InvariantCulture);
            }
            catch
            {
                updateWidgets();
                return;
            }
            Value = new Vector3i(value.X, value.Y, z);
            OnValueCommited?.Invoke(this, EventArgs.Empty);
        }
        void updateWidgets()
        {
            xTextbox.SetValueSilent(value.X.ToString(CultureInfo.InvariantCulture));
            yTextbox.SetValueSilent(value.Y.ToString(CultureInfo.InvariantCulture));
            zTextbox.SetValueSilent(value.Z.ToString(CultureInfo.InvariantCulture));
        }
        protected override void Layout()
        {
            base.Layout();
            layout.Size = Size;
        }
    }
}