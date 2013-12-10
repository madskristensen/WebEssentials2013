
namespace MadsKristensen.EditorExtensions
{
    public static class RulesFactory
    {
        public static IRule FindRule(string id, string data, BestPractices extension)
        {
            switch (id)
            {
                case "robotstxt":
                    return new RobotsTxt(extension);
                //case "favicon":
                //    return new Favicon();
                case "microdata":
                    return new Microdata();
                case "description":
                    return new Description(data, extension);
                case "viewport":
                    return new Viewport(data, extension);
            }

            return null;
        }
    }
}
