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
using Path = System.IO.Path;
using System.Drawing.Printing;
using DocumentFormat.OpenXml.Wordprocessing;



namespace PlaywrightTests;

[Parallelizable(ParallelScope.Fixtures)]
[TestFixture]

internal class CMTestInstanceC : CMom
{
    
    private static bool crsPassed = false;
    private static bool TC274456Passed = false;
    [Test, Order(1)]
    [Category("CMBA Test")]
    public async Task TC274466_CMBA_CREATE_EDIT_USER()
    {
        WaitInit();
        string userSufix = testDateTime;
        await LogIn(CMB_USRB, CMB_PWDB);
        await tp.GotoAsync(CMB_CATALOG_EDITUSER, new PageGotoOptions() { Timeout = dfTimeout});
        await CatchStackTrace();
        await LoNetDom(5);
        string newUser = "";
        switch (ENVIRONMENT.ToLower())
        {
            case "prod":
                newUser = "PROD" + userSufix; break;
            case "qa":
                newUser = "QA" + userSufix; break;
            default:
                throw new ArgumentException($"Invalid environment argument {ENVIRONMENT}");
        }
        await tp.GetByText("Add new User", new PageGetByTextOptions { Exact = true }).ClickAsync();
        await LoNetDom();
        await DelayS(5);
        await tp.Locator("#ctl00_MainContent_TextBox1").FillAsync(newUser);
        await tp.Locator("#ctl00_MainContent_TextBox2").FillAsync(newUser);//first name
        await tp.Locator("#ctl00_MainContent_TextBox3").FillAsync("userLast");//last name
        await tp.Locator("#ctl00_MainContent_TextBox4").FillAsync($"omnicontent+{newUser}@gmail.com");//email
        await tp.Locator("#ctl00_MainContent_TextBox5").FillAsync($"{newUser}!");//password
        await tp.Locator("#ctl00_MainContent_TextBox6").FillAsync($"{newUser}!");//password confirmation
        await tp.GetByLabel("Buyer", new() { Exact = true }).CheckAsync();
        await tp.GetByRole(AriaRole.Link, new() { Name = "Save" }).ClickAsync();
        await LoNetDom();
        await DelayS(2);
        await LogOut();
        await NuLogin(newUser, newUser + "!");
        await tp.Locator("top-bar-user-section[name='User']").ClickAsync();
        await DelayS(2);
        string userProfileName = await tp.Locator("app-topbar-section[icon='user-circle']").Locator("h3[class='topbar-user-section__user']").InnerTextAsync();
        Assert.That(userProfileName.Contains("userLast"), "User profile do not have user last name");
        Assert.That(userProfileName.Contains(newUser), $"User profile do not have user first name {newUser}");
        //Close the user profile widget for log out
        await tp.Locator("top-bar-user-section[name='User']").ClickAsync();
        await DelayS(2);
        await LogOut();
        //Edit new user
        await LogIn(CMB_USRB, CMB_PWDB);
        await tp.GotoAsync(CMB_CATALOG_EDITUSER);
        await LoNetDom();
        await DelayS(5);
        //Search by login
        await tp.Locator("//*[@id=\"ctl00_MainContent_FilterControl1_ctl00_TextBox4\"]").FillAsync(newUser);
        await tp.Locator("//*[@id=\"ctl00_MainContent_FilterControl1_lblSearch\"]").ClickAsync();
        await LoNetDom();
        await DelayS(2);
        await tp.GetByRole(AriaRole.Link, new() { Name = "Edit", Exact = true }).ClickAsync();
        await LoNetDom();
        await DelayS(2);
        //Update last name
        await tp.Locator("#ctl00_MainContent_TextBox3").FillAsync("LastNameEdit");//last name
        await tp.GetByRole(AriaRole.Link, new() { Name = "Save" }).ClickAsync();
        await LoNetDom();
        await DelayS(5);
        await LogOut();
        await NuLogin(newUser, newUser + "!");
        await tp.Locator("top-bar-user-section[name='User']").ClickAsync();
        await DelayS(2);
        userProfileName = await tp.Locator("app-topbar-section[icon='user-circle']").Locator("h3[class='topbar-user-section__user']").InnerTextAsync();
        Assert.That(userProfileName.Contains("LastNameEdit"), "User profile do not have user last name");
    }

    [Test, Order(2)]
    [Category("CMB Test")]
    public async Task TC274468_CMBA_CUSTOM_LANDING_MANAGEMENT()
    {
        string startTime = await GetMonTime();
        await LogIn(CMB_USRB, CMB_PWDB);
        await tp.GotoAsync(CMB_CUST_LANDING);
        await LoNetDom(10);
        string pageName = $"{ENVIRONMENT}_{startTime}_TEST";
        await tp.GetByRole(AriaRole.Link, new() { Name = "Create New Landing Page" }).ClickAsync();
        await DelayS(5);
        Assert.That(await tp.Locator("//*[@id=\"uiNewLandingPage\"]").IsVisibleAsync(), "New custom landing page popup not visible!");
        await tp.Locator("//*[@id=\"newName\"]").FillAsync(pageName);
        await tp.Locator("//*[@id=\"newDescription\"]").FillAsync(pageName);
        await tp.Locator("#uiNewLandingPage").GetByText("Save").ClickAsync();
        await LoNetDom(10);
        await tp.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await DelayS(5);
        //Assign landing page to view
        await tp.Locator("//*[@id=\"availablePage\"]").SelectOptionAsync(new SelectOptionValue() { Label = pageName });
        await DelayS(2);
        string viewName = "";
        if (ENVIRONMENT.ToLower() == "qa")
        {
            viewName = "SVVIEW1";
        }
        if (ENVIRONMENT.ToLower() == "prod")
        {
            viewName = "TESTCOE01";
        }
        await tp.Locator("//*[@id=\"selectedView\"]").SelectOptionAsync(new SelectOptionValue() { Label = viewName });
        await DelayMS(500);
        await tp.Locator("//*[@id=\"configureButton\"]").ClickAsync();
        await LoNetDom(5);
        string pageURL = tp.Url;
        Assert.That(pageURL.Contains("catalog/search5/showMenu.action"), $"Expect in search page but at {pageURL}");
        string footer = await tp.Locator("footer[class='site-footer']").Locator("strong").InnerTextAsync();
        Console.WriteLine(footer);
        Assert.That(footer.Contains(viewName), $"Expect view name {viewName} but returned footnote {footer}");
    }

    [Test, Order(3)]
    [Category("CMB Test")]
    async public Task TC274469_CMB_DATAGROUP_ASSIGNMENT_N_DOWNLOAD()
    {
        bool userAssigned = false;
        string aUser = "";
        switch (ENVIRONMENT.ToLower())
        {
            case "qa":
                aUser = "SVB-0001 Buyer admin"; break;
            case "prod":
                aUser = "Regression User C"; break;
                //No uat, no need default cause thing can't work at the start if not qa / prod
        }
        //Start of test, go to page and get prepared
        string startTime = await GetMonTime();
        string dlTime = await GetDLTime("b");
        await LogIn(CMB_USRB, CMB_PWDB);
        //Head to data group page
        await tp.GotoAsync(CMB_DATAGPUA);
        await LoNetDom(5);
        await CatchStackTrace();
        var dgTable = tp.Locator("//*[@id=\"uiDataGroupContent\"]");
        //find the row that contains '{env} test 1key enrichment'
        string tarEnrich = "test 1key enrichment";
        int dgtRows = await dgTable.Locator("tr").CountAsync();
        int tarRow = 0;
        for (int i = 0; i < dgtRows; i++)
        {
            string name = await dgTable.Locator("tr").Nth(i).Locator("td").Nth(0).InnerTextAsync();
            if (name.Contains(tarEnrich))
            {
                tarRow = i;
                break;
            }
        }
        string tarName = await dgTable.Locator("tr").Nth(tarRow).Locator("td").Nth(0).InnerTextAsync();
        //Because tarRow = 0 could still be not matching!
        Assert.That(tarName.Contains(tarEnrich), "Template name does not match!");
        var userAssignUI = tp.Locator("//*[@id=\"uiUserAssignment\"]");
        try
        {
            //Actual part to unassign - dl - reassign - dl
            //Click the assign user link
            await dgTable.Locator("tr").Nth(tarRow).GetByText("Assign Users").ClickAsync();
            await LoNetDom(5);
            Assert.That(await userAssignUI.IsVisibleAsync());
            //Remove defined user by name
            await tp.Locator("//*[@id=\"uiAddedUsers\"]").SelectOptionAsync(new SelectOptionValue() { Label = aUser });
            await userAssignUI.Locator("a[onclick='removeUsers()']").ClickAsync();
            await DelayS(1);
            await userAssignUI.GetByText("Save").ClickAsync();
            userAssigned = false;
            await LoNetDom(5);
            string templateUser = await dgTable.Locator("tr").Nth(tarRow).Locator("td").Nth(3).InnerTextAsync(); // expect - but will check against user name
            Assert.That(!templateUser.Contains(aUser), $"{aUser} still found as assigned user!");
            //Now go download page
            await tp.GotoAsync(CMB_CATALOG_DL);
            await LoNetDom(5);
            await CatchStackTrace();
            await ReloadIfBackdrop();
            await tp.GetByText("New Download", new PageGetByTextOptions() { Exact = true }).ClickAsync();
            await DelayS(2);
            await tp.Locator("//*[@id=\"uiTemplateType\"]").SelectOptionAsync("cus_enrich");
            await DelayS(1);
            //Get all template options
            int tempCnt = await tp.Locator("//*[@id=\"uiExportTemplateDataGroup\"]").Locator("option").CountAsync();
            string[] tempOpts = new string[tempCnt];
            for (int i = 0; i < tempCnt; i++)
            {
                tempOpts[i] = await tp.Locator("//*[@id=\"uiExportTemplateDataGroup\"]").Locator("option").Nth(i).InnerTextAsync();
            }
            //Make sure current use do not have '{env} test 1key enrichment'
            Assert.That(tempOpts.Any(LinearGradientFill => tempOpts.Contains(tarEnrich)), Is.False, $"{ENVIRONMENT} test 1key enrichment is available to download!");
            //Reassign user to enrichment
            await tp.GotoAsync(CMB_DATAGPUA);
            await LoNetDom(5);
            await CatchStackTrace();
            await dgTable.Locator("tr").Nth(tarRow).GetByText("Assign Users").ClickAsync();
            await LoNetDom(5);
            await tp.Locator("//*[@id=\"uiSelectUsers\"]").SelectOptionAsync(new SelectOptionValue() { Label = aUser });
            await userAssignUI.Locator("a[onclick='addUsers()']").ClickAsync();
            await DelayS(1);
            await userAssignUI.GetByText("Save").ClickAsync();
            await LoNetDom(5);
            templateUser = await dgTable.Locator("tr").Nth(tarRow).Locator("td").Nth(3).InnerTextAsync();
            Assert.That(templateUser.Contains(aUser), $"{aUser} not found as assigned user!");
            userAssigned = true;
            //Now go download page and download the 1key enrichment
            await tp.GotoAsync(CMB_CATALOG_DL);
            await LoNetDom(5);
            await ReloadIfBackdrop();
            await CatchStackTrace();
            await tp.GetByText("New Download", new PageGetByTextOptions() { Exact = true }).ClickAsync();
            await DelayS(2);
            await tp.Locator("//*[@id=\"uiTemplateType\"]").SelectOptionAsync("cus_enrich");
            await DelayS(1);
            //get all template options again
            tempCnt = await tp.Locator("//*[@id=\"uiExportTemplateDataGroup\"]").Locator("option").CountAsync();
            int tempIdx = -1;
            for (int i = 0; i < tempCnt; i++)
            {
                string dgOptions = await tp.Locator("//*[@id=\"uiExportTemplateDataGroup\"]").Locator("option").Nth(i).InnerTextAsync();
                if (dgOptions.Contains(tarName))
                {
                    tempIdx = i;
                    break;
                }
            }
            Assert.That(tempIdx >= 0, "Cannot find template!");
            await tp.Locator("//*[@id=\"uiExportTemplateDataGroup\"]").SelectOptionAsync(new SelectOptionValue() { Index = tempIdx });
            await tp.Locator("//*[@id=\"uiButtoncreateNewExportTemplate\"]").ClickAsync();
            await LoNetDom(5);
            CMProcess[] tempExp =
                [
                new CMProcess("", "Template Export", startTime, "", CMS_B_XLSX_CUSTNAME, "Finished OK")
                ];
            await MonProcesses(CMB_CATALOG_MONITOR, tempExp);
            //Go back download enrichment
            await tp.GotoAsync(CMB_CATALOG_DL);
            await LoNetDom(5);
            await ReloadIfBackdrop();
            await CatchStackTrace();
            //Find from existing list that 1. is later than start time 2. Enrichment 3. has test 1key enrichment
            var dlTable = tp.Locator("//*[@id=\"itemListContainer\"]");
            int dlItemcnt = await dlTable.Locator("tr").CountAsync();
            for (int i = 0; i < dlItemcnt; i++)
            {
                var curRow = dlTable.Locator("tr").Nth(i);
                string timestamp = await curRow.Locator("td").Nth(0).InnerTextAsync();
                string template = await curRow.Locator("td").Nth(1).InnerTextAsync();
                string filename = await curRow.Locator("td").Nth(7).Locator("a").GetAttributeAsync("href");
                filename = Path.GetFileName(filename);
                if (IsLater(dlTime, timestamp) && template.Contains("Enrichment file") && template.Contains("test 1key enrichment"))
                {
                    var wait4DL = tp.WaitForDownloadAsync();
                    await curRow.Locator("td").Nth(7).Locator("a").ClickAsync();
                    var dl = await wait4DL;
                    var saveTo = DL_PATH + filename;
                    Console.WriteLine("File is download to " + saveTo);
                    await dl.SaveAsAsync(saveTo);
                    break;
                }
            }
        }
        finally
        {
            if (!userAssigned)
            {
                Console.WriteLine("Test user is unassigned, need reassign!");
                await tp.GotoAsync(CMB_DATAGPUA);
                await LoNetDom(5);
                await CatchStackTrace();
                await dgTable.Locator("tr").Nth(tarRow).GetByText("Assign Users").ClickAsync();
                await LoNetDom(5);
                await tp.Locator("//*[@id=\"uiSelectUsers\"]").SelectOptionAsync(new SelectOptionValue() { Label = aUser });
                await userAssignUI.Locator("a[onclick='addUsers()']").ClickAsync();
                await DelayS(1);
                await userAssignUI.GetByText("Save").ClickAsync();
            }
            else
            {
                Console.WriteLine("No need to reassign user!");
            }
        }
        
    }

    [Test, Order(4)]
    [Category("CMB Test")]
    async public Task TC274461_CMB_DOWNLOAD_REPORT()
    {
        string startTime = await GetMonTime();
        string dlTime = await GetDLTime("b");
        await LogIn(CMB_USRB, CMB_PWDB);
        await tp.GotoAsync(CMB_CATALOG_RPT);
        await LoNetDom(5);
        await ReloadIfBackdrop();
        await CatchStackTrace();
        await tp.Locator("a[href='#reportParams']").ClickAsync();
        await DelayS(2);
        await tp.Locator("//*[@id=\"ddlReports\"]").SelectOptionAsync("ClassificationList");
        await LoNetDom(2);
        string rptSName = "";
        switch (ENVIRONMENT.ToLower())
        {
            case "qa":
                rptSName = "SV Supplier 1 (654321)"; break;
            case "prod":
                rptSName = "TESTSUPCDO2 (TESTSUPCDO2)"; break;
        }
        await tp.Locator("//*[@id=\"uiSupplierForClassificationReportInput\"]").FillAsync(rptSName);
        await DelayMS(500);
        await tp.Locator("//*[@id=\"uiSupplierForClassificationReportInput\"]").PressAsync("Enter");
        await DelayMS(500);
        await tp.Locator("//*[@id=\"uiButtonCreateRport\"]").ClickAsync(new LocatorClickOptions() { Force = true}); //Force click it even it's not ready yet
        await LoNetDom(5);
        CMProcess[] reportJob =
            [
                new CMProcess("", "Reporting job", startTime, "", CMS_B_XLSX_CUSTNAME, "Finished OK")
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, reportJob);
        //Back to reporting
        await tp.GotoAsync(CMB_CATALOG_RPT);
        await LoNetDom(5);
        await ReloadIfBackdrop();
        await CatchStackTrace();
        //Find from existing list that 1. is later than start time 2.Classification report
        var dlTable = tp.Locator("//*[@id=\"itemListContainer\"]");
        int dlItemcnt = await dlTable.Locator("tr").CountAsync();
        for (int i = 0; i < dlItemcnt; i++)
        {
            var curRow = dlTable.Locator("tr").Nth(i);
            string timestamp = await curRow.Locator("td").Nth(0).InnerTextAsync();
            string template = await curRow.Locator("td").Nth(1).InnerTextAsync();
            string filename = await curRow.Locator("td").Nth(6).Locator("a").GetAttributeAsync("href");
            filename = Path.GetFileName(filename);
            if (IsLater(dlTime, timestamp) && template.Contains("Classification report"))
            {
                var wait4DL = tp.WaitForDownloadAsync();
                await curRow.Locator("td").Nth(6).Locator("a").ClickAsync();
                var dl = await wait4DL;
                var saveTo = DL_PATH + filename;
                Console.WriteLine("File is download to " + saveTo);
                await dl.SaveAsAsync(saveTo);
                break;
            }
        }
    }

    [Test, Order(5)]
    [Category("CMB Test")]
    async public Task TC274462_CMB_DOWNLOAD_TEMPLATE()
    {
        string startTime = await GetMonTime();
        string dlTime = await GetDLTime("b");
        await LogIn(CMB_USRB, CMB_PWDB);
        await tp.GotoAsync(CMB_CATALOG_DL);
        await LoNetDom(5);
        await ReloadIfBackdrop();
        await CatchStackTrace();
        await tp.GetByText("New Download", new PageGetByTextOptions() { Exact = true }).ClickAsync();
        await DelayS(2);
        await tp.Locator("//*[@id=\"uiTemplateType\"]").SelectOptionAsync("classifications");
        await DelayS(1);
        await tp.Locator("//*[@id=\"uiExportTemplateFormat\"]").SelectOptionAsync("EXCEL_2007");
        await DelayS(1);
        await tp.Locator("//*[@id=\"uiButtoncreateNewExportTemplate\"]").ClickAsync();
        await LoNetDom(5);
        CMProcess[] classExp =
            [
            new CMProcess("", "Classification Export", startTime, "", CMS_B_XLSX_CUSTNAME, "Finished OK")
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, classExp);
        //Go back download enrichment
        await tp.GotoAsync(CMB_CATALOG_DL);
        await LoNetDom(5);
        await ReloadIfBackdrop();
        await CatchStackTrace();
        //Find from existing list that 1. is later than start time 2. Enrichment 3. has test 1key enrichment
        var dlTable = tp.Locator("//*[@id=\"itemListContainer\"]");
        int dlItemcnt = await dlTable.Locator("tr").CountAsync();
        for (int i = 0; i < dlItemcnt; i++)
        {
            var curRow = dlTable.Locator("tr").Nth(i);
            string timestamp = await curRow.Locator("td").Nth(0).InnerTextAsync();
            string template = await curRow.Locator("td").Nth(1).InnerTextAsync();
            string filename = await curRow.Locator("td").Nth(7).Locator("a").GetAttributeAsync("href");
            filename = Path.GetFileName(filename);
            if (IsLater(dlTime, timestamp) && template.Contains("Classification"))
            {
                var wait4DL = tp.WaitForDownloadAsync();
                await curRow.Locator("td").Nth(7).Locator("a").ClickAsync();
                var dl = await wait4DL;
                var saveTo = DL_PATH + filename;
                Console.WriteLine("File is download to " + saveTo);
                await dl.SaveAsAsync(saveTo);
                break;
            }
        }
    }

    [Test, Order(6)]
    [Category("CMB Test")]
    async public Task TC274464_CMB_ENRICHMENT_IMPORT()
    {
        string startTime = await GetMonTime();
        string key1Enrich = "1key_enrich_template_new.xlsx";
        string key2Enrich = "2key_multi_key_enrich_template.xlsx";
        await LogIn(userName_C, password_C);
        await HomeDash("b");
        //Upload file
        await tp.Locator("//*[@id=\"btnShowUploadModal\"]").ClickAsync();
        var uploadPop = tp.Locator("//*[@id=\"uiUploadModul\"]");
        Assert.That(await uploadPop.IsVisibleAsync());
        await DelayS(2);
        Console.WriteLine("To upload catalog file");
        await tp.Locator($"[id=\"uiFileSelect\"]").SetInputFilesAsync(
            new[] { FILE_PATH + key1Enrich, FILE_PATH + key2Enrich });
        await DelayMS(500);
        await tp.Locator($"//*[@id=\"{key1Enrich}_selectType\"]").SelectOptionAsync("content");
        await DelayMS(500);
        await tp.Locator($"//*[@id=\"{key2Enrich}_selectType\"]").SelectOptionAsync("content");
        await DelayMS(500);
        await uploadPop.GetByText("Process Files").ClickAsync();
        await DelayMS(500);
        await uploadPop.Locator("button").Nth(0).ClickAsync();
        await DelayS(5);
        CMProcess[] enrichImp =
            [
                new CMProcess("", "Enrichment import", startTime, "", custName_C, "Finished OK"),
                new CMProcess("", "Multikey Enrichment Import", startTime, "", custName_C, "Finished OK")
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, enrichImp);
    }

    [Test, Order(7)]
    [Category("CMS Test")]
    public async Task TC268233_CMS_CATALOG_DOWNLOAD()
    {
        string startTime = await GetMonTime();
        string dlTime = await GetDLTime("s");
        await LogIn(CMS_USRB, CMS_PWDB);
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
        await CMSDownload(blocId, "SCF Export",  "TC268233_CMS_CATALOG_DOWNLOAD.zip", dlTime);

    }
    [Test, Order(8)]
    [Category("CMS Test")]
    public async Task TC268237_CMS_CATALOG_ITEM_N_REPORT()
    {
        //This force test case to wait until the target file exist        
        await WaitTCDone("TC268234_Done.flag");
        
        string startTime = await GetMonTime();
        string dlTime = await GetDLTime("s");
        await LogIn(CMS_USRB, CMS_PWDB);
        await tp.GotoAsync(CMS_CATALOG_HOME, new() { Timeout = 60000 });
        await CatchStackTrace();
        var blocId = await FindCatalog(CMS_C_CUSTNAME);
        var blocLoc = tp.Locator($"id={blocId}");
        string metaId = await GetMetaId(blocId);
        await blocLoc.GetByText("Show Items").ClickAsync();
        await LoNetDom(5);
        await CatchStackTrace();
        string url = tp.Url;
        Assert.That(url, Does.Contain("CatalogManager/supplier/item-list"), $"Expect to be in item list but landed to {url}");
        await DelayS(5);
        await tp.Locator("//*[@id=\"ddlCatalogVersion\"]").SelectOptionAsync("CUS_RELEASED");
        await LoNetDom();
        await DelayS(5);
        await tp.Locator("//*[@id=\"uiDownloadReport\"]").ClickAsync();
        CMProcess[] catalogDL =
            [
                new CMProcess("", "Catalog Download Job", startTime, CMS_C_SUP_NAME, CMS_C_CUSTNAME, "Finished OK")
            ];
        await MonProcesses(CMS_CATALOG_MONITOR, catalogDL);
        await CMSDownload(blocId, "Catalog Download Job", "TC268237_CMS_CATALOG_ITEM_N_REPORT.zip", dlTime);
    }

    [Test, Order(9)]
    [Category("CMS Test")]
    public async Task TC268236_CMS_CHECKROUTINE()
    {
        try
        {
            await WaitTCDone("TC268235_Done.flag");
            string startTime = await GetMonTime();
            await LogIn(CMS_USRC, CMS_PWDC);
            await HomeDash("s");
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
                await HomeDash("s");
                await CatchStackTrace();
                await bloc.GetByText("Show more", new() { Exact = true }).ClickAsync();
                await DelayS(5);
                await bloc.Locator("//a[@data-toggle='tab' and contains(normalize-space(text()), 'Error Correction')]").ClickAsync();
            }
            await DelayS(5);
            //Open Item view
            await tp.Locator($"//*[@id=\"{metaId}_ErrorReportItemsContent\"]").Locator("a[onclick^='showItemViewWithLoading']").ClickAsync();
            await LoNetDom();
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
            await LoNetDom(15);
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
        finally
        {
            File.WriteAllText("TC268236_Done.flag", "DONE");
        }
        
        
    }

    [Test, Order(10)]
    [Category ("CMB Test")]
    public async Task TC274456_CMB_IMPORT_RELEASE_CATALOG()
    {
        WaitInit();
        string startTime = await GetMonTime();
        await LogIn(userName_C, password_C);
        await HomeDash("b");
        await FilterSup(intCatSup_C);
        var blocId = await FindCatalog(intCatSup_C);
        var metaId = await GetMetaId(blocId);
        var blocLoc = tp.Locator($"id={blocId}");//CSS selector
        //Upload file
        await tp.Locator("//*[@id=\"btnShowUploadModal\"]").ClickAsync();
        var uploadPop = tp.Locator("//*[@id=\"uiUploadModul\"]");
        Assert.That(await uploadPop.IsVisibleAsync());
        await DelayS(2);
        Console.WriteLine("To upload catalog file");
        await tp.Locator($"[id=\"uiFileSelect\"]").SetInputFilesAsync(
            new[] { FILE_PATH + fileName_C });
        await DelayMS(500);
        await tp.Locator($"//*[@id=\"{fileName_C}_selectType\"]").SelectOptionAsync("content");
        await DelayMS(500);
        await uploadPop.GetByText("Process Files").ClickAsync();
        await DelayMS(500);
        await uploadPop.Locator("button").Nth(0).ClickAsync();
        await DelayS(5);
        CMProcess[] catImport =
            [
                new CMProcess("", "Simple Catalog import", startTime, intCatSup_C, custName_C, "Finished OK")
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, catImport);
        CMProcess[] releaseCatalog =
            [
                new CMProcess("", "Release catalog", startTime, intCatSup_C, custName_C, "Finished OK")
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, releaseCatalog);
        await HomeDash("b");
        await tp.Locator("//*[@id=\"uiSupplierName\"]").FillAsync(intCatSup_C);
        await tp.Locator("//*[@id=\"uiSearchCatalogs\"]").ClickAsync();
        await LoNetDom(5);
        //Click show more
        await blocLoc.GetByText("Show more").ClickAsync();
        await LoNetDom(5);
        var navWiz = tp.Locator($"//*[@id=\"{metaId}_allTasks_navWizard\"]");
        Console.WriteLine("Create working version");
        string? isActive = await navWiz.Locator("li").Nth(1).GetAttributeAsync("class");
        Assert.That(isActive, Does.Contain("active"), "Supplier catalog chevron expect active but not!");
        //Create working version
        var supCat = tp.Locator($"//*[@id=\"{metaId}_allTasks_tabSupplierCatalog\"]");
        await supCat.GetByText("Create Working Version").ClickAsync();
        await LoNetDom(5);
        await ReloadIfBackdrop();
        CMProcess[] loadCat =
            [
                new CMProcess("", "Load Catalog", startTime, intCatSup_C, custName_C, "Finished OK"),
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, loadCat);
        await HomeDash("b");
        var statusVal = await tp.Locator($"//*[@id=\"{blocId}\"]/div/div[3]/div[2]/div").InnerTextAsync();//*[@id="237593_allTasks_catalog"]/div/div[3]/div[2]/div
        Assert.That(statusVal, Does.Contain("Catalog to approve"));
        await blocLoc.GetByText("Show more", new() { Exact = true }).ClickAsync();
        await LoNetDom(5);
        isActive = await navWiz.Locator("li").Nth(2).GetAttributeAsync("class");
        Assert.That(isActive, Does.Contain("active"), "Approve items chevron expect active but not!");
        //*[@id="237593_allTasks_tabApproveItems"]/div[2]/div/div[2]/a[1]
        var appItems = tp.Locator($"//*[@id=\"{metaId}_allTasks_tabApproveItems\"]");
        //Approve
        await appItems.GetByText("Review Items").ClickAsync();
        await LoNetDom(15);
        await WaitSpinOff(30);
        await CatchStackTrace();
        Assert.That(tp.Url, Does.Contain("/srvs/BuyerCatalogs/items/item-list"), "Expect to be in item review page but not!");
        await ReloadIfBackdrop();
        await DelayS(5);
        await tp.Locator("//*[@id=\"uiTableAction\"]").SelectOptionAsync("approve_all");
        await tp.Locator("//*[@id=\"uiInternalComment\"]").FillAsync($"TC268238_CMB_Release_External_Catalog on {testDate}");
        await DelayS(5);
        await tp.Locator("//*[@id=\"uiSubmitAction\"]").ClickAsync();
        await LoNetDom(15);
        await ReloadIfBackdrop();
        await tp.Locator("//*[@id=\"uiGoToReleaseTab\"]").ClickAsync();
        await LoNetDom(5);
        await WaitSpinOff(5);
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
        await LoNetDom();
        CMProcess[] setLive =
            [
                new CMProcess("", "Set Live", startTime, intCatSup_C, custName_C, "Finished OK"),
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, setLive);

        //Access search now
        Console.WriteLine("Wait 1 min before check on search");
        await DelayS(60);
        await tp.GotoAsync(viewURL_C);
        await LoNetDom();
        await DelayS(5);
        await tp.Locator("//*[@id=\"termAuto\"]").FillAsync(testDateTime);
        await DelayMS(500);
        await tp.Locator("//*[@id=\"termAuto\"]").PressAsync("Enter");
        await LoNetDom();
        await DelayS(2);
        Console.WriteLine("Should have only 1 matching result in catalog item");
                                    //*[@id="itemList"]/tbody/tr/td[4]/div/a
        //string result = await tp.Locator("//*[@id=\"itemList\"]/tbody/tr/td[3]/div/a").InnerTextAsync();
        string result = await tp.Locator("//*[@id=\"itemList\"]/tbody").Locator("tr").Nth(0).Locator("a[onclick*='showItem']").InnerTextAsync();
        Assert.That(result.Equals($"Smoke Internal Catalog 001 {testDateTime}"), $"Item is not expected! {result}");
        Console.WriteLine("Test passed");
        TC274456Passed = true;
    }

    [Test, Order(11)]
    [Category("CMB Test")]
    public async Task TC274460_CMB_ARCHIVE_RESTORE()
    {
        Assume.That(TC274456Passed, "TC274456_CMB_IMPORT_RELEASE_CATALOG failed, skip testing to avoid run out of available catalog");
        string startTime = await GetMonTime();
        await LogIn(userName_C, password_C);
        await HomeDash("b");
        await FilterSup(intCatSup_C);

        var blocId = await FindCatalog(intCatSup_C);
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
        for (int i = 0; i < rowCnt; i++)
        {
            string action = await resultList.Locator("tbody").Locator("tr").Nth(i).Locator("td").Nth(8).InnerTextAsync();
            if (action.Contains("Restore version"))
            {
                lastRestore = i;
            }
        }
        //Restore the earliest restorable catalog now
        await resultList.Locator("tbody").Locator("tr").Nth(lastRestore).Locator("td").Nth(8).Locator("a").ClickAsync();
        await LoNetDom();
        await DelayS(5);
        //User is redirected to monitor page already
        CMProcess[] archive =
            [
                new CMProcess("", "Archive job", startTime, intCatSup_C, custName_C, "Finished OK")
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, archive);
        //Go back home
        await HomeDash("b");
        await tp.Locator("//*[@id=\"uiSupplierName\"]").FillAsync(intCatSup_C);
        await tp.Locator("//*[@id=\"uiSearchCatalogs\"]").ClickAsync();
        await LoNetDom();
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
        Assert.That(restoredAction, Does.Contain("Show"), "Expect to have 'Show' link but not!");
        string resVer = "";
        switch (ENVIRONMENT.ToLower())
        {
            case "qa":
                resVer = "Release version into production"; break;
            case "prod":
                resVer = "Release Version into production"; break;
            default:
                throw new ArgumentException("Wrong environment");
        }
        Assert.That(restoredAction, Does.Contain(resVer), "Expect to have 'Release version into production' but not!");
        //Perform restoration
        tp.Dialog += async (_, dialog) =>
        {
            Console.WriteLine($"Dialog type: {dialog.Type}");
            Console.WriteLine($"Dialog message: {dialog.Message}");

            await dialog.AcceptAsync();
        };
        await resultList.Locator("tbody").Locator("tr").Nth(lastRestore).GetByText(resVer).ClickAsync();
        CMProcess[] restoreLive =
            [
                new CMProcess("", "Set-Live Restored Version", startTime, intCatSup_C, custName_C, "Finished OK")
            ];
        await MonProcesses(CMB_CATALOG_MONITOR, restoreLive);
        //Access search and make sure the internal catalog item is not found
        Console.WriteLine("Wait 1 min before check on search");
        await DelayS(60);
        await tp.GotoAsync(viewURL_C);
        await LoNetDom();
        await DelayS(5);
        await tp.Locator("//*[@id=\"termAuto\"]").FillAsync(testDate);
        await DelayMS(500);
        await tp.Locator("//*[@id=\"termAuto\"]").PressAsync("Enter");
        await LoNetDom();
        await DelayS(2);
        //It could either be No resuls found or 1 similar result
        int noResultCount = await tp.Locator("//*[@id=\"results\"]").GetByText(testDateTime, new() { Exact = true }).CountAsync();
        if (!debugMode)
        {
            Assert.That(noResultCount == 0, $"Expect to have no result contains {testDateTime} but get {noResultCount}");
        }
        else
        {
            Console.WriteLine($"No result count: {noResultCount}");
        }

    }

}
