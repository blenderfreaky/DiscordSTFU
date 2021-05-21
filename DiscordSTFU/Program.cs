namespace DiscordSTFU
{
    using OpenQA.Selenium;
    using OpenQA.Selenium.Firefox;
    using OpenQA.Selenium.Support.UI;
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            using FirefoxDriver driver = new();

            driver.Navigate().GoToUrl("https://discord.com/");

            var wait = new WebDriverWait(driver, TimeSpan.FromMinutes(30));
            var servers = wait.Until(x => x.FindElement(By.CssSelector("div[aria-label=\"Servers\"]")));

            foreach (var server in servers)
            {

            }
        }
    }
}
