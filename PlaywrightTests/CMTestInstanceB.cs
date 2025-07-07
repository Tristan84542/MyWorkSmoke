using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Wordprocessing;
using ICSharpCode.SharpZipLib;
using Microsoft.Playwright;
using NUnit.Framework;


namespace PlaywrightTests;

[Parallelizable(ParallelScope.Fixtures)]
[TestFixture]

internal class CMTestInstanceB : CMom
{
    private static bool TC268238 = false;
    private static bool TC274457 = false;
    private static bool TC274458 = false;

    [Test, Order(1)]
    [Category("CMS Test")]
    public async Task TC268232_CMS_UI_IMPORT_FLAT_SCF()
    {
        string startTime = await GetMonTime();
        await LogIn(CMS_USRB, CMS_PWDB);
        await HomeDash("s");
        string[] CUST1File = [TXT_FILE];
        string[] CUST1Type = ["content"];
        await CMSUploadFile(CMS_B_TXT_CUSTNAME, CUST1File, CUST1Type);
        string[] CUST2File  = [XLS_FILE];
        string[] CUST2Type = ["content"];
        await CMSUploadFile(CMS_B_XLS_CUSTNAME, CUST2File, CUST2Type);
        CMProcess[] fSCFImport =
        [
            new CMProcess("", "Simple Catalog import", startTime, CMS_B_SUP_NAME, CMS_B_TXT_CUSTNAME, "Finished OK"),
            new CMProcess("", "Simple Catalog import", startTime, CMS_B_SUP_NAME, CMS_B_XLS_CUSTNAME, "Finished OK")
        ];
        await MonProcesses(CMS_CATALOG_MONITOR, fSCFImport);
    }

    [Test, Order(2)]
    [Category("CMS Test")]
    public async Task TC268234_CMS_UI_RELEASE_XLSX_SCF_ATTACHMENT()
    {
        try
        {
            string startTime = await GetMonTime();
            await LogIn(CMS_USRB, CMS_PWDB);
            await tp.GotoAsync(CMS_CATALOG_HOME, new() { Timeout = 60000 });
            await CatchStackTrace();
            string[] files = [XLSX_FILE, ATTACHMENT_FILE];
            string[] types = ["content", "attachment"];
            await CMSUploadFile(CMS_B_XLSX_CUSTNAME, files, types);
            CMProcess[] importWAtt =
                [
                    new CMProcess("", "Simple Catalog import", startTime, CMS_B_SUP_NAME, CMS_B_XLSX_CUSTNAME, "Finished OK"),
                new CMProcess("", "Attachment processing", startTime, CMS_B_SUP_NAME, "", "Finished OK")
                ];
            await MonProcesses(CMS_CATALOG_MONITOR, importWAtt);
            await tp.GotoAsync(CMS_CATALOG_HOME, new() { Timeout = 60000 });
            await CatchStackTrace();
            var blocId = await FindCatalog(CMS_B_XLSX_CUSTNAME);
            string metaId = await GetMetaId(blocId);
            var bloc = tp.Locator($"id={blocId}");
            await bloc.GetByText("Show more", new() { Exact = true }).ClickAsync();
            await bloc.Locator("//a[@data-toggle='tab' and normalize-space(text())='Submit Catalog']").ClickAsync();
            await DelayS(5);
            await tp.Locator($"//*[@id=\"{metaId}_submitCat\"]").ClickAsync();//*[@id="63045_submitCat"]
            CMProcess[] releCat =
                [
                    new CMProcess("", "Release catalog", startTime, CMS_B_SUP_NAME, CMS_B_XLSX_CUSTNAME, "Finished OK")
                ];
            await MonProcesses(CMS_CATALOG_MONITOR, releCat);
            File.WriteAllText("TC268234_Passed.flag", "OK");
        }
        finally
        {
            File.WriteAllText("TC268234_Done.flag", "DONE");
        }
        
    }

    [Test, Order(3)]
    [Category("CMB Test")]
    public async Task TC268238_CMB_Release_External_Catalog()
    {
        Assume.That(File.Exists("TC268234_Passed.flag"), "TC268234_CMS_UI_RELEASE_XLSX_SCF_ATTACHMENT failed! Skip testing");
        string startTime = await GetMonTime();
        await LogIn(CMB_USRB, CMB_PWDB);
        await HomeDash("b");
        var blocId = await FindCatalog(CMS_B_SUP_NAME);
        var metaId = await GetMetaId(blocId);
        var blocLoc = tp.Locator($"id={blocId}");
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
                new CMProcess("", "Load Catalog", startTime, CMS_B_SUP_NAME, CMS_B_XLSX_CUSTNAME, "Finished OK"),
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, loadCat);
        await HomeDash("b");
        var statusVal = await tp.Locator($"//*[@id=\"{blocId}\"]/div/div[3]/div[2]/div").InnerTextAsync();//*[@id="237593_allTasks_catalog"]/div/div[3]/div[2]/div
        Assert.That(statusVal, Does.Contain("Catalog to approve"));
        await blocLoc.GetByText("Show more", new() {  Exact = true }).ClickAsync();
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
        //Return to dashboard with release catalog 
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
                new CMProcess("", "Set Live", startTime, CMS_B_SUP_NAME, CMS_B_XLSX_CUSTNAME, "Finished OK"),
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, setLive);
        TC268238 = true;
    }
    [Test, Order(4)]
    [Category("CMB Test")]
    public async Task TC274459_CMB_DIFFING_REPORT()
    {
        //TC268238 = true;
        //Test will not run if release external catalog test failed
        //await WaitTCDone("TC268236_Done.flag");
        Assume.That(TC268238, "Release external catalog failed, skip test");
        //Test will upload a new supplier version and check the diffing report
        //And it shall compare with production version
        //Using the external catalog... xlsx
        string uFile = "Catalog_scf_XLSX_u.xlsx";
        string startTime = await GetMonTime();
        string dlTime = await GetDLTime("b");
        await LogIn(CMB_USRB, CMB_PWDB);
        await HomeDash("b");
        await FilterSup(CMS_B_SUP_NAME);
        var blocId = await FindCatalog(CMS_B_SUP_NAME);
        var metaId = await GetMetaId(blocId);
        var blocLoc = tp.Locator($"id={blocId}");//CSS selector
        //Reject any new supplier catalog
        if (await blocLoc.GetByText("New version available").CountAsync() > 0)
        {
            Console.WriteLine("Catalog in status new version available, need reject catalog");
            await blocLoc.GetByText("Show more").ClickAsync();
            await LoadDom();
            await DelayS(5);
            await tp.Locator($"[id=\"{metaId}_allTasks_tabSupplierCatalog\"]").GetByText("Reject Catalog").ClickAsync();
            await DelayS(5);
            //*[@id="237593_allTasks_tabSupplierCatalog"]/div[2]/div/div[2]/a[2]
            string? rejPopupClass = await tp.Locator("//*[@id='uiRejectComment']").GetAttributeAsync("class");
            Assert.That(rejPopupClass.Contains("modal fade in"));
            await tp.Locator("//*[@id='uiRejectCommentText']").FillAsync("Reject for Diffing report Test");
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
        //Upload test file
        await tp.Locator("//*[@id=\"btnShowUploadModal\"]").ClickAsync();
        var uploadPop = tp.Locator("//*[@id=\"uiUploadModul\"]");
        Assert.That(await uploadPop.IsVisibleAsync());
        await DelayS(2);
        Console.WriteLine("To upload catalog file");
        await tp.Locator($"[id=\"uiFileSelect\"]").SetInputFilesAsync(
            new[] { FILE_PATH + uFile });
        await DelayMS(500);
        await tp.Locator($"//*[@id=\"{uFile}_selectType\"]").SelectOptionAsync("content");
        await DelayMS(500);
        await uploadPop.GetByText("Process Files").ClickAsync();
        await DelayMS(500);
        await uploadPop.Locator("button").Nth(0).ClickAsync();
        await DelayS(5);
        CMProcess[] catImport =
            [
                new CMProcess("", "Simple Catalog import", startTime, CMS_B_SUP_NAME, CMS_B_XLSX_CUSTNAME, "Finished OK")
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, catImport);
        //Back dashboard and view diffing report
        await DelayS(10);
        await HomeDash("b");
        await FilterSup(CMS_B_SUP_NAME);
        //What the hell a delay of status update?
        Assert.That(await blocLoc.GetByText("New version available").IsVisibleAsync(), "Catalog status ");
        await blocLoc.GetByText("Show more").ClickAsync();
        await LoadDom();
        await DelayS(5);
        await tp.Locator($"[id=\"{metaId}_allTasks_supplierCatalogDiffing\"]").GetByText("View Diffing Report").ClickAsync();
        await LoadDom();
        await DelayS(5);
        await CatchStackTrace();
        await ReloadIfBackdrop();
        var bodyContent = tp.Locator("//*[@id=\"bodyContent\"]");
        int rows = await bodyContent.Locator("tr[id^='mainRow']").CountAsync();
        int difItem1Cnt = await bodyContent.Locator("tr[id^='mainRow']").GetByText("11-015.5000", new LocatorGetByTextOptions { Exact = true }).CountAsync();
        int difitem2Cnt = await bodyContent.Locator("tr[id^='mainRow']").GetByText("11-015.9025", new LocatorGetByTextOptions { Exact = true }).CountAsync();
        Assert.That(rows == 2, "Expect only 2 changed item but not!");
        Assert.That(difItem1Cnt == 1, $"Expecting 11-015.5000 but get {difItem1Cnt} of it");
        Assert.That(difItem1Cnt == 1, $"Expecting 11-015.9025 but get {difItem1Cnt} of it");
        await HomeDash("b");
        await blocLoc.GetByText("Show more").ClickAsync();
        await LoadDom();
        await DelayS(5);
        await tp.Locator($"[id=\"{metaId}_allTasks_supplierCatalogDiffing\"]").GetByText("Download Diffing Report").ClickAsync();
        await LoadDom();
        await DelayS(5);
        CMProcess[] templateExport =
            [
                new CMProcess("", "Template Export", startTime, CMS_B_SUP_NAME, CMS_B_XLSX_CUSTNAME, "Finished OK")
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, templateExport);
        await CMBDownload(dlTime, "Diffing Report", "TC274459_CMB_DIFFING_REPORT.zip");

    }

    [Test, Order(5)]
    [Category("CMB Test")]
    public async Task TC274457_CMB_SUPPLIER_CHECKROUTINE()
    {
        string fileName = CRS_FILE;
        string startTime = await GetMonTime();
        await LogIn(CMB_USRB, CMB_PWDB);
        await HomeDash("b");
        await CMBRejectCatalog(CMS_B_SUP_NAME);
        await tp.Locator("//*[@id=\"uiSupplierName\"]").FillAsync(CMS_B_SUP_NAME);
        await tp.Locator("//*[@id=\"uiSearchCatalogs\"]").ClickAsync();
        await LoadDom();
        await DelayS(5);
        var blocId = await FindCatalog(CMS_B_SUP_NAME);
        var metaId = await GetMetaId(blocId);
        var blocLoc = tp.Locator($"id={blocId}");//CSS selector
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
                new CMProcess("", "Simple Catalog import", startTime, CMS_B_SUP_NAME, CMS_B_XLSX_CUSTNAME, "Failed")
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, catImport);
        //Go back home > show more
        await HomeDash("b");
        await FilterSup(CMS_B_SUP_NAME);
        //Click show more
        await blocLoc.GetByText("Show more").ClickAsync();
        await LoadDom();
        await DelayS(5);
        Assert.That(await tp.Locator($"//*[@id=\"{metaId}_allTasks_navWizard\"]/li[1]").IsVisibleAsync(),"Supplier Chevron is not visible");
        Assert.That(await tp.Locator($"//*[@id=\"{metaId}_allTasks_navWizard\"]/li[1]").InnerTextAsync(), Does.Contain("Errors (2)"), "Expect supplier catalog chevron contains 'Error (2)' but not!");
        Assert.That(await tp.Locator($"//*[@id=\"{metaId}_allTasks_navWizard\"]/li[1]").GetAttributeAsync("class"), Does.Contain("active"), "Supplier Catalog chevron is not active!");
        Console.WriteLine("To open item view of error correction");
        //*[@id="63045_237593_SupplierErrorReportItemsContent"]/table/tbody/tr/td[7]/a
        await blocLoc.Locator("div[id$='SupplierErrorReportItemsContent']").Locator("a[onclick^='showSupplierItemViewWithLoading']").ClickAsync();
        await LoadDom();
        await DelayS(5);
        //we have 2 uiItemView, supposingly 1 for supplier checkroutine and 1 for customer checkroutine
        var iVPop = tp.Locator($"div[id$=\"{metaId}_uiItemView\"]").First;
        Assert.That(await iVPop.IsVisibleAsync(), "Item view popup is not visible!");
        var iVRows = iVPop.Locator("//*[@id=\"uiItemViewForm\"]").Locator("tbody");
        int rowCnt = await iVRows.Locator("tr").CountAsync();
        //Update correction value
        for (int j = 0; j < rowCnt; j++)
        {
            string corValue = await iVRows.Locator("tr").Nth(j).Locator("td").Nth(1).InnerTextAsync() + " " + testDate;
            //*[@id="63045_itemViewDetails"]/tr[1]/td[5]/input[1]
            await iVRows.Locator("tr").Nth(j).Locator("td").Nth(4).Locator("input").Nth(0).FillAsync(corValue);
        }
        await DelayS(2);
        await iVPop.GetByText("Save All").ClickAsync();
        await LoadDom();
        await DelayS(5);
        //Revalidate cataglog
        await blocLoc.Locator("a[id$='btnSupplierRevalidate']").ClickAsync();
        await LoadDom();
        await DelayS(2);
        catImport[0].State = "Finished OK";
        await MonProcesses(CMB_CATALOG_MONITOR, catImport);
        CMProcess[] relCat =
            [
                new CMProcess("", "Release catalog", startTime, CMS_B_SUP_NAME, CMS_B_XLSX_CUSTNAME, "Finished OK")
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, relCat);
        await HomeDash("b");
        //Search for spplier
        await FilterSup(CMS_B_SUP_NAME);
        int newVerCnt = await blocLoc.GetByText("New Version available").CountAsync();
        Assert.That(newVerCnt == 1, $"Expect 1 'New Version available but get {newVerCnt}");
        TC274457 = true;
    }
    [Test, Order(6)]
    [Category ("CMB Test")]
    public async Task TC274458_CMB_CUSTOMER_CHECK_ROUTINE()
    {
        //TC274457 = true;
        string startTime = await GetMonTime();

        if (!TC274457)
        {
            //If previous case fail, upload a catalog that is customer check routine and Enrichment
            Console.WriteLine("CMB_SUPPLIER_CHECKROUTINE is not passed, upload file manually!");
            string fileName = "Catalog_scf_CRC.xlsx";

            await LogIn(CMB_USRB, CMB_PWDB);
            await HomeDash("b");
            await CMBRejectCatalog(CMS_B_SUP_NAME);
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
                    new CMProcess("", "Simple Catalog import", startTime, CMS_B_SUP_NAME, CMS_B_XLSX_CUSTNAME, "Finished OK")
                ];
            await MonProcesses(CMB_CATALOG_MONITOR, catImport);
            CMProcess[] relCat =
            [
                new CMProcess("", "Release catalog", startTime, CMS_B_SUP_NAME, CMS_B_XLSX_CUSTNAME, "Finished OK")
            ];
            await MonProcesses(CMB_CATALOG_MONITOR, relCat);
        }
        else
        { 
            await LogIn(CMB_USRB, CMB_PWDB);
        }
        await HomeDash("b");
        await FilterSup(CMS_B_SUP_NAME);
        var blocId = await FindCatalog(CMS_B_SUP_NAME);
        var metaId = await GetMetaId(blocId);
        var blocLoc = tp.Locator($"id={blocId}");//CSS selector
        //Click show more
        await blocLoc.GetByText("Show more").ClickAsync();
        await LoadDom();
        await DelayS(5);
        //Previous test passed, catalog is in new version avaialable
        string? supCatActive = await tp.Locator($"//*[@id=\"{metaId}_allTasks_navWizard\"]").Locator("li").Nth(1).GetAttributeAsync("class");
        //This to avoid during debug that previous customer error still exist
        if (supCatActive != "active")
        {
            await tp.Locator($"//*[@id=\"{metaId}_allTasks_navWizard\"]").Locator("li").Nth(1).ClickAsync();
            await LoadDom();
            await DelayS(5);
        }
        await blocLoc.Locator("a[onclick^='createWorkingVersion']").ClickAsync();
        await LoadDom();
        await DelayS(2);
        CMProcess[] loadCat =
            [
                new CMProcess("", "Load Catalog", startTime, CMS_B_SUP_NAME, CMS_B_XLSX_CUSTNAME, "Finished OK")
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, loadCat);
        await HomeDash("b");
        //Click show more
        await blocLoc.GetByText("Show more").ClickAsync();
        await LoadDom();
        await DelayS(5);
        //*[@id="237593_allTasks_navWizard"]/li[3]
        Assert.That(await tp.Locator($"//*[@id=\"{metaId}_allTasks_navWizard\"]").IsVisibleAsync(), "Progress Chevron is not visible");
        Assert.That(await tp.Locator($"//*[@id=\"{metaId}_allTasks_navWizard\"]").Locator("li").Nth(2).InnerTextAsync(), Does.Contain("Error Correction (2)"), "Expect chevron contains 'Error (2)' but not!");
        Assert.That(await tp.Locator($"//*[@id=\"{metaId}_allTasks_navWizard\"]").Locator("li").Nth(2).GetAttributeAsync("class"), Does.Contain("active"), "Error correction chevron is not active!");
        await tp.Locator($"//*[@id=\"{metaId}_ErrorReportItemsContent\"]").Locator("a[onclick^='showItemViewWithLoading']").ClickAsync();
        await LoadDom();
        await DelayS(5);
        var iVPop = tp.Locator($"//*[@id=\"{metaId}_uiItemView\"]").Last;
        Assert.That(await iVPop.IsVisibleAsync(), "Item view popup is not visible!");
        var iVRows = iVPop.Locator("//*[@id=\"uiItemViewForm\"]").Locator("tbody");
        int rowCnt = await iVRows.Locator("tr").CountAsync();
        //Update correction value
        for (int j = 0; j < rowCnt; j++)
        {
            string corValue = $"Item Long Description -{j}- " + " " + testDate;
            //*[@id="63045_itemViewDetails"]/tr[1]/td[5]/input[1]
            await iVRows.Locator("tr").Nth(j).Locator("td").Nth(4).Locator("input").Nth(0).FillAsync(corValue);
        }
        await DelayS(2);
        await iVPop.GetByText("Save All").ClickAsync();
        await LoadDom();
        await DelayS(5);
        await tp.Locator($"//*[@id=\"{metaId}_btnRevalidate\"]").ClickAsync();
        await LoadDom();
        await DelayS(2);
        CMProcess[] revalCat =
            [
                new CMProcess("", "Revalidate catalog", startTime, CMS_B_SUP_NAME, CMS_B_XLSX_CUSTNAME, "Finished OK")
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, revalCat);
        await HomeDash("b");
        await FilterSup(CMS_B_SUP_NAME);
        int newVerCnt = await blocLoc.GetByText("Catalog to approve").CountAsync();
        Assert.That(newVerCnt == 1, $"Expect 1 'Catalog to approve' but get {newVerCnt}");
        TC274458 = true;
    }
    [Test, Order(7)]
    [Category ("CMB Test")]
    public async Task TC274465_CMB_ENRICHMENT_EXECUTE()
    {
        string startTime = await GetMonTime();
        await LogIn(CMB_USRB, CMB_PWDB);
        await HomeDash("b");
        await FilterSup(CMS_B_SUP_NAME);
        var blocId = await FindCatalog(CMS_B_SUP_NAME);
        var metaId = await GetMetaId(blocId);
        var blocLoc = tp.Locator($"id={blocId}");//CSS selector

        if (!TC274458)
        {
            Console.WriteLine("CMB_CUSTOMER_CHECKROUTINE not passed, upload Enrichment only catalog!");
            string fileName = "Catalog_scf_ENRICH.xlsx";
            await CMBRejectCatalog(CMS_B_SUP_NAME);
            //Upload test file
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
                    new CMProcess("", "Simple Catalog import", startTime, CMS_B_SUP_NAME, CMS_B_XLSX_CUSTNAME, "Finished OK")
                ];
            await MonProcesses(CMB_CATALOG_MONITOR, catImport);
            CMProcess[] relCat =
            [
                new CMProcess("", "Release catalog", startTime, CMS_B_SUP_NAME, CMS_B_XLSX_CUSTNAME, "Finished OK")
            ];
            await MonProcesses(CMB_CATALOG_MONITOR, relCat);
            await HomeDash("b");
            await FilterSup(CMS_B_SUP_NAME);
            //Click show more
            await blocLoc.GetByText("Show more").ClickAsync();
            await LoadDom();
            await DelayS(5);
            //Previous test passed, catalog is in new version avaialable
            string? supCatActive = await tp.Locator($"//*[@id=\"{metaId}_allTasks_navWizard\"]").Locator("li").Nth(1).GetAttributeAsync("class");
            //This to avoid during debug that previous customer error still exist
            if (supCatActive != "active")
            {
                await tp.Locator($"//*[@id=\"{metaId}_allTasks_navWizard\"]").Locator("li").Nth(1).ClickAsync();
                await LoadDom();
                await DelayS(5);
            }
            await blocLoc.Locator("a[onclick^='createWorkingVersion']").ClickAsync();
            await LoadDom();
            await DelayS(2);
            CMProcess[] loadCat =
                [
                    new CMProcess("", "Load Catalog", startTime, CMS_B_SUP_NAME, CMS_B_XLSX_CUSTNAME, "Finished OK")
                ];
            await MonProcesses(CMB_CATALOG_MONITOR, loadCat);
            await HomeDash("b");
            await FilterSup(CMS_B_SUP_NAME);
        }
        else
        {
            Console.WriteLine("CMB_CUSTOMER_CHECKROUTINE passed, catalog is loaded as working version, manually apply enrichment now");
            Console.WriteLine("User is at Dashboard already!");
        }
        //Check automate enrichment
        //Open Item list from review item button
        await blocLoc.GetByText("Show more").ClickAsync();
        await LoadDom();
        await DelayS(5);
        await blocLoc.Locator($"//*[@id=\"{metaId}_allTasks_tabApproveItems\"]").Locator("a").GetByText("Review Items").ClickAsync();
        await LoadDom();
        await DelayS(5);
        await CatchStackTrace();
        await ReloadIfBackdrop();
        //Need to set column to Enrichment set
        await tp.Locator("//*[@id=\"uiColumnSet\"]").SelectOptionAsync(new SelectOptionValue { Label = "Enrichment" });
        await LoadDom();
        await DelayS(5);
        int cnt1Key = await tp.GetByText("test 1key").CountAsync();
        Assert.That(cnt1Key, Is.EqualTo(1), $"1 key mapping result not expected! {cnt1Key}");
        int cnt2Key = await tp.GetByText("test 2key").CountAsync();
        int cnt2_Key = await tp.GetByText("test 2 key").CountAsync();
        Assert.That(cnt2Key == 0 && cnt2_Key == 0, $"2 key mapping result not expected! {cnt2Key} & {cnt2_Key}");
        await HomeDash("b");
        await FilterSup(CMS_B_SUP_NAME);
        //Click show more
        await blocLoc.GetByText("Show more").ClickAsync();
        await LoadDom();
        await DelayS(5);
        //Should land at approve item chevron
        Assert.That(await tp.Locator($"//*[@id=\"{metaId}_allTasks_navWizard\"]").IsVisibleAsync(), "Progress Chevron is not visible");
        Assert.That(await tp.Locator($"//*[@id=\"{metaId}_allTasks_navWizard\"]").Locator("li").Nth(2).InnerTextAsync(), Does.Contain("Approve Items"), "Expect chevron is approve item but not!");
        Assert.That(await tp.Locator($"//*[@id=\"{metaId}_allTasks_navWizard\"]").Locator("li").Nth(2).GetAttributeAsync("class"), Does.Contain("active"), "Chevron is not active!");
        //Open enrichment menu
        await blocLoc.Locator($"//*[@id=\"{metaId}_allTasks_tabApproveItems\"]").Locator("a").GetByText("Enrichment").ClickAsync();
        await LoadDom();
        await DelayS(2);
        //Find row that contains 2key mapping manual
        //First assert ui available
        var manEnrichPop = tp.Locator("//*[@id=\"uiManualEnrichments\"]");
        Assert.That(await manEnrichPop.IsVisibleAsync(), "UI is not visible!");
        var enrichTable = manEnrichPop.Locator("//*[@id=\"uiManualEnrichmentsContent\"]");
        int enrichRows = await enrichTable.Locator("tr").CountAsync();
        bool enrichFound = false;
        for (int i = 0; i < enrichRows; i++)
        {
            //*[@id="uiManualEnrichmentsContent"]/tr[2]/td[3]
            string description = await enrichTable.Locator("tr").Nth(i).Locator("td").Nth(2).InnerTextAsync();
            if (description.Contains("2key mapping manual"))
            {
                await enrichTable.Locator("tr").Nth(i).Locator("td").Nth(0).Locator("input").CheckAsync();
                enrichFound = true;
                break;
            }
        }
        Assert.That(enrichFound, Is.True, "CANNOT found target enrichment '2key mapping manual' !!!");
        await DelayMS(500);
        //Make sure it is selected enrichment only
        await tp.Locator("//*[@id=\"uiManualEnrichmentSelectionType\"]").SelectOptionAsync("selected");
        await DelayMS(500);
        await tp.Locator("//*[@id=\"btnExecuteManualEnrichments\"]").ClickAsync();
        await LoadDom();
        await DelayS(5);
        CMProcess[] enrichProc =
            [
                new CMProcess("", "Enrichment", startTime, CMS_B_SUP_NAME, CMS_B_XLSX_CUSTNAME, "Finished OK")
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, enrichProc);
        //Check enrichment execution
        await HomeDash("b");
        await FilterSup(CMS_B_SUP_NAME);
        await blocLoc.GetByText("Show more").ClickAsync();
        await LoadDom();
        await DelayS(5);
        await blocLoc.Locator($"//*[@id=\"{metaId}_allTasks_tabApproveItems\"]").Locator("a").GetByText("Review Items").ClickAsync();
        await LoadDom();
        await DelayS(5);
        await CatchStackTrace();
        await ReloadIfBackdrop();
        cnt2Key = await tp.GetByText("test 2key").CountAsync();
        cnt2_Key = await tp.GetByText("test 2 key").CountAsync();
        cnt1Key = await tp.GetByText("test 1key").CountAsync();
        Assert.That(cnt1Key, Is.EqualTo(1), $"1 key mapping result not expected! {cnt1Key}");
        Assert.That((cnt2Key == 1 && cnt2_Key == 1), $"2 key mapping result not expected! {cnt2Key} & {cnt2_Key}");
    }
    
}
