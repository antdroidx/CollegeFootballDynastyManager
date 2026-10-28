using Android.App;
using Android.Content.PM;
using Microsoft.Maui;

namespace DynastyManager.App;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges =
        ConfigChanges.ScreenSize |
        ConfigChanges.Orientation |
        ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize |
        ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    const int ExportDocumentRequest = 0xCF11;
    TaskCompletionSource<Android.Net.Uri?>? exportDocument;
    public async Task<bool> SaveDraftClassAsync(string path)
    {
        if(exportDocument is not null)throw new InvalidOperationException("A file save is already open.");
        var pending=new TaskCompletionSource<Android.Net.Uri?>(TaskCreationOptions.RunContinuationsAsynchronously);exportDocument=pending;
        try
        {
            var intent=new Android.Content.Intent(Android.Content.Intent.ActionCreateDocument);
            intent.AddCategory(Android.Content.Intent.CategoryOpenable);intent.SetType("application/json");intent.PutExtra(Android.Content.Intent.ExtraTitle,System.IO.Path.GetFileName(path));
            StartActivityForResult(intent,ExportDocumentRequest);
            var uri=await pending.Task;if(uri is null)return false;
            using var input=System.IO.File.OpenRead(path);using var output=ContentResolver!.OpenOutputStream(uri,"wt")??throw new IOException("The selected location cannot be written.");
            await input.CopyToAsync(output);await output.FlushAsync();return true;
        }
        finally{exportDocument=null;}
    }
#pragma warning disable CS0672
    protected override void OnActivityResult(int requestCode,[Android.Runtime.GeneratedEnum] Result resultCode,Android.Content.Intent? data)
    {
        base.OnActivityResult(requestCode,resultCode,data);
        if(requestCode==ExportDocumentRequest)exportDocument?.TrySetResult(resultCode==Result.Ok?data?.Data:null);
    }
#pragma warning restore CS0672
}
