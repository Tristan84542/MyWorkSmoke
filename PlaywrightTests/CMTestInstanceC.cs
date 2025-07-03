using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2021.Excel.RichDataWebImage;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using FluentAssertions;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;



namespace PlaywrightTests;

[Parallelizable(ParallelScope.Fixtures)]
[TestFixture]

internal class CMTestInstanceC : CMom
{
    private static bool crsPassed = false;
    private static bool TC274456Passed = false;
    private static string testDateTime;

    private string intCatSup;
    private string custName;
    private string userName = "";
    private string password = "";
    private string viewURL;
    private string fileName = "Catalog_scf_IntCatalog.xlsx";
    

    [OneTimeSetUp]
    public void InstanceCOTS()
    {
        DateTime now = DateTime.Now;
        testDateTime = now.ToString("yyyymmddHHmm");

        if (!debugMode)
        {
            CMCoordinator.WaitForStage(3);
            const string path = "TC268234_Passed.flag";
            int waited = 0;
            while (!File.Exists(path) && waited < 20)
            {
                Thread.Sleep(60000);
                waited++;
            }
            if (!File.Exists(path))
            {
                throw new Exception("TC268234 is not completed within 20 min");
            }
            CMCoordinator.StageDone();
        }

        if (environment == "QA")
        {
            intCatSup = "LenaSupplier1";
            custName = "SV Buyer";
            userName = "SVB-0001ba";
            password = "Xsw23edc!";
            viewURL = "https://search.qa.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=SVVIEW1&VIEW_PASSWD=q0E2Aft3PQy18&USER_ID=SV&BRANDING=search5&LANGUAGE=EN&HOOK_URL=https://portal.qa.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp&ADMIN=1&COUNTRY=GB&EASYORDER=1";
            UpdateExcel(fileName, "Data 1", "F3", $"Smoke Internal Catalog 001 {testDateTime}");

        }
        else if (environment == "PROD")
        {
            intCatSup = "TESTSUPCDO9";
            custName = "TESTCUSTCDO 1";
            userName = "EPAM_TC-0001";
            password = "xsw23edc";
            viewURL = "https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE05-05&VIEW_PASSWD=t3S4TcKp89Rqy&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp";
            UpdateExcel(fileName, "Data 1", "C3", $"Smoke Internal Catalog 001 {testDateTime}");
        }
        else
        {
            throw new Exception("Check runner Environment value");
        }
    }

    [Test, Order(1)]
    [Category("CMS Test")]
    public async Task TC268237_CMS_CATALOG_ITEM_N_REPORT()
    {
        string startTime = await GetMonTime();
        string dlTime = await GetDLTime("s");
        await LogIn(CMS_USRA, CMS_PWDA);
        await tp.GotoAsync(CMS_CATALOG_HOME, new() { Timeout = 60000 });
        await CatchStackTrace();
        var blocId = await FindCatalog(CMS_C_CUSTNAME);
        var blocLoc = tp.Locator($"id={blocId}");
        string metaId = await GetMetaId(blocId);
        await blocLoc.GetByText("Show Items").ClickAsync();
        await LoadDom();
        await CatchStackTrace();
        string url = tp.Url;
        Assert.That(url, Does.Contain("CatalogManager/supplier/item-list"), $"Expect to be in item list but landed to {url}");
        await DelayS(5);
        await tp.Locator("//*[@id=\"ddlCatalogVersion\"]").SelectOptionAsync("CUS_RELEASED");
        await LoadDom();
        await DelayS(5);
        await tp.Locator("//*[@id=\"uiDownloadReport\"]").ClickAsync();
        CMProcess[] catalogDL =
            [
                new CMProcess("", "Catalog Download Job", startTime, CMS_C_SUP_NAME, CMS_C_CUSTNAME, "Finished OK")
            ];
        await MonProcesses(CMS_CATALOG_MONITOR, catalogDL);
        await CMSDownload(blocId, "Catalog Download Job", "TC268237_CMS_CATALOG_ITEM_N_REPORT.zip", dlTime);
    }

    [Test, Order(2)]
    [Category("CMS Test")]
    public async Task TC268233_CMS_CATALOG_DOWNLOAD()
    {
        string startTime = await GetMonTime();
        string dlTime = await GetDLTime("s");
        await LogIn(CMS_USRA, CMS_PWDA);
        await tp.GotoAsync(CMS_CATALOG_HOME, new() { Timeout = 60000});
        await CatchStackTrace();
        var blocId = await FindCatalog(CMS_C_CUSTNAME);
        var bloc = tp.Locator($"id={blocId}");
        string metaId = await GetMetaId(blocId);
        await bloc.GetByText("Show more", new() {  Exact = true}).ClickAsync();
        await DelayS(5);
        await bloc.GetByText("Download Template", new() {  Exact = true }).ClickAsync();
        await DelayS(5);
        await tp.Locator($"//*[@id='{metaId}_ddlLanguage']").SelectOptionAsync("en");
        await tp.Locator($"//*[@id=\"{metaId}_ddlExportType\"]").SelectOptionAsync("EXCEL_2007");
        await tp.Locator($"//*[@id=\"{metaId}_ddlVersion\"]").SelectOptionAsync(new SelectOptionValue { Index = 2 }); // This should set to last submitted version
        await bloc.GetByText("Create Template").ClickAsync();
        await DelayS(5);
        CMProcess[] catExport =
            [
                new CMProcess("", "Template Export", startTime, CMS_C_SUP_NAME, CMS_C_CUSTNAME, "Finished OK")
            ];
        await MonProcesses(CMS_CATALOG_MONITOR, catExport);
        await CMSDownload(blocId, "SCF EXPORT",  "TC268233_CMS_CATALOG_DOWNLOAD.zip", dlTime);

    }

    [Test, Order(3)]
    [Category("CMS Test")]
    public async Task TC268236_CMS_CHECKROUTINE()
    {
        crsPassed = false;
        string startTime = await GetMonTime();
        await LogIn(CMS_USRC, CMS_PWDC);
        await tp.GotoAsync(CMS_CATALOG_HOME, new() { Timeout = 60000 });
        await CatchStackTrace();
        string[] file = [CRS_FILE];
        string[] type = ["content"];
        var blocId = await FindCatalog(CMS_C_CUSTNAME);
        string metaId = await GetMetaId(blocId);
        var bloc = tp.Locator($"id={blocId}");
        await CMSUploadFile(CMS_C_CUSTNAME, file, type);
        CMProcess[] crsImport =
            [
                new CMProcess ("", "Simple Catalog import", startTime, CMS_C_SUP_NAME, CMS_C_CUSTNAME, "Failed")
            ];
        await MonProcesses(CMS_CATALOG_MONITOR, crsImport);
        //Determind which row has the correct pid
        CMProcess[] procList = await ReadMainRow(10);
        int matchRow = 0;
        for (int i = 0; i < 10; i++)
        {
            if (procList[i].Pid == crsImport[0].Pid)
            {
                matchRow = i;
                break;
            }
        }
        //Click the Error correction link and open error correction chevron
        try
        {
            await tp.Locator("//*[@id=\"itemListContainer\"]").Locator("tr[id^='detail-']").Nth(matchRow).GetByText("Error Correction").ClickAsync();
            await CatchStackTrace();
        }
        catch (TimeoutException te)
        {
            Console.WriteLine($"Failed to open error correction chevron, do it manually! {te}");
            await tp.GotoAsync(CMS_CATALOG_HOME, new() { Timeout = 60000 });
            await CatchStackTrace();
            await bloc.GetByText("Show more", new() { Exact = true }).ClickAsync();
            await DelayS(5);
            await bloc.Locator("//a[@data-toggle='tab' and contains(normalize-space(text()), 'Error Correction')]").ClickAsync();
        }
        await DelayS(5);
        //Open Item view
        await tp.Locator($"//*[@id=\"{metaId}_ErrorReportItemsContent\"]").Locator("a[onclick^='showItemViewWithLoading']").ClickAsync();
        await LoadDom();
        await DelayS(2);
        int iVRows = await tp.Locator($"//*[@id=\"{metaId}_itemViewDetails\"]").Locator("tr").CountAsync();
        //Update correction value
        for (int j = 0; j < iVRows; j++)
        {
            string corValue = await tp.Locator($"//*[@id=\"{metaId}_itemViewDetails\"]").Locator("tr").Nth(j).Locator("td").Nth(1).InnerTextAsync() + " " + testDate;
            //*[@id="63045_itemViewDetails"]/tr[1]/td[5]/input[1]
            await tp.Locator($"//*[@id=\"{metaId}_itemViewDetails\"]").Locator("tr").Nth(j).Locator("td").Nth(4).Locator("input").Nth(0).FillAsync(corValue);
        }
        await DelayS(5);
        await tp.Locator($"//*[@id=\"{metaId}_saveAllItemViewDetails\"]").ClickAsync();
        await LoadDom();
        await DelayS(5);
        //Revalidate catalog
        if (await ReloadIfBackdrop())
        {
            await bloc.GetByText("Show more", new() { Exact = true }).ClickAsync();
            await DelayS(5);
            await bloc.Locator("//a[@data-toggle='tab' and contains(normalize-space(text()), 'Error Correction')]").ClickAsync();
        }
        await tp.Locator($"//*[@id=\"{metaId}_btnRevalidate\"]").ClickAsync();
        await WaitLoad("load");
        await WaitLoad("dom");
        crsImport[0].State = "Finished OK";
        await MonProcesses(CMS_CATALOG_MONITOR, crsImport);
        crsPassed = true;
    }

    [Test, Order(4)]
    [Category ("CMS Test")]
    public async Task TC268235_CMS_DIFFINGREPORT()
    {
        Assume.That(crsPassed, "SKip test because TC268236_CMS_CHECKROUTINE not pass");
        string startTime = await GetMonTime();
        string dlTime = await GetDLTime("s");
        await LogIn(CMS_USRC, CMS_PWDC);
        await HomeDash("s");
        var blocId = await FindCatalog(CMS_C_CUSTNAME); //*[@id="(63045)_catalog"]/div/div[1]/a
        var bloc = tp.Locator($"id={blocId}");
        var metaId = await GetMetaId(blocId);
        var cogX = $"//*[@id=\"({metaId})_catalog\"]/div/div[1]";
        await tp.Locator(cogX).ClickAsync();
        await DelayS(2);
        await bloc.Locator("div[class='settings open']").GetByText("Diffing Report").ClickAsync();
        await LoadDom();
        await CatchStackTrace();
        await DelayS(2);
        Assert.That(tp.Url, Does.Contain("CatalogManager/diffing/diffing-supplier"));
        var diffTable = tp.Locator("//*[@id=\"bodyContent\"]");
        var mainRow = diffTable.Locator("tr[id^='mainRow']");
        //Define reference mainrow value
        string[,] refMain = new string[,]
        {
            {"11-015.5000",  $"11-015.5000 {testDate}", "Changed", "1"},
            {"11-015.9025", $"11-015.9025 {testDate}", "Changed", "1" }
        };
        //Read in actual main row then compare
        //Make sure main row equal to reference value
        int rowCnt = await mainRow.CountAsync();
        Assert.That(rowCnt, Is.EqualTo(2), $"Expect to have 2 result but get {rowCnt}");
        int matchRow = 0;
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < rowCnt; j++)
            {
                string itemId = await mainRow.Nth(j).Locator("td").Nth(0).InnerTextAsync();
                string shortDesc = await mainRow.Nth(j).Locator("td").Nth(1).InnerTextAsync();
                string state = await mainRow.Nth(j).Locator("td").Nth(2).InnerTextAsync();
                string fields = await mainRow.Nth(j).Locator("td").Nth(3).InnerTextAsync();
                if (refMain[i, 0] == itemId &&
                    refMain[i, 1] == shortDesc &&
                    refMain[i, 2] == state &&
                    refMain[i, 3] == fields
                    ) 
                { 
                    matchRow++;
                    break;
                }
            }
        }
        Assert.That(matchRow, Is.EqualTo(2), $"Expect to have 2 match but get {matchRow} match");
        //Download csv diffing report
        await tp.Locator("//*[@id=\"uiDiffingReportType\"]").SelectOptionAsync("CSV");
        await DelayMS(500);
        var waitForDownload = tp.WaitForDownloadAsync();
        await tp.GetByText("Download Report").ClickAsync();
        var download = await waitForDownload;
        var fileName = DL_PATH + $"TC268235_CMS_DIFFINGREPORT.csv";
        Console.WriteLine("Filed download as " + fileName);
        await download.SaveAsAsync(fileName);
        //Download xlsx diffing report
        await DelayS(5);
        await tp.Locator("//*[@id=\"uiDiffingReportType\"]").SelectOptionAsync("XLSX");
        await tp.GetByText("Download Report").ClickAsync();
        await DelayS(2);
        await LoadDom();
        await DelayS(3);
        Assert.That(tp.Url == CMS_CATALOG_MONITOR);
        await CatchStackTrace();
        CMProcess[] xlsxDiff =
            [
                new CMProcess("", "Template Export", startTime, CMS_C_SUP_NAME, CMS_C_CUSTNAME, "Finished OK")
            ];
        await MonProcesses(CMS_CATALOG_MONITOR, xlsxDiff);
        await CMSDownload(blocId, "Diffing Report", "TC268235_CMS_DIFFINGREPORT.zip", dlTime);
    }

    [Test, Order(5)]
    [Category ("CMB Test")]
    public async Task TC274456_CMB_IMPORT_RELEASE_CATALOG()
    {
        

        string startTime = await GetMonTime();
        await LogIn(userName, password);
        await HomeDash("b");
        var blocId = await FindCatalog(intCatSup);
        var metaId = await GetMetaId(blocId);
        var blocLoc = tp.Locator($"id={blocId}");//CSS selector
        //Upload file
        await tp.Locator("//*[@id=\"btnShowUploadModal\"]").ClickAsync();
        var uploadPop = tp.Locator("//*[@id=\"uiUploadModul\"]");
        Assert.That(await uploadPop.IsVisibleAsync());
        await DelayS(2);
        Console.WriteLine("To upload catalog file");
        await tp.Locator($"[id=\"uiFileSelect\"]").SetInputFilesAsync(
            new[] { FILE_PATH + fileName });
        await DelayMS(500);
        await tp.Locator($"//*[@id=\"{fileName}_selectType\"]").SelectOptionAsync("content");
        await DelayMS(500);
        await uploadPop.GetByText("Process Files").ClickAsync();
        await DelayMS(500);
        await uploadPop.Locator("button").Nth(0).ClickAsync();
        await DelayS(5);
        CMProcess[] catImport =
            [
                new CMProcess("", "Simple Catalog import", startTime, intCatSup, custName, "Finished OK")
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, catImport);
        CMProcess[] releaseCatalog =
            [
                new CMProcess("", "Release catalog", startTime, intCatSup, custName, "Finished OK")
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, releaseCatalog);
        await HomeDash("b");
        await tp.Locator("//*[@id=\"uiSupplierName\"]").FillAsync(intCatSup);
        await tp.Locator("//*[@id=\"uiSearchCatalogs\"]").ClickAsync();
        await LoadDom();
        await DelayS(5);
        //Click show more
        await blocLoc.GetByText("Show more").ClickAsync();
        await LoadDom();
        var navWiz = tp.Locator($"//*[@id=\"{metaId}_allTasks_navWizard\"]");
        Console.WriteLine("Create working version");
        string? isActive = await navWiz.Locator("li").Nth(1).GetAttributeAsync("class");
        Assert.That(isActive, Does.Contain("active"), "Supplier catalog chevron expect active but not!");
        //Create working version
        var supCat = tp.Locator($"//*[@id=\"{metaId}_allTasks_tabSupplierCatalog\"]");
        await supCat.GetByText("Create Working Version").ClickAsync();
        await LoadDom();
        await ReloadIfBackdrop();
        CMProcess[] loadCat =
            [
                new CMProcess("", "Load Catalog", startTime, intCatSup, custName, "Finished OK"),
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, loadCat);
        await HomeDash("b");
        var statusVal = await tp.Locator($"//*[@id=\"{blocId}\"]/div/div[3]/div[2]/div").InnerTextAsync();//*[@id="237593_allTasks_catalog"]/div/div[3]/div[2]/div
        Assert.That(statusVal, Does.Contain("Catalog to approve"));
        await blocLoc.GetByText("Show more", new() { Exact = true }).ClickAsync();
        await LoadDom();
        isActive = await navWiz.Locator("li").Nth(2).GetAttributeAsync("class");
        Assert.That(isActive, Does.Contain("active"), "Approve items chevron expect active but not!");
        //*[@id="237593_allTasks_tabApproveItems"]/div[2]/div/div[2]/a[1]
        var appItems = tp.Locator($"//*[@id=\"{metaId}_allTasks_tabApproveItems\"]");
        //Approve
        await appItems.GetByText("Review Items").ClickAsync();
        await LoadDom();
        await CatchStackTrace();
        Assert.That(tp.Url, Does.Contain("/srvs/BuyerCatalogs/items/item-list"), "Expect to be in item review page but not!");
        await ReloadIfBackdrop();
        await DelayS(5);
        await tp.Locator("//*[@id=\"uiTableAction\"]").SelectOptionAsync("approve_all");
        await tp.Locator("//*[@id=\"uiInternalComment\"]").FillAsync($"TC268238_CMB_Release_External_Catalog on {testDate}");
        await DelayS(2);
        await tp.Locator("//*[@id=\"uiSubmitAction\"]").ClickAsync();
        await LoadDom();
        await DelayS(5);
        await ReloadIfBackdrop();
        await tp.Locator("//*[@id=\"uiGoToReleaseTab\"]").ClickAsync();
        await LoadDom();
        Console.WriteLine("Ready to release directly");
        //Return to dashboard with release catalog chevron ready
        Assert.That(tp.Url, Does.Contain(CMB_CATALOG_HOME), "Expect to be back to dashboard but not!");
        isActive = await navWiz.Locator("li").Nth(3).GetAttributeAsync("class");
        Assert.That(isActive, Does.Contain("active"), "Release Catalog chevron expect active but not!");
        //*[@id="237593_allTasks_tabReleaseCatalog"]/div[2]/div/div[1]/a[1]
        var relCat = tp.Locator($"//*[@id=\"{metaId}_allTasks_tabReleaseCatalog\"]");
        //Set live!!!
        await relCat.GetByText("Direct Release").ClickAsync();
        Console.WriteLine("Direct release catalog now!");
        await tp.Locator("//*[@id=\"uiDirectRelease\"]").GetByText("OK", new() { Exact = true }).ClickAsync();
        await LoadDom();
        CMProcess[] setLive =
            [
                new CMProcess("", "Set Live", startTime, intCatSup, custName, "Finished OK"),
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, setLive);

        //Access search now
        Console.WriteLine("Wait 1 min before check on search");
        await DelayS(60);
        await tp.GotoAsync(viewURL);
        await LoadDom();
        await DelayS(5);
        await tp.Locator("//*[@id=\"termAuto\"]").FillAsync(testDateTime);
        await DelayMS(500);
        await tp.Locator("//*[@id=\"termAuto\"]").PressAsync("Enter");
        await LoadDom();
        await DelayS(2);
        string result = await tp.Locator("//*[@id=\"itemList\"]/tbody/tr/td[3]/div/a").InnerTextAsync();
        Assert.That(result.Equals($"Smoke Internal Catalog 001 {testDateTime}"), $"Item is not expected! {result}");
        Console.WriteLine("Test passed");
        TC274456Passed = true;
    }

    [Test, Order(6)]
    [Category ("CMB Test")]
    public async Task TC274460_CMB_ARCHIVE_RESTORE()
    {
        Assert.That(TC274456Passed == true, "TC274456_CMB_IMPORT_RELEASE_CATALOG failed, skip testing to avoid run out of available catalog");
        string startTime = await GetMonTime();
        await LogIn(userName, password);
        await HomeDash("b");
        await tp.Locator("//*[@id=\"uiSupplierName\"]").FillAsync(intCatSup);
        await tp.Locator("//*[@id=\"uiSearchCatalogs\"]").ClickAsync();
        await LoadDom();
        await DelayS(5);
        var blocId = await FindCatalog(intCatSup);
        var metaId = await GetMetaId(blocId);
        var blocLoc = tp.Locator($"id={blocId}");//CSS selector
        await blocLoc.Locator("div[class='settings']").ClickAsync();
        await blocLoc.Locator("div[class*='settings']").GetByText("Show History").ClickAsync();

        var verHistory = tp.Locator("//*[@id=\"versionHistory\"]");
        int timeLasped = 0;
        while (timeLasped < 5 * 3)
        {
            if (!(await verHistory.IsVisibleAsync()))
            {
                Console.WriteLine("Catalog history is not visible yet, delay 20s");
                await DelayS(20);
                timeLasped++;
            }
            else
            {
                Console.WriteLine($"Catalog history is visible after {timeLasped * 20}s");
                break;
            }
        }
        await DelayS(5);
        Assert.That(await verHistory.IsVisibleAsync(), "Expect Catelog history popup visible but not after 5 mins!");
        var resultList = tp.Locator("//*[@id=\"divVersionHistoryContent\"]");
        int rowCnt = await resultList.Locator("tbody").Locator("tr").CountAsync();
        int lastRestore = 0;
        for (int i = 0; i < rowCnt; i++) {
            string action = await resultList.Locator("tbody").Locator("tr").Nth(i).Locator("td").Nth(8).InnerTextAsync();
            if (action.Contains("Restore version"))
            {
                lastRestore = i;
            }
        }
        //Restore the earliest restorable catalog now
        await resultList.Locator("tbody").Locator("tr").Nth(lastRestore).Locator("td").Nth(8).Locator("a").ClickAsync();
        await LoadDom();
        await DelayS(5);
        //User is redirected to monitor page already
        CMProcess[] archive =
            [
                new CMProcess("", "Archive job", startTime, intCatSup, custName, "Finished OK")
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, archive);
        //Go back home
        await HomeDash("b");
        await tp.Locator("//*[@id=\"uiSupplierName\"]").FillAsync(intCatSup);
        await tp.Locator("//*[@id=\"uiSearchCatalogs\"]").ClickAsync();
        await LoadDom();
        await DelayS(5);
        await blocLoc.Locator("div[class='settings']").ClickAsync();
        await blocLoc.Locator("div[class*='settings']").GetByText("Show History").ClickAsync();
        timeLasped = 0;
        while (timeLasped < 5 * 3)
        {
            if (!(await verHistory.IsVisibleAsync()))
            {
                Console.WriteLine("Catalog history is not visible yet, delay 20s");
                await DelayS(20);
                timeLasped++;
            }
            else
            {
                Console.WriteLine($"Catalog history is visible after {timeLasped * 20}s");
                break;
            }
        }
        await DelayS(5);
        Assert.That(await verHistory.IsVisibleAsync(), "Expect Catelog history popup visible but not after 5 mins!");
        //Check last restore text is changed
        string restoredAction = await resultList.Locator("tbody").Locator("tr").Nth(lastRestore).Locator("td").Nth(8).InnerTextAsync();
        Assert.That(restoredAction, Does.Contain("Show") , "Expect to have 'Show' link but not!");
        Assert.That(restoredAction, Does.Contain("Release version into production"), "Expect to have 'Release version into production' but not!");
        //Perform restoration
        tp.Dialog += async (_, dialog) =>
        {
            Console.WriteLine($"Dialog type: {dialog.Type}");
            Console.WriteLine($"Dialog message: {dialog.Message}");

            await dialog.AcceptAsync();
        };
        await resultList.Locator("tbody").Locator("tr").Nth(lastRestore).GetByText("Release version into production").ClickAsync();
        CMProcess[] restoreLive =
            [
                new CMProcess("", "Set-Live Restored Version", startTime, intCatSup, custName, "Finished OK")
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, restoreLive);
        //Access search and make sure the internal catalog item is not found
        Console.WriteLine("Wait 1 min before check on search");
        await DelayS(60);
        await tp.GotoAsync(viewURL);
        await LoadDom();
        await DelayS(5);
        await tp.Locator("//*[@id=\"termAuto\"]").FillAsync(testDate);
        await DelayMS(500);
        await tp.Locator("//*[@id=\"termAuto\"]").PressAsync("Enter");
        await LoadDom();
        await DelayS(2);
        //It could either be No resuls found or 1 similar result
        int noResultCount = await tp.Locator("//*[@id=\"results\"]").GetByText(testDateTime, new() { Exact = true }).CountAsync();
        if (!debugMode)
        {
            Assert.That(noResultCount == 0, $"Expect to have no result contains {testDateTime} but get {noResultCount}");
        } else
        {
            Console.WriteLine($"No result count: {noResultCount}");
        }
        
    }
}
 