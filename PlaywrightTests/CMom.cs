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
using System.Diagnostics;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using ClosedXML.Excel;



namespace PlaywrightTests;

public abstract class CMom : CMParam
{
    protected IPlaywright pw;
    protected IBrowser browser;
    protected IBrowserContext context;
    protected IPage tp;

    [OneTimeSetUp]
    public void ReadRunnerSettings()
    {
        CMCoordinator.WaitForStage(0);
        environment = TestContext.Parameters.Get("Environment", "QA");
        browserName = TestContext.Parameters.Get("BrowserName", "chromium");
        headless = TestContext.Parameters.Get("Headless", false);
        channel = TestContext.Parameters.Get("Channel", "chrome");
        debugMode = TestContext.Parameters.Get("DebugMode", false);
        //Initialize CMParams
        InitParam(environment);
    }

    [SetUp]
    public async Task CMInit()
    {
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
        context = await browser.NewContextAsync(new() { ViewportSize = new ViewportSize() { Width = 1600, Height = 1000} });
        tp = await context.NewPageAsync();

        //return (pw, browser, context, tp);
        
    }

    [TearDown]
    public async Task TearDown()
    {
        await context.CloseAsync();
        await browser.CloseAsync();
        pw.Dispose();
    }

    public async Task<string> GetMonTime()
    {
        DateTime now = DateTime.Now.AddMinutes(-1); //Minus current time by 1 minute
        string val = now.ToString("dd/MM/yyyy (HH:mm)");
        return val;
    }

    public async Task<string> GetDLTime(string CMX)
    {
        DateTime now = DateTime.Now.AddMinutes(-1);
        if (CMX.ToLower() == "b")
        {
            return now.ToString("dd/MM/yyyy HH:mm:ss");
        }
        else if (CMX.ToLower() == "s")
        {
            return now.ToString("(dd/MM/yyyy HH:mm:ss)");
        }
        else
        {
            throw new Exception("Invalid CM designation (b/s)");
        }
    }
    public async Task DelayMS (int ms)
    {
        await tp.WaitForTimeoutAsync(ms);
    }
    public async Task DelayS (int s)
    {
        await tp.WaitForTimeoutAsync(s * 1000);
    }

    public async Task LoadDom()
    {
        await DelayMS(500);
        await WaitLoad("load");
        await DelayMS(500);
        await WaitLoad("dom");
    }
    public async Task WaitLoad(string state)
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
    public async Task CatchTBNErr(string url)
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
    public async Task CatchStackTrace()
    {
        int stCnt = await tp.GetByText("StackTrace").CountAsync();
        if (stCnt > 0)
        {
            Console.WriteLine("StackTrace Error found! \nReload page!");
            await tp.ReloadAsync();
        }        
    }
    public async Task<bool> ReloadIfBackdrop()
    {
        int bdCnt = await tp.Locator("div[class*='backdrop']").CountAsync();
        string loadStyle = await tp.Locator("//*[@id=\"loadingScreen\"]").GetAttributeAsync("style") ?? "";

        if (bdCnt > 0 && loadStyle.Contains("none")){
            Console.WriteLine("Backdrop exist after loading finished.\nReload " + tp.Url);
            await tp.ReloadAsync();
            await WaitLoad("dom");
            return true;
        } else
        {
            Console.WriteLine("No brackdrop detected");
            return false;
        }
    }

    public async Task LogIn(string username, string password)
    {
        await LogIn(CMParam.PORTAL_LOGIN, username, password);
    }
    public async Task LogIn(string portal, string username, string password)
    {
        await tp.GotoAsync(portal);
        await WaitLoad("load");
        await WaitLoad("dom");
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
    public async Task HomeDash(string co)
    {
        Console.WriteLine("Go to home page of CM");
        string URL = "";
        switch (co.ToLower())
        {
            case ("s"): 
                URL = CMS_CATALOG_HOME; 
                break;
            case ("b"):
                URL = CMB_CATALOG_HOME;
                break;
            default:
                throw new Exception("Invalid company type ( s/b )");
        }
        await tp.GotoAsync(URL, new() { Timeout = 60000 });
        await CatchStackTrace();
        await DelayS(2);
    }


    public async Task CustFilter(string custName, string custId)
    {
        ILocator panel = tp.Locator("//*[@id=\"searchformWrapper\"]");
        await panel.Locator("//*[@id=\"uiCustomerName\"]").FillAsync(custName);
        await DelayMS(200);
        await panel.Locator("//*[@id=\"uiCustomerId\"]").FillAsync(custId);
        await DelayMS(200);
        await panel.Locator("//*[@id=\"uiSearchCatalogs\"]").ClickAsync();
        await WaitLoad("load");
        await WaitLoad("dom");
        await ReloadIfBackdrop();

    }

    //Universal find the block id of a customer / supplier by name
    public async Task<string> FindCatalog(string name)
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
    public async Task<string> GetMetaId(string id)
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
    public async Task CMSUploadFile (string custName, string[] file, string[] type)
    {
        var blocId = await FindCatalog (custName);
        var bloc = tp.Locator($"id={blocId}");
        string eCatID = await GetMetaId(blocId);
        await bloc.GetByText("Show more", new() { Exact = true }).ClickAsync();
        await DelayS(2);
        await WaitLoad("dom");
        await bloc.GetByText("Upload Files", new() { Exact = true}).ClickAsync();
        await DelayS(2); 
        await WaitLoad("dom");
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
        await DelayS (1);
        //Process file
        await bloc.GetByText("Process Files").ClickAsync(); //Some doesn't have this onclick function?????
        await WaitLoad("load");
        await WaitLoad("dom");
        
    }
    public async Task MonProcesses(string url, CMProcess[] tProc)
    {
        await tp.GotoAsync(url); //Go to monitor page
        await CatchStackTrace();
        await WaitLoad("dom");
        await DelayS(5);//Delay further 5 sec for stability
        Console.WriteLine("Hope monitor page stable after 5 sec");
        int itemPerPage = 10;
        await tp.Locator("//*[@id='uiRecordCount']").ClickAsync();
        await DelayMS(500);
        await tp.RunAndWaitForResponseAsync(async () =>
        {
            Console.WriteLine($"Set item per page to {itemPerPage}");
            await tp.Locator("ul[role='menu']").Locator($"a[onclick='setPageCount({itemPerPage})']").ClickAsync();
        }, response => response.Url.Contains("GetItemCount") && response.Status == 200, new() { Timeout = 60000 });
        await WaitLoad("dom");
        await DelayS(5);//Delay further 5 sec for stability
        await ReloadIfBackdrop();
        await tp.Locator("//*[@id=\"ddlRefreshTime\"]").SelectOptionAsync("0"); //Set for manual testing
        Console.WriteLine("Set to manual refresh. Will trigger a page load");
        await WaitLoad("load"); //Wait until page done loading
        await WaitLoad("idle");
        await WaitLoad("dom");
        await DelayS(5);//Delay further 5 sec for stability
        await ReloadIfBackdrop();
        //For 1 minute, find the process by matching process, start time, supplier & customer
        int toMatch = tProc.Length;
        int tryCnt = 0;
        int matchCnt = 0;
        Boolean allMatch = false;
        //First read in all mon process
        CMProcess[] mainList = await ReadMainRow(itemPerPage);
        //Handle process to be reprocessed
        foreach (CMProcess process in tProc)
        {
            if (process.Pid != "") //IF any pid is empty then break loop and all match is false
            {
                allMatch = true;
            }
            else
            {
                allMatch = false;
                break;
            }
        }
        //To match processes from monitor page
        while (!allMatch && tryCnt < 3)
        {
            for (int i = 0; i < itemPerPage; i++) //For item list
            {
                for (int j = 0; j < tProc.Length; j++) //For process
                {
                    if (tProc[j].Pid == "") //Only check for process that has no match yet
                    {
                        if (tProc[j].PName == mainList[i].PName &&
                            IsLater(tProc[j].STime, mainList[i].STime) &&
                            tProc[j].Sup == mainList[i].Sup &&
                            tProc[j].Cust == mainList[i].Cust)
                        {
                            tProc[j].Pid = mainList[i].Pid;
                            matchCnt++;
                            Console.WriteLine($"Match process found! {tProc[j]}");
                            break;
                        }
                    }
                }
            }
            if (matchCnt == toMatch)
            {
                allMatch = true;
            }
            else
            {
                Console.WriteLine($"Not all process match found in trial {tryCnt + 1} / 3");
                if (tryCnt < 3)
                {
                    Console.WriteLine("Delay 20 sec then refresh page and try again");
                    await DelayS(20);
                    await tp.Locator("a[onclick='getProcessItemList(1)']").ClickAsync();
                    await WaitLoad("dom");
                    await ReloadIfBackdrop();
                    mainList = await ReadMainRow(itemPerPage);
                }
            }
        }
        //If not all process match after 1 min throw error
        if (!allMatch)
        {
            throw new Exception($"No complete match found monitor page for {tProc}");
        }
        else
        {
            foreach (var proc in tProc)
            {
                Console.WriteLine(proc);
            }
        }
        //Dynamically determine row number that has same pid
        int monDuration = 20; //Check process duration
        int timeLasped = 0;
        Boolean complete = false;
        
        while (!complete && timeLasped < monDuration * 3)
        {
            //Read in result
            int statMatchCnt = 0;
            CMProcess[] results = await ReadMainRow(itemPerPage);
            int[] matchRow = new int[tProc.Length];
            for (int i = 0; i < tProc.Length; i++)
            {
                for (int j = 0; j < results.Length; j++)
                {
                    if (tProc[i].Pid == results[j].Pid)
                    {
                        if (tProc[i].State == results[j].State)
                        {
                            statMatchCnt++;
                            break;
                        } else if ((tProc[i].State == "Finished OK" && results[j].State == "Failed") || (tProc[i].State == "Failed" && results[j].State == "Finished OK"))
                        {
                            throw new Exception($"{tProc[i]} finished in opposite state (Finished OK <-> Failed)");
                        }
                    }
                }
            }
            if (statMatchCnt == tProc.Length)
            {
                Console.WriteLine("All processes completed expectedly");
                complete = true;
            } else
            {
                Console.WriteLine("Process in progress, wait 20 secs and try again");
                await DelayS(15);
                await tp.Locator("a[onclick='getProcessItemList(1)']").ClickAsync();
                await DelayS(5);
                await ReloadIfBackdrop();
                timeLasped++;
            }
        }
        if (!complete)
        {
            Console.WriteLine($"Some process did not complete as required in {monDuration}min");
        }    
    }
    public async Task<CMProcess[]> ReadMainRow(int rows)
    {
        CMProcess[] procMainRow = new CMProcess[rows];
        ILocator mainRow = tp.Locator("//*[@id=\"itemListContainer\"]").Locator("tr[id^='mainRow-']");
        for (int i = 0; i < rows; i++)
        {
            procMainRow[i] = new CMProcess();
            procMainRow[i].Pid = await mainRow.Nth(i).Locator("td").Nth(0).InnerTextAsync();
            procMainRow[i].PName = await mainRow.Nth(i).Locator("td").Nth(1).InnerTextAsync();
            procMainRow[i].STime = await mainRow.Nth(i).Locator("td").Nth(2).InnerTextAsync();
            procMainRow[i].Sup = await mainRow.Nth(i).Locator("td").Nth(3).InnerTextAsync();
            procMainRow[i].Cust = await mainRow.Nth(i).Locator("td").Nth(4).InnerTextAsync();
            procMainRow[i].State = await mainRow.Nth(i).Locator("td").Nth(5).InnerTextAsync();
        }
        return procMainRow;
    }
    public static bool IsLater(string time1, string time2)
    {
        string t1 = time1.Replace("(", "").Replace(")", "");
        string t2 = time2.Replace("(", "").Replace(")", "");
        DateTime dt1 = DateTime.Parse(t1);
        DateTime dt2 = DateTime.Parse(t2);
        if (dt1 < dt2)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public async Task CMSDownload(string blocId, string linkName, string nameOFile, string dlTime)
    {
        var bloc = tp.Locator($"id={blocId}");
        string metaId = await GetMetaId(blocId);
        await tp.GotoAsync(CMS_CATALOG_HOME, new() { Timeout = 60000 });
        await CatchStackTrace();
        await bloc.GetByText("Show more", new() { Exact = true }).ClickAsync();
        await DelayS(5);
        await bloc.GetByText("Download Template", new() { Exact = true }).ClickAsync();
        await DelayS(5);
        ILocator dlList = tp.Locator($"//*[@id=\"{metaId}_DownloadFilesContent\"]");
        int listRows = await dlList.Locator("li").CountAsync();
        string[] dlItems = new string[listRows];
        string[] dlTimes = new string[listRows];
        //Read DL list + time
        for (int i = 0; i < listRows; i++)
        {
            dlItems[i] = await dlList.Locator("li").Nth(i).InnerTextAsync();
            Console.WriteLine(dlItems[i]);
            //Strip the time for the whole string
            dlTimes[i] = Regex.Match(dlItems[i], @"\([^)]+\)").Value;
            if (dlItems[i].Contains(linkName) && IsLater(dlTime, dlTimes[i]))
            {
                var waitForDownload = tp.WaitForDownloadAsync();
                await dlList.GetByText(linkName).Nth(i).ClickAsync(); //*[@id="63045_DownloadFilesContent"]/li[1]/a
                var download = await waitForDownload;
                var fileName = DL_PATH + nameOFile;//"TC268233_CMS_CATALOG_DOWNLOAD.zip";
                Console.WriteLine("Filed download as " + fileName);
                await download.SaveAsAsync(fileName);
                break;
            }
        }
    }
    /// <summary>
    /// Updates the value of a specific cell in a given worksheet of an XLSX file.
    /// </summary>
    /// <param name="file">Name of the xlsx file</param>
    /// <param name="sheetName">Name of the worksheet</param>
    /// <param name="cellAddress">Cell address (e.g., "B2")</param>
    /// <param name="newValue">New value to set</param>
    public static void UpdateExcel(string file, string sheet, string cell, string newValue)
    {
        try
        {
            using (var workbook = new XLWorkbook(FILE_PATH + file))
            {
                var worksheet = workbook.Worksheet(sheet);
                if (worksheet == null)
                {
                    throw new ArgumentException($"Worksheet {sheet} not found");
                }
                worksheet.Cell(cell).Value = newValue;
                workbook.Save();
            }
            Console.WriteLine($"Cell {cell} in {sheet} is updated to {newValue}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating cell: {ex.Message}");
        }

    }
    public async Task FilterSup(string supplierName)
    {
        //Filter spplier by name
        await tp.Locator("//*[@id=\"uiSupplierName\"]").FillAsync(supplierName);
        await tp.Locator("//*[@id=\"uiSearchCatalogs\"]").ClickAsync();
        await LoadDom();
        await DelayS(5);
    }

}
