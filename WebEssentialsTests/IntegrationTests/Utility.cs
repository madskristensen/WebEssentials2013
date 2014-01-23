using System.Windows.Forms;

namespace WebEssentialsTests.IntegrationTests
{
    static class Utility
    {
        public static void TypeString(string text)
        {
            // TODO: Find a way to do this without using SendKeys
            SendKeys.SendWait(text);
            System.Threading.Thread.Sleep(200);
        }
    }
}
