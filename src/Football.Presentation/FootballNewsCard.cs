namespace DynastyManager.App;
/// <summary>Identical thumbnail, metadata, headline, preview and story-link alignment for both hosts.</summary>
public static class FootballNewsCard
{
    public static View Layout(IEnumerable<View> source)
    {
        var cards=source.ToArray();var grid=new Grid{ColumnSpacing=12,RowSpacing=12};var previous=0;
        foreach(var card in cards)grid.Add(card);
        void Arrange(){var columns=grid.Width>=720?2:1;if(columns==previous)return;previous=columns;grid.ColumnDefinitions.Clear();grid.RowDefinitions.Clear();for(var c=0;c<columns;c++)grid.ColumnDefinitions.Add(new(GridLength.Star));for(var i=0;i<cards.Length;i++){if(i%columns==0)grid.RowDefinitions.Add(new(GridLength.Auto));Grid.SetColumn(cards[i],i%columns);Grid.SetRow(cards[i],i/columns);}}
        grid.SizeChanged+=(_,_)=>Arrange();Arrange();return grid;
    }
    public static Border Create(View icon,string metadata,string headline,string body,Color accent)
    {
        var heading=new Grid{ColumnSpacing=10,ColumnDefinitions=new(){new(44),new(GridLength.Star)}};
        heading.Add(icon,0,0);heading.Add(new VerticalStackLayout{Spacing=4,Children={new Label{Text=metadata,FontSize=10,TextColor=accent},new Label{Text=headline,FontSize=15,FontAttributes=FontAttributes.Bold,TextColor=DynastyTheme.Text}}},1,0);
        return new Border{Padding=12,BackgroundColor=DynastyTheme.Surface,StrokeThickness=0,Content=new VerticalStackLayout{Spacing=9,Children={heading,new Label{Text=body,FontSize=12,MaxLines=3,LineBreakMode=LineBreakMode.TailTruncation,TextColor=DynastyTheme.Muted},new Label{Text="Read story ›",FontSize=12,TextColor=accent}}}};
    }
}
