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


namespace PlaywrightTests;

[Parallelizable(ParallelScope.Fixtures)]
[TestFixture]

internal class CMTestInstanceC : CMom
{
    private static bool crsPassed = true;
    [OneTimeSetUp]
    public void InstanceCOTS()
    {
        //CMCoordinator.WaitForStage(3);
        const string path = "TC267234_Passed.flag";
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
        crsPassed = false;
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
        string intCatSup;
        string custName;
        string userName = "";
        string password = "";
        string viewURL;
        string fileName = "Catalog_scf_IntCatalog.xlsx";

        if (environment == "QA")
        {
            intCatSup = "LenaSupplier1";
            custName = "SV Buyer";
            userName = "SVB-0001ba";
            password = "Xsw23edc!";
            viewURL = "https://search.qa.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=SVVIEW1&VIEW_PASSWD=q0E2Aft3PQy18&USER_ID=SV&BRANDING=search5&LANGUAGE=EN&HOOK_URL=https://portal.qa.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp&ADMIN=1&COUNTRY=GB&EASYORDER=1";

        }
        else if (environment == "PROD")
        {
            intCatSup = "TESTSUPCDO9";
            custName = "TESTCUSTCDO 1";
            userName = "EPAM_TC-0001";
            password = "xsw23edc";
            viewURL = "https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE05-05&VIEW_PASSWD=t3S4TcKp89Rqy&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp";
        }
        else
        {
            throw new Exception("Check runner Environment value");
        }

        string startTime = await GetMonTime();
        await LogIn(userName, password);
        await HomeDash("b");
        var blocId = await FindCatalog(intCatSup);
        var metaId = await GetMetaId(blocId);
        var blocLoc = tp.Locator($"id={blocId}");//CSS selector

    }
}
