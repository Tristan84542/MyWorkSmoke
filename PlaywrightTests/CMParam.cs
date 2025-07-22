using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using UglyToad.PdfPig.Tokens;

namespace PlaywrightTests;

public class CMParam
{
    //Test runner settings param
    public static bool debugMode;
    public static string browserName;
    public static string channel;
    public static int dfTimeout;
    public static string ENVIRONMENT;
    public static bool headless;
    public static volatile bool INITDONE = false;

    //One time setup param
    public static int currentStage = 0;

    public static string testDate;
    public static string testDateTime;
    public static string PORTAL_URL; //(https://portal.hubwoo.com for prod)
    public static string PORTAL_LOGIN;
    public static string PORTAL_LOGOUT;
    public static string PORTAL_MAIN;
    public static string CMS_CATALOG_HOME;
    public static string CMS_CATALOG_MONITOR;
    public static string CMB_CATALOG_HOME;
    public static string CMB_CATALOG_MONITOR;
    public static string CMB_CATALOG_DL;
    public static string CMB_CATALOG_RPT;
    public static string CMB_CATALOG_EDITUSER;
    public static string CMB_CUST_LANDING;
    public static string CMB_DATAGPUA;

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
    public static string intCatSup_C;
    public static string custName_C;
    public static string userName_C = "";
    public static string password_C = "";
    public static string viewURL_C;
    public static string fileName_C = "Catalog_scf_IntCatalog.xlsx";


    public static string CMS_B_TXT_CUSTID;
    public static string CMS_B_XLS_CUSTID;
    public static string CMS_B_XLSX_CUSTID;

    public static string CMB_USRB;
    public static string CMB_PWDB;
    
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
        testDateTime = DateTime.Now.ToString("yyyyMMddHHmm");

        FILE_PATH = System.IO.Path.Combine(subFolderRoot, $@"PlaywrightTests\CMB\{Environment}\");
        DL_PATH = System.IO.Path.Combine(dir, $@"RESULT\{Environment}\{testDate}\");
        TXT_FILE = "Catalog_scf_TXT.zip";
        XLS_FILE = "Catalog_scf_XLS.xls";
        XLSX_FILE = "Catalog_scf_XLSX.xlsx";
        ATTACHMENT_FILE = "baseAttachmentUpload.zip";
        CRS_FILE = "Catalog_scf_CRS.xlsx";

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
            CMB_USRB = "SVB-0001ba";
            CMB_PWDB = "Xsw23edc!";
            intCatSup_C = "LenaSupplier1";
            custName_C = "SV Buyer";
            userName_C = "SVB-0001ba";
            password_C = "Xsw23edc!";
            viewURL_C = "https://search.qa.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=SVVIEW1&VIEW_PASSWD=q0E2Aft3PQy18&USER_ID=SV&BRANDING=search5&LANGUAGE=EN&HOOK_URL=https://portal.qa.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp&ADMIN=1&COUNTRY=GB&EASYORDER=1";
            CMom.UpdateExcel(fileName_C, "Data 1", "F3", $"Smoke Internal Catalog 001 {testDateTime}");

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
            CMS_B_SUP_NAME = "TESTSUPCDO2";
            CMS_B_TXT_CUSTNAME = "TESTCUSTCDO 6 Customer Classification";
            CMS_B_XLS_CUSTNAME = "TESTCUSTCDO 7";
            CMS_B_XLSX_CUSTNAME = "TESTCUSTCDO 1";
            CMS_C_CUSTNAME = CMS_B_XLSX_CUSTNAME;
            CMS_C_SUP_NAME = CMS_B_SUP_NAME;
            CMB_USRB = "RegUserB";
            CMB_PWDB = "RegUserB1!";
            intCatSup_C = "TESTSUPCDO9";
            custName_C = "TESTCUSTCDO 1";
            userName_C = "RegUserC";
            password_C = "RegUserC1!";
            viewURL_C = "https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE05-05&VIEW_PASSWD=t3S4TcKp89Rqy&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp";
            CMom.UpdateExcel(fileName_C, "Data 1", "C3", $"Smoke Internal Catalog 001 {testDateTime}");


        }
        PORTAL_LOGIN = PORTAL_URL + "/auth/login?ReturnUrl=%2Fmain%2F";
        PORTAL_LOGOUT = PORTAL_URL + "/srvs/login/logout";
        PORTAL_MAIN = PORTAL_URL + "/main/";
        CMS_CATALOG_HOME = PORTAL_URL + "/srvs/CatalogManager/";
        CMS_CATALOG_MONITOR = PORTAL_URL + "/srvs/CatalogManager/monitor/MonitorSupplier";
        CMB_CATALOG_HOME = PORTAL_URL + "/srvs/BuyerCatalogs";
        CMB_CATALOG_MONITOR = PORTAL_URL + "/srvs/BuyerCatalogs/monitor/MonitorBuyer";
        CMB_CATALOG_DL = PORTAL_URL + "/srvs/BuyerCatalogs/export/index";
        CMB_CATALOG_RPT = PORTAL_URL + "/srvs/BuyerCatalogs/reporting/index";
        CMB_CATALOG_EDITUSER = PORTAL_URL + "/srvs/omnicontent/BuyerManageUsers.aspx";
        CMB_CUST_LANDING = PORTAL_URL + "/srvs/BuyerCatalogs/admin/LandingPage";
        CMB_DATAGPUA = PORTAL_URL + "/srvs/BuyerCatalogs/admin/DataGroupUserAssignment";

    }

}