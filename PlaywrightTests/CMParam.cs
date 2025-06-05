using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace PlaywrightTests;

public class CMParam
{
    string Environment = "QA"; //(QA|UAT|PROD)
    public string PORTAL_URL; //(https://portal.hubwoo.com for prod)
    public string PORTAL_LOGIN;
    public string PORTAL_LOGOUT;
    public string PORTAL_MAIN;
    public string CMS_CATALOG_HOME;
    public string CMS_CATALOG_MONITOR;
    public string CMB_CATALOG_HOME;
    public string CMB_CATALOG_MONITOR;
    public string FTP_SUP_USR;
    public string FTP_SUP_PWD;
    public string FTP_USR;
    public string FTP_PWD;
}

public InitParam(string Environment)
    {
        if (Environment == "QA")
        {
            PORTAL_URL = "https://portal.hubwoo.com";
            FTP_SUP_USR = "SVS1";
            FTP_SUP_PWD = "Xsw23edc!";
        } 
        else if (Environment == "PROD")
        {
            PORTAL_URL = "https://portal.hubwoo.com";
            FTP_SUP_USR = "EPAM_TS1";
            FTP_SUP_PWD = "xsw23edc";
            FTP_USR = 

        }
        PORTAL_LOGIN = PORTAL_URL + "/auth/login?ReturnUrl=%2Fmain%2F";
        PORTAL_LOGOUT = PORTAL_URL + "/srvs/login/logout";
        PORTAL_MAIN = PORTAL_URL + "/main/";
        CMS_CATALOG_HOME = PORTAL_URL + "/srvs/CatalogManager/";
        CMS_CATALOG_MONITOR = PORTAL_URL + "/srvs/CatalogManager/monitor/MonitorSupplier"; 
        CMB_CATALOG_HOME = PORTAL_URL + "/srvs/BuyerCatalogs";
        CMB_CATALOG_MONITOR = PORTAL_URL + "/srvs/BuyerCatalogs/monitor/MonitorBuyer";
    }

