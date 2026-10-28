namespace DynastyManager.App;
/// <summary>One compact row when space permits; bounded controls wrap without horizontal scrolling on phones.</summary>
public static class CompactFilters
{
    public static View Create(params (View Control,double Width)[] controls)
    {
        var row=new FlexLayout{Direction=Microsoft.Maui.Layouts.FlexDirection.Row,Wrap=Microsoft.Maui.Layouts.FlexWrap.Wrap,AlignItems=Microsoft.Maui.Layouts.FlexAlignItems.Center};
        foreach(var (control,width) in controls){control.WidthRequest=width;control.MinimumWidthRequest=0;control.Margin=new Thickness(0,0,8,6);if(control is Picker p)p.FontSize=12;if(control is InputView input)input.FontSize=12;row.Add(control);}
        row.SizeChanged+=(_,_)=>{if(row.Width<=0)return;foreach(var (control,width) in controls){var target=row.Width>=650?width:Math.Min(width,Math.Max(100,(row.Width-16)/2));if(Math.Abs(control.WidthRequest-target)>1)control.WidthRequest=target;}};return row;
    }
}
