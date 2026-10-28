namespace DynastyManager.App;
/// <summary>Explicit grid columns avoid platform flex rounding that wraps the final filter.</summary>
public static class CompactFilters
{
    public static View Create(params (View Control,double Width)[] controls)
    {
        var grid=new Grid{ColumnSpacing=8,RowSpacing=6};var previous=0;
        foreach(var (control,_) in controls){control.WidthRequest=-1;control.MinimumWidthRequest=0;control.HorizontalOptions=LayoutOptions.Fill;control.Margin=0;if(control is Picker p)p.FontSize=12;if(control is InputView input)input.FontSize=12;grid.Add(control);}
        void Arrange()
        {
            var columns=grid.Width>=controls.Length*108-8?controls.Length:grid.Width>=280?Math.Min(2,controls.Length):1;
            if(columns==previous)return;previous=columns;grid.ColumnDefinitions.Clear();grid.RowDefinitions.Clear();
            for(var c=0;c<columns;c++)grid.ColumnDefinitions.Add(new(new GridLength(columns==controls.Length?controls[c].Width:1,GridUnitType.Star)));
            for(var i=0;i<controls.Length;i++){if(i%columns==0)grid.RowDefinitions.Add(new(GridLength.Auto));Grid.SetColumn(controls[i].Control,i%columns);Grid.SetRow(controls[i].Control,i/columns);}
        }
        grid.SizeChanged+=(_,_)=>Arrange();Arrange();return grid;
    }
}
