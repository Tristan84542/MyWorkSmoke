using System;
using System.Collections.Generic;
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
        CMCoordinator.WaitForStage(2);
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
}
