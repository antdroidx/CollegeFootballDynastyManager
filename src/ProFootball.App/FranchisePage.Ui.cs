using DynastyManager.App;
using DynastyManager.Core.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    // Shared FM-Touch-style primitives used across all Pro screens.

    // Primary accent call-to-action (large, filled, top-right in card rows).
    View PrimaryAction(string text, Func<Task> action, string? id = null)
    {
        var b = Action(text, action, id);
        b.BackgroundColor = DynastyTheme.Accent;
        b.TextColor = DynastyTheme.IsLight ? Colors.White : Color.FromArgb("#0B1726");
        b.FontAttributes = FontAttributes.Bold;
        b.FontSize = 15;
        b.Padding = new Thickness(18, 10);
        b.CornerRadius = 12;
        b.HorizontalOptions = LayoutOptions.Fill;
        b.MinimumHeightRequest = 52;
        return b;
    }

    static string PositionGroupTitle(Position p) => p switch { Position.QB=>"Quarterbacks",Position.RB=>"Running backs",Position.FB=>"Fullbacks",Position.WR=>"Wide receivers",Position.TE=>"Tight ends",Position.OL=>"Offensive line",Position.DT=>"Defensive tackles",Position.DE=>"Defensive ends",Position.OLB=>"Outside linebackers",Position.MLB=>"Middle linebackers",Position.CB=>"Cornerbacks",Position.FS=>"Free safeties",Position.SS=>"Strong safeties",Position.K=>"Kickers",Position.P=>"Punters",_=>p.DisplayName() };
    // Rounded colored position chip.
    static View PosPill(Position position)
    {
        var (bg, fg) = position switch
        {
            Position.QB => (Color.FromArgb("#C0392B"), Colors.White),
            Position.RB or Position.FB => (Color.FromArgb("#27AE60"), Colors.White),
            Position.WR => (Color.FromArgb("#2980B9"), Colors.White),
            Position.TE => (Color.FromArgb("#8E44AD"), Colors.White),
            Position.OL => (Color.FromArgb("#E67E22"), Colors.White),
            Position.DT or Position.DE => (Color.FromArgb("#16A085"), Colors.White),
            Position.OLB or Position.MLB => (Color.FromArgb("#D4A017"), Color.FromArgb("#172B40")),
            Position.CB => (Color.FromArgb("#2980B9"), Colors.White),
            Position.FS or Position.SS => (Color.FromArgb("#7F8C8D"), Colors.White),
            Position.K => (Color.FromArgb("#34495E"), Colors.White), Position.P => (Color.FromArgb("#5D6D7E"), Colors.White),
            _ => (DynastyTheme.Panel, DynastyTheme.Text)
        };
        return new Border { BackgroundColor = bg, StrokeShape = new RoundRectangle { CornerRadius = 8 }, Padding = new Thickness(8, 2), Content = new Label { Text = position.DisplayName(), TextColor = fg, FontSize = 11, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center } };
    }

    // Consistent page header: large title, muted subtitle, optional action row.
    View PageHeader(string title, string? subtitle = null, params View[] actions)
    {
        var stack = new VerticalStackLayout { Spacing = 4 };
        stack.Add(new Label { Text = title, TextColor = DynastyTheme.Text, FontSize = 24, FontAttributes = FontAttributes.Bold });
        if (!string.IsNullOrWhiteSpace(subtitle)) stack.Add(new Label { Text = subtitle, TextColor = DynastyTheme.Muted, FontSize = 12 });
        if (actions.Length > 0)
        {
            var row = new ActionRow();
            foreach (var a in actions) row.Add(a);
            stack.Add(row);
        }
        return stack;
    }

    // Even row of stat tiles inside one card.
    View StatRow(params View[] tiles)
    {
        var grid = new Grid { ColumnSpacing = 8 };
        foreach (var _ in tiles) grid.ColumnDefinitions.Add(new(GridLength.Star));
        for (var i = 0; i < tiles.Length; i++) grid.Add(tiles[i], i, 0);
        return new Border { Padding = 12, BackgroundColor = DynastyTheme.Surface, StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 }, Content = grid };
    }
    // Compact stat tile: big value over muted caption.
    static View StatTile(string title, string value, string? caption = null)
    {
        var column = new VerticalStackLayout { Spacing = 2 };
        column.Add(new Label { Text = title, TextColor = DynastyTheme.Muted, FontSize = 11 });
        column.Add(new Label { Text = value, TextColor = DynastyTheme.Text, FontSize = 20, FontAttributes = FontAttributes.Bold });
        if (caption is not null) column.Add(new Label { Text = caption, TextColor = DynastyTheme.Muted, FontSize = 11 });
        return column;
    }

    // Labelled 0-100 meter used on coach profile cards.
    static View AbilityBar(string label, int value, Color? color = null)
    {
        var pct = Math.Clamp(value, 0, 100) / 100d;
        var bar = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 8 };
        bar.Add(new Label { Text = label, TextColor = DynastyTheme.Text, FontSize = 12, VerticalTextAlignment = TextAlignment.Center }, 0, 0);
        bar.Add(new Label { Text = value.ToString(), TextColor = DynastyTheme.Text, FontSize = 12, FontAttributes = FontAttributes.Bold, VerticalTextAlignment = TextAlignment.Center }, 1, 0);
        var track = new BoxView { Color = DynastyTheme.Panel, CornerRadius = 4, HeightRequest = 8, VerticalOptions = LayoutOptions.Center };
        var fill = new BoxView { Color = color ?? DynastyTheme.Accent, CornerRadius = 4, HeightRequest = 8, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Start };
        var meter = new Grid { HeightRequest = 8, Margin = new Thickness(0, 2, 0, 0), ColumnDefinitions = { new(GridLength.Star) } };
        meter.Add(track); meter.Add(fill);
        meter.SizeChanged += (_, _) => fill.WidthRequest = Math.Max(0, meter.Width * pct);
        return new VerticalStackLayout { Spacing = 2, Children = { bar, meter } };
    }
    // Divider-free small header card used to group content sections.
    View SectionCard(string title, string? subtitle, params View[] views)
    {
        var content = new VerticalStackLayout { Spacing = 10 };
        if (!string.IsNullOrWhiteSpace(title)) content.Add(new Label { Text = title, TextColor = DynastyTheme.Text, FontSize = 17, FontAttributes = FontAttributes.Bold });
        if (!string.IsNullOrWhiteSpace(subtitle)) content.Add(Text(subtitle, 12, false));
        foreach (var v in views) content.Add(v);
        return new Border { Padding = 14, BackgroundColor = DynastyTheme.Surface, StrokeShape = new RoundRectangle { CornerRadius = 14 }, StrokeThickness = 0, Content = content };
    }

    // Large tile used in the weekly-tasks grid on Overview.
    View TaskTile(string title, string sub, Func<Task> action, string? id = null)
    {
        var column = new VerticalStackLayout { Spacing = 3 };
        column.Add(new Label { Text = title, TextColor = DynastyTheme.Text, FontSize = 14, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation });
        column.Add(new Label { Text = sub, TextColor = DynastyTheme.Muted, FontSize = 11, LineBreakMode = LineBreakMode.TailTruncation });
        var border = new Border
        {
            Padding = 12,
            BackgroundColor = DynastyTheme.Panel,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            StrokeThickness = 0,
            Content = column,
            MinimumHeightRequest = 64,
        };
        var tap = new TapGestureRecognizer(); tap.Tapped += async (_, _) => { if (!busy && !openingOffseason) { busy = true; loading.IsVisible = true; try { await Task.Delay(16); await action(); } catch (Exception e) { await GameDialogs.AlertAsync(this, "Unable to continue", e.Message, "OK"); } finally { busy = false; loading.IsVisible = openingOffseason; } } };
        border.GestureRecognizers.Add(tap); SemanticProperties.SetDescription(border, title + ". " + sub); return border;
    }
    // Space-aware team label: prefer the city name when it fits, else abbreviation.
    internal static string CityLabel(ProTeam team,int maxChars=12)
    {
        if(team is null) return "";
        var city=team.City??string.Empty;
        return string.IsNullOrWhiteSpace(city)||city.Length>maxChars?team.Abbreviation:city;
    }
}