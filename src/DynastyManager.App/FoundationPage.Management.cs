using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Branding;
using DynastyManager.Data.Presentation;
using Microsoft.Maui.Controls.Shapes;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private Action? _refreshOpenPlayerProfile;
    private Grid? _managementRoot;
    private Border? _managementSidebar;
    private readonly Dictionary<AppSection, Button> _managementLinks = new();
    private readonly ContentView _managementBadge = new();
    private bool _navigationExpanded;
    private readonly Button _menuButton = new() { Text = "☰", AutomationId = "management-menu", WidthRequest = 44, BackgroundColor = Colors.Transparent };
    private readonly Grid _rosterWorkspace = new() { ColumnSpacing = 12, RowSpacing = 10,
        ColumnDefinitions = new() { new(GridLength.Star), new(0) }, RowDefinitions = new() { new(GridLength.Auto), new(GridLength.Auto) } };
    private ContentView? _recruitWorkspaceList;

    private Color ManagementAccent
    {
        get
        {
            var brand = TeamBrandCatalog.Get(_currentDynasty?.UserTeamName ?? "Washington");
            return Color.FromArgb(ManagementColors.ReadableAccent(brand.Primary,
                DynastyTheme.IsLight ? "#E7EEF6" : "#192E43"));
        }
    }

    private void ConfigureManagementShell(Grid root, Grid header, Grid pages)
    {
        _managementRoot = root;
        root.ColumnDefinitions.Add(new ColumnDefinition(0));
        root.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        Grid.SetColumn(header, 1); Grid.SetColumn(pages, 1); Grid.SetColumn(_navigationBar, 1);
        var links = new VerticalStackLayout { Spacing = 3, Padding = 8, Children = { _managementBadge } };
        foreach (var (section, title) in new[] {
            (AppSection.Dynasty, "Overview"), (AppSection.Roster, "Roster"), (AppSection.Recruiting, "Recruit"),
            (AppSection.Playbook, "Coach"), (AppSection.Season, "Season"), (AppSection.Schedule, "Schedule"),
            (AppSection.Rankings, "Standings & polls"), (AppSection.Stats, "Statistics"), (AppSection.Program, "Program & staff"),
            (AppSection.News, "News"), (AppSection.Settings, "Settings") })
        {
            var button = new Button { Text = title, HorizontalOptions = LayoutOptions.Fill, FontSize = 12,
                Padding = new Thickness(10, 6), BackgroundColor = Colors.Transparent, TextColor = DynastyTheme.Muted,
                AutomationId = "nav-" + section.ToString().ToLowerInvariant() };
            button.Clicked += (_, _) => { _navigationExpanded = false; ShowSection(section); };
            _managementLinks[section] = button; links.Add(button);
        }
        _managementSidebar = new Border { Padding = 0, BackgroundColor = DynastyTheme.Surface,
            StrokeShape = new RoundRectangle { CornerRadius = 0 }, Content = new ScrollView { Content = links } };
        root.Add(_managementSidebar, 0, 0); Grid.SetRowSpan(_managementSidebar, 3);
        // Exit remains in its original position; menu occupies a separate leading cell.
        header.ColumnDefinitions.Insert(0, new ColumnDefinition(GridLength.Auto));
        foreach (var child in header.Children.OfType<View>()) Grid.SetColumn(child, Grid.GetColumn(child)+1);
        header.Add(_menuButton, 0, 0);
        _menuButton.Clicked += (_, _) => { _navigationExpanded = !_navigationExpanded; ArrangeManagementShell(); };
        root.SizeChanged += (_, _) => ArrangeManagementShell();
    }

    private void ArrangeManagementShell()
    {
        if (_managementRoot is null || _managementSidebar is null) return;
        var inDynasty = _currentDynasty is not null && _activeSection is not (AppSection.Home or AppSection.NewDynasty);
        var width = _managementRoot.Width;
        var side = inDynasty && ManagementLayout.Sidebar(width, _navigationExpanded);
        _managementSidebar.IsVisible = side;
        _managementRoot.ColumnDefinitions[0].Width = side ? ManagementLayout.NavigationWidth(width) : 0;
        _menuButton.IsVisible = inDynasty && width < 600;
        _navigationBar.IsVisible = inDynasty && !side;
        SemanticProperties.SetDescription(_menuButton, side ? "Collapse navigation" : "Expand navigation");
    }

    private void RefreshManagementNavigation()
    {
        if (_managementRoot is null) return;
        foreach (var (section, button) in _managementLinks)
        {
            var selected = section == _activeSection;
            button.BackgroundColor = selected ? DynastyTheme.Panel : Colors.Transparent;
            button.TextColor = selected ? ManagementAccent : DynastyTheme.Muted;
            button.FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None;
        }
        if (_currentDynasty is not null)
            _managementBadge.Content = SchoolBanner(_currentDynasty.UserTeamName, _currentDynasty.SeasonYear.ToString());
        ArrangeManagementShell();
    }

    private static View ManagementPanel(string title, View content, string? subtitle = null) => new Border
    {
        Padding = 10, BackgroundColor = DynastyTheme.Surface, Content = new VerticalStackLayout { Spacing = 6, Children =
        { SectionTitle(title, subtitle ?? ""), content } }
    };

    private static View ResponsivePanels(params View[] panels)
    {
        var grid=new Grid { ColumnSpacing=12,RowSpacing=12 };
        var previous=0;
        void Arrange(int columns)
        {
            if(columns==previous)return;previous=columns;
            grid.Children.Clear();grid.ColumnDefinitions.Clear();grid.RowDefinitions.Clear();
            for(var i=0;i<columns;i++)grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            for(var i=0;i<panels.Length;i++)
            {
                if(i%columns==0)grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                grid.Add(panels[i],i%columns,i/columns);
            }
        }
        Arrange(1); grid.SizeChanged+=(_,_)=>Arrange(grid.Width>=1100?3:grid.Width>=600?2:1);return grid;
    }

    private View ProfileTabs(params (string Title, View Content)[] panels)
    {
        var body = new ContentView();
        var tabs = new HorizontalStackLayout { Spacing = 4 };
        var buttons = new List<Button>();
        void Select(int index)
        {
            body.Content = panels[index].Content;
            for (var i=0;i<buttons.Count;i++)
            { buttons[i].BackgroundColor = i == index ? DynastyTheme.Panel : Colors.Transparent;
              buttons[i].TextColor = i == index ? DynastyTheme.Text : DynastyTheme.Muted; }
        }
        for (var i=0;i<panels.Length;i++)
        {
            var index=i;
            var button = new Button { Text = panels[i].Title, FontSize = 11, Padding = new Thickness(10,5), MinimumHeightRequest = 44 };
            button.Clicked += (_, _) => Select(index); buttons.Add(button); tabs.Add(button);
        }
        Select(0);
        return new VerticalStackLayout { Spacing = 10, Children = {
            new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = tabs }, body } };
    }

    private View BuildRosterWorkspace()
    {
        _rosterWorkspace.Add(_transferPortalList, 0, 0);
        _rosterWorkspace.Add(_rosterDetail, 0, 1);
        _rosterWorkspace.SizeChanged += (_, _) => ArrangeRosterWorkspace();
        return _rosterWorkspace;
    }

    private void ArrangeRosterWorkspace()
    {
        var wide = ManagementLayout.SplitProfile(_rosterWorkspace.Width);
        _transferPortalList.IsVisible = wide || !_rosterDetail.IsVisible;
        _rosterWorkspace.ColumnDefinitions[1].Width = wide && _rosterDetail.IsVisible ? new GridLength(380) : 0;
        Grid.SetColumn(_rosterDetail, wide ? 1 : 0); Grid.SetRow(_rosterDetail, wide ? 0 : 1);
    }

    private View BuildRecruitWorkspace(View results)
    {
        _recruitWorkspaceList = new ContentView { Content = results };
        var detail = new ScrollView { Content = _recruitingDetailHost };
        var grid = new Grid { ColumnSpacing = 12, ColumnDefinitions = new() { new(GridLength.Star), new(0) } };
        grid.Add(_recruitWorkspaceList,0,0); grid.Add(detail,1,0);
        void Arrange()
        {
            var selected = _recruitingDetailHost.IsVisible && _recruitingDetailHost.Content is not null;
            var wide = ManagementLayout.SplitProfile(grid.Width);
            _recruitWorkspaceList.IsVisible = wide || !selected;
            detail.IsVisible = selected;
            grid.ColumnDefinitions[1].Width = selected && wide ? new GridLength(370) : 0;
            Grid.SetColumn(detail, wide ? 1 : 0);
        }
        grid.SizeChanged += (_,_) => Arrange();
        _recruitingDetailHost.PropertyChanged += (_,_) => Arrange();
        Arrange(); return grid;
    }

    private async Task RenderRecruitingTableAsync(RecruitingRowPresentation[] presentations, CancellationToken cancellation)
    {
        var rows = new List<View[]>();
        foreach (var item in presentations)
        {
            cancellation.ThrowIfCancellationRequested();
            var prospect = item.Prospect;
            var name = new Button { Text = prospect.FullName, FontSize = 12, BackgroundColor = Colors.Transparent,
                TextColor = DynastyTheme.Accent, HorizontalOptions = LayoutOptions.Fill, Padding = new Thickness(6,2) };
            SemanticProperties.SetDescription(name, "View recruit " + prospect.FullName);
            name.Clicked += (_,_) => { _selectedRecruitSource=prospect.Source; _selectedRecruitId=prospect.ProspectId; RenderSelectedRecruitDetail(); };
            var quick = new Button { Text = item.Interaction.IsPriorityScout ? "Scouting" : "Scout weekly", FontSize = 10,
                IsEnabled = item.Interaction.CommittedTeamName is null && item.Interaction.ScoutingPercent < 100, Padding = new Thickness(5,2) };
            quick.Clicked += (_,_) => { if (_currentDynasty is null) return;
                _currentDynasty=ScoutingDepartmentService.TogglePriority(_currentDynasty,prospect.Source,prospect.ProspectId); RenderRecruitingScreen(); };
            var board = new Button { Text=item.Interaction.IsOnTargetBoard ? "On board" : "Add", FontSize=10, Padding=new Thickness(5,2),
                IsEnabled=!item.Interaction.IsOnTargetBoard && item.Interaction.CommittedTeamName is null };
            board.Clicked+=(_,_)=>{ if(_currentDynasty is null)return;
                _currentDynasty=InteractiveRecruitingService.ToggleTargetBoard(_currentDynasty,prospect.Source,prospect.ProspectId);RenderRecruitingScreen(); };
            var actions=new HorizontalStackLayout { Spacing=3,Children={quick,board} };
            var appearance=_currentDynasty?.TransferPortalEntries.FirstOrDefault(p=>p.Player.PlayerId==prospect.ProspectId)?.Player.Appearance
                ?? _currentDynasty?.ActiveRoster.FirstOrDefault(p=>p.PlayerId==prospect.ProspectId)?.Appearance;
            var portrait = PersonPortrait.Create(prospect.ProspectId,prospect.FullName,Color.FromArgb("#31566C"),Color.FromArgb("#A9CEF3"),28,appearance:appearance);
            var identity=new Grid { ColumnSpacing=3,ColumnDefinitions=new() {new(28),new(GridLength.Star)} };
            identity.Add(portrait,0,0);identity.Add(name,1,0); identity.SetValue(TableValueProperty,prospect.FullName);
            var starsCell=TableText(new string('★',item.PublicStars), color: DynastyTheme.IsLight ? Color.FromArgb("#806000") : Color.FromArgb("#F4CC66"));
            starsCell.SetValue(TableValueProperty,item.PublicStars.ToString());
            var projection=TableText(item.Projection);projection.SetValue(TableValueProperty,item.Projection.Split('-', ' ')[0]);
            var scouting=TableText(item.Scouting);scouting.SetValue(TableValueProperty,item.Interaction.ScoutingPercent.ToString());
            var interest=TableText(item.UserStatus);
            var percentage=System.Text.RegularExpressions.Regex.Match(item.UserStatus,@"\d+(?=%)").Value;
            interest.SetValue(TableValueProperty,percentage.Length>0?percentage:null);
            rows.Add([TableText(prospect.Position.ToString()), identity,
                starsCell, projection, scouting, interest, actions,
                TableText("#"+item.NationalRank), TableText(item.Origin), TableText(item.MarketStatus)]);
            if (rows.Count % 12 == 0) await Task.Delay(1,cancellation);
        }
        _recruitingList.Add(DataTable([("Pos",44),("Prospect",174),("Stars",76),("Scouted OVR",104),("Scouting",95),
            ("Interest / status",195),("Action",182),("Rank",60),("Origin",175),("Competition",250)],rows, configureColumns:true));
    }

    private void PolishRecruitProfile(RecruitingProspectDisplay prospect, RecruitingInteraction interaction, View metrics, View market, View actions)
    {
        if (_recruitingDetailHost.Content is not Border { Content: VerticalStackLayout old }) return;
        var parts = old.Children.OfType<View>().ToArray(); old.Children.Clear();
        var overview = new VerticalStackLayout { Spacing=10, Children={
            new Label { Text="#"+GetProspectNationalRank(_currentDynasty!,prospect)+" nationally",TextColor=DynastyTheme.Accent,FontAttributes=FontAttributes.Bold },
            parts[1],metrics,parts[3],parts[5]} };
        var recruitment = new VerticalStackLayout { Spacing=10, Children={market,actions} };
        _recruitingDetailHost.Content = new VerticalStackLayout { Spacing=10, Children={RecruitCloseButton(),parts[0],
            ProfileTabs(("Overview",overview),("Recruitment",recruitment)) } };
    }

    private Button RecruitCloseButton()
    {
        var close = new Button { Text="Back to prospects", AutomationId="recruit-profile-back" };
        close.Clicked += (_,_) => { _selectedRecruitId=null; _selectedRecruitSource=null; _recruitingDetailHost.Content=null; _recruitingDetailHost.IsVisible=false; };
        return close;
    }

    private View OrganizePlayerProfile(VerticalStackLayout original, DynastyPlayer player, bool active)
    {
        var parts=original.Children.OfType<View>().ToArray(); original.Children.Clear();
        var overview=new VerticalStackLayout { Spacing=10 };
        var attributes=new VerticalStackLayout { Spacing=10 };
        var history=new VerticalStackLayout { Spacing=10 };
        var destination=overview;
        // Keep every existing view/action; only move them into labelled tabs.
        for(var i=3;i<parts.Length;i++)
        {
            var view=parts[i];
            var title=view is VerticalStackLayout stack ? stack.Children.OfType<Label>().FirstOrDefault()?.Text : null;
            if(title=="Player attributes") destination=attributes;
            if(title is "Honors & awards" or "Player of the Game" or "Career snapshot") destination=history;
            destination.Add(view);
        }
        var summary=new Grid { ColumnSpacing=8, ColumnDefinitions=new() { new(GridLength.Star),new(GridLength.Star) } };
        summary.Add(ManagementPanel("OVERALL",RatingCell(player.OverallRating)),0,0);
        summary.Add(ManagementPanel("POTENTIAL",new Label { Text=active ? FormatPotentialStars(player.PotentialRating) : "Archived", TextColor=DynastyTheme.Accent }),1,0);
        overview.Insert(0,summary);
        // Morale is also visible on the opening overview tab.

        var tabs=new List<(string Title,View Content)> { ("Overview",overview),("Attributes",attributes),("Stats & history",history) };
        if(active && player.TeamName==_currentDynasty?.UserTeamName)
        {
            var roster=_currentDynasty.ActiveRoster.Where(p=>p.TeamName==player.TeamName).ToArray();
            var management=(Border)CreateRosterPlayerDetail(player,roster.Count(p=>p.Position==player.Position&&!p.IsRedshirted),
                GetLatestUserDevelopmentRecords().FirstOrDefault(r=>r.PlayerId==player.PlayerId));
            var source=(VerticalStackLayout)management.Content!;
            var controls=source.Children.OfType<View>().TakeLast(2).ToArray();source.Children.Clear();
            tabs.Add(("Manage",new VerticalStackLayout {Spacing=10,Children={
                SectionTitle("Roster management","Availability follows the current dynasty phase"),controls[0],controls[1]} }));
        }
        return new VerticalStackLayout { Padding=12, Spacing=12, Children={parts[0],parts[1],parts[2],ProfileTabs(tabs.ToArray())} };
    }
}
