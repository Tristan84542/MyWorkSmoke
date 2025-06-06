using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Text.RegularExpressions;



namespace PlaywrightTests;

public class CMom : CMParam
{
    public static IPlaywright? pw;
    public static IBrowser? browser;
    public static IBrowserContext? context;
    public static IPage? tp;
    public static int dfTimeout;
    public static string environment;

    [SetUp]
    public static async Task CMInit()
    {
        environment = TestContext.Parameters.Get("Environment", "QA");
        string browserName = TestContext.Parameters.Get("BrowserName", "chromium");
        Boolean headless = TestContext.Parameters.Get("Headless", false);
        string channel = TestContext.Parameters.Get("Channel", "chrome");
        dfTimeout = TestContext.Parameters.Get("Timeout", 60000);

        //Create playwright
        pw = await Playwright.CreateAsync();
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = headless,
            Channel = channel
        };
        //Create browser
        switch (browserName.ToLower())
        {
            case "chromium":
                browser = await pw.Chromium.LaunchAsync(launchOptions);
                break;
            case "firefox":
                browser = await pw.Firefox.LaunchAsync(launchOptions);
                break;
            case "webkit":
                browser = await pw.Webkit.LaunchAsync(launchOptions);
                break;
            default:
                throw new ArgumentException($"Unsupported browser: {browserName}");
        }
        //Create browsercontext
        context = await browser.NewContextAsync();
        tp = await context.NewPageAsync();

        //Initialize CMParams
        CMParam.InitParam(environment);
    }

    [TearDown]
    public static async Task TearDown()
    {
        await context.CloseAsync();
        await browser.CloseAsync();
        pw.Dispose();
    }

    public static async Task DelayMS (int ms)
    {
        await tp.WaitForTimeoutAsync(ms);
    }
    public static async Task DelayS (int s)
    {
        await tp.WaitForTimeoutAsync(s);
    }
    public static async Task waitState(string state)
    {
        switch (state.ToLower())
        {
            case "load":
                await tp.WaitForLoadStateAsync(LoadState.Load);
                break;
            case "idle":
                await tp.WaitForLoadStateAsync(LoadState.NetworkIdle);
                break;
            case "dom":
                await tp.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                break;
            default:
                Console.WriteLine("Invalid load state param (load / idle / dom)");
                break;
        }
    }
    public static async Task CatchTBNErr(string url)
    {
        try
        {
            await tp.WaitForURLAsync(url, new PageWaitForURLOptions { Timeout = dfTimeout });
        } catch (TimeoutException ex)
        {
            Console.WriteLine(ex.Message);
            if (tp.Url.Contains("Error.aspx"))
            {
                Console.WriteLine($"TBN error encountered, manually going to '{url}'");
                await tp.GotoAsync(url);
            }
        }
    }
    public static async Task CatchStackTrace()
    {
        int stCnt = await tp.GetByText("StackTrace").CountAsync();
        if (stCnt > 0)
        {
            Console.WriteLine("StackTrace Error found! \nReload page!");
            await tp.ReloadAsync();
        }        
    }
    public static async Task<bool> ReloadIfBackdrop()
    {
        int bdCnt = await tp.Locator("div[class*='backdrop']").CountAsync();
        string loadStyle = await tp.Locator("//*[@id=\"loadingScreen\"]").GetAttributeAsync("style") ?? "";

        if (bdCnt > 0 && loadStyle.Contains("none")){
            Console.WriteLine("Backdrop exist after loading finished.\nReload " + tp.Url);
            await tp.ReloadAsync();
            await waitState("dom");
            return true;
        } else
        {
            Console.WriteLine("No brackdrop detected");
            return false;
        }
    }

    public static async Task LogIn(string username, string password)
    {
        await LogIn(CMParam.PORTAL_LOGIN, username, password);
    }
    public static async Task LogIn(string portal, string username, string password)
    {
        await tp.GotoAsync(portal);
        await waitState("load");
        await waitState("dom");
        await DelayS(1);
        Console.WriteLine("Fill credentials");
        await tp.Locator("//*[@id='signInUsername']").FillAsync(username);
        await DelayMS(200);
        await tp.Locator("//*[@id='SignIn_Password']").FillAsync(password);
        await DelayMS(500);
        await tp.Locator("#signInButtonId").ClickAsync();
        await CatchTBNErr(CMParam.PORTAL_MAIN);
        await CatchStackTrace();
    }

    public static async Task CustFilter(string custName, string custId)
    {
        ILocator panel = tp.Locator("//*[@id=\"searchformWrapper\"]");
        await panel.Locator("//*[@id=\"uiCustomerName\"]").FillAsync(custName);
        await DelayMS(200);
        await panel.Locator("//*[@id=\"uiCustomerId\"]").FillAsync(custId);
        await DelayMS(200);
        await panel.Locator("//*[@id=\"uiSearchCatalogs\"]").ClickAsync();
        await waitState("load");
        await waitState("dom");
        await ReloadIfBackdrop();

    }
}
