using System;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using GW2StoryTimes.Models;
using Microsoft.Xna.Framework;

namespace GW2StoryTimes.UI
{
    public class FeedbackPrompt : Panel
    {
        private static readonly Logger Logger = Logger.GetLogger<FeedbackPrompt>();

        private readonly Mission _mission;
        private readonly TimeSpan _elapsed;
        private readonly TimeEstimate _estimate;
        private readonly SubmissionCategory _preferredCategory;
        private bool _isSubmitting;

        public FeedbackPrompt(Mission mission, TimeSpan elapsed, TimeEstimate estimate, SubmissionCategory preferredCategory)
        {
            _mission = mission;
            _elapsed = elapsed;
            _estimate = estimate;
            _preferredCategory = preferredCategory;

            Width = 420;
            Height = 180;
            ShowBorder = true;
            BackgroundColor = new Color(0, 0, 0, 200);
            Location = new Point(
                (GameService.Graphics.SpriteScreen.Width - 420) / 2,
                (GameService.Graphics.SpriteScreen.Height - 180) / 2);

            BuildLayout();
        }

        private void BuildLayout()
        {
            var elapsedFormatted = FormatTimeSpan(_elapsed);

            new Label
            {
                Text = "Mission Complete!",
                Font = GameService.Content.DefaultFont18,
                TextColor = new Color(255, 200, 50),
                ShowShadow = true,
                AutoSizeHeight = true,
                AutoSizeWidth = true,
                Location = new Point(20, 12),
                Parent = this
            };

            new Label
            {
                Text = _mission.Name,
                Font = GameService.Content.DefaultFont16,
                TextColor = Color.White,
                ShowShadow = true,
                AutoSizeHeight = true,
                AutoSizeWidth = true,
                Location = new Point(20, 38),
                Parent = this
            };

            var timeColor = Color.White;
            var comparisonText = "";

            if (_estimate?.AvgMins != null)
            {
                var diff = _elapsed.TotalMinutes - _estimate.AvgMins.Value;
                if (diff < -2)
                {
                    timeColor = new Color(100, 220, 100);
                    comparisonText = $"  ({Math.Abs(diff):F0} min faster than estimate)";
                }
                else if (diff > 2)
                {
                    timeColor = new Color(240, 80, 80);
                    comparisonText = $"  ({diff:F0} min slower than estimate)";
                }
                else
                {
                    timeColor = new Color(100, 220, 100);
                    comparisonText = "  (right on target!)";
                }
            }

            new Label
            {
                Text = $"Your time: {elapsedFormatted}{comparisonText}",
                Font = GameService.Content.DefaultFont14,
                TextColor = timeColor,
                ShowShadow = true,
                AutoSizeHeight = true,
                AutoSizeWidth = true,
                Location = new Point(20, 65),
                Parent = this
            };

            var preferFull = _preferredCategory == SubmissionCategory.Full;

            var primaryButton = new StandardButton
            {
                Text = preferFull ? "Submit as Full Experience" : "Submit as Speedrun",
                Width = 220,
                Height = 32,
                Location = new Point(20, 98),
                Parent = this
            };
            primaryButton.Click += (s, e) => Task.Run(() => Submit(preferFull ? "full" : "speed"));

            var secondaryButton = new StandardButton
            {
                Text = preferFull ? "Submit as Speedrun" : "Submit as Full Experience",
                Width = 150,
                Height = 28,
                Location = new Point(248, 100),
                Parent = this
            };
            secondaryButton.Click += (s, e) => Task.Run(() => Submit(preferFull ? "speed" : "full"));

            var dismissButton = new StandardButton
            {
                Text = "Dismiss",
                Width = 100,
                Height = 28,
                Location = new Point(300, 140),
                Parent = this
            };
            dismissButton.Click += (s, e) => Dispose();
        }

        private async Task Submit(string category)
        {
            if (_isSubmitting) return;
            _isSubmitting = true;

            var durationMins = _elapsed.TotalMinutes;
            var apiClient = GW2StoryTimesModule.Instance?.ApiClient;
            if (apiClient == null) return;

            var result = await apiClient.SubmitTimeAsync(_mission.Id, category, durationMins);

            if (result.Success)
            {
                ScreenNotification.ShowNotification(
                    $"Story Times: Time submitted for {_mission.Name}!",
                    ScreenNotification.NotificationType.Info);
            }
            else
            {
                ScreenNotification.ShowNotification(
                    $"Story Times: {result.Error}",
                    ScreenNotification.NotificationType.Warning);
            }

            Dispose();
        }

        private static string FormatTimeSpan(TimeSpan ts)
        {
            return ts.TotalHours >= 1
                ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }
}
