using StorybrewCommon.Storyboarding;
using System;

namespace StorybrewEditor.Storyboarding
{
    public abstract class Effect
    {
        private Project project;
        public Project Project => project;

        public abstract string Name { get; set; }
        public abstract string BaseName { get; }
        public virtual string Path => null;

        public virtual EffectStatus Status { get; }
        public virtual string StatusMessage { get; }

        public event EventHandler OnChanged;
        protected void RaiseChanged()
            => OnChanged?.Invoke(this, EventArgs.Empty);

        public EffectConfig Config = new EffectConfig();
        public event EventHandler OnConfigFieldsChanged;
        protected void RaiseConfigFieldsChanged()
            => OnConfigFieldsChanged?.Invoke(this, EventArgs.Empty);

        public Effect(Project project)
        {
            this.project = project;
        }

        /// <summary>
        /// Should only be called by Project.QueueEffectUpdate(Effect).
        /// Doesn't run on the main thread.
        /// </summary>
        public abstract void Update();
        public abstract void Clear();

        // Queues an Update call
        public abstract void Refresh();

        /// <summary>
        /// Used at load time to let the effect know about placeholder layers it should use.
        /// </summary>
        public abstract void AddPlaceholder(EditorStoryboardLayer layer);
    }

    public enum EffectStatus
    {
        Initializing,
        Loading,
        Configuring,
        Updating,
        ReloadPending,
        Ready,
        CompilationFailed,
        LoadingFailed,
        ExecutionFailed,
    }
}
