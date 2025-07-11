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
        string[] files = Directory.GetFiles(".", "*.flag");
        foreach (string file in files)
        {
            File.Delete(file);
        }

        ENVIRONMENT = TestContext.Parameters.Get("Environment", "QA");
        browserName = TestContext.Parameters.Get("BrowserName", "chromium");
        channel = TestContext.Parameters.Get("Channel", "chrome");
        //string tempHead = TestContext.Parameters.Get("Headless", "false");
        //string tempDebug = TestContext.Parameters.Get("DebugMode", "xyz");
        headless = TestContext.Parameters.Get("Headless", false);
        debugMode = TestContext.Parameters.Get("DebugMode", false);

        //switch (tempHead)
        //{
        //    case "true": headless = true; break;
        //    case "false": headless = false; break;
        //}
        //switch (tempDebug)
        //{
        //    case "true": debugMode = true;break;
        //    case "false": debugMode = false;break;
        //        default:
        //        throw new ArgumentException("Wrong DebugMode value");
        //}
        
        //Initialize CMParams
        InitParam(ENVIRONMENT);
        
        Thread.Sleep(1000);
        INITDONE = true;
    }
    //Parallel Test instance specific for FTP upload test
    [Test, Order(1)]
    [Category("CMS FTP import")]
    async public Task TC01_268231_CMS_FTP_import()
    {
        
    }
}
