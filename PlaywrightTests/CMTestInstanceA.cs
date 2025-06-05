using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaywrightTests;

internal class CMTestInstanceA
{
    [Test, Order(1)]
    [Category("CMS FTP import")]
    async public Task TC01_268231_CMS_FTP_import()
    {
        await SingInPortal(PROTAL_MAIN_URL, SUPPLIER)
    }
}
