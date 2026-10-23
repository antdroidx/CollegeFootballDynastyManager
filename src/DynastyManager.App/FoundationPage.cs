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

public sealed partial class FoundationPage : ContentPage
{
    private static Color PositiveChangeColor => DynastyTheme.Palette("#22C55E");
    private static Color NegativeChangeColor => DynastyTheme.Palette("#EF4444");
    private static Color NeutralChangeColor => DynastyTheme.Palette("#94A3B8");
    private enum AppSection
    {
        Home,
        NewDynasty,
        Dynasty,
        Schedule,
        Roster,
        Teams,
        Recruiting,
        Playbook,
        Rankings,
        Stats,
        Program,
        Season,
        Settings,
        GameDay,
        News,
        Injuries
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
        bool HasRedshirted,
        int? Potential,
        int? StarRating,
        int OverallRating);
    private readonly Label _importStatus;
    private readonly Entry _dynastyNameEntry;
    private readonly Picker _teamPicker;
    private readonly Picker _leaguePicker = new()
    {
        Title = "League structure", ItemsSource = new[] { "Modern FBS (2026) - 138 teams", "Legacy FBS - 130 teams" },
        SelectedIndex = 0
    };
    private IReadOnlyList<RealCoachSeed> _realCoachSeeds = [];
    private IReadOnlyList<Team> _legacyTeams = Array.Empty<Team>();
    private IReadOnlyList<Team> _newDynastyTeams = Array.Empty<Team>();
    private readonly Button _newDynastyButton;
    private readonly Label _currentDynastyLabel;
    private readonly Button _advanceWeekButton;
    private readonly Button _manualSaveButton;
    private readonly Button _autosaveButton;
    private readonly Switch _rollingAutosaveSwitch;
    private readonly Switch _recruitingAssistanceSwitch;
    private readonly Slider _recruitingAssistanceWorkloadSlider;
    private readonly Label _recruitingAssistanceWorkloadLabel;
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
    private readonly Grid _recruitingLoadingOverlay;
    private readonly ActivityIndicator _recruitingLoadingIndicator;
    private readonly Label _recruitingLoadingStatus;
    private CancellationTokenSource? _recruitingLoadCancellation;
    private int _recruitingLoadVersion;
    private readonly Grid _advanceOverlay;
    private readonly Label _advanceOverlayStatus;
    private readonly ProgressBar _advanceOverlayProgress;
    private readonly Label _programStatus;
    private readonly VerticalStackLayout _programList;
    private readonly Label _teamBrowserStatus;
    private readonly Picker _teamBrowserPicker;
    private readonly VerticalStackLayout _teamBrowserList;
    private readonly Dictionary<AppSection, Button> _navButtons = new();
    private readonly Dictionary<AppSection, BoxView> _navIndicators = new();
    private Border _navigationBar = null!;
    private readonly Button _libraryButton = new() { Text = "‹ Exit Dynasty", FontSize = 12, Padding = new Thickness(0, 2, 12, 2), BackgroundColor = Colors.Transparent };
    private readonly Label _headerContext = new() { FontSize = 11, TextColor = DynastyTheme.Muted };
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
    private IReadOnlyList<TeamGamePlan>? _cachedProfileGamePlans;
    private IReadOnlyList<DynastyPlayer>? _cachedProfileRoster;
    private IReadOnlyList<StaffMember>? _cachedProfileStaff;
    private StaffMember? _cachedProfileUserCoach;
    private IReadOnlyList<ProgramPrestigeSnapshot>? _cachedProfilePrestige;
    private IReadOnlyDictionary<string, TeamSimulationProfile>? _cachedCurrentProfiles;
    private IReadOnlyList<ScheduledGame>? _cachedRankingSchedule;
    private IReadOnlyList<ConferenceChampionRecord>? _cachedRankingConferenceHistory;
    private IReadOnlyList<NationalChampionRecord>? _cachedRankingNationalHistory;
    private IReadOnlyDictionary<string, TeamSimulationProfile>? _cachedRankingProfiles;
    private IReadOnlyList<NationalRanking>? _cachedNationalRankings;
    private IReadOnlyList<PollSnapshot>? _cachedRankingPollHistory;
    private bool _loaded;
    private bool _isAdvancing;

    public FoundationPage()
    {
        Title = "Dynasty Manager";

        _importStatus = new Label
        {
            Text = "Loading league presets…",
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

        _leaguePicker.SelectedIndexChanged += (_, _) => UpdateNewDynastyTeams();

        _newDynastyButton = new Button
        {
            Text = "Start dynasty",
            TextColor = Colors.White
        };
        _newDynastyButton.Clicked += CreateNewDynasty;

        _currentDynastyLabel = new Label
        {
            Text = "No dynasty loaded.",
            FontSize = 14
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
            Text = "Manual save",
            TextColor = Colors.White,
            IsEnabled = false
        };
        _manualSaveButton.Clicked += async (_, _) => await SaveCurrentAsync(SaveKind.Manual);

        _autosaveButton = new Button
        {
            Text = "Save settings & dynasty",
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

        _recruitingAssistanceWorkloadLabel = new Label
        {
            Text = GetRecruitingAssistanceWorkloadText(100),
            FontAttributes = FontAttributes.Bold,
            FontSize = 12
        };

        _recruitingAssistanceWorkloadSlider = new Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = 100
        };
        _recruitingAssistanceWorkloadSlider.ValueChanged += (_, args) =>
        {
            var workload = Math.Clamp(
                (int)Math.Round(args.NewValue / 10.0) * 10,
                0,
                100);

            _recruitingAssistanceWorkloadLabel.Text =
                GetRecruitingAssistanceWorkloadText(workload);

            if (_currentDynasty is null)
                return;

            _currentDynasty = _currentDynasty with
            {
                RecruitingAssistanceWorkloadPercent = workload
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
            Text = "The playoff field is selected after conference championships.",
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
            FontSize = 18,
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

        _recruitingLoadingIndicator = new ActivityIndicator { Color = Colors.White };
        _recruitingLoadingStatus = new Label
        {
            Text = "Loading recruits…", TextColor = Colors.White, FontSize = 18,
            HorizontalTextAlignment = TextAlignment.Center
        };
        _recruitingLoadingOverlay = new Grid
        {
            IsVisible = false, BackgroundColor = DynastyTheme.Palette("#F010243E"), Padding = 24,
            Children =
            {
                new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center, Spacing = 12,
                    Children = { _recruitingLoadingIndicator, _recruitingLoadingStatus }
                }
            }
        };

        _advanceOverlayStatus = new Label
        {
            Text = "Preparing weekly advance...",
            TextColor = Colors.White,
            FontSize = 14,
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
            BackgroundColor = DynastyTheme.Palette("#D910243E"),
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

        _teamBrowserStatus = new Label
        {
            Text = "Choose a program to view its roster and current information.",
            FontSize = 13
        };

        _teamBrowserPicker = new Picker
        {
            Title = "Choose program"
        };
        _teamBrowserPicker.SelectedIndexChanged += (_, _) =>
            RenderTeamBrowser();

        _teamBrowserList = new VerticalStackLayout
        {
            Spacing = 5
        };

        var navigation = new Grid { ColumnSpacing = 2, Padding = new Thickness(6, 3),
            ColumnDefinitions = new ColumnDefinitionCollection
            { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) } };

        foreach (var section in new[] { AppSection.Dynasty, AppSection.Roster, AppSection.Playbook, AppSection.Recruiting, AppSection.Season })
        {
            var captured = section;
            var button = new Button
            {
                Text = section switch
                {
                    AppSection.Recruiting => "Recruit",
                    AppSection.Rankings => "Ranks",
                    AppSection.Teams => "Teams",
                    AppSection.Playbook => "Coach",
                    AppSection.Dynasty => "Overview",
                    AppSection.Season => "Season",
                    _ => section.ToString()
                },
                FontSize = 12,
                Padding = new Thickness(2, 5),
                HorizontalOptions = LayoutOptions.Fill,
                TextColor = Colors.White
            };

            button.Clicked += (_, _) => ShowSection(captured);
            _navButtons[section] = button;
            var indicator = new BoxView { HeightRequest = 2, Color = DynastyTheme.Accent, IsVisible = false };
            _navIndicators[section] = indicator;
            var tab = new Grid { RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Auto) } };
            tab.Add(button, 0, 0); tab.Add(indicator, 0, 1);
            navigation.Add(tab, _navButtons.Count - 1, 0);
        }

        _navigationBar = new Border
        {
            StrokeThickness = 0,
            Padding = 0,
            Content = navigation
        };

        var pages = new Grid();

        foreach (var section in Enum.GetValues<AppSection>())
        {
            View sectionContent = section switch
            {
                AppSection.Home => BuildLibrarySection(),
                AppSection.NewDynasty => BuildNewDynastySection(),
                AppSection.Dynasty => BuildHomeSection(),
                AppSection.Schedule => BuildScheduleSection(),
                AppSection.Roster => BuildRosterSection(),
                AppSection.Teams => BuildTeamsSection(),
                AppSection.Recruiting => BuildRecruitingSection(),
                AppSection.Rankings => BuildRankingsSection(),
                AppSection.Stats => BuildStatisticsSection(),
                AppSection.Program => BuildProgramSection(),
                AppSection.Playbook => BuildGamePlanSection(),
                AppSection.Season => BuildSeasonHub(),
                AppSection.Settings => BuildMoreSection(),
                AppSection.GameDay => BuildMatchupHub(),
                AppSection.News => BuildNewsSection(),
                AppSection.Injuries => BuildInjurySection(),
                _ => BuildHomeSection()
            };

            View page;

            if (section == AppSection.Recruiting)
            {
                sectionContent.Margin =
                    new Thickness(14, 6, 14, 10);
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
                        Padding = new Thickness(14, 8, 14, 16),
                        Spacing = 12,
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

        _libraryButton.Clicked += async (_, _) => await ReturnToLibraryAsync();
        var heading = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center, Children = { _screenTitle, _headerContext } };
        _settingsButton.Clicked += (_, _) => ShowSection(AppSection.Settings);
        var header = new Grid { Padding = new Thickness(14, 10, 14, 4),
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        _screenTitle.LineBreakMode = LineBreakMode.TailTruncation;
        header.Add(_libraryButton, 0, 0); header.Add(heading, 1, 0); header.Add(_settingsButton, 2, 0);
        root.Add(header, 0, 0);

        root.Add(pages);
        Grid.SetRow(pages, 1);

        root.Add(_navigationBar);
        Grid.SetRow(_navigationBar, 2);

        ConfigureManagementShell(root, header, pages);
        var shell = new Grid();
        shell.Children.Add(root);
        shell.Children.Add(_advanceOverlay);
        Content = shell;
        ShowSection(AppSection.Home);

        Loaded += OnLoaded;
    }

    private void ShowSection(AppSection section)
    {
        if (_currentDynasty is null && section is not (AppSection.Home or AppSection.NewDynasty)) section = AppSection.Home;
        RememberSection(section);
        _activeSection = section;
        UpdateShellContext();
        RefreshManagementNavigation();
        if (section != AppSection.Recruiting)
        {
            _recruitingLoadCancellation?.Cancel();
            _recruitingLoadingOverlay.IsVisible = false;
            _recruitingLoadingIndicator.IsRunning = false;
        }

        foreach (var pair in _navButtons)
        {
            pair.Value.FontAttributes =
                pair.Key == PrimarySection(section)
                    ? FontAttributes.Bold
                    : FontAttributes.None;
            var selected = pair.Key == PrimarySection(section);
            pair.Value.Opacity = 1.0;
            pair.Value.BackgroundColor = selected ? DynastyTheme.Selected : Colors.Transparent;
            pair.Value.TextColor = selected ? DynastyTheme.Accent : DynastyTheme.Muted;
            _navIndicators[pair.Key].IsVisible = selected;
        }

        foreach (var pair in _sectionViews)
            pair.Value.IsVisible = pair.Key == section;

        _screenTitle.Text = section switch
        {
            AppSection.Home => "College Football",
            AppSection.NewDynasty => "Start a dynasty",
            AppSection.Dynasty => "Overview",
            AppSection.Schedule => "Schedule",
            AppSection.Roster => "Roster & Depth Chart",
            AppSection.Teams => "Teams & Rosters",
            AppSection.Recruiting => "Recruiting",
            AppSection.Rankings => "Rankings & Postseason",
            AppSection.Stats => "Player statistics",
            AppSection.Program => "Program",
            AppSection.Playbook => "Coaching Hub",
            AppSection.Season => "Season center",
            AppSection.Settings => "Settings",
            AppSection.GameDay => "Next game",
            AppSection.News => "News & headlines",
            AppSection.Injuries => "Injury report",
            _ => section.ToString()
        };

        if (_currentDynasty is not null || section == AppSection.Playbook)
            RenderSection(section);
    }

    private View BuildHomeSection() => BuildOverviewSection();

    private View BuildScheduleSection() => BuildOrganizedScheduleSection();

    private View BuildRosterSection() => BuildOrganizedRosterSection();

    private View BuildTeamsSection() =>
        new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label
                {
                    Text = "Browse any program's current roster, ratings, staff, record, and recruiting class.",
                    FontSize = 13
                },
                _teamBrowserPicker,
                _teamBrowserStatus,
                _teamBrowserList
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

        var tabScroller = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = _recruitingTabs
        };
        var controls = new ScrollView
        {
            Content = new VerticalStackLayout { Spacing = 7, Children =
            {
                _recruitingMetrics, _recruitingStatus, tabScroller,
                _recruitingSearch, _recruitingFilterScroller
            } }
        };
        var results = new ScrollView
        {
            Content = new VerticalStackLayout { Spacing = 7, Children =
            {
                _recruitingResultsStatus, _recruitingList
            } }
        };
        _recruitingDetailHost.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ContentView.Content) && _recruitingDetailHost.Content is not null)
                _ = results.ScrollToAsync(0, 0, false);
        };
        var root = new Grid
        {
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) },
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(0) },
            RowSpacing = 7, ColumnSpacing = 0
        };
        root.Add(controls, 0, 0);
        var prospectWorkspace = BuildRecruitWorkspace(results);
        root.Add(prospectWorkspace, 0, 1);
        root.Add(_recruitingLoadingOverlay, 0, 0);
        Grid.SetRowSpan(_recruitingLoadingOverlay, 2);
        Grid.SetColumnSpan(_recruitingLoadingOverlay, 2);
        bool? sideBySide = null;
        root.SizeChanged += (_, _) =>
        {
            var wide = root.Width >= 650 && root.Height < 420;
            if (sideBySide == wide) return;
            sideBySide = wide;
            root.ColumnSpacing = wide ? 14 : 0;
            root.ColumnDefinitions[0].Width = wide ? new GridLength(260) : GridLength.Star;
            root.ColumnDefinitions[1].Width = wide ? GridLength.Star : new GridLength(0);
            root.RowDefinitions[0].Height = wide ? GridLength.Star : GridLength.Auto;
            root.RowDefinitions[1].Height = wide ? new GridLength(0) : GridLength.Star;
            Grid.SetColumn(prospectWorkspace, wide ? 1 : 0);
            Grid.SetRow(prospectWorkspace, wide ? 0 : 1);
        };
        return root;
    }

    private View BuildRankingsSection() => BuildOrganizedRankingsSection();

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

    private static string FormatOriginalRecruitingStars(DynastyPlayer player) =>
        player.OriginalRecruitingStars is int stars ? player.RecruitingStarsEstimated ? $"Estimated HS stars: {stars}★" : $"HS recruit: {stars} stars"
            : player.IsWalkOn ? "Walk-on" : "HS recruit: unknown";

    private static string FormatClassYear(int classYear, bool redshirted = false)
        => PlayerClassYearService.Format(classYear, redshirted);

    private Button CreateCareerStatsButton(DynastyPlayer player)
    {
        var button = new Button { Text = "Player profile", FontSize = 12 };
        button.Clicked += async (_, _) => await ShowPlayerProfileAsync(player.PlayerId);
        return button;
    }

    private View BuildMoreSection() =>
        new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                new Label
                {
                    Text = "Preferences",
                    FontAttributes = FontAttributes.Bold
                },

                BuildThemeSettings(),
                BuildLeagueFeatureSettings(),
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
                            Text = "Staff can run recruiting for you",
                            VerticalTextAlignment = TextAlignment.Center,
                            FontAttributes = FontAttributes.Bold
                        }
                    }
                },
                _recruitingAssistanceWorkloadLabel,
                _recruitingAssistanceWorkloadSlider,
                new Label
                {
                    Text = "0% = manual only. 100% = full delegation. Staff can use the selected share of recruiting points for realistic targets, scholarship offers, scouting, and pitches. Your manually managed prospects are not overwritten.",
                    FontSize = 12
                },
                new Label
                {
                    Text = "AUTOSAVE",
                    FontAttributes = FontAttributes.Bold,
                    Margin = new Thickness(0, 10, 0, 0)
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
                _autosaveButton,

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
            using (var coachStream = await FileSystem.OpenAppPackageFileAsync("coaches_2025.csv"))
            using (var coachReader = new StreamReader(coachStream))
                _realCoachSeeds = RealCoachCsvImporter.Read(coachReader);

            await ShowAdvanceProgressAsync("Building simulation profiles...", 0.72);
            _simulationProfiles = LegacyRosterSimulationProfileBuilder.Build(
                roster.Players,
                universe.Teams);

            _teamNames = _teamsByName.Keys
                .OrderBy(name => name)
                .ToArray();

            _legacyTeams = universe.Teams;
            await ShowAdvanceProgressAsync("Loading NCAA NEXT starting roster...", 0.80);
            var nextRosterCsv = await ReadAssetAsync("next26_roster.csv");
            var nextRosterTeams = ModernFbsUniverseFactory.Create(_legacyTeams).Select(t => t.Name).ToArray();
            _bundledStartingRoster = await Task.Run(() => Next26RosterCsvImporter.LoadPack(new StringReader(nextRosterCsv), nextRosterTeams));
            _startingLibraryRepository = new StartingLibraryRepository(Path.Combine(FileSystem.Current.AppDataDirectory, "starting-library.json"));
            try { _startingLibrary = await _startingLibraryRepository.LoadAsync(); }
            catch (Exception ex)
            {
                _startingLibraryRepository = null;
                _startingLibraryError = $"Could not load starting-library.json: {ex.Message}. The original library file was preserved. Saved dynasties can still be loaded.";
            }
            RefreshStartingChoices();
            _teamBrowserPicker.ItemsSource = _teamNames.ToList();

            _importStatus.Text =
                "League presets ready: Modern FBS (138 teams) or Legacy FBS (130 teams).";

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
            _libraryStatus.Text = $"Could not load dynasty data: {ex.Message}";
        }
        finally
        {
            _advanceOverlay.IsVisible = false;
        }
    }

    private void UpdateNewDynastyTeams()
    {
        if (_legacyTeams.Count == 0) return;
        var previous = _teamPicker.SelectedItem as string;
        _newDynastyTeams = SelectedStartingSetup?.Teams ?? [];
        _teamPicker.ItemsSource = _newDynastyTeams.Select(team => team.Name)
            .OrderBy(name => name).ToList();
        if (previous is not null) _teamPicker.SelectedItem = previous;
        UpdateStartingRosterChoices();
    }

    private void ConfigureLeague(IReadOnlyList<Team> teams)
    {
        _teamsByName = teams.ToDictionary(team => team.Name, StringComparer.OrdinalIgnoreCase);
        _teamNames = teams.Select(team => team.Name).OrderBy(name => name).ToArray();
        _teamBrowserPicker.ItemsSource = _teamNames.ToList();
        _cachedCurrentProfiles = null;
        _cachedNationalRankings = null;
    }

    private async void CreateNewDynasty(object? sender, EventArgs e)
    {
        if (_teamPicker.SelectedItem is not string teamName ||
            !_newDynastyTeams.Any(team => team.Name == teamName))
        {
            _newDynastyStatus.Text = "Choose a school before starting your dynasty.";
            return;
        }
        var startingSetup = SelectedStartingSetup;
        if (startingSetup is null) { _newDynastyStatus.Text = "Add a league setup from the main screen."; return; }
        var startingRoster = SelectedStartingRoster;
        _newDynastyStatus.Text = "";
        ConfigureLeague(_newDynastyTeams);
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
                startingYear, startingSetup.Rules);

            await ShowAdvanceProgressAsync("Building team rosters...", 0.42);
            var activeRoster = LegacyDynastyRosterFactory.Create(
                _legacyRosterRows,
                _teamsByName.Values,
                dynastyId,
                startingYear);

            activeRoster = StartingLibraryService.CreateRoster(startingSetup, startingRoster, activeRoster);
            _currentDynasty = new DynastyState
            {
                DynastyId = dynastyId,
                DynastyName = dynastyName,
                LeagueTeams = _newDynastyTeams.ToArray(),
                LeagueName = startingSetup.Name, LeagueRules = startingSetup.Rules with { },
                StartingRosterName = startingRoster?.Name ?? "Default roster",
                UserTeamName = teamName,
                SeasonYear = startingYear,
                Week = 0,
                Phase = SeasonPhase.Preseason,
                RecruitingAssistanceEnabled =
                    _recruitingAssistanceSwitch.IsToggled,
                RecruitingAssistanceWorkloadPercent =
                    Math.Clamp(
                        (int)Math.Round(
                            _recruitingAssistanceWorkloadSlider.Value / 10.0) * 10,
                        0,
                        100),
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
            _currentDynasty = RealCoachCsvImporter.ApplyToNewDynasty(_currentDynasty, _realCoachSeeds);
            await ShowAdvanceProgressAsync("Hiring scouting department...", 0.88);
            _currentDynasty = ScoutingDepartmentService.EnsureDepartment(
                _currentDynasty, _teamsByName[teamName]);

            _currentDynasty = SeasonAwardsService.EnsurePreseason(_currentDynasty);
            _currentDynasty = CollegePollService.Record(_currentDynasty, _teamsByName, BuildCurrentSimulationProfiles());
            await ShowAdvanceProgressAsync("Dynasty ready", 1.0);
            SetDynastyControlsEnabled(true);
            RenderCurrentDynasty();
            ShowSection(AppSection.Dynasty);
        }
        catch (Exception ex)
        {
            _currentDynasty = null;
            _newDynastyStatus.Text =
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
            var decisionState = _currentDynasty;
            _currentDynasty = await Task.Run(() => InteractiveRecruitingService
                .ResolveCurrentPhase(decisionState, midseasonRecruitingTeam, _teamsByName));
        }

        if (phaseBeforeAdvance is
            SeasonPhase.TransferPortal or SeasonPhase.Recruiting)
        {
            await ShowAdvanceProgressAsync("Resolving recruiting and portal activity...", 0.22);
            var marketProgress = new Progress<(int Completed, int Total, string TeamName)>(item =>
            {
                _advanceOverlayStatus.Text = $"Recruiting offers: {item.TeamName} ({item.Completed}/{item.Total})...";
                _advanceOverlayProgress.Progress = 0.22 + 0.07 * item.Completed / item.Total;
            });
            if (_teamsByName.TryGetValue(
                    _currentDynasty.UserTeamName,
                    out var recruitingTeam))
            {
                var decisionState = _currentDynasty;
                _currentDynasty = await Task.Run(() => InteractiveRecruitingService
                    .ResolveCurrentPhase(decisionState, recruitingTeam, _teamsByName, marketProgress));
            }

            if (phaseBeforeAdvance ==
                SeasonPhase.Recruiting)
            {
                var weeklyRecruitingState = _currentDynasty;
                _currentDynasty = await Task.Run(() => CpuRecruitingService
                    .AdvanceCpuRecruitingWeek(weeklyRecruitingState, _teamsByName));
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
                var recruitingProgress = new Progress<(int Completed, int Total, string TeamName)>(item =>
                {
                    _advanceOverlayStatus.Text =
                        $"Resolving recruiting: {item.TeamName} ({item.Completed}/{item.Total})...";
                    _advanceOverlayProgress.Progress = 0.24 + 0.05 * item.Completed / item.Total;
                });
                _currentDynasty = await Task.Run(() =>
                    CpuRecruitingService.ApplyPhaseAssistance(
                        recruitingState,
                        _teamsByName,
                        recruitingState.RecruitingAssistanceEnabled,
                        recruitingProgress));

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
            var simulationState = _currentDynasty;
            var simulationTeams = _teamsByName;
            var simulationProfiles = BuildCurrentSimulationProfiles();
            _currentDynasty = await Task.Run(() => WeekSimulation.SimulateCurrentRegularSeasonWeek(
                simulationState, simulationTeams, simulationProfiles));

            _currentDynasty = InjuryService
                .AdvanceAndGenerateForCurrentWeek(_currentDynasty);

            await ShowAdvanceProgressAsync("Processing the transfer portal...", 0.62);
            _currentDynasty = TransferPortalMarketService
                .AdvanceRegularSeasonWeek(_currentDynasty);

            if (weekAtStart is 3 or 6 or 9 or 12)
            {
                await ShowAdvanceProgressAsync(
                    "Applying automatic player training...", 0.68);
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
        _currentDynasty = ProgramDisciplineService.AdvanceWeek(_currentDynasty);
        _currentDynasty = CollegePollService.Record(_currentDynasty, _teamsByName, BuildCurrentSimulationProfiles());
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
                _currentDynasty = CpuRecruitingService
                    .ApplyWeeklyUserAssistance(
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
            _currentDynasty = CpuRecruitingService
                .ApplyWeeklyUserAssistance(
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
            _currentDynasty = LeagueEvolutionService.AdvanceOffseason(_currentDynasty);
            ConfigureLeague(_currentDynasty.LeagueTeams);
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

        if (_currentDynasty.Phase == SeasonPhase.Preseason)
        {
            _currentDynasty = SeasonAwardsService.EnsurePreseason(_currentDynasty);
            _currentDynasty = CollegePollService.Record(_currentDynasty, _teamsByName, BuildCurrentSimulationProfiles());
        }
        await ShowAdvanceProgressAsync("Saving weekly progress...", 0.90);
        RenderCurrentDynasty();

        if (_rollingAutosaveSwitch.IsToggled && _saveRepository is not null && _currentDynasty.Week == 0)
            await SaveCurrentAsync(SaveKind.AutosaveCurrent);

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
        var snapshot = _currentDynasty;
        var repository = _saveRepository;
        if (snapshot is null || repository is null)
            return;

        Guid? saveId = null;

        if (kind != SaveKind.Manual)
        {
            var existing = (await repository.ListAsync())
                .FirstOrDefault(slot =>
                    slot.DynastyId == snapshot.DynastyId &&
                    slot.Kind == kind);

            saveId = existing?.SaveId;
        }

        await repository.SaveAsync(snapshot, kind, saveId);
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

        ConfigureLeague(state.LeagueTeams.Count > 0 ? state.LeagueTeams : _legacyTeams);

        await ShowAdvanceProgressAsync("Preparing dynasty screen...", 0.90);
        _currentDynasty = state;
        _currentDynasty = CollegePollService.EnsurePublished(_currentDynasty, _teamsByName, BuildCurrentSimulationProfiles());
        _recruitingAssistanceSwitch.IsToggled =
            state.RecruitingAssistanceEnabled;
        _recruitingAssistanceWorkloadSlider.Value =
            Math.Clamp(
                state.RecruitingAssistanceWorkloadPercent,
                0,
                100);
        _recruitingAssistanceWorkloadLabel.Text =
            GetRecruitingAssistanceWorkloadText(
                state.RecruitingAssistanceWorkloadPercent);
        SetDynastyControlsEnabled(true);
        RenderCurrentDynasty();
        ShowSection(AppSection.Dynasty);
        await ShowAdvanceProgressAsync("Dynasty loaded", 1.0);
        }
        catch (Exception error)
        {
            _advanceOverlay.IsVisible = false;
            _libraryStatus.Text = error.Message;
            await DisplayAlertAsync("Could not load dynasty", error.Message, "Close");
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
                Text = "No saved dynasties yet. Start a new dynasty to begin.", FontSize = 13, TextColor = DynastyTheme.Muted
            });
            return;
        }

        foreach (var career in saves.GroupBy(save => save.DynastyId).OrderByDescending(group => group.Max(save => save.UpdatedUtc)))
        {
            var versions = career.OrderByDescending(save => save.UpdatedUtc).ToArray();
            var latest = versions[0];
            var load = new Button { Text = "Load", FontSize = 12 };
            load.Clicked += async (_, _) => await LoadSaveAsync(latest.SaveId);
            var manage = new Button { Text = "Versions", FontSize = 12, BackgroundColor = Colors.Transparent,
                TextColor = DynastyTheme.Muted, Padding = new Thickness(8, 5) };
            manage.Clicked += async (_, _) => await OpenSaveVersionsAsync(versions);
            var info = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center, Children =
            {
                new Label { Text = latest.DynastyName, FontSize = 14, FontAttributes = FontAttributes.Bold },
                new Label { Text = $"{latest.UserTeamName} · {latest.SeasonYear} · {FormatWeek(latest)}", FontSize = 12 },
                new Label { Text = $"Last saved {latest.UpdatedUtc.ToLocalTime():g}", FontSize = 11, TextColor = DynastyTheme.Muted }
            } };
            var actions = new HorizontalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center, Children = { load, manage } };
            var row = new Grid { ColumnSpacing = 10,
                ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
            row.Add(info, 0, 0); row.Add(actions, 1, 0);
            _saveList.Children.Add(new Border { Content = row, Padding = new Thickness(12, 10) });
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
                "The playoff field is selected after conference championships.";
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

        _currentDynastyLabel.Text =
            $"{_currentDynasty.DynastyName}\n" +
            $"Record {wins}-{losses} · Conference {conferenceWins}-{conferenceLosses}\n" +
            $"{_currentDynasty.LeagueName} · {_teamsByName.Count} teams";

        UpdateShellContext();
        RefreshManagementNavigation();
        RenderSection(_activeSection);
        _refreshOpenPlayerProfile?.Invoke();
    }

    private void RenderSection(AppSection section)
    {
        switch (section)
        {
            case AppSection.Dynasty:
                RenderHomeScreen();
                break;

            case AppSection.Schedule:
                RenderUserSchedule();
                break;

            case AppSection.Roster:
                RenderOffseasonRoster();
                break;

            case AppSection.Teams:
                RenderTeamBrowser();
                break;

            case AppSection.Recruiting:
                RenderRecruitingScreen();
                break;

            case AppSection.Stats:
                RenderStatistics();
                break;
            case AppSection.Rankings:
                RenderNationalRankings();
                RenderConferenceRace();
                RenderPostseason();
                break;

            case AppSection.Program:
                RenderProgramScreen();
                break;
            case AppSection.Playbook:
                RenderGamePlanScreen();
                break;

            case AppSection.Season:
                RenderSeasonHub();
                break;
            case AppSection.GameDay:
                RenderMatchupHub();
                break;
            case AppSection.News:
                RenderNews();
                break;
            case AppSection.Injuries:
                RenderInjuryReport();
                break;
            case AppSection.Settings:
            default:
                break;
        }
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

    private async void RenderRecruitingScreen()
    {
        var version = ++_recruitingLoadVersion;
        _recruitingLoadCancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        _recruitingLoadCancellation = cancellation;
        try
        {
            if (_currentDynasty is not null)
            {
                _recruitingLoadingStatus.Text = "Loading recruits…";
                _recruitingLoadingOverlay.IsVisible = _activeSection == AppSection.Recruiting;
                _recruitingLoadingIndicator.IsRunning = _recruitingLoadingOverlay.IsVisible;
                // Give Android a frame to paint the loading screen before preparing data.
                await Task.Delay(16, cancellation.Token);
                var marketInput = _currentDynasty;
                var marketTeams = _teamsByName;
                var market = await Task.Run(() => CompetingRecruitingService.AdvanceOffers(marketInput, marketTeams), cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                if (!ReferenceEquals(marketInput, _currentDynasty)) return;
                _currentDynasty = market;
            }
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

            var snapshot = _currentDynasty;
            var teams = _teamsByName;
            var view = _recruitingView;
            var search = _recruitingSearch.Text?.Trim();
            Position? position = _recruitingPositionFilter.SelectedIndex > 0 &&
                Enum.TryParse<Position>(_recruitingPositionFilter.SelectedItem?.ToString(), out var parsedPosition)
                    ? parsedPosition : null;
            var stars = _recruitingStarFilter.SelectedIndex;
            var stateFilter = _recruitingStateFilter.SelectedIndex;
            var region = _recruitingRegionFilter.SelectedIndex;
            var scouting = (ScoutedFilter)Math.Clamp(_recruitingScoutedFilter.SelectedIndex,
                0, Enum.GetValues<ScoutedFilter>().Length - 1);
            var sort = (RecruitingSort)Math.Clamp(_recruitingSortPicker.SelectedIndex,
                0, Enum.GetValues<RecruitingSort>().Length - 1);
            var result = await Task.Run(() => PrepareRecruitingList(snapshot, teams, view,
                search, position, stars, stateFilter, region, scouting, sort, cancellation.Token), cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            if (version != _recruitingLoadVersion || !ReferenceEquals(snapshot, _currentDynasty)) return;

            _recruitingResultsStatus.Text = result.Total == 0 ? GetEmptyRecruitingMessage()
                : result.Total > result.Rows.Length
                    ? $"Showing {result.Rows.Length} of {result.Total:N0} • use search/filters to narrow the list"
                    : $"{result.Total:N0} players";
            await RenderRecruitingTableAsync(result.Rows, cancellation.Token);

        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            System.Diagnostics.Debug.WriteLine($"Recruit list load failed: {error}");
            if (version == _recruitingLoadVersion)
                _recruitingResultsStatus.Text = "Unable to load recruits. Open Recruit again to retry.";
        }
        finally
        {
            if (version == _recruitingLoadVersion)
            {
                _recruitingLoadingOverlay.IsVisible = false;
                _recruitingLoadingIndicator.IsRunning = false;
                _recruitingLoadCancellation = null;
            }
            cancellation.Dispose();
        }
    }

    private static (int Total, RecruitingRowPresentation[] Rows) PrepareRecruitingList(
        DynastyState state, IReadOnlyDictionary<string, Team> teams, RecruitingView view,
        string? search, Position? selectedPosition, int starFilter, int stateFilter,
        int regionFilter, ScoutedFilter scoutedFilter, RecruitingSort sort, CancellationToken cancellation)
    {
        teams.TryGetValue(state.UserTeamName, out var userTeam);
        var prospects = GetRecruitingProspectsForView(state, view);

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

        if (selectedPosition is not null)
        {
            prospects = prospects
                .Where(prospect =>
                    prospect.Position ==
                        selectedPosition)
                .ToList();
        }

        if (view ==
                RecruitingView.HighSchool &&
            starFilter > 0)
        {
            var stars =
                6 - starFilter;

            prospects = prospects
                .Where(prospect =>
                    prospect.StarRating == stars)
                .ToList();
        }


        var geographyRecruits = view == RecruitingView.HighSchool &&
            (stateFilter > 0 || regionFilter > 0)
                ? state.HighSchoolRecruitingPool.ToDictionary(item => item.RecruitId)
                : null;

        if (view == RecruitingView.HighSchool &&
            stateFilter > 0)
        {
            var stateIndex = stateFilter - 1;
            prospects = prospects.Where(prospect =>
            {
                var recruit = geographyRecruits![prospect.ProspectId];
                return RecruitGeography.GetStateIndex(recruit) == stateIndex;
            }).ToList();
        }

        if (view == RecruitingView.HighSchool &&
            regionFilter > 0)
        {
            var regionIndex = regionFilter - 1;
            prospects = prospects.Where(prospect =>
            {
                var recruit = geographyRecruits![prospect.ProspectId];
                return RecruitGeography.GetRegion(recruit) == regionIndex;
            }).ToList();
        }

        if (scoutedFilter != ScoutedFilter.All)
        {
            prospects = prospects.Where(prospect =>
            {
                var percent = InteractiveRecruitingService.GetInteraction(
                    state, prospect.Source,
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

        prospects = SortRecruitingProspects(prospects, state, userTeam, sort);

        var total = prospects.Count;
        var visible = prospects
            .Take(80)
            .ToArray();

        var rows = new List<RecruitingRowPresentation>(visible.Length);
        foreach (var prospect in visible)
        {
            cancellation.ThrowIfCancellationRequested();
            rows.Add(PrepareRecruitingRow(state, teams, prospect));
        }
        return (total, rows.ToArray());
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
                StrokeThickness = 0,
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
                            FontSize = 14
                        }
                    }
                }
            });
    }

    private static List<RecruitingProspectDisplay>
        GetRecruitingProspectsForView(DynastyState state, RecruitingView view)
    {
        if (view ==
            RecruitingView.HighSchool)
        {
            return state
                .HighSchoolRecruitingPool
                .Where(recruit =>
                    recruit.SeasonYear == 0 ||
                    recruit.SeasonYear ==
                        state.SeasonYear)
                .Select(recruit =>
                    new RecruitingProspectDisplay(
                        RecruitingSource.HighSchool,
                        recruit.RecruitId,
                        recruit.FullName,
                        recruit.Position,
                        $"{recruit.StarRating}★ • {RecruitGeography.GetStateName(recruit)} • {RecruitGeography.GetRegionName(recruit)}",
                        1,
                        false,
                        recruit.PotentialRating,
                        recruit.StarRating,
                        recruit.TrueOverallRating))
                .ToList();
        }

        if (view ==
            RecruitingView.TransferPortal)
        {
            return state
                .TransferPortalEntries
                .Where(entry =>
                    !entry.OriginTeamName.Equals(
                        state.UserTeamName,
                        StringComparison.OrdinalIgnoreCase))
                .Select(entry =>
                    new RecruitingProspectDisplay(
                        RecruitingSource.TransferPortal,
                        entry.Player.PlayerId,
                        entry.Player.FullName,
                        entry.Player.Position,
                        $"{entry.OriginTeamName} • {entry.WeeksInPortal} wk in portal",
                        entry.Player.ClassYear,
                        entry.Player.IsRedshirted || entry.Player.HasRedshirted,
                        entry.Player.PotentialRating > 0
                            ? entry.Player.PotentialRating
                            : null,
                        null,
                        entry.Player.OverallRating))
                .ToList();
        }

        var board = new List<RecruitingProspectDisplay>();

        foreach (var interaction in
                 state.RecruitingInteractions
                     .Where(interaction =>
                         interaction.SeasonYear ==
                             state.SeasonYear &&
                         interaction.IsOnTargetBoard &&
                         interaction.CommittedTeamName is null))
        {
            var prospect =
                TryGetRecruitingProspect(
                    state, interaction.Source,
                    interaction.ProspectId);

            if (prospect is not null)
                board.Add(prospect);
        }

        return board;
    }

    private RecruitingProspectDisplay?
        TryGetRecruitingProspect(RecruitingSource source, Guid prospectId) =>
        _currentDynasty is null ? null : TryGetRecruitingProspect(_currentDynasty, source, prospectId);

    private static RecruitingProspectDisplay?
        TryGetRecruitingProspect(
            DynastyState state, RecruitingSource source,
            Guid prospectId)
    {
        if (source ==
            RecruitingSource.HighSchool)
        {
            var recruit =
                state.HighSchoolRecruitingPool
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
                    false,
                    recruit.PotentialRating,
                    recruit.StarRating,
                    recruit.TrueOverallRating);
        }

        var entry =
            state.TransferPortalEntries
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
                entry.Player.IsRedshirted || entry.Player.HasRedshirted,
                entry.Player.PotentialRating > 0
                    ? entry.Player.PotentialRating
                    : null,
                null,
                entry.Player.OverallRating);
    }

    private static List<RecruitingProspectDisplay>
        SortRecruitingProspects(
            List<RecruitingProspectDisplay> prospects, DynastyState state, Team? userTeam, RecruitingSort sort)
    {
        IEnumerable<RecruitingProspectDisplay> query =
            sort switch
            {
                RecruitingSort.Interest =>
                    prospects.OrderByDescending(
                            prospect => GetRecruitingDisplayInterest(state, userTeam, prospect))
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

    private static int GetRecruitingDisplayInterest(
        DynastyState state, Team? userTeam, RecruitingProspectDisplay prospect)
    {
        if (userTeam is null) return 0;

        var interaction =
            InteractiveRecruitingService.GetInteraction(
                state,
                prospect.Source,
                prospect.ProspectId);

        return RecruitPreferenceService
            .GetCurrentUserInterest(
                state,
                userTeam,
                prospect.Source,
                prospect.ProspectId,
                interaction);
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

    private sealed record RecruitingRowPresentation(RecruitingProspectDisplay Prospect,
        RecruitingInteraction Interaction, string Title, string Ranking, string Projection,
        string Origin, string Scouting, string UserStatus, string MarketStatus);

    private static RecruitingRowPresentation PrepareRecruitingRow(DynastyState state,
        IReadOnlyDictionary<string, Team> teams, RecruitingProspectDisplay prospect)
    {
        var interaction =
            InteractiveRecruitingService.GetInteraction(
                state,
                prospect.Source,
                prospect.ProspectId);

        var range =
            InteractiveRecruitingService.GetScoutedOverallRange(
                state,
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

        var nationalRank = GetProspectNationalRank(state, prospect);
        var recruitRating =
            RecruitingRankingService.GetProspectRating(
                state,
                prospect.Source,
                prospect.ProspectId);
        var recruitStars =
            RecruitingRankingService.GetStars(
                recruitRating);
        var publicRatingText =
            prospect.Source == RecruitingSource.HighSchool
                ? $"247-style {recruitRating:0.00}"
                : $"{recruitStars}★ • 247-style {recruitRating:0.00}";

        var hasUserTeam = teams.TryGetValue(
            state.UserTeamName,
            out var userTeamForInterest);

        var currentUserInterest = hasUserTeam
            ? RecruitPreferenceService.GetCurrentUserInterest(
                state,
                userTeamForInterest!,
                prospect.Source,
                prospect.ProspectId,
                interaction)
            : Math.Clamp(
                interaction.UserInterest,
                0,
                100);

        var activeOffers = CompetingRecruitingService.GetOffers(state, prospect.Source, prospect.ProspectId, activeOnly: true);
        var leadingOffer = activeOffers.FirstOrDefault();
        var userStatus = interaction.CommittedTeamName is not null
            ? $"Committed to {interaction.CommittedTeamName}"
            : $"Your interest: {currentUserInterest}%" +
              (interaction.ScholarshipOffered ? interaction.WasCpuAssisted ? " · CPU-assisted offer" : " · Scholarship offered" : "");
        var marketStatus = interaction.CommittedTeamName is not null
            ? "Recruitment closed"
            : activeOffers.Count == 0 ? "No active scholarship offers"
            : $"{activeOffers.Count} active offer{(activeOffers.Count == 1 ? "" : "s")}" +
              (leadingOffer is null ? "" : $" · Leader: {leadingOffer.TeamName} ({leadingOffer.Interest}%)");

        return new(prospect, interaction,
            $"{boardMarker}{prospect.Position} {prospect.FullName}",
            $"#{nationalRank} nationally · {publicRatingText}", overallText,
            prospect.Subtitle, $"{interaction.ScoutingPercent}% complete", userStatus, marketStatus);
    }

    private void AddRecruitingCompactRow(RecruitingRowPresentation presentation)
    {
        var prospect = presentation.Prospect;
        var interaction = presentation.Interaction;
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
            IsVisible = _recruitingView == RecruitingView.Board && IsRecruitingOpen(prospect.Source),
            IsEnabled =
                interaction.CommittedTeamName is null &&
                IsRecruitingOpen(prospect.Source)
        };
        quickOfferButton.Clicked += (_, _) =>
        {
            if (_currentDynasty is null ||
                !_teamsByName.TryGetValue(
                    _currentDynasty.UserTeamName,
                    out var userTeam))
            {
                return;
            }

            _currentDynasty =
                InteractiveRecruitingService.ToggleScholarship(
                    _currentDynasty,
                    userTeam,
                    prospect.Source,
                    prospect.ProspectId);
            RenderCurrentDynasty();
        };

        static View Metric(string label, string value) => new Border
        {
            StrokeThickness = 0, Padding = new Thickness(9, 7), BackgroundColor = DynastyTheme.Palette("#182630"),
            Content = new VerticalStackLayout { Spacing = 2, Children =
            {
                new Label { Text = label, FontSize = 9, FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Palette("#8FAABD") },
                new Label { Text = value, FontSize = 12, LineBreakMode = LineBreakMode.WordWrap, TextColor = DynastyTheme.Palette("#E6EBF1") }
            } }
        };
        var metrics = new Grid { ColumnSpacing = 7, RowSpacing = 7,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Auto) } };
        metrics.Add(Metric("RANK & RATING", presentation.Ranking), 0, 0);
        metrics.Add(Metric("SCOUTED PROJECTION", presentation.Projection), 1, 0);
        metrics.Add(Metric("ORIGIN", presentation.Origin), 0, 1);
        metrics.Add(Metric("SCOUTING", presentation.Scouting), 1, 1);
        var statusPanel = new Border { StrokeThickness = 0, Padding = new Thickness(9, 7), BackgroundColor = DynastyTheme.Palette("#1B3039"),
            Content = new VerticalStackLayout { Spacing = 3, Children =
            {
                new Label { Text = presentation.UserStatus, FontSize = 12, FontAttributes = FontAttributes.Bold,
                    TextColor = interaction.CommittedTeamName is not null ? DynastyTheme.Palette("#E8C979") : DynastyTheme.Palette("#83D6B6") },
                new Label { Text = presentation.MarketStatus, FontSize = 11, TextColor = DynastyTheme.Palette("#BDE5F5"), LineBreakMode = LineBreakMode.WordWrap }
            } } };
        var info = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                RecruitIdentity(prospect.ProspectId, prospect.FullName, presentation.Title,
                    prospect.Source == RecruitingSource.HighSchool ? "HIGH SCHOOL PROSPECT" : "TRANSFER PROSPECT")
            }
        };
        var actions = new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, JustifyContent = FlexJustify.End };
        var quickScout = new Button { Text = interaction.IsPriorityScout ? "Scout weekly ✓" : "Scout weekly", FontSize = 11,
            Padding = new Thickness(10, 5), IsEnabled = interaction.CommittedTeamName is null && interaction.ScoutingPercent < 100 };
        quickScout.Opacity = quickScout.IsEnabled ? 1 : .4;
        quickScout.Clicked += (_, _) => { if (_currentDynasty is null) return;
            _currentDynasty = ScoutingDepartmentService.TogglePriority(_currentDynasty, prospect.Source, prospect.ProspectId); RenderCurrentDynasty(); };
        foreach (var action in new[] { viewButton, boardButton, quickScout }) { action.Margin = new Thickness(3); actions.Children.Add(action); }
        if (_recruitingView == RecruitingView.Board) actions.Children.Add(quickOfferButton);
        var cardContent = new VerticalStackLayout { Spacing = 8, Children = { info, actions, metrics, statusPanel } };

        _recruitingList.Children.Add(
            new Border
            {
                BackgroundColor = DynastyTheme.Palette(interaction.IsOnTargetBoard ? "#263F48" : "#202F39"),
                Stroke = Color.FromArgb(interaction.ScholarshipOffered ? "#5A9B83" : "#486677"),
                StrokeThickness = 0,
                Padding = new Thickness(12, 10),
                Content = cardContent
            });
    }

    private static int GetProspectNationalRank(
        DynastyState state, RecruitingProspectDisplay prospect)
    {
        if (prospect.Source ==
            RecruitingSource.HighSchool)
        {
            var target = state
                .HighSchoolRecruitingPool
                .FirstOrDefault(recruit =>
                    recruit.RecruitId ==
                    prospect.ProspectId);

            if (target is null)
                return 0;

            var targetRating =
                RecruitingRankingService
                    .GetHighSchoolRating(target);

            return 1 +
                state
                    .HighSchoolRecruitingPool
                    .Where(recruit =>
                        recruit.SeasonYear == 0 ||
                        recruit.SeasonYear ==
                            state.SeasonYear)
                    .Count(recruit =>
                    {
                        var rating =
                            RecruitingRankingService
                                .GetHighSchoolRating(
                                    recruit);

                        return rating >
                                   targetRating ||
                               (Math.Abs(
                                    rating -
                                    targetRating) <
                                0.001 &&
                                string.Compare(
                                    recruit.FullName,
                                    target.FullName,
                                    StringComparison
                                        .OrdinalIgnoreCase) <
                                0);
                    });
        }

        var targetTransfer =
            state.TransferPortalEntries
                .FirstOrDefault(entry =>
                    entry.Player.PlayerId ==
                    prospect.ProspectId);

        if (targetTransfer is null)
            return 0;

        var targetTransferRating =
            RecruitingRankingService
                .GetTransferRating(
                    targetTransfer.Player);

        return 1 +
            state.TransferPortalEntries
                .Count(entry =>
                {
                    var rating =
                        RecruitingRankingService
                            .GetTransferRating(
                                entry.Player);

                    return rating >
                               targetTransferRating ||
                           (Math.Abs(
                                rating -
                                targetTransferRating) <
                            0.001 &&
                            string.Compare(
                                entry.Player.FullName,
                                targetTransfer.Player
                                    .FullName,
                                StringComparison
                                    .OrdinalIgnoreCase) <
                            0);
                });
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

        var hasDetailUserTeam =
            _teamsByName.TryGetValue(
                _currentDynasty.UserTeamName,
                out var detailUserTeam);

        var currentUserInterest = hasDetailUserTeam
            ? RecruitPreferenceService.GetCurrentUserInterest(
                _currentDynasty,
                detailUserTeam!,
                source,
                prospect.ProspectId,
                interaction)
            : Math.Clamp(
                interaction.UserInterest,
                0,
                100);

        var currentRivalInterest = hasDetailUserTeam ? RecruitPreferenceService.GetBestRivalInterest(
            _currentDynasty, detailUserTeam!, _teamsByName, source, prospect.ProspectId) : 0;
        var scholarshipOffers = CompetingRecruitingService.GetOffers(_currentDynasty, source, prospect.ProspectId);
        var offerList = new VerticalStackLayout { Spacing = 2 };
        if (scholarshipOffers.Count == 0)
            offerList.Children.Add(new Label { Text = "No scholarship offers yet.", FontSize = 11 });
        foreach (var offer in scholarshipOffers)
            offerList.Children.Add(new Label
            {
                Text = $"{offer.TeamName}: {offer.Interest}% - {offer.Status}" +
                    (offer.LastContactWeek >= 0 ? $" - last contact W{offer.LastContactWeek}" : ""),
                FontSize = 11,
                Opacity = offer.Status == RecruitingOfferStatus.Active || offer.Status == RecruitingOfferStatus.Signed ? 1 : 0.65
            });

        var preRecruitFavorites =
            RecruitPreferenceService.GetInitialTopSchools(
                _currentDynasty,
                _teamsByName,
                source,
                prospect.ProspectId,
                5);

        var favoritesText =
            "PRE-RECRUIT FAVORITES • " +
            string.Join(
                " • ",
                preRecruitFavorites.Select(item =>
                    $"{item.TeamName} {item.Interest}%"));

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
            if (_currentDynasty is null ||
                !_teamsByName.TryGetValue(
                    _currentDynasty.UserTeamName,
                    out var userTeam))
            {
                return;
            }

            _currentDynasty =
                InteractiveRecruitingService
                    .ToggleScholarship(
                        _currentDynasty,
                        userTeam,
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

        scoutButton.IsVisible = offerButton.IsVisible = pitchButton.IsVisible = pitchPicker.IsVisible = recruitingOpen;
        pitchPicker.IsEnabled = pitchButton.IsEnabled;
        foreach (var action in new[] { scoutButton, offerButton, pitchButton, priorityButton, boardButton })
            action.Opacity = action.IsEnabled ? 1 : .4;
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
        var potentialVisible =
            source == RecruitingSource.TransferPortal
                ? interaction.ScoutingPercent >= 25
                : interaction.ScoutingPercent >= 50;
        var publicRecruitRating =
            RecruitingRankingService.GetProspectRating(
                _currentDynasty,
                source,
                prospect.ProspectId);
        var publicRecruitStars =
            RecruitingRankingService.GetStars(
                publicRecruitRating);
        var publicRecruitRatingText =
            source == RecruitingSource.HighSchool
                ? $"247-style {publicRecruitRating:0.00}"
                : $"{publicRecruitStars}★ • 247-style {publicRecruitRating:0.00}";

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

        static View RecruitMetric(string label, string value, Color? accent = null) => new Border
        {
            StrokeThickness = 0,
            BackgroundColor = DynastyTheme.Palette("#182630"),
            Padding = new Thickness(10, 8),
            Content = new VerticalStackLayout { Spacing = 2, Children =
            {
                new Label { Text = label, FontSize = 9, FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Palette("#8FAABD") },
                new Label { Text = value, FontSize = 13, FontAttributes = FontAttributes.Bold,
                    TextColor = accent ?? DynastyTheme.Palette("#E6EBF1"), LineBreakMode = LineBreakMode.WordWrap }
            } }
        };
        static View RecruitSection(string title, View content) => new Border
        {
            StrokeThickness = 0,
            BackgroundColor = DynastyTheme.Palette("#17242E"),
            Padding = new Thickness(10, 8),
            Content = new VerticalStackLayout { Spacing = 5, Children =
            {
                new Label { Text = title, FontSize = 10, FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Palette("#9EC6E7") },
                content
            } }
        };

        var metricGrid = new Grid
        {
            ColumnSpacing = 7, RowSpacing = 7,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Auto) }
        };
        metricGrid.Add(RecruitMetric("SCOUTED OVR", range.Minimum == range.Maximum ? range.Minimum.ToString() : $"{range.Minimum}-{range.Maximum}"), 0, 0);
        metricGrid.Add(RecruitMetric("POTENTIAL", potentialVisible ? $"{potentialRange.Minimum}-{potentialRange.Maximum}" : "Unknown"), 1, 0);
        metricGrid.Add(RecruitMetric("YOUR INTEREST", $"{currentUserInterest}%", DynastyTheme.Palette("#83D6B6")), 0, 1);
        metricGrid.Add(RecruitMetric("BEST OFFERED RIVAL", $"{currentRivalInterest}%", DynastyTheme.Palette("#E8C979")), 1, 1);

        var marketGrid = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) }
        };
        marketGrid.Add(RecruitSection("SCHOLARSHIP OFFERS", new ScrollView
        {
            HeightRequest = Math.Min(100, Math.Max(30, scholarshipOffers.Count * 22)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = offerList
        }), 0, 0);
        marketGrid.Add(RecruitSection("REVEALED PRIORITIES", preferenceLayout), 1, 0);

        _recruitingDetailHost.Content =
            new Border
            {
                StrokeThickness = 0,
                Padding = new Thickness(12, 10),
                BackgroundColor = DynastyTheme.Palette("#202B37"),
                Content = new VerticalStackLayout
                {
                    Spacing = 9,
                    Children =
                    {
                        RecruitIdentity(prospect.ProspectId, prospect.FullName, $"{prospect.Position} {prospect.FullName}",
                            prospect.Source == RecruitingSource.HighSchool ? "HIGH SCHOOL PROSPECT" : "TRANSFER PROSPECT"),
                        new Label
                        {
                            Text =
                                $"{publicRecruitRatingText} • " +
                                $"{prospect.Subtitle} • {FormatClassYear(prospect.ClassYear, prospect.HasRedshirted)}",
                            FontSize = 11,
                            TextColor = DynastyTheme.Palette("#B8D8FA")
                        },
                        metricGrid,
                        new Label
                        {
                            Text = $"SCOUTED {interaction.ScoutingPercent}%{evaluationText}{lastPitchText}",
                            FontSize = 10, FontAttributes = FontAttributes.Bold,
                            TextColor = recruitingOpen ? DynastyTheme.Palette("#83D6B6") : DynastyTheme.Muted
                        },
                        marketGrid,
                        new Label
                        {
                            Text = favoritesText,
                            FontSize = 10,
                            TextColor = DynastyTheme.Muted,
                            LineBreakMode = LineBreakMode.WordWrap
                        },
                        actions
                    }
                }
            };

        PolishRecruitProfile(prospect, interaction, metricGrid, marketGrid, actions);
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
                StrokeThickness = 0,
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
                            FontSize = 14
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
                    if (_currentDynasty is null ||
                        !_teamsByName.TryGetValue(
                            _currentDynasty.UserTeamName,
                            out var userTeam))
                    {
                        return;
                    }

                    _currentDynasty =
                        InteractiveRecruitingService
                            .ToggleScholarship(
                                _currentDynasty,
                                userTeam,
                                item.Source,
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
                    StrokeThickness = 0,
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

    private static string GetRecruitingAssistanceWorkloadText(
        int workload) =>
        Math.Clamp(workload, 0, 100) switch
        {
            0 => "WORKLOAD 0% • Manual recruiting",
            <= 30 => $"WORKLOAD {workload}% • Light help",
            <= 60 => $"WORKLOAD {workload}% • Shared recruiting",
            <= 90 => $"WORKLOAD {workload}% • Staff-led recruiting",
            _ => "WORKLOAD 100% • Full Auto"
        };

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

        var classRankings =
            RecruitingRankingService.BuildTeamRankings(
                _currentDynasty,
                _teamNames)
            .ToArray();

        _recruitingList.Children.Add(new Label
        {
            Text = "247-STYLE OVERALL CLASS RANKINGS",
            FontAttributes = FontAttributes.Bold
        });
        _recruitingList.Children.Add(new Label
        {
            Text =
                "Best recruits carry the most weight; later commits receive diminishing Gaussian value.",
            FontSize = 11,
            Opacity = 0.75
        });

        foreach (var item in classRankings.Take(25))
        {
            _recruitingList.Children.Add(new Label
            {
                Text =
                    $"#{item.Rank} {item.TeamName} • " +
                    $"{item.TeamScore:0.00} pts • " +
                    $"Avg {item.AverageRecruitRating:0.00} • " +
                    $"{item.CommitCount} commits " +
                    $"({item.HighSchoolCount} HS / {item.TransferCount} portal)",
                FontAttributes = item.TeamName.Equals(
                    _currentDynasty.UserTeamName,
                    StringComparison.OrdinalIgnoreCase)
                    ? FontAttributes.Bold
                    : FontAttributes.None
            });
        }

        var userClass = classRankings.FirstOrDefault(item =>
            item.TeamName.Equals(
                _currentDynasty.UserTeamName,
                StringComparison.OrdinalIgnoreCase));
        if (userClass is not null &&
            userClass.Rank > 25)
        {
            _recruitingList.Children.Add(new Label
            {
                Text =
                    $"#{userClass.Rank} {userClass.TeamName} • " +
                    $"{userClass.TeamScore:0.00} pts • " +
                    $"Avg {userClass.AverageRecruitRating:0.00} • " +
                    $"{userClass.CommitCount} commits " +
                    $"({userClass.HighSchoolCount} HS / {userClass.TransferCount} portal)",
                FontAttributes = FontAttributes.Bold
            });
        }

        _recruitingList.Children.Add(new Label
        {
            Text = "YOUR COMMITMENTS",
            FontAttributes = FontAttributes.Bold,
            TextColor = DynastyTheme.Palette("#9EC6E7"),
            Margin = new Thickness(0, 8, 0, 2)
        });
        foreach (var commitment in userCommitments)
            AddCommitmentRow(commitment, false);

        _recruitingList.Children.Add(new Label
        {
            Text = "AROUND THE NATION",
            FontAttributes = FontAttributes.Bold,
            TextColor = DynastyTheme.Palette("#9EC6E7"),
            Margin = new Thickness(0, 10, 0, 2)
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
        var recruitRating =
            RecruitingRankingService.GetCommitmentRating(
                _currentDynasty!,
                commitment);
        var recruitStars =
            RecruitingRankingService.GetStars(
                recruitRating);

        _recruitingList.Children.Add(new Border
        {
            StrokeThickness = 0,
            Padding = new Thickness(10, 7),
            Content = new Label
            {
                Text = $"{commitment.Position} {commitment.PlayerName} • " +
                       $"{recruitStars}★ • 247-style {recruitRating:0.00} • " +
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

    private void RenderHomeScreen() => RenderOverviewDashboard();

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

        _programList.Children.Add(CreateCoachCareerButton(coach));

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
                StrokeThickness = 0,
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
                TextColor = Colors.White,
                IsEnabled =
                    !_currentDynasty.UserCoachIsFired ||
                    pendingOffers.Length > 1
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
                StrokeThickness = 0,
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
                : $"{FormatStaffRole(role)} · OVR {staff.OverallRating}{status}";

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

            if (staff is not null) info.Children.Insert(0, CoachLink(staff));

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

            _programList.Children.Add(new Border { Padding = 12, Content = row });
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

    private void RenderRosterManagement(IReadOnlyList<DynastyPlayer> userRoster) => RenderRosterGrid(userRoster);

    private View CreateRosterPlayerDetail(
        DynastyPlayer player,
        int playableCount,
        PlayerDevelopmentRecord? development)
    {
        if (_currentDynasty is null)
            return new ContentView();

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

        return new Border
        {
            StrokeThickness = 0,
            Padding = 8,
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    CloseRosterDetailButton(),
                    IdentityHeader(player.PlayerId, player.FullName, player.TeamName,
                        $"{OffensiveLineService.Label(player)} · {FormatClassYear(player.ClassYear, player.IsRedshirted || player.HasRedshirted)} · OVR {player.OverallRating}", appearance: player.Appearance),
                    PlayerMoraleSummary(player),
                    new Label { Text = PlayerPhysicalService.Format(player.HeightInches, player.WeightPounds), TextColor = DynastyTheme.Accent },
                    new Label
                    {
                        Text =
                            $"{depthLabel} • Potential {FormatPotentialStars(player.PotentialRating)} • " +
                            FormatOriginalRecruitingStars(player) +
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
                            $"INJ {player.InjuryRating}",
                        FontSize = 12
                    },
                    CreateCareerStatsButton(player),
                    new Label
                    {
                        Text = development is null
                            ? string.Empty
                            : $"{(development.Stage == PlayerDevelopmentStage.Midseason ? $"{development.SeasonYear} midseason" : $"{development.SeasonYear}→{development.SeasonYear + 1} offseason")}: " +
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
        };
    }

    private IReadOnlyList<PlayerDevelopmentRecord>
        GetLatestUserDevelopmentRecords()
    {
        if (_currentDynasty is null)
            return Array.Empty<PlayerDevelopmentRecord>();

        var records = PlayerDevelopmentService.GetLatestRecords(
            _currentDynasty.PlayerDevelopmentHistory,
            _currentDynasty.UserTeamName);

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

        if (_cachedCurrentProfiles is not null &&
            ReferenceEquals(_cachedProfileGamePlans, _currentDynasty.TeamGamePlans) &&
            ReferenceEquals(
                _cachedProfileRoster,
                _currentDynasty.ActiveRoster) &&
            ReferenceEquals(
                _cachedProfileStaff,
                _currentDynasty.Staff) &&
            ReferenceEquals(
                _cachedProfileUserCoach,
                _currentDynasty.UserHeadCoach) &&
            ReferenceEquals(
                _cachedProfilePrestige,
                _currentDynasty.ProgramPrestigeHistory))
        {
            return _cachedCurrentProfiles;
        }

        _cachedCurrentProfiles =
            DynastyRosterSimulationProfileBuilder.Build(
                _currentDynasty,
                _teamsByName.Values);

        _cachedProfileGamePlans = _currentDynasty.TeamGamePlans;
        _cachedProfileRoster =
            _currentDynasty.ActiveRoster;
        _cachedProfileStaff =
            _currentDynasty.Staff;
        _cachedProfileUserCoach =
            _currentDynasty.UserHeadCoach;
        _cachedProfilePrestige =
            _currentDynasty.ProgramPrestigeHistory;

        _cachedNationalRankings = null;

        return _cachedCurrentProfiles;
    }

    private IReadOnlyList<NationalRanking>
        BuildCurrentNationalRankings()
    {
        if (_currentDynasty is null)
            return Array.Empty<NationalRanking>();

        var profiles =
            BuildCurrentSimulationProfiles();

        if (_cachedNationalRankings is not null && ReferenceEquals(_cachedRankingPollHistory, _currentDynasty.PollHistory) &&
            ReferenceEquals(
                _cachedRankingSchedule,
                _currentDynasty.Schedule) &&
            ReferenceEquals(
                _cachedRankingConferenceHistory,
                _currentDynasty
                    .ConferenceChampionshipHistory) &&
            ReferenceEquals(
                _cachedRankingNationalHistory,
                _currentDynasty
                    .NationalChampionshipHistory) &&
            ReferenceEquals(
                _cachedRankingProfiles,
                profiles))
        {
            return _cachedNationalRankings;
        }

        _cachedNationalRankings =
            CollegePollService.GetPublished(
                _currentDynasty,
                _teamsByName, PollKind.Media,
                profiles);

        _cachedRankingPollHistory = _currentDynasty.PollHistory;
        _cachedRankingSchedule =
            _currentDynasty.Schedule;
        _cachedRankingConferenceHistory =
            _currentDynasty
                .ConferenceChampionshipHistory;
        _cachedRankingNationalHistory =
            _currentDynasty
                .NationalChampionshipHistory;
        _cachedRankingProfiles =
            profiles;

        return _cachedNationalRankings;
    }

    private void RenderTeamBrowser()
    {
        _teamBrowserList.Children.Clear();

        if (_currentDynasty is null ||
            _teamsByName.Count == 0)
        {
            _teamBrowserStatus.Text =
                "No team information is available.";
            return;
        }

        var selectedTeamName =
            _teamBrowserPicker.SelectedItem
                as string;

        if (string.IsNullOrWhiteSpace(
                selectedTeamName) ||
            !_teamsByName.ContainsKey(
                selectedTeamName))
        {
            selectedTeamName =
                _currentDynasty.UserTeamName;

            var selectedIndex =
                _teamNames
                    .Select((name, index) =>
                        new
                        {
                            Name = name,
                            Index = index
                        })
                    .FirstOrDefault(item =>
                        item.Name.Equals(
                            selectedTeamName,
                            StringComparison
                                .OrdinalIgnoreCase))
                    ?.Index ?? -1;

            if (selectedIndex >= 0 &&
                _teamBrowserPicker.SelectedIndex !=
                    selectedIndex)
            {
                _teamBrowserPicker.SelectedIndex =
                    selectedIndex;
            }
        }

        if (!_teamsByName.TryGetValue(
                selectedTeamName,
                out var team))
        {
            _teamBrowserStatus.Text =
                "Program not found.";
            return;
        }

        var playedGames =
            _currentDynasty.Schedule
                .Where(game =>
                    game.SeasonYear ==
                        _currentDynasty.SeasonYear &&
                    game.HasPlayed &&
                    game.InvolvesTeam(team.Name))
                .ToArray();

        var wins = playedGames.Count(game =>
            game.WinnerTeamName?.Equals(
                team.Name,
                StringComparison.OrdinalIgnoreCase) ==
            true);
        var losses =
            playedGames.Length - wins;

        var conferenceGames =
            playedGames.Where(game =>
                game.GameType ==
                    ScheduledGameType.Conference)
                .ToArray();
        var conferenceWins =
            conferenceGames.Count(game =>
                game.WinnerTeamName?.Equals(
                    team.Name,
                    StringComparison.OrdinalIgnoreCase) ==
                true);
        var conferenceLosses =
            conferenceGames.Length -
            conferenceWins;

        var rank =
            BuildCurrentNationalRankings()
                .FirstOrDefault(item =>
                    item.TeamName.Equals(
                        team.Name,
                        StringComparison
                            .OrdinalIgnoreCase))
                ?.Rank;

        var prestige =
            ProgramPrestigeService
                .GetCurrentPrestige(
                    _currentDynasty,
                    team);

        var profile =
            BuildCurrentSimulationProfiles()
                .TryGetValue(
                    team.Name,
                    out var teamProfile)
                ? teamProfile
                : null;

        var roster =
            _currentDynasty.ActiveRoster
                .Where(player =>
                    player.TeamName.Equals(
                        team.Name,
                        StringComparison
                            .OrdinalIgnoreCase))
                .ToArray();

        var commitments =
            _currentDynasty
                .RecruitingCommitments
                .Where(item =>
                    item.SeasonYear ==
                        _currentDynasty.SeasonYear &&
                    item.TeamName.Equals(
                        team.Name,
                        StringComparison
                            .OrdinalIgnoreCase))
                .ToArray();

        _teamBrowserStatus.Text =
            $"{(rank is <= 25 ? $"#{rank} " : string.Empty)}" +
            $"{team.Name} • {wins}-{losses} " +
            $"({conferenceWins}-{conferenceLosses} conf) • " +
            $"Prestige {prestige} • {team.ConferenceName}\n" +
            $"{roster.Length}/{DynastyRosterRules.MaximumRosterSize} players • " +
            $"{commitments.Length} current recruiting commits";

        _teamBrowserList.Children.Add(SchoolBanner(team.Name, $"{team.ConferenceName} · {wins}-{losses} · Prestige {prestige}"));
        if (profile is not null)
        {
            _teamBrowserList.Children.Add(
                new Label
                {
                    Text =
                        $"TEAM RATINGS • OFF {profile.OffenseRating:0.0} • " +
                        $"DEF {profile.DefenseRating:0.0} • " +
                        $"ST {profile.SpecialTeamsRating:0.0}",
                    FontAttributes =
                        FontAttributes.Bold
                });
        }

        _teamBrowserList.Children.Add(
            new Label
            {
                Text = "COACHING STAFF",
                FontAttributes =
                    FontAttributes.Bold,
                Margin =
                    new Thickness(0, 8, 0, 0)
            });

        _teamBrowserList.Children.Add(TeamStaffTable(team.Name));

        var injuries =
            roster.Where(player =>
                    player.CurrentInjury is not null)
                .ToArray();

        if (injuries.Length > 0)
        {
            _teamBrowserList.Children.Add(
                new Label
                {
                    Text =
                        $"INJURIES • {injuries.Length}",
                    FontAttributes =
                        FontAttributes.Bold,
                    TextColor =
                        NegativeChangeColor,
                    Margin =
                        new Thickness(0, 8, 0, 0)
                });
        }

        _teamBrowserList.Children.Add(
            new Label
            {
                Text = "ROSTER",
                FontAttributes =
                    FontAttributes.Bold,
                Margin =
                    new Thickness(0, 10, 0, 0)
            });

        _teamBrowserList.Children.Add(CreateTeamRosterTable(roster));
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

        var rankings = _pollKind.SelectedIndex <= 0 ? BuildCurrentNationalRankings() : CollegePollService.GetPublished(_currentDynasty, _teamsByName, (PollKind)_pollKind.SelectedIndex, BuildCurrentSimulationProfiles());

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

        _nationalRankingStatus.Text += " · Polls update on week advancement";

        var shown = _rankingsRange.SelectedIndex == 1 ? rankings : rankings.Take(25);
        _nationalRankingsList.Children.Add(DataTable(
            [("Rank", 45), ("School", 195), ("W", 38), ("L", 38), ("Conference", 110), ("PPG", 60), ("Opp PPG", 75), ("Score", 70)],
            shown.Select(ranking => new View[]
            {
                TableText($"#{ranking.Rank}", true, ranking.Rank <= 4 ? DynastyTheme.Palette("#F4CC66") : DynastyTheme.Accent),
                SchoolCell(ranking.TeamName, false), TableText(ranking.Wins.ToString()), TableText(ranking.Losses.ToString()),
                TableText(ranking.ConferenceName), TableText(ranking.PointsPerGame.ToString("F1")),
                TableText(ranking.OpponentPointsPerGame.ToString("F1")), TableText(ranking.Score.ToString("F1"))
            })));
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
        else if (field.Length >= 2)
        {
            _postseasonStatus.Text = field.Length == 12
                ? "12-team CFP field: five highest-ranked conference champions + seven at-large teams."
                : $"{field.Length}-team playoff field.";
        }
        else
        {
            _postseasonStatus.Text =
                $"The {_currentDynasty.LeagueRules.PlayoffTeams}-team playoff field is selected after conference championships.";
            return;
        }

        if (field.Length > 0)
            _postseasonList.Children.Add(DataTable([("Seed", 45), ("School", 195), ("Rank", 50), ("Bid", 90), ("Round", 80)],
                field.Select(selection => new View[]
                {
                    TableText(selection.Seed.ToString(), true), SchoolCell(selection.TeamName), TableText($"#{selection.NationalRank}"),
                    TableText(selection.IsAutomaticBid ? "Automatic" : "At-large"), TableText(field.Length == 12 ? selection.Seed <= 4 ? "Bye" : "First round" : field.Length == 8 ? "Quarterfinal" : field.Length == 4 ? "Semifinal" : "Final")
                })));
        var playoffGames = _currentDynasty.Schedule.Where(game => game.SeasonYear == _currentDynasty.SeasonYear &&
                game.GameType == ScheduledGameType.CollegeFootballPlayoff)
            .OrderBy(game => game.Week).ThenBy(game => game.PlayoffBracketSlot).ToArray();
        if (playoffGames.Length > 0)
        {
            _postseasonList.Children.Add(SectionTitle("College Football Playoff", "Bracket progression from the first round through the national championship"));
            _postseasonList.Children.Add(BuildPlayoffTree(playoffGames));
        }
        var bowlGames = _currentDynasty.Schedule.Where(game => game.SeasonYear == _currentDynasty.SeasonYear &&
                game.GameType == ScheduledGameType.Bowl)
            .OrderBy(game => game.Week).ThenBy(game => game.BowlName).ToArray();
        if (bowlGames.Length > 0)
        {
            _postseasonList.Children.Add(SectionTitle("Bowl schedule", "Non-playoff postseason games organized separately"));
            _postseasonList.Children.Add(DataTable([("Bowl", 180), ("Away", 195), ("Home", 195), ("Score (A–H)", 105)],
                bowlGames.Select(game => new View[] { TableText(game.BowlName ?? "Bowl"), SchoolCell(game.AwayTeamName),
                    SchoolCell(game.HomeTeamName), TableText(game.HasPlayed ? $"{game.AwayScore}–{game.HomeScore}" : "Upcoming", true) })));
        }
    }

    private View BuildPlayoffTree(IReadOnlyList<ScheduledGame> games)
    {
        var rounds = new[] { PostseasonRound.FirstRound, PostseasonRound.Quarterfinal,
            PostseasonRound.Semifinal, PostseasonRound.NationalChampionship };
        var tree = new HorizontalStackLayout { Spacing = 10 };
        var initialRound = _currentDynasty?.LeagueRules.PlayoffTeams switch { 2 => PostseasonRound.NationalChampionship, 4 => PostseasonRound.Semifinal, 8 => PostseasonRound.Quarterfinal, _ => PostseasonRound.FirstRound };
        foreach (var round in rounds.Where(r => r >= initialRound))
        {
            var column = new VerticalStackLayout { Spacing = 8, WidthRequest = 235 };
            column.Children.Add(new Label { Text = FormatPostseasonRound(round).ToUpperInvariant(), FontSize = 11,
                FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Palette("#9EC6E7") });
            var roundGames = games.Where(game => game.PostseasonRound == round).OrderBy(game => game.PlayoffBracketSlot).ToArray();
            if (roundGames.Length == 0)
                column.Children.Add(new Label { Text = "Matchups pending", FontSize = 11, TextColor = DynastyTheme.Muted });
            foreach (var game in roundGames)
            {
                var awaySeed = game.AwaySeed is int away ? $"#{away} " : string.Empty;
                var homeSeed = game.HomeSeed is int home ? $"#{home} " : string.Empty;
                column.Children.Add(new Border { StrokeThickness = 0, BackgroundColor = DynastyTheme.Palette("#1B2B37"), Padding = 9,
                    Content = new VerticalStackLayout { Spacing = 5, Children =
                    {
                        new Label { Text = $"{awaySeed}{game.AwayTeamName}  {(game.HasPlayed ? game.AwayScore : "—")}",
                            FontAttributes = game.HasPlayed && game.AwayScore > game.HomeScore ? FontAttributes.Bold : FontAttributes.None },
                        new Label { Text = $"{homeSeed}{game.HomeTeamName}  {(game.HasPlayed ? game.HomeScore : "—")}",
                            FontAttributes = game.HasPlayed && game.HomeScore > game.AwayScore ? FontAttributes.Bold : FontAttributes.None },
                        new Label { Text = game.HasPlayed ? "FINAL" : "UPCOMING", FontSize = 9,
                            TextColor = game.HasPlayed ? DynastyTheme.Palette("#83D6B6") : DynastyTheme.Muted }
                    } } });
            }
            tree.Children.Add(column);
        }
        return new ScrollView { Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = tree };
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

        var conferences = _teamsByName.Values
            .Select(team => team.ConferenceName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var selectedConference = _conferencePicker.SelectedItem as string;
        if (selectedConference is null || !conferences.Contains(selectedConference, StringComparer.OrdinalIgnoreCase))
            selectedConference = userTeam.ConferenceName;
        if (!(_conferencePicker.ItemsSource as IEnumerable<string> ?? []).SequenceEqual(conferences))
        {
            _conferencePicker.ItemsSource = conferences;
            _conferencePicker.SelectedItem = selectedConference;
        }

        var conferenceTeams = _teamsByName.Values
            .Where(team => team.ConferenceName.Equals(
                selectedConference,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (conferenceTeams.Length <
                ConferenceChampionshipService.MinimumConferenceTeams ||
            selectedConference.Equals(
                "Independent",
                StringComparison.OrdinalIgnoreCase))
        {
            _conferenceChampionshipStatus.Text =
                $"{selectedConference}: no conference championship.";
            return;
        }

        var nationalRanks = BuildCurrentNationalRankings()
            .ToDictionary(
                ranking => ranking.TeamName,
                ranking => ranking.Rank,
                StringComparer.OrdinalIgnoreCase);

        var standings = ConferenceStandings.Build(
            _currentDynasty,
            _teamsByName,
            selectedConference,
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
                    selectedConference,
                    StringComparison.OrdinalIgnoreCase));

        var champion = _currentDynasty
            .ConferenceChampionshipHistory
            .FirstOrDefault(record =>
                record.SeasonYear == _currentDynasty.SeasonYear &&
                record.ConferenceName.Equals(
                    selectedConference,
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
                $"{selectedConference} Championship: " +
                $"{titleGame.AwayTeamName} @ {titleGame.HomeTeamName}";
        }
        else
        {
            _conferenceChampionshipStatus.Text =
                $"Top two {selectedConference} teams qualify for the championship.";
        }

        _conferenceStandingsList.Children.Add(DataTable(
            [("Place", 50), ("School", 195), ("Conf W", 65), ("Conf L", 65), ("W", 40), ("L", 40), ("Conf +/-", 80)],
            standings.Select((standing, index) => new View[]
            {
                TableText((index + 1).ToString(), true), SchoolCell(standing.TeamName),
                TableText(standing.ConferenceWins.ToString()), TableText(standing.ConferenceLosses.ToString()),
                TableText(standing.OverallWins.ToString()), TableText(standing.OverallLosses.ToString()),
                TableText(FormatSigned(standing.ConferencePointDifferential), color: GetChangeColor(standing.ConferencePointDifferential))
            })));

        var championshipGames = _currentDynasty.Schedule
            .Where(game => game.SeasonYear == _currentDynasty.SeasonYear &&
                game.GameType == ScheduledGameType.ConferenceChampionship)
            .OrderBy(game => _teamsByName.TryGetValue(game.HomeTeamName, out var home) ? home.ConferenceName : game.HomeTeamName)
            .ToArray();
        if (championshipGames.Length > 0)
        {
            _conferenceStandingsList.Children.Add(SectionTitle("Conference championship games", "Every title matchup and result"));
            _conferenceStandingsList.Children.Add(DataTable(
                [("Conference", 130), ("Away", 195), ("Home", 195), ("Result", 105)],
                championshipGames.Select(game => new View[]
                {
                    TableText(_teamsByName.TryGetValue(game.HomeTeamName, out var home) ? home.ConferenceName : "Conference"),
                    SchoolCell(game.AwayTeamName), SchoolCell(game.HomeTeamName),
                    TableText(game.HasPlayed ? $"{game.AwayScore}–{game.HomeScore}" : "Upcoming", true)
                })));
        }
    }

    private void RenderUserSchedule() => RenderScheduleGrid();

    private Task<IReadOnlyList<ScheduledGame>> GenerateScheduleAsync(
        Guid dynastyId,
        int seasonYear, LeagueRules? rules = null)
    {
        // The conference/OOC scheduler is CPU-bound. Running it on the UI
        // thread makes Android appear frozen while the full 130-team slate is
        // constructed, especially on lower-power devices.
        var teams = _teamsByName.Values.ToArray();
        rules ??= _currentDynasty?.LeagueRules ?? new LeagueRules();

        return Task.Run<IReadOnlyList<ScheduledGame>>(() =>
            FcsLeagueService.BuildSchedule(teams, dynastyId, seasonYear, rules));
    }

    private void SetNewDynastyControlsEnabled(bool enabled)
    {
        _newDynastyButton.IsEnabled = enabled && SelectedStartingSetup is not null;
        _dynastyNameEntry.IsEnabled = enabled;
        _teamPicker.IsEnabled = enabled;
        _leaguePicker.IsEnabled = enabled;
        _startingRosterPicker.IsEnabled = enabled;
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
