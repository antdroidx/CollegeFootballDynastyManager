using DynastyManager.Core.Simulation;
namespace DynastyManager.App;
public sealed partial class FoundationPage
{
    Button DraftClassExportButton()
    {
        var button=new Button{Text="Export College draft class",AutomationId="college-export-class"};
        button.Clicked+=async(_,_)=>{
            try
            {
                var file=PlayerExportService.Latest(_currentDynasty!);
                var body=new VerticalStackLayout{Padding=18,Spacing=12};
                body.Add(new Label{Text=$"{file.Year} draft class",FontSize=24,FontAttributes=FontAttributes.Bold,TextColor=DynastyTheme.Text});
                body.Add(new Label{Text=$"{file.Players.Count} graduates and early pro declarations · all schools. Full attributes and illustrated appearances are retained.",TextColor=DynastyTheme.Text});
                foreach(var group in file.Players.GroupBy(p=>p.Position).OrderBy(g=>g.Key))body.Add(new Label{Text=$"{group.Key}: {group.Count()}",TextColor=DynastyTheme.Text});
                var path=Path.Combine(FileSystem.CacheDirectory,"draft-classes",$"college-class-{file.Year}.json");
                var save=new Button{Text="Save JSON file",AutomationId="college-save-class-file"};
                var share=new Button{Text="Share draft class",AutomationId="college-share-class"};var status=new Label{TextColor=DynastyTheme.Text};
                async Task Prepare(){Directory.CreateDirectory(Path.GetDirectoryName(path)!);await File.WriteAllTextAsync(path,PlayerExportService.Serialize(file));}
                save.Clicked+=async(_,_)=>{save.IsEnabled=share.IsEnabled=false;try{await Prepare();var activity=Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as MainActivity??throw new InvalidOperationException("Open this export in the Android app.");status.Text=await activity.SaveDraftClassAsync(path)?"Saved using Android file picker.":"Save canceled. Choose Save or Share to try again.";}catch(Exception ex){await GameDialogs.AlertAsync(this,"Unable to save",ex.Message,"Close");}finally{save.IsEnabled=share.IsEnabled=true;}};
                share.Clicked+=async(_,_)=>{save.IsEnabled=share.IsEnabled=false;try{await Prepare();await Share.Default.RequestAsync(new ShareFileRequest{Title=$"College {file.Year} draft class",File=new ShareFile(path,"application/json")});}catch(Exception ex){await GameDialogs.AlertAsync(this,"Unable to share",ex.Message,"Close");}finally{save.IsEnabled=share.IsEnabled=true;}};
                var close=new Button{Text="Close"};close.Clicked+=async(_,_)=>await Navigation.PopModalAsync();body.Add(status);body.Add(save);body.Add(share);body.Add(close);
                await Navigation.PushModalAsync(new ContentPage{Title="Export draft class",BackgroundColor=DynastyTheme.Background,Content=new ScrollView{Content=body}});
            }
            catch(Exception ex){await GameDialogs.AlertAsync(this,"Draft-class export",ex.Message,"Close");}
        };return button;
    }
}
