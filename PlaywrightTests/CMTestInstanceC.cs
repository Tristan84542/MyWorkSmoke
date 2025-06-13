using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2021.Excel.RichDataWebImage;
using Microsoft.Playwright;
using NUnit.Framework;


namespace PlaywrightTests;

[Parallelizable(ParallelScope.Fixtures)]
[TestFixture]

internal class CMTestInstanceC : CMom
{
    private static bool crsPassed = false;
    [OneTimeSetUp]
    public void InstanceCOTS()
    {
        //CMCoordinator.WaitForStage(3);
        const string path = "TC267234_Passed.flag";
        int waited = 0;
        while (false)// (!File.Exists(path) && waited < 20)
        {
            Thread.Sleep(60000);
            waited++;
        }
        if (!File.Exists(path))
        {
           // throw new Exception("TC268234 is not completed within 20 min");
        }
        //CMCoordinator.StageDone();
    }

    [Test, Order(1)]
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
            if (dlItems[i].Contains("SCF Export") && IsLater(dlTime, dlTimes[i]))
            {
                var waitForDownload = tp.WaitForDownloadAsync();
                await tp.GetByText("SCF Export").Nth(i).ClickAsync();
                var download = await waitForDownload;
                var fileName = DL_PATH + "TC268233_CMS_CATALOG_DOWNLOAD.zip";
                Console.WriteLine("Filed download as " + fileName);
                await download.SaveAsAsync(fileName);
                break;
            }
        }
    }

    [Test, Order(2)]
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

    [Test, Order(3)]
    [Category ("CMS Test")]
    public async Task TC268235_CMS_DIFFINGREPORT()
    {
        Assume.That(crsPassed, "SKip test because TC268236_CMS_CHECKROUTINE not pass");


    }
}
