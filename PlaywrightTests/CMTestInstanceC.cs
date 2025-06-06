using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;


namespace PlaywrightTests;

[Parallelizable(ParallelScope.Fixtures)]
[TestFixture]

internal class CMTestInstanceC : CMom
{
    [Test, Order(1)]
    [Category("CMS Test")]
    public async Task TC02_268232_CMS_UI_IMPORT_FLAT_SCF()
    {
        await LogIn(CMS_USRA, CMS_PWDA);
        await tp.GotoAsync(CMS_CATALOG_HOME);

    }
}
