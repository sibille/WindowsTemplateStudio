using Android.Content;
using Param_RootNamespace;
using Param_RootNamespace.Droid;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(wts.ItemNamePage), typeof(wts.ItemNameRenderer))]
namespace Param_RootNamespace.Droid
{
    public class wts.ItemNameRenderer : PageRenderer
    {

        public wts.ItemNameRenderer(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);
        }

    }
}