using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

using static Microsoft.Playwright.Assertions;



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

    //Universal find the block id of a customer / supplier by name
    public static async Task<string> FindCatalog(string name)
    {
        string path = $"//strong[normalize-space(text())='{name}']/ancestor::div[@id][1]";
        var target = tp.Locator($"xpath={path}");
        var tarParent = await target.ElementHandleAsync();
        if (tarParent is null)
        {
            return "";
        }
        var id = await tarParent.GetAttributeAsync("id");
        Console.WriteLine($"Block id {id}" );
        return id;
    }
    public static async Task<string> GetMetaId(string id)
    {
        Match match = Regex.Match(id, @"\d+");
        Console.WriteLine($"MetaCat Id: {match}");
        if (match.Success)
        {
            return match.Value;
        } else 
        {
            throw new Exception($"Cannot find MetacatId from {match.Value}");
        }
    }
    public static async Task CMSUploadFile (string custName, string[] file, string[] type)
    {
        var blocId = await FindCatalog (custName);
        var bloc = tp.Locator($"id={blocId}");
        string eCatID = await GetMetaId(blocId);
        await bloc.GetByText("Show more", new() { Exact = true}).ClickAsync();
        await waitState("dom");
        await DelayS(1);
        await bloc.GetByText("Upload Files", new() { Exact = true}).ClickAsync();
        await waitState("dom");
        await DelayS(1);
        //Reprocess files to contains path
        string[] fileWPath = new string[file.Length];
        for (int i = 0; i < file.Length; i++)
        {
            fileWPath[i] = FILE_PATH + file[i];
        }
        await tp.Locator($"[id='{eCatID}_fileSelect']").SetInputFilesAsync(fileWPath);
        //Replace filename extension to zip and set file types
        for (int i = 0; i < file.Length; i++)
        {
            string disFile = Regex.Replace(file[i], @"\.[^.]+$", ".zip");
            await Expect(tp.Locator($"[id='{eCatID}_uploadFileList']")).ToContainTextAsync(disFile); //*[@id="63080_uploadFileList"]
            await tp.Locator($"[id='{eCatID}_{disFile}_selectType']").SelectOptionAsync(type[i]);
        }
        //Process file
        await tp.Locator($"[onclick=\"processUploadedFiles('{eCatID}')\"]").ClickAsync();
        await waitState("load");
        await waitState("dom");
        
    }
}
