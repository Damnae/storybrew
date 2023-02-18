using BrewLib.UserInterface;
using OpenTK;
using StorybrewCommon.OpenTKUtil;
using System;
using System.Globalization;

namespace StorybrewEditor.UserInterface
{
    public class Vector2Picker : Widget, Field
    {
        readonly LinearLayout layout;
        readonly Textbox xTextbox, yTextbox;

        public override Vector2 MinSize => layout.MinSize;
        public override Vector2 MaxSize => Vector2.Zero;
        public override Vector2 PreferredSize => layout.PreferredSize;

        Vector2 value;
        public Vector2 Value
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
            set => Value = (Vector2)value;
        }

        public event EventHandler OnValueChanged, OnValueCommited;

        public Vector2Picker(WidgetManager manager) : base(manager)
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
                    }
                }
            });
            updateWidgets();

            xTextbox.OnValueCommited += xTextbox_OnValueCommited;
            yTextbox.OnValueCommited += yTextbox_OnValueCommited;
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
            Value = new Vector2(x, value.Y);
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
            Value = new Vector2(value.X, y);
            OnValueCommited?.Invoke(this, EventArgs.Empty);
        }
        void updateWidgets()
        {
            xTextbox.SetValueSilent(value.X.ToString(CultureInfo.InvariantCulture));
            yTextbox.SetValueSilent(value.Y.ToString(CultureInfo.InvariantCulture));
        }
        protected override void Layout()
        {
            base.Layout();
            layout.Size = Size;
        }
    }
    public class Vector2dPicker : Widget, Field
    {
        readonly LinearLayout layout;
        readonly Textbox xTextbox, yTextbox;

        public override Vector2 MinSize => layout.MinSize;
        public override Vector2 MaxSize => Vector2.Zero;
        public override Vector2 PreferredSize => layout.PreferredSize;

        Vector2d value;
        public Vector2d Value
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
            set => Value = (Vector2d)value;
        }

        public event EventHandler OnValueChanged, OnValueCommited;

        public Vector2dPicker(WidgetManager manager) : base(manager)
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
                    }
                }
            });
            updateWidgets();

            xTextbox.OnValueCommited += xTextbox_OnValueCommited;
            yTextbox.OnValueCommited += yTextbox_OnValueCommited;
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
            Value = new Vector2d(x, value.Y);
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
            Value = new Vector2d(value.X, y);
            OnValueCommited?.Invoke(this, EventArgs.Empty);
        }
        void updateWidgets()
        {
            xTextbox.SetValueSilent(value.X.ToString(CultureInfo.InvariantCulture));
            yTextbox.SetValueSilent(value.Y.ToString(CultureInfo.InvariantCulture));
        }
        protected override void Layout()
        {
            base.Layout();
            layout.Size = Size;
        }
    }
    public class Vector2iPicker : Widget, Field
    {
        readonly LinearLayout layout;
        readonly Textbox xTextbox, yTextbox;

        public override Vector2 MinSize => layout.MinSize;
        public override Vector2 MaxSize => Vector2.Zero;
        public override Vector2 PreferredSize => layout.PreferredSize;

        Vector2i value;
        public Vector2i Value
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
            set => Value = (Vector2i)value;
        }

        public event EventHandler OnValueChanged, OnValueCommited;

        public Vector2iPicker(WidgetManager manager) : base(manager)
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
                    }
                }
            });
            updateWidgets();

            xTextbox.OnValueCommited += xTextbox_OnValueCommited;
            yTextbox.OnValueCommited += yTextbox_OnValueCommited;
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
            Value = new Vector2i(x, value.Y);
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
            Value = new Vector2i(value.X, y);
            OnValueCommited?.Invoke(this, EventArgs.Empty);
        }
        void updateWidgets()
        {
            xTextbox.SetValueSilent(value.X.ToString(CultureInfo.InvariantCulture));
            yTextbox.SetValueSilent(value.Y.ToString(CultureInfo.InvariantCulture));
        }
        protected override void Layout()
        {
            base.Layout();
            layout.Size = Size;
        }
    }
}