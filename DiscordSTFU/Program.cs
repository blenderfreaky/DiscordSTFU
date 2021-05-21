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
        public record ServerOrFolder(IWebElement Element, string Name);

        public static async Task Main(string[] args)
        {
            _ = args;
            using FirefoxDriver driver = new();

            driver.Navigate().GoToUrl("https://discord.com/");

            var wait = new WebDriverWait(driver, TimeSpan.FromMinutes(30));
            var serverElements = wait.Until(x => x
                .FindElement(By.CssSelector("div[aria-label=\"Servers\"]"))
                .FindElements(By.XPath("*")));

            const int delay = 50;

            ServerOrFolder noFolder = new(null!, "No folder");
            Dictionary<ServerOrFolder, List<ServerOrFolder>> serversByFolder = new();

            List<ServerOrFolder> GetServers(ServerOrFolder folder) =>
                serversByFolder.TryGetValue(folder, out var ret) ? ret : serversByFolder[folder] = new();

            foreach (var serverOrFolder in serverElements)
            {
                var serverOrFolderName = GetServerName(serverOrFolder, out var isFolder);

                if (serverOrFolderName == null)
                {
                    Bruh();
                    continue;
                }

                ServerOrFolder folder = new(serverOrFolder, serverOrFolderName);

                if (isFolder)
                {
                    var children = serverOrFolder.FindElements(By.CssSelector("ul>div"));
                    if (children.Count == 0)
                    {
                        driver.ExecuteScript("arguments[0].scrollIntoView(true);", serverOrFolder);
                        new Actions(driver).MoveToElement(serverOrFolder).Click(serverOrFolder).Build().Perform();
                        children = serverOrFolder.FindElements(By.CssSelector("ul>div"));
                        serverOrFolder.Click();
                    }

                    GetServers(folder).AddRange(children.Select(x => new ServerOrFolder(x, GetServerName(x, out _)!)));
                }
                else
                {
                    GetServers(noFolder).Add(new(serverOrFolder, serverOrFolderName!));
                }

                //if (isFolder) topServer.Click();
            }

            List<(ServerOrFolder? Folder, ServerOrFolder Server)> toMute = new();

            foreach (var (folder, servers) in serversByFolder)
            {
                var anc = ANCPrompt($"Mute Folder \"{folder.Name}\"", AllNoneChoose.Choose);

                if (anc == AllNoneChoose.None) continue;

                foreach (var server in servers)
                {
                    if (anc != AllNoneChoose.All && !YNPrompt($"Mute Server \"{server.Name}\"", false))
                    {
                        continue;
                    }

                    toMute.Add((folder, server));
                }
            }

            Console.WriteLine("\nWhen you're ready, press any key and focus into the browser within 5 seconds.");
            Console.ReadKey();
            Console.WriteLine("\nStarting int 5 seconds...");
            await Task.Delay(5000).ConfigureAwait(false);
            Console.WriteLine("\nStarting...");

            foreach (var (folder, server) in toMute)
            {
                driver.ExecuteScript("arguments[0].scrollIntoView(true);", server.Element);
                new Actions(driver).MoveToElement(server.Element).ContextClick(server.Element).Build().Perform();
                await Task.Delay(delay).ConfigureAwait(false);

                await MuteSelectedServer(driver, delay).ConfigureAwait(false);
            }

            Console.WriteLine("Done. Closing Browser.");
            driver.Close();
        }

        public static string? GetServerName(IWebElement server, out bool isFolder)
        {
            var names = server.FindElementOrDefault(By.CssSelector("div>div>svg>foreignObject>div"));
            if (names == null) { isFolder = false; return null; }
            var name = names.GetAttribute("aria-label");
            isFolder = name.EndsWith(", folder ");
            return isFolder ? name[..^9] : name[2..];
        }

        public static IWebElement? FindElementOrDefault(this ISearchContext driver, By by)
        {
            var elements = driver.FindElements(by);
            if (elements.Count == 0)
            {
                return null;
            }

            return elements[0];
        }

        public static bool Bruh()
        {
            Console.WriteLine("Experienced a serious bruh moment");
            return true;
        }

        public static async Task MuteSelectedServer(IWebDriver driver, int delay)
        {
            var elem = driver.FindElementOrDefault(By.CssSelector("div[role=\"group\"]>div#guild-context-notifications"));
            if (elem == null && Bruh())
            {
                return;
            }

            elem!.Click();
            await Task.Delay(delay).ConfigureAwait(false);

            var c1 = driver
                .FindElementOrDefault(By.CssSelector("div[class^=\"layer-\"]>div[class^=\"focusLock-\"]>div[class^=\"root-\"]>div[class^=\"content-\"]"))
                ?.FindElements(By.XPath("*"));
            if (c1 == null)
            {
                Bruh();
                return;
            }

            var mute = c1[0]?.FindElementOrDefault(By.CssSelector("div[class^=\"container-\"]>div[class^=\"labelRow-\"]>div[class^=\"control-\"]>div[class^=\"container-\"]>input"));
            if (mute == null)
            {
                Bruh();
            }
            else if (!mute!.Selected)
            {
                mute.Click();
                await Task.Delay(delay).ConfigureAwait(false);
            }

            var c2 = c1[2]?.FindElements(By.XPath("*"));
            IWebElement? MuteCheckbox(int i) => c2?[i]?
                    .FindElementOrDefault(By.CssSelector("div[class^=\"labelRow-\"]>div[class^=\"control-\"]>div>input"));

            var everyone = MuteCheckbox(0);
            if (everyone == null)
            {
                Bruh();
            }
            else if (!everyone.Selected)
            {
                everyone.Click();
                await Task.Delay(delay).ConfigureAwait(false);
            }

            var mentions = MuteCheckbox(1);
            if (mentions == null)
            {
                Bruh();
            }
            else if (!mentions.Selected)
            {
                mentions.Click();
                await Task.Delay(delay).ConfigureAwait(false);
            }

            var done = driver.FindElementOrDefault(By.CssSelector("div[class^=\"layer-\"]>div[class^=\"focusLock-\"]>div[class^=\"root-\"]>div[class^=\"flex-\"]>button"));
            if (done == null && Bruh())
            {
                return;
            }

            done!.Click();
            await Task.Delay(delay).ConfigureAwait(false);
        }

        public static bool YNPrompt(string message, string yesMessage, string noMessage, bool? defaultValue = null)
        {
            bool choice = YNPrompt(message, defaultValue);
            if (choice)
            {
                Console.WriteLine(yesMessage);
            }
            else
            {
                Console.WriteLine(noMessage);
            }

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
                    if (defaultValue != null)
                    {
                        return defaultValue.Value;
                    }

                    break;
                default:
                    if (defaultValue != null && string.IsNullOrWhiteSpace(input))
                    {
                        return defaultValue.Value;
                    }

                    break;
            }

            Console.WriteLine("Invalid option.");
            goto ContinuePrompt;
        }

        public enum AllNoneChoose
        {
            All,
            None,
            Choose
        }

        public static AllNoneChoose ANCPrompt(string message, AllNoneChoose? defaultValue)
        {
            ContinuePrompt:
            Console.Write(message + " " + (defaultValue switch
            {
                AllNoneChoose.All => "[(A)LL/(n)one/(c)hoose]",
                AllNoneChoose.None => "[(a)ll/(N)ONE/(c)hoose]",
                AllNoneChoose.Choose => "[(a)ll/(n)one/(C)HOOSE]",
                _ => "[all/none/choose]"
            }) + " ");

            var input = Console.ReadLine();

            switch (input)
            {
                case "A":
                case "a":
                    return AllNoneChoose.All;
                case "N":
                case "n":
                    return AllNoneChoose.None;
                case "C":
                case "c":
                    return AllNoneChoose.Choose;
                case "":
                    if (defaultValue != null)
                    {
                        return defaultValue.Value;
                    }

                    break;
                default:
                    if (defaultValue != null && string.IsNullOrWhiteSpace(input))
                    {
                        return defaultValue.Value;
                    }

                    break;
            }

            Console.WriteLine("Invalid option.");
            goto ContinuePrompt;
        }
    }
}
