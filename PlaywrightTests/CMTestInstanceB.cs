using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        await LogIn(CMS_USRB, CMS_PWDB);
        await tp.GotoAsync(CMS_CATALOG_HOME);
        await CatchStackTrace();
        string[] CUST1File = new[] { TXT_FILE };
        string[] CUST1Type = new[] { "content" };
        await CMSUploadFile(CMS_CUSTB1_NAME, CUST1File, CUST1Type);

    }
}
