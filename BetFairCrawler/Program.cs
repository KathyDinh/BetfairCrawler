using Fiddler;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;


namespace BetFairCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            // Note that we're using a desired port of 0, which tells
            // Fiddler to select a random available port to listen on.
            int proxyPort = StartFiddlerProxy(0);


            FiddlerApplication.OnNotification += delegate(object sender, NotificationEventArgs oNEA) { Console.WriteLine("** NotifyUser: " + oNEA.NotifyString); };
            FiddlerApplication.Log.OnLogString += delegate(object sender, LogEventArgs oLEA) { Console.WriteLine("** LogString: " + oLEA.LogString); };

            // Hook up the event for monitoring proxied traffic.
            FiddlerApplication.AfterSessionComplete += delegate(Session targetSession)
            {
                targetSession["X-AutoAuth"] = "(default)";
                Console.WriteLine("Requested resource from URL {0}",
                                targetSession.fullUrl);

                //targetSession.utilDecodeResponse();

                if (!targetSession.RequestHeaders["Content-Type"].Contains("json")) return;

                var responseBody = targetSession.GetResponseBodyAsString();
                var responseBodyDeserialized = JsonConvert.DeserializeObject(responseBody);

                var responseBodySerialized = JsonConvert.SerializeObject(responseBodyDeserialized, Formatting.Indented);
                Console.WriteLine("Response body is {0}",
                                  responseBodySerialized);
            };


            // We are only proxying HTTP traffic, but could just as easily
            // proxy HTTPS or FTP traffic.
            OpenQA.Selenium.Proxy proxy = new OpenQA.Selenium.Proxy();
            proxy.HttpProxy = string.Format("TI01DS74.titansoft.com.sg:{0}", proxyPort);

            // Eventually, we will use different browsers to prove this
            // solution works cross-browser, but for now, we will use
            // Firefox only.
            //FirefoxProfile profile = new FirefoxProfile();
            //profile.SetProxyPreferences(proxy);
            //IWebDriver driver = new FirefoxDriver(profile);
            var proxyHost = "localhost";
            ChromeOptions options = new ChromeOptions();
            options.AddArgument(string.Format("--proxy-server=http={0}:{1};https={0}:{1}", proxyHost, proxyPort));
            IWebDriver driver = new ChromeDriver(options);

            TestStatusCodes(driver);

            Console.WriteLine("Press ESC to stop");
            do
            {
                while (!Console.KeyAvailable)
                {
                    // Do something
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);

            StopFiddlerProxy();

            Console.WriteLine("Complete! Press <Enter> to exit.");
            Console.ReadLine();

            driver.Quit();

        }

        private static int StartFiddlerProxy(int desiredPort)
        {
            // We explicitly do *NOT* want to register this running Fiddler
            // instance as the system proxy. This lets us keep isolation.
            Console.WriteLine("Starting Fiddler proxy");
            FiddlerCoreStartupFlags flags = FiddlerCoreStartupFlags.Default &
                                            ~FiddlerCoreStartupFlags.RegisterAsSystemProxy;

            FiddlerApplication.Startup(desiredPort, flags);
            int proxyPort = FiddlerApplication.oProxy.ListenPort;
            Console.WriteLine("Fiddler proxy listening on port {0}", proxyPort);
            return proxyPort;
        }

        private static void StopFiddlerProxy()
        {
            Console.WriteLine("Shutting down Fiddler proxy");
            FiddlerApplication.Shutdown();
        }

        private static void TestStatusCodes(IWebDriver driver)
        {
            // Using Mozilla's main page, because it demonstrates some of
            // the potential problems with HTTP status code retrieval, and
            // why there is not a one-size-fits-all approach to it.
            string url = "https://www.betfair.com.au/sports/cricket";
            driver.Navigate().GoToUrl(url);

            //string elementId = "promo-1";
            //IWebElement element = driver.FindElement(By.Id(elementId));
            //element.Click();

            //// Demonstrates navigating to a 404 page.
            //url = "http://www.mozilla.org/en-US/doesnotexist.html";
            //driver.Navigate().GoToUrl(url);
        }
    }
}
