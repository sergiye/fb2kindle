using System.Web.Optimization;

namespace jail
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-3.6.0.min.js",
                        "~/Scripts/darkreader.min.js",
                        "~/Scripts/helper.js",
                        "~/Scripts/main.js",
                        "~/Scripts/modal.popup.control.js"));

            // bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
            //             "~/Scripts/jquery.validate*"));
            
            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.min.js"
//                    , "~/Scripts/respond.js"
                      ));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap-theme.min.css",
                      "~/Content/bootstrap.min.css",
                      "~/Content/site.css"));
        }
    }
}
