namespace DiscordSTFU
{
    using OpenQA.Selenium;
    using OpenQA.Selenium.Firefox;
    using OpenQA.Selenium.Interactions;
    using OpenQA.Selenium.Support.UI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            _ = args;
            using FirefoxDriver driver = new();
            Actions actions = new(driver);

            driver.Navigate().GoToUrl("https://discord.com/");

            var wait = new WebDriverWait(driver, TimeSpan.FromMinutes(30));
            var servers = wait.Until(x => x
                .FindElement(By.CssSelector("div[aria-label=\"Servers\"]"))
                .FindElements(By.CssSelector("div")));

            const int delay = 100;

            foreach (var topServer in servers)
            {
                var topName = GetServerName(topServer, out var isFolder);

                IEnumerable<(string Name, IWebElement Server)> subServers;

                if (isFolder)
                {
                    var children = topServer.FindElements(By.CssSelector("ul>div"));
                    if (children.Count == 0)
                    {
                        topServer.Click();
                        children = topServer.FindElements(By.CssSelector("ul>div"));
                    }

                    subServers = children.Select(x => (GetServerName(x, out _), x));
                }
                else
                {
                    subServers = new[] { (topName, topServer) };
                }

                foreach (var (name, server) in subServers)
                {
                    if (!YNPrompt($"Skip Server \"{name}\"", false))
                    {
                        actions.ContextClick(server);
                        await Task.Delay(delay).ConfigureAwait(false);

                        await MuteSelectedServer(driver, delay).ConfigureAwait(false);
                    }
                }
            }
        }

        public static string GetServerName(IWebElement server, out bool isFolder)
        {
            var name = server
                .FindElement(By.CssSelector("div>div>svg>foreignObject>div"))
                .GetAttribute("aria-label");
            isFolder = name.EndsWith(", folder ");
            return isFolder ? name[..^9] : name[1..];
        }

        public static async Task MuteSelectedServer(IWebDriver driver, int delay)
        {
            var elem = driver.FindElement(By.CssSelector("#guild-context-notifications > div:nth-child(1)"));
            elem.Click();
            await Task.Delay(delay).ConfigureAwait(false);

            const string prefix = "div[class^=\"layer-\"]>div[class^=\"focusLock-\"]>div[class^=\"root-\"]>div[class^=\"content-\"]";

            var mute = driver.FindElement(By.CssSelector(prefix + ":nth-child(1)>div[class^=\"container-\"]>div[class^=\"labelRow-\"]>div[class^=\"control-\"]>div[class^=\"container-\"]>input"));
            if (!mute.Selected) mute.Click();
            await Task.Delay(delay).ConfigureAwait(false);

            var notifs = driver.FindElement(By.CssSelector(prefix + ":nth-child(2)>div[role=\"radiogroup\"]:nth-child(3)"));
            notifs.Click();
            await Task.Delay(delay).ConfigureAwait(false);

            IWebElement MuteCheckbox(int i) => driver.FindElement(By.CssSelector(prefix + ":nth-child(3)>div:nth-child(" + i + ")>div[class^=\"labelRow-\"]>div[class^=\"control-\"]>div[class^=\"container-\"]>input"));
            var everyone = MuteCheckbox(1);
            if (!everyone.Selected) everyone.Click();
            await Task.Delay(delay).ConfigureAwait(false);

            var mentions = MuteCheckbox(2);
            if (!mentions.Selected) mentions.Click();
            await Task.Delay(delay).ConfigureAwait(false);

            var pushNotifs = MuteCheckbox(2);
            if (pushNotifs.Selected) pushNotifs.Click();
            await Task.Delay(delay).ConfigureAwait(false);

            var done = driver.FindElement(By.CssSelector("div[class^=\"layer-\"]>div[class^=\"focusLock-\"]>div[class^=\"root-\"]>div[class^=\"flex-\"]>button"));
            done.Click();
            await Task.Delay(delay).ConfigureAwait(false);
        }

        public static bool YNPrompt(string message, string yesMessage, string noMessage, bool? defaultValue = null)
        {
            bool choice = YNPrompt(message, defaultValue);
            if (choice) Console.WriteLine(yesMessage);
            else Console.WriteLine(noMessage);
            return choice;
        }

        public static bool YNPrompt(string message, bool? defaultValue = null)
        {
            ContinuePrompt:
            Console.Write(message + " " + (defaultValue switch { null => "[y/n]", true => "[Y/n]", false => "[y/N]" }) + " ");

            var input = Console.ReadLine();

            switch (input)
            {
                case "Y":
                case "y":
                    return true;
                case "N":
                case "n":
                    return false;
                case "":
                    if (defaultValue != null) return defaultValue.Value;
                    break;
                default:
                    if (defaultValue != null && string.IsNullOrWhiteSpace(input)) return defaultValue.Value;
                    break;
            }

            Console.WriteLine("Invalid option.");
            goto ContinuePrompt;
        }
    }
}
