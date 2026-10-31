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

    static string PositionGroupTitle(Position p) => p switch { Position.QB=>"Quarterbacks",Position.RB=>"Running backs",Position.FB=>"Fullbacks",Position.WR=>"Wide receivers",Position.TE=>"Tight ends",Position.OL=>"Offensive line",Position.DT=>"Defensive tackles",Position.DE=>"Defensive ends",Position.OLB=>"Outside linebackers",Position.MLB=>"Middle linebackers",Position.CB=>"Cornerbacks",Position.FS=>"Free safeties",Position.SS=>"Strong safeties",Position.K=>"Kickers",_=>p.DisplayName() };
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
            Position.K => (Color.FromArgb("#34495E"), Colors.White),
            _ => (DynastyTheme.Panel, DynastyTheme.Text)
        };
        return new Border { BackgroundColor = bg, StrokeShape = new RoundRectangle { CornerRadius = 8 }, Padding = new Thickness(8, 2), Content = new Label { Text = position.DisplayName(), TextColor = fg, FontSize = 11, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center } };
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

    // Divider-free small header card used to group content sections.
    View SectionCard(string title, string? subtitle, params View[] views)
    {
        var content = new VerticalStackLayout { Spacing = 10 };
        content.Add(new Label { Text = title, TextColor = DynastyTheme.Text, FontSize = 17, FontAttributes = FontAttributes.Bold });
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