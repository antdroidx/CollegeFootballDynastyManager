using DynastyManager.Core.Models;
using DynastyManager.Data.Import;
using DynastyManager.Data.Persistence;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace DynastyManager.App;

public sealed class FoundationPage : ContentPage
{
    private readonly Label _importStatus;
    private readonly Entry _dynastyNameEntry;
    private readonly Picker _teamPicker;
    private readonly Label _currentDynastyLabel;
    private readonly Button _advanceWeekButton;
    private readonly Button _manualSaveButton;
    private readonly Button _autosaveButton;
    private readonly Switch _rollingAutosaveSwitch;
    private readonly Label _rollingAutosaveStatus;
    private readonly VerticalStackLayout _saveList;

    private SqliteDynastySaveRepository? _saveRepository;
    private DynastyState? _currentDynasty;
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

        var newDynastyButton = new Button
        {
            Text = "Create New Dynasty"
        };
        newDynastyButton.Clicked += CreateNewDynasty;

        _currentDynastyLabel = new Label
        {
            Text = "No dynasty loaded.",
            FontSize = 17
        };

        _advanceWeekButton = new Button
        {
            Text = "Advance Week",
            IsEnabled = false
        };
        _advanceWeekButton.Clicked += AdvanceWeek;

        _manualSaveButton = new Button
        {
            Text = "Manual Save",
            IsEnabled = false
        };
        _manualSaveButton.Clicked += async (_, _) => await SaveCurrentAsync(SaveKind.Manual);

        _autosaveButton = new Button
        {
            Text = "Autosave Current",
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
                        Text = "Phase 4 — Dynasty Save Lab",
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
                    newDynastyButton,

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

            _teamNames = universe.Teams
                .Select(team => team.Name)
                .OrderBy(name => name)
                .ToArray();

            _teamPicker.ItemsSource = _teamNames.ToList();

            _importStatus.Text =
                $"Legacy data ready: {universe.Conferences.Count:N0} conferences • " +
                $"{universe.Teams.Count:N0} teams • {roster.Players.Count:N0} players • " +
                $"{coaches.Count:N0} coaches";

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

    private void CreateNewDynasty(object? sender, EventArgs e)
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

        _currentDynasty = new DynastyState
        {
            DynastyName = dynastyName,
            UserTeamName = teamName,
            SeasonYear = 2026,
            Week = 0,
            Phase = SeasonPhase.Preseason
        };

        SetDynastyControlsEnabled(true);
        RenderCurrentDynasty();
    }

    private async void AdvanceWeek(object? sender, EventArgs e)
    {
        if (_currentDynasty is null)
            return;

        _currentDynasty = _currentDynasty.Phase == SeasonPhase.Preseason
            ? _currentDynasty with
            {
                Week = 1,
                Phase = SeasonPhase.RegularSeason
            }
            : _currentDynasty with
            {
                Week = _currentDynasty.Week + 1
            };

        RenderCurrentDynasty();

        if (_rollingAutosaveSwitch.IsToggled && _saveRepository is not null)
        {
            var kind = RollingAutosavePolicy.GetSlotForWeek(_currentDynasty.Week);
            await _saveRepository.SaveRollingWeeklyAsync(_currentDynasty);
            await RefreshSaveSlotsAsync();
            _rollingAutosaveStatus.Text =
                $"Saved Week {_currentDynasty.Week} to {RollingAutosavePolicy.GetDisplayName(kind)}.";
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
                Text = "Load"
            };
            loadButton.Clicked += async (_, _) => await LoadSaveAsync(save.SaveId);

            var deleteButton = new Button
            {
                Text = "Delete"
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
            return;
        }

        _currentDynastyLabel.Text =
            $"{_currentDynasty.DynastyName}\n" +
            $"{_currentDynasty.UserTeamName}\n" +
            $"{_currentDynasty.SeasonYear} • {_currentDynasty.Phase} • " +
            $"{(_currentDynasty.Week == 0 ? "Preseason" : $"Week {_currentDynasty.Week}")}\n" +
            $"Dynasty ID: {_currentDynasty.DynastyId}";
    }

    private void SetDynastyControlsEnabled(bool enabled)
    {
        _advanceWeekButton.IsEnabled = enabled;
        _manualSaveButton.IsEnabled = enabled;
        _autosaveButton.IsEnabled = enabled;
    }

    private static string FormatWeek(DynastySaveInfo save) =>
        save.Week == 0 ? save.Phase.ToString() : $"Week {save.Week}";

    private static async Task<string> ReadAssetAsync(string fileName)
    {
        await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(fileName);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
