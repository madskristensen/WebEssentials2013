using System.Threading;
using System.Windows.Forms;

namespace WebEssentialsTests.IntegrationTests
{
    static class Utility
    {
        public static void TypeString(string text)
        {
            SendKeys.SendWait(text);
            Thread.Sleep(400);
        }
    }
}
