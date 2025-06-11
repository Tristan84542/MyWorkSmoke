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
    public static string PORTAL_URL; //(https://portal.hubwoo.com for prod)
    public static string PORTAL_LOGIN;
    public static string PORTAL_LOGOUT;
    public static string PORTAL_MAIN;
    public static string CMS_CATALOG_HOME;
    public static string CMS_CATALOG_MONITOR;
    public static string CMB_CATALOG_HOME;
    public static string CMB_CATALOG_MONITOR;

	public static string CMS_USRA; //cms user for instance A
	public static string CMS_PWDA; //cms pwd for instance A
	public static string CMS_USRB; //cms user for instance B
    public static string CMS_PWDB; //cms pwd for instance B
    public static string CMS_SUPB_NAME;
    public static string CMS_CUSTB1_NAME;
    public static string CMS_CUSTB2_NAME;
    public static string CMS_CUSTB1_ID;
    public static string CMS_CUSTB2_ID;
	public static string CMS_USRC; //cms user for instance C
	public static string CMS_PWDC; //cms pwd for instance C

	public static string FTP_USR; //ftp login username
    public static string FTP_PWD; //ftp login pwd
    public static string FILE_PATH;
    public static string DL_PATH;
    public static string TXT_FILE;
    public static string XLS_FILE;

    public static void InitParam(string Environment)
    {
        string dir = Directory.GetCurrentDirectory();
        string subFolderRoot = "";
        int start = dir.IndexOf("\\PlaywrightTests");
        if (start > 0)
        {
            subFolderRoot = dir.Substring(0, start);
        }
        if (Environment == "QA")
        {
            PORTAL_URL = "https://portal.qa.hubwoo.com";
            FILE_PATH = System.IO.Path.Combine(subFolderRoot, @"PlaywrightTests\CMB\QA\");
            DL_PATH = System.IO.Path.Combine(dir, @"QATESTRESULTS\CMB\");
            CMS_USRA = "SVS1";
			CMS_PWDA = "Xsw23edc!";
            CMS_USRB = CMS_USRA;
            CMS_PWDB = CMS_PWDA;
			CMS_USRC = CMS_USRA;
			CMS_PWDC = CMS_PWDA;
            CMS_SUPB_NAME = "SV Supplier 1";
            CMS_CUSTB1_NAME = "eCat CM buyer QA1";
            CMS_CUSTB2_NAME = "eCat CM buyer QA 2";
            TXT_FILE = "QA_txt_scf_catalog.zip";
            XLS_FILE = "QA_xls_scf_catalog.xls";

        }
        else if (Environment == "PROD")
        {
            PORTAL_URL = "https://portal.hubwoo.com";
            FILE_PATH = System.IO.Path.Combine(subFolderRoot, @"PlaywrightTests\CMB\PROD\");
            DL_PATH = System.IO.Path.Combine(dir, @"PRODTESTRESULTS\CMB\");
            CMS_USRA = "EPAM_TS1";
			CMS_PWDA = "Xsw23edc!";
			CMS_USRB = CMS_USRA;
			CMS_PWDB = CMS_PWDA;
			CMS_USRC = CMS_USRA;
			CMS_PWDC = CMS_PWDA;
			FTP_USR = "anilava-epamusr01";
            FTP_PWD = "z1mYS2GX62!";


        }
        PORTAL_LOGIN = PORTAL_URL + "/auth/login?ReturnUrl=%2Fmain%2F";
        PORTAL_LOGOUT = PORTAL_URL + "/srvs/login/logout";
        PORTAL_MAIN = PORTAL_URL + "/main/";
        CMS_CATALOG_HOME = PORTAL_URL + "/srvs/CatalogManager/";
        CMS_CATALOG_MONITOR = PORTAL_URL + "/srvs/CatalogManager/monitor/MonitorSupplier";
        CMB_CATALOG_HOME = PORTAL_URL + "/srvs/BuyerCatalogs";
        CMB_CATALOG_MONITOR = PORTAL_URL + "/srvs/BuyerCatalogs/monitor/MonitorBuyer";
    }

}