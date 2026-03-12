using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using GW2StoryTimes.Models;
using GW2StoryTimes.Services;
using Microsoft.Xna.Framework;

namespace GW2StoryTimes.UI
{
    public class StoryTimesWindow : StandardWindow
    {
        private static readonly Logger Logger = Logger.GetLogger<StoryTimesWindow>();

        private readonly StoryTimesApiClient _apiClient;
        private BrowsePanel _browsePanel;

        public StoryTimesWindow(AsyncTexture2D backgroundTexture, StoryTimesApiClient apiClient)
            : base(
                backgroundTexture,
                new Rectangle(25, 26, 560, 640),
                new Rectangle(40, 50, 540, 590))
        {
            _apiClient = apiClient;

            Title = "Story Times";
            Subtitle = "Browse Missions";
            Id = "GW2StoryTimes_SelectorWindow_v2";
            SavesPosition = true;
            Location = new Point(300, 300);

            BuildContents();
        }

        private void BuildContents()
        {
            _browsePanel = new BrowsePanel(_apiClient)
            {
                Width = 520,
                HeightSizingMode = SizingMode.Fill,
                Location = new Point(0, 0),
                Parent = this
            };

            _browsePanel.MissionSelected += OnBrowseMissionSelected;
        }

        private void OnBrowseMissionSelected(object sender, Mission mission)
        {
            if (GW2StoryTimesModule.Instance != null)
            {
                GW2StoryTimesModule.Instance.ActiveMission = mission;
                GW2StoryTimesModule.Instance.TimerService?.Reset();
            }

            Hide();
        }

        protected override void DisposeControl()
        {
            if (_browsePanel != null)
                _browsePanel.MissionSelected -= OnBrowseMissionSelected;

            _browsePanel?.Dispose();
            base.DisposeControl();
        }
    }
}
