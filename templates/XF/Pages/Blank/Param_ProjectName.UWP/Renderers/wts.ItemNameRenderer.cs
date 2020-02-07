using Param_RootNamespace.UWP;

using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(wts.ItemNamePage), typeof(wts.ItemNamePageRenderer))]
namespace Param_RootNamespace.UWP
{
    public class wts.ItemNamePageRenderer : PageRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.Page> e)
        {
        
        }

        protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
        {
            
            return base.ArrangeOverride(finalSize);
        }
    }
}