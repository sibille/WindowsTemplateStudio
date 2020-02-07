using Param_RootNamespace;
using Param_RootNamespace.iOS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly:ExportRenderer (typeof(wts.ItemNamePage), typeof(wts.ItemNamePageRenderer))]
namespace Param_RootNamespace.iOS
{
    public class wts.ItemNamePageRenderer : PageRenderer
    {

        protected override void OnElementChanged (VisualElementChangedEventArgs e)
        {
            base.OnElementChanged (e);

            
        }

    }
}