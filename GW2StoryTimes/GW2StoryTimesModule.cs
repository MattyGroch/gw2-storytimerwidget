using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
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
        private NextMissionPrompt _nextMissionPrompt;

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
            _nextMissionPrompt?.Dispose();
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
            if (SettingShowFeedbackPrompt?.Value == false)
            {
                var cat = SettingPreferredCategory?.Value ?? SubmissionCategory.Full;
                var catStr = cat == SubmissionCategory.Speed ? "speed" : "full";
                Task.Run(() => SubmitDirectly(mission, elapsed, catStr));
                return;
            }

            _feedbackPrompt?.Dispose();

            var category = SettingPreferredCategory?.Value ?? SubmissionCategory.Full;
            var estimate = category == SubmissionCategory.Speed ? mission.Times?.Speed : mission.Times?.Full;

            _feedbackPrompt = new FeedbackPrompt(mission, elapsed, estimate, category)
            {
                Parent = GameService.Graphics.SpriteScreen
            };
            _feedbackPrompt.Disposed += (s, e) => _widget?.ReenableSubmit();
        }

        private async Task SubmitDirectly(Mission mission, TimeSpan elapsed, string category)
        {
            var apiClient = ApiClient;
            if (apiClient == null)
            {
                _widget?.ReenableSubmit();
                return;
            }

            var result = await apiClient.SubmitTimeAsync(mission.Id, category, elapsed.TotalMinutes);

            if (result.Success)
            {
                ScreenNotification.ShowNotification(
                    $"Story Times: Time submitted for {mission.Name}!",
                    ScreenNotification.NotificationType.Info);
                OnSubmissionCompleted(mission);
            }
            else
            {
                ScreenNotification.ShowNotification(
                    $"Story Times: {result.Error}",
                    ScreenNotification.NotificationType.Warning);
                _widget?.ReenableSubmit();
            }
        }

        internal void OnSubmissionCompleted(Mission submittedMission)
        {
            ActiveMission = null;
            TimerService?.Reset();
            _widget?.ReenableSubmit();

            Task.Run(async () =>
            {
                try
                {
                    var nextMission = await FindNextMissionAsync(submittedMission);
                    if (nextMission != null)
                        ShowNextMissionPrompt(nextMission);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Failed to find next mission: {ex.Message}");
                }
            });
        }

        private async Task<Mission> FindNextMissionAsync(Mission current)
        {
            if (current == null || string.IsNullOrEmpty(current.SeasonId)) return null;

            var season = await ApiClient.GetSeasonAsync(current.SeasonId);
            if (season?.Stories == null) return null;

            var playerRace = GetPlayerRace();
            var allMissions = new List<(Mission mission, string storyName)>();

            foreach (var story in season.Stories.OrderBy(s => s.Order))
            {
                if (story.Races != null && story.Races.Count > 0 && playerRace != null)
                {
                    if (!story.Races.Contains(playerRace))
                        continue;
                }

                if (story.Missions == null) continue;

                foreach (var mission in story.Missions.OrderBy(m => m.Order))
                {
                    allMissions.Add((mission, story.Name));
                }
            }

            for (int i = 0; i < allMissions.Count - 1; i++)
            {
                if (allMissions[i].mission.Id == current.Id)
                {
                    var next = allMissions[i + 1].mission;
                    if (string.IsNullOrEmpty(next.SeasonName))
                        next.SeasonName = season.Name;
                    if (string.IsNullOrEmpty(next.StoryName))
                        next.StoryName = allMissions[i + 1].storyName;
                    if (string.IsNullOrEmpty(next.SeasonId))
                        next.SeasonId = season.Id;
                    return next;
                }
            }

            return null;
        }

        private void ShowNextMissionPrompt(Mission nextMission)
        {
            _nextMissionPrompt?.Dispose();
            _nextMissionPrompt = new NextMissionPrompt(nextMission)
            {
                Parent = GameService.Graphics.SpriteScreen
            };
        }

        private static string GetPlayerRace()
        {
            try
            {
                return GameService.Gw2Mumble.PlayerCharacter.Race.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
