using DynastyManager.Core.Models;
using DynastyManager.Core.Seasons;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;
using DynastyManager.Data.Persistence;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Storage;

namespace DynastyManager.App;

public sealed class FoundationPage : ContentPage
{
    private static readonly Color PositiveChangeColor = Color.FromArgb("#22C55E");
    private static readonly Color NegativeChangeColor = Color.FromArgb("#EF4444");
    private static readonly Color NeutralChangeColor = Color.FromArgb("#94A3B8");
    private enum AppSection
    {
        Home,
        Schedule,
        Roster,
        Recruiting,
        Rankings,
        Program,
        More
    }

    private enum RecruitingView
    {
        Board,
        HighSchool,
        TransferPortal,
        Scouting,
        Commitments
    }

    private enum RecruitingSort
    {
        BestAvailable,
        Interest,
        Position,
        Name
    }

    private enum ScoutedFilter
    {
        All,
        Scouted,
        Unscouted,
        Partial,
        Mostly,
        Fully
    }

    private sealed record RecruitingProspectDisplay(
        RecruitingSource Source,
        Guid ProspectId,
        string FullName,
        Position Position,
        string Subtitle,
        int ClassYear,
        int? Potential,
        int? StarRating,
        int OverallRating);
    private readonly Label _importStatus;
    private readonly Entry _dynastyNameEntry;
    private readonly Picker _teamPicker;
    private readonly Button _newDynastyButton;
    private readonly Label _currentDynastyLabel;
    private readonly Button _advanceWeekButton;
    private readonly Button _manualSaveButton;
    private readonly Button _autosaveButton;
    private readonly Switch _rollingAutosaveSwitch;
    private readonly Switch _recruitingAssistanceSwitch;
    private readonly Label _rollingAutosaveStatus;
    private readonly Label _offseasonRosterStatus;
    private readonly VerticalStackLayout _transferPortalList;
    private readonly Label _nationalRankingStatus;
    private readonly VerticalStackLayout _nationalRankingsList;
    private readonly Label _postseasonStatus;
    private readonly VerticalStackLayout _postseasonList;
    private readonly Label _conferenceChampionshipStatus;
    private readonly VerticalStackLayout _conferenceStandingsList;
    private readonly VerticalStackLayout _scheduleList;
    private readonly VerticalStackLayout _saveList;
    private readonly ContentView _sectionHost;
    private readonly Label _screenTitle;
    private readonly Label _homeStatus;
    private readonly VerticalStackLayout _homeHighlights;
    private readonly Label _recruitingStatus;
    private readonly HorizontalStackLayout _recruitingMetrics;
    private readonly HorizontalStackLayout _recruitingTabs;
    private readonly Dictionary<RecruitingView, Button> _recruitingTabButtons = new();
    private readonly SearchBar _recruitingSearch;
    private readonly Picker _recruitingPositionFilter;
    private readonly Picker _recruitingSortPicker;
    private readonly Picker _recruitingStarFilter;
    private readonly Picker _recruitingStateFilter;
    private readonly Picker _recruitingRegionFilter;
    private readonly Picker _recruitingScoutedFilter;
    private ScrollView _recruitingFilterScroller = null!;
    private readonly Label _recruitingResultsStatus;
    private readonly ContentView _recruitingDetailHost;
    private readonly VerticalStackLayout _recruitingList;
    private readonly Grid _advanceOverlay;
    private readonly Label _advanceOverlayStatus;
    private readonly ProgressBar _advanceOverlayProgress;
    private readonly Label _programStatus;
    private readonly VerticalStackLayout _programList;
    private readonly Dictionary<AppSection, Button> _navButtons = new();
    private readonly Dictionary<AppSection, View> _sectionViews = new();
    private AppSection _activeSection = AppSection.Home;
    private RecruitingView _recruitingView = RecruitingView.HighSchool;
    private RecruitingSource? _selectedRecruitSource;
    private Guid? _selectedRecruitId;

    private SqliteDynastySaveRepository? _saveRepository;
    private DynastyState? _currentDynasty;
    private IReadOnlyDictionary<string, Team> _teamsByName = new Dictionary<string, Team>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<string, TeamSimulationProfile> _simulationProfiles =
        new Dictionary<string, TeamSimulationProfile>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<ImportedPlayerRow> _legacyRosterRows =
        Array.Empty<ImportedPlayerRow>();
    private IReadOnlyList<string> _teamNames = Array.Empty<string>();
    private bool _loaded;
    private bool _isAdvancing;

    public FoundationPage()
    {
        Title = "Dynasty Manager";

        _importStatus = new Label
        {
            Text = "Loading legacy universe…",
            FontSize = 14
        };

        _dynastyNameEntry = new Entry
        {
            Placeholder = "Dynasty name",
            Text = "My Dynasty"
        };

        _teamPicker = new Picker
        {
            Title = "Choose your team"
        };

        _newDynastyButton = new Button
        {
            Text = "Create New Dynasty",
            TextColor = Colors.White
        };
        _newDynastyButton.Clicked += CreateNewDynasty;

        _currentDynastyLabel = new Label
        {
            Text = "No dynasty loaded.",
            FontSize = 17
        };

        _advanceWeekButton = new Button
        {
            Text = "Advance Week",
            TextColor = Colors.White,
            IsEnabled = false
        };
        _advanceWeekButton.Clicked += AdvanceWeek;

        _manualSaveButton = new Button
        {
            Text = "Manual Save",
            TextColor = Colors.White,
            IsEnabled = false
        };
        _manualSaveButton.Clicked += async (_, _) => await SaveCurrentAsync(SaveKind.Manual);

        _autosaveButton = new Button
        {
            Text = "Autosave Current",
            TextColor = Colors.White,
            IsEnabled = false
        };
        _autosaveButton.Clicked += async (_, _) => await SaveCurrentAsync(SaveKind.AutosaveCurrent);

        _rollingAutosaveSwitch = new Switch
        {
            IsToggled = Preferences.Default.Get("rolling_autosave_enabled", true)
        };
        _rollingAutosaveSwitch.Toggled += (_, args) =>
            Preferences.Default.Set("rolling_autosave_enabled", args.Value);

        _recruitingAssistanceSwitch = new Switch
        {
            IsToggled = true
        };
        _recruitingAssistanceSwitch.Toggled += (_, args) =>
        {
            if (_currentDynasty is null)
                return;

            _currentDynasty = _currentDynasty with
            {
                RecruitingAssistanceEnabled = args.Value
            };
        };

        _rollingAutosaveStatus = new Label
        {
            Text = "Keeps the latest three weekly checkpoints automatically.",
            FontSize = 13
        };

        _offseasonRosterStatus = new Label
        {
            Text = "Roster data will appear after a dynasty is created.",
            FontSize = 13
        };

        _transferPortalList = new VerticalStackLayout
        {
            Spacing = 3
        };

        _nationalRankingStatus = new Label
        {
            Text = "National rankings will appear after a dynasty is created.",
            FontSize = 13
        };

        _nationalRankingsList = new VerticalStackLayout
        {
            Spacing = 3
        };

        _postseasonStatus = new Label
        {
            Text = "The 12-team CFP field is selected after conference championships.",
            FontSize = 13
        };

        _postseasonList = new VerticalStackLayout
        {
            Spacing = 4
        };

        _conferenceChampionshipStatus = new Label
        {
            Text = "Conference standings will appear after a dynasty is created.",
            FontSize = 13
        };

        _conferenceStandingsList = new VerticalStackLayout
        {
            Spacing = 4
        };

        _scheduleList = new VerticalStackLayout
        {
            Spacing = 6
        };

        _saveList = new VerticalStackLayout
        {
            Spacing = 10
        };

        _sectionHost = new ContentView();

        _screenTitle = new Label
        {
            Text = "Home",
            FontSize = 23,
            FontAttributes = FontAttributes.Bold
        };

        _homeStatus = new Label
        {
            Text = "Create or load a dynasty to begin.",
            FontSize = 14
        };

        _homeHighlights = new VerticalStackLayout
        {
            Spacing = 8
        };

        _recruitingStatus = new Label
        {
            Text = "Recruiting information will appear after a dynasty is created.",
            FontSize = 12
        };

        _recruitingMetrics = new HorizontalStackLayout
        {
            Spacing = 8
        };

        _recruitingTabs = new HorizontalStackLayout
        {
            Spacing = 6
        };

        foreach (var view in Enum.GetValues<RecruitingView>())
        {
            var capturedView = view;
            var button = new Button
            {
                Text = view switch
                {
                    RecruitingView.Board => "Target Board",
                    RecruitingView.HighSchool => "High School",
                    RecruitingView.TransferPortal => "Portal",
                    RecruitingView.Scouting => "Scouting",
                    RecruitingView.Commitments => "Commits",
                    _ => view.ToString()
                },
                FontSize = 12,
                Padding = new Thickness(12, 7),
                TextColor = Colors.White
            };

            button.Clicked += (_, _) =>
            {
                _recruitingView = capturedView;
                _selectedRecruitSource = null;
                _selectedRecruitId = null;
                RenderRecruitingScreen();
            };

            _recruitingTabButtons[view] = button;
            _recruitingTabs.Children.Add(button);
        }

        _recruitingSearch = new SearchBar
        {
            Placeholder = "Search recruits by name or school",
            FontSize = 13
        };
        _recruitingSearch.TextChanged += (_, _) =>
            RenderRecruitingScreen();

        _recruitingPositionFilter = new Picker
        {
            Title = "Position",
            ItemsSource = new[]
            {
                "All Positions"
            }.Concat(
                Enum.GetValues<Position>()
                    .Select(position => position.ToString()))
             .ToList(),
            SelectedIndex = 0
        };
        _recruitingPositionFilter.SelectedIndexChanged += (_, _) =>
            RenderRecruitingScreen();

        _recruitingSortPicker = new Picker
        {
            Title = "Sort",
            ItemsSource = new[]
            {
                "Best Available",
                "Interest",
                "Position",
                "Name"
            }.ToList(),
            SelectedIndex = 0
        };
        _recruitingSortPicker.SelectedIndexChanged += (_, _) =>
            RenderRecruitingScreen();

        _recruitingStarFilter = new Picker
        {
            Title = "Stars",
            ItemsSource = new[]
            {
                "All Stars",
                "5 Star",
                "4 Star",
                "3 Star",
                "2 Star",
                "1 Star"
            }.ToList(),
            SelectedIndex = 0
        };
        _recruitingStarFilter.SelectedIndexChanged += (_, _) =>
            RenderRecruitingScreen();

        _recruitingStateFilter = new Picker
        {
            Title = "State",
            ItemsSource = new[] { "All States" }
                .Concat(RecruitGeography.StateNames).ToList(),
            SelectedIndex = 0
        };
        _recruitingStateFilter.SelectedIndexChanged += (_, _) =>
            RenderRecruitingScreen();

        _recruitingRegionFilter = new Picker
        {
            Title = "Region",
            ItemsSource = new[] { "All Regions" }
                .Concat(RecruitGeography.RegionNames).ToList(),
            SelectedIndex = 0
        };
        _recruitingRegionFilter.SelectedIndexChanged += (_, _) =>
            RenderRecruitingScreen();

        _recruitingScoutedFilter = new Picker
        {
            Title = "Scouted",
            ItemsSource = new[]
            {
                "All Scouting",
                "Scouted (Any Level)",
                "Unscouted",
                "Partially Scouted",
                "Mostly Scouted",
                "Fully Scouted"
            }.ToList(),
            SelectedIndex = 0
        };
        _recruitingScoutedFilter.SelectedIndexChanged += (_, _) =>
            RenderRecruitingScreen();

        _recruitingResultsStatus = new Label
        {
            FontSize = 12
        };

        _recruitingDetailHost = new ContentView
        {
            IsVisible = false
        };

        _recruitingList = new VerticalStackLayout
        {
            Spacing = 4
        };

        _advanceOverlayStatus = new Label
        {
            Text = "Preparing weekly advance...",
            TextColor = Colors.White,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        };
        _advanceOverlayProgress = new ProgressBar
        {
            Progress = 0,
            ProgressColor = Colors.White
        };
        _advanceOverlay = new Grid
        {
            IsVisible = false,
            BackgroundColor = Color.FromArgb("#D910243E"),
            Padding = new Thickness(32),
            Children =
            {
                new VerticalStackLayout
                {
                    Spacing = 14,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        _advanceOverlayStatus,
                        _advanceOverlayProgress
                    }
                }
            }
        };

        _programStatus = new Label
        {
            Text = "Program information will appear after a dynasty is created.",
            FontSize = 14
        };

        _programList = new VerticalStackLayout
        {
            Spacing = 8
        };

        var navigation = new HorizontalStackLayout
        {
            Spacing = 6,
            Padding = new Thickness(8, 6)
        };

        foreach (var section in Enum.GetValues<AppSection>())
        {
            var captured = section;
            var button = new Button
            {
                Text = section switch
                {
                    AppSection.Recruiting => "Recruit",
                    AppSection.Rankings => "Ranks",
                    _ => section.ToString()
                },
                FontSize = 11,
                Padding = new Thickness(11, 8),
                TextColor = Colors.White
            };

            button.Clicked += (_, _) => ShowSection(captured);
            _navButtons[section] = button;
            navigation.Children.Add(button);
        }

        var navigationBar = new Border
        {
            StrokeThickness = 1,
            Padding = 0,
            Content = new ScrollView
            {
                Orientation = ScrollOrientation.Horizontal,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
                Content = navigation
            }
        };

        var pages = new Grid();

        foreach (var section in Enum.GetValues<AppSection>())
        {
            View sectionContent = section switch
            {
                AppSection.Home => BuildHomeSection(),
                AppSection.Schedule => BuildScheduleSection(),
                AppSection.Roster => BuildRosterSection(),
                AppSection.Recruiting => BuildRecruitingSection(),
                AppSection.Rankings => BuildRankingsSection(),
                AppSection.Program => BuildProgramSection(),
                AppSection.More => BuildMoreSection(),
                _ => BuildHomeSection()
            };

            View page;

            if (section == AppSection.Recruiting)
            {
                sectionContent.Margin =
                    new Thickness(16, 6, 16, 10);
                sectionContent.IsVisible = false;
                page = sectionContent;
            }
            else
            {
                page = new ScrollView
                {
                    IsVisible = false,
                    Content = new VerticalStackLayout
                    {
                        Padding = new Thickness(16, 10, 16, 24),
                        Spacing = 14,
                        Children =
                        {
                            sectionContent
                        }
                    }
                };
            }

            _sectionViews[section] = page;
            pages.Children.Add(page);
        }

        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            }
        };

        root.Add(_screenTitle);
        Grid.SetRow(_screenTitle, 0);
        _screenTitle.Margin = new Thickness(16, 12, 16, 4);

        root.Add(pages);
        Grid.SetRow(pages, 1);

        root.Add(navigationBar);
        Grid.SetRow(navigationBar, 2);

        var shell = new Grid();
        shell.Children.Add(root);
        shell.Children.Add(_advanceOverlay);
        Content = shell;
        ShowSection(AppSection.Home);

        Loaded += OnLoaded;
    }

    private void ShowSection(AppSection section)
    {
        _activeSection = section;

        foreach (var pair in _navButtons)
        {
            pair.Value.FontAttributes =
                pair.Key == section
                    ? FontAttributes.Bold
                    : FontAttributes.None;
            pair.Value.Opacity =
                pair.Key == section
                    ? 1.0
                    : 0.72;
        }

        foreach (var pair in _sectionViews)
            pair.Value.IsVisible = pair.Key == section;

        _screenTitle.Text = section switch
        {
            AppSection.Home => "Dynasty Home",
            AppSection.Schedule => "Schedule",
            AppSection.Roster => "Roster & Depth Chart",
            AppSection.Recruiting => "Recruiting",
            AppSection.Rankings => "Rankings & Postseason",
            AppSection.Program => "Program",
            AppSection.More => "Dynasty & Saves",
            _ => section.ToString()
        };
    }

    private View BuildHomeSection()
    {
        var dynastyManagerButton = new Button
        {
            Text = "Create / Load Dynasty",
            TextColor = Colors.White
        };
        dynastyManagerButton.Clicked += (_, _) =>
            ShowSection(AppSection.More);

        return new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                new Label
                {
                    Text = "CURRENT DYNASTY",
                    FontAttributes = FontAttributes.Bold
                },
                _currentDynastyLabel,
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        _advanceWeekButton,
                        _manualSaveButton,
                        _autosaveButton
                    }
                },
                dynastyManagerButton,
                _homeStatus,
                _homeHighlights
            }
        };
    }

    private View BuildScheduleSection() =>
        new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label
                {
                    Text = "Your complete season schedule, results, and postseason games.",
                    FontSize = 13
                },
                _scheduleList
            }
        };

    private View BuildRosterSection() =>
        new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                _offseasonRosterStatus,
                _transferPortalList
            }
        };

    private View BuildRecruitingSection()
    {
        var filterStrip = new HorizontalStackLayout
        {
            Spacing = 8,
            Children =
            {
                _recruitingPositionFilter,
                _recruitingStarFilter,
                _recruitingStateFilter,
                _recruitingRegionFilter,
                _recruitingScoutedFilter,
                _recruitingSortPicker
            }
        };

        _recruitingFilterScroller = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = filterStrip
        };

        var listScroll = new ScrollView
        {
            Content = _recruitingList
        };

        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            },
            RowSpacing = 7
        };

        root.Add(_recruitingMetrics);
        Grid.SetRow(_recruitingMetrics, 0);

        root.Add(_recruitingStatus);
        Grid.SetRow(_recruitingStatus, 1);

        var tabScroller = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility =
                ScrollBarVisibility.Never,
            Content = _recruitingTabs
        };

        root.Add(tabScroller);
        Grid.SetRow(tabScroller, 2);

        root.Add(_recruitingSearch);
        Grid.SetRow(_recruitingSearch, 3);

        root.Add(_recruitingFilterScroller);
        Grid.SetRow(_recruitingFilterScroller, 4);

        root.Add(_recruitingDetailHost);
        Grid.SetRow(_recruitingDetailHost, 5);

        root.Add(_recruitingResultsStatus);
        Grid.SetRow(_recruitingResultsStatus, 6);

        root.Add(listScroll);
        Grid.SetRow(listScroll, 7);

        return root;
    }

    private View BuildRankingsSection() =>
        new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                new Label
                {
                    Text = "NATIONAL TOP 25",
                    FontAttributes = FontAttributes.Bold
                },
                _nationalRankingStatus,
                _nationalRankingsList,
                new BoxView { HeightRequest = 1 },
                new Label
                {
                    Text = "CONFERENCE RACE",
                    FontAttributes = FontAttributes.Bold
                },
                _conferenceChampionshipStatus,
                _conferenceStandingsList,
                new BoxView { HeightRequest = 1 },
                new Label
                {
                    Text = "COLLEGE FOOTBALL PLAYOFF & BOWLS",
                    FontAttributes = FontAttributes.Bold
                },
                _postseasonStatus,
                _postseasonList
            }
        };

    private View BuildProgramSection() =>
        new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                _programStatus,
                _programList
            }
        };

    private View BuildMoreSection() =>
        new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                new Label
                {
                    Text = "NEW DYNASTY",
                    FontAttributes = FontAttributes.Bold
                },
                _dynastyNameEntry,
                _teamPicker,
                _newDynastyButton,
                new BoxView { HeightRequest = 1 },
                new Label
                {
                    Text = "RECRUITING ASSISTANCE",
                    FontAttributes = FontAttributes.Bold
                },
                new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        _recruitingAssistanceSwitch,
                        new Label
                        {
                            Text = "CPU suggests weekly targets and offers",
                            VerticalTextAlignment = TextAlignment.Center,
                            FontAttributes = FontAttributes.Bold
                        }
                    }
                },
                new Label
                {
                    Text = "Enabled by default. Assistance adds realistic need-based targets and scholarship offers to your board each recruiting week. Walk-ons fill any remaining roster shortages.",
                    FontSize = 12
                },
                new BoxView { HeightRequest = 1 },
                new Label
                {
                    Text = "AUTOSAVE",
                    FontAttributes = FontAttributes.Bold
                },
                new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        _rollingAutosaveSwitch,
                        new Label
                        {
                            Text = "3-slot rolling weekly autosave",
                            VerticalTextAlignment = TextAlignment.Center,
                            FontAttributes = FontAttributes.Bold
                        }
                    }
                },
                _rollingAutosaveStatus,
                new BoxView { HeightRequest = 1 },
                new Label
                {
                    Text = "SAVE SLOTS",
                    FontAttributes = FontAttributes.Bold
                },
                _saveList,
                new BoxView { HeightRequest = 1 },
                new Label
                {
                    Text = "DATA STATUS",
                    FontAttributes = FontAttributes.Bold
                },
                _importStatus
            }
        };

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (_loaded)
            return;

        _loaded = true;

        try
        {
            await ShowAdvanceProgressAsync("Loading team database...", 0.08);
            var universeCsv = await ReadAssetAsync("legacy_universe.csv");
            await ShowAdvanceProgressAsync("Loading player database...", 0.26);
            var rosterCsv = await ReadAssetAsync("legacy_roster.csv");
            await ShowAdvanceProgressAsync("Loading coaching database...", 0.42);
            var coachCsv = await ReadAssetAsync("legacy_coach.csv");

            await ShowAdvanceProgressAsync("Preparing teams and rosters...", 0.56);
            var universe = LegacyUniverseCsvImporter.Import(universeCsv);
            var roster = LegacyRosterCsvImporter.Import(rosterCsv);
            var coaches = LegacyCoachCsvImporter.Import(coachCsv);

            _teamsByName = universe.Teams.ToDictionary(
                team => team.Name,
                StringComparer.OrdinalIgnoreCase);

            _legacyRosterRows = roster.Players;

            await ShowAdvanceProgressAsync("Building simulation profiles...", 0.72);
            _simulationProfiles = LegacyRosterSimulationProfileBuilder.Build(
                roster.Players,
                universe.Teams);

            _teamNames = _teamsByName.Keys
                .OrderBy(name => name)
                .ToArray();

            _teamPicker.ItemsSource = _teamNames.ToList();

            _importStatus.Text =
                $"Legacy data ready: {universe.Conferences.Count:N0} conferences • " +
                $"{universe.Teams.Count:N0} teams • {roster.Players.Count:N0} players • " +
                $"{coaches.Count:N0} coaches • {_simulationProfiles.Count:N0} sim profiles";

            var databasePath = Path.Combine(
                FileSystem.Current.AppDataDirectory,
                "dynasties.db3");

            await ShowAdvanceProgressAsync("Loading saved dynasties...", 0.88);
            _saveRepository = new SqliteDynastySaveRepository(databasePath);
            await _saveRepository.InitializeAsync();
            await RefreshSaveSlotsAsync();
            await ShowAdvanceProgressAsync("Ready", 1.0);
        }
        catch (Exception ex)
        {
            _importStatus.Text = $"Startup failed: {ex.Message}";
        }
        finally
        {
            _advanceOverlay.IsVisible = false;
        }
    }

    private async void CreateNewDynasty(object? sender, EventArgs e)
    {
        if (_teamPicker.SelectedIndex < 0 ||
            _teamPicker.SelectedIndex >= _teamNames.Count)
        {
            _currentDynastyLabel.Text = "Choose a team before creating a dynasty.";
            return;
        }

        var teamName = _teamNames[_teamPicker.SelectedIndex];
        var dynastyName = string.IsNullOrWhiteSpace(_dynastyNameEntry.Text)
            ? $"{teamName} Dynasty"
            : _dynastyNameEntry.Text.Trim();

        var dynastyId = Guid.NewGuid();
        const int startingYear = 2026;

        SetNewDynastyControlsEnabled(false);
        SetDynastyControlsEnabled(false);
        _currentDynastyLabel.Text =
            $"Generating the {_teamsByName.Count}-team schedule…";

        try
        {
            await ShowAdvanceProgressAsync("Creating dynasty...", 0.05);
            await ShowAdvanceProgressAsync("Generating league schedule...", 0.18);
            var schedule = await GenerateScheduleAsync(
                dynastyId,
                startingYear);

            await ShowAdvanceProgressAsync("Building team rosters...", 0.42);
            var activeRoster = LegacyDynastyRosterFactory.Create(
                _legacyRosterRows,
                _teamsByName.Values,
                dynastyId,
                startingYear);

            _currentDynasty = new DynastyState
            {
                DynastyId = dynastyId,
                DynastyName = dynastyName,
                UserTeamName = teamName,
                SeasonYear = startingYear,
                Week = 0,
                Phase = SeasonPhase.Preseason,
                RecruitingAssistanceEnabled =
                    _recruitingAssistanceSwitch.IsToggled,
                Schedule = schedule,
                ActiveRoster = activeRoster,
                HighSchoolRecruitingPool =
                    HighSchoolRecruitingPoolFactory.Create(
                        dynastyId,
                        startingYear,
                        _teamsByName.Count,
                        _legacyRosterRows)
            };

            _currentDynasty = PlayerRatingService
                .EnsureProfiles(_currentDynasty);
            await ShowAdvanceProgressAsync("Setting depth charts...", 0.68);
            _currentDynasty = RosterManagementService
                .NormalizeAllDepthCharts(_currentDynasty);
            await ShowAdvanceProgressAsync("Hiring program staff...", 0.76);
            _currentDynasty = ProgramPrestigeService.EnsureInitialPrestige(
                _currentDynasty, _teamsByName.Values);
            _currentDynasty = StaffManagementService.EnsureLeagueStaff(
                _currentDynasty, _teamsByName.Values);
            await ShowAdvanceProgressAsync("Hiring scouting department...", 0.88);
            _currentDynasty = ScoutingDepartmentService.EnsureDepartment(
                _currentDynasty, _teamsByName[teamName]);

            await ShowAdvanceProgressAsync("Dynasty ready", 1.0);
            SetDynastyControlsEnabled(true);
            RenderCurrentDynasty();
            ShowSection(AppSection.Home);
        }
        catch (Exception ex)
        {
            _currentDynasty = null;
            _currentDynastyLabel.Text =
                $"Schedule generation failed: {ex.Message}";
            _scheduleList.Children.Clear();
        }
        finally
        {
            _advanceOverlay.IsVisible = false;
            SetNewDynastyControlsEnabled(true);
        }
    }

    private async void AdvanceWeek(object? sender, EventArgs e)
    {
        if (_currentDynasty is null || _isAdvancing)
            return;

        if (_currentDynasty.UserCoachIsFired)
        {
            await DisplayAlert(
                "Head Coach Job Required",
                "You were fired from your previous program. Open Program and accept a head-coaching offer before advancing.",
                "OK");
            ShowSection(AppSection.Program);
            return;
        }

        _isAdvancing = true;
        SetDynastyControlsEnabled(false);
        await ShowAdvanceProgressAsync("Preparing weekly advance...", 0.04);
        await Task.Yield();

        var phaseAtStart = _currentDynasty.Phase;
        var weekAtStart = _currentDynasty.Week;

        try
        {
            var phaseBeforeAdvance = _currentDynasty.Phase;
            var finalRecruitingWeek =
                phaseBeforeAdvance == SeasonPhase.Recruiting &&
                SeasonProgression.IsFinalRecruitingWeek(
                    _currentDynasty);

        if (phaseBeforeAdvance == SeasonPhase.RegularSeason &&
            _teamsByName.TryGetValue(
                _currentDynasty.UserTeamName,
                out var midseasonRecruitingTeam))
        {
            await ShowAdvanceProgressAsync("Processing recruiting decisions...", 0.14);
            _currentDynasty = InteractiveRecruitingService
                .ResolveCurrentPhase(
                    _currentDynasty,
                    midseasonRecruitingTeam,
                    _teamsByName);
        }

        if (phaseBeforeAdvance is
            SeasonPhase.TransferPortal or SeasonPhase.Recruiting)
        {
            await ShowAdvanceProgressAsync("Resolving recruiting and portal activity...", 0.22);
            if (_teamsByName.TryGetValue(
                    _currentDynasty.UserTeamName,
                    out var recruitingTeam))
            {
                _currentDynasty = InteractiveRecruitingService
                    .ResolveCurrentPhase(
                        _currentDynasty,
                        recruitingTeam,
                        _teamsByName);
            }

            if (phaseBeforeAdvance ==
                    SeasonPhase.TransferPortal ||
                finalRecruitingWeek)
            {
                SetDynastyControlsEnabled(false);
                _rollingAutosaveStatus.Text =
                    phaseBeforeAdvance ==
                            SeasonPhase.TransferPortal
                        ? "CPU teams are resolving transfer-portal needs…"
                        : "Finalizing recruiting classes…";

                var recruitingState = _currentDynasty;
                _currentDynasty = await Task.Run(() =>
                    CpuRecruitingService.ApplyPhaseAssistance(
                        recruitingState,
                        _teamsByName,
                        recruitingState.RecruitingAssistanceEnabled));

                _currentDynasty = PlayerRatingService
                    .EnsureProfiles(_currentDynasty);

                SetDynastyControlsEnabled(true);
            }

            _currentDynasty = _currentDynasty with
            {
                RecruitingPointsRemaining = 0
            };
        }

        if (phaseBeforeAdvance == SeasonPhase.RosterManagement)
        {
            await ShowAdvanceProgressAsync("Balancing rosters and walk-ons...", 0.30);
            SetDynastyControlsEnabled(false);
            _rollingAutosaveStatus.Text =
                "Filling remaining roster shortages with walk-ons…";

            var rosterState = _currentDynasty;
            _currentDynasty = await Task.Run(() =>
                WalkOnRosterService.FillAllTeams(
                    rosterState,
                    _teamsByName));

            _currentDynasty = PlayerRatingService
                .EnsureProfiles(_currentDynasty);

            _currentDynasty = RosterManagementService
                .NormalizeAllDepthCharts(_currentDynasty);

            SetDynastyControlsEnabled(true);
        }

        if (phaseBeforeAdvance == SeasonPhase.RegularSeason)
        {
            await ShowAdvanceProgressAsync("Scouting this week's prospects...", 0.34);
            if (_teamsByName.TryGetValue(_currentDynasty.UserTeamName,
                    out var scoutingTeam))
            {
                _currentDynasty = ScoutingDepartmentService
                    .AdvanceRegularSeasonWeek(_currentDynasty, scoutingTeam);
            }

            await ShowAdvanceProgressAsync("Simulating games...", 0.48);
            _currentDynasty = WeekSimulation.SimulateCurrentRegularSeasonWeek(
                _currentDynasty,
                _teamsByName,
                BuildCurrentSimulationProfiles());

            _currentDynasty = InjuryService
                .AdvanceAndGenerateForCurrentWeek(_currentDynasty);

            await ShowAdvanceProgressAsync("Processing the transfer portal...", 0.62);
            _currentDynasty = TransferPortalMarketService
                .AdvanceRegularSeasonWeek(_currentDynasty);

            if (weekAtStart == 6)
            {
                await ShowAdvanceProgressAsync(
                    "Applying midseason player development...", 0.68);
                var midseasonState = _currentDynasty;
                _currentDynasty = await Task.Run(() =>
                    PlayerDevelopmentService.ApplyMidseasonDevelopment(
                        midseasonState));
            }
        }
        else if (phaseBeforeAdvance == SeasonPhase.ConferenceChampionship)
        {
            await ShowAdvanceProgressAsync("Simulating conference championships...", 0.48);
            _currentDynasty = ConferenceChampionshipService
                .SimulateChampionships(
                    _currentDynasty,
                    _teamsByName,
                    BuildCurrentSimulationProfiles());

            _currentDynasty = InjuryService
                .AdvanceAndGenerateForCurrentWeek(_currentDynasty);
        }
        else if (phaseBeforeAdvance == SeasonPhase.Postseason)
        {
            await ShowAdvanceProgressAsync("Simulating postseason games...", 0.48);
            var postseasonProfiles =
                BuildCurrentSimulationProfiles();

            _currentDynasty = BowlService
                .SimulateCurrentWeek(
                    _currentDynasty,
                    _teamsByName,
                    postseasonProfiles);

            _currentDynasty = CollegeFootballPlayoffService
                .SimulateCurrentRound(
                    _currentDynasty,
                    _teamsByName,
                    postseasonProfiles);

            _currentDynasty = InjuryService
                .AdvanceAndGenerateForCurrentWeek(_currentDynasty);

            _currentDynasty = CollegeFootballPlayoffService
                .ScheduleNextRound(_currentDynasty);
        }

        if (phaseBeforeAdvance == SeasonPhase.Offseason)
        {
            await ShowAdvanceProgressAsync("Applying player progression...", 0.58);
            SetDynastyControlsEnabled(false);
            _rollingAutosaveStatus.Text =
                "Applying player development and offseason regression…";

            var developmentState = _currentDynasty;
            _currentDynasty = await Task.Run(() =>
                PlayerDevelopmentService.ApplyOffseasonDevelopment(
                    developmentState));

            SetDynastyControlsEnabled(true);
        }

        var wasOffseason = phaseBeforeAdvance == SeasonPhase.Offseason;
        await ShowAdvanceProgressAsync("Updating rankings, stats, and calendar...", 0.72);
        _currentDynasty = SeasonProgression.Advance(_currentDynasty);

        if (phaseBeforeAdvance == SeasonPhase.Postseason &&
            _currentDynasty.Phase == SeasonPhase.TransferPortal)
        {
            await ShowAdvanceProgressAsync("Finalizing season history and awards...", 0.74);
            var finalProfiles = BuildCurrentSimulationProfiles();
            _currentDynasty = DynastyHistoryService.FinalizeSeason(
                _currentDynasty, _teamsByName, finalProfiles);
            await ShowAdvanceProgressAsync("Updating program prestige...", 0.76);
            _currentDynasty = ProgramPrestigeService.ApplySeasonResults(
                _currentDynasty, _teamsByName, finalProfiles);
            _currentDynasty = UserCoachCareerService
                .EvaluateCompletedSeason(
                    _currentDynasty,
                    _teamsByName);

            await ShowAdvanceProgressAsync("Evaluating coaching staffs...", 0.77);
            _currentDynasty = StaffManagementService.AdvanceSeason(
                _currentDynasty);
            _currentDynasty = StaffMarketService.EnsureMarket(
                _currentDynasty, _teamsByName);
            _currentDynasty = StaffMarketService.RunCpuCarousel(
                _currentDynasty, _teamsByName);

            if (_teamsByName.TryGetValue(_currentDynasty.UserTeamName,
                    out var reportTeam))
            {
                _currentDynasty = ScoutingDepartmentService
                    .GenerateRecommendationReport(_currentDynasty, reportTeam);
            }

            await ShowAdvanceProgressAsync("Opening the transfer portal...", 0.78);
            _currentDynasty = OffseasonPlayerLifecycleService
                .EnterTransferPortal(_currentDynasty);

            _currentDynasty = TransferPortalMarketService
                .MaterializePendingCommitments(_currentDynasty);

            if (_teamsByName.TryGetValue(
                    _currentDynasty.UserTeamName,
                    out var portalTeam))
            {
                _currentDynasty = InteractiveRecruitingService
                    .EnsurePhaseInitialized(
                        _currentDynasty,
                        portalTeam);
            }
        }

        if (_currentDynasty.Phase == SeasonPhase.RegularSeason &&
            _teamsByName.TryGetValue(
                _currentDynasty.UserTeamName,
                out var regularSeasonRecruitingTeam))
        {
            _currentDynasty = InteractiveRecruitingService
                .EnsurePhaseInitialized(
                    _currentDynasty,
                    regularSeasonRecruitingTeam);
        }

        if (phaseBeforeAdvance == SeasonPhase.Recruiting &&
            _currentDynasty.Phase == SeasonPhase.RosterManagement)
        {
            _currentDynasty = PlayerRatingService
                .EnsureProfiles(_currentDynasty);
            _currentDynasty = RosterManagementService
                .NormalizeAllDepthCharts(_currentDynasty);
        }

        if (phaseBeforeAdvance == SeasonPhase.TransferPortal &&
            _currentDynasty.Phase == SeasonPhase.Recruiting)
        {
            if (_currentDynasty.HighSchoolRecruitingPool.Count == 0 ||
                _currentDynasty.HighSchoolRecruitingPool.Any(recruit =>
                    recruit.SeasonYear != _currentDynasty.SeasonYear))
            {
                _currentDynasty = _currentDynasty with
                {
                    HighSchoolRecruitingPool =
                        HighSchoolRecruitingPoolFactory.Create(
                            _currentDynasty.DynastyId,
                            _currentDynasty.SeasonYear,
                            _teamsByName.Count,
                            _legacyRosterRows)
                };
            }

            if (_teamsByName.TryGetValue(
                    _currentDynasty.UserTeamName,
                    out var recruitingTeam))
            {
                _currentDynasty = InteractiveRecruitingService
                    .EnsurePhaseInitialized(
                        _currentDynasty,
                        recruitingTeam);
            }
        }

        if (_currentDynasty.Phase == SeasonPhase.Recruiting &&
            _teamsByName.TryGetValue(
                _currentDynasty.UserTeamName,
                out var weeklyRecruitingTeam))
        {
            _currentDynasty = InteractiveRecruitingService
                .EnsurePhaseInitialized(
                    _currentDynasty,
                    weeklyRecruitingTeam);
            _currentDynasty = CpuRecruitingService
                .ApplyWeeklyUserAssistance(
                    _currentDynasty,
                    weeklyRecruitingTeam);
        }

        if (phaseBeforeAdvance == SeasonPhase.RegularSeason &&
            _currentDynasty.Phase == SeasonPhase.ConferenceChampionship)
        {
            _currentDynasty = ConferenceChampionshipService
                .ScheduleChampionships(
                    _currentDynasty,
                    _teamsByName,
                    BuildCurrentSimulationProfiles());
        }

        if (phaseBeforeAdvance == SeasonPhase.ConferenceChampionship &&
            _currentDynasty.Phase == SeasonPhase.Postseason)
        {
            _currentDynasty = CollegeFootballPlayoffService
                .InitializePlayoff(
                    _currentDynasty,
                    _teamsByName,
                    BuildCurrentSimulationProfiles());

            _currentDynasty = BowlService
                .InitializeBowls(
                    _currentDynasty,
                    _teamsByName,
                    BuildCurrentSimulationProfiles());
        }

        if (wasOffseason && _currentDynasty.Phase == SeasonPhase.Preseason)
        {
            SetDynastyControlsEnabled(false);
            _rollingAutosaveStatus.Text =
                $"Generating {_currentDynasty.SeasonYear} schedule…";

            try
            {
                var schedule = await GenerateScheduleAsync(
                    _currentDynasty.DynastyId,
                    _currentDynasty.SeasonYear);

                _currentDynasty = _currentDynasty with
                {
                    Schedule = schedule,
                    HighSchoolRecruitingPool =
                        HighSchoolRecruitingPoolFactory.Create(
                            _currentDynasty.DynastyId,
                            _currentDynasty.SeasonYear,
                            _teamsByName.Count,
                            _legacyRosterRows),
                    ScoutingRecommendationReport = null
                };

                _currentDynasty = StaffManagementService.EnsureLeagueStaff(
                    _currentDynasty, _teamsByName.Values);

                if (_teamsByName.TryGetValue(_currentDynasty.UserTeamName,
                        out var newSeasonTeam))
                {
                    _currentDynasty = ScoutingDepartmentService
                        .EnsureDepartment(_currentDynasty, newSeasonTeam);
                }
            }
            catch (Exception ex)
            {
                _rollingAutosaveStatus.Text =
                    $"Schedule generation failed: {ex.Message}";
                SetDynastyControlsEnabled(true);
                RenderCurrentDynasty();
                return;
            }

            SetDynastyControlsEnabled(true);
        }

        await ShowAdvanceProgressAsync("Saving weekly progress...", 0.90);
        RenderCurrentDynasty();

        if (_rollingAutosaveSwitch.IsToggled &&
            _saveRepository is not null &&
            _currentDynasty.Week >= 1)
        {
            var kind = RollingAutosavePolicy.GetSlotForWeek(_currentDynasty.Week);
            await _saveRepository.SaveRollingWeeklyAsync(_currentDynasty);
            await RefreshSaveSlotsAsync();
            _rollingAutosaveStatus.Text =
                $"Saved Week {_currentDynasty.Week} to {RollingAutosavePolicy.GetDisplayName(kind)}.";
        }
        else if (_currentDynasty.Phase == SeasonPhase.Preseason)
        {
            _rollingAutosaveStatus.Text =
                $"Started {_currentDynasty.SeasonYear} preseason.";
        }
        else if (_currentDynasty.Phase == SeasonPhase.RegularSeason &&
                 _currentDynasty.Week == 0)
        {
            _rollingAutosaveStatus.Text =
                "Week 0 is ready. Only the scheduled opening games will be simulated.";
        }
        else
        {
            _rollingAutosaveStatus.Text =
                $"{_currentDynasty.SeasonYear} • {_currentDynasty.Phase} • " +
                $"Week {_currentDynasty.Week}";
        }
        await ShowAdvanceProgressAsync("Advance complete", 1.0);
        }
        catch (Exception ex)
        {
            SetDynastyControlsEnabled(true);
            _rollingAutosaveStatus.Text =
                $"Advance failed at {phaseAtStart} Week {weekAtStart}: {ex.Message}";
            RenderCurrentDynasty();
        }
        finally
        {
            _isAdvancing = false;
            _advanceOverlay.IsVisible = false;
            SetDynastyControlsEnabled(_currentDynasty is not null);
        }
    }

    private async Task SaveCurrentAsync(SaveKind kind)
    {
        if (_currentDynasty is null || _saveRepository is null)
            return;

        Guid? saveId = null;

        if (kind != SaveKind.Manual)
        {
            var existing = (await _saveRepository.ListAsync())
                .FirstOrDefault(slot =>
                    slot.DynastyId == _currentDynasty.DynastyId &&
                    slot.Kind == kind);

            saveId = existing?.SaveId;
        }

        await _saveRepository.SaveAsync(_currentDynasty, kind, saveId);
        await RefreshSaveSlotsAsync();
    }

    private async Task LoadSaveAsync(Guid saveId)
    {
        if (_saveRepository is null)
            return;

        SetNewDynastyControlsEnabled(false);
        SetDynastyControlsEnabled(false);
        try
        {
        await ShowAdvanceProgressAsync("Loading dynasty save...", 0.08);
        var state = await _saveRepository.LoadAsync(saveId);
        if (state is null)
            return;

        if (state.Schedule.Count == 0 && _teamsByName.Count > 1)
        {
            SetDynastyControlsEnabled(false);
            _currentDynastyLabel.Text =
                "Generating schedule for this older save…";

            try
            {
                await ShowAdvanceProgressAsync("Rebuilding missing schedule...", 0.24);
                state = state with
                {
                    Schedule = await GenerateScheduleAsync(
                        state.DynastyId,
                        state.SeasonYear)
                };
            }
            catch (Exception ex)
            {
                _currentDynastyLabel.Text =
                    $"Schedule generation failed: {ex.Message}";
                return;
            }
        }

        if (state.ActiveRoster.Count == 0 &&
            _legacyRosterRows.Count > 0)
        {
            await ShowAdvanceProgressAsync("Restoring team rosters...", 0.38);
            state = state with
            {
                ActiveRoster = LegacyDynastyRosterFactory.Create(
                    _legacyRosterRows,
                    _teamsByName.Values,
                    state.DynastyId,
                    state.SeasonYear)
            };
        }

        await ShowAdvanceProgressAsync("Checking players and depth charts...", 0.54);
        state = PlayerRatingService.EnsureProfiles(state);
        state = RosterManagementService
            .NormalizeAllDepthCharts(state);
        state = ProgramPrestigeService.EnsureInitialPrestige(
            state, _teamsByName.Values);
        state = StaffManagementService.EnsureLeagueStaff(
            state,
            _teamsByName.Values,
            state.StaffMarketSeasonYear == state.SeasonYear
                ? state.UserTeamName
                : null);

        if ((state.Phase is
                 SeasonPhase.TransferPortal or
                 SeasonPhase.Recruiting or
                 SeasonPhase.RosterManagement or
                 SeasonPhase.Offseason) &&
            state.TeamSeasonHistory.Any(item =>
                item.SeasonYear == state.SeasonYear))
        {
            state = UserCoachCareerService
                .EvaluateCompletedSeason(
                    state,
                    _teamsByName);
            state = StaffManagementService.AdvanceSeason(state);
            state = StaffMarketService.EnsureMarket(
                state, _teamsByName);
            state = StaffMarketService.RunCpuCarousel(
                state, _teamsByName);
        }

        if (state.Phase == SeasonPhase.TransferPortal &&
            state.TransferPortalEntryWindowSeasonYear !=
                state.SeasonYear)
        {
            if (state.TransferPortalEntries.Any(entry =>
                    entry.SeasonYear == state.SeasonYear))
            {
                state = state with
                {
                    TransferPortalEntryWindowSeasonYear =
                        state.SeasonYear
                };
            }
            else
            {
                state = OffseasonPlayerLifecycleService
                    .EnterTransferPortal(state);
            }
        }

        if (state.Phase <= SeasonPhase.Recruiting &&
            (state.HighSchoolRecruitingPool.Count == 0 ||
             state.HighSchoolRecruitingPool.Any(recruit =>
                 recruit.SeasonYear != state.SeasonYear)))
        {
            state = state with
            {
                HighSchoolRecruitingPool =
                    HighSchoolRecruitingPoolFactory.Create(
                        state.DynastyId,
                        state.SeasonYear,
                        _teamsByName.Count,
                        _legacyRosterRows)
            };
        }

        if (_teamsByName.TryGetValue(state.UserTeamName, out var scoutingTeam))
        {
            await ShowAdvanceProgressAsync("Restoring scouting department...", 0.72);
            state = ScoutingDepartmentService.EnsureDepartment(
                state, scoutingTeam);
        }

        if ((state.Phase is
                 SeasonPhase.TransferPortal or SeasonPhase.Recruiting ||
             (state.Phase == SeasonPhase.RegularSeason &&
              state.TransferPortalEntries.Count > 0)) &&
            _teamsByName.TryGetValue(
                state.UserTeamName,
                out var phaseTeam))
        {
            state = InteractiveRecruitingService
                .EnsurePhaseInitialized(
                    state,
                    phaseTeam);

            if (state.Phase == SeasonPhase.Recruiting)
            {
                state = CpuRecruitingService
                    .ApplyWeeklyUserAssistance(
                        state,
                        phaseTeam);
            }
        }

        if (state.Phase == SeasonPhase.RosterManagement)
        {
            state = RosterManagementService
                .NormalizeAllDepthCharts(state);
        }

        await ShowAdvanceProgressAsync("Preparing dynasty screen...", 0.90);
        _currentDynasty = state;
        _recruitingAssistanceSwitch.IsToggled =
            state.RecruitingAssistanceEnabled;
        SetDynastyControlsEnabled(true);
        RenderCurrentDynasty();
        ShowSection(AppSection.Home);
        await ShowAdvanceProgressAsync("Dynasty loaded", 1.0);
        }
        finally
        {
            _advanceOverlay.IsVisible = false;
            SetNewDynastyControlsEnabled(true);
            SetDynastyControlsEnabled(_currentDynasty is not null);
        }
    }

    private async Task DeleteSaveAsync(Guid saveId)
    {
        if (_saveRepository is null)
            return;

        await _saveRepository.DeleteAsync(saveId);
        await RefreshSaveSlotsAsync();
    }

    private async Task RefreshSaveSlotsAsync()
    {
        if (_saveRepository is null)
            return;

        _saveList.Children.Clear();
        var saves = await _saveRepository.ListAsync();

        if (saves.Count == 0)
        {
            _saveList.Children.Add(new Label
            {
                Text = "No saves yet."
            });
            return;
        }

        foreach (var save in saves)
        {
            var loadButton = new Button
            {
                Text = "Load",
                TextColor = Colors.White
            };
            loadButton.Clicked += async (_, _) => await LoadSaveAsync(save.SaveId);

            var deleteButton = new Button
            {
                Text = "Delete",
                TextColor = Colors.White
            };
            deleteButton.Clicked += async (_, _) => await DeleteSaveAsync(save.SaveId);

            var card = new VerticalStackLayout
            {
                Padding = new Thickness(12),
                Spacing = 6,
                Children =
                {
                    new Label
                    {
                        Text = save.DynastyName,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 17
                    },
                    new Label
                    {
                        Text =
                            $"{save.UserTeamName} • {save.SeasonYear} • " +
                            $"{FormatWeek(save)} • {RollingAutosavePolicy.GetDisplayName(save.Kind)}"
                    },
                    new Label
                    {
                        Text = $"Updated {save.UpdatedUtc.ToLocalTime():g}",
                        FontSize = 12
                    },
                    new HorizontalStackLayout
                    {
                        Spacing = 10,
                        Children =
                        {
                            loadButton,
                            deleteButton
                        }
                    }
                }
            };

            _saveList.Children.Add(new Border
            {
                StrokeThickness = 1,
                Padding = 0,
                Content = card
            });
        }
    }

    private void RenderCurrentDynasty()
    {
        if (_currentDynasty is null)
        {
            _currentDynastyLabel.Text = "No dynasty loaded.";
            _offseasonRosterStatus.Text =
                "Roster data will appear after a dynasty is created.";
            _transferPortalList.Children.Clear();
            _recruitingStatus.Text =
                "Recruiting information will appear after a dynasty is created.";
            _recruitingList.Children.Clear();
            _homeStatus.Text =
                "Create or load a dynasty to begin.";
            _homeHighlights.Children.Clear();
            _programStatus.Text =
                "Program information will appear after a dynasty is created.";
            _programList.Children.Clear();
            _nationalRankingStatus.Text =
                "National rankings will appear after a dynasty is created.";
            _nationalRankingsList.Children.Clear();
            _postseasonStatus.Text =
                "The 12-team CFP field is selected after conference championships.";
            _postseasonList.Children.Clear();
            _conferenceChampionshipStatus.Text =
                "Conference standings will appear after a dynasty is created.";
            _conferenceStandingsList.Children.Clear();
            _scheduleList.Children.Clear();
            return;
        }

        var userGames = _currentDynasty.Schedule
            .Where(game => game.InvolvesTeam(_currentDynasty.UserTeamName) && game.HasPlayed)
            .ToArray();

        var wins = userGames.Count(game =>
            game.WinnerTeamName?.Equals(
                _currentDynasty.UserTeamName,
                StringComparison.OrdinalIgnoreCase) == true);
        var losses = userGames.Length - wins;

        var conferenceGames = userGames
            .Where(game =>
                game.GameType == ScheduledGameType.Conference)
            .ToArray();

        var conferenceWins = conferenceGames.Count(game =>
            game.WinnerTeamName?.Equals(
                _currentDynasty.UserTeamName,
                StringComparison.OrdinalIgnoreCase) == true);
        var conferenceLosses =
            conferenceGames.Length - conferenceWins;

        var weekLabel = _currentDynasty.Phase switch
        {
            SeasonPhase.Preseason => "Preseason",
            SeasonPhase.Recruiting =>
                $"Recruiting Week {SeasonProgression.GetRecruitingWeekNumber(_currentDynasty)} " +
                $"of {SeasonProgression.RecruitingWeekCount}",
            _ => $"Week {_currentDynasty.Week}"
        };

        _currentDynastyLabel.Text =
            $"{_currentDynasty.DynastyName}\n" +
            $"{_currentDynasty.UserTeamName} • Record {wins}-{losses} • " +
            $"Conf {conferenceWins}-{conferenceLosses}\n" +
            $"{_currentDynasty.SeasonYear} • {_currentDynasty.Phase} • {weekLabel}\n" +
            $"Dynasty ID: {_currentDynasty.DynastyId}";

        RenderOffseasonRoster();
        RenderRecruitingScreen();
        RenderHomeScreen();
        RenderProgramScreen();
        RenderNationalRankings();
        RenderPostseason();
        RenderConferenceRace();
        RenderUserSchedule();
    }

    private void RenderOffseasonRoster()
    {
        _transferPortalList.Children.Clear();

        if (_currentDynasty is null)
        {
            _offseasonRosterStatus.Text =
                "No roster data available.";
            return;
        }

        var userRoster = _currentDynasty.ActiveRoster
            .Where(player => player.TeamName.Equals(
                _currentDynasty.UserTeamName,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        RenderRosterManagement(userRoster);

        var activeInjuries = userRoster
            .Where(player => player.CurrentInjury is not null)
            .OrderByDescending(player => player.CurrentInjury!.Severity)
            .ThenByDescending(player => player.CurrentInjury!.WeeksRemaining)
            .ToArray();

        if (activeInjuries.Length > 0)
        {
            _transferPortalList.Children.Insert(
                0,
                new Label
                {
                    Text =
                        $"CURRENT INJURIES: {activeInjuries.Length} • " +
                        string.Join(
                            " • ",
                            activeInjuries.Take(4).Select(player =>
                                $"{player.Position} {player.FullName} " +
                                $"({player.CurrentInjury!.Severity}, " +
                                $"{player.CurrentInjury.WeeksRemaining} wk)")),
                    FontSize = 12
                });
        }
    }

    private void RenderRecruitingScreen()
    {
        _recruitingList.Children.Clear();
        _recruitingMetrics.Children.Clear();

        if (_currentDynasty is null)
        {
            _recruitingStatus.Text =
                "No recruiting data available.";
            _recruitingResultsStatus.Text = string.Empty;
            _recruitingDetailHost.Content = null;
            _recruitingDetailHost.IsVisible = false;
            return;
        }

        NormalizeRecruitingViewForPhase();

        foreach (var pair in _recruitingTabButtons)
        {
            pair.Value.FontAttributes =
                pair.Key == _recruitingView
                    ? FontAttributes.Bold
                    : FontAttributes.None;
            pair.Value.Opacity =
                pair.Key == _recruitingView
                    ? 1.0
                    : 0.68;
        }

        var showsProspectList = _recruitingView is
            RecruitingView.Board or RecruitingView.HighSchool or
            RecruitingView.TransferPortal;
        _recruitingSearch.IsVisible = showsProspectList;
        _recruitingFilterScroller.IsVisible = showsProspectList;
        _recruitingPositionFilter.IsVisible = showsProspectList;
        _recruitingSortPicker.IsVisible = showsProspectList;
        _recruitingScoutedFilter.IsVisible = showsProspectList;
        _recruitingStarFilter.IsVisible =
            _recruitingView == RecruitingView.HighSchool;
        _recruitingStateFilter.IsVisible =
            _recruitingView == RecruitingView.HighSchool;
        _recruitingRegionFilter.IsVisible =
            _recruitingView == RecruitingView.HighSchool;

        _recruitingTabButtons[RecruitingView.HighSchool]
            .IsEnabled =
            _currentDynasty.HighSchoolRecruitingPool.Count > 0;

        _recruitingTabButtons[RecruitingView.TransferPortal]
            .IsEnabled =
            _currentDynasty.TransferPortalEntries.Count > 0;

        var userRosterCount =
            _currentDynasty.ActiveRoster.Count(player =>
                player.TeamName.Equals(
                    _currentDynasty.UserTeamName,
                    StringComparison.OrdinalIgnoreCase));

        var availablePortal =
            _currentDynasty.TransferPortalEntries
                .Where(entry =>
                    !entry.OriginTeamName.Equals(
                        _currentDynasty.UserTeamName,
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

        var boardCount =
            _currentDynasty.RecruitingInteractions.Count(
                interaction =>
                    interaction.SeasonYear ==
                        _currentDynasty.SeasonYear &&
                    interaction.IsOnTargetBoard &&
                    interaction.CommittedTeamName is null);

        var userCommitments =
            _currentDynasty.RecruitingCommitments.Count(
                commitment =>
                    commitment.TeamName.Equals(
                        _currentDynasty.UserTeamName,
                        StringComparison.OrdinalIgnoreCase) &&
                    commitment.SeasonYear ==
                        _currentDynasty.SeasonYear);

        if (_recruitingView == RecruitingView.Scouting)
        {
            var evaluatedCount = _currentDynasty.RecruitingInteractions.Count(
                interaction => interaction.SeasonYear == _currentDynasty.SeasonYear &&
                               interaction.ScoutingPercent >= 50);
            var priorityCount = _currentDynasty.RecruitingInteractions.Count(
                interaction => interaction.SeasonYear == _currentDynasty.SeasonYear &&
                               interaction.IsPriorityScout &&
                               interaction.CommittedTeamName is null);
            AddRecruitingMetric("SCOUTS",
                _currentDynasty.ScoutingStaff.Count.ToString());
            AddRecruitingMetric("EVALUATED", evaluatedCount.ToString("N0"));
            AddRecruitingMetric("PRIORITY", priorityCount.ToString());
            AddRecruitingMetric("REPORT",
                _currentDynasty.ScoutingRecommendationReport?.SeasonYear ==
                _currentDynasty.SeasonYear ? "READY" : "PENDING");
        }
        else
        {
            AddRecruitingMetric(
                "WEEK",
                _currentDynasty.Phase == SeasonPhase.Recruiting
                    ? $"{SeasonProgression.GetRecruitingWeekNumber(_currentDynasty)}/{SeasonProgression.RecruitingWeekCount}"
                    : _currentDynasty.Phase == SeasonPhase.TransferPortal
                        ? "ENTRY"
                        : _currentDynasty.Phase == SeasonPhase.RegularSeason
                            ? $"W{_currentDynasty.Week}"
                            : "—");
            AddRecruitingMetric("POINTS",
                _currentDynasty.RecruitingPointsRemaining.ToString("N0"));
            AddRecruitingMetric("BOARD", boardCount.ToString());
            AddRecruitingMetric("COMMITS", userCommitments.ToString());
        }

        _recruitingStatus.Text =
            _currentDynasty.Phase switch
            {
                SeasonPhase.TransferPortal =>
                    $"Portal entry window open • " +
                    $"{availablePortal.Length:N0} available • " +
                    $"Roster {userRosterCount}/85",

                SeasonPhase.Recruiting =>
                    $"Offseason recruiting • " +
                    $"{_currentDynasty.HighSchoolRecruitingPool.Count:N0} HS recruits • " +
                    $"{availablePortal.Length:N0} unsigned transfers • " +
                    "weekly points do not roll over",

                SeasonPhase.RegularSeason =>
                    $"Portal entry closed • " +
                    $"{availablePortal.Length:N0} unsigned transfers remain • " +
                    $"mid-season commits join {_currentDynasty.SeasonYear + 1}",

                _ =>
                    $"Recruiting currently closed • " +
                    $"{availablePortal.Length:N0} unsigned transfers remain"
            };

        if (_recruitingView == RecruitingView.Scouting)
        {
            _recruitingStatus.Text = _currentDynasty.Phase ==
                SeasonPhase.RegularSeason
                ? "Scouts are active this week. Assign coverage or mark individual priority prospects."
                : "Review staff assignments and the latest scouting department recommendations.";
        }

        RenderSelectedRecruitDetail();

        if (_recruitingView ==
            RecruitingView.Commitments)
        {
            RenderRecruitingCommitments();
            return;
        }

        if (_recruitingView == RecruitingView.Scouting)
        {
            RenderScoutingDepartment();
            return;
        }

        var prospects = GetRecruitingProspectsForCurrentView();

        var search = _recruitingSearch.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            prospects = prospects
                .Where(prospect =>
                    prospect.FullName.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase) ||
                    prospect.Subtitle.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (_recruitingPositionFilter.SelectedIndex > 0 &&
            Enum.TryParse<Position>(
                _recruitingPositionFilter.SelectedItem?.ToString(),
                out var selectedPosition))
        {
            prospects = prospects
                .Where(prospect =>
                    prospect.Position ==
                        selectedPosition)
                .ToList();
        }

        if (_recruitingView ==
                RecruitingView.HighSchool &&
            _recruitingStarFilter.SelectedIndex > 0)
        {
            var stars =
                6 - _recruitingStarFilter.SelectedIndex;

            prospects = prospects
                .Where(prospect =>
                    prospect.StarRating == stars)
                .ToList();
        }


        if (_recruitingView == RecruitingView.HighSchool &&
            _recruitingStateFilter.SelectedIndex > 0)
        {
            var stateIndex = _recruitingStateFilter.SelectedIndex - 1;
            prospects = prospects.Where(prospect =>
            {
                var recruit = _currentDynasty.HighSchoolRecruitingPool
                    .First(item => item.RecruitId == prospect.ProspectId);
                return RecruitGeography.GetStateIndex(recruit) == stateIndex;
            }).ToList();
        }

        if (_recruitingView == RecruitingView.HighSchool &&
            _recruitingRegionFilter.SelectedIndex > 0)
        {
            var regionIndex = _recruitingRegionFilter.SelectedIndex - 1;
            prospects = prospects.Where(prospect =>
            {
                var recruit = _currentDynasty.HighSchoolRecruitingPool
                    .First(item => item.RecruitId == prospect.ProspectId);
                return RecruitGeography.GetRegion(recruit) == regionIndex;
            }).ToList();
        }

        var scoutedFilter = (ScoutedFilter)Math.Clamp(
            _recruitingScoutedFilter.SelectedIndex,
            0,
            Enum.GetValues<ScoutedFilter>().Length - 1);
        if (scoutedFilter != ScoutedFilter.All)
        {
            prospects = prospects.Where(prospect =>
            {
                var percent = InteractiveRecruitingService.GetInteraction(
                    _currentDynasty, prospect.Source,
                    prospect.ProspectId).ScoutingPercent;
                return scoutedFilter switch
                {
                    ScoutedFilter.Scouted => percent > 0,
                    ScoutedFilter.Unscouted => percent == 0,
                    ScoutedFilter.Partial => percent is > 0 and < 50,
                    ScoutedFilter.Mostly => percent is >= 50 and < 100,
                    ScoutedFilter.Fully => percent >= 100,
                    _ => true
                };
            }).ToList();
        }

        prospects = SortRecruitingProspects(
            prospects);

        var total = prospects.Count;
        var visible = prospects
            .Take(80)
            .ToArray();

        _recruitingResultsStatus.Text =
            total == 0
                ? GetEmptyRecruitingMessage()
                : total > visible.Length
                    ? $"Showing {visible.Length} of {total:N0} • use search/filters to narrow the list"
                    : $"{total:N0} players";

        foreach (var prospect in visible)
            AddRecruitingCompactRow(prospect);
    }

    private void NormalizeRecruitingViewForPhase()
    {
        if (_currentDynasty is null)
            return;

        if (_currentDynasty.Phase == SeasonPhase.TransferPortal)
        {
            return;
        }

        if (_currentDynasty.Phase ==
                SeasonPhase.Recruiting &&
            _recruitingView ==
                RecruitingView.TransferPortal &&
            _currentDynasty.TransferPortalEntries.Count == 0)
        {
            _recruitingView =
                RecruitingView.HighSchool;
            return;
        }

        if (_currentDynasty.Phase is
                (SeasonPhase.RosterManagement or SeasonPhase.Offseason) &&
            _recruitingView is
                RecruitingView.HighSchool or
                RecruitingView.TransferPortal)
        {
            _recruitingView =
                RecruitingView.Commitments;
        }
    }

    private void AddRecruitingMetric(
        string label,
        string value)
    {
        _recruitingMetrics.Children.Add(
            new Border
            {
                StrokeThickness = 1,
                Padding = new Thickness(10, 5),
                Content = new VerticalStackLayout
                {
                    Spacing = 0,
                    Children =
                    {
                        new Label
                        {
                            Text = label,
                            FontSize = 9,
                            Opacity = 0.7
                        },
                        new Label
                        {
                            Text = value,
                            FontAttributes =
                                FontAttributes.Bold,
                            FontSize = 15
                        }
                    }
                }
            });
    }

    private List<RecruitingProspectDisplay>
        GetRecruitingProspectsForCurrentView()
    {
        if (_currentDynasty is null)
            return new();

        if (_recruitingView ==
            RecruitingView.HighSchool)
        {
            return _currentDynasty
                .HighSchoolRecruitingPool
                .Where(recruit =>
                    recruit.SeasonYear == 0 ||
                    recruit.SeasonYear ==
                        _currentDynasty.SeasonYear)
                .Select(recruit =>
                    new RecruitingProspectDisplay(
                        RecruitingSource.HighSchool,
                        recruit.RecruitId,
                        recruit.FullName,
                        recruit.Position,
                        $"{recruit.StarRating}★ • {RecruitGeography.GetStateName(recruit)} • {RecruitGeography.GetRegionName(recruit)}",
                        1,
                        recruit.PotentialRating,
                        recruit.StarRating,
                        recruit.TrueOverallRating))
                .ToList();
        }

        if (_recruitingView ==
            RecruitingView.TransferPortal)
        {
            return _currentDynasty
                .TransferPortalEntries
                .Where(entry =>
                    !entry.OriginTeamName.Equals(
                        _currentDynasty.UserTeamName,
                        StringComparison.OrdinalIgnoreCase))
                .Select(entry =>
                    new RecruitingProspectDisplay(
                        RecruitingSource.TransferPortal,
                        entry.Player.PlayerId,
                        entry.Player.FullName,
                        entry.Player.Position,
                        $"{entry.OriginTeamName} • {entry.WeeksInPortal} wk in portal",
                        entry.Player.ClassYear,
                        entry.Player.PotentialRating > 0
                            ? entry.Player.PotentialRating
                            : null,
                        null,
                        entry.Player.OverallRating))
                .ToList();
        }

        var board = new List<RecruitingProspectDisplay>();

        foreach (var interaction in
                 _currentDynasty.RecruitingInteractions
                     .Where(interaction =>
                         interaction.SeasonYear ==
                             _currentDynasty.SeasonYear &&
                         interaction.IsOnTargetBoard &&
                         interaction.CommittedTeamName is null))
        {
            var prospect =
                TryGetRecruitingProspect(
                    interaction.Source,
                    interaction.ProspectId);

            if (prospect is not null)
                board.Add(prospect);
        }

        return board;
    }

    private RecruitingProspectDisplay?
        TryGetRecruitingProspect(
            RecruitingSource source,
            Guid prospectId)
    {
        if (_currentDynasty is null)
            return null;

        if (source ==
            RecruitingSource.HighSchool)
        {
            var recruit =
                _currentDynasty.HighSchoolRecruitingPool
                    .FirstOrDefault(item =>
                        item.RecruitId == prospectId);

            return recruit is null
                ? null
                : new RecruitingProspectDisplay(
                    source,
                    recruit.RecruitId,
                    recruit.FullName,
                    recruit.Position,
                    $"{recruit.StarRating}★ • {RecruitGeography.GetStateName(recruit)} • {RecruitGeography.GetRegionName(recruit)}",
                    1,
                    recruit.PotentialRating,
                    recruit.StarRating,
                    recruit.TrueOverallRating);
        }

        var entry =
            _currentDynasty.TransferPortalEntries
                .FirstOrDefault(item =>
                    item.Player.PlayerId ==
                        prospectId);

        return entry is null
            ? null
            : new RecruitingProspectDisplay(
                source,
                entry.Player.PlayerId,
                entry.Player.FullName,
                entry.Player.Position,
                $"{entry.OriginTeamName} • {entry.WeeksInPortal} wk in portal",
                entry.Player.ClassYear,
                entry.Player.PotentialRating > 0
                    ? entry.Player.PotentialRating
                    : null,
                null,
                entry.Player.OverallRating);
    }

    private List<RecruitingProspectDisplay>
        SortRecruitingProspects(
            List<RecruitingProspectDisplay> prospects)
    {
        if (_currentDynasty is null)
            return prospects;

        var sort = (RecruitingSort)Math.Clamp(
            _recruitingSortPicker.SelectedIndex,
            0,
            Enum.GetValues<RecruitingSort>().Length - 1);

        IEnumerable<RecruitingProspectDisplay> query =
            sort switch
            {
                RecruitingSort.Interest =>
                    prospects.OrderByDescending(
                            prospect =>
                                InteractiveRecruitingService
                                    .GetInteraction(
                                        _currentDynasty,
                                        prospect.Source,
                                        prospect.ProspectId)
                                    .UserInterest)
                        .ThenByDescending(
                            prospect =>
                                prospect.OverallRating),

                RecruitingSort.Position =>
                    prospects.OrderBy(
                            prospect =>
                                prospect.Position)
                        .ThenByDescending(
                            prospect =>
                                prospect.OverallRating),

                RecruitingSort.Name =>
                    prospects.OrderBy(
                        prospect =>
                            prospect.FullName,
                        StringComparer.OrdinalIgnoreCase),

                _ =>
                    prospects.OrderByDescending(
                            prospect =>
                                prospect.OverallRating)
                        .ThenByDescending(
                            prospect =>
                                prospect.StarRating ?? 0)
            };

        return query.ToList();
    }

    private string GetEmptyRecruitingMessage() =>
        _recruitingView switch
        {
            RecruitingView.Board =>
                "Target board is empty. Open a player and choose Add to Board.",
            RecruitingView.HighSchool =>
                "No high-school recruits match these filters.",
            RecruitingView.TransferPortal =>
                "No available transfers match these filters.",
            _ => "No results."
        };

    private void AddRecruitingCompactRow(
        RecruitingProspectDisplay prospect)
    {
        if (_currentDynasty is null)
            return;

        var interaction =
            InteractiveRecruitingService.GetInteraction(
                _currentDynasty,
                prospect.Source,
                prospect.ProspectId);

        var range =
            InteractiveRecruitingService.GetScoutedOverallRange(
                _currentDynasty,
                prospect.Source,
                prospect.ProspectId);

        var overallText =
            range.Minimum == range.Maximum
                ? $"{range.Minimum} OVR"
                : $"{range.Minimum}-{range.Maximum} OVR";

        var boardMarker =
            interaction.IsOnTargetBoard
                ? "★ "
                : string.Empty;

        var offerMarker =
            interaction.ScholarshipOffered
                ? interaction.WasCpuAssisted
                    ? "CPU ASSIST • OFFER • "
                    : "OFFER • "
                : interaction.WasCpuAssisted
                    ? "CPU ASSIST • "
                    : string.Empty;

        var nationalRank = GetProspectNationalRank(prospect);

        var interest =
            interaction.CommittedTeamName is not null
                ? $"Committed: {interaction.CommittedTeamName}"
                : $"{offerMarker}Interest {interaction.UserInterest} / {interaction.RivalInterest}";

        var viewButton = new Button
        {
            Text = "View",
            FontSize = 11,
            Padding = new Thickness(12, 5),
            TextColor = Colors.White
        };

        viewButton.Clicked += (_, _) =>
        {
            _selectedRecruitSource =
                prospect.Source;
            _selectedRecruitId =
                prospect.ProspectId;
            RenderRecruitingScreen();
        };

        var boardButton = new Button
        {
            Text = interaction.IsOnTargetBoard ? "On Board" : "Add",
            FontSize = 11,
            Padding = new Thickness(10, 5),
            TextColor = Colors.White,
            IsEnabled = interaction.CommittedTeamName is null &&
                        !interaction.IsOnTargetBoard
        };
        boardButton.Clicked += (_, _) =>
        {
            if (_currentDynasty is null)
                return;
            _currentDynasty = InteractiveRecruitingService.ToggleTargetBoard(
                _currentDynasty, prospect.Source, prospect.ProspectId);
            RenderCurrentDynasty();
        };

        var quickOfferButton = new Button
        {
            Text = interaction.ScholarshipOffered ? "Withdraw" : "Offer",
            FontSize = 11,
            Padding = new Thickness(10, 5),
            TextColor = Colors.White,
            IsVisible = _recruitingView == RecruitingView.Board,
            IsEnabled =
                interaction.CommittedTeamName is null &&
                IsRecruitingOpen(prospect.Source)
        };
        quickOfferButton.Clicked += (_, _) =>
        {
            if (_currentDynasty is null)
                return;

            _currentDynasty = InteractiveRecruitingService.ToggleScholarship(
                _currentDynasty, prospect.Source, prospect.ProspectId);
            RenderCurrentDynasty();
        };

        var info = new VerticalStackLayout
        {
            Spacing = 1,
            Children =
            {
                new Label
                {
                    Text =
                        $"{boardMarker}{prospect.Position} {prospect.FullName}",
                    FontAttributes =
                        FontAttributes.Bold,
                    FontSize = 14
                },
                new Label
                {
                    Text =
                        $"#{nationalRank} {prospect.Source} • {overallText} • " +
                        $"{prospect.Subtitle} • " +
                        $"Scout {interaction.ScoutingPercent}%",
                    FontSize = 11,
                    Opacity = 0.82
                },
                new Label
                {
                    Text = interest,
                    FontSize = 11,
                    Opacity = 0.82
                }
            }
        };

        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition
                {
                    Width = GridLength.Star
                },
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                },
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                },
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                }
            },
            ColumnSpacing = 8
        };

        row.Add(info);
        Grid.SetColumn(info, 0);

        row.Add(viewButton);
        Grid.SetColumn(viewButton, 1);
        row.Add(boardButton);
        Grid.SetColumn(boardButton, 2);

        if (_recruitingView == RecruitingView.Board)
        {
            row.Add(quickOfferButton);
            Grid.SetColumn(quickOfferButton, 3);
        }

        _recruitingList.Children.Add(
            new Border
            {
                StrokeThickness = 1,
                Padding = new Thickness(10, 7),
                Content = row
            });
    }

    private int GetProspectNationalRank(
        RecruitingProspectDisplay prospect)
    {
        if (_currentDynasty is null)
            return 0;

        IEnumerable<Guid> rankedIds = prospect.Source ==
            RecruitingSource.HighSchool
            ? _currentDynasty.HighSchoolRecruitingPool
                .Where(recruit => recruit.SeasonYear == 0 ||
                    recruit.SeasonYear == _currentDynasty.SeasonYear)
                .OrderByDescending(recruit => recruit.StarRating)
                .ThenByDescending(recruit => recruit.TrueOverallRating)
                .ThenByDescending(recruit => recruit.PotentialRating)
                .ThenBy(recruit => recruit.FullName,
                    StringComparer.OrdinalIgnoreCase)
                .Select(recruit => recruit.RecruitId)
            : _currentDynasty.TransferPortalEntries
                .OrderByDescending(entry => entry.Player.OverallRating)
                .ThenByDescending(entry => entry.Player.PotentialRating)
                .ThenBy(entry => entry.Player.FullName,
                    StringComparer.OrdinalIgnoreCase)
                .Select(entry => entry.Player.PlayerId);

        var rank = 1;
        foreach (var id in rankedIds)
        {
            if (id == prospect.ProspectId)
                return rank;
            rank++;
        }

        return rank;
    }

    private void RenderSelectedRecruitDetail()
    {
        if (_currentDynasty is null ||
            _selectedRecruitSource is null ||
            _selectedRecruitId is null)
        {
            _recruitingDetailHost.Content = null;
            _recruitingDetailHost.IsVisible = false;
            return;
        }

        var source =
            _selectedRecruitSource.Value;
        var prospect =
            TryGetRecruitingProspect(
                source,
                _selectedRecruitId.Value);

        if (prospect is null)
        {
            _selectedRecruitSource = null;
            _selectedRecruitId = null;
            _recruitingDetailHost.Content = null;
            _recruitingDetailHost.IsVisible = false;
            return;
        }

        var interaction =
            InteractiveRecruitingService.GetInteraction(
                _currentDynasty,
                source,
                prospect.ProspectId);

        var recruitingOpen = IsRecruitingOpen(source);

        var range =
            InteractiveRecruitingService.GetScoutedOverallRange(
                _currentDynasty,
                source,
                prospect.ProspectId);

        var revealedPreferences =
            RecruitPreferenceService.GetRevealedPreferences(
                _currentDynasty,
                source,
                prospect.ProspectId);

        var boardButton = new Button
        {
            Text = interaction.IsOnTargetBoard
                ? "Remove from Board"
                : "Add to Board",
            FontSize = 11,
            TextColor = Colors.White
        };

        boardButton.Clicked += (_, _) =>
        {
            if (_currentDynasty is null)
                return;

            _currentDynasty =
                InteractiveRecruitingService
                    .ToggleTargetBoard(
                        _currentDynasty,
                        source,
                        prospect.ProspectId);

            RenderCurrentDynasty();
        };

        var scoutButton = new Button
        {
            Text =
                $"Extra Review ({InteractiveRecruitingService.ScoutCost})",
            FontSize = 11,
            TextColor = Colors.White,
            IsEnabled =
                recruitingOpen &&
                interaction.CommittedTeamName is null &&
                interaction.ScoutingPercent < 100 &&
                _currentDynasty.RecruitingPointsRemaining >=
                    InteractiveRecruitingService.ScoutCost
        };

        scoutButton.Clicked += (_, _) =>
        {
            if (_currentDynasty is null)
                return;

            _currentDynasty =
                InteractiveRecruitingService.Scout(
                    _currentDynasty,
                    source,
                    prospect.ProspectId);

            RenderCurrentDynasty();
        };

        var priorityButton = new Button
        {
            Text = interaction.IsPriorityScout
                ? "Priority On"
                : "Priority Scout",
            FontSize = 11,
            TextColor = Colors.White,
            IsEnabled = interaction.CommittedTeamName is null
        };
        priorityButton.Clicked += (_, _) =>
        {
            if (_currentDynasty is null)
                return;
            _currentDynasty = ScoutingDepartmentService.TogglePriority(
                _currentDynasty, source, prospect.ProspectId);
            RenderCurrentDynasty();
        };

        var offerButton = new Button
        {
            Text = interaction.ScholarshipOffered
                ? "Withdraw Offer"
                : "Offer Scholarship",
            FontSize = 11,
            TextColor = Colors.White,
            IsEnabled =
                recruitingOpen &&
                interaction.CommittedTeamName is null
        };

        offerButton.Clicked += (_, _) =>
        {
            if (_currentDynasty is null)
                return;

            _currentDynasty =
                InteractiveRecruitingService
                    .ToggleScholarship(
                        _currentDynasty,
                        source,
                        prospect.ProspectId);

            RenderCurrentDynasty();
        };

        var pitchPicker = new Picker
        {
            Title = "Choose pitch",
            ItemsSource = Enum.GetValues<RecruitPitchType>()
                .Select(FormatPitchType)
                .ToList(),
            SelectedIndex = interaction.LastPitchType is not null
                ? (int)interaction.LastPitchType.Value
                : revealedPreferences.Count > 0
                    ? (int)revealedPreferences[0].Type
                    : (int)RecruitPitchType.ProgramPrestige
        };

        var pitchButton = new Button
        {
            Text =
                $"Pitch ({InteractiveRecruitingService.PitchCost})",
            FontSize = 11,
            TextColor = Colors.White,
            IsEnabled =
                recruitingOpen &&
                interaction.CommittedTeamName is null &&
                _currentDynasty.RecruitingPointsRemaining >=
                    InteractiveRecruitingService.PitchCost
        };

        pitchButton.Clicked += (_, _) =>
        {
            if (_currentDynasty is null ||
                !_teamsByName.TryGetValue(
                    _currentDynasty.UserTeamName,
                    out var userTeam))
            {
                return;
            }

            var pitch = (RecruitPitchType)Math.Clamp(
                pitchPicker.SelectedIndex,
                0,
                Enum.GetValues<RecruitPitchType>().Length - 1);

            _currentDynasty =
                InteractiveRecruitingService.Pitch(
                    _currentDynasty,
                    userTeam,
                    source,
                    prospect.ProspectId,
                    pitch);

            RenderCurrentDynasty();
        };

        var closeButton = new Button
        {
            Text = "Close",
            FontSize = 11,
            Padding = new Thickness(10, 5)
        };
        closeButton.Clicked += (_, _) =>
        {
            _selectedRecruitSource = null;
            _selectedRecruitId = null;
            RenderRecruitingScreen();
        };

        var preferenceLayout =
            new VerticalStackLayout
            {
                Spacing = 2
            };

        if (revealedPreferences.Count == 0)
        {
            preferenceLayout.Children.Add(
                new Label
                {
                    Text =
                        "Scout to reveal what matters to this player. " +
                        "25% scouting reveals the top priority.",
                    FontSize = 11,
                    Opacity = 0.8
                });
        }
        else if (_teamsByName.TryGetValue(
                     _currentDynasty.UserTeamName,
                     out var userTeam))
        {
            foreach (var preference in
                     revealedPreferences)
            {
                var grade =
                    RecruitPreferenceService.GetProgramGrade(
                        _currentDynasty,
                        userTeam,
                        source,
                        prospect.ProspectId,
                        preference.Type);

                preferenceLayout.Children.Add(
                    new Label
                    {
                        Text =
                            $"{FormatPitchType(preference.Type)}: " +
                            $"{RecruitPreferenceService.GetImportanceLabel(preference.Importance)} • " +
                            $"{userTeam.Name} {RecruitPreferenceService.GetGradeLabel(grade)}",
                        FontSize = 11
                    });
            }
        }

        var actions = new FlexLayout
        {
            Direction = FlexDirection.Row,
            Wrap = FlexWrap.Wrap,
            AlignItems = FlexAlignItems.Center,
            JustifyContent = FlexJustify.Start
        };

        foreach (var button in new[]
                 {
                     boardButton,
                     priorityButton,
                     scoutButton,
                     offerButton
                 })
        {
            button.Margin =
                new Thickness(0, 0, 6, 6);
            actions.Children.Add(button);
        }

        pitchPicker.Margin =
            new Thickness(0, 0, 6, 6);
        pitchPicker.WidthRequest = 170;
        actions.Children.Add(pitchPicker);

        pitchButton.Margin =
            new Thickness(0, 0, 6, 6);
        actions.Children.Add(pitchButton);

        closeButton.Margin =
            new Thickness(0, 0, 6, 6);
        actions.Children.Add(closeButton);

        var potentialRange = InteractiveRecruitingService
            .GetScoutedPotentialRange(_currentDynasty, source,
                prospect.ProspectId);
        var potText = interaction.ScoutingPercent >= 50
            ? $" • POT {potentialRange.Minimum}-{potentialRange.Maximum} est."
            : " • POT unknown";

        var evaluationText = string.Empty;
        if (source == RecruitingSource.HighSchool &&
            interaction.ScoutingPercent >= 75)
        {
            var recruit = _currentDynasty.HighSchoolRecruitingPool
                .First(item => item.RecruitId == prospect.ProspectId);
            evaluationText = ScoutingDepartmentService.IsSleeper(recruit)
                ? " • SLEEPER"
                : ScoutingDepartmentService.HasBustRisk(recruit)
                    ? " • BUST RISK"
                    : string.Empty;
        }

        var lastPitchText =
            interaction.LastPitchType is not null
                ? $" • Last pitch: {FormatPitchType(interaction.LastPitchType.Value)}"
                : string.Empty;

        _recruitingDetailHost.Content =
            new Border
            {
                StrokeThickness = 1,
                Padding = 10,
                Content = new VerticalStackLayout
                {
                    Spacing = 5,
                    Children =
                    {
                        new Label
                        {
                            Text =
                                $"{prospect.Position} {prospect.FullName}",
                            FontAttributes =
                                FontAttributes.Bold,
                            FontSize = 17
                        },
                        new Label
                        {
                            Text =
                                $"{prospect.Subtitle} • Year {prospect.ClassYear}",
                            FontSize = 11
                        },
                        new Label
                        {
                            Text =
                                $"OVR {(range.Minimum == range.Maximum ? range.Minimum.ToString() : $"{range.Minimum}-{range.Maximum} est.")}{potText} • " +
                                $"Scout {interaction.ScoutingPercent}% • " +
                                $"Interest {interaction.UserInterest} vs {interaction.RivalInterest}" +
                                evaluationText +
                                lastPitchText,
                            FontSize = 12
                        },
                        new Label
                        {
                            Text = recruitingOpen
                                ? "Recruiting actions are available."
                                : "Recruiting actions are closed in the current phase.",
                            FontSize = 10,
                            Opacity = 0.72
                        },
                        new Label
                        {
                            Text = "REVEALED PRIORITIES",
                            FontAttributes =
                                FontAttributes.Bold,
                            FontSize = 11
                        },
                        preferenceLayout,
                        actions
                    }
                }
            };

        _recruitingDetailHost.IsVisible = true;
    }

    private void RenderScoutingDepartment()
    {
        if (_currentDynasty is null)
            return;

        _recruitingDetailHost.Content = null;
        _recruitingDetailHost.IsVisible = false;
        _recruitingResultsStatus.Text =
            "Scouts work automatically each regular-season week. Priority prospects are evaluated first.";

        var assignmentChoices = new[]
        {
            "National", "Team Needs", "Offense", "Defense", "Special Teams"
        }.Concat(RecruitGeography.RegionNames.Select(name => $"Region: {name}"))
         .Concat(RecruitGeography.StateNames.Select(name => $"State: {name}"))
         .Concat(Enum.GetValues<Position>().Select(position => $"Position: {position}"))
         .ToList();

        foreach (var scout in _currentDynasty.ScoutingStaff)
        {
            var assignment = _currentDynasty.ScoutAssignments.First(item =>
                item.ScoutId == scout.ScoutId);
            var assignmentText = FormatScoutAssignment(assignment);
            var picker = new Picker
            {
                Title = "Assignment",
                ItemsSource = assignmentChoices,
                SelectedIndex = Math.Max(0, assignmentChoices.IndexOf(assignmentText))
            };
            picker.SelectedIndexChanged += (_, _) =>
            {
                if (_currentDynasty is null || picker.SelectedItem is not string selected)
                    return;
                var parsed = ParseScoutAssignment(selected);
                _currentDynasty = ScoutingDepartmentService.SetAssignment(
                    _currentDynasty, scout.ScoutId, parsed.Scope, parsed.Target);
                RenderCurrentDynasty();
            };

            _recruitingList.Children.Add(new Border
            {
                StrokeThickness = 1,
                Padding = new Thickness(10, 8),
                Content = new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        new Label
                        {
                            Text = scout.FullName,
                            FontAttributes = FontAttributes.Bold,
                            FontSize = 15
                        },
                        new Label
                        {
                            Text = $"Talent {scout.TalentEvaluation} • Potential {scout.PotentialEvaluation} • " +
                                   $"Regional {scout.RegionalKnowledge} • Capacity {scout.WorkRate}",
                            FontSize = 11
                        },
                        picker
                    }
                }
            });
        }

        var report = _currentDynasty.ScoutingRecommendationReport;
        if (report?.SeasonYear != _currentDynasty.SeasonYear)
            return;

        _recruitingList.Children.Add(new Label
        {
            Text = "SCOUTING DEPARTMENT RECOMMENDATIONS",
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 10, 0, 2)
        });

        foreach (var section in Enum.GetValues<ScoutingRecommendationSection>())
        {
            var recommendations = report.Recommendations
                .Where(item => item.Section == section).ToArray();
            if (recommendations.Length == 0)
                continue;
            _recruitingList.Children.Add(new Label
            {
                Text = string.Concat(section.ToString().Select((character, index) =>
                    index > 0 && char.IsUpper(character) ? $" {character}" : character.ToString())),
                FontAttributes = FontAttributes.Bold,
                FontSize = 13
            });
            foreach (var item in recommendations)
            {
                var interaction = InteractiveRecruitingService.GetInteraction(
                    _currentDynasty, item.Source, item.ProspectId);
                var viewButton = new Button
                {
                    Text = "View",
                    FontSize = 11,
                    Padding = new Thickness(12, 5),
                    TextColor = Colors.White
                };
                viewButton.Clicked += (_, _) =>
                {
                    _recruitingView = item.Source == RecruitingSource.HighSchool
                        ? RecruitingView.HighSchool
                        : RecruitingView.TransferPortal;
                    _selectedRecruitSource = item.Source;
                    _selectedRecruitId = item.ProspectId;
                    RenderRecruitingScreen();
                };

                var offerButton = new Button
                {
                    Text = interaction.ScholarshipOffered ? "Offered" : "Offer",
                    FontSize = 11,
                    Padding = new Thickness(12, 5),
                    TextColor = Colors.White,
                    IsEnabled = !interaction.ScholarshipOffered &&
                                interaction.CommittedTeamName is null &&
                                IsRecruitingOpen(item.Source)
                };
                offerButton.Clicked += (_, _) =>
                {
                    if (_currentDynasty is null)
                        return;
                    _currentDynasty = InteractiveRecruitingService
                        .ToggleScholarship(_currentDynasty, item.Source,
                            item.ProspectId);
                    RenderCurrentDynasty();
                };

                var actions = new HorizontalStackLayout
                {
                    Spacing = 6,
                    Children = { viewButton, offerButton }
                };
                _recruitingList.Children.Add(new Border
                {
                    StrokeThickness = 1,
                    Padding = new Thickness(10, 7),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 5,
                        Children =
                        {
                            new Label
                            {
                                Text = $"{item.Position} {item.PlayerName}",
                                FontAttributes = FontAttributes.Bold,
                                FontSize = 13
                            },
                            new Label
                            {
                                Text = item.Summary,
                                FontSize = 11,
                                Opacity = 0.8
                            },
                            actions
                        }
                    }
                });
            }
        }
    }

    private bool IsRecruitingOpen(RecruitingSource source)
    {
        if (_currentDynasty is null)
            return false;

        return source == RecruitingSource.HighSchool
            ? _currentDynasty.Phase is
                SeasonPhase.TransferPortal or SeasonPhase.Recruiting
            : _currentDynasty.Phase is
                SeasonPhase.TransferPortal or SeasonPhase.Recruiting or
                SeasonPhase.RegularSeason;
    }

    private static string FormatScoutAssignment(ScoutAssignment assignment) =>
        assignment.Scope switch
        {
            ScoutAssignmentScope.TeamNeeds => "Team Needs",
            ScoutAssignmentScope.SpecialTeams => "Special Teams",
            ScoutAssignmentScope.Region when int.TryParse(assignment.Target, out var regionIndex) =>
                $"Region: {RecruitGeography.RegionNames[Math.Clamp(regionIndex, 0, 4)]}",
            ScoutAssignmentScope.State when int.TryParse(assignment.Target, out var stateIndex) =>
                $"State: {RecruitGeography.StateNames[Math.Clamp(stateIndex, 0, 49)]}",
            ScoutAssignmentScope.Position => $"Position: {assignment.Target}",
            _ => assignment.Scope.ToString()
        };

    private static (ScoutAssignmentScope Scope, string? Target)
        ParseScoutAssignment(string value)
    {
        if (value.StartsWith("Region: ", StringComparison.Ordinal))
            return (ScoutAssignmentScope.Region,
                RecruitGeography.RegionNames.ToList().IndexOf(value[8..]).ToString());
        if (value.StartsWith("State: ", StringComparison.Ordinal))
            return (ScoutAssignmentScope.State,
                RecruitGeography.StateNames.ToList().IndexOf(value[7..]).ToString());
        if (value.StartsWith("Position: ", StringComparison.Ordinal))
            return (ScoutAssignmentScope.Position, value[10..]);
        return value switch
        {
            "Team Needs" => (ScoutAssignmentScope.TeamNeeds, null),
            "Offense" => (ScoutAssignmentScope.Offense, null),
            "Defense" => (ScoutAssignmentScope.Defense, null),
            "Special Teams" => (ScoutAssignmentScope.SpecialTeams, null),
            _ => (ScoutAssignmentScope.National, null)
        };
    }

    private void RenderRecruitingCommitments()
    {
        if (_currentDynasty is null)
            return;

        _recruitingDetailHost.Content = null;
        _recruitingDetailHost.IsVisible = false;

        var allCommitments = _currentDynasty.RecruitingCommitments
            .Where(record => record.SeasonYear == _currentDynasty.SeasonYear)
            .ToArray();
        var userCommitments = allCommitments
            .Where(record => record.TeamName.Equals(
                _currentDynasty.UserTeamName,
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(record => record.OverallRating)
            .ToArray();

        _recruitingResultsStatus.Text = allCommitments.Length == 0
            ? "No commitments yet. Rankings update as players commit."
            : $"{allCommitments.Length:N0} national commitments • " +
              $"{userCommitments.Length:N0} to your team";

        var classRankings = allCommitments
            .GroupBy(record => record.TeamName,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                TeamName = group.Key,
                Count = group.Count(),
                PortalCount = group.Count(record =>
                    record.Source == RecruitingSource.TransferPortal),
                Score = group.Sum(record =>
                    record.OverallRating * record.OverallRating / 10.0)
            })
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Count)
            .ThenBy(item => item.TeamName,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _recruitingList.Children.Add(new Label
        {
            Text = "RECRUITING CLASS RANKINGS",
            FontAttributes = FontAttributes.Bold
        });

        foreach (var item in classRankings.Take(25).Select((item, index) =>
                     new { Item = item, Rank = index + 1 }))
        {
            _recruitingList.Children.Add(new Label
            {
                Text = $"#{item.Rank} {item.Item.TeamName} • " +
                       $"{item.Item.Count} commits • " +
                       $"{item.Item.PortalCount} transfers",
                FontAttributes = item.Item.TeamName.Equals(
                    _currentDynasty.UserTeamName,
                    StringComparison.OrdinalIgnoreCase)
                    ? FontAttributes.Bold
                    : FontAttributes.None
            });
        }

        var userRank = Array.FindIndex(classRankings, item =>
            item.TeamName.Equals(_currentDynasty.UserTeamName,
                StringComparison.OrdinalIgnoreCase));
        if (userRank >= 25)
        {
            var item = classRankings[userRank];
            _recruitingList.Children.Add(new Label
            {
                Text = $"#{userRank + 1} {item.TeamName} • " +
                       $"{item.Count} commits • {item.PortalCount} transfers",
                FontAttributes = FontAttributes.Bold
            });
        }

        _recruitingList.Children.Add(new BoxView { HeightRequest = 1 });
        _recruitingList.Children.Add(new Label
        {
            Text = "YOUR COMMITMENTS",
            FontAttributes = FontAttributes.Bold
        });
        foreach (var commitment in userCommitments)
            AddCommitmentRow(commitment, false);

        _recruitingList.Children.Add(new BoxView { HeightRequest = 1 });
        _recruitingList.Children.Add(new Label
        {
            Text = "AROUND THE NATION",
            FontAttributes = FontAttributes.Bold
        });
        foreach (var commitment in allCommitments
                     .Where(record => !record.TeamName.Equals(
                         _currentDynasty.UserTeamName,
                         StringComparison.OrdinalIgnoreCase))
                     .OrderByDescending(record => record.OverallRating)
                     .ThenBy(record => record.TeamName,
                         StringComparer.OrdinalIgnoreCase)
                     .Take(100))
        {
            AddCommitmentRow(commitment, true);
        }
    }

    private void AddCommitmentRow(
        RecruitingCommitmentRecord commitment,
        bool showTeam)
    {
        var joinText = commitment.JoinSeasonYear > commitment.SeasonYear
            ? $" • joins {commitment.JoinSeasonYear}"
            : string.Empty;
        var teamText = showTeam ? $" • {commitment.TeamName}" : string.Empty;

        _recruitingList.Children.Add(new Border
        {
            StrokeThickness = 1,
            Padding = new Thickness(10, 7),
            Content = new Label
            {
                Text = $"{commitment.Position} {commitment.PlayerName} • " +
                       $"OVR {commitment.OverallRating} • {commitment.Source}" +
                       teamText + joinText +
                       (commitment.WasCpuAssisted ? " • CPU ASSIST" : string.Empty),
                FontAttributes = FontAttributes.Bold
            }
        });
    }

    private static string FormatPitchType(
        RecruitPitchType type) =>
        type switch
        {
            RecruitPitchType.PlayingTime =>
                "Playing Time",
            RecruitPitchType.ProgramPrestige =>
                "Program Prestige",
            RecruitPitchType.ProPotential =>
                "Pro Potential",
            RecruitPitchType.Development =>
                "Development",
            RecruitPitchType.SchemeFit =>
                "Scheme Fit",
            RecruitPitchType.Proximity =>
                "Location",
            RecruitPitchType.Facilities =>
                "Facilities",
            RecruitPitchType.Academics =>
                "Academics",
            _ => type.ToString()
        };

    private void RenderHomeScreen()
    {
        _homeHighlights.Children.Clear();

        if (_currentDynasty is null)
        {
            _homeStatus.Text = "Create or load a dynasty to begin.";
            return;
        }

        var rankings = NationalRankingService.Build(
            _currentDynasty,
            _teamsByName,
            BuildCurrentSimulationProfiles());

        var userRanking = rankings.FirstOrDefault(ranking =>
            ranking.TeamName.Equals(
                _currentDynasty.UserTeamName,
                StringComparison.OrdinalIgnoreCase));

        var nextGame = _currentDynasty.Schedule
            .Where(game =>
                game.SeasonYear == _currentDynasty.SeasonYear &&
                game.InvolvesTeam(_currentDynasty.UserTeamName) &&
                !game.HasPlayed)
            .OrderBy(game => game.Week)
            .FirstOrDefault();

        var injuries = _currentDynasty.ActiveRoster
            .Where(player =>
                player.TeamName.Equals(
                    _currentDynasty.UserTeamName,
                    StringComparison.OrdinalIgnoreCase) &&
                player.CurrentInjury is not null)
            .ToArray();

        _homeStatus.Text =
            $"{_currentDynasty.SeasonYear} • {_currentDynasty.Phase} • " +
            (userRanking is null
                ? "Unranked"
                : userRanking.Rank <= 25
                    ? $"#{userRanking.Rank} nationally"
                    : $"NR ({userRanking.Rank})");

        _homeHighlights.Children.Add(new Border
        {
            StrokeThickness = 1,
            Padding = 12,
            Content = new Label
            {
                Text = nextGame is null
                    ? "NEXT GAME\nNo upcoming game currently scheduled."
                    : $"NEXT GAME\nWeek {nextGame.Week}: " +
                      $"{(nextGame.HomeTeamName.Equals(_currentDynasty.UserTeamName, StringComparison.OrdinalIgnoreCase) ? "vs" : "@")} " +
                      $"{(nextGame.HomeTeamName.Equals(_currentDynasty.UserTeamName, StringComparison.OrdinalIgnoreCase) ? nextGame.AwayTeamName : nextGame.HomeTeamName)}",
                FontAttributes = FontAttributes.Bold
            }
        });

        _homeHighlights.Children.Add(new Border
        {
            StrokeThickness = 1,
            Padding = 12,
            Content = new Label
            {
                Text = injuries.Length == 0
                    ? "INJURIES\nNo current injuries."
                    : $"INJURIES\n{injuries.Length} active • " +
                      string.Join(
                          " • ",
                          injuries.Take(3).Select(player =>
                              $"{player.Position} {player.FullName} " +
                              $"{player.CurrentInjury!.WeeksRemaining}wk"))
            }
        });

        if (_currentDynasty.UserCoachIsFired)
        {
            _homeHighlights.Children.Add(new Border
            {
                StrokeThickness = 1,
                Padding = 12,
                Content = new Label
                {
                    Text =
                        "ACTION REQUIRED\nYou were fired. Open Program and accept a new head-coaching job.",
                    FontAttributes = FontAttributes.Bold,
                    TextColor = NegativeChangeColor
                }
            });
        }

        if (_currentDynasty.Phase is
            SeasonPhase.TransferPortal or
            SeasonPhase.Recruiting or
            SeasonPhase.RosterManagement)
        {
            _homeHighlights.Children.Add(new Border
            {
                StrokeThickness = 1,
                Padding = 12,
                Content = new Label
                {
                    Text =
                        $"ACTION REQUIRED\n{_currentDynasty.Phase}: " +
                        "open Recruiting or Roster to review the offseason before advancing.",
                    FontAttributes = FontAttributes.Bold
                }
            });
        }
    }

    private void RenderProgramScreen()
    {
        _programList.Children.Clear();

        if (_currentDynasty is null ||
            !_teamsByName.TryGetValue(
                _currentDynasty.UserTeamName,
                out var team))
        {
            _programStatus.Text =
                "No program data available.";
            return;
        }

        var currentPrestige = ProgramPrestigeService.GetCurrentPrestige(
            _currentDynasty, team);
        var latestPrestige = _currentDynasty.ProgramPrestigeHistory
            .Where(item => item.TeamName.Equals(
                team.Name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.SeasonYear)
            .FirstOrDefault();
        var prestigeChange = latestPrestige?.Change ?? 0;

        _programStatus.Text =
            $"{team.Name} • {team.ConferenceName} • Prestige {currentPrestige} " +
            $"({FormatSigned(prestigeChange)})";
        _programStatus.TextColor = GetChangeColor(prestigeChange);

        var titles = _currentDynasty.NationalChampionshipHistory
            .Where(record => record.ChampionTeamName.Equals(
                team.Name,
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(record => record.SeasonYear)
            .ToArray();

        var conferenceTitles = _currentDynasty.ConferenceChampionshipHistory
            .Where(record => record.ChampionTeamName.Equals(
                team.Name,
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(record => record.SeasonYear)
            .ToArray();

        _programList.Children.Add(new Label
        {
            Text =
                $"National titles: {titles.Length} • " +
                $"Conference titles: {conferenceTitles.Length}",
            FontAttributes = FontAttributes.Bold
        });

        RenderUserCoachCareer(team);
        RenderProgramStaff(team);
        RenderProgramHistory(team);

        var latestDevelopment = GetLatestUserDevelopmentRecords();
        if (latestDevelopment.Count > 0)
        {
            var developmentAverage = latestDevelopment.Average(record => record.OverallChange);
            _programList.Children.Add(new Label
            {
                Text =
                    $"Latest player development: " +
                    $"{FormatSigned(developmentAverage)} OVR average",
                TextColor = GetChangeColor(developmentAverage)
            });
        }

        if (titles.Length > 0)
        {
            _programList.Children.Add(new Label
            {
                Text = "NATIONAL CHAMPIONSHIPS",
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 8, 0, 0)
            });

            foreach (var title in titles.Take(10))
            {
                _programList.Children.Add(new Label
                {
                    Text =
                        $"{title.SeasonYear} • defeated {title.RunnerUpTeamName} " +
                        $"{title.ChampionScore}-{title.RunnerUpScore}"
                });
            }
        }

        if (conferenceTitles.Length > 0)
        {
            _programList.Children.Add(new Label
            {
                Text = "CONFERENCE CHAMPIONSHIPS",
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 8, 0, 0)
            });

            foreach (var title in conferenceTitles.Take(10))
            {
                _programList.Children.Add(new Label
                {
                    Text =
                        $"{title.SeasonYear} • {title.ConferenceName} • " +
                        $"defeated {title.RunnerUpTeamName} " +
                        $"{title.ChampionScore}-{title.RunnerUpScore}"
                });
            }
        }
    }

    private void RenderUserCoachCareer(Team team)
    {
        if (_currentDynasty is null ||
            _currentDynasty.UserHeadCoach is null)
        {
            return;
        }

        var coach = _currentDynasty.UserHeadCoach;
        var security =
            _currentDynasty.UserCoachJobSecurity;
        var securityLabel =
            UserCoachCareerService.GetJobSecurityLabel(
                security);
        var career = _currentDynasty.UserCoachCareerHistory;
        var wins = career.Sum(item => item.Wins);
        var losses = career.Sum(item => item.Losses);
        var titles = career.Count(item =>
            item.NationalChampion);
        var conferenceTitles = career.Count(item =>
            item.ConferenceChampion);

        _programList.Children.Add(new Label
        {
            Text = "HEAD COACH • YOU",
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 10, 0, 0),
            TextColor = _currentDynasty.UserCoachIsFired
                ? NegativeChangeColor
                : Colors.White
        });

        _programList.Children.Add(new Label
        {
            Text =
                $"OVR {coach.OverallRating} • REP {coach.Reputation} • " +
                $"Job Security {security}% ({securityLabel}) • " +
                $"Contract {coach.ContractYearsRemaining}y",
            FontAttributes = FontAttributes.Bold,
            TextColor = security switch
            {
                <= 20 => NegativeChangeColor,
                <= 35 => NegativeChangeColor,
                >= 70 => PositiveChangeColor,
                _ => NeutralChangeColor
            }
        });

        _programList.Children.Add(new Label
        {
            Text =
                $"Leadership {coach.Leadership} • Recruiting {coach.Recruiting} • " +
                $"Development {coach.PlayerDevelopment} • Game Mgmt {coach.GameManagement}",
            FontSize = 11
        });

        _programList.Children.Add(new Label
        {
            Text =
                $"Career {wins}-{losses} • Tenure {coach.TenureYears}y • " +
                $"National titles {titles} • Conference titles {conferenceTitles}",
            FontSize = 11
        });

        var latest = career
            .OrderByDescending(item => item.SeasonYear)
            .FirstOrDefault();
        if (latest is not null)
        {
            _programList.Children.Add(new Label
            {
                Text =
                    $"{latest.SeasonYear} COACH DEVELOPMENT • " +
                    $"Leadership {FormatSigned(latest.LeadershipChange)} • " +
                    $"Recruiting {FormatSigned(latest.RecruitingChange)} • " +
                    $"Development {FormatSigned(latest.PlayerDevelopmentChange)} • " +
                    $"Game Mgmt {FormatSigned(latest.GameManagementChange)}",
                FontSize = 11,
                TextColor = GetChangeColor(
                    latest.OverallAfter -
                    latest.OverallBefore)
            });
        }

        if (_currentDynasty.UserCoachIsFired)
        {
            _programList.Children.Add(new Border
            {
                StrokeThickness = 1,
                Padding = 10,
                Margin = new Thickness(0, 6, 0, 4),
                Content = new Label
                {
                    Text =
                        $"FIRED • {_currentDynasty.UserCoachFiredFromTeamName}\n" +
                        "Accept another head-coaching job before advancing the dynasty.",
                    FontAttributes = FontAttributes.Bold,
                    TextColor = NegativeChangeColor
                }
            });
        }

        var pendingOffers =
            _currentDynasty.UserCoachJobOffers
                .Where(item =>
                    item.Status ==
                        UserCoachJobOfferStatus.Pending)
                .OrderByDescending(item =>
                    item.ProgramPrestige)
                .ThenByDescending(item =>
                    item.FitScore)
                .ToArray();

        if (pendingOffers.Length == 0)
            return;

        _programList.Children.Add(new Label
        {
            Text = "HEAD COACH JOB OFFERS",
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 8, 0, 0)
        });

        foreach (var offer in pendingOffers)
        {
            var acceptButton = new Button
            {
                Text = "Accept",
                FontSize = 11,
                Padding = new Thickness(10, 4),
                TextColor = Colors.White
            };
            var declineButton = new Button
            {
                Text = "Decline",
                FontSize = 11,
                Padding = new Thickness(10, 4),
                TextColor = Colors.White
            };

            var selectedOffer = offer;
            acceptButton.Clicked += async (_, _) =>
            {
                if (_currentDynasty is null)
                    return;

                var accept = await DisplayAlert(
                    "Accept Head Coaching Job?",
                    $"Leave {_currentDynasty.UserTeamName} for {selectedOffer.TeamName}? " +
                    $"Prestige {selectedOffer.ProgramPrestige} • " +
                    $"{selectedOffer.ContractYears}-year contract.",
                    "Accept",
                    "Cancel");

                if (!accept || _currentDynasty is null)
                    return;

                _currentDynasty =
                    UserCoachCareerService.AcceptJobOffer(
                        _currentDynasty,
                        selectedOffer.OfferId,
                        _teamsByName);
                RenderCurrentDynasty();
                ShowSection(AppSection.Program);
            };

            declineButton.Clicked += (_, _) =>
            {
                if (_currentDynasty is null)
                    return;

                _currentDynasty =
                    UserCoachCareerService.DeclineJobOffer(
                        _currentDynasty,
                        selectedOffer.OfferId);
                RenderCurrentDynasty();
            };

            var actions = new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    acceptButton,
                    declineButton
                }
            };

            _programList.Children.Add(new Border
            {
                StrokeThickness = 1,
                Padding = 9,
                Content = new VerticalStackLayout
                {
                    Spacing = 3,
                    Children =
                    {
                        new Label
                        {
                            Text =
                                $"{offer.TeamName} • Prestige {offer.ProgramPrestige} • " +
                                $"{offer.ContractYears}y • Fit {offer.FitScore}",
                            FontAttributes =
                                FontAttributes.Bold
                        },
                        new Label
                        {
                            Text = offer.Reason,
                            FontSize = 11
                        },
                        actions
                    }
                }
            });
        }
    }

    private void RenderProgramStaff(Team team)
    {
        if (_currentDynasty is null)
            return;

        _programList.Children.Add(new Label
        {
            Text = "STAFF",
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 10, 0, 0)
        });
        _programList.Children.Add(new Label
        {
            Text = "You are the Head Coach • Manage the staff reporting to you below.",
            FontSize = 11,
            FontAttributes = FontAttributes.Italic
        });

        foreach (var role in Enum.GetValues<StaffRole>()
                     .Where(role => role != StaffRole.HeadCoach))
        {
            var staff = StaffManagementService.GetStaff(
                _currentDynasty, team.Name, role);

            var candidatesButton = new Button
            {
                Text = staff is null ? "Fill Vacancy" : "Candidates",
                FontSize = 11,
                Padding = new Thickness(10, 4),
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.End
            };
            var selectedRole = role;
            candidatesButton.Clicked += async (_, _) =>
                await OpenStaffMarketAsync(team, selectedRole);

            var detail = staff is null
                ? "VACANT • Select a candidate from the shared staff market."
                : GetStaffRoleDetail(staff);

            var status = role == StaffRole.HeadCoach && staff is not null
                ? $" • {StaffMarketService.GetHeadCoachStatus(_currentDynasty, team)}"
                : string.Empty;

            var title = staff is null
                ? $"{FormatStaffRole(role)} • VACANT"
                : $"{FormatStaffRole(role)} • {staff.FullName} • OVR {staff.OverallRating}{status}";

            var subtext = staff is null
                ? detail
                : $"{detail} • Age {staff.Age} • Upside {staff.GrowthPotential}/5 • " +
                  $"Tenure {staff.TenureYears}y • Contract {staff.ContractYearsRemaining}y";

            var textColor = staff is null
                ? NegativeChangeColor
                : role == StaffRole.HeadCoach &&
                  StaffMarketService.GetHeadCoachStatus(
                      _currentDynasty, team) == "Hot Seat"
                    ? NegativeChangeColor
                    : NeutralChangeColor;

            var info = new VerticalStackLayout
            {
                Spacing = 1,
                Children =
                {
                    new Label
                    {
                        Text = title,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 12,
                        TextColor = textColor
                    },
                    new Label
                    {
                        Text = subtext,
                        FontSize = 11
                    }
                }
            };

            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 8
            };
            row.Add(info);
            row.Add(candidatesButton);
            Grid.SetColumn(candidatesButton, 1);

            _programList.Children.Add(row);
        }

        RenderStaffCarouselHistory();
    }

    private static string GetStaffRoleDetail(StaffMember staff) =>
        staff.Role switch
        {
            StaffRole.ChiefScout =>
                $"EVAL {staff.TalentEvaluation}/{staff.PotentialEvaluation} • " +
                $"REG {staff.RegionalKnowledge} • MGMT {staff.StaffManagement}",
            StaffRole.MedicalTrainingDirector =>
                $"MED {staff.Medical} • LEAD {staff.Leadership}",
            StaffRole.StrengthConditioningDirector =>
                $"COND {staff.Conditioning} • DEV {staff.PlayerDevelopment}",
            StaffRole.SpecialTeamsCoordinator =>
                $"ST {staff.SpecialTeams} • DEV {staff.PlayerDevelopment} • GM {staff.GameManagement}",
            StaffRole.OffensiveCoordinator or StaffRole.DefensiveCoordinator =>
                $"SCHEME {staff.Scheme} • DEV {staff.PlayerDevelopment} • REC {staff.Recruiting}",
            _ =>
                $"LEAD {staff.Leadership} • DEV {staff.PlayerDevelopment} • " +
                $"REC {staff.Recruiting} • GM {staff.GameManagement}"
        };

    private async Task OpenStaffMarketAsync(
        Team team,
        StaffRole role)
    {
        if (_currentDynasty is null)
            return;

        _currentDynasty = StaffMarketService.EnsureMarket(
            _currentDynasty, _teamsByName);

        var candidates = StaffMarketService.GetCandidatesForTeam(
            _currentDynasty,
            team,
            role,
            9);

        if (candidates.Count == 0)
        {
            await DisplayAlert(
                "Staff Market",
                $"No available {FormatStaffRole(role)} candidates are currently interested.",
                "OK");
            return;
        }

        var choices = candidates
            .Select((candidate, index) =>
            {
                var fit = StaffMarketService.GetCandidateFitScore(
                    _currentDynasty, team, candidate);
                var source = candidate.Origin == StaffCandidateOrigin.FreeAgent
                    ? "Generated Free Agent"
                    : candidate.SourceTeamName ?? candidate.Origin.ToString();

                return $"{index + 1}. {candidate.Profile.FullName} • " +
                       $"OVR {StaffMarketService.GetProjectedOverall(candidate)} • Fit {fit} • " +
                       $"{candidate.StyleLabel} • {source}";
            })
            .ToArray();

        var selected = await DisplayActionSheet(
            $"{FormatStaffRole(role)} Candidates",
            "Cancel",
            null,
            choices);

        if (string.IsNullOrWhiteSpace(selected) ||
            selected.Equals("Cancel", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var selectedIndex = Array.IndexOf(choices, selected);
        if (selectedIndex < 0 || selectedIndex >= candidates.Count)
            return;

        var candidate = candidates[selectedIndex];
        var profile = candidate.Profile;
        var fitScore = StaffMarketService.GetCandidateFitScore(
            _currentDynasty, team, candidate);
        var prestige = ProgramPrestigeService.GetCurrentPrestige(
            _currentDynasty, team);
        var interest = prestige + 8 >= candidate.MinimumProgramPrestige
            ? "Interested"
            : "Long shot";
        var sourceText = candidate.Origin switch
        {
            StaffCandidateOrigin.FreeAgent => "Generated Free Agent",
            StaffCandidateOrigin.FiredStaff =>
                $"Previously at {candidate.SourceTeamName}",
            _ =>
                $"Currently {FormatStaffRole(candidate.SourceRole ?? profile.Role)} at " +
                $"{candidate.SourceTeamName}"
        };

        var hire = await DisplayAlert(
            $"Hire {profile.FullName}?",
            $"{FormatStaffRole(role)} • OVR {StaffMarketService.GetProjectedOverall(candidate)} • Fit {fitScore}\n" +
            $"{candidate.StyleLabel} • {sourceText}\n" +
            $"Reputation {profile.Reputation} • Upside {candidate.GrowthPotential}/5 • Age {profile.Age}\n" +
            $"{GetStaffCandidateDetail(profile, role)}\n" +
            $"Market interest: {interest} • Minimum prestige {candidate.MinimumProgramPrestige}\n\n" +
            "Hiring active staff can create a vacancy at their current school, which the CPU will refill from the same market.",
            "Hire",
            "Cancel");

        if (!hire || _currentDynasty is null)
            return;

        _currentDynasty = StaffMarketService.HireUserCandidate(
            _currentDynasty,
            team,
            candidate.CandidateId,
            _teamsByName);

        if (role == StaffRole.ChiefScout)
        {
            _currentDynasty = ScoutingDepartmentService.RebuildDepartment(
                _currentDynasty, team);
        }

        RenderCurrentDynasty();
    }

    private static string GetStaffCandidateDetail(
        StaffMember staff,
        StaffRole role) =>
        role switch
        {
            StaffRole.HeadCoach =>
                $"Leadership {staff.Leadership} • Recruiting {staff.Recruiting} • " +
                $"Development {staff.PlayerDevelopment} • Game Mgmt {staff.GameManagement}",
            StaffRole.OffensiveCoordinator or StaffRole.DefensiveCoordinator =>
                $"Scheme {staff.Scheme} • Development {staff.PlayerDevelopment} • " +
                $"Recruiting {staff.Recruiting} • Game Mgmt {staff.GameManagement}",
            StaffRole.SpecialTeamsCoordinator =>
                $"Special Teams {staff.SpecialTeams} • Development {staff.PlayerDevelopment} • " +
                $"Game Mgmt {staff.GameManagement}",
            StaffRole.MedicalTrainingDirector =>
                $"Medical {staff.Medical} • Leadership {staff.Leadership} • " +
                $"Development {staff.PlayerDevelopment}",
            StaffRole.StrengthConditioningDirector =>
                $"Conditioning {staff.Conditioning} • Development {staff.PlayerDevelopment} • " +
                $"Leadership {staff.Leadership}",
            StaffRole.ChiefScout =>
                $"Talent Eval {staff.TalentEvaluation} • Potential Eval {staff.PotentialEvaluation} • " +
                $"Regional {staff.RegionalKnowledge} • Management {staff.StaffManagement}",
            _ => $"OVR {staff.OverallRating}"
        };

    private void RenderStaffCarouselHistory()
    {
        if (_currentDynasty is null)
            return;

        var events = _currentDynasty.StaffMovementHistory
            .Where(item => item.SeasonYear == _currentDynasty.SeasonYear)
            .TakeLast(14)
            .Reverse()
            .ToArray();

        var generatedFreeAgents = _currentDynasty.StaffMarketCandidates.Count(item =>
            item.IsAvailable &&
            item.Origin == StaffCandidateOrigin.FreeAgent);
        var releasedCandidates = _currentDynasty.StaffMarketCandidates.Count(item =>
            item.IsAvailable &&
            item.Origin == StaffCandidateOrigin.FiredStaff);
        var activeCandidates = _currentDynasty.StaffMarketCandidates.Count(item =>
            item.IsAvailable &&
            item.Origin == StaffCandidateOrigin.ActiveStaff);

        if (generatedFreeAgents + releasedCandidates + activeCandidates > 0)
        {
            _programList.Children.Add(new Label
            {
                Text =
                    $"STAFF MARKET • {generatedFreeAgents} generated free agents • " +
                    $"{releasedCandidates} released staff • {activeCandidates} active/poachable",
                FontSize = 11,
                Margin = new Thickness(0, 8, 0, 0)
            });
        }

        if (events.Length == 0)
            return;

        _programList.Children.Add(new Label
        {
            Text = "COACHING CAROUSEL",
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 10, 0, 0)
        });

        foreach (var movement in events)
        {
            var route = movement.MovementType == StaffMovementType.Fired
                ? $"{movement.FromTeamName} released {movement.FullName}"
                : $"{movement.FullName}: " +
                  $"{movement.FromTeamName ?? "Free Agent"} → {movement.ToTeamName}";

            _programList.Children.Add(new Label
            {
                Text =
                    $"{movement.MovementType.ToString().ToUpperInvariant()} • " +
                    $"{FormatStaffRole(movement.Role)} • {route} • " +
                    $"OVR {movement.OverallRating}",
                FontSize = 11,
                TextColor = movement.MovementType == StaffMovementType.Fired
                    ? NegativeChangeColor
                    : PositiveChangeColor
            });
        }
    }

    private void RenderProgramHistory(Team team)
    {
        if (_currentDynasty is null)
            return;

        var prestige = _currentDynasty.ProgramPrestigeHistory
            .Where(item =>
                item.TeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase) &&
                item.SeasonYear >= _currentDynasty.SeasonYear - 8)
            .OrderByDescending(item => item.SeasonYear)
            .ToArray();

        var completedPrestige = prestige
            .FirstOrDefault(item => item.SeasonYear >= _currentDynasty.SeasonYear - 1 &&
                                    item.Reasons.Any(reason =>
                                        !reason.Equals("Initial program reputation",
                                            StringComparison.OrdinalIgnoreCase)));
        if (completedPrestige is not null)
        {
            _programList.Children.Add(new Label
            {
                Text =
                    $"PRESTIGE TREND • {completedPrestige.SeasonYear}: " +
                    $"{completedPrestige.StartingPrestige}→{completedPrestige.EndingPrestige} " +
                    $"({FormatSigned(completedPrestige.Change)})",
                TextColor = GetChangeColor(completedPrestige.Change),
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 8, 0, 0)
            });
            _programList.Children.Add(new Label
            {
                Text = string.Join(" • ", completedPrestige.Reasons),
                FontSize = 11
            });
        }

        var seasons = _currentDynasty.TeamSeasonHistory
            .Where(item => item.TeamName.Equals(
                team.Name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.SeasonYear)
            .Take(8)
            .ToArray();

        if (seasons.Length > 0)
        {
            _programList.Children.Add(new Label
            {
                Text = "SEASON HISTORY",
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 10, 0, 0)
            });

            foreach (var season in seasons)
            {
                var achievements = new List<string>();
                if (season.NationalChampion) achievements.Add("National Champion");
                else if (season.PlayoffParticipant) achievements.Add("CFP");
                if (season.ConferenceChampion) achievements.Add("Conference Champion");
                if (season.FinalRanking > 0) achievements.Add($"#{season.FinalRanking}");
                if (!string.IsNullOrWhiteSpace(season.BowlName))
                    achievements.Add(
                        $"{season.BowlName} {(season.WonBowl == true ? "W" : "L")}");

                _programList.Children.Add(new Label
                {
                    Text =
                        $"{season.SeasonYear} • {season.Wins}-{season.Losses} " +
                        $"({season.ConferenceWins}-{season.ConferenceLosses} conf)" +
                        (achievements.Count > 0
                            ? $" • {string.Join(" • ", achievements)}"
                            : string.Empty),
                    FontSize = 12
                });
            }
        }

        if (_currentDynasty.PlayerSeasonStats.Count > 0)
        {
            var latestStatSeason = _currentDynasty.PlayerSeasonStats.Max(item => item.SeasonYear);
            var nationalLeaders = _currentDynasty.PlayerSeasonStats
                .Where(item => item.SeasonYear == latestStatSeason)
                .OrderByDescending(item =>
                    item.ScrimmageYards + item.Touchdowns * 100)
                .Take(5)
                .ToArray();

            _programList.Children.Add(new Label
            {
                Text = $"NATIONAL STAT LEADERS • {latestStatSeason}",
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 10, 0, 0)
            });
            foreach (var stat in nationalLeaders)
            {
                _programList.Children.Add(new Label
                {
                    Text =
                        $"{stat.Position} {stat.PlayerName} ({stat.TeamName}) • " +
                        $"{stat.ScrimmageYards:N0} yds • {stat.Touchdowns} TD",
                    FontSize = 11
                });
            }

            var conferenceLeaders = _currentDynasty.PlayerSeasonStats
                .Where(item =>
                    item.SeasonYear == latestStatSeason &&
                    _teamsByName.TryGetValue(item.TeamName, out var statTeam) &&
                    statTeam.ConferenceName.Equals(
                        team.ConferenceName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item =>
                    item.ScrimmageYards + item.Touchdowns * 100)
                .Take(5)
                .ToArray();

            _programList.Children.Add(new Label
            {
                Text = $"{team.ConferenceName.ToUpperInvariant()} STAT LEADERS",
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 8, 0, 0)
            });
            foreach (var stat in conferenceLeaders)
            {
                _programList.Children.Add(new Label
                {
                    Text =
                        $"{stat.Position} {stat.PlayerName} ({stat.TeamName}) • " +
                        $"{stat.ScrimmageYards:N0} yds • {stat.Touchdowns} TD",
                    FontSize = 11
                });
            }

            var schoolCareerLeader = _currentDynasty.PlayerSeasonStats
                .Where(item => item.TeamName.Equals(
                    team.Name, StringComparison.OrdinalIgnoreCase))
                .GroupBy(item => new { item.PlayerId, item.PlayerName, item.Position })
                .Select(group => new
                {
                    group.Key.PlayerName,
                    group.Key.Position,
                    Yards = group.Sum(item => item.ScrimmageYards),
                    Touchdowns = group.Sum(item => item.Touchdowns)
                })
                .OrderByDescending(item => item.Yards)
                .FirstOrDefault();

            if (schoolCareerLeader is not null)
            {
                _programList.Children.Add(new Label
                {
                    Text =
                        $"SCHOOL CAREER YARDS LEADER • {schoolCareerLeader.Position} " +
                        $"{schoolCareerLeader.PlayerName} • " +
                        $"{schoolCareerLeader.Yards:N0} yds • " +
                        $"{schoolCareerLeader.Touchdowns} TD",
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 11,
                    Margin = new Thickness(0, 6, 0, 0)
                });
            }
        }

        var latestAwardSeason = _currentDynasty.PlayerAwardHistory
            .Select(item => item.SeasonYear)
            .DefaultIfEmpty(0)
            .Max();
        var programAwards = _currentDynasty.PlayerAwardHistory
            .Where(item =>
                item.SeasonYear == latestAwardSeason &&
                item.TeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.AwardName)
            .ToArray();

        if (programAwards.Length > 0)
        {
            _programList.Children.Add(new Label
            {
                Text = $"AWARDS • {latestAwardSeason}",
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 10, 0, 0)
            });
            foreach (var award in programAwards)
            {
                _programList.Children.Add(new Label
                {
                    Text =
                        $"{award.AwardName} • {award.Position} {award.PlayerName} • " +
                        award.Summary,
                    FontSize = 11
                });
            }
        }
    }

    private static string FormatStaffRole(StaffRole role) => role switch
    {
        StaffRole.HeadCoach => "Head Coach",
        StaffRole.OffensiveCoordinator => "Offensive Coordinator",
        StaffRole.DefensiveCoordinator => "Defensive Coordinator",
        StaffRole.SpecialTeamsCoordinator => "Special Teams",
        StaffRole.MedicalTrainingDirector => "Medical / Training",
        StaffRole.StrengthConditioningDirector => "Strength & Conditioning",
        StaffRole.ChiefScout => "Chief Scout",
        _ => role.ToString()
    };

    private void RenderRosterManagement(
        IReadOnlyList<DynastyPlayer> userRoster)
    {
        if (_currentDynasty is null)
            return;

        var warnings = RosterManagementService.GetPositionWarnings(
            _currentDynasty,
            _currentDynasty.UserTeamName);

        var redshirts = userRoster.Count(player =>
            player.IsRedshirted);
        var walkOns = userRoster.Count(player =>
            player.IsWalkOn);

        var rosterManagementOpen =
            _currentDynasty.Phase == SeasonPhase.RosterManagement;

        _offseasonRosterStatus.Text =
            $"{(rosterManagementOpen ? "ROSTER MANAGEMENT" : "ROSTER")} • {_currentDynasty.UserTeamName}: " +
            $"{userRoster.Count}/{DynastyRosterRules.MaximumRosterSize} players • " +
            $"{redshirts} redshirts • {walkOns} walk-ons\n" +
            (warnings.Count == 0
                ? "All legacy position minimums are satisfied. "
                : $"POSITION WARNINGS: {string.Join(" • ", warnings)}\n") +
            (rosterManagementOpen
                ? "Use ↑/↓ to set depth order. Redshirted players do not count toward playable minimums. " +
                  "Cuts are filled with walk-ons when you advance."
                : "Depth chart changes are available now. Redshirt and Cut controls open during Roster Management.");

        var latestDevelopment = GetLatestUserDevelopmentRecords()
            .ToDictionary(record => record.PlayerId);

        RenderDevelopmentResults();

        foreach (var position in Enum.GetValues<Position>())
        {
            var players = userRoster
                .Where(player => player.Position == position)
                .OrderBy(player => player.DepthChartOrder)
                .ThenByDescending(player => player.OverallRating)
                .ToArray();

            if (players.Length == 0)
                continue;

            var playableCount = players.Count(player =>
                !player.IsRedshirted);

            var target = DynastyRosterRules.TargetPositionCounts.TryGetValue(
                position,
                out var targetCount)
                ? targetCount
                : 0;

            var minimum = DynastyRosterRules.MinimumPositionCounts.TryGetValue(
                position,
                out var minimumCount)
                ? minimumCount
                : 0;

            var starters = DynastyRosterRules.StarterPositionCounts.TryGetValue(
                position,
                out var starterCount)
                ? starterCount
                : 0;

            _transferPortalList.Children.Add(new Label
            {
                Text =
                    $"{position} • {players.Length}/{target} roster • " +
                    $"{playableCount} playable • min {minimum} • starters {starters}" +
                    (playableCount < minimum ? " • NEED DEPTH" : string.Empty),
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 8, 0, 2)
            });

            foreach (var player in players)
            {
                latestDevelopment.TryGetValue(
                    player.PlayerId,
                    out var development);

                AddRosterPlayerCard(
                    player,
                    playableCount,
                    development);
            }
        }
    }

    private void AddRosterPlayerCard(
        DynastyPlayer player,
        int playableCount,
        PlayerDevelopmentRecord? development)
    {
        if (_currentDynasty is null)
            return;

        var status = new List<string>();

        if (RosterManagementService.IsStarter(player))
            status.Add("STARTER");

        if (player.IsRedshirted)
            status.Add("REDSHIRT");

        if (player.IsWalkOn)
            status.Add("WALK-ON");

        if (player.HasRedshirted && !player.IsRedshirted)
            status.Add("RS USED");

        if (player.CurrentInjury is not null)
        {
            status.Add(
                $"{player.CurrentInjury.Severity.ToString().ToUpperInvariant()} " +
                $"{player.CurrentInjury.BodyArea} " +
                $"{player.CurrentInjury.WeeksRemaining}WK");
        }

        var depthLabel = player.IsRedshirted
            ? "RS"
            : $"#{player.DepthChartOrder}";

        var upButton = new Button
        {
            Text = "↑",
            TextColor = Colors.White,
            IsEnabled =
                !player.IsRedshirted &&
                player.DepthChartOrder > 1
        };

        upButton.Clicked += (_, _) =>
        {
            if (_currentDynasty is null)
                return;

            _currentDynasty = RosterManagementService.MovePlayer(
                _currentDynasty,
                _currentDynasty.UserTeamName,
                player.PlayerId,
                -1);

            RenderCurrentDynasty();
        };

        var downButton = new Button
        {
            Text = "↓",
            TextColor = Colors.White,
            IsEnabled =
                !player.IsRedshirted &&
                player.DepthChartOrder < playableCount
        };

        downButton.Clicked += (_, _) =>
        {
            if (_currentDynasty is null)
                return;

            _currentDynasty = RosterManagementService.MovePlayer(
                _currentDynasty,
                _currentDynasty.UserTeamName,
                player.PlayerId,
                1);

            RenderCurrentDynasty();
        };

        var redshirtButton = new Button
        {
            Text = player.IsRedshirted
                ? "Remove RS"
                : player.HasRedshirted
                    ? "RS Used"
                    : "Redshirt",
            TextColor = Colors.White,
            IsEnabled =
                _currentDynasty.Phase == SeasonPhase.RosterManagement &&
                RosterManagementService.CanRedshirt(player)
        };

        redshirtButton.Clicked += (_, _) =>
        {
            if (_currentDynasty is null)
                return;

            _currentDynasty = RosterManagementService.ToggleRedshirt(
                _currentDynasty,
                _currentDynasty.UserTeamName,
                player.PlayerId);

            RenderCurrentDynasty();
        };

        var cutButton = new Button
        {
            Text = "Cut",
            TextColor = Colors.White,
            IsEnabled =
                _currentDynasty.Phase == SeasonPhase.RosterManagement
        };

        cutButton.Clicked += async (_, _) =>
        {
            if (_currentDynasty is null)
                return;

            var confirmed = await DisplayAlertAsync(
                "Release player?",
                $"Release {player.FullName}? A walk-on can fill the open roster spot when you advance.",
                "Release",
                "Cancel");

            if (!confirmed || _currentDynasty is null)
                return;

            _currentDynasty = RosterManagementService.ReleasePlayer(
                _currentDynasty,
                _currentDynasty.UserTeamName,
                player.PlayerId);

            RenderCurrentDynasty();
        };

        _transferPortalList.Children.Add(new Border
        {
            StrokeThickness = 1,
            Padding = 8,
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label
                    {
                        Text =
                            $"{depthLabel} {player.FullName} • OVR {player.OverallRating} • " +
                            $"POT {FormatPotentialStars(player.PotentialRating)} • Year {player.ClassYear}" +
                            (status.Count > 0
                                ? $" • {string.Join(" • ", status)}"
                                : string.Empty),
                        FontAttributes =
                            RosterManagementService.IsStarter(player)
                                ? FontAttributes.Bold
                                : FontAttributes.None
                    },
                    new Label
                    {
                        Text =
                            $"SPD {player.SpeedRating} • STR {player.StrengthRating} • " +
                            $"AGI {player.AgilityRating} • AWR {player.AwarenessRating} • " +
                            $"TECH {player.TechniqueRating} • DUR {player.DurabilityRating}",
                        FontSize = 12
                    },
                    new Label
                    {
                        Text = development is null
                            ? string.Empty
                            : $"Last dev ({development.SeasonYear}→{development.SeasonYear + 1}): " +
                              $"OVR {development.BeforeOverall}→{development.AfterOverall} " +
                              $"({FormatSigned(development.OverallChange)}) • " +
                              $"POT {FormatPotentialStars(development.BeforePotential)}→" +
                              $"{FormatPotentialStars(development.AfterPotential)}",
                        FontSize = 12,
                        IsVisible = development is not null,
                        TextColor = GetChangeColor(development?.OverallChange ?? 0),
                        FontAttributes = development?.OverallChange is > 0
                            ? FontAttributes.Bold
                            : FontAttributes.None
                    },
                    new HorizontalStackLayout
                    {
                        Spacing = 6,
                        Children =
                        {
                            upButton,
                            downButton,
                            redshirtButton,
                            cutButton
                        }
                    }
                }
            }
        });
    }

    private IReadOnlyList<PlayerDevelopmentRecord>
        GetLatestUserDevelopmentRecords()
    {
        if (_currentDynasty is null)
            return Array.Empty<PlayerDevelopmentRecord>();

        var records = PlayerDevelopmentService.GetLatestRecords(
            _currentDynasty.PlayerDevelopmentHistory,
            _currentDynasty.UserTeamName);

        if (records.Count > 0 &&
            records.All(record =>
                record.Stage == PlayerDevelopmentStage.Midseason) &&
            !(_currentDynasty.Phase == SeasonPhase.RegularSeason &&
              _currentDynasty.Week == 7 &&
              records.All(record =>
                  record.SeasonYear == _currentDynasty.SeasonYear)))
        {
            return Array.Empty<PlayerDevelopmentRecord>();
        }

        return records;
    }

    private void RenderDevelopmentResults()
    {
        var records = GetLatestUserDevelopmentRecords();

        if (records.Count == 0)
            return;

        var seasonYear = records[0].SeasonYear;
        var isMidseason = records.All(record =>
            record.Stage == PlayerDevelopmentStage.Midseason);
        var improved = records.Count(record =>
            record.OverallChange > 0);
        var unchanged = records.Count(record =>
            record.OverallChange == 0);
        var regressed = records.Count(record =>
            record.OverallChange < 0);
        var average = records.Average(record =>
            record.OverallChange);

        _transferPortalList.Children.Add(new Label
        {
            Text = isMidseason
                ? $"{seasonYear} MIDSEASON PLAYER DEVELOPMENT"
                : $"{seasonYear}→{seasonYear + 1} PLAYER DEVELOPMENT RESULTS",
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 8, 0, 0)
        });

        _transferPortalList.Children.Add(new Label
        {
            Text =
                $"Improved {improved} • Unchanged {unchanged} • " +
                $"Regressed {regressed} • Avg {FormatSigned(average)} OVR",
            FontSize = 13
        });

        var movers = records
            .Where(record => record.OverallChange != 0)
            .OrderByDescending(record =>
                Math.Abs(record.OverallChange))
            .ThenByDescending(record =>
                record.OverallChange)
            .ThenBy(record =>
                record.PlayerName,
                StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToArray();

        if (movers.Length == 0)
            return;

        _transferPortalList.Children.Add(new Label
        {
            Text = "BIGGEST MOVERS",
            FontAttributes = FontAttributes.Bold,
            FontSize = 12
        });

        foreach (var record in movers)
        {
            _transferPortalList.Children.Add(new Label
            {
                Text =
                    $"{record.Position} {record.PlayerName} • " +
                    $"OVR {record.BeforeOverall}→{record.AfterOverall} " +
                    $"({FormatSigned(record.OverallChange)}) • " +
                    $"POT {FormatPotentialStars(record.BeforePotential)}→" +
                    $"{FormatPotentialStars(record.AfterPotential)}",
                FontSize = 12,
                TextColor = GetChangeColor(record.OverallChange)
            });
        }
    }

    private static string FormatPotentialStars(int potential) =>
        potential switch
        {
            >= 92 => "5★",
            >= 84 => "4★",
            >= 76 => "3★",
            >= 68 => "2★",
            _ => "1★"
        };

    private static Color GetChangeColor(int value) =>
        value > 0
            ? PositiveChangeColor
            : value < 0
                ? NegativeChangeColor
                : NeutralChangeColor;

    private static Color GetChangeColor(double value) =>
        value > 0.05
            ? PositiveChangeColor
            : value < -0.05
                ? NegativeChangeColor
                : NeutralChangeColor;

    private static string FormatSigned(int value) =>
        value > 0
            ? $"+{value}"
            : value.ToString();

    private static string FormatSigned(double value) =>
        value > 0
            ? $"+{value:0.0}"
            : value.ToString("0.0");

    private IReadOnlyDictionary<string, TeamSimulationProfile>
        BuildCurrentSimulationProfiles()
    {
        if (_currentDynasty is null ||
            _currentDynasty.ActiveRoster.Count == 0)
        {
            return _simulationProfiles;
        }

        return DynastyRosterSimulationProfileBuilder.Build(
            _currentDynasty,
            _teamsByName.Values);
    }

    private void RenderNationalRankings()
    {
        _nationalRankingsList.Children.Clear();

        if (_currentDynasty is null)
        {
            _nationalRankingStatus.Text =
                "No ranking data available.";
            return;
        }

        var rankings = NationalRankingService.Build(
            _currentDynasty,
            _teamsByName,
            BuildCurrentSimulationProfiles());

        var userRanking = rankings.FirstOrDefault(ranking =>
            ranking.TeamName.Equals(
                _currentDynasty.UserTeamName,
                StringComparison.OrdinalIgnoreCase));

        _nationalRankingStatus.Text =
            userRanking is null
                ? "User team is not ranked."
                : userRanking.Rank <= 25
                    ? $"Your team: #{userRanking.Rank} {userRanking.TeamName}"
                    : $"Your team: NR ({userRanking.Rank}) {userRanking.TeamName}";

        foreach (var ranking in rankings.Take(25))
        {
            _nationalRankingsList.Children.Add(new Label
            {
                Text =
                    $"#{ranking.Rank} {ranking.TeamName} • " +
                    $"{ranking.Wins}-{ranking.Losses} • " +
                    $"{ranking.ConferenceName}",
                FontAttributes = ranking.TeamName.Equals(
                    _currentDynasty.UserTeamName,
                    StringComparison.OrdinalIgnoreCase)
                    ? FontAttributes.Bold
                    : FontAttributes.None
            });
        }
    }

    private void RenderPostseason()
    {
        _postseasonList.Children.Clear();

        if (_currentDynasty is null)
        {
            _postseasonStatus.Text =
                "No postseason data available.";
            return;
        }

        var field = _currentDynasty.CollegeFootballPlayoffHistory
            .Where(record => record.SeasonYear == _currentDynasty.SeasonYear)
            .OrderBy(record => record.Seed)
            .ToArray();

        var champion = _currentDynasty.NationalChampionshipHistory
            .FirstOrDefault(record =>
                record.SeasonYear == _currentDynasty.SeasonYear);

        if (champion is not null)
        {
            _postseasonStatus.Text =
                $"{champion.SeasonYear} National Champion: " +
                $"{champion.ChampionTeamName} " +
                $"{champion.ChampionScore}-{champion.RunnerUpScore} " +
                $"over {champion.RunnerUpTeamName}";
        }
        else if (field.Length == CollegeFootballPlayoffService.PlayoffTeamCount)
        {
            _postseasonStatus.Text =
                "12-team CFP field: five highest-ranked conference champions + seven at-large teams.";
        }
        else
        {
            _postseasonStatus.Text =
                "The 12-team CFP field is selected after conference championships.";
            return;
        }

        foreach (var selection in field)
        {
            var marker = selection.IsAutomaticBid
                ? "AUTO"
                : "AT-LARGE";

            _postseasonList.Children.Add(new Label
            {
                Text =
                    $"{selection.Seed}. {selection.TeamName} • " +
                    $"#{selection.NationalRank} • {marker}" +
                    (selection.Seed <= 4 ? " • BYE" : string.Empty),
                FontAttributes = selection.TeamName.Equals(
                    _currentDynasty.UserTeamName,
                    StringComparison.OrdinalIgnoreCase)
                    ? FontAttributes.Bold
                    : FontAttributes.None
            });
        }

        foreach (var game in _currentDynasty.Schedule
                     .Where(game =>
                         game.SeasonYear == _currentDynasty.SeasonYear &&
                         game.GameType == ScheduledGameType.CollegeFootballPlayoff)
                     .OrderBy(game => game.Week)
                     .ThenBy(game => game.PlayoffBracketSlot))
        {
            var result = game.HasPlayed &&
                         game.HomeScore is int homeScore &&
                         game.AwayScore is int awayScore
                ? $"{awayScore}-{homeScore}"
                : "Upcoming";

            _postseasonList.Children.Add(new Label
            {
                Text =
                    $"{FormatPostseasonRound(game.PostseasonRound)}: " +
                    $"#{game.AwaySeed} {game.AwayTeamName} vs " +
                    $"#{game.HomeSeed} {game.HomeTeamName} • {result}"
            });
        }

        var bowls = _currentDynasty.Schedule
            .Where(game =>
                game.SeasonYear == _currentDynasty.SeasonYear &&
                game.GameType == ScheduledGameType.Bowl)
            .OrderBy(game => game.Week)
            .ThenBy(game => game.BowlName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (bowls.Length > 0)
        {
            _postseasonList.Children.Add(new Label
            {
                Text = "NON-CFP BOWLS",
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 8, 0, 0)
            });

            foreach (var game in bowls)
            {
                var result = game.HasPlayed &&
                             game.HomeScore is int homeScore &&
                             game.AwayScore is int awayScore
                    ? $"{game.AwayTeamName} {awayScore}, {game.HomeTeamName} {homeScore}"
                    : $"{game.AwayTeamName} vs {game.HomeTeamName} • Upcoming";

                _postseasonList.Children.Add(new Label
                {
                    Text = $"{game.BowlName}: {result}",
                    FontAttributes = game.InvolvesTeam(
                        _currentDynasty.UserTeamName)
                        ? FontAttributes.Bold
                        : FontAttributes.None
                });
            }
        }
    }

    private static string FormatPostseasonRound(PostseasonRound round) =>
        round switch
        {
            PostseasonRound.FirstRound => "First Round",
            PostseasonRound.Quarterfinal => "Quarterfinal",
            PostseasonRound.Semifinal => "Semifinal",
            PostseasonRound.NationalChampionship => "National Championship",
            _ => "Postseason"
        };

    private void RenderConferenceRace()
    {
        _conferenceStandingsList.Children.Clear();

        if (_currentDynasty is null ||
            !_teamsByName.TryGetValue(
                _currentDynasty.UserTeamName,
                out var userTeam))
        {
            _conferenceChampionshipStatus.Text =
                "No conference data available.";
            return;
        }

        var conferenceTeams = _teamsByName.Values
            .Where(team => team.ConferenceName.Equals(
                userTeam.ConferenceName,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (conferenceTeams.Length <
                ConferenceChampionshipService.MinimumConferenceTeams ||
            userTeam.ConferenceName.Equals(
                "Independent",
                StringComparison.OrdinalIgnoreCase))
        {
            _conferenceChampionshipStatus.Text =
                $"{userTeam.ConferenceName}: no conference championship.";
            return;
        }

        var nationalRanks = NationalRankingService
            .Build(
                _currentDynasty,
                _teamsByName,
                BuildCurrentSimulationProfiles())
            .ToDictionary(
                ranking => ranking.TeamName,
                ranking => ranking.Rank,
                StringComparer.OrdinalIgnoreCase);

        var standings = ConferenceStandings.Build(
            _currentDynasty,
            _teamsByName,
            userTeam.ConferenceName,
            nationalRanks);

        var titleGame = _currentDynasty.Schedule
            .FirstOrDefault(game =>
                game.SeasonYear == _currentDynasty.SeasonYear &&
                game.GameType ==
                    ScheduledGameType.ConferenceChampionship &&
                _teamsByName.TryGetValue(
                    game.HomeTeamName,
                    out var home) &&
                home.ConferenceName.Equals(
                    userTeam.ConferenceName,
                    StringComparison.OrdinalIgnoreCase));

        var champion = _currentDynasty
            .ConferenceChampionshipHistory
            .FirstOrDefault(record =>
                record.SeasonYear == _currentDynasty.SeasonYear &&
                record.ConferenceName.Equals(
                    userTeam.ConferenceName,
                    StringComparison.OrdinalIgnoreCase));

        if (champion is not null)
        {
            _conferenceChampionshipStatus.Text =
                $"{champion.ConferenceName} Champion: " +
                $"{champion.ChampionTeamName} " +
                $"{champion.ChampionScore}-{champion.RunnerUpScore} " +
                $"over {champion.RunnerUpTeamName}";
        }
        else if (titleGame is not null)
        {
            _conferenceChampionshipStatus.Text =
                $"{userTeam.ConferenceName} Championship: " +
                $"{titleGame.AwayTeamName} @ {titleGame.HomeTeamName}";
        }
        else
        {
            _conferenceChampionshipStatus.Text =
                $"Top two {userTeam.ConferenceName} teams qualify for the championship.";
        }

        for (var index = 0; index < standings.Count; index++)
        {
            var standing = standings[index];

            _conferenceStandingsList.Children.Add(new Label
            {
                Text =
                    $"{index + 1}. {standing.TeamName} • " +
                    $"Conf {standing.ConferenceWins}-{standing.ConferenceLosses} • " +
                    $"Overall {standing.OverallWins}-{standing.OverallLosses}",
                FontAttributes = standing.TeamName.Equals(
                    _currentDynasty.UserTeamName,
                    StringComparison.OrdinalIgnoreCase)
                    ? FontAttributes.Bold
                    : FontAttributes.None
            });
        }
    }

    private void RenderUserSchedule()
    {
        _scheduleList.Children.Clear();

        if (_currentDynasty is null || _currentDynasty.Schedule.Count == 0)
        {
            _scheduleList.Children.Add(new Label
            {
                Text = "No schedule available."
            });
            return;
        }

        foreach (var game in _currentDynasty.Schedule
                     .Where(game => game.InvolvesTeam(_currentDynasty.UserTeamName))
                     .OrderBy(game => game.Week))
        {
            var isHome = game.HomeTeamName.Equals(
                _currentDynasty.UserTeamName,
                StringComparison.OrdinalIgnoreCase);
            var opponent = isHome ? game.AwayTeamName : game.HomeTeamName;
            var location = isHome ? "vs" : "@";
            var status = "Upcoming";

            if (game.HasPlayed && game.HomeScore is int homeScore && game.AwayScore is int awayScore)
            {
                var userScore = isHome ? homeScore : awayScore;
                var opponentScore = isHome ? awayScore : homeScore;
                var result = userScore > opponentScore ? "W" : "L";
                status = $"{result} {userScore}-{opponentScore}";

                var userStats = isHome ? game.HomeStats : game.AwayStats;
                var opponentStats = isHome ? game.AwayStats : game.HomeStats;

                if (userStats is not null && opponentStats is not null)
                {
                    status +=
                        $" • {userStats.TotalYards}-{opponentStats.TotalYards} yds" +
                        $" • TO {userStats.Turnovers}-{opponentStats.Turnovers}";
                }
            }

            var gameType = game.GameType switch
            {
                ScheduledGameType.Conference => "CONF",
                ScheduledGameType.ConferenceChampionship => "CCG",
                ScheduledGameType.CollegeFootballPlayoff => "CFP",
                ScheduledGameType.Bowl => "BOWL",
                _ => "OOC"
            };

            var resultColor = game.HasPlayed && game.WinnerTeamName is not null
                ? game.WinnerTeamName.Equals(
                    _currentDynasty.UserTeamName, StringComparison.OrdinalIgnoreCase)
                    ? PositiveChangeColor
                    : NegativeChangeColor
                : NeutralChangeColor;

            _scheduleList.Children.Add(new Label
            {
                Text = $"Week {game.Week}: {location} {opponent} • {gameType} • {status}",
                TextColor = resultColor,
                FontAttributes =
                    _currentDynasty.Phase == SeasonPhase.RegularSeason &&
                    game.Week == _currentDynasty.Week
                        ? FontAttributes.Bold
                        : FontAttributes.None
            });
        }
    }

    private Task<IReadOnlyList<ScheduledGame>> GenerateScheduleAsync(
        Guid dynastyId,
        int seasonYear)
    {
        // The conference/OOC scheduler is CPU-bound. Running it on the UI
        // thread makes Android appear frozen while the full 130-team slate is
        // constructed, especially on lower-power devices.
        var teams = _teamsByName.Values.ToArray();

        return Task.Run<IReadOnlyList<ScheduledGame>>(() =>
            SeasonScheduleBuilder.BuildRegularSeason(
                teams,
                dynastyId,
                seasonYear));
    }

    private void SetNewDynastyControlsEnabled(bool enabled)
    {
        _newDynastyButton.IsEnabled = enabled;
        _dynastyNameEntry.IsEnabled = enabled;
        _teamPicker.IsEnabled = enabled;
    }

    private void SetDynastyControlsEnabled(bool enabled)
    {
        var controlsEnabled = enabled && !_isAdvancing;
        _advanceWeekButton.IsEnabled = controlsEnabled;
        _manualSaveButton.IsEnabled = controlsEnabled;
        _autosaveButton.IsEnabled = controlsEnabled;
    }

    private async Task ShowAdvanceProgressAsync(string status, double progress)
    {
        _advanceOverlayStatus.Text = status;
        _advanceOverlayProgress.Progress = Math.Clamp(progress, 0, 1);
        _advanceOverlay.IsVisible = true;
        await Task.Yield();
    }

    private static string FormatWeek(DynastySaveInfo save) =>
        save.Phase switch
        {
            SeasonPhase.Preseason => "Preseason",
            SeasonPhase.Recruiting =>
                $"Recruiting Week {Math.Clamp(
                    save.Week - SeasonProgression.FirstRecruitingWeek + 1,
                    1,
                    SeasonProgression.RecruitingWeekCount)}",
            _ => $"Week {save.Week}"
        };

    private static async Task<string> ReadAssetAsync(string fileName)
    {
        await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(fileName);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
