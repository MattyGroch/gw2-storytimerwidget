using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using GW2StoryTimes.Models;
using GW2StoryTimes.Services;
using GW2StoryTimes.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GW2StoryTimes
{
    [Export(typeof(Module))]
    public class GW2StoryTimesModule : Module
    {
        private static readonly Logger Logger = Logger.GetLogger<GW2StoryTimesModule>();

        internal static GW2StoryTimesModule Instance { get; private set; }

        internal SettingsManager SettingsManager => ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => ModuleParameters.DirectoriesManager;

        internal StoryTimesApiClient ApiClient { get; private set; }
        internal TimerService TimerService { get; private set; }

        private CornerIcon _cornerIcon;
        private StoryTimesWidget _widget;
        private StoryTimesWindow _selectorWindow;
        private FeedbackPrompt _feedbackPrompt;

        internal Mission ActiveMission { get; set; }

        internal SettingEntry<bool> SettingShowFeedbackPrompt { get; private set; }
        internal SettingEntry<SubmissionCategory> SettingPreferredCategory { get; private set; }
        internal SettingEntry<KeyBinding> SettingToggleWidgetHotkey { get; private set; }
        internal SettingEntry<int> SettingWidgetX { get; private set; }
        internal SettingEntry<int> SettingWidgetY { get; private set; }

        [ImportingConstructor]
        public GW2StoryTimesModule([Import("ModuleParameters")] ModuleParameters moduleParameters)
            : base(moduleParameters)
        {
            Instance = this;
        }

        protected override void DefineSettings(SettingCollection settings)
        {
            SettingShowFeedbackPrompt = settings.DefineSetting(
                "ShowFeedbackPrompt",
                true,
                () => "Show Feedback Prompt",
                () => "Show a prompt to submit your completion time after finishing a mission.");

            SettingPreferredCategory = settings.DefineSetting(
                "PreferredCategory_v2",
                SubmissionCategory.Full,
                () => "Default Submission Type",
                () => "Which time estimate to display and which category to default when submitting.");

            SettingToggleWidgetHotkey = settings.DefineSetting(
                "ToggleWidgetHotkey",
                new KeyBinding(Keys.None),
                () => "Toggle Widget Hotkey",
                () => "Keybind to show or hide the Story Times widget overlay.");

            var internal_ = settings.AddSubCollection("internal", false);
            SettingWidgetX = internal_.DefineSetting("WidgetPositionX", -1);
            SettingWidgetY = internal_.DefineSetting("WidgetPositionY", -1);
        }

        protected override async Task LoadAsync()
        {
            ApiClient = new StoryTimesApiClient();
            TimerService = new TimerService();

            SettingToggleWidgetHotkey.Value.Enabled = true;
            SettingToggleWidgetHotkey.Value.Activated += OnToggleWidgetHotkeyActivated;

            await ApiClient.PreloadSeasonsAsync();

            CreateCornerIcon();
            CreateWidget();
            CreateSelectorWindow();
        }

        protected override void Update(GameTime gameTime)
        {
        }

        protected override void Unload()
        {
            if (SettingToggleWidgetHotkey?.Value != null)
                SettingToggleWidgetHotkey.Value.Activated -= OnToggleWidgetHotkeyActivated;

            _cornerIcon?.Dispose();
            _widget?.Dispose();
            _selectorWindow?.Dispose();
            _feedbackPrompt?.Dispose();
            ApiClient?.Dispose();
            TimerService?.Dispose();

            Instance = null;
        }

        private void CreateCornerIcon()
        {
            var cornerIconTexture = AsyncTexture2D.FromAssetId(440023);

            _cornerIcon = new CornerIcon
            {
                IconName = "Story Times",
                Icon = cornerIconTexture,
                BasicTooltipText = "Story Times — Toggle Widget",
                Priority = 748291035,
                Parent = GameService.Graphics.SpriteScreen
            };

            _cornerIcon.Click += (s, e) => ToggleWidget();
        }

        private void CreateWidget()
        {
            _widget = new StoryTimesWidget
            {
                Parent = GameService.Graphics.SpriteScreen,
                Visible = false
            };
        }

        private void CreateSelectorWindow()
        {
            var windowBackgroundTexture = AsyncTexture2D.FromAssetId(155997);

            _selectorWindow = new StoryTimesWindow(windowBackgroundTexture, ApiClient)
            {
                Parent = GameService.Graphics.SpriteScreen
            };
        }

        private void ToggleWidget()
        {
            if (_widget != null)
                _widget.Visible = !_widget.Visible;
        }

        private void OnToggleWidgetHotkeyActivated(object sender, EventArgs e)
        {
            ToggleWidget();
        }

        internal void OpenMissionSelector()
        {
            _selectorWindow?.ToggleWindow();
        }

        internal void ShowFeedbackPrompt(Mission mission, TimeSpan elapsed)
        {
            _feedbackPrompt?.Dispose();

            var category = SettingPreferredCategory?.Value ?? SubmissionCategory.Full;
            var estimate = category == SubmissionCategory.Speed ? mission.Times?.Speed : mission.Times?.Full;

            _feedbackPrompt = new FeedbackPrompt(mission, elapsed, estimate)
            {
                Parent = GameService.Graphics.SpriteScreen
            };
        }
    }
}
