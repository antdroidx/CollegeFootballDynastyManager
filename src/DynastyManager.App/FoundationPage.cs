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

    private SqliteDynastySaveRepository? _saveRepository;
    private DynastyState? _currentDynasty;
    private IReadOnlyDictionary<string, Team> _teamsByName = new Dictionary<string, Team>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<string, TeamSimulationProfile> _simulationProfiles =
        new Dictionary<string, TeamSimulationProfile>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<ImportedPlayerRow> _legacyRosterRows =
        Array.Empty<ImportedPlayerRow>();
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
                        Text = "Phase 9 — Player Development & Injuries",
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
                        Text = "ROSTER & TRANSFER PORTAL",
                        FontAttributes = FontAttributes.Bold
                    },
                    _offseasonRosterStatus,
                    _transferPortalList,

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

            _legacyRosterRows = roster.Players;

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
            $"Generating the {_teamsByName.Count}-team schedule…";

        try
        {
            var schedule = await GenerateScheduleAsync(
                dynastyId,
                startingYear);

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
                Schedule = schedule,
                ActiveRoster = activeRoster
            };

            _currentDynasty = PlayerRatingService
                .EnsureProfiles(_currentDynasty);
            _currentDynasty = RosterManagementService
                .NormalizeAllDepthCharts(_currentDynasty);

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

        if (phaseBeforeAdvance is
            SeasonPhase.TransferPortal or SeasonPhase.Recruiting)
        {
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

            SetDynastyControlsEnabled(false);
            _rollingAutosaveStatus.Text =
                phaseBeforeAdvance == SeasonPhase.TransferPortal
                    ? "CPU teams are resolving transfer-portal needs…"
                    : "CPU teams are resolving recruiting classes…";

            var recruitingState = _currentDynasty;
            _currentDynasty = await Task.Run(() =>
                CpuRecruitingService.ApplyPhaseAssistance(
                    recruitingState,
                    _teamsByName));

            _currentDynasty = PlayerRatingService
                .EnsureProfiles(_currentDynasty);

            _currentDynasty = _currentDynasty with
            {
                RecruitingPointsRemaining = 0
            };

            SetDynastyControlsEnabled(true);
        }

        if (phaseBeforeAdvance == SeasonPhase.RosterManagement)
        {
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
            _currentDynasty = WeekSimulation.SimulateCurrentRegularSeasonWeek(
                _currentDynasty,
                _teamsByName,
                BuildCurrentSimulationProfiles());

            _currentDynasty = InjuryService
                .AdvanceAndGenerateForCurrentWeek(_currentDynasty);
        }
        else if (phaseBeforeAdvance == SeasonPhase.ConferenceChampionship)
        {
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
        _currentDynasty = SeasonProgression.Advance(_currentDynasty);

        if (phaseBeforeAdvance == SeasonPhase.Postseason &&
            _currentDynasty.Phase == SeasonPhase.TransferPortal)
        {
            _currentDynasty = OffseasonPlayerLifecycleService
                .EnterTransferPortal(_currentDynasty);

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
            if (_currentDynasty.HighSchoolRecruitingPool.Count == 0)
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

        if (state.ActiveRoster.Count == 0 &&
            _legacyRosterRows.Count > 0)
        {
            state = state with
            {
                ActiveRoster = LegacyDynastyRosterFactory.Create(
                    _legacyRosterRows,
                    _teamsByName.Values,
                    state.DynastyId,
                    state.SeasonYear)
            };
        }

        state = PlayerRatingService.EnsureProfiles(state);
        state = RosterManagementService
            .NormalizeAllDepthCharts(state);

        if (state.Phase == SeasonPhase.TransferPortal &&
            state.TransferPortalEntries.Count == 0)
        {
            state = OffseasonPlayerLifecycleService
                .EnterTransferPortal(state);
        }

        if (state.Phase == SeasonPhase.Recruiting &&
            state.HighSchoolRecruitingPool.Count == 0)
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

        if (state.Phase is
                SeasonPhase.TransferPortal or SeasonPhase.Recruiting &&
            _teamsByName.TryGetValue(
                state.UserTeamName,
                out var phaseTeam))
        {
            state = InteractiveRecruitingService
                .EnsurePhaseInitialized(
                    state,
                    phaseTeam);
        }

        if (state.Phase == SeasonPhase.RosterManagement)
        {
            state = RosterManagementService
                .NormalizeAllDepthCharts(state);
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
            _offseasonRosterStatus.Text =
                "Roster data will appear after a dynasty is created.";
            _transferPortalList.Children.Clear();
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

        RenderOffseasonRoster();
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

        var userPortal = _currentDynasty.TransferPortalEntries
            .Where(entry => entry.OriginTeamName.Equals(
                _currentDynasty.UserTeamName,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var userDepartures = _currentDynasty.RecentPlayerDepartures
            .Where(departure => departure.TeamName.Equals(
                _currentDynasty.UserTeamName,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var graduates = userDepartures.Count(departure =>
            departure.Reason == PlayerDepartureReason.Graduation);
        var earlyPro = userDepartures.Count(departure =>
            departure.Reason == PlayerDepartureReason.EarlyProDeclaration);

        if (_currentDynasty.Phase == SeasonPhase.TransferPortal)
        {
            _offseasonRosterStatus.Text =
                $"TRANSFER PORTAL • Recruiting points: {_currentDynasty.RecruitingPointsRemaining:N0} • " +
                $"{_currentDynasty.TransferPortalEntries.Count:N0} national entries • " +
                $"{_currentDynasty.UserTeamName}: {userRoster.Length}/85 active • " +
                $"{userPortal.Length} transfers out • {graduates} graduates • {earlyPro} early pro\n" +
                "CPU Assist: ON — remaining transfer needs are filled when you advance.";

            if (userPortal.Length > 0)
            {
                _transferPortalList.Children.Add(new Label
                {
                    Text = "YOUR TRANSFERS OUT",
                    FontAttributes = FontAttributes.Bold
                });

                foreach (var entry in userPortal
                             .OrderByDescending(entry => entry.Player.OverallRating)
                             .ThenBy(entry => entry.Player.FullName, StringComparer.OrdinalIgnoreCase)
                             .Take(10))
                {
                    _transferPortalList.Children.Add(new Label
                    {
                        Text =
                            $"{entry.Player.Position} {entry.Player.FullName} • " +
                            $"OVR {entry.Player.OverallRating} • Year {entry.Player.ClassYear}"
                    });
                }
            }

            _transferPortalList.Children.Add(new Label
            {
                Text = "TOP PORTAL TARGETS",
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 6, 0, 0)
            });

            foreach (var entry in _currentDynasty.TransferPortalEntries
                         .Where(entry => !entry.OriginTeamName.Equals(
                             _currentDynasty.UserTeamName,
                             StringComparison.OrdinalIgnoreCase))
                         .OrderByDescending(entry => entry.Player.OverallRating)
                         .ThenByDescending(entry => entry.Player.TalentLevel)
                         .ThenBy(entry => entry.Player.FullName, StringComparer.OrdinalIgnoreCase)
                         .Take(10))
            {
                AddRecruitingTargetCard(
                    RecruitingSource.TransferPortal,
                    entry.Player.PlayerId,
                    entry.Player.FullName,
                    entry.Player.Position,
                    entry.OriginTeamName,
                    entry.Player.ClassYear,
                    null);
            }

            return;
        }

        if (_currentDynasty.Phase == SeasonPhase.Recruiting)
        {
            _offseasonRosterStatus.Text =
                $"HIGH-SCHOOL RECRUITING • Recruiting points: {_currentDynasty.RecruitingPointsRemaining:N0} • " +
                $"{_currentDynasty.HighSchoolRecruitingPool.Count:N0} recruits • " +
                $"{_currentDynasty.UserTeamName}: {userRoster.Length}/85 active\n" +
                "CPU Assist: ON — remaining recruiting needs are filled when you advance.";

            _transferPortalList.Children.Add(new Label
            {
                Text = "TOP HIGH-SCHOOL TARGETS",
                FontAttributes = FontAttributes.Bold
            });

            foreach (var recruit in _currentDynasty.HighSchoolRecruitingPool
                         .OrderByDescending(recruit => recruit.StarRating)
                         .ThenByDescending(recruit => recruit.TrueOverallRating)
                         .ThenBy(recruit => recruit.FullName, StringComparer.OrdinalIgnoreCase)
                         .Take(12))
            {
                AddRecruitingTargetCard(
                    RecruitingSource.HighSchool,
                    recruit.RecruitId,
                    recruit.FullName,
                    recruit.Position,
                    $"{recruit.StarRating}-star recruit",
                    1,
                    recruit.PotentialRating);
            }

            return;
        }

        if (_currentDynasty.Phase == SeasonPhase.RosterManagement)
        {
            RenderRosterManagement(userRoster);
            return;
        }

        var userCommitments = _currentDynasty.RecruitingCommitments
            .Where(commitment =>
                commitment.SeasonYear == _currentDynasty.SeasonYear &&
                commitment.TeamName.Equals(
                    _currentDynasty.UserTeamName,
                    StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var cpuAssisted = userCommitments.Count(commitment =>
            commitment.WasCpuAssisted);

        var manualSignings = userCommitments.Length - cpuAssisted;

        var walkOns = userRoster.Count(player =>
            player.IsWalkOn);

        var safetyText = _currentDynasty.Phase == SeasonPhase.RosterManagement
            ? "Advance once more to fill any remaining shortages with walk-ons."
            : walkOns > 0
                ? $"{walkOns} walk-ons filled remaining roster shortages."
                : "No walk-ons were needed.";

        var recentDevelopment = _currentDynasty.PlayerDevelopmentHistory
            .Where(record =>
                record.TeamName.Equals(
                    _currentDynasty.UserTeamName,
                    StringComparison.OrdinalIgnoreCase) &&
                record.SeasonYear >= _currentDynasty.SeasonYear - 1)
            .ToArray();

        var developmentText = recentDevelopment.Length == 0
            ? string.Empty
            : $" • Dev avg {recentDevelopment.Average(record => record.OverallChange):+0.0;-0.0;0.0}";

        var activeInjuries = userRoster
            .Where(player => player.CurrentInjury is not null)
            .OrderByDescending(player =>
                player.CurrentInjury!.Severity)
            .ThenByDescending(player =>
                player.CurrentInjury!.WeeksRemaining)
            .ToArray();

        _offseasonRosterStatus.Text =
            $"{_currentDynasty.UserTeamName}: {userRoster.Length}/85 active players • " +
            $"{manualSignings} manual signings • {cpuAssisted} CPU-assisted signings" +
            $"{developmentText}. " +
            safetyText;

        if (activeInjuries.Length > 0)
        {
            _transferPortalList.Children.Add(new Label
            {
                Text = "CURRENT INJURIES",
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 6, 0, 0)
            });

            foreach (var player in activeInjuries.Take(12))
            {
                var injury = player.CurrentInjury!;

                _transferPortalList.Children.Add(new Label
                {
                    Text =
                        $"{player.Position} {player.FullName} • OVR {player.OverallRating} • " +
                        $"{injury.BodyArea} • {injury.Severity} • " +
                        $"{injury.WeeksRemaining} wk"
                });
            }
        }
    }

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

        _offseasonRosterStatus.Text =
            $"ROSTER MANAGEMENT • {_currentDynasty.UserTeamName}: " +
            $"{userRoster.Count}/{DynastyRosterRules.MaximumRosterSize} players • " +
            $"{redshirts} redshirts • {walkOns} walk-ons\n" +
            (warnings.Count == 0
                ? "All legacy position minimums are satisfied. "
                : $"POSITION WARNINGS: {string.Join(" • ", warnings)}\n") +
            "Use ↑/↓ to set depth order. Redshirted players do not count toward playable minimums. " +
            "Cuts are filled with walk-ons when you advance.";

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
                AddRosterPlayerCard(player, playableCount);
        }
    }

    private void AddRosterPlayerCard(
        DynastyPlayer player,
        int playableCount)
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
            TextColor = Colors.White
        };

        cutButton.Clicked += async (_, _) =>
        {
            if (_currentDynasty is null)
                return;

            var confirmed = await DisplayAlert(
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
                            $"POT {player.PotentialRating} • Year {player.ClassYear}" +
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

    private void AddRecruitingTargetCard(
        RecruitingSource source,
        Guid prospectId,
        string fullName,
        Position position,
        string subtitle,
        int classYear,
        int? potential)
    {
        if (_currentDynasty is null)
            return;

        var interaction = InteractiveRecruitingService.GetInteraction(
            _currentDynasty,
            source,
            prospectId);

        var range = InteractiveRecruitingService.GetScoutedOverallRange(
            _currentDynasty,
            source,
            prospectId);

        var overallText = range.Minimum == range.Maximum
            ? $"OVR {range.Minimum}"
            : $"OVR {range.Minimum}-{range.Maximum} est.";

        var interestStatus = interaction.CommittedTeamName is not null
            ? $"Committed: {interaction.CommittedTeamName}"
            : $"Interest {interaction.UserInterest} vs rival {interaction.RivalInterest}";

        var details =
            $"{position} {fullName} • {overallText} • {subtitle} • " +
            $"Year {classYear} • Scout {interaction.ScoutingPercent}%";

        if (source == RecruitingSource.HighSchool &&
            potential is not null &&
            interaction.ScoutingPercent >= 75)
        {
            details += $" • POT {potential}";
        }

        var scoutButton = new Button
        {
            Text = $"Scout ({InteractiveRecruitingService.ScoutCost})",
            TextColor = Colors.White,
            IsEnabled =
                interaction.CommittedTeamName is null &&
                interaction.ScoutingPercent < 100 &&
                _currentDynasty.RecruitingPointsRemaining >=
                    InteractiveRecruitingService.ScoutCost
        };

        scoutButton.Clicked += (_, _) =>
        {
            if (_currentDynasty is null)
                return;

            _currentDynasty = InteractiveRecruitingService.Scout(
                _currentDynasty,
                source,
                prospectId);
            RenderCurrentDynasty();
        };

        var offerButton = new Button
        {
            Text = interaction.ScholarshipOffered
                ? "Withdraw Offer"
                : "Offer Scholarship",
            TextColor = Colors.White,
            IsEnabled = interaction.CommittedTeamName is null
        };

        offerButton.Clicked += (_, _) =>
        {
            if (_currentDynasty is null)
                return;

            _currentDynasty = InteractiveRecruitingService.ToggleScholarship(
                _currentDynasty,
                source,
                prospectId);
            RenderCurrentDynasty();
        };

        var pitchButton = new Button
        {
            Text = $"Pitch ({InteractiveRecruitingService.PitchCost})",
            TextColor = Colors.White,
            IsEnabled =
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

            _currentDynasty = InteractiveRecruitingService.Pitch(
                _currentDynasty,
                userTeam,
                source,
                prospectId);
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
                        Text = details,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text =
                            $"{interestStatus} • " +
                            $"{(interaction.ScholarshipOffered ? "Scholarship offered" : "No scholarship offer")}",
                        FontSize = 12
                    },
                    new HorizontalStackLayout
                    {
                        Spacing = 6,
                        Children =
                        {
                            scoutButton,
                            offerButton,
                            pitchButton
                        }
                    }
                }
            }
        });
    }

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