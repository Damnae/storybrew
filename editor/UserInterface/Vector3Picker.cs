using BrewLib.UserInterface;
using OpenTK;
using System;
using System.Globalization;

namespace StorybrewEditor.UserInterface
{
    public class Vector3Picker : Widget, Field
    {
        private readonly LinearLayout layout;
        private readonly Textbox xTextbox;
        private readonly Textbox yTextbox;
        private readonly Textbox zTextbox;

        public override Vector2 MinSize => layout.MinSize;
        public override Vector2 MaxSize => Vector2.Zero;
        public override Vector2 PreferredSize => layout.PreferredSize;

        private Vector3 value;
        public Vector3 Value
        {
            get { return value; }
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
            get { return Value; }
            set { Value = (Vector3)value; }
        }

        public event EventHandler OnValueChanged;
        public event EventHandler OnValueCommited;

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
                                CanGrow = false,
                            },
                            xTextbox = new Textbox(manager)
                            {
                                EnterCommits = true,
                            },
                        },
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
                                CanGrow = false,
                            },
                            yTextbox = new Textbox(manager)
                            {
                                EnterCommits = true,
                            },
                        },
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
                                CanGrow = false,
                            },
                            zTextbox = new Textbox(manager)
                            {
                                EnterCommits = true,
                            },
                        },
                    },
                },
            });
            updateWidgets();

            xTextbox.OnValueCommited += xTextbox_OnValueCommited;
            yTextbox.OnValueCommited += yTextbox_OnValueCommited;
            zTextbox.OnValueCommited += zTextbox_OnValueCommited;
        }

        private void xTextbox_OnValueCommited(object sender, EventArgs e)
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

        private void yTextbox_OnValueCommited(object sender, EventArgs e)
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

        private void zTextbox_OnValueCommited(object sender, EventArgs e)
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

        private void updateWidgets()
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
