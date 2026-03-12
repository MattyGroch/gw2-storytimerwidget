using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using GW2StoryTimes.Models;
using GW2StoryTimes.Services;
using Microsoft.Xna.Framework;

namespace GW2StoryTimes.UI
{
    public class BrowsePanel : Panel
    {
        private static readonly Logger Logger = Logger.GetLogger<BrowsePanel>();

        private readonly StoryTimesApiClient _apiClient;

        private FlowPanel _seasonList;
        private FlowPanel _missionList;
        private TextBox _searchBox;
        private Label _detailHeader;
        private Label _filterHint;
        private string _selectedSeasonId;

        private readonly List<RenderedRow> _renderedRows = new List<RenderedRow>();

        public event EventHandler<Mission> MissionSelected;

        public BrowsePanel(StoryTimesApiClient apiClient)
        {
            _apiClient = apiClient;
            ShowBorder = false;

            BuildLayout();
        }

        private void BuildLayout()
        {
            _seasonList = new FlowPanel
            {
                Title = "Seasons",
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                Width = 180,
                HeightSizingMode = SizingMode.Fill,
                CanScroll = true,
                ShowBorder = true,
                ControlPadding = new Vector2(0, 2),
                Location = new Point(0, 0),
                Parent = this
            };

            var rightPanel = new Panel
            {
                Width = 330,
                HeightSizingMode = SizingMode.Fill,
                Location = new Point(190, 0),
                ShowBorder = true,
                Parent = this
            };

            _searchBox = new TextBox
            {
                PlaceholderText = "Search missions...",
                Width = 310,
                Height = 26,
                Location = new Point(10, 6),
                Parent = rightPanel
            };
            _searchBox.TextChanged += (s, e) => ApplyFilter();

            _detailHeader = new Label
            {
                Text = "Select a season",
                Font = GameService.Content.DefaultFont16,
                TextColor = Color.LightGray,
                AutoSizeHeight = true,
                Width = 310,
                WrapText = true,
                Location = new Point(10, 38),
                Parent = rightPanel
            };

            _filterHint = new Label
            {
                Text = "",
                Font = GameService.Content.DefaultFont12,
                TextColor = Color.DarkGray,
                AutoSizeHeight = true,
                Width = 310,
                Location = new Point(10, 58),
                Visible = false,
                Parent = rightPanel
            };

            _missionList = new FlowPanel
            {
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                Width = 320,
                HeightSizingMode = SizingMode.Fill,
                CanScroll = true,
                ControlPadding = new Vector2(0, 1),
                Location = new Point(5, 65),
                Parent = rightPanel
            };

            PopulateSeasonList();
        }

        private void PopulateSeasonList()
        {
            var seasons = _apiClient.GetCachedSeasons()
                                    .OrderBy(s => s.Order)
                                    .ToList();

            foreach (var season in seasons)
            {
                var totalFormatted = FormatMins(season.TotalFullMins);

                var row = new Panel
                {
                    Width = 170,
                    Height = 40,
                    Parent = _seasonList
                };

                new Label
                {
                    Text = season.Name,
                    Font = GameService.Content.DefaultFont14,
                    TextColor = Color.White,
                    AutoSizeHeight = true,
                    Width = 160,
                    Location = new Point(5, 2),
                    Parent = row
                };

                new Label
                {
                    Text = $"{season.MissionCount} missions — {totalFormatted}",
                    Font = GameService.Content.DefaultFont12,
                    TextColor = Color.DarkGray,
                    AutoSizeHeight = true,
                    Width = 160,
                    Location = new Point(5, 20),
                    Parent = row
                };

                var capturedSeason = season;
                row.Click += (s, e) =>
                {
                    _selectedSeasonId = capturedSeason.Id;
                    _searchBox.Text = "";
                    Task.Run(() => LoadSeasonDetail(capturedSeason));
                };
            }
        }

        private async Task LoadSeasonDetail(Season seasonSummary)
        {
            _detailHeader.Text = $"Loading {seasonSummary.Name}...";
            _filterHint.Visible = false;
            ClearMissionList();

            var season = await _apiClient.GetSeasonAsync(seasonSummary.Id);
            if (season?.Stories == null)
            {
                _detailHeader.Text = "Failed to load season data.";
                return;
            }

            if (_selectedSeasonId != seasonSummary.Id)
                return;

            _detailHeader.Text = season.Name;

            var playerRace = GetPlayerRace();
            var hasRaceBranching = season.Stories.Any(s => s.Races != null && s.Races.Count > 0);
            var isFiltering = hasRaceBranching && playerRace != null;

            if (isFiltering)
            {
                _filterHint.Text = $"Showing quests for {playerRace}";
                _filterHint.Visible = true;
            }
            else
            {
                _filterHint.Visible = false;
            }

            foreach (var story in season.Stories.OrderBy(s => s.Order))
            {
                if (isFiltering && story.Races != null && story.Races.Count > 0)
                {
                    if (!story.Races.Contains(playerRace))
                        continue;
                }

                var storyText = story.GroupName != null ? $"{story.Name} ({story.GroupName})" : story.Name;
                var storyLabel = new Label
                {
                    Text = storyText,
                    Font = GameService.Content.DefaultFont14,
                    TextColor = new Color(200, 180, 120),
                    AutoSizeHeight = true,
                    Width = 310,
                    Location = new Point(0, 0),
                    Parent = _missionList
                };

                var storyRows = new List<RenderedRow>();
                _renderedRows.Add(new RenderedRow(storyLabel, storyText, null, storyRows));

                if (story.Missions == null) continue;

                var dedupedMissions = DeduplicateMissions(story.Missions.OrderBy(m => m.Order));
                foreach (var mission in dedupedMissions)
                {
                    var estimate = mission.Times?.Full;
                    var timeText = estimate?.FormattedEstimate ?? "—";
                    var submissionHint = estimate != null && estimate.HasCommunityData
                        ? $" ({estimate.Submissions} reports)"
                        : "";

                    var missionRow = new Panel
                    {
                        Width = 310,
                        Height = 28,
                        Parent = _missionList
                    };

                    new Label
                    {
                        Text = $"  {mission.Name}",
                        Font = GameService.Content.DefaultFont12,
                        TextColor = Color.White,
                        AutoSizeHeight = true,
                        Width = 200,
                        Location = new Point(0, 4),
                        Parent = missionRow
                    };

                    new Label
                    {
                        Text = $"{timeText}{submissionHint}",
                        Font = GameService.Content.DefaultFont12,
                        TextColor = new Color(255, 200, 50),
                        AutoSizeHeight = true,
                        AutoSizeWidth = true,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Location = new Point(200, 4),
                        Parent = missionRow
                    };

                    var capturedMission = mission;
                    missionRow.Click += (s, e) => MissionSelected?.Invoke(this, capturedMission);

                    var missionRowEntry = new RenderedRow(missionRow, mission.Name, storyText, null);
                    _renderedRows.Add(missionRowEntry);
                    storyRows.Add(missionRowEntry);
                }
            }

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var query = _searchBox?.Text?.Trim() ?? "";
            var hasQuery = query.Length > 0;

            foreach (var row in _renderedRows)
            {
                if (row.ChildMissionRows != null)
                {
                    // Story header: visible if any child mission matches (or no filter)
                    if (!hasQuery)
                    {
                        row.Control.Visible = true;
                    }
                    else
                    {
                        var anyChildMatch = row.ChildMissionRows.Any(
                            m => m.MissionName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
                        var headerMatch = row.MissionName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
                        row.Control.Visible = anyChildMatch || headerMatch;
                    }
                }
                else
                {
                    // Mission row
                    if (!hasQuery)
                    {
                        row.Control.Visible = true;
                    }
                    else
                    {
                        var nameMatch = row.MissionName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
                        var storyMatch = row.StoryName != null &&
                                         row.StoryName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
                        row.Control.Visible = nameMatch || storyMatch;
                    }
                }
            }
        }

        private static string GetPlayerRace()
        {
            try
            {
                var race = GameService.Gw2Mumble.PlayerCharacter.Race;
                return race.ToString();
            }
            catch
            {
                return null;
            }
        }

        private void ClearMissionList()
        {
            _renderedRows.Clear();
            _missionList.ClearChildren();
        }

        private static List<Mission> DeduplicateMissions(IEnumerable<Mission> missions)
        {
            var seen = new Dictionary<string, Mission>();
            foreach (var m in missions)
            {
                if (!seen.ContainsKey(m.Name))
                {
                    seen[m.Name] = m;
                }
            }
            return seen.Values.ToList();
        }

        private static string FormatMins(double mins)
        {
            if (mins <= 0) return "—";
            var hours = (int)(mins / 60);
            var remaining = (int)(mins % 60);
            return hours > 0 ? $"{hours}h {remaining}m" : $"{remaining}m";
        }

        protected override void DisposeControl()
        {
            _seasonList?.Dispose();
            _missionList?.Dispose();
            base.DisposeControl();
        }

        private class RenderedRow
        {
            public Control Control { get; }
            public string MissionName { get; }
            public string StoryName { get; }
            public List<RenderedRow> ChildMissionRows { get; }

            public RenderedRow(Control control, string missionName, string storyName, List<RenderedRow> childMissionRows)
            {
                Control = control;
                MissionName = missionName;
                StoryName = storyName;
                ChildMissionRows = childMissionRows;
            }
        }
    }
}
