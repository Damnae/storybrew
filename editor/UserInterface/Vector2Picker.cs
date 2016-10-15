using OpenTK;
using System;

namespace StorybrewEditor.UserInterface
{
    public class Vector2Picker : Widget, Field
    {
        private LinearLayout layout;
        private Textbox xTextbox;
        private Textbox yTextbox;

        public override Vector2 MinSize => new Vector2(layout.MinSize.X, layout.MinSize.Y);
        public override Vector2 MaxSize => Vector2.Zero;
        public override Vector2 PreferredSize => new Vector2(layout.PreferredSize.X, layout.PreferredSize.Y);

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
                x = float.Parse(xCommit);
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
                y = float.Parse(yCommit);
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
            xTextbox.SetValueSilent(value.X.ToString());
            yTextbox.SetValueSilent(value.Y.ToString());
        }

        protected override void Layout()
        {
            base.Layout();
            layout.Size = Size;
        }
    }
}
