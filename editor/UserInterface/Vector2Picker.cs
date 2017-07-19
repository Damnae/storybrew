using BrewLib.UserInterface;
using OpenTK;
using System;
using System.Globalization;

namespace StorybrewEditor.UserInterface
{
    public class Vector2Picker : Widget, Field
    {
        private LinearLayout layout;
        private Textbox xTextbox;
        private Textbox yTextbox;

        public override Vector2 MinSize => layout.MinSize;
        public override Vector2 MaxSize => Vector2.Zero;
        public override Vector2 PreferredSize => layout.PreferredSize;

        private Vector2 value;
        public Vector2 Value
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
            set { Value = (Vector2)value; }
        }

        public event EventHandler OnValueChanged;
        public event EventHandler OnValueCommited;

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
                },
            });
            updateWidgets();

            xTextbox.OnValueCommited += xTextbox_OnValueCommited;
            yTextbox.OnValueCommited += yTextbox_OnValueCommited;
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
            Value = new Vector2(x, value.Y);
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
            Value = new Vector2(value.X, y);
            OnValueCommited?.Invoke(this, EventArgs.Empty);
        }

        private void updateWidgets()
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
