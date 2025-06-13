using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using UglyToad.PdfPig.Tokens;

namespace PlaywrightTests;

public class CMParam
{
    public static string testDate;
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
    public static string CMS_B_SUP_NAME;
    public static string CMS_B_TXT_CUSTNAME;
    public static string CMS_B_XLS_CUSTNAME;
    public static string CMS_B_XLSX_CUSTNAME;
    public static string CMS_C_CUSTNAME;
    public static string CMS_C_SUP_NAME;

    public static string CMS_B_TXT_CUSTID;
    public static string CMS_B_XLS_CUSTID;
    public static string CMS_B_XLSX_CUSTID;

    
    public static string CMS_USRC; //cms user for instance C
	public static string CMS_PWDC; //cms pwd for instance C

	public static string FTP_USR; //ftp login username
    public static string FTP_PWD; //ftp login pwd
    public static string FILE_PATH;
    public static string DL_PATH;
    public static string TXT_FILE;
    public static string XLS_FILE;
    public static string XLSX_FILE;
    public static string CRS_FILE;
    public static string ATTACHMENT_FILE;

    public static void InitParam(string Environment)
    {
        string dir = Directory.GetCurrentDirectory();
        string subFolderRoot = "";
        int start = dir.IndexOf("\\PlaywrightTests");
        if (start > 0)
        {
            subFolderRoot = dir.Substring(0, start);
        }
        testDate = DateTime.Today.ToString("yyyyMMdd");
        if (Environment == "QA")
        {
            PORTAL_URL = "https://portal.qa.hubwoo.com";
            CMS_USRA = "SVS1";
			CMS_PWDA = "Xsw23edc!";
            CMS_USRB = CMS_USRA;
            CMS_PWDB = CMS_PWDA;
			CMS_USRC = CMS_USRA;
			CMS_PWDC = CMS_PWDA;
            CMS_B_SUP_NAME = "SV Supplier 1";
            CMS_B_TXT_CUSTNAME = "eCat CM buyer QA1";
            CMS_B_XLS_CUSTNAME = "eCat CM buyer QA 2";
            CMS_B_XLSX_CUSTNAME = "SV Buyer";
            CMS_C_CUSTNAME = CMS_B_XLSX_CUSTNAME;
            CMS_C_SUP_NAME = CMS_B_SUP_NAME;

        }
        else if (Environment == "PROD")
        {
            PORTAL_URL = "https://portal.hubwoo.com";
            CMS_USRA = "EPAM_TS1";
			CMS_PWDA = "xsw23edc";
			CMS_USRB = "EPAM_TS2";
			CMS_PWDB = "xsw23edc";
			CMS_USRC = CMS_USRB;
			CMS_PWDC = CMS_PWDB;
			FTP_USR = "anilava-epamusr01";
            FTP_PWD = "z1mYS2GX62!";


        }
        FILE_PATH = System.IO.Path.Combine(subFolderRoot, $@"PlaywrightTests\CMB\{Environment}\");
        DL_PATH = System.IO.Path.Combine(dir, $@"RESULT\{Environment}\{testDate}\");
        TXT_FILE = "Catalog_scf_TXT.zip";
        XLS_FILE = "Catalog_scf_XLS.xls";
        XLSX_FILE = "Catalog_scf_XLSX.xlsx";
        ATTACHMENT_FILE = "baseAttachmentUpload.zip";
        CRS_FILE = "Catalog_scf_CRS.xlsx";

        PORTAL_LOGIN = PORTAL_URL + "/auth/login?ReturnUrl=%2Fmain%2F";
        PORTAL_LOGOUT = PORTAL_URL + "/srvs/login/logout";
        PORTAL_MAIN = PORTAL_URL + "/main/";
        CMS_CATALOG_HOME = PORTAL_URL + "/srvs/CatalogManager/";
        CMS_CATALOG_MONITOR = PORTAL_URL + "/srvs/CatalogManager/monitor/MonitorSupplier";
        CMB_CATALOG_HOME = PORTAL_URL + "/srvs/BuyerCatalogs";
        CMB_CATALOG_MONITOR = PORTAL_URL + "/srvs/BuyerCatalogs/monitor/MonitorBuyer";
    }

}