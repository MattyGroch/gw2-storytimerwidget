using Blish_HUD;
using Blish_HUD.Controls;
using GW2StoryTimes.Models;
using Microsoft.Xna.Framework;

namespace GW2StoryTimes.UI
{
    public class NextMissionPrompt : Panel
    {
        private readonly Mission _nextMission;

        public NextMissionPrompt(Mission nextMission)
        {
            _nextMission = nextMission;

            Width = 420;
            Height = 130;
            ShowBorder = true;
            BackgroundColor = new Color(0, 0, 0, 200);
            Location = new Point(
                (GameService.Graphics.SpriteScreen.Width - 420) / 2,
                (GameService.Graphics.SpriteScreen.Height - 130) / 2);

            BuildLayout();
        }

        private void BuildLayout()
        {
            new Label
            {
                Text = "Continue to the next mission?",
                Font = GameService.Content.DefaultFont16,
                TextColor = new Color(255, 200, 50),
                ShowShadow = true,
                AutoSizeHeight = true,
                Width = 380,
                Location = new Point(20, 12),
                Parent = this
            };

            new Label
            {
                Text = _nextMission.Breadcrumb ?? "",
                Font = GameService.Content.DefaultFont12,
                TextColor = Color.LightGray,
                AutoSizeHeight = true,
                Width = 380,
                Location = new Point(20, 38),
                Parent = this
            };

            new Label
            {
                Text = _nextMission.Name,
                Font = GameService.Content.DefaultFont14,
                TextColor = Color.White,
                ShowShadow = true,
                AutoSizeHeight = true,
                Width = 380,
                Location = new Point(20, 56),
                Parent = this
            };

            var yesButton = new StandardButton
            {
                Text = "Start Next Mission",
                Width = 160,
                Height = 30,
                Location = new Point(20, 88),
                Parent = this
            };
            yesButton.Click += (s, e) =>
            {
                var module = GW2StoryTimesModule.Instance;
                if (module != null)
                {
                    module.ActiveMission = _nextMission;
                    module.TimerService?.Reset();
                }
                Dispose();
            };

            var noButton = new StandardButton
            {
                Text = "No Thanks",
                Width = 100,
                Height = 28,
                Location = new Point(300, 90),
                Parent = this
            };
            noButton.Click += (s, e) => Dispose();
        }
    }
}
