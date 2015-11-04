using Android.App;
using Android.OS;

namespace MaskedEditText.Sample
{
    [Activity(Label = "MaskedEditText Sample", MainLauncher = true, Theme = "@android:style/Theme.Holo.Light")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);
        }
    }
}

