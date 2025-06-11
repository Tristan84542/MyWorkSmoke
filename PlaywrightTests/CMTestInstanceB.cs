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
    [Test, Order(1)]
    [Category("CMS Test")]
    public async Task TC02_268232_CMS_UI_IMPORT_FLAT_SCF()
    {
        string startTime = await getMonTime();
        await LogIn(CMS_USRB, CMS_PWDB);
        await tp.GotoAsync(CMS_CATALOG_HOME, new() { Timeout = 60000 });
        await CatchStackTrace();
        string[] CUST1File = [TXT_FILE];
        string[] CUST1Type = ["content"];
        await CMSUploadFile(CMS_CUSTB1_NAME, CUST1File, CUST1Type);
        string[] CUST2File  = [XLS_FILE];
        string[] CUST2Type = ["content"];
        await CMSUploadFile(CMS_CUSTB2_NAME, CUST2File, CUST2Type);
        CMProcess[] fSCFImport =
        [
            new CMProcess("", "Simple Catalog import", startTime, CMS_SUPB_NAME, CMS_CUSTB1_NAME, "Finished OK"),
            new CMProcess("", "Simple Catalog import", startTime, CMS_SUPB_NAME, CMS_CUSTB2_NAME, "Finished OK")
        ];
        await MonProcesses(CMS_CATALOG_MONITOR, fSCFImport);
    }
}
