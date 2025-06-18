using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Wordprocessing;
using NUnit.Framework;


namespace PlaywrightTests;

[Parallelizable(ParallelScope.Fixtures)]
[TestFixture]

internal class CMTestInstanceB : CMom
{
    [OneTimeSetUp]
    public void InstanceBOTS()
    {
        CMCoordinator.WaitForStage(1);//2);
        File.Delete("TC267234_Passed.flag");
        CMCoordinator.StageDone();
    }
    [Test, Order(1)]
    [Category("CMS Test")]
    public async Task TC268232_CMS_UI_IMPORT_FLAT_SCF()
    {
        string startTime = await GetMonTime();
        await LogIn(CMS_USRB, CMS_PWDB);
        await tp.GotoAsync(CMS_CATALOG_HOME, new() { Timeout = 60000 });
        await CatchStackTrace();
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
    public async Task TC268234_CMS_UI_RELEASE_XLS_SCF_ATTACHMENT()
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
        await bloc.GetByText("Show more", new() { Exact = true}).ClickAsync();
        await bloc.Locator("//a[@data-toggle='tab' and normalize-space(text())='Submit Catalog']").ClickAsync();
        await DelayS(5);
        await tp.Locator($"//*[@id=\"{metaId}_submitCat\"]").ClickAsync();//*[@id="63045_submitCat"]
        CMProcess[] releCat =
            [
                new CMProcess("", "Release catalog", startTime, CMS_B_SUP_NAME, CMS_B_XLSX_CUSTNAME, "Finished OK")
            ];
        await MonProcesses(CMS_CATALOG_MONITOR, releCat);
        File.WriteAllText("TC267234_Passed.flag", "ok");
    }

    [Test, Order(3)]
    [Category("CMB Test")]
    public async Task TC268238_CMB_Release_External_Catalog()
    {
        Assert.That(File.Exists("TC267234_Passed.flag"), "TC268234_CMS_UI_RELEASE_XLS_SCF_ATTACHMENT failed! Skip testing");
        File.Delete("TC267234_Passed.flag");
        string startTime = await GetMonTime();
        await LogIn(CMB_USRB, CMB_PWDB);
        await HomeDash("b");
        var blocId = await FindCatalog(CMS_B_SUP_NAME);
        var metaId = await GetMetaId(blocId);
        var blocX = tp.Locator($"id={blocId}");
        //Click show more
        await blocX.GetByText("Show more").ClickAsync();
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
        await blocX.GetByText("Show more", new() {  Exact = true }).ClickAsync();
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
    }
}
