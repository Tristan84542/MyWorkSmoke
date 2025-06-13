using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
namespace PlaywrightTests;

[Parallelizable(ParallelScope.Fixtures)]
[TestFixture]

internal class CMTestInstanceA : CMom
{
    [OneTimeSetUp]
    public void InstanceAOTS()
    {
        CMCoordinator.WaitForStage(1);
        //... setup code here
        CMCoordinator.StageDone();
    }
    //Parallel Test instance specific for FTP upload test
    [Test, Order(1)]
    [Category("CMS FTP import")]
    async public Task TC01_268231_CMS_FTP_import()
    {
        
    }
}
