
using MadsKristensen.EditorExtensions.Settings;

namespace MadsKristensen.EditorExtensions
{
    public static class RulesFactory
    {
        public static IRule FindRule(string id, string data, BestPractices extension)
        {
            switch (id)
            {
                case "robotstxt":
                    return new RobotsTxtRule(extension);
                //case "favicon":
                //    return new Favicon();
                case "microdata":
                    if (WESettings.Instance.Html.EnableSEOValidation)
                        return new Microdata();
                    break;
                case "description":
                    return new Description(data, extension);
                case "viewport":
                    return new Viewport(data, extension);
            }

            return null;
        }
    }
}
