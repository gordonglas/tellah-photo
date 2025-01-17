using RazorEngineCore;
using System.Collections.Concurrent;
using TellahPhotoLibrary.Common;

namespace TellahPhotoLibrary.Html
{
    // uses: https://github.com/adoconnection/RazorEngineCore
    public static class HtmlRenderer
    {
        private static readonly ConcurrentDictionary<int, IRazorEngineCompiledTemplate> TemplateCache = new ConcurrentDictionary<int, IRazorEngineCompiledTemplate>();

        /// <summary>
        /// Renders html from a razor cshtml template file and model.
        /// While TellahPhoto itself requires a minimum runtime version of .NET,
        /// the RazorEngineCore nuget package restricts the version of .NET that can
        /// be used in the template code. See RazorEngineCore to see what version of
        /// .NET is supported in templates.
        /// </summary>
        /// <param name="templateName">Razor cshtml template file. See cshtml files under TellahPhotoConsole/RazorTemplates/</param>
        /// <param name="model">Model that the cshtml file will use to render html</param>
        /// <returns>html string</returns>
        public static string RenderRazorTemplate(string templateName, object model)
        {
            // modified from: https://github.com/adoconnection/RazorEngineCore/wiki/Switch-from-RazorEngine-cshtml-templates

            if (templateName.EndsWith(".cshtml"))
                templateName = templateName.Remove(0, templateName.LastIndexOf(".cshtml"));

            string exePath = PathUtils.GetMainExePath();
            string razorTemplatePath = System.IO.Path.Combine(exePath, "RazorTemplates");

            int hashCode = templateName.GetHashCode();

            IRazorEngineCompiledTemplate compiledTemplate = TemplateCache.GetOrAdd(hashCode, i =>
            {
                RazorEngine razorEngine = new RazorEngine();
                string viewPath = System.IO.Path.Combine(razorTemplatePath, templateName + ".cshtml");
                return razorEngine.Compile(System.IO.File.ReadAllText(viewPath));
            });

            return compiledTemplate.Run(model);
        }

        private static void CopyStaticWebFile(string srcFilePath, string destFilePath)
        {
            if (System.IO.File.Exists(destFilePath))
                System.IO.File.Delete(destFilePath);
            System.IO.File.Copy(srcFilePath, destFilePath);
        }

        public static void CopyStaticWebFiles(string htmlPath)
        {
            string exePath = PathUtils.GetMainExePath();

            // TODO: maybe add swipe support later.
            //       See: https://stackoverflow.com/questions/2264072/detect-a-finger-swipe-through-javascript-on-the-iphone-and-android
            //       and: https://www.html5rocks.com/en/mobile/touchandmouse/
            //CopyStaticWebFile(
            //    System.IO.Path.Combine(exePath, "js", "swiped-events.min.js"),
            //    System.IO.Path.Combine(htmlPath, "swiped-events.min.js"));

            CopyStaticWebFile(
                System.IO.Path.Combine(exePath, "img", "left-arrow-48px.png"),
                System.IO.Path.Combine(htmlPath, "left-arrow-48px.png"));

            CopyStaticWebFile(
                System.IO.Path.Combine(exePath, "img", "right-arrow-48px.png"),
                System.IO.Path.Combine(htmlPath, "right-arrow-48px.png"));

            CopyStaticWebFile(
                System.IO.Path.Combine(exePath, "img", "close-48px.png"),
                System.IO.Path.Combine(htmlPath, "close-48px.png"));

            CopyStaticWebFile(
                System.IO.Path.Combine(exePath, "img", "play-btn.png"),
                System.IO.Path.Combine(htmlPath, "play-btn.png"));
        }
    }
}
