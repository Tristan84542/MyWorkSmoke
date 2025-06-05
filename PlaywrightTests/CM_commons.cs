using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PlaywrightTests;

public class Commons : PageTest
{
    public async Task LogIn(string username, string password)
    {
        await LogIn(PORTAL_MAIN_URL, username, password);
    }
    public async Task LogIn(string portal, string username, string password)
    {
        await Page.GoToAsync(PORTAL_LOGIN);
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }
}
