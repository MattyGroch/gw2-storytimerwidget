using System;
using Blish_HUD;
using Blish_HUD.Controls;
using GW2StoryTimes.Models;
using Microsoft.Xna.Framework;

namespace GW2StoryTimes.UI
{
    public class StoryTimesWidget : Panel
    {
        private static readonly Color ColorOnPace = new Color(100, 220, 100);
        private static readonly Color ColorApproaching = new Color(255, 200, 50);
        private static readonly Color ColorOvertime = new Color(240, 80, 80);

        private Label _breadcrumbLabel;
        private Label _missionNameLabel;
        private Label _estimateLabel;
        private Label _timerLabel;
        private Label _statusLabel;
        private StandardButton _toggleButton;
        private StandardButton _resetButton;
        private StandardButton _submitButton;
        private StandardButton _menuButton;
        private StandardButton _clearButton;

        private Mission _displayedMission;
        private bool _dragging;
        private Point _dragOffset;
        private bool _positioned;

        public StoryTimesWidget()
        {
            Width = 360;
            Height = 135;
            BackgroundColor = new Color(0, 0, 0, 180);
            ShowBorder = true;

            BuildLayout();
        }

        private void BuildLayout()
        {
            _menuButton = new StandardButton
            {
                Text = "...",
                Width = 30,
                Height = 22,
                Location = new Point(4, 4),
                BasicTooltipText = "Browse Missions",
                Parent = this
            };
            _menuButton.Click += (s, e) => GW2StoryTimesModule.Instance?.OpenMissionSelector();

            _clearButton = new StandardButton
            {
                Text = "X",
                Width = 26,
                Height = 22,
                Location = new Point(326, 4),
                BasicTooltipText = "Clear Mission",
                Visible = false,
                Parent = this
            };
            _clearButton.Click += (s, e) => ClearMission();

            _breadcrumbLabel = new Label
            {
                Text = "No mission selected",
                Font = GameService.Content.DefaultFont12,
                TextColor = Color.LightGray,
                AutoSizeHeight = true,
                Width = 310,
                Location = new Point(40, 6),
                Parent = this
            };
            _breadcrumbLabel.Click += OnMissionAreaClicked;

            _missionNameLabel = new Label
            {
                Text = "(click to browse)",
                Font = GameService.Content.DefaultFont16,
                TextColor = Color.White,
                ShowShadow = true,
                AutoSizeHeight = true,
                Width = 230,
                Location = new Point(40, 24),
                Parent = this
            };
            _missionNameLabel.Click += OnMissionAreaClicked;

            _estimateLabel = new Label
            {
                Text = "",
                Font = GameService.Content.DefaultFont14,
                TextColor = ColorApproaching,
                ShowShadow = true,
                AutoSizeHeight = true,
                AutoSizeWidth = true,
                HorizontalAlignment = HorizontalAlignment.Right,
                Location = new Point(270, 26),
                Parent = this
            };

            _timerLabel = new Label
            {
                Text = "00:00",
                Font = GameService.Content.DefaultFont32,
                TextColor = Color.White,
                ShowShadow = true,
                AutoSizeHeight = true,
                AutoSizeWidth = true,
                Location = new Point(8, 52),
                Parent = this
            };

            _toggleButton = new StandardButton
            {
                Text = "Start",
                Width = 60,
                Height = 26,
                Location = new Point(155, 58),
                Parent = this
            };
            _toggleButton.Click += (s, e) => GW2StoryTimesModule.Instance?.TimerService?.Toggle();

            _resetButton = new StandardButton
            {
                Text = "Reset",
                Width = 60,
                Height = 26,
                Location = new Point(220, 58),
                Parent = this
            };
            _resetButton.Click += (s, e) => GW2StoryTimesModule.Instance?.TimerService?.Reset();

            _submitButton = new StandardButton
            {
                Text = "Submit",
                Width = 65,
                Height = 26,
                Location = new Point(285, 58),
                Visible = false,
                Parent = this
            };
            _submitButton.Click += (s, e) => OnSubmitClicked();

            _statusLabel = new Label
            {
                Text = "",
                Font = GameService.Content.DefaultFont12,
                TextColor = Color.DarkGray,
                AutoSizeHeight = true,
                Width = 340,
                Location = new Point(8, 88),
                Parent = this
            };
        }

        public void UpdateMission(Mission mission)
        {
            _displayedMission = mission;
            _clearButton.Visible = mission != null;

            if (mission == null)
            {
                _breadcrumbLabel.Text = "No mission selected";
                _missionNameLabel.Text = "(click to browse)";
                _estimateLabel.Text = "";
                return;
            }

            _breadcrumbLabel.Text = mission.Breadcrumb ?? "";
            _missionNameLabel.Text = mission.Name;

            var category = GW2StoryTimesModule.Instance?.SettingPreferredCategory?.Value ?? SubmissionCategory.Full;
            var estimate = category == SubmissionCategory.Speed ? mission.Times?.Speed : mission.Times?.Full;

            _estimateLabel.Text = estimate?.FormattedEstimate != null
                ? $"~{estimate.FormattedEstimate}"
                : "";
        }

        private void ClearMission()
        {
            var module = GW2StoryTimesModule.Instance;
            if (module == null) return;

            module.ActiveMission = null;
            module.TimerService?.Reset();
        }

        public override void UpdateContainer(GameTime gameTime)
        {
            base.UpdateContainer(gameTime);

            if (!_positioned)
                ApplyInitialPosition();

            if (_dragging)
            {
                var mousePos = GameService.Input.Mouse.Position;
                Location = new Point(mousePos.X - _dragOffset.X, mousePos.Y - _dragOffset.Y);
            }

            var module = GW2StoryTimesModule.Instance;
            var timer = module?.TimerService;
            if (timer == null) return;

            if (_displayedMission != module.ActiveMission)
                UpdateMission(module.ActiveMission);

            _timerLabel.Text = timer.FormattedElapsed;
            _toggleButton.Text = timer.IsRunning ? "Pause" : "Start";

            var hasTime = !timer.IsRunning && timer.Elapsed.TotalSeconds >= 30;
            var hasMission = module.ActiveMission != null;
            _submitButton.Visible = hasTime && hasMission;

            UpdateTimerColor(timer, module);
        }

        private void ApplyInitialPosition()
        {
            var screen = GameService.Graphics.SpriteScreen;
            if (screen.Width < 100 || screen.Height < 100)
                return;

            _positioned = true;

            var module = GW2StoryTimesModule.Instance;
            var sx = module?.SettingWidgetX?.Value ?? -1;
            var sy = module?.SettingWidgetY?.Value ?? -1;

            if (sx >= 0 && sy >= 0 && sx < screen.Width && sy < screen.Height)
                Location = new Point(sx, sy);
            else
                Location = new Point((screen.Width - Width) / 2, (screen.Height - Height) / 2);
        }

        protected override void OnLeftMouseButtonPressed(Blish_HUD.Input.MouseEventArgs e)
        {
            base.OnLeftMouseButtonPressed(e);

            var relPos = GameService.Input.Mouse.Position - Location;
            if (relPos.Y < 50)
            {
                _dragging = true;
                _dragOffset = relPos;
            }
        }

        protected override void OnLeftMouseButtonReleased(Blish_HUD.Input.MouseEventArgs e)
        {
            base.OnLeftMouseButtonReleased(e);

            if (_dragging)
            {
                _dragging = false;
                SavePosition();
            }
        }

        private void SavePosition()
        {
            var module = GW2StoryTimesModule.Instance;
            if (module != null)
            {
                module.SettingWidgetX.Value = Location.X;
                module.SettingWidgetY.Value = Location.Y;
            }
        }

        private void UpdateTimerColor(Services.TimerService timer, GW2StoryTimesModule module)
        {
            var mission = module.ActiveMission;
            if (mission == null)
            {
                _timerLabel.TextColor = Color.White;
                _statusLabel.Text = timer.IsRunning ? "Timer running" : "";
                return;
            }

            if (!timer.IsRunning && timer.Elapsed.TotalSeconds < 1)
            {
                _timerLabel.TextColor = Color.White;
                _statusLabel.Text = "";
                return;
            }

            var category = module.SettingPreferredCategory?.Value ?? SubmissionCategory.Full;
            var estimate = category == SubmissionCategory.Speed ? mission.Times?.Speed : mission.Times?.Full;

            if (estimate?.AvgMins == null)
            {
                _timerLabel.TextColor = Color.White;
                _statusLabel.Text = timer.IsRunning ? "No estimate to compare" : "";
                return;
            }

            var estimateMins = estimate.AvgMins.Value;
            var elapsedMins = timer.Elapsed.TotalMinutes;
            var ratio = elapsedMins / estimateMins;

            if (ratio < 0.75)
            {
                _timerLabel.TextColor = ColorOnPace;
                _statusLabel.Text = $"On pace (est. ~{estimate.FormattedEstimate})";
            }
            else if (ratio < 1.0)
            {
                _timerLabel.TextColor = ColorApproaching;
                _statusLabel.Text = $"Approaching estimate (~{estimate.FormattedEstimate})";
            }
            else
            {
                _timerLabel.TextColor = ColorOvertime;
                var overBy = elapsedMins - estimateMins;
                _statusLabel.Text = $"Over estimate by ~{overBy:F0} min";
            }
        }

        private void OnMissionAreaClicked(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            if (_displayedMission == null)
                GW2StoryTimesModule.Instance?.OpenMissionSelector();
        }

        private void OnSubmitClicked()
        {
            var module = GW2StoryTimesModule.Instance;
            var mission = module?.ActiveMission;
            var timer = module?.TimerService;

            if (mission == null || timer == null) return;

            module.ShowFeedbackPrompt(mission, timer.Elapsed);
        }
    }
}
