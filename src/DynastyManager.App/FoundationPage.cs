using DynastyManager.Core.Models;
using DynastyManager.Core.Seasons;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;
using DynastyManager.Data.Persistence;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;

namespace DynastyManager.App;

public sealed class FoundationPage : ContentPage
{
    private readonly Label _importStatus;
    private readonly Entry _dynastyNameEntry;
    private readonly Picker _teamPicker;
    private readonly Button _newDynastyButton;
    private readonly Label _currentDynastyLabel;
    private readonly Button _advanceWeekButton;
    private readonly Button _manualSaveButton;
    private readonly Button _autosaveButton;
    private readonly Switch _rollingAutosaveSwitch;
    private readonly Label _rollingAutosaveStatus;
    private readonly Label _nationalRankingStatus;
    private readonly VerticalStackLayout _nationalRankingsList;
    private readonly Label _postseasonStatus;
    private readonly VerticalStackLayout _postseasonList;
    private readonly Label _conferenceChampionshipStatus;
    private readonly VerticalStackLayout _conferenceStandingsList;
    private readonly VerticalStackLayout _scheduleList;
    private readonly VerticalStackLayout _saveList;

    private SqliteDynastySaveRepository? _saveRepository;
    private DynastyState? _currentDynasty;
    private IReadOnlyDictionary<string, Team> _teamsByName = new Dictionary<string, Team>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<string, TeamSimulationProfile> _simulationProfiles =
        new Dictionary<string, TeamSimulationProfile>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<string> _teamNames = Array.Empty<string>();
    private bool _loaded;

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

        _rollingAutosaveStatus = new Label
        {
            Text = "Keeps the latest three weekly checkpoints automatically.",
            FontSize = 13
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

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24),
                Spacing = 14,
                Children =
                {
                    new Label
                    {
                        Text = "College Football Dynasty Manager",
                        FontSize = 28,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = "Phase 6 — National Rankings & Postseason Foundation",
                        FontSize = 18
                    },
                    _importStatus,

                    new BoxView { HeightRequest = 1 },

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
                        Text = "CURRENT DYNASTY",
                        FontAttributes = FontAttributes.Bold
                    },
                    _currentDynastyLabel,
                    new HorizontalStackLayout
                    {
                        Spacing = 10,
                        Children =
                        {
                            _advanceWeekButton,
                            _manualSaveButton,
                            _autosaveButton
                        }
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
                        Text = "NATIONAL TOP 25",
                        FontAttributes = FontAttributes.Bold
                    },
                    _nationalRankingStatus,
                    _nationalRankingsList,

                    new BoxView { HeightRequest = 1 },

                    new Label
                    {
                        Text = "COLLEGE FOOTBALL PLAYOFF",
                        FontAttributes = FontAttributes.Bold
                    },
                    _postseasonStatus,
                    _postseasonList,

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
                        Text = "USER SCHEDULE",
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = "Each team gets 12 games. A small national Week 0 slate opens the season, followed by Weeks 1–13. Conference members use the legacy conference/OOC mix; Independents play nonconference schedules."
                    },
                    _scheduleList,

                    new BoxView { HeightRequest = 1 },

                    new Label
                    {
                        Text = "SAVE SLOTS",
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = "Manual saves create new slots. Autosave Current updates one slot. Weekly Auto 1–3 rotate automatically after each advanced week when enabled."
                    },
                    _saveList
                }
            }
        };

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (_loaded)
            return;

        _loaded = true;

        try
        {
            var universeCsv = await ReadAssetAsync("legacy_universe.csv");
            var rosterCsv = await ReadAssetAsync("legacy_roster.csv");
            var coachCsv = await ReadAssetAsync("legacy_coach.csv");

            var universe = LegacyUniverseCsvImporter.Import(universeCsv);
            var roster = LegacyRosterCsvImporter.Import(rosterCsv);
            var coaches = LegacyCoachCsvImporter.Import(coachCsv);

            _teamsByName = universe.Teams.ToDictionary(
                team => team.Name,
                StringComparer.OrdinalIgnoreCase);

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

            _saveRepository = new SqliteDynastySaveRepository(databasePath);
            await _saveRepository.InitializeAsync();
            await RefreshSaveSlotsAsync();
        }
        catch (Exception ex)
        {
            _importStatus.Text = $"Startup failed: {ex.Message}";
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
            "Generating the 130-team schedule…";

        try
        {
            var schedule = await GenerateScheduleAsync(
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
                Schedule = schedule
            };

            SetDynastyControlsEnabled(true);
            RenderCurrentDynasty();
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
            SetNewDynastyControlsEnabled(true);
        }
    }

    private async void AdvanceWeek(object? sender, EventArgs e)
    {
        if (_currentDynasty is null)
            return;

        var phaseBeforeAdvance = _currentDynasty.Phase;

        if (phaseBeforeAdvance == SeasonPhase.RegularSeason)
        {
            _currentDynasty = WeekSimulation.SimulateCurrentRegularSeasonWeek(
                _currentDynasty,
                _teamsByName,
                _simulationProfiles);
        }
        else if (phaseBeforeAdvance == SeasonPhase.ConferenceChampionship)
        {
            _currentDynasty = ConferenceChampionshipService
                .SimulateChampionships(
                    _currentDynasty,
                    _teamsByName,
                    _simulationProfiles);
        }
        else if (phaseBeforeAdvance == SeasonPhase.Postseason)
        {
            _currentDynasty = CollegeFootballPlayoffService
                .SimulateCurrentRound(
                    _currentDynasty,
                    _teamsByName,
                    _simulationProfiles);

            _currentDynasty = CollegeFootballPlayoffService
                .ScheduleNextRound(_currentDynasty);
        }

        var wasOffseason = phaseBeforeAdvance == SeasonPhase.Offseason;
        _currentDynasty = SeasonProgression.Advance(_currentDynasty);

        if (phaseBeforeAdvance == SeasonPhase.RegularSeason &&
            _currentDynasty.Phase == SeasonPhase.ConferenceChampionship)
        {
            _currentDynasty = ConferenceChampionshipService
                .ScheduleChampionships(
                    _currentDynasty,
                    _teamsByName,
                    _simulationProfiles);
        }

        if (phaseBeforeAdvance == SeasonPhase.ConferenceChampionship &&
            _currentDynasty.Phase == SeasonPhase.Postseason)
        {
            _currentDynasty = CollegeFootballPlayoffService
                .InitializePlayoff(
                    _currentDynasty,
                    _teamsByName,
                    _simulationProfiles);
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
                    Schedule = schedule
                };
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

        _currentDynasty = state;
        SetDynastyControlsEnabled(true);
        RenderCurrentDynasty();
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

        var weekLabel = _currentDynasty.Phase == SeasonPhase.Preseason
            ? "Preseason"
            : _currentDynasty.Phase == SeasonPhase.RegularSeason
                ? $"Week {_currentDynasty.Week}"
                : $"Week {_currentDynasty.Week}";

        _currentDynastyLabel.Text =
            $"{_currentDynasty.DynastyName}\n" +
            $"{_currentDynasty.UserTeamName} • Record {wins}-{losses} • " +
            $"Conf {conferenceWins}-{conferenceLosses}\n" +
            $"{_currentDynasty.SeasonYear} • {_currentDynasty.Phase} • {weekLabel}\n" +
            $"Dynasty ID: {_currentDynasty.DynastyId}";

        RenderNationalRankings();
        RenderPostseason();
        RenderConferenceRace();
        RenderUserSchedule();
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
            _simulationProfiles);

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
                ? $"{homeScore}-{awayScore}"
                : "Upcoming";

            _postseasonList.Children.Add(new Label
            {
                Text =
                    $"{FormatPostseasonRound(game.PostseasonRound)}: " +
                    $"#{game.AwaySeed} {game.AwayTeamName} vs " +
                    $"#{game.HomeSeed} {game.HomeTeamName} • {result}"
            });
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
                _simulationProfiles)
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
                _ => "OOC"
            };

            _scheduleList.Children.Add(new Label
            {
                Text = $"Week {game.Week}: {location} {opponent} • {gameType} • {status}",
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
        _advanceWeekButton.IsEnabled = enabled;
        _manualSaveButton.IsEnabled = enabled;
        _autosaveButton.IsEnabled = enabled;
    }

    private static string FormatWeek(DynastySaveInfo save) =>
        save.Phase == SeasonPhase.Preseason
            ? "Preseason"
            : $"Week {save.Week}";

    private static async Task<string> ReadAssetAsync(string fileName)
    {
        await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(fileName);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}