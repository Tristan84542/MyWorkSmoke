using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Math;
using NUnit.Framework;
namespace PlaywrightTests;

[Parallelizable(ParallelScope.Fixtures)]
[TestFixture]

internal class CMTestInstanceA : CMom
{
    [OneTimeSetUp]
    public void InstanceAOTS()
    {
        ENVIRONMENT = TestContext.Parameters.Get("Environment", "QA");
        browserName = TestContext.Parameters.Get("BrowserName", "chromium");
        headless = TestContext.Parameters.Get("Headless", false);
        channel = TestContext.Parameters.Get("Channel", "chrome");
        debugMode = TestContext.Parameters.Get("DebugMode", false);
        //Initialize CMParams
        InitParam(ENVIRONMENT);
        string[] files = Directory.GetFiles(".", "*.flag");
        foreach (string file in files)
        {
            File.Delete(file);
        }
        
        //File.Delete("TC268234_Passed.flag");
        //File.Delete("TC268234_Done.flag");
        //File.Delete("TC268236_Done.flag");
        Thread.Sleep(10000);
    }
    //Parallel Test instance specific for FTP upload test
    [Test, Order(1)]
    [Category("CMS FTP import")]
    async public Task TC01_268231_CMS_FTP_import()
    {
        
    }
}
