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
using FluentAssertions.Extensions;
using NUnit.Framework.Constraints;
using Microsoft.ApplicationInsights;



namespace PlaywrightTests;

public abstract class CMom : CMParam
{
    protected IPlaywright pw;
    protected IBrowser browser;
    protected IBrowserContext context;
    protected IPage tp;

    [SetUp]
    public async Task CMInit()
    {
        WaitInit();
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
        context = await browser.NewContextAsync(new() { ViewportSize = new ViewportSize() { Width = 1600, Height = 1000}});
        context.SetDefaultTimeout(dfTimeout);
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
        DateTime now = DateTime.Now.AddMinutes(-2); //Minus current time by 1 minute
        string val = now.ToString("dd/MM/yyyy (HH:mm)");
        Console.WriteLine($"Test start time {val}");
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

    public async Task LoNetDom()
    {
        await LoNetDom(0);
    }
    public async Task LoNetDom(int s)
    {
        await DelayMS(200);
        await WaitLoad("load");
        await DelayMS(200);
        await WaitLoad("idle");
        await DelayMS(200);
        await WaitLoad("dom");
        if (s > 0)
        {
            await DelayS(s);
        }
    }
    public async Task WaitLoad(string state)
    {
        switch (state.ToLower())
        {
            case "load":
                await tp.WaitForLoadStateAsync(LoadState.Load);
                break;
            case "idle":
                await tp.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions() { Timeout = dfTimeout});
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
        await WaitLoad("dom");
        int stCnt = await tp.GetByText("StackTrace").CountAsync();
        if (stCnt > 0)
        {
            Console.WriteLine("StackTrace Error found! \nReload page!");
            await tp.ReloadAsync();
            await LoNetDom(5);
        }        
    }
    public async Task<bool> ReloadIfBackdrop()
    {
        await WaitLoad("dom");
        int bdCnt = await tp.Locator("div[class*='backdrop']").CountAsync();
        string loadStyle = await tp.Locator("//*[@id=\"loadingScreen\"]").GetAttributeAsync("style") ?? "";

        if (bdCnt > 0 && loadStyle.Contains("none")){
            Console.WriteLine("Backdrop exist after loading finished.\nReload " + tp.Url);
            await tp.ReloadAsync();
            await LoNetDom(5);
            return true;
        } else
        {
            Console.WriteLine("No brackdrop detected");
            return false;
        }
    }

    public async Task LogIn(string username, string password)
    {
        await LogIn(PORTAL_LOGIN, username, password, "N");
    }
    public async Task NuLogin(string usernam, string password)
    {
        await LogIn(PORTAL_LOGIN, usernam, password, "Y");
    }

    public async Task LogIn(string portal, string username, string password, string newUser)
    {
        string callFrom;
        switch (ENVIRONMENT.ToLower())
        {
            case "qa":
                callFrom = "portal.qa.hubwoo.com"; break;
            case "prod":
                callFrom = "portal.hubwoo.com"; break;
            default:
                throw new ArgumentException("Check environment parameter QA / PROD only");
        }
        string newUserProfile = PORTAL_URL + $"/main/Admin/MyProfile?takeTo=https:%2F%2F{callFrom}%2Fmain%2F";
        //https://portal.qa.hubwoo.com/main/Admin/MyProfile?takeTo=https:%2F%2Fportal.qa.hubwoo.com%2Fmain%2F
                   //https://portal.hubwoo.com/main/Admin/MyProfile?takeTo=https:%2F%2Fportal.hubwoo.com%2Fmain%2F
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
        switch (newUser) {
            case "N":
                await CatchTBNErr(PORTAL_MAIN);
                break;
            case "Y":
                await CatchTBNErr(newUserProfile); break;
            default:
                throw new ArgumentException($"Check new user argument! (Y/N) but have {newUser}");
        }
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
        try
        {
            await tp.GotoAsync(URL, new() { Timeout = dfTimeout });
        }
        catch (TimeoutException te)
        {
            Console.WriteLine($"Failed to open {URL} for 1 time, will try 1 more time");
            await tp.GotoAsync(URL, new() { Timeout = dfTimeout });
        }
        await LoNetDom(5);
        await WaitSpinOff(10);
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
        await LoNetDom(5);
        try
        {
            await ReloadIfBackdrop();
            await CatchStackTrace();
        }
        catch (TimeoutException te)
        {
            Console.WriteLine("Something sort of error during catch backdrop / stacktrace");
        }
        await DelayS(5);//Delay further 5 sec for stability
        Console.WriteLine("Hope monitor page stable after 5 sec");
        int itemPerPage = 20;
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
        await LoNetDom(5);
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
                tryCnt++;
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
    public static bool IsLater(string earlyTime, string lateTime)
    {
        string t1 = earlyTime.Replace("(", "").Replace(")", "");
        string t2 = lateTime.Replace("(", "").Replace(")", "");
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
            var match = Regex.Match(dlItems[i], @"\(([^)]+)\)");
            string matchTime = "";
            if (match.Success)
            {
                matchTime = match.Groups[1].Value;
            }
            Console.WriteLine(dlItems[i]);
            //Strip the time for the whole string
            if (dlItems[i].Contains(linkName) && IsLater(dlTime, matchTime))
            {
                var waitForDownload = tp.WaitForDownloadAsync();
                await dlList.GetByText(linkName).Nth(i).ClickAsync(); //*[@id="63045_DownloadFilesContent"]/li[1]/a
                var download = await waitForDownload;
                var fileName = DL_PATH + nameOFile;//"TC268233_CMS_CATALOG_DOWNLOAD.zip";
                Console.WriteLine("File is downloaded to " + fileName);
                await download.SaveAsAsync(fileName);
                break;
            }
        }
    }
    public async Task CMBDownload(string refTime, string template, string fileName)
    {
        await tp.GotoAsync(CMB_CATALOG_DL);
        await LoNetDom();
        await DelayS(5);
        await CatchStackTrace();
        var dlList = tp.Locator("//*[@id=\"itemListContainer\"]");
        //*[@id="itemListContainer"]/tr[1]/td[1]
        //find the diffing report that meets test start time
        int rowCnt = await dlList.Locator("tr").CountAsync();
        bool match = false;
        for (int i = 0; i < rowCnt; i++)
        {
            string r1DLTime = await dlList.Locator("tr").Nth(i).Locator("td").Nth(0).InnerTextAsync();
            string r1Template = await dlList.Locator("tr").Nth(i).Locator("td").Nth(1).InnerTextAsync();
            bool timecheck = IsLater(refTime, r1DLTime);
            if (timecheck && r1Template == template)
            {
                match = true;
                Console.WriteLine($"Matching {template} found, download it now");
                var waitForDL = tp.WaitForDownloadAsync();
                await dlList.Locator("tr").Nth(i).Locator("td").Last.Locator("a").ClickAsync();
                var dl = await waitForDL;
                var saveTo = DL_PATH + fileName;
                Console.WriteLine("File is downloaded to " + saveTo);
                await dl.SaveAsAsync(saveTo);
                break;
            }
        }
        Assert.That(match, $"Unable to find target {template} to download");
    }
    public async Task CMBRejectCatalog(string supplier)
    {
        var blocId = await FindCatalog(supplier);
        var metaId = await GetMetaId(blocId);
        var blocLoc = tp.Locator($"id={blocId}");//CSS selector
        if (await blocLoc.GetByText("New version available").CountAsync() > 0)
        {
            Console.WriteLine("Catalog in status new version available, need reject catalog");
            await blocLoc.GetByText("Show more").ClickAsync();
            await LoNetDom(5);
            await OldCusErrHandle(metaId);
            await tp.Locator($"[id=\"{metaId}_allTasks_tabSupplierCatalog\"]").GetByText("Reject Catalog").ClickAsync();
            await DelayS(5);
            //*[@id="237593_allTasks_tabSupplierCatalog"]/div[2]/div/div[2]/a[2]
            string rejPopupClass = await tp.Locator("//*[@id='uiRejectComment']").GetAttributeAsync("class");
            Assert.That(rejPopupClass.Contains("modal fade in"));
            await tp.Locator("//*[@id='uiRejectCommentText']").FillAsync("Reject for Enrichment Test");
            await tp.Locator("//*[@id='uiUpdateRejectCatalog']").ClickAsync();//Fire catalog rejection
            await tp.WaitForLoadStateAsync(LoadState.Load);
            await tp.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await tp.WaitForTimeoutAsync(2000);
            await tp.Locator("//*[@id=\"uiCatalogRejectedMessage\"]/div/center/a").ClickAsync(); //This close popup but noticed the popup is not properly closed during automation
            await tp.WaitForTimeoutAsync(5000);
            var isPopupClosed = await tp.Locator("//*[@id='uiRejectComment']").IsHiddenAsync(); //Manually close the remaining popup after 5 second
            if (!isPopupClosed)
            {
                await tp.Locator("//*[@id=\"uiRejectComment\"]/div/div/div[1]/button").ClickAsync();
                await tp.WaitForTimeoutAsync(2000);
            }
        }
        else 
        {
            Console.WriteLine("No catalog to reject!");
        }
        await HomeDash("b");
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
        await DelayMS(200);
        await tp.Locator("//*[@id=\"uiSearchCatalogs\"]").ClickAsync();
        await LoNetDom(1);
        await WaitSpinOff(30);
    }
    public static void WaitInit()
    {
        while (!INITDONE)
        {
            Console.WriteLine("Parameters not yet initialized, wait for 10 secs!");
            Thread.Sleep(10000);
        }
    }
    public async Task WaitTCDone(string file)
    {
        WaitInit();
        if (!debugMode)
        {
            Thread.Sleep(10000);
            //This force test case to wait until the target file exist        
            int waited = 0;
            while (!File.Exists(file) && waited < 60)
            {
                await Task.Delay(20000);
                waited++;
            }
            if (!File.Exists(file))
            {
                throw new Exception($"{file} is not completed within 20 min");
            }
        }
        
    }
    public async Task LogOut()
    {
        //Do not use network idle on signin page
        Console.WriteLine("Log off from CM");
        //await ReloadPageIfBackrop();
        await tp.Locator("top-bar-user-section[name='User']").ClickAsync();
        await tp.WaitForTimeoutAsync(500);
        await tp.Locator("top-bar-item[name='Log Off']").ClickAsync();
        await LoNetDom();
        await DelayS(5);
    }
    public async Task OldCusErrHandle(string metaId)
    {
        var navWiz = tp.Locator($"//*[@id=\"{metaId}_allTasks_navWizard\"]");
        Console.WriteLine("Create working version");
        string? isActive = await navWiz.Locator("li").Nth(1).GetAttributeAsync("class");
        if (isActive == null || !isActive.Equals("active"))
        {
            //This catch previous catalog has customer side error
            try
            {
                await navWiz.Locator("li").Nth(1).ClickAsync();
                await LoNetDom(5);
                isActive = await navWiz.Locator("li").Nth(1).GetAttributeAsync("class"); //Get attribute again!
            }
            catch (TimeoutException te)
            {
                Console.WriteLine("Supplier Catalog chevron is not active for some reason! " + te.Message);
                isActive = await navWiz.Locator("li").Nth(1).GetAttributeAsync("class"); //Get attribute again!
            }
        }
        Assert.That(isActive, Does.Contain("active"), "Supplier catalog chevron expect active but not!");
    }

    public async Task WaitSpinOff(int timeout)
    {
        int to = timeout * 1000;
        await tp.Locator("#loadingScreen").WaitForAsync(new() { State = WaitForSelectorState.Hidden , Timeout = to});

    }
}
