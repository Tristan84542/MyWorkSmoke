
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using FluentAssertions.Primitives;
using Google.Apis.Gmail.v1;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using System.Diagnostics.Metrics;
using System.Dynamic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using TinyCsvParser;
using TinyCsvParser.Mapping;
using UglyToad.PdfPig.Content;

namespace PlaywrightTests;

public class csvClassification
{
	public string ClassificationCode { get; set; }

	public string Classification { get; set; }

	public int Count { get; set; }
}

public static partial class QaSupplierItemIdValidator
{
	//see https://stevetalkscode.co.uk/regex-source-generator
	[GeneratedRegex("11-015[.]5000|11-015[.]9025")]
	private static partial Regex QaSupplierItemIdRegex();

	public static bool IsExpectedQASupplierItemNumber(string itemId) => QaSupplierItemIdRegex().IsMatch(itemId);
}

public static partial class ProdSupplierItemIdValidator
{
	[GeneratedRegex("01-081[.]9010|01-655[.]1000|02-570[.]1000|02-570[.]9020|10-020[.]5000|10-020[.]5001|11-015[.]5000|11-015[.]9025")]
	private static partial Regex ProdSupplierItemIdRegex();

	public static bool IsExpectedProdSupplierItemNumber(string itemId) => ProdSupplierItemIdRegex().IsMatch(itemId);
}

public class CsvClassificationMapping : CsvMapping<csvClassification>
{
	//see https://github.com/TinyCsvParser/TinyCsvParser
	public CsvClassificationMapping()
					: base()
	{
		MapProperty(0, x => x.ClassificationCode);
		MapProperty(1, x => x.Classification);
		MapProperty(2, x => x.Count);
	}
}

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public partial class CMS_CMB_Tests : PageTest
{
	////////////////////////////////////////////////////////////////////////////

	string Environment = "PROD";  //QA  / UAT  / PROD    SET TESTING ENVIRONMENT HERE, NOTE HAS NOT BEEN TESTED ON UAT AND PARAMETERS NOT FULLY IMPLEMENTED FOR QA 

	////////////////////////////////////////////////////////////////////////////

	// before running, unless you require headless mode, you may need to require to configure access to the runsettings file:
	//in test explorer
	//click the down arrow on the settings cog icon
	//select the configure Run settings option from the context menu
	//enable the auto detect run settings file option
	//then select the select solution wide run settings file and configure it to point to the file ..\catalog-manager\PlaywrightTests\PlayWright.runsettings

	//The email tests in this class , assume that all emails are for a variant of the email address omnicontenttest@gmail.com, the omnicontenttest@gmail.com google account
	//has a project configured in the google cloud/developer console with an Oauth client configured and the gmail api enabled.
	//The credentials required by the gmail api service are located in the Credentials.json file located in the bin folder
	//(..\catalog-manager\PlaywrightTests\PlaywrightTests\bin\Debug\net8.0) for this project

	/*
 to begin codegen in vs terminal
PS C:...\catalog-manager\PlaywrightTests> cd PlaywrightTests
PS C:...\catalog-manager\PlaywrightTests> cd PlaywrightTests
PS C:....\catalog-manager\PlaywrightTests\PlaywrightTests> cd bin\debug\net8.0
PS C:...\catalog-manager\PlaywrightTests\PlaywrightTests\bin\debug\net8.0> pwsh playwright.ps1 codegen
may also need to execute the following line to install browsers if first time running codegen:
pwsh playwright.ps1 install
*/

	string _browserName = "";
	static string[] Scopes = { GmailService.Scope.GmailReadonly };//scopes for gmail api
	static string ApplicationName = "emailtester"; // Gmail API .NET Quickstart
	string testStartSecondsSinceEpoch = "";
	string testStarted = "";//stores time the entire test suite was started
	string downloadPath = "";

	string PORTAL_LOGIN = "https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F";
	string PORTAL_LOGOUT = "https://portal.hubwoo.com/srvs/login/logout";
	string PORTAL_MAIN_URL = "https://portal.hubwoo.com/main/";
	string CMA_ADMIN_COMPANY_FIND_URL = "https://portal.hubwoo.com/srvs/Contentadmin/AdminCompanyFind2007.aspx";
	string QQS_OFFER_DETAIL_URL = " https://portal.qa.hubwoo.com/srvs/easyorder/OfferDetail2007.aspx";
	string QQB_REQUEST_LIST_URL = "https://portal.hubwoo.com/srvs/easyorder/RequestList2007.aspx";
	string QQS_REQUEST_LIST_URL = "https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx";
	string ADMIN_RELATIONEDIT_REGEX = "^https://portal.hubwoo.com/srvs/Contentadmin/AdminRelationEdit2007.aspx";
	string ADMIN_EO_PROPERTIES_EDIT_REGEX = "^https://portal.hubwoo.com/srvs/ContentAdmin/AdminEOPropertiesEdit.aspx";
	string CUSTOM_FORM_EDIT_REGEX = "^https://portal.hubwoo.com/srvs/easyorder/CustomForms.aspx";
	string QQS_OFFER_DETAIL_URL_REGEX = "";
	string CMS_CATALOG_HOME_URL = "https://portal.hubwoo.com/srvs/CatalogManager/";
	string CMS_MONITOR_URL = "https://portal.hubwoo.com/srvs/CatalogManager/monitor/MonitorSupplier";
	string CMB_MONITOR_URL = "https://portal.hubwoo.com/srvs/BuyerCatalogs/monitor/MonitorBuyer";
	string CMB_CATALOG_HOME_URL1 = "https://portal.hubwoo.com/srvs/BuyerCatalogs";
	string CMB_CATALOG_HOME_URL = "https://portal.hubwoo.com/srvs/BuyerCatalogs/";
	string CONTENTADMIN_LOGIN = "";
	string CONTENTADMIN_PASSWORD = "";
	string SUPPLIER_USER1_LOGIN = "";
	string SUPPLIER_USER1_PASSWORD = "";
	string BUYER_USER1_LOGIN = "";
	string BUYER_USER1_PASSWORD = "";

	string CMS_MONITOR_PROCESS_FILTER_SIMPLE_CATALOG_IMPORT = "79";// Simple Catalog Import 79
	string CMS_MONITOR_PROCESS_FILTER_TEMPLATE_EXPORT = "31";// Template Export 31
	string CMS_MONITOR_PROCESS_FILTER_RELEASE_CATALOG = "80";//80 Release catalog

	/*
 CMS uiProcessType options
	<option selected="selected" value="all">All Types</option>
	<option value="88">Attachment processing</option>
	<option value="85">BMECat file upload</option>
	<option value="80">Release catalog</option>
	<option value="79">Simple Catalog import</option>
	<option value="70">TLC file upload</option>
	<option value="31">Template Export</option>
	<option value="194">Attachments Export</option>
	<option value="29">Excel import</option>
	<option value="141">Smart form importer</option>
*/

	string CMB_MONITOR_PROCESS_FILTER_ARCHIVE = "110"; //added in version 24.3
	string CMB_MONITOR_PROCESS_FILTER_RELEASE_CATALOG = "80";//80 Release catalog added in version 24.3
	string CMB_MONITOR_PROCESS_FILTER_SET_LIVE = "52";//
	string CMB_MONITOR_PROCESS_FILTER_LOAD_CATALOG = "86";//
	string CMS_MONITOR_PROCESS_FILTER_ATTACHMENT_PROCESSING = "88";
	string CMB_MONITOR_PROCESS_FILTER_ENRICHMENT = "111";

	/* CMB uiProcessType options
	<option selected="selected" value="all">All Types</option>
<option selected="selected" value="110">Archive</option> -- added in 24.3
	<option value="88">Attachment processing</option>
	<option value="25">Catalog duplication</option>
	<option value="54">Classification Import</option>
	<option value="23">Enrichment import</option>
	<option value="111">Enrichment</option>
	<option value="29">Excel import</option>
	<option value="86">Load Catalog</option>
	<option value="163">Multikey Enrichment Import</option>
	<option value="6">Notification</option>
	<option value="20">Revalidate catalog</option>
	<option value="52">Set Live</option>
	<option value="79">Simple Catalog import</option>
	<option value="31">Template Export</option>
	<option value="34">XREF Implementation</option>
	<option value="165">Search User Login Report</option>
	<option value="154">Search Shopping Cart Transfers</option>
	<option value="152">Price Change Summary</option>
 */
	/*
 *options for the reviews items column set ddl
 <select class="form-control" id="uiColumnSet" onchange="changeColumnSet()">
<option value="default">Default</option>
<option value="1365">Price-Differences</option>
<option selected="selected" value="1544">Enrichment</option>
</select>
 */
	string CMB_REVIEW_ITEMS_COLUMNS_SET_ENRICHMENT = "1544";

	string TC01_CATALOG_FILE_PATH = "";
	string TCO1_CATALOG_SELECTOR = "";
	string TCO1_CATALOG_SELECTOR_ID = "";
	string TC01_CATALOG_ID = "";
	string TC01_CUSTOMER_ID = "";
	string TC01_CUSTOMERNAME = "";
	int TC01_CATALOG_LOAD_ATTEMPTS = 20; //how many loops to attempt before excepting a failed catalog upload/release
	int MONITOR_CHECK_ATTEMPTS = 60;
	string TC01_CATALOG_METACATID = "";

	string TC04_CATALOG_SELECTOR = "\\37 7418_";
	string TC04_CATALOG_SELECTOR_ID = "";
	string TC04_SUPPLIERNAME = "TESTSUPCDO2";
	string TC04_SUPPLIER_ID = "";
	string TC04_SUPPLIER_METACATID = "77418";
	string TC04_APPROVE_ITEMS_REGEX = "^https://portal.hubwoo.com/srvs/BuyerCatalogs/items/item-list?show=";
	string TC05_CATALOG_SELECTOR_ID = "";

	string BUYER_ADMIN_HOME = "";
	string BUYER_ADMIN_LANDING_PAGE_URL = "";
	string TC10_SELECTED_VIEW = "";
	string SEARCH_CONFIGURE_LANDING_URL = "";
	string BUYER_ADMIN_EDIT_USERS = "";
	string BUYER_ADMIN_EDIT_PROFILE_URL = "https://portal.hubwoo.com/main/Admin/MyProfile?takeTo=https:%2F%2Fportal.hubwoo.com%2Fsrvs%2FDefault.aspx";
	string NEW_USERLOGIN = "";
	string BUYER_ADMIN_CREATE_USER_URL = "https://portal.hubwoo.com/srvs/omnicontent/BuyerAdminCreateUser.aspx";
	string BUYER_ADMIN_DOWNLOAD_URL = "https://portal.hubwoo.com/srvs/BuyerCatalogs/export/index";
	string BUYER_ADMIN_REPORTING_URL = "https://portal.hubwoo.com/srvs/BuyerCatalogs/reporting/index";
	string DOWNLOAD_REPORT_SUPPLIERNAME = "";
	string SHOW_HISTORY_SUPPLIERNAME = "";
	string ARCHIVE_CATALOG_SUPPLIER_ID = "";
	string CATALOG_RESTORE_VERSION = "";
	string DATA_GROUPS_USER_ASSIGNMENT_URL = "https://portal.hubwoo.com/srvs/BuyerCatalogs/admin/DataGroupUserAssignment";
	string DATAGROUP_NAME = "Prod test 1key enrichment";
	string TC16_DATAGROUP_ASSIGNMENT_REMOVE_USER = "";
	string CMB_MONITOR_PROCESS_FILTER_ENRICHMENT_IMPORT = "";
	string CMB_MONITOR_PROCESS_FILTER_MULTI_KEY_ENRICHMENT_IMPORT = "";//Multikey Enrichment Import 163 (prod)
	string ENRICHMENT_DATAGROUP_DOWNLOAD_NAME = "";  //TEST TC18
	string CMB_MONITOR_PROCESS_FILTER_TEMPLATE_EXPORT = "";//Template export 31 (prod)
	string UPLOAD_ENRICHMENT_FILE1 = "1key_enrich_template_new.xlsx";
	string UPLOAD_ENRICHMENT_FILE2 = "2key_multi_key_enrich_template.xlsx";
	string SUPPLIER_CHECK_ROUTINE_FILE = "";
	string CUSTOMER_CHECK_ROUTINE_FILE = "xlsx_prod_catalog_SCF_prod_file_base_checkroutine_customer.xlsx";
	string CMB_MONITOR_PROCESS_FILTER_SIMPLE_CATALOG_IMPORT = "";
	string CUSTOMER_UPLOAD_AND_RELEASE_CATALOG_FILE = "";
	string TC19_DASHBOARD_CATALOGID = "6 2376_77418";
	string TC19_ERROR_CORRECTION_VALUE = "32151201";
	string TC20_ERROR_CORRECTION_VALUE = "has long description";
	string CATALOG_IMPORT_WITH_ATTACHMENTS_FILE = "baseAttachmentUpload.zip";  //TC07
	string CATALOG_IMPORT_WITH_ATTACHMENTS_CATALOG = "xlsx_prod_catalog_SCF_prod_file_base_attachmentUpload.xlsx";
	string VIEW_AND_DOWNLOAD_DIFFING_REPORT = "xlsx_prod_catalog_SCF_prod_file_updated.xlsx";
	string EXECUTE_ENRICHMENT_CATALOG_FILE = "xlsx_prod_catalog_SCF_prod_file_base_enrichment.xlsx";
	string CMB_DIFFING_REPORT1 = "xlsx_prod_catalog_SCF_prod_file_base.xlsx";
	string CMB_DIFFING_REPORT2 = "xlsx_prod_catalog_SCF_prod_file_updated.xlsx";
	string CMB_DATA_GROUPS_USER_ASSIGNMENT = "https://portal.hubwoo.com/srvs/BuyerCatalogs/admin/DataGroupUserAssignment";
	int ONE_KEY_DATAGROUP_VERSION = 0;
	int TWO_KEY_DATAGROUP_VERSION = 0;
	string SEARCH_URL_RELEASE_TEST = "https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE05-05&VIEW_PASSWD=t3S4TcKp89Rqy&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp";
	int ATTACHMENT_UPLOAD_ATTEMPTS = 30;
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		//runs once before all tests start
		Console.WriteLine("OneTimeSetUp");
		TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
		testStarted = DateTime.Now.ToLongTimeString();
		int secondsSinceEpoch = (int)t.TotalSeconds;
		secondsSinceEpoch = secondsSinceEpoch - 600;
		//in some tests the epoch closely matches the time the email is being received, perhaps due to server differences. So lets subtract 10 minutes (600 seconds for safety)
		testStartSecondsSinceEpoch = secondsSinceEpoch.ToString();  //used to timeframe when email messages should be checked after
		if (Environment == "QA")
		{
			//will use buyer/supplier companies created by sviatlana
			/*
company: SV Buyer
company shortname : SVB-0001
login: SVB-0001ba 
password:Xsw23edc!  
catalog id SVB-0001 
metacatid: 63045  
companyid: 4045656

company:eCat SV Supplier 1
companyshortname : SVS1
login:	SVS1 
password: Xsw23edc!
catalogid:SVS1
metacatid:237593
companyid:4045657
*/
			CONTENTADMIN_LOGIN = "epamcontentadmin";
			CONTENTADMIN_PASSWORD = "password1";
			PORTAL_LOGIN = "https://portal.qa.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F";
			PORTAL_MAIN_URL = "https://portal.qa.hubwoo.com/main/";
			CMA_ADMIN_COMPANY_FIND_URL = "https://portal.qa.hubwoo.com/srvs/Contentadmin/AdminCompanyFind2007.aspx";
			PORTAL_LOGOUT = "https://portal.hubwoo.com/srvs/login/logout";
			QQB_REQUEST_LIST_URL = "https://portal.qa.hubwoo.com/srvs/easyorder/RequestList2007.aspx";
			QQS_REQUEST_LIST_URL = "https://portal.qa.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx";
			QQS_OFFER_DETAIL_URL = "https://portal.qa.hubwoo.com/srvs/easyorder/OfferDetail2007.aspx";
			QQS_OFFER_DETAIL_URL_REGEX = "^https://portal.qa.hubwoo.com/srvs/easyorder/OfferDetail2007.aspx";
			ADMIN_RELATIONEDIT_REGEX = "^https://portal.qa.hubwoo.com/srvs/Contentadmin/AdminRelationEdit2007.aspx";
			ADMIN_EO_PROPERTIES_EDIT_REGEX = "^https://portal.qa.hubwoo.com/srvs/ContentAdmin/AdminEOPropertiesEdit.aspx";
			CUSTOM_FORM_EDIT_REGEX = "^https://portal.qa.hubwoo.com/srvs/easyorder/CustomForms.aspx";
			CMS_CATALOG_HOME_URL = "https://portal.qa.hubwoo.com/srvs/CatalogManager/";
			CMS_MONITOR_URL = "https://portal.qa.hubwoo.com/srvs/CatalogManager/monitor/MonitorSupplier";
			CMB_CATALOG_HOME_URL = "https://portal.qa.hubwoo.com/srvs/BuyerCatalogs/";
			CMB_CATALOG_HOME_URL1 = "https://portal.qa.hubwoo.com/srvs/BuyerCatalogs";
			CMB_MONITOR_URL = "https://portal.qa.hubwoo.com/srvs/BuyerCatalogs/monitor/MonitorBuyer";
			SUPPLIER_USER1_LOGIN = "SVS1";
			SUPPLIER_USER1_PASSWORD = "Xsw23edc!";

			BUYER_USER1_LOGIN = "SVB-0001ba";
			BUYER_USER1_PASSWORD = "Xsw23edc!";
			CMS_MONITOR_PROCESS_FILTER_SIMPLE_CATALOG_IMPORT = "79";// Simple Catalog Import 79
			CMS_MONITOR_PROCESS_FILTER_RELEASE_CATALOG = "80";//80 Release catalog
			CMB_MONITOR_PROCESS_FILTER_SET_LIVE = "52";//
			CMB_MONITOR_PROCESS_FILTER_LOAD_CATALOG = "86";//
			CMS_MONITOR_PROCESS_FILTER_ATTACHMENT_PROCESSING = "88";

			TCO1_CATALOG_SELECTOR = "\\36 3045_";
			TCO1_CATALOG_SELECTOR_ID = "36 3045_";
			TC01_CATALOG_ID = "36 3045";
			TC01_CUSTOMER_ID = "SVB-0001";
			TC01_CUSTOMERNAME = "SV Buyer";
			TC01_CATALOG_LOAD_ATTEMPTS = 20;
			TC01_CATALOG_METACATID = "#\\36 3045_";
			CMB_REVIEW_ITEMS_COLUMNS_SET_ENRICHMENT = "940";
			TC04_SUPPLIER_ID = "SVS1";
			TC04_CATALOG_SELECTOR = "\\32 37593_";
			TC04_CATALOG_SELECTOR_ID = "\\32 37593";
			TC04_SUPPLIERNAME = "SV Supplier 1";
			TC04_SUPPLIER_METACATID = "237593";
			TC04_APPROVE_ITEMS_REGEX = "^https://portal.qa.hubwoo.com/srvs/BuyerCatalogs/items/item-list?show=";
			MONITOR_CHECK_ATTEMPTS = 30;
			TC05_CATALOG_SELECTOR_ID = "(63045)";
			BUYER_ADMIN_HOME = "https://portal.qa.hubwoo.com/main/contactmanagement/Default.aspx";
			BUYER_ADMIN_LANDING_PAGE_URL = "https://portal.qa.hubwoo.com/srvs/BuyerCatalogs/admin/LandingPage";
			TC10_SELECTED_VIEW = "SVVIEW1";
			SEARCH_CONFIGURE_LANDING_URL = "https://econtent.hubwoo.com/catalog/search5/showMenu.action";
			BUYER_ADMIN_EDIT_USERS = "https://portal.qa.hubwoo.com/srvs/omnicontent/BuyerManageUsers.aspx";
			BUYER_ADMIN_EDIT_PROFILE_URL = "https://portal.qa.hubwoo.com/main/Admin/MyProfile?takeTo=https:%2F%2Fportal.hubwoo.com%2Fmain%2F";
			BUYER_ADMIN_CREATE_USER_URL = "https://portal.qa.hubwoo.com/srvs/omnicontent/BuyerAdminCreateUser.aspx";
			BUYER_ADMIN_DOWNLOAD_URL = "https://portal.qa.hubwoo.com/srvs/BuyerCatalogs/export/index";
			BUYER_ADMIN_REPORTING_URL = "https://portal.qa.hubwoo.com/srvs/BuyerCatalogs/reporting/index";
			DOWNLOAD_REPORT_SUPPLIERNAME = "SV Supplier 1 (654321)";
			SHOW_HISTORY_SUPPLIERNAME = "SV Supplier 1";
			ARCHIVE_CATALOG_SUPPLIER_ID = "2 37593";
			DATA_GROUPS_USER_ASSIGNMENT_URL = "https://portal.qa.hubwoo.com/srvs/BuyerCatalogs/admin/DataGroupUserAssignment";
			DATAGROUP_NAME = "Qa test 1key enrichment";
			TC16_DATAGROUP_ASSIGNMENT_REMOVE_USER = "SVB-0001 Buyer admin";
			CMB_MONITOR_PROCESS_FILTER_ENRICHMENT_IMPORT = "23";
			CMB_MONITOR_PROCESS_FILTER_MULTI_KEY_ENRICHMENT_IMPORT = "163";
			ENRICHMENT_DATAGROUP_DOWNLOAD_NAME = "CUS63045_2_Key_datagrouptest";
			CMB_MONITOR_PROCESS_FILTER_TEMPLATE_EXPORT = "31";
			UPLOAD_ENRICHMENT_FILE1 = "1key_enrich_template_new.xlsx";
			UPLOAD_ENRICHMENT_FILE2 = "2key_multi_key_enrich_template.xlsx";
			SUPPLIER_CHECK_ROUTINE_FILE = "xlsx_qa_catalog_SCF_qa_file_base_checkroutine_supplier.xlsx";
			CMB_MONITOR_PROCESS_FILTER_SIMPLE_CATALOG_IMPORT = "79";
			CUSTOMER_UPLOAD_AND_RELEASE_CATALOG_FILE = "xlsx_qa_catalog_SCF_qa_file_base_searchCheck.xlsx";
			TC19_DASHBOARD_CATALOGID = "6 3045_237593";
			CUSTOMER_CHECK_ROUTINE_FILE = "xlsx_qa_catalog_SCF_qa_file_base_checkroutine_customer.xlsx";
			TC20_ERROR_CORRECTION_VALUE = "has long description";
			CATALOG_IMPORT_WITH_ATTACHMENTS_FILE = "baseAttachmentUpload.zip";
			CATALOG_IMPORT_WITH_ATTACHMENTS_CATALOG = "xlsx_qa_catalog_SCF_qa_file_base_attachmentUpload.xlsx";
			CMS_MONITOR_PROCESS_FILTER_TEMPLATE_EXPORT = "31";
			VIEW_AND_DOWNLOAD_DIFFING_REPORT = "xlsx_qa_catalog_SCF_qa_file_updated.xlsx";
			EXECUTE_ENRICHMENT_CATALOG_FILE = "xlsx_qa_catalog_SCF_qa_file_base_enrichment.xlsx";
			CMB_DATA_GROUPS_USER_ASSIGNMENT = "https://portal.qa.hubwoo.com/srvs/BuyerCatalogs/admin/DataGroupUserAssignment";
			SEARCH_URL_RELEASE_TEST = "https://search.qa.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=SVVIEW1&VIEW_PASSWD=q0E2Aft3PQy18&USER_ID=SV&BRANDING=search5&LANGUAGE=EN&HOOK_URL=https://portal.qa.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp&ADMIN=1&COUNTRY=GB&EASYORDER=1";
			CMB_DIFFING_REPORT1 = "xlsx_qa_catalog_SCF_qa_file_base.xlsx";
			CMB_DIFFING_REPORT2 = "xlsx_qa_catalog_SCF_qa_file_updated.xlsx";
		}

		if (Environment == "UAT")
		{
			//note none of this has been tested in uat, not sure the buyer.supplier accounts/datasheets/forms etc exist to test!!!!!!
			CONTENTADMIN_LOGIN = "";
			CONTENTADMIN_PASSWORD = "";

			PORTAL_LOGIN = "https://portal.uat.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F";
			PORTAL_MAIN_URL = "https://portal.uat.hubwoo.com/main/";
			CMA_ADMIN_COMPANY_FIND_URL = "https://portal.uat.hubwoo.com/srvs/Contentadmin/AdminCompanyFind2007.aspx";
			PORTAL_LOGOUT = "https://portal.uat.hubwoo.com/srvs/login/logout";
			QQB_REQUEST_LIST_URL = "https://portal.uat.hubwoo.com/srvs/easyorder/RequestList2007.aspx";
			QQS_REQUEST_LIST_URL = "https://portal.uat.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx";
			ADMIN_RELATIONEDIT_REGEX = "^https://portal.uat.hubwoo.com/srvs/Contentadmin/AdminRelationEdit2007.aspx";
			QQS_OFFER_DETAIL_URL = "https://portal.uat.hubwoo.com/srvs/easyorder/OfferDetail2007.aspx";
			QQS_OFFER_DETAIL_URL_REGEX = "^https://portal.uat.hubwoo.com/srvs/easyorder/OfferDetail2007.aspx";
			ADMIN_EO_PROPERTIES_EDIT_REGEX = "^https://portal.uat.hubwoo.com/srvs/ContentAdmin/AdminEOPropertiesEdit.aspx";
			CUSTOM_FORM_EDIT_REGEX = "^https://portal.uat.hubwoo.com/srvs/easyorder/CustomForms.aspx";
			CMS_CATALOG_HOME_URL = "https://portal.uat.hubwoo.com/srvs/CatalogManager/";
			CMS_MONITOR_URL = "https://portal.uat.hubwoo.com/srvs/CatalogManager/monitor/MonitorSupplier";

			CMB_CATALOG_HOME_URL = "https://portal.uat.hubwoo.com/srvs/BuyerCatalogs/";
			CMB_CATALOG_HOME_URL1 = "https://portal.uat.hubwoo.com/srvs/BuyerCatalogs";
			CMB_MONITOR_URL = "https://portal.uat.hubwoo.com/srvs/BuyerCatalogs/monitor/MonitorBuyer";

			SUPPLIER_USER1_LOGIN = "";
			SUPPLIER_USER1_PASSWORD = "";
			BUYER_USER1_LOGIN = "";
			BUYER_USER1_PASSWORD = "";

			CMS_MONITOR_PROCESS_FILTER_SIMPLE_CATALOG_IMPORT = "79";// Simple Catalog Import 79
			CMS_MONITOR_PROCESS_FILTER_RELEASE_CATALOG = "80";//80 Release catalog
			CMB_MONITOR_PROCESS_FILTER_SET_LIVE = "";//
			CMB_MONITOR_PROCESS_FILTER_LOAD_CATALOG = "";//
			CMS_MONITOR_PROCESS_FILTER_ATTACHMENT_PROCESSING = "88";
			CMB_REVIEW_ITEMS_COLUMNS_SET_ENRICHMENT = "";
			TCO1_CATALOG_SELECTOR = "\\36 2376_";
			TC01_CUSTOMER_ID = "TESTCUSTCDO-0001";
			TC01_CUSTOMERNAME = "";
			TC01_CATALOG_METACATID = "";

			TC04_SUPPLIER_ID = "";
			TC04_CATALOG_SELECTOR = "";
			TC04_CATALOG_SELECTOR_ID = "";
			TC04_SUPPLIERNAME = "";
			TC04_SUPPLIER_METACATID = "77418";
			TC04_APPROVE_ITEMS_REGEX = "^https://portal.uat.hubwoo.com/srvs/BuyerCatalogs/items/item-list?show=";
			TC05_CATALOG_SELECTOR_ID = "";

			MONITOR_CHECK_ATTEMPTS = 30;

			BUYER_ADMIN_HOME = "https://portal.uat.hubwoo.com/main/contactmanagement/Default.aspx";
			BUYER_ADMIN_LANDING_PAGE_URL = "https://portal.uat.hubwoo.com/srvs/BuyerCatalogs/admin/LandingPage";
			TC10_SELECTED_VIEW = "";
			SEARCH_CONFIGURE_LANDING_URL = "https://econtent.hubwoo.com/catalog/search5/showMenu.action";
			BUYER_ADMIN_EDIT_USERS = "https://portal.uat.hubwoo.com/srvs/omnicontent/BuyerManageUsers.aspx";
			BUYER_ADMIN_EDIT_PROFILE_URL = "https://portal.uat.hubwoo.com/main/Admin/MyProfile?takeTo=https:%2F%2Fportal.hubwoo.com%2Fmain%2F";
			BUYER_ADMIN_CREATE_USER_URL = "https://portal.uat.hubwoo.com/srvs/omnicontent/BuyerAdminCreateUser.aspx";
			BUYER_ADMIN_DOWNLOAD_URL = "https://portal.uat.hubwoo.com/srvs/BuyerCatalogs/export/index";
			BUYER_ADMIN_REPORTING_URL = "https://portal.uat.hubwoo.com/srvs/BuyerCatalogs/reporting/index";
			DOWNLOAD_REPORT_SUPPLIERNAME = "";
			SHOW_HISTORY_SUPPLIERNAME = "";
			ARCHIVE_CATALOG_SUPPLIER_ID = "";
			DATA_GROUPS_USER_ASSIGNMENT_URL = "https://portal.uat.hubwoo.com/srvs/BuyerCatalogs/admin/DataGroupUserAssignment";
			DATAGROUP_NAME = "";
			TC16_DATAGROUP_ASSIGNMENT_REMOVE_USER = "";
			CMB_MONITOR_PROCESS_FILTER_ENRICHMENT_IMPORT = "";
			CMB_MONITOR_PROCESS_FILTER_MULTI_KEY_ENRICHMENT_IMPORT = "";
			ENRICHMENT_DATAGROUP_DOWNLOAD_NAME = "";
			CMB_MONITOR_PROCESS_FILTER_TEMPLATE_EXPORT = "";
			UPLOAD_ENRICHMENT_FILE1 = "1key_enrich_template_new.xlsx";
			UPLOAD_ENRICHMENT_FILE2 = "2key_multi_key_enrich_template.xlsx";
			SUPPLIER_CHECK_ROUTINE_FILE = "";
			CMB_MONITOR_PROCESS_FILTER_SIMPLE_CATALOG_IMPORT = "79";
			CUSTOMER_UPLOAD_AND_RELEASE_CATALOG_FILE = "";
			TC19_DASHBOARD_CATALOGID = "6 2376_77418";
			CUSTOMER_CHECK_ROUTINE_FILE = "xlsx_prod_catalog_SCF_prod_file_base_checkroutine_customer.xlsx";
			TC20_ERROR_CORRECTION_VALUE = "has long description";
			CATALOG_IMPORT_WITH_ATTACHMENTS_FILE = "baseAttachmentUpload.zip";
			CATALOG_IMPORT_WITH_ATTACHMENTS_CATALOG = "xlsx_prod_catalog_SCF_prod_file_base_attachmentUpload.xlsx";
			CMS_MONITOR_PROCESS_FILTER_TEMPLATE_EXPORT = "31";
			VIEW_AND_DOWNLOAD_DIFFING_REPORT = "xlsx_prod_catalog_SCF_prod_file_updated.xlsx";
			EXECUTE_ENRICHMENT_CATALOG_FILE = "xlsx_prod_catalog_SCF_prod_file_base_enrichment.xlsx";
			CMB_DATA_GROUPS_USER_ASSIGNMENT = "https://portal.uat.hubwoo.com/srvs/BuyerCatalogs/admin/DataGroupUserAssignment";
			SEARCH_URL_RELEASE_TEST = "https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE05-05&VIEW_PASSWD=t3S4TcKp89Rqy&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp";
		}

		if (Environment == "PROD")
		{
			CONTENTADMIN_LOGIN = "wai-ho.leung@proactis.com";
			CONTENTADMIN_PASSWORD = "initpass7654321#";

			PORTAL_LOGIN = "https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F";
			PORTAL_MAIN_URL = "https://portal.hubwoo.com/main/";
			CMA_ADMIN_COMPANY_FIND_URL = "https://portal.hubwoo.com/srvs/Contentadmin/AdminCompanyFind2007.aspx";
			PORTAL_LOGOUT = "https://portal.hubwoo.com/srvs/login/logout";
			QQB_REQUEST_LIST_URL = "https://portal.hubwoo.com/srvs/easyorder/RequestList2007.aspx";
			QQS_REQUEST_LIST_URL = "https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx";
			QQS_OFFER_DETAIL_URL = "https://portal.hubwoo.com/srvs/easyorder/OfferDetail2007.aspx";
			QQS_OFFER_DETAIL_URL_REGEX = "^https://portal.hubwoo.com/srvs/easyorder/OfferDetail2007.aspx";
			ADMIN_RELATIONEDIT_REGEX = "^https://portal.hubwoo.com/srvs/Contentadmin/AdminRelationEdit2007.aspx";
			CMS_CATALOG_HOME_URL = "https://portal.hubwoo.com/srvs/CatalogManager/";
			CMS_MONITOR_URL = "https://portal.hubwoo.com/srvs/CatalogManager/monitor/MonitorSupplier";

			CMB_CATALOG_HOME_URL = "https://portal.hubwoo.com/srvs/BuyerCatalogs/"; //test for url
			CMB_CATALOG_HOME_URL1 = "https://portal.hubwoo.com/srvs/BuyerCatalogs"; //test for href
			CMB_MONITOR_URL = "https://portal.hubwoo.com/srvs/BuyerCatalogs/monitor/MonitorBuyer";

			SUPPLIER_USER1_LOGIN = "EPAM_TS2";
			SUPPLIER_USER1_PASSWORD = "xsw23edc";

			BUYER_USER1_LOGIN = "EPAM_TC-0001";
			BUYER_USER1_PASSWORD = "xsw23edc";

			CMS_MONITOR_PROCESS_FILTER_SIMPLE_CATALOG_IMPORT = "79";// Simple Catalog Import 79
			CMS_MONITOR_PROCESS_FILTER_RELEASE_CATALOG = "80";//80 Release catalog
			CMB_MONITOR_PROCESS_FILTER_SET_LIVE = "52";//
			CMB_MONITOR_PROCESS_FILTER_LOAD_CATALOG = "86";//
			CMS_MONITOR_PROCESS_FILTER_ATTACHMENT_PROCESSING = "88";

			TCO1_CATALOG_SELECTOR = "\\36 2376_";
			TCO1_CATALOG_SELECTOR_ID = "36 2376_";
			TC01_CATALOG_ID = "62376";
			TC01_CUSTOMER_ID = "TESTCUSTCDO-0001";
			TC01_CUSTOMERNAME = "TESTCUSTCDO 1";
			TC01_CATALOG_LOAD_ATTEMPTS = 20; //approx 7 minutes on prod to upload catalog/load/release
			TC01_CATALOG_METACATID = "#\\36 2376_";   //62376 SELECT * FROM customer_cat WHERE cus_id_cat = 62376 TESTCUSTCDO-0001	Test Customer eContent - QA

			TC04_SUPPLIER_ID = "TESTSUPCDO2";
			TC04_CATALOG_SELECTOR = "\\37 7418_";
			TC04_CATALOG_SELECTOR_ID = "\\37 7418";
			TC04_SUPPLIERNAME = "TESTSUPCDO2";
			TC04_SUPPLIER_METACATID = "77418";
			TC04_APPROVE_ITEMS_REGEX = "^https://portal.hubwoo.com/srvs/BuyerCatalogs/items/item-list?show=";
			TC05_CATALOG_SELECTOR_ID = "(62376)";

			MONITOR_CHECK_ATTEMPTS = 30;

			BUYER_ADMIN_LANDING_PAGE_URL = "https://portal.hubwoo.com/srvs/BuyerCatalogs/admin/LandingPage";
			BUYER_ADMIN_HOME = "https://portal.hubwoo.com/main/contactmanagement/Default.aspx";
			TC10_SELECTED_VIEW = "TESTCOE01";
			SEARCH_CONFIGURE_LANDING_URL = "https://econtent.hubwoo.com/catalog/search5/showMenu.action";

			BUYER_ADMIN_EDIT_USERS = "https://portal.hubwoo.com/srvs/omnicontent/BuyerManageUsers.aspx";
			BUYER_ADMIN_EDIT_PROFILE_URL = "https://portal.hubwoo.com/main/Admin/MyProfile?takeTo=https:%2F%2Fportal.hubwoo.com%2Fmain%2F";
			BUYER_ADMIN_CREATE_USER_URL = "https://portal.hubwoo.com/srvs/omnicontent/BuyerAdminCreateUser.aspx";
			BUYER_ADMIN_DOWNLOAD_URL = "https://portal.hubwoo.com/srvs/BuyerCatalogs/export/index";
			DOWNLOAD_REPORT_SUPPLIERNAME = "TESTSUPCDO2 (TESTSUPCDO2)";
			SHOW_HISTORY_SUPPLIERNAME = "TESTSUPCDO2";
			ARCHIVE_CATALOG_SUPPLIER_ID = "7 7418";
			DATA_GROUPS_USER_ASSIGNMENT_URL = "https://portal.hubwoo.com/srvs/BuyerCatalogs/admin/DataGroupUserAssignment";
			DATAGROUP_NAME = "Prod test 1key enrichment";
			TC16_DATAGROUP_ASSIGNMENT_REMOVE_USER = "Buyer Admin EPAM";
			CMB_MONITOR_PROCESS_FILTER_ENRICHMENT_IMPORT = "23";
			CMB_MONITOR_PROCESS_FILTER_MULTI_KEY_ENRICHMENT_IMPORT = "163";
			ENRICHMENT_DATAGROUP_DOWNLOAD_NAME = "CUS62376_2_Key_datagrouptest";
			CMB_MONITOR_PROCESS_FILTER_TEMPLATE_EXPORT = "31";
			UPLOAD_ENRICHMENT_FILE1 = "1key_enrich_template_new.xlsx";
			UPLOAD_ENRICHMENT_FILE2 = "2key_multi_key_enrich_template.xlsx";
			SUPPLIER_CHECK_ROUTINE_FILE = "xlsx_prod_catalog_SCF_prod_file_base_checkroutine_supplier.xlsx";//TC08
			CMB_MONITOR_PROCESS_FILTER_SIMPLE_CATALOG_IMPORT = "79";
			CUSTOMER_UPLOAD_AND_RELEASE_CATALOG_FILE = "xlsx_prod_catalog_SCF_prod_file_base_searchCheck.xlsx"; //TEST TC18a
			TC19_DASHBOARD_CATALOGID = "6 2376_77418";
			CUSTOMER_CHECK_ROUTINE_FILE = "xlsx_prod_catalog_SCF_prod_file_base_checkroutine_customer.xlsx";//The catalog file is for the supplier TESTSUPCDO2 it has an empty long description for one of the catalog items. 
			TC20_ERROR_CORRECTION_VALUE = "has long description";
			CATALOG_IMPORT_WITH_ATTACHMENTS_FILE = "baseAttachmentUpload.zip";
			CATALOG_IMPORT_WITH_ATTACHMENTS_CATALOG = "xlsx_prod_catalog_SCF_prod_file_base_attachmentUpload.xlsx";
			CMS_MONITOR_PROCESS_FILTER_TEMPLATE_EXPORT = "31";
			VIEW_AND_DOWNLOAD_DIFFING_REPORT = "xlsx_prod_catalog_SCF_prod_file_updated.xlsx"; //FOR TEST TC05_CMS_View_And_Download_Diffing_Report
			EXECUTE_ENRICHMENT_CATALOG_FILE = "xlsx_prod_catalog_SCF_prod_file_base_enrichment.xlsx";
			CMB_DATA_GROUPS_USER_ASSIGNMENT = "https://portal.hubwoo.com/srvs/BuyerCatalogs/admin/DataGroupUserAssignment";
			SEARCH_URL_RELEASE_TEST = "https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE05-05&VIEW_PASSWD=t3S4TcKp89Rqy&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp";
		}

		string directory = Directory.GetCurrentDirectory();
		string playwrightTestsSubFolderStart = "";
		int startof = directory.IndexOf("\\PlaywrightTests");
		if (startof > 0)
		{
			playwrightTestsSubFolderStart = directory.Substring(0, startof);
		}
		if (Environment == "QA")
		{
			downloadPath = System.IO.Path.Combine(directory, @"QATESTRESULTS\CMB\");
			TC01_CATALOG_FILE_PATH = System.IO.Path.Combine(playwrightTestsSubFolderStart, @"PlaywrightTests\CMB\QA\");
		}

		if (Environment == "UAT")
		{
			downloadPath = System.IO.Path.Combine(directory, @"UATTESTRESULTS\CMB\");
			TC01_CATALOG_FILE_PATH = System.IO.Path.Combine(playwrightTestsSubFolderStart, @"PlaywrightTests\CMB\UAT\");
		}

		if (Environment == "PROD")
		{
			//downloadPath EXAMPLE  C:\Sourcegit\ecat2023\catalog-manager\PlaywrightTests\bin\Debug\net7.0\PRODTESTRESULTS\CMB\
			//TC01_CATALOG_FILE_PATH EXAMPLE C:\\Sourcegit\ecat2023\catalog-manager\PlaywrightTests\\CMB\PROD\
			TC01_CATALOG_FILE_PATH = System.IO.Path.Combine(playwrightTestsSubFolderStart, @"PlaywrightTests\CMB\PROD\");//path where import catalog files required for some tests are stored
			downloadPath = System.IO.Path.Combine(directory, @"PRODTESTRESULTS\CMB\");//where downloads/exception screenshots are saved to

			Console.WriteLine("TC01_CATALOG_FILE_PATH: " + TC01_CATALOG_FILE_PATH);
			Console.WriteLine("downloadPath: " + downloadPath);
		}
	}

	public string ExtractZipFile(string archivePath, string outFolder)
	{
		string entryFileName = String.Empty;
		using (var fsInput = File.OpenRead(archivePath))
		using (var zf = new ZipFile(fsInput))
		{
			foreach (ZipEntry zipEntry in zf)
			{
				if (!zipEntry.IsFile)
				{
					// Ignore directories
					continue;
				}
				entryFileName = zipEntry.Name;
				// to remove the folder from the entry:
				//entryFileName = Path.GetFileName(entryFileName);
				// Optionally match entrynames against a selection list here
				// to skip as desired.
				// The unpacked length is available in the zipEntry.Size property.

				// Manipulate the output filename here as desired.
				var fullZipToPath = System.IO.Path.Combine(outFolder, entryFileName);
				var directoryName = System.IO.Path.GetDirectoryName(fullZipToPath);
				if (directoryName.Length > 0)
				{
					Directory.CreateDirectory(directoryName);
				}

				// 4K is optimum
				var buffer = new byte[4096];

				// Unzip file in buffered chunks. This is just as fast as unpacking
				// to a buffer the full size of the file, but does not waste memory.
				// The "using" will close the stream even if an exception occurs.
				using (var zipStream = zf.GetInputStream(zipEntry))
				using (Stream fsOutput = File.Create(fullZipToPath))
				{
					StreamUtils.Copy(zipStream, fsOutput, buffer);
				}
			}
		}
		return entryFileName;
	}

	async public Task SignInPortal(string username, string password)
	{
		await SignInPortal(PORTAL_MAIN_URL, username, password);
	}
	async public Task SignInPortal(string main, string username, string password)
	{
		await Page.GotoAsync(PORTAL_LOGIN);
		//The marquee may cause network never idle
		await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
		await Page.WaitForTimeoutAsync(1000);
		await DeleteFooter();
		Console.WriteLine("Filling in credentials.");
		await Page.Locator("//*[@id='signInUsername']").FillAsync(username);
		await Page.WaitForTimeoutAsync(200);
		await Page.Locator("//*[@id='SignIn_Password']").FillAsync(password);
		try
		{
			await Page.Locator("#signInButtonId").ClickAsync();
			await Task.Delay(500);
			await Page.WaitForURLAsync(main);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		}
		catch (TimeoutException tex)
		{
			Console.WriteLine("TimeoutException caught: " + tex.Message);
			if (Page.Url.Contains("Error.aspx"))
			{
				Console.WriteLine("Error encountered, navigating to main page directly.");
				await Page.GotoAsync(main, new PageGotoOptions { Timeout = 60000 });
			}
			else
			{
				Console.WriteLine("Unknown error during login!");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("Unexpected exception: " + ex);
			// Depending on your test framework, rethrow or handle appropriately.
		}
		await Page.WaitForSelectorAsync("//side-bar-item[@name='Home']", new PageWaitForSelectorOptions { Timeout = 60000 });
	}

	async public Task SignOut()
	{
		//Do not use network idle on signin page
		Console.WriteLine("Log off from CM");
		//await ReloadPageIfBackrop();
		await Page.Locator("top-bar-user-section[name='User']").ClickAsync();
		await Page.WaitForTimeoutAsync(500);
		await Page.Locator("top-bar-item[name='Log Off']").ClickAsync();
		await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
	}

	async public Task SetManualRefresh()
	{
		await Page.RunAndWaitForResponseAsync(async () =>
		{
			await Page.Locator("//*[@id=\"ddlRefreshTime\"]").SelectOptionAsync("0");
		}, response => response.Url.Contains("GetItemCount") && response.Status == 200, new() { Timeout = 60000 });
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page.WaitForTimeoutAsync(1000);//Task.Delay(1000);
		await ReloadPageIfBackrop();
	}


	async public Task ReloadPageIfBackrop()
	{ //Sometimes a backdrop stays at screen even loading screen has fade out
		//Backdrop is some UI render issue
		var loadingStatus = "";
		loadingStatus = await Page.Locator("//*[@id=\"loadingScreen\"]").GetAttributeAsync("style"); //Get loadingScreen style value - should be none after fade out
		if (loadingStatus != null)
		{
			if (await Page.Locator("div[class*='backdrop']").CountAsync() > 0)//Backdrop exist if count > 0
			{
				if (loadingStatus.Contains("none"))//Loading screen not showing
				{
					Console.WriteLine("Backdrop is found at screen, reload page!");
					await Page.ReloadAsync();
					await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
					await Page.WaitForTimeoutAsync(3000);
				}
			}
		}
	}

	async public Task ReloadIfStacktrace(Boolean tried)
	{
		await ReloadIfStacktrace(Page, tried);
	}
	async public Task ReloadIfStacktrace(IPage page, Boolean tried)
	{
		//Check if screen has word Stacktrace
		//Suppose to failed by timeout exception because response will not pass
		if (await page.GetByText("Stacktrace").CountAsync() > 0)
		{
			//Stacktrace found and not yet tried so reload the page
			if (!tried)
			{
				Console.WriteLine("Page to reload: " + page.Url);
				await page.ReloadAsync(new() { Timeout = 30000 }); //Allow max of 30sec to reload and would fail if dont
				await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
				await page.WaitForTimeoutAsync(3000);
				await ReloadIfStacktrace(page, true); //Casual check if stacktrace still exist
			}
			else
			{
				Console.WriteLine("Stacktrace still visible after reload");
			}
		}
		else
		{
			Console.WriteLine("Screen doesn't have StackTrace, nice");
		}
	}

	async public void ReloadMonitorFilter(IPage page, string customer, string filter)
	{//Reload monitor page but will reset filter so need to add back filter
		try
		{
			await page.RunAndWaitForResponseAsync(async () =>
			{
				await page.ReloadAsync(new() { Timeout = 30000 });
			}, response => response.Url.Contains("GetItemCount") && response.Status == 200);
		}
		catch (TimeoutException TE)
		{
			await ReloadIfStacktrace(page, false); //Page reloaded, check if get into stacktrace first
		}
		await ReloadPageIfBackrop(); //Then check if get backdrop
																 //No error so we can set filter now
		await page.GetByLabel("Customer ID:").FillAsync(customer);
		await page.GetByLabel("Process Type:").SelectOptionAsync(new[] { filter });  //simple catalog type
		await page.RunAndWaitForResponseAsync(async () =>
		{
			await page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();
		}, response => response.Url.Contains("GetItemCount") && response.Status == 200);
		await ReloadPageIfBackrop();
	}
	async public Task GoWithErrWrap(string URL, int timeout)
	{
		int to = timeout * 1000;
		try
		{
			await Page.GotoAsync(URL, new() { Timeout = to });
			Console.WriteLine("Go to: " + URL);
			await Page.WaitForURLAsync(URL, new() { Timeout = to });
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = to });
			await Page.WaitForTimeoutAsync(1000);
			await ReloadPageIfBackrop();
		}
		catch (TimeoutException)
		{
			await ReloadIfStacktrace(Page, false);
		}
	}
	async public Task ManualRefresh()
	{
		try
		{
			await Page.RunAndWaitForResponseAsync(async () =>
			{
				await Page.GetByText("Manual Refresh").ClickAsync();
			}, response => response.Url.Contains("GetItemCount") && response.Status == 200, new() { Timeout = 60000 });
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Page.WaitForTimeoutAsync(1000);

		}
		catch (TimeoutException)
		{
			await ReloadIfStacktrace(Page, false);
		}
		await ReloadPageIfBackrop();
	}

	async public Task MonitorProcessStatueAsync(string procid, string process, DateTime time, string supplier, string customer, string expStatus)
	{
		await MonitorProcessStatueAsync(Page, procid, process, time, supplier, customer, expStatus);
	}
	async public Task MonitorProcessStatueAsync(IPage page, string procid, string process, DateTime time, string supplier, string customer, string expStatus)
	//After accessed monitor page & set refresh to manual
	{
		await page.WaitForTimeoutAsync(1000);
		var duration = 15; //Allowed duration for a process is 15min
		var refresh = 30; //Refresh interval = 30s
		var exitflag = false;
		string fPid = procid;
		string[] exitCriteria = { "Finished OK", "Failed" };

		//Check process status start time
		DateTime startTime = time.AddMinutes(-1); //Minus 1 mins to mitigate local time - server time difference

		//Make sure no. of record is 10
		string recPerPage = await page.Locator("//*[@id='uiRecordCount']").InnerTextAsync();
		if (!recPerPage.Contains("10"))
		{
			await page.Locator("//*[@id='uiRecordCount']").ClickAsync();
			try
			{
				await page.RunAndWaitForResponseAsync(async () =>
				{
					await page.Locator("ul[role='menu']").Locator("li[onclick='setPageCount(10)']").ClickAsync();
				}, response => response.Url.Contains("GetItemCount") && response.Status == 200, new() { Timeout = 60000 });
				//Screen should have refreshed
				await page.WaitForTimeoutAsync(1000);
			}
			catch (TimeoutException TE)
			{
				await ReloadIfStacktrace(page, false);
			}
			await ReloadPageIfBackrop();
		}

		//New process, no id available... match with other information then store id
		if (fPid == "")
		{
			var table = page.Locator("//*[@id='itemListContainer']");
			var retried = 0;
			while (fPid == "" && retried <= 2)
			{
				Console.WriteLine(DateTime.Now);
				for (int i = 0; i < 10; i++)
				{
					//Read table mainRow cell 

					var row = table.Locator("tr[id^=mainRow]").Nth(i);
					string procName = await row.Locator("td").Nth(1).InnerTextAsync();
					string mPTime = await row.Locator("td").Nth(2).InnerTextAsync();
					DateTime procTime = TimeConverter(mPTime);
					string procSup = await row.Locator("td").Nth(3).InnerTextAsync();
					string procCust = await row.Locator("td").Nth(4).InnerTextAsync();
					Console.WriteLine("Retrieved time:" + procTime + "|Start time: " + startTime + " - ");
					Console.WriteLine("Retrieved Process: " + procName + " - " + procName.Equals(process)
							+ "\n|Supplier: " + procSup + " - " + procSup.Equals(supplier)
							+ "\n|Customer:" + procCust + " - " + procCust.Equals(customer));

					if (procName.Equals(process, StringComparison.OrdinalIgnoreCase) && procTime >= startTime && procSup.Equals(supplier) && procCust.Equals(customer))
					{
						//Record process id for later use
						fPid = await row.Locator("td").Nth(0).InnerTextAsync();
						break;
					}
				}
				if (fPid == "")
				{
					Console.WriteLine("Process not found at " + (retried + 1) + " try, refresh result and try again");
					await page.WaitForTimeoutAsync(2000);// Task.Delay(2000);
					ManualRefresh();
					await page.WaitForTimeoutAsync(8000);// Task.Delay(8000);
					retried++;
				}
			}
			if (String.IsNullOrEmpty(fPid)) //After reteriving process id from result table
			{
				throw new Exception("No process found after refresh");
			}
		}
		//At this stage fPid should be either found or parsed
		//Target process could be dynamic, especially during set live
		int pRow = -1;
		//Look for current process row by pid
		//Get process status
		//If process status is not finished ok or failed within duration then refresh monitor page
		//Process status check alter exitflag
		long kickoff = DateTimeOffset.Now.ToUnixTimeSeconds();
		long curTime = DateTimeOffset.Now.ToUnixTimeSeconds();
		long timeDiff = curTime - kickoff;
		string tProcessState = "";
		while (!exitflag && timeDiff <= (duration * 60))
		{
			//Get mainRow number that has process id
			for (int i = 0; i < 10; i++)
			{
				var tPid = await page.Locator("tr[id^='mainRow-']").Nth(i).Locator("td").Nth(0).InnerTextAsync();
				if (tPid == fPid && i < 10)
				{
					pRow = i;
					break;
				}
			}
			//Throw exception if no pid is found (which should not be possible)
			if (pRow == -1)
			{
				throw new RuntimeWrappedException("Process ID disappeared?");
			}
			else
			{
				//Console.WriteLine("Process is located at row " + (pRow + 1));
				//Get target process state
				tProcessState = await page.Locator("tr[id^='mainRow-']").Nth(pRow).Locator("td").Nth(5).InnerTextAsync();
				//if target process is finsihed OK or failed, exit loop
				if (exitCriteria.Contains(tProcessState))
				{
					exitflag = true;
					curTime = DateTimeOffset.Now.ToUnixTimeSeconds();
					timeDiff = curTime - kickoff;
				}
				else
				{
					//if not then wait @refresh seconds the refresh screen
					try
					{
						await page.RunAndWaitForResponseAsync(async () =>
						{
							await page.WaitForTimeoutAsync((refresh - 1) * 1000);
							await page.GetByText("Manual Refresh").ClickAsync();
						}, response => response.Url.Contains("GetItemCount") && response.Status == 200, new() { Timeout = 60000 });
						await page.WaitForTimeoutAsync(1000);// Task.Delay(1000);
					}
					catch (TimeoutException TE)
					{
						await ReloadIfStacktrace(page, false);
					}
					await ReloadPageIfBackrop();
					//update current time
					curTime = DateTimeOffset.Now.ToUnixTimeSeconds();
					timeDiff = curTime - kickoff;
				}
			}
		}
		//Check target process state again expected process state
		if (exitflag)
		{
			if (tProcessState.Equals(expStatus))
			{
				Console.WriteLine("Process " + fPid + " finished as expected (" + expStatus + ") in " + timeDiff + "s");
			}
			else
			{
				throw new AssertionException("Process " + fPid + " expected to finish with state " + expStatus + "\nBut get state " + tProcessState + "\nAfter " + timeDiff + "s");
			}
		}
		else
		{
			throw new TimeoutException("Process " + fPid + "is still running after " + duration + "mins");
		}
	}

	async public Task CMSFilter(string custName, string custID)
	{
		Console.WriteLine("filter catalogs via customer info");
		await Page.GetByLabel("Customer Name").FillAsync(custName);
		await Page.GetByLabel("Customer ID").FillAsync(custID);
		Console.WriteLine("click search");
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();
		await Page.WaitForTimeoutAsync(500);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page.WaitForTimeoutAsync(2000);
	}

	async public Task CMBFilter(string supname, string supID)
	{
		Console.WriteLine("filter catalogs via supplier info");
		await Page.GetByLabel("Supplier Name").FillAsync(supname);
		await Page.GetByLabel("Supplier ID").FillAsync(supID);
		Console.WriteLine("click search");
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();
		await Page.WaitForTimeoutAsync(500);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page.WaitForTimeoutAsync(2000);
	}

	async public Task VerifyCatalogStatus(string metaId, string status)
	{
		//var locator = page.Locator($"div[id*='{x}'][id$='_asd']");

		if (status.Contains("rocess"))
		{
			await Expect(Page.Locator($"div[id *= '{metaId}'][id$= '_catalog']").Locator("div[class^='info-box']").Locator("h5").Nth(1)).ToContainTextAsync("rocess");
		}
		else
		{
			await Expect(Page.Locator($"div[id *= '{metaId}'][id$= '_catalog']").Locator("div[class^='info-box']").Locator("h5").Nth(1)).ToContainTextAsync(status);
		}
	}
	async public Task DeleteFooter()
	{
		var isCookieConsentVisible = await Page.Locator("#cookie-consent-dialog").IsVisibleAsync();
		if (isCookieConsentVisible)
		{
			await Page.GetByRole(AriaRole.Button, new() { Name = "Accept all" }).ClickAsync();
		}
	}
	public DateTime TimeConverter(string dt)
	{
		string[] formats = { "dd/MM/yyyy (HH:mm)", "M/d/yyyy (h:mm tt)" };
		if (DateTime.TryParseExact(dt, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
		{
			return result;
		}
		throw new FormatException("Invalid date format");
	}


	public void WaitForElementToBeHidden(Microsoft.Playwright.IPage page, string elementSelector)
	{
		//runs synchronously, so not useable if we use parallelism in tests??
		//when refreshing the dashboard/monitor a modal mask is often displayed which interferes with playwright actionability checks
		//this function waits for the the element with the id elementSelector to no longer be visible
		Console.WriteLine("WaitForElementToBeHidden waiting for element with id " + elementSelector + " to no longer be visible");
		int attempt = 0;
		var isElementVisibleTask = page.Locator(elementSelector).IsVisibleAsync();
		Boolean isElementVisible = isElementVisibleTask.Result;
		while (isElementVisible && attempt < 30)//waits for a total time of 5 minutes or until the element is no longer visible
		{
			try
			{
				//can't run playwright assertions synchronously e.g. Expect(page.Locator("#loadingScreen")).Not.ToBeVisibleAsync, so instead just wait 
				System.Threading.Thread.Sleep(15000);//sleep for 15 seconds

				isElementVisibleTask = page.Locator(elementSelector).IsVisibleAsync();
				isElementVisible = isElementVisibleTask.Result;
				if (!isElementVisible)
				{
					Console.WriteLine("element " + elementSelector + " is no longer visible");
					break;
				}
				attempt++;
			}
			catch
			{
			}
		}

		if (isElementVisible)
		{
			Console.WriteLine("have waited for 20 iterations and the element " + elementSelector + " is still visible!");
		}
	}

	[OneTimeTearDown]
	public void OneTimeTearDown()
	{
		//runs once after all tests have finished
		Console.WriteLine("OneTimeTearDown");
	}

	[SetUp]
	public void SetUp()
	{
		string testStepStarted = DateTime.Now.ToLongTimeString();
		Console.WriteLine("Test started " + testStepStarted);
		//runs before each test starts
		Console.WriteLine("SetUp");
		Console.WriteLine(Browser.BrowserType.Name);
		Console.WriteLine(Browser.BrowserType.ExecutablePath);
		Console.WriteLine("Environment :" + Environment);
		Console.WriteLine("email epoch after: " + testStartSecondsSinceEpoch);
		_browserName = Browser.BrowserType.Name;
	}

	[TearDown]
	public void TearDown()
	{
		//runs after each test finishes
		Console.WriteLine("TearDown");
	}

	[Test, Order(1)]
	[Category("CMBTests")]
	async public Task TC01_CMS_Catalog_Import_And_Release_CSV()
	{
		//179310 [CMS]Catalog Import and release
		/*
PROD  Login CMS as user "EPAM_TS2" / xsw23edc upload catalog for buyer TESTCUSTCDO 1
QA    Login CMS as User "SVS1" / Xsw23edc!
assuming before this test begins that there is an existing catalog for the supplier and its status is in production or that the lastr catalog was rejetced , either way there is no working version pending an action!
 */
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179307 
		Console.WriteLine("TC01_CMS_Catalog_Import_And_Release_CSV");


		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 60000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 60000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOptionMonitor = new LocatorAssertionsToContainTextOptions { Timeout = 30000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 60000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 60000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 60000 };
		LocatorClickOptions manualRefreshClickOptions = new LocatorClickOptions { Force = true, Timeout = 60000 };
		string url = PORTAL_LOGIN;
		DateTime thisTestStarted = DateTime.UtcNow;
		Console.WriteLine("this test started: " + thisTestStarted.ToLongTimeString());
		await Page.SetViewportSizeAsync(1600, 900);
		await SignInPortal(PORTAL_MAIN_URL, SUPPLIER_USER1_LOGIN, SUPPLIER_USER1_PASSWORD);
		if (Environment == "PROD")
		{
			await Expect(Page.Locator("#notificationsWidget")).ToContainTextAsync("Notifications");
		}

		(await GetSideBarHeaderTextAsync()).Should().Contain("The Business Network");
		await GoWithErrWrap(CMS_CATALOG_HOME_URL, 120);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("My Catalogs");
		Console.WriteLine(Page.Url);
		await CMSFilter("", TC01_CUSTOMER_ID);
		await Expect(Page.GetByText(TC01_CUSTOMERNAME)).ToBeVisibleAsync();
		Console.WriteLine("click show more");
		await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Show more" })).ToBeVisibleAsync(locatorVisibleAssertion);
		//await Page.GetByRole(AriaRole.Link, new() { Name = "Show more" }).ClickAsync(locatorClickOptions);
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Task.Delay(2000);

		await Page.GetByRole(AriaRole.Link, new() { Name = "Upload Files" }).ClickAsync(locatorClickOptions);
		if (Environment == "PROD")
		{
			Console.WriteLine("select catalog zip file to upload: prod_catalog_scf_csv.zip");
			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}fileSelect\"]").SetInputFilesAsync(new[] { TC01_CATALOG_FILE_PATH + "prod_catalog_scf_csv.zip" });
			await Expect(Page.GetByRole(AriaRole.Table)).ToContainTextAsync("prod_catalog_scf_csv.zip");
			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}prod_catalog_scf_csv\\.zip_selectType\"]").SelectOptionAsync(new[] { "content" });
		}
		if (Environment == "QA")
		{
			Console.WriteLine("select catalog zip file to upload: qa_catalog_scf_csv.zip");
			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}fileSelect\"]").SetInputFilesAsync(new[] { TC01_CATALOG_FILE_PATH + "qa_catalog_scf_csv.zip" });
			await Expect(Page.GetByRole(AriaRole.Table)).ToContainTextAsync("qa_catalog_scf_csv.zip");
			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}qa_catalog_scf_csv\\.zip_selectType\"]").SelectOptionAsync(new[] { "content" });
		}


		if (Environment == "UAT")
		{
			//not implemented
		}
		Console.WriteLine("upload catalog file");
		DateTime jobstart = DateTime.Now;
		await Task.Delay(4000);//fails when this is removed

		await Page.GetByRole(AriaRole.Link, new() { Name = "Process files" }).ClickAsync(locatorClickOptions);
		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}uploadFileList\"]")).ToContainTextAsync("Your upload files were placed in the process queue. They will be processed as soon as possible.Please refresh your screen (press F5) in a few seconds or go to Monitor for detailed process information.");
		Console.WriteLine("******************************************************");
		Console.WriteLine("Go to monitor SIMPLE CATALOG IMPORT");
		Console.WriteLine("******************************************************");
		await GoWithErrWrap(CMS_MONITOR_URL, 120);
		await SetManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Simple Catalog import", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");

		////////////////////////////////////////////////////////////////////
		Console.WriteLine("******************************************************");
		Console.WriteLine("Catalog upload complete");
		Console.WriteLine("******************************************************");
		////////////////////////////////////////////////////////////////////

		////////////////////////////////////////////////////////
		//                  RELEASE CATALOG
		////////////////////////////////////////////////////////
		await GoWithErrWrap(CMS_CATALOG_HOME_URL, 120);
		await CMSFilter("", TC01_CUSTOMER_ID);
		int tried = 0;
		while (await Page.GetByText("Imported").CountAsync() > 0 && tried <= 2)
		{
			Console.WriteLine("Handle race condition that status not changed to Imported yet");
			await Page.WaitForTimeoutAsync(3000);
			await ReloadPageIfBackrop();
			tried++;
		}
		await Page.WaitForTimeoutAsync(2000);
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(3000);

		//is the submit catalog link expanded?

		//sometimes get here and the upload files chevron is active??

		//click the submit chevron
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}tab4_link\"]").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		//assert the div displaying the text 'Currently, this catalog is set to "Manual" Submit Mode.'  is visible
		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitModeText\"]")).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine($"assert [id={TCO1_CATALOG_SELECTOR}submitCat  button is visible");
		/*
 the submit catalog chevron is not active why?
 2024-03-21T20:53:16.551Z pw:api   locator resolved to <div id="62376_submitModeText">Currently, this catalog is set to "Manual" SubmitΓÇª</div>
 2024-03-21T20:53:16.551Z pw:api   unexpected value "hidden" 
 */
		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitCat\"]")).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("click submit catalog button");
		await Task.Delay(4000);
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitCat\"]").ClickAsync(locatorClickOptions);
		jobstart = DateTime.Now;
		await Page.WaitForTimeoutAsync(200);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page.WaitForTimeoutAsync(2000);
							/*try
							{
			
							}
							catch
							{
								await Page.GotoAsync(CMS_CATALOG_HOME_URL, pageGotoOptions);
								Console.WriteLine("Waiting for " + CMS_CATALOG_HOME_URL);
								await Page.WaitForURLAsync(CMS_CATALOG_HOME_URL, pageWaitForUrlOptions);
								Console.WriteLine(Page.Url);

								//wait for https://portal.hubwoo.com/srvs/CatalogManager/  CMS_CATALOG_HOME_URL
								Console.WriteLine("filter catalogs via customer catalog id");
								await Page.GetByLabel("Customer ID").FillAsync(TC01_CUSTOMER_ID);
								Console.WriteLine("click search");
								await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
								Console.WriteLine("click show more");
								await Task.Delay(2000);
								await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);
								await Task.Delay(2000);
								//click the submit chevron
								await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}tab4_link\"]").ClickAsync(locatorClickOptions);

								//assert the div displaying the text 'Currently, this catalog is set to "Manual" Submit Mode.'  is visible
								await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitModeText\"]")).ToBeVisibleAsync(locatorVisibleAssertion);

								await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitCat\"]")).ToBeVisibleAsync(locatorVisibleAssertion);
							}
					*/
		//release catalog  62376_submitCat
		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitCatalogMessage\"]")).ToContainTextAsync("Your catalog was placed in the process queue and will be submitted to your customer. Please refresh your screen (press F5) in a few seconds or go to Monitor for detailed process information.");
		await Page.WaitForTimeoutAsync(1000);
		Console.WriteLine("******************************************************");
		Console.WriteLine("Go to monitor RELEASE CATALOG");//href = "/srvs/CatalogManager/monitor/MonitorSupplier"
		Console.WriteLine("******************************************************");
		await GoWithErrWrap(CMS_MONITOR_URL, 120);
		await ManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Release catalog", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Catalog Released");
		Console.WriteLine("**********************************************");
		////////////////////////////////////////////////////////////////////
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}


	[Test, Order(2)]
	[Category("CMBTests")]
	async public Task TC02_CMS_Catalog_Import_And_Release_XLS()
	{
		//179310 [CMS]Catalog Import and release

		/*
 Login CMS as user "EPAM_TS2" / xsw23edc
 */
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179307 
		Console.WriteLine("TC02_CMS_Catalog_Import_And_Release_XLS");

		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 60000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 60000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOptionMonitor = new LocatorAssertionsToContainTextOptions { Timeout = 30000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 60000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 60000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 60000 };
		LocatorClickOptions manualRefreshClickOptions = new LocatorClickOptions { Force = true, Timeout = 60000 };
		string url = PORTAL_LOGIN;
		await Page.SetViewportSizeAsync(1600, 900);
		await SignInPortal(PORTAL_MAIN_URL, SUPPLIER_USER1_LOGIN, SUPPLIER_USER1_PASSWORD);
		await Page.WaitForSelectorAsync("//side-bar-item[@name='Home']", new PageWaitForSelectorOptions { Timeout = 60000 });
		if (Environment == "PROD")
		{
			await Expect(Page.Locator("#notificationsWidget")).ToContainTextAsync("Notifications");
			Console.WriteLine(Page.Url);
		}
		(await GetSideBarHeaderTextAsync()).Should().Contain("The Business Network");

		//click catalogs tab
		Console.WriteLine("go to catalogs tab");
		await GoWithErrWrap(CMS_CATALOG_HOME_URL, 120);
		await CMSFilter("", TC01_CUSTOMER_ID);
		await Task.Delay(4000);

		//await Expect(Page.GetByText(TC01_CUSTOMERNAME)).ToBeVisibleAsync();
		Console.WriteLine("expand and upload");

		Console.WriteLine("click show more");
		await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Show more" })).ToBeVisibleAsync(locatorVisibleAssertion);
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(2000);
		await Page.GetByRole(AriaRole.Link, new() { Name = "Upload Files" }).ClickAsync(locatorClickOptions);

		Console.WriteLine("Upload catalog file and set to content");
		if (Environment == "PROD")
		{
			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}fileSelect\"]").SetInputFilesAsync(new[] { TC01_CATALOG_FILE_PATH + "xls_prod_catalog_SCF_prod_file.xls" });
			await Expect(Page.GetByRole(AriaRole.Table)).ToContainTextAsync("xls_prod_catalog_SCF_prod_file.zip");//not sure why ui display .zip when .xls was selected, but whatever?

			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}xls_prod_catalog_SCF_prod_file\\.zip_selectType\"]").SelectOptionAsync(new[] { "content" });
		}
		else if (Environment == "QA")
		{
			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}fileSelect\"]").SetInputFilesAsync(new[] { TC01_CATALOG_FILE_PATH + "xls_qa_catalog_SCF_qa_file.xls" });
			await Expect(Page.GetByRole(AriaRole.Table)).ToContainTextAsync("xls_qa_catalog_SCF_qa_file.zip");//not sure why ui display .zip when .xls was selected, but whatever?
			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}xls_qa_catalog_SCF_qa_file\\.zip_selectType\"]").SelectOptionAsync(new[] { "content" });
		}
		else if (Environment == "UAT")
		{
			//not implemented
		}
		Console.WriteLine("Catalog file uploaded");
		await Task.Delay(2000);
		Console.WriteLine("Process file now");
		DateTime jobstart = DateTime.Now;
		await Page.GetByRole(AriaRole.Link, new() { Name = "Process files" }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}uploadFileList\"]")).ToContainTextAsync("Your upload files were placed in the process queue. They will be processed as soon as possible.Please refresh your screen (press F5) in a few seconds or go to Monitor for detailed process information.");
		await Page.WaitForTimeoutAsync(2000);
		Console.WriteLine("******************************");
		Console.WriteLine("Go to monitor SIMPLE CATALOG IMPORT");
		Console.WriteLine("******************************");
		await GoWithErrWrap(CMS_MONITOR_URL, 120);
		await ManualRefresh();
		await MonitorProcessStatueAsync("", "Simple Catalog import", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		////////////////////////////////////////////////////////////////////
		Console.WriteLine("******************************");
		Console.WriteLine("Catalog upload complete");
		Console.WriteLine("******************************");
		////////////////////////////////////////////////////////////////////

		////////////////////////////////////////////////////////
		//RELEASE CATALOG
		////////////////////////////////////////////////////////
		await GoWithErrWrap(CMS_CATALOG_HOME_URL, 120);
		await CMSFilter("", TC01_CUSTOMER_ID);
		int tried = 0;
		while (await Page.GetByText("Imported").CountAsync() > 0 && tried <= 2)
		{
			Console.WriteLine("Handle race condition that status not changed to Imported yet");
			await Page.WaitForTimeoutAsync(3000);
			await ReloadPageIfBackrop();
			tried++;
		}
		Console.WriteLine("click show more");
		await Task.Delay(2000);
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(2000);
		//href="/srvs/CatalogManager/supplier/item-list?show=UI_77418_62376_BME&cid=62376&sid=77418&enter=true"

		Console.WriteLine("click submit catalog chevron");
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}tab4_link\"]").ClickAsync(locatorClickOptions);
		await Task.Delay(3000);
		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitCat\"]")).ToBeVisibleAsync(locatorVisibleAssertion);
		
		Console.WriteLine("submit catalog");
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitCat\"]").ClickAsync(new LocatorClickOptions { Force = true, Timeout = 60000 });  //e.g. #\36 2376_submitCat
		await Page.WaitForTimeoutAsync(200);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		jobstart = DateTime.Now;
		//release catalog  62376_submitCat
		await Task.Delay(2000);
		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitCatalogMessage\"]")).ToContainTextAsync("Your catalog was placed in the process queue and will be submitted to your customer. Please refresh your screen (press F5) in a few seconds or go to Monitor for detailed process information.");

		Console.WriteLine("*******************************************************");
		Console.WriteLine("Go to monitor : RELEASE CATALOG");
		Console.WriteLine("******************************************************");
		await GoWithErrWrap(CMS_MONITOR_URL, 120);
		await ManualRefresh();
		await MonitorProcessStatueAsync("", "Release catalog", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		////////////////////////////////////////////////////////////////////
		Console.WriteLine("******************************************************");
		Console.WriteLine("Catalog Released");
		Console.WriteLine("******************************************************");
		////////////////////////////////////////////////////////////////////
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}



	[Test, Order(3)]
	[Category("CMBTests")]
	async public Task TC03_CMS_Catalog_Import_And_Release_XLSX()
	{
		//179310 [CMS]Catalog Import and release

		/*
 Login CMS as user "EPAM_TS2" / xsw23edc
 */
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179307 
		Console.WriteLine("TC03_CMS_Catalog_Import_And_Release_XLSX");

		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 60000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 60000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOptionMonitor = new LocatorAssertionsToContainTextOptions { Timeout = 30000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 60000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 60000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 60000 };
		LocatorClickOptions manualRefreshClickOptions = new LocatorClickOptions { Force = true, Timeout = 60000 };
		string url = PORTAL_LOGIN;
		await Page.SetViewportSizeAsync(1600, 900);
		await SignInPortal(PORTAL_MAIN_URL, SUPPLIER_USER1_LOGIN, SUPPLIER_USER1_PASSWORD);		
		if (Environment == "PROD")
		{
			await Expect(Page.Locator("#notificationsWidget")).ToContainTextAsync("Notifications");
		}

			(await GetSideBarHeaderTextAsync()).Should().Contain("The Business Network");

		//click catalogs tab
		await GoWithErrWrap(CMS_CATALOG_HOME_URL, 120);
		await CMSFilter("", TC01_CUSTOMER_ID);
		await Task.Delay(4000);

		await Expect(Page.GetByText(TC01_CUSTOMERNAME)).ToBeVisibleAsync();
		Console.WriteLine("expand and upload");
		await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Show more" })).ToBeVisibleAsync(locatorVisibleAssertion);
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Task.Delay(2000);

		await Page.GetByRole(AriaRole.Link, new() { Name = "Upload Files" }).ClickAsync(locatorClickOptions);

		Console.WriteLine("select catalog xlsx file to upload");

		if (Environment == "PROD")
		{
			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}fileSelect\"]").SetInputFilesAsync(new[] { TC01_CATALOG_FILE_PATH + "xlsx_prod_catalog_SCF_prod_file_base.xlsx" });
			await Expect(Page.GetByRole(AriaRole.Table)).ToContainTextAsync("xlsx_prod_catalog_SCF_prod_file_base.zip");
			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}xlsx_prod_catalog_SCF_prod_file_base\\.zip_selectType\"]").SelectOptionAsync(new[] { "content" });
		} else if (Environment == "QA")
		{
			//TODO create qa catalog test files,  users and buyer/supplier companies
			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}fileSelect\"]").SetInputFilesAsync(new[] { TC01_CATALOG_FILE_PATH + "xlsx_qa_catalog_SCF_qa_file_base.xlsx" });
			await Expect(Page.GetByRole(AriaRole.Table)).ToContainTextAsync("xlsx_qa_catalog_SCF_qa_file_base.zip");
			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}xlsx_qa_catalog_SCF_qa_file_base\\.zip_selectType\"]").SelectOptionAsync(new[] { "content" });
		} else if (Environment == "UAT")
		{
			//not implemented
		}
		Console.WriteLine("upload catalog file");
		await Task.Delay(2000);
		await Page.GetByRole(AriaRole.Link, new() { Name = "Process files" }).ClickAsync(locatorClickOptions);
		await Page.WaitForTimeoutAsync(2000);
		DateTime jobstart = DateTime.Now;
		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}uploadFileList\"]")).ToContainTextAsync("Your upload files were placed in the process queue. They will be processed as soon as possible.Please refresh your screen (press F5) in a few seconds or go to Monitor for detailed process information.");
		await Page.WaitForTimeoutAsync(2000);
		Console.WriteLine("******************************************");
		Console.WriteLine("Go to monitor SIMPLE CATALOG IMPORT");
		Console.WriteLine("******************************************");
		await GoWithErrWrap(CMS_MONITOR_URL, 120);
		await ManualRefresh();
		await MonitorProcessStatueAsync("", "Simple Catalog import", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("Catalog upload complete");
		///////////////////////////////////////////////////////////////////////////////////////

		////////////////////////////////////////////////////////
		//RELEASE CATALOG
		////////////////////////////////////////////////////////
		await GoWithErrWrap(CMS_CATALOG_HOME_URL, 120);
		await CMSFilter("", TC01_CUSTOMER_ID);
		await Task.Delay(2000);//TCO1_CATALOG_SELECTOR = "\\36 2376_";
		int tried = 0;
		while (await Page.GetByText("Imported").CountAsync() > 0 && tried <= 2)
		{
			Console.WriteLine("Handle race condition that status not changed to Imported yet");
			await Page.WaitForTimeoutAsync(3000);
			await ReloadPageIfBackrop();
			tried++;
		}
		Console.WriteLine("click show more");		
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);//62376_btnShowMore
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page.WaitForTimeoutAsync(2000);
		Console.WriteLine("click Submit Catalog Chevron");//62376_tab4_link
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}tab4_link\"]").ClickAsync(locatorClickOptions);
		await Task.Delay(2000);
		//Click submit button
		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitCat\"]")).ToBeVisibleAsync(locatorVisibleAssertion);//62376_submitCat
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitCat\"]").ClickAsync();
		jobstart = DateTime.Now;
		await Page.WaitForTimeoutAsync(200);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(2000);
		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitCatalogMessage\"]")).ToContainTextAsync("Your catalog was placed in the process queue and will be submitted to your customer. Please refresh your screen (press F5) in a few seconds or go to Monitor for detailed process information.");
		await Page.WaitForTimeoutAsync(2000);
		//////////////////////////////////////////////////////////////////////////////////
		///  RELEASE CATALOG JOB
		//////////////////////////////////////////////////////////////////////////////////
		await GoWithErrWrap(CMS_MONITOR_URL, 120);
		await ManualRefresh();
		await MonitorProcessStatueAsync("", "Release Catalog", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("Catalog Released");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(4)]
	[Category("CMBTests")]
	async public Task TC04_CMB_Load_and_Release_Catalog()
	{
		//183142
		//this test has the precondition that one of tests tc01, tc02 or tc03 have been run and that there is a new version available of the catalog to the buyer
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179307
		Console.WriteLine("TC04_CMB_Load_and_Release_Catalog");

		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 60000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 60000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 60000 };
		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOptionMonitor = new LocatorAssertionsToContainTextOptions { Timeout = 30000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 60000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 60000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 60000 };
		LocatorTextContentOptions locatorTextContentOptions = new LocatorTextContentOptions { Timeout = 60000 };
		LocatorClickOptions manualRefreshClickOptions = new LocatorClickOptions { Force = true, Timeout = 60000 };
		string url = PORTAL_LOGIN;
		await Page.SetViewportSizeAsync(1600, 900);
		await SignInPortal(PORTAL_MAIN_URL, BUYER_USER1_LOGIN, BUYER_USER1_PASSWORD);
		await Expect(Page.Locator("#notificationsWidget")).ToContainTextAsync("Notifications");
		(await GetSideBarHeaderTextAsync()).Should().Contain("The Business Network");

		if (Environment == "PROD")
		{
			(await GetTopBarUserTextAsync()).Should().Contain("Buyer Admin EPAM");
		}
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 120);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Buyer Dashboard");

		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buyer Dashboard" })).ToBeVisibleAsync(locatorVisibleAssertion);

		Console.WriteLine("filter catalogs via supplier catalog id");
		await CMBFilter("", TC04_SUPPLIER_ID);
		//note on prod filtering for supplier id testsupcdo2 results in 2 catalogs! so need to be aware of locator strictness
		//could sort by status of New version available also?
		//in which case we should expect contentAllTasks to contain only 1 <div class=row>
		var count = await Page.Locator("#contentAllTasks > div[class=\"row\"]").CountAsync();

		Console.WriteLine("catalog rows on page 1 of the dashboard: " + count);
		//could assert that this is 1 here?
		await Task.Delay(4000);

		//assert catalog exists
		Console.WriteLine("assert catalog exists");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_catalog\"]")).ToContainTextAsync(TC04_SUPPLIERNAME, locatorToContainTextOptionMonitor);

		//assert new version available
		Console.WriteLine("assert that the catalog status is new  version available");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_catalog\"]")).ToContainTextAsync("New Version available", new LocatorAssertionsToContainTextOptions { Timeout = 180000 });

		Console.WriteLine("click show more");
		//await Expect(Page.Locator("[id=\"\\37 7418_allTasks_btnShowMore\"]")).ToBeVisibleAsync();
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_btnShowMore\"]")).ToBeVisibleAsync(locatorVisibleAssertion);

		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_btnShowMore\"]").ClickAsync(locatorClickOptions);

		/////////////////////////////////            CREATE WORKING VERSION                  /////////////////////////////////
		//click supplier CATALOG chevron
		await Page.Locator($"[href*=\"#{TC04_SUPPLIER_METACATID}_allTasks_tabSupplierCatalog\"]").ClickAsync(locatorClickOptions);
		Console.WriteLine("create working version");
		//create working version
		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_tabSupplierCatalog\"]").GetByText("Create Working Version").ClickAsync(locatorClickOptions);

		await Task.Delay(6000);

		DateTime jobStarted = DateTime.Now;
		Console.WriteLine("job created " + jobStarted.ToLongDateString());
		await VerifyCatalogStatus(TC04_SUPPLIER_METACATID, "Waiting for processing");

		//go to monitor
		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor LOAD CATALOG");//href = "/srvs/BuyerCatalogs/monitor/MonitorBuyer"
		Console.WriteLine("**********************************************");
		await GoWithErrWrap(CMB_MONITOR_URL, 120);
		await SetManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Load Catalog", jobStarted, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Catalog Loaded");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 120);
		/////////////////////////////////       APPROVE CATALOG ITEMS    /////////////////////////////////
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Buyer Dashboard");

		//filter catalogs 
		await CMBFilter("", TC04_SUPPLIER_ID);
		await Task.Delay(4000);

		Console.WriteLine("assert catalog status is Catalogs to approve");

		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_catalog\"]")).ToContainTextAsync("Catalog to approve");

		Console.WriteLine("click show more");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_btnShowMore\"]")).ToBeVisibleAsync(locatorVisibleAssertion);

		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_btnShowMore\"]").ClickAsync(locatorClickOptions);
		//await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Review Items" })).ToBeVisibleAsync(locatorVisibleAssertion);

		Console.WriteLine("assert that the active chevron is approve items");//#\37 7418_allTasks_navWizard > li.active
		await Expect(Page.Locator($"#{TC04_CATALOG_SELECTOR_ID}_allTasks_navWizard > li.active")).ToContainTextAsync("Approve Items");

		Console.WriteLine("click the Review Items button");//#\32 20716_allTasks_tabApproveItems > div.catalog-actions.col-lg-7.col-md-7.col-sm-8 > div > div.pull-right > a.btn.btn-success

		try
		{ //Make sure when page is loaded, it does not get stacktrace error
			await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_tabApproveItems\"]").GetByRole(AriaRole.Link, new() { Name = "Review Items" }).ClickAsync(locatorClickOptions);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		}
		catch (TimeoutException)
		{
			await ReloadIfStacktrace(false);
		}

		//wait for uiitems table
		//#uiItems
		Console.WriteLine("wait for uiitems: start " + DateTime.Now.ToLongTimeString());
		await Page.WaitForSelectorAsync("#uiItems", new PageWaitForSelectorOptions { Timeout = 60000 });
		Console.WriteLine("wait for uiitems: end " + DateTime.Now.ToLongTimeString());

		await Task.Delay(8000);

		Console.WriteLine("Page: " + Page.Url);
		//assert url like
		//https://portal.hubwoo.com/srvs/BuyerCatalogs/items/item-list?show=ACCEPTED_77418_62376&cid=62376&sid=77418&mode=approval&ignore=no
		await Expect(Page.GetByLabel("Action", new() { Exact = true })).ToBeVisibleAsync(locatorVisibleAssertion);
		await Expect(Page.GetByLabel("Comment:")).ToBeVisibleAsync(locatorVisibleAssertion);

		//the items/item-list page by default has the default action please select...
		//the comment field is empty
		//the confirm button  (#uiSubmitAction) is disabled, it becomes active when an action e.g. approve all is selected
		//the user does not need to add a comment
		//the Submit catalog link is available, but clicking this BEFORE the Confirm buttonconfirm only sends the user to the dashboard where the release catalog chevron is active
		//and no catalog status change occurs

		Console.WriteLine("Assert that the Confirm button (#uiSubmitAction) is disabled");
		await Expect(Page.Locator("#uiSubmitAction")).ToBeDisabledAsync();

		if (Environment == "PROD")
		{
			Console.WriteLine("Assert that the Submit Catalog link (#uiGoToReleaseTab) is visible");
			await Expect(Page.Locator("#uiGoToReleaseTab")).ToBeVisibleAsync(locatorVisibleAssertion);
		}

		Console.WriteLine("select the approve all action");

		IReadOnlyList<string> selectedActions = await Page.Locator("#uiTableAction").SelectOptionAsync(new[] { "approve_all" }, new LocatorSelectOptionOptions { Force = true });

		await Task.Delay(5000);

		Console.WriteLine(selectedActions.Count.ToString());

		await Task.Delay(3000);

		//check that the confirm button(uiSubmitAction) is now active after the approval action has been set
		try
		{
			Console.WriteLine("NOT DISABLED: check that the confirm button(uiSubmitAction) is now active after the approval action has been set (\"#uiSubmitAction\")).Not.ToBeDisabledAsync()");
			await Expect(Page.Locator("#uiSubmitAction")).Not.ToBeDisabledAsync();
		}
		catch (Exception ex)
		{
			Console.WriteLine("failed ToBeDisabledAsync check " + ex.Message);
		}

		try
		{
			Console.WriteLine("ENABLED check that the confirm button(uiSubmitAction) is now active after the approval action has been set (\"#uiSubmitAction\")).ToBeEnabledAsync()");
			await Expect(Page.Locator("#uiSubmitAction")).ToBeEnabledAsync();
		}
		catch (Exception ex)
		{
			Console.WriteLine("failed ToBeEnabledAsync check " + ex.Message);
		}

		//click confirm button
		Console.WriteLine("click the Confirm button (#uiSubmitAction)");
		await Task.Delay(2000);

		//this submit button is not being seen by playwright as being enabled, even though the assertion above passes
		//get this error
		/*
	waiting for Locator("#uiSubmitAction")
	-   locator resolved to <button disabled type="button" id="uiSubmitAction" oncli…>↵⇆⇆⇆⇆⇆⇆⇆⇆⇆⇆⇆⇆⇆Confirm↵⇆⇆⇆⇆⇆⇆⇆⇆⇆⇆⇆⇆</button>
	- attempting click action
	-   waiting for element to be visible, enabled and stable
	-   element is not enabled

since this works in debug, the assumption is that it is a timing based issue and added wait for uiitems table
*/
		try
		{
			Console.WriteLine("click the Confirm button with LocatorClickOptions force = true, this works , button click works in debug but not when running");
			await Page.Locator("#uiSubmitAction").ClickAsync(new LocatorClickOptions { Force = true, Timeout = 60000 });
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
		await Task.Delay(2000);

		//wait for approvals to be saved
		WaitForElementToBeHidden(Page, "#loadingScreen");

		/////////////////////////////////            SUBMIT CATALOG                /////////////////////////////////

		Console.WriteLine("click the Submit Catalog link (#uiGoToReleaseTab)");
		try
		{
			await Page.Locator("#uiGoToReleaseTab").ClickAsync(locatorClickOptions);//doesn't submit but redirects user to dashboard with the release catalog chevron for the specific catalog in focus and active
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		}
		catch (TimeoutException)
		{
			await ReloadIfStacktrace(false);
		}


		//should now be on dashboard with direct release button available
		await Task.Delay(4000);
		Console.WriteLine(Page.Url);
		await Expect(Page).ToHaveURLAsync(CMB_CATALOG_HOME_URL1);
		Console.WriteLine("on dashboard with release chevron active");
		await Expect(Page.GetByTitle("Direct Release")).ToBeVisibleAsync(locatorVisibleAssertion);

		//release catalog chevron visible

		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_navWizard\"]")).ToContainTextAsync("Release Catalog");
		Console.WriteLine("click direct release for catalog " + TC04_SUPPLIERNAME);
		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_tabReleaseCatalog\"]").GetByTitle("Direct Release").ClickAsync(locatorClickOptions);

		/////////////////////////////////     RELEASE CATALOG        /////////////////////////////////
		await Task.Delay(4000);
		//assert popup
		Console.WriteLine("direct release popup dialog displayed");
		await Expect(Page.Locator("#uiDirectRelease")).ToBeVisibleAsync(locatorVisibleAssertion);
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Direct Release" })).ToBeVisibleAsync(locatorVisibleAssertion);
		await Expect(Page.Locator("#uiDirectRelease")).ToContainTextAsync("OK");
		await Page.Locator("#uiDirectRelease").Locator("#uiDirectReleaseOk").ClickAsync(locatorClickOptions);
		jobStarted = DateTime.Now;
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page.WaitForTimeoutAsync(1000);
		//wait for dashboard
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buyer Dashboard" })).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("**********************************************");
		Console.WriteLine("go to monitor SET LIVE");
		Console.WriteLine("**********************************************");
		//go to monitor
		await GoWithErrWrap(CMB_MONITOR_URL, 120);
		await SetManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Set Live", jobStarted, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		////////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Catalog Released");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////
		//navigate to
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 120);
		Console.WriteLine("Navigate to " + CMB_CATALOG_HOME_URL);
		await Expect(Page).ToHaveURLAsync(CMB_CATALOG_HOME_URL);
		Console.WriteLine("return to dashboard assert catalog status is in production");

		//FILTER CATALOGS BY RELEASED STATUS
		await Page.GetByLabel("Status:").SelectOptionAsync(new[] { "released" });
		await Page.Locator("#uiSupplierId").FillAsync(TC04_SUPPLIER_ID);
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);

		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buyer Dashboard" })).ToBeVisibleAsync(locatorVisibleAssertion);


		Console.WriteLine("assert that status is now 'In Production'");
		//#\37 7418_allTasks_catalog
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_catalog\"]")).ToContainTextAsync("In Production", new LocatorAssertionsToContainTextOptions { Timeout = 180000 });

		Console.WriteLine("**********************************************");
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(5)]
	[Category("CMBTests")]
	async public Task TC05_CMS_View_And_Download_Diffing_Report()
	{
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179307
		//prod cms smoke test 179315
		//this test should be run after CMB case "Load and release catalog" (183142) i.e. TC04_CMB_Load_and_Release_Catalog
		//this test is Dependent upon tc01 - tc04 having been run in the correct sequence 
		//in this test the supplier uploads an updated catalog file and views the resulting diffing report
		//the updated file has 5 changes, 2 long description changes, 1 prce change and 2 generic item updates
		//
		Console.WriteLine("TC05_CMS_View_And_Download_Diffing_Report");
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 60000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 60000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOptionMonitor = new LocatorAssertionsToContainTextOptions { Timeout = 30000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 60000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 60000 };
		LocatorTextContentOptions locatorTextContentOptions = new LocatorTextContentOptions { Timeout = 60000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 60000 };
		LocatorClickOptions manualRefreshClickOptions = new LocatorClickOptions { Force = true, Timeout = 60000 };
		string url = PORTAL_LOGIN;
		await Page.SetViewportSizeAsync(1600, 900);
		await Page.GotoAsync(url, pageGotoOptions);
		Console.WriteLine("Waiting for " + url);
		int loginAttempt = 0;
		bool loginScreenRendered = false;
		while (loginScreenRendered == false && loginAttempt < 10)
		{
			try
			{
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loginScreenRendered = true;
			}
			catch
			{
				loginAttempt++;
			}
		}

		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		Console.WriteLine(Page.Url);
		DateTime thisTestStarted = DateTime.UtcNow;
		Console.WriteLine("this test started: " + thisTestStarted.ToLongTimeString());
		DateTime today = DateTime.Now;
		string CurrentDate = "";
		var process = "";
		var isCookieConsentVisible = await Page.Locator("#cookie-consent-dialog").IsVisibleAsync();
		if (isCookieConsentVisible)
		{
			await Page.GetByRole(AriaRole.Button, new() { Name = "Accept all" }).ClickAsync(locatorClickOptions);
		}

		await Page.GetByPlaceholder("Enter your user name").FillAsync(SUPPLIER_USER1_LOGIN);
		await Page.GetByPlaceholder("Enter your password").FillAsync(SUPPLIER_USER1_PASSWORD);
		await Page.Locator("#signInButtonId").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await Page.WaitForSelectorAsync("//side-bar-item[@name='Home']", new PageWaitForSelectorOptions { Timeout = 60000 });
		if (Environment == "PROD")
		{
			await Expect(Page.Locator("#notificationsWidget")).ToContainTextAsync("Notifications");
		}
				 (await GetSideBarHeaderTextAsync()).Should().Contain("The Business Network");

		//click catalogs tab
		Console.WriteLine("go to catalogs tab");
		await Page.GotoAsync(CMS_CATALOG_HOME_URL, pageGotoOptions);
		Console.WriteLine("waiting for " + CMS_CATALOG_HOME_URL);
		await Page.WaitForURLAsync(CMS_CATALOG_HOME_URL, pageWaitForUrlOptions);
		Console.WriteLine(Page.Url);
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "My Catalogs" })).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("filter catalogs via customer catalog id");
		await Page.GetByLabel("Customer ID").FillAsync(TC01_CUSTOMER_ID);//TESTCUSTCDO-0001
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
		await Task.Delay(4000);

		Console.WriteLine("click show more");

		await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Show more" })).ToBeVisibleAsync(locatorVisibleAssertion);
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Task.Delay(2000);
		Console.WriteLine("click the upload chevron");
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}tab2_link\"]").ClickAsync(locatorClickOptions);

		Console.WriteLine("select catalog xlsx file to upload: " + VIEW_AND_DOWNLOAD_DIFFING_REPORT);//VIEW_AND_DOWNLOAD_DIFFING_REPORT xlsx_prod_catalog_SCF_prod_file_updated.xlsx
		await Task.Delay(3000);

		if (Environment == "PROD")
		{
			Console.WriteLine("set input file: " + TC01_CATALOG_FILE_PATH + "xlsx_prod_catalog_SCF_prod_file_updated.xlsx");
			//62376_fileSelect  #\36 2376_fileSelect
			Console.WriteLine("TCO1_CATALOG_SELECTOR = " + TCO1_CATALOG_SELECTOR);
			Console.WriteLine($"selector: [id=\"{TCO1_CATALOG_SELECTOR}fileSelect\"]");

			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}fileSelect\"]").SetInputFilesAsync(TC01_CATALOG_FILE_PATH + "xlsx_prod_catalog_SCF_prod_file_updated.xlsx");

			await Expect(Page.GetByRole(AriaRole.Table)).ToContainTextAsync("xlsx_prod_catalog_SCF_prod_file_updated");

			Console.WriteLine("set filetype");
			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}xlsx_prod_catalog_SCF_prod_file_updated\\.zip_selectType\"]").SelectOptionAsync(new[] { "content" });
		}

		if (Environment == "QA")
		{
			Console.WriteLine("set input file: " + TC01_CATALOG_FILE_PATH + "xlsx_qa_catalog_SCF_qa_file_updated.xlsx");
			//62376_fileSelect  #\36 2376_fileSelect
			Console.WriteLine("TCO1_CATALOG_SELECTOR = " + TCO1_CATALOG_SELECTOR);
			Console.WriteLine($"selector: [id=\"{TCO1_CATALOG_SELECTOR}fileSelect\"]");

			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}fileSelect\"]").SetInputFilesAsync(TC01_CATALOG_FILE_PATH + "xlsx_qa_catalog_SCF_qa_file_updated.xlsx");

			await Expect(Page.GetByRole(AriaRole.Table)).ToContainTextAsync("xlsx_qa_catalog_SCF_qa_file_updated");

			Console.WriteLine("set filetype");
			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}xlsx_qa_catalog_SCF_qa_file_updated\\.zip_selectType\"]").SelectOptionAsync(new[] { "content" });
		}

		if (Environment == "UAT")
		{
			//not implemented
		}

		Console.WriteLine("upload catalog file");

		await Task.Delay(4000);

		await Page.GetByRole(AriaRole.Link, new() { Name = "Process files" }).ClickAsync(locatorClickOptions);
		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}uploadFileList\"]")).ToContainTextAsync("Your upload files were placed in the process queue. They will be processed as soon as possible.Please refresh your screen (press F5) in a few seconds or go to Monitor for detailed process information.");
		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor SIMPLE CATALOG IMPORT");
		Console.WriteLine("**********************************************");


		bool monitorPageRendered = false;
		int loadMonitorPageAttempts = 0;
		while (monitorPageRendered == false && loadMonitorPageAttempts < 10)
		{
			try
			{
				await Page.GotoAsync(CMS_MONITOR_URL, pageGotoOptions);
				Console.WriteLine("waiting for : " + CMS_MONITOR_URL);
				await Page.WaitForURLAsync(CMS_MONITOR_URL, pageWaitForUrlOptions);
				Console.WriteLine(Page.Url);
				monitorPageRendered = true;
			}
			catch (Exception ex)
			{
				loadMonitorPageAttempts++;
				Console.WriteLine("Issue navigating to cmb monitor Page, attempt " + loadMonitorPageAttempts.ToString());
				Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
				Console.WriteLine("exception: " + ex.Message);
			}
		}

		//assert we are on the monitor page  //#pageTitle > h4
		await Expect(Page.Locator("#pageTitle > h4")).ToContainTextAsync("Process Monitor");

		await Page.GetByLabel("Customer ID:").FillAsync(TC01_CUSTOMER_ID);
		Console.WriteLine("filter processes for Simple catalog import");
		await Page.GetByLabel("Process Type:").SelectOptionAsync(new[] { CMS_MONITOR_PROCESS_FILTER_SIMPLE_CATALOG_IMPORT });  //simple catalog type
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);

		//assert refresh monitor button is visible
		await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" })).ToBeVisibleAsync();

		Console.WriteLine("Manually Refresh monitor");
		await Page.Locator("#contentWrapper").GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
		await Page.WaitForLoadStateAsync();

		await Task.Delay(4000);
		//read first row of the itemListContainer, tbody that has the id #itemListContainer
		Console.WriteLine("assert first row has a simple catalog import process");
		process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
		Console.WriteLine("process: " + process);
		//upload catalog
		await Expect(Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)")).ToContainTextAsync("Simple Catalog import");

		var status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync();
		var startedFrom = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(3)").TextContentAsync();
		Console.WriteLine("status: " + status);

		if (status == "Finished OK" || status == "Failed")
		{
			Console.WriteLine("Status is Finished/Failed , Manually Refresh monitor");
			//bypass the actionability checks and force the click

			await Page.Locator("#contentWrapper").GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
			await Page.WaitForLoadStateAsync();

			await Task.Delay(4000);

			await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" })).ToBeVisibleAsync();
			process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
			Console.WriteLine("process: " + process);
			status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync();
			Console.WriteLine("status: " + status);
		}

		if (status == "Failed" && process == "Simple Catalog import")
		{
			Console.WriteLine("Simple Catalog import status still failed after refresh, taking a screenshot ");
			//take screenshot after clicking on failed status

			CurrentDate = $"{today.Year}{today.Month}{today.Day}{today.Hour}{today.Minute}";
			await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").ClickAsync(locatorClickOptions);
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC03_Catalog_Import_Failure" + CurrentDate + ".png"
			});
		}

		//expect status to be in processing - sometimes job completes by the time we start monitoring - await Expect(Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div")).ToContainTextAsync(new Regex(@"(\W|^)(Waiting to be processed|Currently processing)(\W|$)"));
		Console.WriteLine("Monitor loop attempt limit:" + TC01_CATALOG_LOAD_ATTEMPTS.ToString());
		int attempt = 0;
		while (attempt <= TC01_CATALOG_LOAD_ATTEMPTS)
		{
			try
			{
				Console.WriteLine("Manually Refresh monitor");
				await Page.Locator("#contentWrapper").GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
				await Page.WaitForLoadStateAsync();

				await Task.Delay(4000);
				attempt++;
				Console.WriteLine("Waiting for catalog file to upload : " + attempt.ToString());
				status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync();
				Console.WriteLine("status: " + status);
				//#itemListContainer > tr:nth-child(1) > td:nth-child(2)
				process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync();
				Console.WriteLine("process: " + process);
				startedFrom = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(3)").TextContentAsync();
				Console.WriteLine("current job started at " + startedFrom);
				var processid = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(1) > a").TextContentAsync();
				Console.WriteLine("processid:" + processid);
				//date time string  format displayed in cms monitor is different to cmb monitor
				//but this is due to the language profile selection for the 2 different users
				//English (United States) => 6/5/2024 (11:34 AM)  => M/d/yyyy (h:mm tt)
				//English (United kingdom) => 05/06/2024 (11:34) => dd/MM/yyyy (HH:mm)
				//cmb user at the time of writing has English USA = 	4/27/2024 (11:12 AM)  	5/4/2024 (10:06 AM)   4/30/2024 (9:36 PM) => m/d/yyyy (hh:mm tt)
				//cms user at the time of writing has English UK = 02/05/2024 (19:33)  27/04/2024 (09:50)    ==> dd/MM/yyyy (HH:mm)

				DateTime currentProcessStarted = DateTime.Now;
				try
				{
					currentProcessStarted = DateTime.ParseExact(startedFrom, "dd/MM/yyyy (HH:mm)", CultureInfo.InvariantCulture);
				}
				catch
				{
					currentProcessStarted = DateTime.ParseExact(startedFrom, "M/d/yyyy (h:mm tt)", CultureInfo.InvariantCulture);
				}
				//ui time of local pc test run on (currentProcessStarted) , different to the time on the server (thisTestStarted), so remove 5 minutes
				if (process == "Simple Catalog import" && status == "Finished OK" && currentProcessStarted > thisTestStarted.AddMinutes(-8))
				{
					//exit while loop
					break;
				}
				else
				{
					Console.WriteLine("still waiting, don't break...");
					Console.WriteLine("currentProcessStarted: " + currentProcessStarted.ToLongTimeString());
					Console.WriteLine("thisTestStarted.AddMinutes(-8)) " + thisTestStarted.AddMinutes(-8).ToLongTimeString());
				}

				await Expect(Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div")).ToContainTextAsync("Finished OK", locatorToContainTextOptionMonitor);

			}
			catch (Exception ex)
			{
				Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
				Console.WriteLine(ex.Message);
				status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync();
				Console.WriteLine("status: " + status);
				process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
				Console.WriteLine("PROCESS: " + process);
				if (attempt == 5 || attempt == 10 || attempt == 15 || attempt == 20 || attempt == 25)
				{
					Console.WriteLine("attempt: " + attempt.ToString() + " reloading monitor page and resetting filter");
					//sometimes the monitor screen just locks up and can only be reactivated by reloading the page
					monitorPageRendered = false;
					loadMonitorPageAttempts = 0;
					while (monitorPageRendered == false && loadMonitorPageAttempts < 10)
					{
						try
						{
							await Page.GotoAsync(CMS_MONITOR_URL, pageGotoOptions);
							Console.WriteLine("waiting for : " + CMS_MONITOR_URL);
							await Page.WaitForURLAsync(CMS_MONITOR_URL, pageWaitForUrlOptions);
							Console.WriteLine(Page.Url);
							monitorPageRendered = true;
						}
						catch (Exception exception)
						{
							loadMonitorPageAttempts++;
							Console.WriteLine("Issue navigating to cmb monitor Page, attempt " + loadMonitorPageAttempts.ToString());
							Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
							Console.WriteLine("exception: " + exception.Message);
						}
					}


					await Page.GetByLabel("Customer ID:").FillAsync(TC01_CUSTOMER_ID);
					Console.WriteLine("filter processes for Simple catalog import");
					await Page.GetByLabel("Process Type:").SelectOptionAsync(new[] { CMS_MONITOR_PROCESS_FILTER_SIMPLE_CATALOG_IMPORT });  //simple catalog type
					await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
					await Task.Delay(3000);
				}
				if (attempt >= TC01_CATALOG_LOAD_ATTEMPTS || status == "Failed")
				{
					Console.WriteLine("Catalog upload Failed");
					throw ex;
				}
			}
		}

		if (attempt >= TC01_CATALOG_LOAD_ATTEMPTS || status != "Finished OK")
		{
			throw new Exception("Number of attempts to wait forCatalog upload job to finish exceeded");
		}

		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("Catalog upload complete");
		///////////////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("go back to dashboard");
		Console.WriteLine("go to catalogs tab");
		Console.WriteLine("waiting for " + CMS_CATALOG_HOME_URL);
		await Page.GotoAsync(CMS_CATALOG_HOME_URL, pageGotoOptions);
		await Page.WaitForURLAsync(CMS_CATALOG_HOME_URL, pageWaitForUrlOptions);
		Console.WriteLine(Page.Url);
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "My Catalogs" })).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("filter catalogs via customer catalog id" + TC01_CUSTOMER_ID);
		await Page.GetByLabel("Customer ID").FillAsync(TC01_CUSTOMER_ID);//TESTCUSTCDO-0001

		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
		await Task.Delay(4000);

		Console.WriteLine("click cog wheel icon");


		if (Environment == "PROD")
		{
			try
			{
				//note force =  true does not override the fact that an element is hidden!!!
				//#\(62376\)_catalog > div > div.settings > a
				Console.WriteLine("cog wheel menu selector: " + $"#\\{TC05_CATALOG_SELECTOR_ID}\\_catalog > div > div.settings > a.dropdown-toggle");
				await Page.EvalOnSelectorAsync($"#\\(62376\\)_catalog > div > div.settings > a.dropdown-toggle", "el => el.click()");
				//await Page.EvalOnSelectorAsync($"#\\{TC05_CATALOG_SELECTOR_ID}\\_catalog > div > div.settings > a.dropdown-toggle", "el => el.click()");
				//https://playwright.dev/dotnet/docs/api/class-page#page-eval-on-selector
				//https://github.com/microsoft/playwright-dotnet/issues/923
				//here we want to click the hidden li item as cannot get the menu to display using playwright, this method should be used with care as all actionability checks are ignored
				//and we are emulating a Javascript click event on the element referenced by the selector

				Console.WriteLine("assert diffing report option is available");
				await Expect(Page.Locator($"#\\(62376\\)_catalog > div > div.settings.open > ul.dropdown-menu > li:nth-child(6)  > a")).ToContainTextAsync("Diffing Report");

				Console.WriteLine("select Diffing Report menu option");
				//#\(62376\)_catalog > div > div.settings.open > ul > li:nth-child(6) > a

				//url for prod diffing in this test https://portal.hubwoo.com/srvs/CatalogManager/diffing/diffing-supplier?cid=62376&sid=77418&show=UI_77418_62376_BME

				Console.WriteLine("click Diffing report option");
				Console.WriteLine("Expect to be to be redirected to page  https://portal.hubwoo.com/srvs/CatalogManager/diffing/diffing-supplier?cid=62376&sid=77418&show=UI_77418_62376_BME ");
				await Page.EvalOnSelectorAsync($"#\\(62376\\)_catalog > div > div.settings.open > ul.dropdown-menu > li:nth-child(6)  > a", "el => el.click()");
			}
			catch
			{
				Console.WriteLine("Cannot click cog wheel menu!!");

				await Page.GotoAsync("https://portal.hubwoo.com/srvs/CatalogManager/diffing/diffing-supplier?cid=62376&sid=77418&show=UI_77418_62376_BME", pageGotoOptions);
			}
		}

		if (Environment == "QA")
		{
			try
			{
				//note force =  true does not override the fact that an element is hidden!!!
				//#\(62376\)_catalog > div > div.settings > a
				Console.WriteLine("cog wheel menu selector: " + $"#\\{TC05_CATALOG_SELECTOR_ID}\\_catalog > div > div.settings > a.dropdown-toggle");
				await Page.EvalOnSelectorAsync($"#\\(63045\\)_catalog > div > div.settings > a.dropdown-toggle", "el => el.click()");
				//await Page.EvalOnSelectorAsync($"#\\{TC05_CATALOG_SELECTOR_ID}\\_catalog > div > div.settings > a.dropdown-toggle", "el => el.click()");
				//https://playwright.dev/dotnet/docs/api/class-page#page-eval-on-selector
				//https://github.com/microsoft/playwright-dotnet/issues/923
				//here we want to click the hidden li item as cannot get the menu to display using playwright, this method should be used with care as all actionability checks are ignored
				//and we are emulating a Javascript click event on the element referenced by the selector

				Console.WriteLine("assert diffing report option is available");
				await Expect(Page.Locator($"#\\(63045\\)_catalog > div > div.settings.open > ul.dropdown-menu > li:nth-child(6)  > a")).ToContainTextAsync("Diffing Report");

				Console.WriteLine("select Diffing Report menu option");
				//#\(62376\)_catalog > div > div.settings.open > ul > li:nth-child(6) > a

				//url for qa diffing in this testhttps://portal.qa.hubwoo.com/srvs/CatalogManager/diffing/diffing-supplier?cid=63045&sid=237593&show=UI_237593_63045_BME

				Console.WriteLine("click Diffing report option");

				Console.WriteLine("Expect to be to be redirected to page https://portal.qa.hubwoo.com/srvs/CatalogManager/diffing/diffing-supplier?cid=63045&sid=237593&show=UI_237593_63045_BME");
				await Page.EvalOnSelectorAsync($"#\\(63045\\)_catalog > div > div.settings.open > ul.dropdown-menu > li:nth-child(6)  > a", "el => el.click()");
			}
			catch
			{
				Console.WriteLine("Cannot click cog wheel menu!!");

				await Page.GotoAsync("https://portal.qa.hubwoo.com/srvs/CatalogManager/diffing/diffing-supplier?cid=63045&sid=237593&show=UI_237593_63045_BME", pageGotoOptions);
			}
		}

		//wait for loadingScreen to disappear
		Console.WriteLine("waiting for loadingScreen to disappear");
		attempt = 0;
		var isLoadingScreenVisible = await Page.Locator("#loadingScreen").IsVisibleAsync();
		while (isLoadingScreenVisible && attempt < 10)
		{
			try
			{
				await Expect(Page.Locator("#loadingScreen")).Not.ToBeVisibleAsync(locatorVisibleAssertion);
				isLoadingScreenVisible = false;
				Console.WriteLine("loadingScreen gone");
				break;
			}
			catch
			{
				attempt++;
				isLoadingScreenVisible = await Page.Locator("#loadingScreen").IsVisibleAsync();
			}
		}


		Console.WriteLine("page :" + Page.Url);


		Console.WriteLine("assert diffing report values");

		if (Environment == "PROD")
		{
			//expected number of rows 8 ?

			//what is the order of the diffing report items, there is no sort applied to the sql in the diffing code
			//expect supplier item number to be one of 01-081[.]9010|01-655[.]1000|02-570[.]1000|02-570[.]9020|10-020[.]5000|10-020[.]5001|11-015[.]5000|11-015[.]9025
			await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Diffing Report | TESTCUSTCDO 1");
			var itemId = await Page.Locator("#mainRow-0 > td:nth-child(1) > a").TextContentAsync();
			Console.WriteLine("check for diff item on #mainRow-0: " + itemId);
			if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemId))
			{
				throw new Exception($"unexpected supplier item number {itemId} in #mainRow-0");
			}

			itemId = await Page.Locator("#mainRow-1 > td:nth-child(1) > a").TextContentAsync();
			Console.WriteLine("check for diff item on #mainRow-1: " + itemId);
			if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemId))
			{
				throw new Exception($"unexpected supplier item number {itemId} in #mainRow-1");
			}

			itemId = await Page.Locator("#mainRow-2 > td:nth-child(1) > a").TextContentAsync();
			Console.WriteLine("check for diff item on #mainRow-2: " + itemId);
			if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemId))
			{
				throw new Exception($"unexpected supplier item number {itemId} in #mainRow-2");
			}
			itemId = await Page.Locator("#mainRow-3 > td:nth-child(1) > a").TextContentAsync();
			Console.WriteLine("check for diff item on #mainRow-3: " + itemId);
			if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemId))
			{
				throw new Exception($"unexpected supplier item number {itemId} in #mainRow-3");
			}

			itemId = await Page.Locator("#mainRow-4 > td:nth-child(1) > a").TextContentAsync();
			Console.WriteLine("check for diff item on #mainRow-4: " + itemId);
			if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemId))
			{
				throw new Exception($"unexpected supplier item number {itemId} in #mainRow-4");
			}

			itemId = await Page.Locator("#mainRow-5 > td:nth-child(1) > a").TextContentAsync();
			Console.WriteLine("check for diff item on #mainRow-5: " + itemId);
			if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemId))
			{
				throw new Exception($"unexpected supplier item number {itemId} in #mainRow-5");
			}

			itemId = await Page.Locator("#mainRow-6 > td:nth-child(1) > a").TextContentAsync();
			Console.WriteLine("check for diff item on #mainRow-6: " + itemId);
			if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemId))
			{
				throw new Exception($"unexpected supplier item number {itemId} in #mainRow-6");
			}

			itemId = await Page.Locator("#mainRow-7 > td:nth-child(1) > a").TextContentAsync();
			Console.WriteLine("check for diff item on #mainRow-7: " + itemId);
			if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemId))
			{
				throw new Exception($"unexpected supplier item number {itemId} in #mainRow-7");
			}
		}

		if (Environment == "QA")
		{
			await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Diffing Report | SV Buyer");
			var itemId = await Page.Locator("#mainRow-0 > td:nth-child(1) > a").TextContentAsync();
			Console.WriteLine("check for diff item on #mainRow-0: " + itemId);
			//expect item supplier number to be one of 11-015[.]5000|11-015[.]9025
			if (!QaSupplierItemIdValidator.IsExpectedQASupplierItemNumber(itemId))
			{
				throw new Exception($"unexpected supplier item number {itemId} in #mainRow-0");
			}

			itemId = await Page.Locator("#mainRow-0 > td:nth-child(1) > a").TextContentAsync();
			Console.WriteLine("check for diff item on #mainRow-1: " + itemId);
			if (!QaSupplierItemIdValidator.IsExpectedQASupplierItemNumber(itemId))
			{
				throw new Exception($"unexpected supplier item number {itemId} in #mainRow-1");
			}
		}

		if (Environment == "UAT")
		{
			//not implemented
		}

		Console.WriteLine("select csv diffing report type");
		await Page.GetByLabel("Select Diffing File Type:").SelectOptionAsync(new[] { "CSV" });
		Console.WriteLine("download csv diffing report type");

		//download the file
		var waitForDownloadTask = Page.WaitForDownloadAsync();
		await Page.GetByRole(AriaRole.Button, new() { Name = "Download Report" }).ClickAsync(locatorClickOptions);

		var download = await waitForDownloadTask;
		var fileName = downloadPath + "TC05_" + download.SuggestedFilename;

		Console.WriteLine("****************************************************");
		Console.WriteLine("File downloaded to " + fileName);
		Console.WriteLine("****************************************************");

		// Wait for the download process to complete and save the downloaded file somewhere
		await download.SaveAsAsync(fileName);


		Console.WriteLine("select xlsx diffing report type");
		await Page.GetByLabel("Select Diffing File Type:").SelectOptionAsync(new[] { "XLSX" });

		await Task.Delay(4000);

		Console.WriteLine("download xlsx diffing report type");
		await Page.GetByRole(AriaRole.Button, new() { Name = "Download Report" }).ClickAsync(locatorClickOptions);

		//assert on monitor page with new Template Export job
		Console.WriteLine("**********************************************");
		Console.WriteLine("go to monitor TEMPLATE EXPORT");
		Console.WriteLine("**********************************************");
		await Task.Delay(2000);
		monitorPageRendered = false;
		loadMonitorPageAttempts = 0;
		while (monitorPageRendered == false && loadMonitorPageAttempts < 10)
		{
			try
			{
				await Page.GotoAsync(CMS_MONITOR_URL, pageGotoOptions);
				Console.WriteLine("waiting for : " + CMS_MONITOR_URL);
				await Page.WaitForURLAsync(CMS_MONITOR_URL, pageWaitForUrlOptions);
				Console.WriteLine(Page.Url);
				monitorPageRendered = true;
			}
			catch (Exception ex)
			{
				loadMonitorPageAttempts++;
				Console.WriteLine("Issue navigating to cmb monitor Page, attempt " + loadMonitorPageAttempts.ToString());
				Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
				Console.WriteLine("exception: " + ex.Message);
			}
		}
		await Page.GetByLabel("Process Type:").SelectOptionAsync(new[] { "31" });
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
		await Task.Delay(3000);
		Console.WriteLine("Manually Refresh monitor");
		await Page.Locator("#contentWrapper").GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
		await Page.WaitForLoadStateAsync();

		//read first row of the itemListContainer, tbody that has the id #itemListContainer
		Console.WriteLine("assert first monitor list row has a Template Export job");//31
																																								 //CurrentDate = $"{today.Day}/{today.Month}/{today.Year}";
		var date = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(3)").TextContentAsync(locatorTextContentOptions);

		//int firstBracket = date.IndexOf("(");
		//string actionDate = date.Substring(0, firstBracket).Trim();   //remove characters after the first (  e.g. 4/17/2024 (3:38 PM)
		Console.WriteLine("date for last Template Export job :" + date);

		//expect first row in table to have new process
		await Expect(Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)")).ToContainTextAsync("Template Export", locatorToContainTextOption);
		//get process and status of the item in row 1 of the table
		status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
		process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
		Console.WriteLine("status: " + status);

		if (status == "Finished OK" || status == "Failed")
		{
			Console.WriteLine("Status is Finished/Failed , Manually Refresh monitor");
			await Page.Locator("#contentWrapper").GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
			await Page.WaitForLoadStateAsync();
			await Task.Delay(4000);

			await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" })).ToBeVisibleAsync(locatorVisibleAssertion);
			//get process and status of the item in row 1 of the table
			status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
			process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
			Console.WriteLine("status: " + status);
		}

		if (status == "Finished OK")
		{
			Console.WriteLine("Template Export status succeeded after refresh, taking a screenshot ");
			//take screenshot after clicking on failed status
			DateTime dateNow = DateTime.Now;
			string CurrentDate1 = $"{dateNow.Year}{dateNow.Month}{dateNow.Day}{dateNow.Hour}{dateNow.Minute}";
			await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").ClickAsync(locatorClickOptions);
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC05_CMS_View_And_Download_Catalog_DiffingReport1_" + CurrentDate1 + ".png"
			});
		}

		attempt = 0;
		while (attempt <= MONITOR_CHECK_ATTEMPTS)
		{
			try
			{
				Console.WriteLine("Manually Refresh monitor");
				attempt++;
				await Page.Locator("#contentWrapper").GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
				await Page.WaitForLoadStateAsync();

				await Task.Delay(4000);

				Console.WriteLine("Waiting for Template Export: " + attempt.ToString());
				//get process and status of the item in row 1 of the table
				process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
				status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
				Console.WriteLine("status: " + status);
				Console.WriteLine("process: " + process);
				var processid = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(1) > a").TextContentAsync();
				Console.WriteLine("processid:" + processid);
				startedFrom = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(3)").TextContentAsync();
				Console.WriteLine("current job started at " + startedFrom);
				//date time string  format displayed in cms monitor is different to cmb monitor
				//but this is due to the language profile selection for the 2 different users
				//English (United States) => 6/5/2024 (11:34 AM)  => M/d/yyyy (h:mm tt)
				//English (United kingdom) => 05/06/2024 (11:34) => dd/MM/yyyy (HH:mm)
				//cmb user at the time of writing has English USA = 	4/27/2024 (11:12 AM)  	5/4/2024 (10:06 AM)   4/30/2024 (9:36 PM) => m/d/yyyy (hh:mm tt)
				//cms user at the time of writing has English UK = 02/05/2024 (19:33)  27/04/2024 (09:50)    ==> dd/MM/yyyy (HH:mm)

				DateTime currentProcessStarted = DateTime.Now;
				try
				{
					currentProcessStarted = DateTime.ParseExact(startedFrom, "dd/MM/yyyy (HH:mm)", CultureInfo.InvariantCulture);
				}
				catch
				{
					currentProcessStarted = DateTime.ParseExact(startedFrom, "M/d/yyyy (h:mm tt)", CultureInfo.InvariantCulture);
				}
				//ui time of local pc test run on (currentProcessStarted) , different to the time on the server (thisTestStarted), so remove 5 minutes

				if (process == "Template Export" && status == "Finished OK" && currentProcessStarted > thisTestStarted.AddMinutes(-8))
				{
					break;
				}
				else
				{
					Console.WriteLine("still waiting, don't break...");
					Console.WriteLine("currentProcessStarted: " + currentProcessStarted.ToLongTimeString());
					Console.WriteLine("thisTestStarted.AddMinutes(-8)) " + thisTestStarted.AddMinutes(-8).ToLongTimeString());
				}
				await Expect(Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div")).ToContainTextAsync("Finished OK", locatorToContainTextOptionMonitor);
			}
			catch (Exception ex)
			{
				Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
				Console.WriteLine(ex.Message);
				status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
				Console.WriteLine("status: " + status);
				process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
				Console.WriteLine("PROCESS: " + process);
				if (attempt == 5 || attempt == 10 || attempt == 15 || attempt == 20 || attempt == 25)
				{
					Console.WriteLine("attempt: " + attempt.ToString() + " reloading monitor page and resetting filter");
					//sometimes the monitor screen just locks up and can only be reactivated by reloading the page
					monitorPageRendered = false;
					loadMonitorPageAttempts = 0;
					while (monitorPageRendered == false && loadMonitorPageAttempts < 10)
					{
						try
						{
							await Page.GotoAsync(CMS_MONITOR_URL, pageGotoOptions);
							Console.WriteLine("waiting for : " + CMS_MONITOR_URL);
							await Page.WaitForURLAsync(CMS_MONITOR_URL, pageWaitForUrlOptions);
							Console.WriteLine(Page.Url);
							monitorPageRendered = true;
						}
						catch (Exception exception)
						{
							loadMonitorPageAttempts++;
							Console.WriteLine("Issue navigating to cmb monitor Page, attempt " + loadMonitorPageAttempts.ToString());
							Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
							Console.WriteLine("exception: " + exception.Message);
						}
					}
					await Page.GetByLabel("Process Type:").SelectOptionAsync(new[] { "31" });
					await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
					await Task.Delay(3000);
				}
				if (attempt >= MONITOR_CHECK_ATTEMPTS || status == "Failed")
				{
					Console.WriteLine("Template Export failed");
					throw ex;
				}
			}
		}

		if (attempt >= MONITOR_CHECK_ATTEMPTS || status != "Finished OK")
		{
			throw new Exception("Number of attempts to wait for Template Export job to finish exceeded");
		}
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("****************************************************");
		Console.WriteLine("Template Export succeeded");
		Console.WriteLine("****************************************************");
		///////////////////////////////////////////////////////////////////////////////////////


		Console.WriteLine("goto download template chevron");
		await Page.GotoAsync(CMS_CATALOG_HOME_URL, pageGotoOptions);
		Console.WriteLine("waiting for " + CMS_CATALOG_HOME_URL);
		await Page.WaitForURLAsync(CMS_CATALOG_HOME_URL, pageWaitForUrlOptions);
		Console.WriteLine(Page.Url);
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "My Catalogs" })).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("filter catalogs via customer catalog id");
		await Page.GetByLabel("Customer ID").FillAsync(TC01_CUSTOMER_ID);//TESTCUSTCDO-0001

		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);

		await Task.Delay(4000);

		await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Show more" })).ToBeVisibleAsync(locatorVisibleAssertion);
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Task.Delay(2000);

		await Page.GetByRole(AriaRole.Link, new() { Name = "Upload Files" }).ClickAsync(locatorClickOptions);
		await Page.GetByRole(AriaRole.Link, new() { Name = "Download Template" }).ClickAsync(locatorClickOptions);
		Console.WriteLine("download diffing reports");
		Console.WriteLine("Refresh the download list");
		await Page.GetByText("Refresh", new() { Exact = true }).ClickAsync(locatorClickOptions);
		await Task.Delay(2000);
		//example selectors
		//#\36 2376_DownloadFilesContent > li:nth-child(1) > a  download type
		//#\36 2376_DownloadFilesContent > li:nth-child(1) > span   date
		await Expect(Page.Locator($"#{TCO1_CATALOG_SELECTOR}DownloadFilesContent > li:nth-child(1) > a")).ToContainTextAsync("Diffing Report");

		//download the file
		var waitForDownloadTask2 = Page.WaitForDownloadAsync();//get link e.g https://portal.hubwoo.com/srvs/omnicontent/templatearchive/9574769_SCF_77418_62376_295.1_2024.04.18_file.zip

		var link = await Page.Locator($"#{TCO1_CATALOG_SELECTOR}DownloadFilesContent > li:nth-child(1) > a").GetAttributeAsync("href");

		Console.WriteLine("Download " + link);
		await Page.Locator($"#{TCO1_CATALOG_SELECTOR}DownloadFilesContent > li:nth-child(1) > a").ClickAsync(locatorClickOptions);

		var download2 = await waitForDownloadTask2;
		var fileName2 = downloadPath + "TC05_" + download2.SuggestedFilename;

		Console.WriteLine("****************************************************");
		Console.WriteLine("File downloaded to " + fileName2);
		Console.WriteLine("****************************************************");

		// Wait for the download process to complete and save the downloaded file somewhere
		await download2.SaveAsAsync(fileName2);


		Console.WriteLine("unzip file " + fileName2);
		//unzip the file to downloadPath 
		string excelFileName = $"{downloadPath}{ExtractZipFile(fileName2, downloadPath)}";

		Console.WriteLine("use ClosedXML to validate contents of file: " + excelFileName);
		//assert contents of xlsx
		Console.WriteLine("assert contents of xlsx file " + excelFileName);
		try
		{
			using var xlWorkbook = new XLWorkbook(excelFileName);
			var ws1 = xlWorkbook.Worksheet(1);
			if (Environment == "PROD")
			{
				//the order here is not always the same as the sql used to create the report does not have an order by clause
				var itemid = ws1.Cell("A2").GetValue<string>();
				Console.WriteLine("assert itemd id cell A2:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A2");
				}

				itemid = ws1.Cell("A3").GetValue<string>();
				Console.WriteLine("assert itemd id cell A3:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A3");
				}

				itemid = ws1.Cell("A4").GetValue<string>();
				Console.WriteLine("assert itemd id cell A4:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A4");
				}

				itemid = ws1.Cell("A5").GetValue<string>();
				Console.WriteLine("assert itemd id cell A5:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A5");
				}

				itemid = ws1.Cell("A6").GetValue<string>();
				Console.WriteLine("assert itemd id cell A6:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A6");
				}

				itemid = ws1.Cell("A7").GetValue<string>();
				Console.WriteLine("assert itemd id cell A7:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A7");
				}


				itemid = ws1.Cell("A8").GetValue<string>();
				Console.WriteLine("assert itemd id cell A8:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A8");
				}


				itemid = ws1.Cell("A9").GetValue<string>();
				Console.WriteLine("assert itemd id cell A9:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A9");
				}

				itemid = ws1.Cell("A10").GetValue<string>();
				Console.WriteLine("assert itemd id cell A10:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A10");
				}

				itemid = ws1.Cell("A11").GetValue<string>();
				Console.WriteLine("assert itemd id cell A11:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A11");
				}

				itemid = ws1.Cell("A12").GetValue<string>();
				Console.WriteLine("assert itemd id cell A12:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A12");
				}

				itemid = ws1.Cell("A13").GetValue<string>();
				Console.WriteLine("assert itemd id cell A13:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A13");
				}

				itemid = ws1.Cell("A14").GetValue<string>();
				Console.WriteLine("assert itemd id cell A14:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A14");
				}

				itemid = ws1.Cell("A15").GetValue<string>();
				Console.WriteLine("assert itemd id cell A15:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A15");
				}

				itemid = ws1.Cell("A16").GetValue<string>();
				Console.WriteLine("assert itemd id cell A16:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A16");
				}

				itemid = ws1.Cell("A17").GetValue<string>();
				Console.WriteLine("assert itemd id cell A17:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A17");
				}

				itemid = ws1.Cell("A18").GetValue<string>();
				Assert.That(itemid, Is.EqualTo(""));

				var field = ws1.Cell("E2").GetValue<string>();
				Console.WriteLine("assert field id cell E2:" + field);
				if (!field.Trim().Contains("Type of Attachment 1") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Picture"))
				{
					throw new Exception("Unexpected Field in cell E2");
				}

				field = ws1.Cell("E3").GetValue<string>();
				Console.WriteLine("assert field id cell E3:" + field);
				if (!field.Trim().Contains("Type of Attachment 1") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Picture"))
				{
					throw new Exception("Unexpected Field in cell E3");
				}

				field = ws1.Cell("E4").GetValue<string>();
				Console.WriteLine("assert field id cell E4:" + field);
				if (!field.Trim().Contains("Type of Attachment 1") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Picture"))
				{
					throw new Exception("Unexpected Field in cell E4");
				}

				field = ws1.Cell("E5").GetValue<string>();
				Console.WriteLine("assert field id cell E5:" + field);
				if (!field.Trim().Contains("Type of Attachment 1") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Picture"))
				{
					throw new Exception("Unexpected Field in cell E5");
				}

				field = ws1.Cell("E6").GetValue<string>();
				Console.WriteLine("assert field id cell E6:" + field);
				if (!field.Trim().Contains("Type of Attachment 1") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Picture"))
				{
					throw new Exception("Unexpected Field in cell E6");
				}

				field = ws1.Cell("E7").GetValue<string>();
				Console.WriteLine("assert field id cell E7:" + field);
				if (!field.Trim().Contains("Type of Attachment 1") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Picture"))
				{
					throw new Exception("Unexpected Field in cell E7");
				}

				field = ws1.Cell("E8").GetValue<string>();
				Console.WriteLine("assert field id cell E8:" + field);
				if (!field.Trim().Contains("Type of Attachment 1") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Picture"))
				{
					throw new Exception("Unexpected Field in cell E8");
				}

				field = ws1.Cell("E9").GetValue<string>();
				Console.WriteLine("assert field id cell E9:" + field);
				if (!field.Trim().Contains("Type of Attachment 1") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Picture"))
				{
					throw new Exception("Unexpected Field in cell E9");
				}

				field = ws1.Cell("E10").GetValue<string>();
				Console.WriteLine("assert field id cell E10:" + field);
				if (!field.Trim().Contains("Type of Attachment 1") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Picture"))
				{
					throw new Exception("Unexpected Field in cell E10");
				}

				field = ws1.Cell("E11").GetValue<string>();
				Console.WriteLine("assert field id cell E11:" + field);
				if (!field.Trim().Contains("Type of Attachment 1") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Picture"))
				{
					throw new Exception("Unexpected Field in cell E11");
				}

				field = ws1.Cell("E12").GetValue<string>();
				Console.WriteLine("assert field id cell E12:" + field);
				if (!field.Trim().Contains("Type of Attachment 1") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Picture"))
				{
					throw new Exception("Unexpected Field in cell E12");
				}
			}
			if (Environment == "QA")
			{
				var itemid = ws1.Cell("A2").GetValue<string>();
				Console.WriteLine("assert itemd id cell A2:" + itemid);
				if (!QaSupplierItemIdValidator.IsExpectedQASupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A2");
				}

				itemid = ws1.Cell("A3").GetValue<string>();
				Console.WriteLine("assert itemd id cell A3:" + itemid);
				if (!QaSupplierItemIdValidator.IsExpectedQASupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A3");
				}

				itemid = ws1.Cell("A4").GetValue<string>();
				Console.WriteLine("assert itemd id cell A4:" + itemid);
				if (!QaSupplierItemIdValidator.IsExpectedQASupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A4");
				}

				itemid = ws1.Cell("A5").GetValue<string>();
				Console.WriteLine("assert itemd id cell A5:" + itemid);
				if (!QaSupplierItemIdValidator.IsExpectedQASupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A5");
				}

				itemid = ws1.Cell("A6").GetValue<string>();
				Console.WriteLine("assert itemd id cell A6:" + itemid);
				if (!QaSupplierItemIdValidator.IsExpectedQASupplierItemNumber(itemid))
				{
					throw new Exception($"unexpected supplier item number {itemid} in cell A6");
				}

				itemid = ws1.Cell("A7").GetValue<string>();
				Console.WriteLine("assert itemd id cell A7 is empty:" + itemid);
				Assert.That(itemid, Is.EqualTo(""));

				var field = ws1.Cell("E2").GetValue<string>();
				Console.WriteLine("assert field id cell E2:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem"))
				{
					throw new Exception("Unexpected Field in cell E2");
				}

				field = ws1.Cell("E3").GetValue<string>();
				Console.WriteLine("assert field id cell E3:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem"))
				{
					throw new Exception("Unexpected Field in cell E3");
				}

				field = ws1.Cell("E4").GetValue<string>();
				Console.WriteLine("assert field id cell E4:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem"))
				{
					throw new Exception("Unexpected Field in cell E4");
				}

				field = ws1.Cell("E5").GetValue<string>();
				Console.WriteLine("assert field id cell E5:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem"))
				{
					throw new Exception("Unexpected Field in cell E5");
				}

				field = ws1.Cell("E6").GetValue<string>();
				Console.WriteLine("assert field id cell E6:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem"))
				{
					throw new Exception("Unexpected Field in cell E6");
				}
			}
		}
		catch (Exception e)
		{
			Console.WriteLine("exception asserting contents of diffing report file: " + e.Message);
		}
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(6)]
	[Category("CMBTests")]
	async public Task TC06_CMS_View_Catalog_Item_And_Download_Catalog_Report()
	{
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179307
		//prod cms smoke test 179341
		Console.WriteLine("TC06_CMS_View_Catalog_Item_And_Download_Catalog_Report");


		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 60000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 60000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 60000 };
		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 60000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 60000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 60000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 60000 };
		LocatorClickOptions manualRefreshClickOptions = new LocatorClickOptions { Force = true, Timeout = 60000 };
		LocatorTextContentOptions locatorTextContentOptions = new LocatorTextContentOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOptionMonitor = new LocatorAssertionsToContainTextOptions { Timeout = 30000 };

		//SUPPLIER_USER1_LOGIN = "EPAM_TS2";
		//SUPPLIER_USER1_PASSWORD = "xsw23edc";
		string url = PORTAL_LOGIN; //https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F (prod)

		await Page.SetViewportSizeAsync(1600, 900);
		Console.WriteLine("SetViewportSizeAsync(1600, 900)");
		await Page.GotoAsync(url, pageGotoOptions);
		Console.WriteLine("Waiting for " + url);
		int loginAttempt = 0;
		bool loginScreenRendered = false;
		while (loginScreenRendered == false && loginAttempt < 10)
		{
			try
			{
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loginScreenRendered = true;
			}
			catch
			{
				loginAttempt++;
			}
		}

		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		DateTime thisTestStarted = DateTime.UtcNow;
		Console.WriteLine("this test started: " + thisTestStarted.ToLongTimeString());
		var isCookieConsentVisible = await Page.Locator("#cookie-consent-dialog").IsVisibleAsync();
		if (isCookieConsentVisible)
		{
			await Page.GetByRole(AriaRole.Button, new() { Name = "Accept all" }).ClickAsync(locatorClickOptions);
		}

		await Page.GetByPlaceholder("Enter your user name").FillAsync(SUPPLIER_USER1_LOGIN);
		await Page.GetByPlaceholder("Enter your password").FillAsync(SUPPLIER_USER1_PASSWORD);
		await Page.Locator("#signInButtonId").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//wait for page to load https://portal.hubwoo.com/main/

		await Page.WaitForSelectorAsync("//side-bar-item[@name='Home']", new PageWaitForSelectorOptions { Timeout = 60000 });
		if (Environment == "PROD")
		{
			await Expect(Page.Locator("#notificationsWidget")).ToContainTextAsync("Notifications");
		}

			(await GetSideBarHeaderTextAsync()).Should().Contain("The Business Network");
		Console.WriteLine(Page.Url);
		//click catalogs tab
		Console.WriteLine("click catalogs tab");

		await Page.GotoAsync(CMS_CATALOG_HOME_URL, pageGotoOptions);
		await Page.WaitForURLAsync(CMS_CATALOG_HOME_URL, pageWaitForUrlOptions);

		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("My Catalogs");
		Console.WriteLine(Page.Url);

		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "My Catalogs" })).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("filter catalogs via customer catalog id");
		await Page.GetByLabel("Customer ID").FillAsync(TC01_CUSTOMER_ID);//TC01_CUSTOMER_ID = "TESTCUSTCDO-0001";

		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("My Catalogs");
		await Task.Delay(4000);//fails when this is removed

		//TCO1_CATALOG_SELECTOR = "\\36 2376_" (PROD)
		await Expect(Page.GetByText(TC01_CUSTOMERNAME)).ToBeVisibleAsync();
		Console.WriteLine("expand and upload");
		await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Show more" })).ToBeVisibleAsync(locatorVisibleAssertion);

		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Task.Delay(2000);

		Console.WriteLine("goto item list page");
		//#\(62376\)_catalog > div > div.col-lg-9.col-md-10.col-sm-10 > div:nth-child(3) > div > span > a
		Console.WriteLine("");
		if (Environment == "PROD")
		{
			await Page.GotoAsync("https://portal.hubwoo.com/srvs/CatalogManager/supplier/item-list?show=UI_77418_62376_BME&cid=62376&sid=77418&enter=true", pageGotoOptions);
			await Page.WaitForURLAsync("https://portal.hubwoo.com/srvs/CatalogManager/supplier/item-list?show=UI_77418_62376_BME&cid=62376&sid=77418&enter=true", pageWaitForUrlOptions);
		}

		if (Environment == "UAT")
		{
			//await Page.GotoAsync("https://portal.uat.hubwoo.com/srvs/CatalogManager/supplier/item-list?show=UI_77418_62376_BME&cid=62376&sid=77418&enter=true");
		}

		if (Environment == "QA")
		{
			await Page.GotoAsync("https://portal.qa.hubwoo.com/srvs/CatalogManager/supplier/item-list?show=UI_237593_63045_BME&cid=63045&sid=237593&enter=true");
			await Page.WaitForURLAsync("https://portal.qa.hubwoo.com/srvs/CatalogManager/supplier/item-list?show=UI_237593_63045_BME&cid=63045&sid=237593&enter=true", pageWaitForUrlOptions);
		}

		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		Console.WriteLine(Page.Url);
		Console.WriteLine("Select Catalog Version");
		await Page.Locator("#ddlCatalogVersion").SelectOptionAsync(new[] { "CUS_RELEASED" });

		await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Download Report" })).ToBeVisibleAsync();
		Console.WriteLine("Click Download Report Button");
		await Task.Delay(4000);
		try
		{
			await Page.Locator("#uiDownloadReport").ClickAsync(locatorClickOptions);
		}
		catch
		{

		}

		await Expect(Page.Locator("#downloadCatalogMessage > span")).ToContainTextAsync("Your download was placed into");

		/////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor CATALOG DOWNLOAD JOB");
		Console.WriteLine("**********************************************");
		bool monitorPageRendered = false;
		int loadMonitorPageAttempts = 0;
		while (monitorPageRendered == false && loadMonitorPageAttempts < 10)
		{
			try
			{
				await Page.GotoAsync(CMS_MONITOR_URL, pageGotoOptions);
				Console.WriteLine("waiting for : " + CMS_MONITOR_URL);
				await Page.WaitForURLAsync(CMS_MONITOR_URL, pageWaitForUrlOptions);
				Console.WriteLine(Page.Url);
				monitorPageRendered = true;
			}
			catch (Exception ex)
			{
				loadMonitorPageAttempts++;
				Console.WriteLine("Issue navigating to cmb monitor Page, attempt " + loadMonitorPageAttempts.ToString());
				Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
				Console.WriteLine("exception: " + ex.Message);
			}
		}

		//assert we are on the monitor page  //#pageTitle > h4
		await Expect(Page.Locator("#pageTitle > h4")).ToContainTextAsync("Process Monitor");

		await Page.Locator("#ddlRefreshTime").SelectOptionAsync(new[] { "0" });//dont want to autorefresh, do it after each attempt timesout
		Console.WriteLine(Page.Url);
		await Page.GetByLabel("Customer ID:").FillAsync(TC01_CUSTOMER_ID);
		await Page.GetByLabel("Process Type:").SelectOptionAsync(new[] { "all" });
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);

		//assert refresh monitor button is visible
		await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" })).ToBeVisibleAsync();

		Console.WriteLine("Manually Refresh monitor");
		await Page.Locator("#contentWrapper").GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
		await Page.WaitForLoadStateAsync();
		//wait for ajax call to complete

		//read first row of the itemListContainer, tbody that has the id #itemListContainer
		Console.WriteLine("assert first of monitor list row has a Catalog Download Job");
		//process the monitor page
		DateTime today = DateTime.Now;
		string CurrentDate = $"{today.Day}/{today.Month}/{today.Year}";
		var date = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(3)").TextContentAsync(locatorTextContentOptions);
		int firstBracket = date.IndexOf("(");
		string actionDate = date.Substring(0, firstBracket).Trim();   //remove characters after the first (  e.g. 4/17/2024 (3:38 PM)
		Console.WriteLine("date for latest Catalog Download job :" + date);

		if (CurrentDate != actionDate)
		{
			Console.WriteLine("action date for last Catalog Download Job different from expected: " + CurrentDate + " actual: " + date);
		}

		//expect first row in table to have new process
		try
		{
			await Expect(Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)")).ToContainTextAsync("Catalog Download Job", locatorToContainTextOption);
		}
		catch
		{
			Console.WriteLine("Manually Refresh monitor");
			await Page.Locator("#contentWrapper").GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
			await Page.WaitForLoadStateAsync();
		}
		//get process and status of the item in row 1 of the table
		var status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
		var process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
		var startedFrom = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(3)").TextContentAsync();
		Console.WriteLine("status: " + status);

		if (status == "Finished OK" || status == "Failed")
		{
			Console.WriteLine("Status is Finished/Failed , Manually Refresh monitor");
			await Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
			await Page.WaitForLoadStateAsync();

			await Task.Delay(4000);

			await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" })).ToBeVisibleAsync(locatorVisibleAssertion);
			//get process and status of the item in row 1 of the table
			status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
			process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
			Console.WriteLine("status: " + status);
		}

		if (status == "Finished OK")
		{
			Console.WriteLine("Catalog Download Job status succeeded after refresh, taking a screenshot ");
			//take screenshot after clicking on failed status
			DateTime dateNow = DateTime.Now;
			string CurrentDate1 = $"{dateNow.Year}{dateNow.Month}{dateNow.Day}{dateNow.Hour}{dateNow.Minute}";
			await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").ClickAsync(locatorClickOptions);
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC06_CMS_Download_Catalog_Report_Failure1_" + CurrentDate1 + ".png"
			});
		}

		int attempt = 0;
		while (attempt <= MONITOR_CHECK_ATTEMPTS)
		{
			try
			{
				Console.WriteLine("Manually Refresh monitor");
				attempt++;
				await Page.Locator("#contentWrapper").GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
				await Page.WaitForLoadStateAsync();

				await Task.Delay(4000);

				Console.WriteLine("Waiting for Catalog Download Job: " + attempt.ToString());
				//get process and status of the item in row 1 of the table
				process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
				status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
				Console.WriteLine("status: " + status);
				Console.WriteLine("process: " + process);
				var processid = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(1) > a").TextContentAsync();
				Console.WriteLine("processid:" + processid);
				startedFrom = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(3)").TextContentAsync();
				Console.WriteLine("current job started at " + startedFrom);
				//date time string  format displayed in cms monitor is different to cmb monitor
				//but this is due to the language profile selection for the 2 different users
				//English (United States) => 6/5/2024 (11:34 AM)  => M/d/yyyy (h:mm tt)
				//English (United kingdom) => 05/06/2024 (11:34) => dd/MM/yyyy (HH:mm)
				//cmb user at the time of writing has English USA = 	4/27/2024 (11:12 AM)  	5/4/2024 (10:06 AM)   4/30/2024 (9:36 PM) => m/d/yyyy (hh:mm tt)
				//cms user at the time of writing has English UK = 02/05/2024 (19:33)  27/04/2024 (09:50)    ==> dd/MM/yyyy (HH:mm)

				DateTime currentProcessStarted = DateTime.Now;
				try
				{
					currentProcessStarted = DateTime.ParseExact(startedFrom, "dd/MM/yyyy (HH:mm)", CultureInfo.InvariantCulture);
				}
				catch
				{
					currentProcessStarted = DateTime.ParseExact(startedFrom, "M/d/yyyy (h:mm tt)", CultureInfo.InvariantCulture);
				}
				//ui time of local pc test run on (currentProcessStarted) , different to the time on the server (thisTestStarted), so remove 5 minutes

				if (process == "Catalog Download Job" && status == "Finished OK" && currentProcessStarted > thisTestStarted.AddMinutes(-8))
				{
					break;
				}
				else
				{
					Console.WriteLine("still waiting, don't break...");
					Console.WriteLine("currentProcessStarted: " + currentProcessStarted.ToLongTimeString());
					Console.WriteLine("thisTestStarted.AddMinutes(-8)) " + thisTestStarted.AddMinutes(-8).ToLongTimeString());
				}
				await Expect(Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div")).ToContainTextAsync("Finished OK", locatorToContainTextOptionMonitor);
			}
			catch (Exception ex)
			{
				Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
				Console.WriteLine(ex.Message);
				status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);

				Console.WriteLine("status: " + status);
				process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
				Console.WriteLine("PROCESS: " + process);
				if (attempt == 5 || attempt == 10 || attempt == 15 || attempt == 20 || attempt == 25)
				{
					Console.WriteLine("attempt: " + attempt.ToString() + " reloading monitor page and resetting filter");
					//sometimes the monitor screen just locks up and can only be reactivated by reloading the page
					monitorPageRendered = false;
					loadMonitorPageAttempts = 0;
					while (monitorPageRendered == false && loadMonitorPageAttempts < 10)
					{
						try
						{
							await Page.GotoAsync(CMS_MONITOR_URL, pageGotoOptions);
							Console.WriteLine("waiting for : " + CMS_MONITOR_URL);
							await Page.WaitForURLAsync(CMS_MONITOR_URL, pageWaitForUrlOptions);
							Console.WriteLine(Page.Url);
							monitorPageRendered = true;
						}
						catch (Exception exception)
						{
							loadMonitorPageAttempts++;
							Console.WriteLine("Issue navigating to cmb monitor Page, attempt " + loadMonitorPageAttempts.ToString());
							Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
							Console.WriteLine("exception: " + exception.Message);
						}
					}
					await Page.GetByLabel("Customer ID:").FillAsync(TC01_CUSTOMER_ID);
					await Page.GetByLabel("Process Type:").SelectOptionAsync(new[] { "all" });
					await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
					await Task.Delay(3000);
				}
				if (attempt >= MONITOR_CHECK_ATTEMPTS || status == "Failed")
				{
					Console.WriteLine("Catalog Download Job failed");
					throw ex;
				}
			}
		}

		if (attempt >= MONITOR_CHECK_ATTEMPTS || status != "Finished OK")
		{
			throw new Exception("Number of attempts to wait for Catalog Download Job to finish exceeded");
		}
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Catalog Download Job succeeded");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("Go to dashboard , is there a new download link?");
		await Page.GotoAsync(CMS_CATALOG_HOME_URL, pageGotoOptions);
		await Page.WaitForURLAsync(CMS_CATALOG_HOME_URL, pageWaitForUrlOptions);

		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("My Catalogs");
		Console.WriteLine(Page.Url);

		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "My Catalogs" })).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("filter catalogs via customer catalog id");
		await Page.GetByLabel("Customer ID").FillAsync(TC01_CUSTOMER_ID);//TC01_CUSTOMER_ID = "TESTCUSTCDO-0001";

		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("My Catalogs");
		await Task.Delay(5000);//fails when this is removed
		Console.WriteLine("click show more");
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Task.Delay(2000);
		Console.WriteLine("click the upload download template chevron");

		//TCO1_CATALOG_SELECTOR = "\\36 2376_" (PROD)
		await Expect(Page.GetByText(TC01_CUSTOMERNAME)).ToBeVisibleAsync();
		Console.WriteLine("click show more and then the download template chevron");
		await Page.GetByRole(AriaRole.Link, new() { Name = "Download Template" }).ClickAsync(locatorClickOptions);//62376_tab1_link

		await Task.Delay(3000);
		Console.WriteLine("Refresh the download list");
		await Page.GetByText("Refresh", new() { Exact = true }).ClickAsync(locatorClickOptions);
		//example selectors
		//#\36 2376_DownloadFilesContent > li:nth-child(1) > a  download type
		//#\36 2376_DownloadFilesContent > li:nth-child(1) > span   date
		await Expect(Page.Locator($"#{TCO1_CATALOG_SELECTOR}DownloadFilesContent > li:nth-child(1) > a")).ToContainTextAsync("Catalog Download Job");

		var downloadlinkdate = await Page.Locator($"#{TCO1_CATALOG_SELECTOR}DownloadFilesContent > li:nth-child(1) > span").TextContentAsync(locatorTextContentOptions);
		int firstSpace = date.IndexOf(" ");
		string downloadlinkDate = downloadlinkdate.Substring(1, (firstBracket - 1)).Trim();   //remove characters from (18/04/2024 18:31:05)  to get 18/04/2024

		//assert downloadlinkDate  and CurrentDate dates match

		//download the file
		var waitForDownloadTask = Page.WaitForDownloadAsync();

		//get link e.g https://portal.hubwoo.com/srvs/omnicontent/templatearchive/21230447_catalog_browser.zip

		var link = await Page.Locator($"#{TCO1_CATALOG_SELECTOR}DownloadFilesContent > li:nth-child(1) > a").GetAttributeAsync("href");

		Console.WriteLine("Download " + link);
		await Page.Locator($"#{TCO1_CATALOG_SELECTOR}DownloadFilesContent > li:nth-child(1) > a").ClickAsync(locatorClickOptions);

		var download = await waitForDownloadTask;
		var fileName = downloadPath + "TC06_" + download.SuggestedFilename;

		Console.WriteLine("File downloaded to " + fileName);

		// Wait for the download process to complete and save the downloaded file somewhere
		await download.SaveAsAsync(fileName);

		Console.WriteLine("TO DO MANUALLY CONFIRM: A csv with same name as the zip file is extracted\r\nThe file contains following headers:\r\n- Item ID\r\n- Classification\r\n- Short Description\r\n- Long Description\r\n- Price\r\n- Orderunit\r\n- Content Unit");

		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}


	[Test, Order(7)]
	[Category("CMBTests")]
	async public Task TC07_CMS_Catalog_Import_With_Attachments()
	{
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179307
		//prod cms smoke test 179314
		Console.WriteLine("TC07_CMS_Catalog_Import_With_Attachments");
		//on prod this test takes less than 10 seconds but the process is not being correctly identified as having completed!?
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 60000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 60000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 60000 };
		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 60000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 60000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 60000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 60000 };
		LocatorClickOptions manualRefreshClickOptions = new LocatorClickOptions { Force = true, Timeout = 60000 };
		LocatorTextContentOptions locatorTextContentOptions = new LocatorTextContentOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOptionMonitor = new LocatorAssertionsToContainTextOptions { Timeout = 30000 };

		//SUPPLIER_USER1_LOGIN = "EPAM_TS2";
		//SUPPLIER_USER1_PASSWORD = "xsw23edc";
		string url = PORTAL_LOGIN; //https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F (prod)

		await Page.SetViewportSizeAsync(1600, 900);
		await SignInPortal(SUPPLIER_USER1_LOGIN, SUPPLIER_USER1_PASSWORD);
		(await GetSideBarHeaderTextAsync()).Should().Contain("The Business Network");
		await GoWithErrWrap(CMS_CATALOG_HOME_URL, 120);
		await CMSFilter("", TC01_CUSTOMER_ID);
		await Task.Delay(2000);

		//TCO1_CATALOG_SELECTOR = "\\36 2376_" (PROD)
		await Expect(Page.GetByText(TC01_CUSTOMERNAME, new() { Exact = true })).ToBeVisibleAsync();
		Console.WriteLine("expand and upload");
		await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Show more" })).ToBeVisibleAsync(locatorVisibleAssertion);
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(2000);
		await Page.GetByRole(AriaRole.Link, new() { Name = "Upload Files" }).ClickAsync(locatorClickOptions);//62376_tab2_link
		await Page.WaitForTimeoutAsync(1500);
		Console.WriteLine("select attachments zip file to upload " + CATALOG_IMPORT_WITH_ATTACHMENTS_FILE);  //baseAttachmentUpload.zip (prod)
																																																				 //add the attachments file and set type
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}fileSelect\"]").SetInputFilesAsync(new[] { TC01_CATALOG_FILE_PATH + CATALOG_IMPORT_WITH_ATTACHMENTS_FILE });
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
		await Page.WaitForTimeoutAsync(1000);
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}fileSelect\"]").SetInputFilesAsync(new[] { TC01_CATALOG_FILE_PATH + CATALOG_IMPORT_WITH_ATTACHMENTS_CATALOG });//
		await Expect(Page.Locator($"#{TCO1_CATALOG_SELECTOR}uploadFileList")).ToContainTextAsync(CATALOG_IMPORT_WITH_ATTACHMENTS_FILE);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
		await Page.WaitForTimeoutAsync(1000);
		await Expect(Page.Locator($"#{TCO1_CATALOG_SELECTOR}uploadFileList")).ToContainTextAsync(CATALOG_IMPORT_WITH_ATTACHMENTS_CATALOG.Replace(".xlsx", ".zip"));
		await Task.Delay(2000);
		//set upload types
		Console.WriteLine("set upload types");
		//example selector: 62376_baseAttachmentUpload.zip_selectType
		Console.WriteLine("1: attachment");
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}{CATALOG_IMPORT_WITH_ATTACHMENTS_FILE}_selectType\"]").SelectOptionAsync(new[] { "attachment" });
		await Task.Delay(2000);
		Console.WriteLine("2: content");
		//example selector : 62376_xlsx_prod_catalog_SCF_prod_file_base_attachmentUpload.zip_selectType
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}{CATALOG_IMPORT_WITH_ATTACHMENTS_CATALOG.Replace(".xlsx", ".zip")}_selectType\"]").SelectOptionAsync(new[] { "content" });
		//CATALOG_IMPORT_WITH_ATTACHMENTS_CATALOG = "";
		Console.WriteLine("Click Process Files");
		await Task.Delay(4000);
		await Page.Locator($"#{TCO1_CATALOG_SELECTOR}tab2 > form > div.form-group > div > a.btn.btn-primary").ClickAsync(new LocatorClickOptions { Force = true, Timeout = 60000 });
		DateTime jobStarted = DateTime.Now;
		await Page.WaitForTimeoutAsync(200);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page.WaitForTimeoutAsync(1000);
		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}uploadFileList\"]")).ToContainTextAsync("Your upload files were placed in the process queue");
		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor ATTACHMENT PROCESSING");
		Console.WriteLine("**********************************************");
		await GoWithErrWrap(CMS_MONITOR_URL, 120);
		await ManualRefresh();
		await MonitorProcessStatueAsync("", "Attachment processing", jobStarted, TC04_SUPPLIERNAME, "", "Finished OK");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("Attachment processing succeeded");
		///////////////////////////////////////////////////////////////////////////////////////
		await MonitorProcessStatueAsync("", "Simple Catalog import", jobStarted, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("Simple Catalog import succeeded");
		///////////////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(8)]
	[Category("CMBTests")]
	async public Task TC08_CMS_Supplier_Side_Check_Routine()
	{
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179307
		//prod cms smoke test 179340
		Console.WriteLine("TC08_CMS_Supplier_Side_Check_Routine");
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 60000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 60000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOptionMonitor = new LocatorAssertionsToContainTextOptions { Timeout = 30000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 60000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 60000 };
		LocatorTextContentOptions locatorTextContentOptions = new LocatorTextContentOptions { Timeout = 60000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 60000 };
		LocatorClickOptions manualRefreshClickOptions = new LocatorClickOptions { Force = true, Timeout = 60000 };
		string url = PORTAL_LOGIN;
		await Page.SetViewportSizeAsync(1600, 900);
		await Page.GotoAsync(url, pageGotoOptions);
		Console.WriteLine("Waiting for " + url);
		int loginAttempt = 0;
		bool loginScreenRendered = false;
		while (loginScreenRendered == false && loginAttempt < 10)
		{
			try
			{
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loginScreenRendered = true;
			}
			catch
			{
				loginAttempt++;
			}
		}

		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		Console.WriteLine(Page.Url);
		DateTime thisTestStarted = DateTime.UtcNow;
		Console.WriteLine("this test started: " + thisTestStarted.ToLongTimeString());
		var isCookieConsentVisible = await Page.Locator("#cookie-consent-dialog").IsVisibleAsync();
		if (isCookieConsentVisible)
		{
			await Page.GetByRole(AriaRole.Button, new() { Name = "Accept all" }).ClickAsync(locatorClickOptions);
		}

		await Page.GetByPlaceholder("Enter your user name").FillAsync(SUPPLIER_USER1_LOGIN);
		await Page.GetByPlaceholder("Enter your password").FillAsync(SUPPLIER_USER1_PASSWORD);
		await Page.Locator("#signInButtonId").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//wait for page to load https://portal.hubwoo.com/main/

		await Page.WaitForSelectorAsync("//side-bar-item[@name='Home']", new PageWaitForSelectorOptions { Timeout = 60000 });
		if (Environment == "PROD")
		{
			await Expect(Page.Locator("#notificationsWidget")).ToContainTextAsync("Notifications");
		}

			(await GetSideBarHeaderTextAsync()).Should().Contain("The Business Network");

		//click catalogs tab
		Console.WriteLine("go to catalogs tab");
		await Page.GotoAsync(CMS_CATALOG_HOME_URL, pageGotoOptions);
		Console.WriteLine("waiting for " + CMS_CATALOG_HOME_URL);
		await Page.WaitForURLAsync(CMS_CATALOG_HOME_URL, pageWaitForUrlOptions);
		Console.WriteLine(Page.Url);
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "My Catalogs" })).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("filter catalogs via customer catalog id");
		await Page.GetByLabel("Customer ID").FillAsync(TC01_CUSTOMER_ID);

		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);

		await Task.Delay(4000);

		await Expect(Page.GetByText(TC01_CUSTOMERNAME)).ToBeVisibleAsync();
		Console.WriteLine("expand and upload");
		Console.WriteLine("click show more");
		await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Show more" })).ToBeVisibleAsync(locatorVisibleAssertion);
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Task.Delay(2000);

		await Page.GetByRole(AriaRole.Link, new() { Name = "Upload Files" }).ClickAsync(locatorClickOptions);
		//SUPPLIER_CHECK_ROUTINE_FILE  xlsx_prod_catalog_SCF_prod_file_base_checkroutine_supplier.xlsx
		Console.WriteLine("select catalog xlsx file to upload");

		if (Environment == "PROD")
		{

			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}fileSelect\"]").SetInputFilesAsync(new[] { TC01_CATALOG_FILE_PATH + "xlsx_prod_catalog_SCF_prod_file_base_checkroutine_supplier.xlsx" });
			await Expect(Page.GetByRole(AriaRole.Table)).ToContainTextAsync("xlsx_prod_catalog_SCF_prod_file_base_checkroutine_supplier");
			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}xlsx_prod_catalog_SCF_prod_file_base_checkroutine_supplier\\.zip_selectType\"]").SelectOptionAsync(new[] { "content" });
		}

		if (Environment == "QA")
		{

			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}fileSelect\"]").SetInputFilesAsync(new[] { TC01_CATALOG_FILE_PATH + "xlsx_qa_catalog_SCF_qa_file_base_checkroutine_supplier.xlsx" });
			await Expect(Page.GetByRole(AriaRole.Table)).ToContainTextAsync("xlsx_qa_catalog_SCF_qa_file_base_checkroutine_supplier");
			await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}xlsx_qa_catalog_SCF_qa_file_base_checkroutine_supplier\\.zip_selectType\"]").SelectOptionAsync(new[] { "content" });
		}

		if (Environment == "UAT")
		{
			//not implemented
		}


		Console.WriteLine("upload catalog file");

		await Task.Delay(4000);

		await Page.GetByRole(AriaRole.Link, new() { Name = "Process files" }).ClickAsync(locatorClickOptions);

		DateTime jobStarted = DateTime.Now;
		Console.WriteLine("job created " + jobStarted.ToLongDateString());

		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}uploadFileList\"]")).ToContainTextAsync("Your upload files were placed in the process queue. They will be processed as soon as possible.Please refresh your screen (press F5) in a few seconds or go to Monitor for detailed process information.");
		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor SIMPLE CATALOG IMPORT");
		Console.WriteLine("**********************************************");
		bool monitorPageRendered = false;
		int loadMonitorPageAttempts = 0;
		while (monitorPageRendered == false && loadMonitorPageAttempts < 10)
		{
			try
			{
				await Page.GotoAsync(CMS_MONITOR_URL, pageGotoOptions);
				Console.WriteLine("waiting for : " + CMS_MONITOR_URL);
				await Page.WaitForURLAsync(CMS_MONITOR_URL, pageWaitForUrlOptions);
				Console.WriteLine(Page.Url);
				monitorPageRendered = true;
			}
			catch (Exception exception)
			{
				loadMonitorPageAttempts++;
				Console.WriteLine("Issue navigating to cmb monitor Page, attempt " + loadMonitorPageAttempts.ToString());
				Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
				Console.WriteLine("exception: " + exception.Message);
			}
		}

		await Page.GetByLabel("Customer ID:").FillAsync(TC01_CUSTOMER_ID);
		Console.WriteLine("filter processes for Simple catalog import");
		await Page.GetByLabel("Process Type:").SelectOptionAsync(new[] { CMS_MONITOR_PROCESS_FILTER_SIMPLE_CATALOG_IMPORT });  //simple catalog type
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);

		//assert refresh monitor button is visible
		await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" })).ToBeVisibleAsync();

		Console.WriteLine("Manually Refresh monitor");
		await Page.Locator("#contentWrapper").GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
		await Page.WaitForLoadStateAsync();

		await Task.Delay(4000);

		//read first row of the itemListContainer, tbody that has the id #itemListContainer
		Console.WriteLine("assert first row has a simple catalog import process");

		//upload catalog
		await Expect(Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)")).ToContainTextAsync("Simple Catalog import");

		//get process and status of the item in row 1 of the table
		//simpleCatalogImportProcessId = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(1)").TextContentAsync(locatorTextContentOptions);
		var status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
		var process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
		var startedFrom = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(3)").TextContentAsync();
		Console.WriteLine("status: " + status);

		if (status == "Finished OK" || status == "Failed")
		{
			Console.WriteLine("Status is Finished/Failed , Manually Refresh monitor");
			await Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
			await Page.WaitForLoadStateAsync();

			await Task.Delay(4000);

			await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" })).ToBeVisibleAsync(locatorVisibleAssertion);
			//get process and status of the item in row 1 of the table
			status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
			process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
			Console.WriteLine("status: " + status);
		}

		if (status == "Finished OK")
		{
			Console.WriteLine("Simple Catalog import status succeeded after refresh, taking a screenshot ");
			//take screenshot after clicking on failed status
			DateTime dateNow = DateTime.Now;
			string CurrentDate1 = $"{dateNow.Year}{dateNow.Month}{dateNow.Day}{dateNow.Hour}{dateNow.Minute}";
			try
			{
				await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").ClickAsync(locatorClickOptions);
			}
			catch (Exception e)
			{
				if (e.Message.Contains("<div class=\"modal-backdrop fade in\">"))
				{
					await Page.EvalOnSelectorAsync("body > div.modal-backdrop.fade", "el => el.style.display = 'none'");
					await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").ClickAsync(locatorClickOptions);
				}
			}
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC19_Supplier_Check_Routine_Failure" + CurrentDate1 + ".png"
			});
		}

		int attempt = 0;
		while (attempt <= MONITOR_CHECK_ATTEMPTS)
		{
			try
			{
				Console.WriteLine("Manually Refresh monitor");
				attempt++;
				await Page.Locator("#contentWrapper").GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
				await Page.WaitForLoadStateAsync();
				await Task.Delay(4000);

				Console.WriteLine("Waiting for Simple Catalog import: " + attempt.ToString());
				//get process and status of the item in row 1 of the table
				process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
				status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
				Console.WriteLine("status: " + status);
				Console.WriteLine("process: " + process);
				var processid = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(1) > a").TextContentAsync();
				Console.WriteLine("processid:" + processid);
				startedFrom = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(3)").TextContentAsync();
				Console.WriteLine("current job started at " + startedFrom);
				//date time string  format displayed in cms monitor is different to cmb monitor
				//but this is due to the language profile selection for the 2 different users
				//English (United States) => 6/5/2024 (11:34 AM)  => M/d/yyyy (h:mm tt)
				//English (United kingdom) => 05/06/2024 (11:34) => dd/MM/yyyy (HH:mm)
				//cmb user at the time of writing has English USA = 	4/27/2024 (11:12 AM)  	5/4/2024 (10:06 AM)   4/30/2024 (9:36 PM) => m/d/yyyy (hh:mm tt)
				//cms user at the time of writing has English UK = 02/05/2024 (19:33)  27/04/2024 (09:50)    ==> dd/MM/yyyy (HH:mm)

				DateTime currentProcessStarted = DateTime.Now;
				try
				{
					currentProcessStarted = DateTime.ParseExact(startedFrom, "dd/MM/yyyy (HH:mm)", CultureInfo.InvariantCulture);
				}
				catch
				{
					currentProcessStarted = DateTime.ParseExact(startedFrom, "M/d/yyyy (h:mm tt)", CultureInfo.InvariantCulture);
				}
				//ui time of local pc test run on (currentProcessStarted) , different to the time on the server (thisTestStarted), so remove 5 minutes
				if (process == "Simple Catalog import" && status == "Failed" && currentProcessStarted > thisTestStarted.AddMinutes(-8))
				{
					break;
				}
				else
				{
					Console.WriteLine("still waiting, don't break...");
					Console.WriteLine("currentProcessStarted: " + currentProcessStarted.ToLongTimeString());
					Console.WriteLine("thisTestStarted.AddMinutes(-8)) " + thisTestStarted.AddMinutes(-8).ToLongTimeString());
				}
				await Expect(Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div")).ToContainTextAsync("Finished OK", locatorToContainTextOptionMonitor);
			}
			catch (Exception ex)
			{
				Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
				Console.WriteLine(ex.Message);
				status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
				Console.WriteLine("status: " + status);
				process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
				Console.WriteLine("PROCESS: " + process);
				if (attempt == 5 || attempt == 10 || attempt == 15 || attempt == 20 || attempt == 25)
				{
					Console.WriteLine("attempt: " + attempt.ToString() + " reloading monitor page and resetting filter");
					//sometimes the monitor screen just locks up and can only be reactivated by reloading the page
					monitorPageRendered = false;
					loadMonitorPageAttempts = 0;
					while (monitorPageRendered == false && loadMonitorPageAttempts < 10)
					{
						try
						{
							await Page.GotoAsync(CMS_MONITOR_URL, pageGotoOptions);
							Console.WriteLine("waiting for : " + CMS_MONITOR_URL);
							await Page.WaitForURLAsync(CMS_MONITOR_URL, pageWaitForUrlOptions);
							Console.WriteLine(Page.Url);
							monitorPageRendered = true;
						}
						catch (Exception exception)
						{
							loadMonitorPageAttempts++;
							Console.WriteLine("Issue navigating to cmb monitor Page, attempt " + loadMonitorPageAttempts.ToString());
							Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
							Console.WriteLine("exception: " + exception.Message);
						}
					}
					await Page.GetByLabel("Customer ID:").FillAsync(TC01_CUSTOMER_ID);
					await Page.GetByLabel("Process Type:").SelectOptionAsync(new[] { CMS_MONITOR_PROCESS_FILTER_SIMPLE_CATALOG_IMPORT });  //simple catalog type
					await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
					await Task.Delay(3000);
				}
				if (attempt >= MONITOR_CHECK_ATTEMPTS || status == "Finished OK")
				{
					Console.WriteLine("Simple Catalog import SUCEEDED BY EXPECTED IT TO FAIL");
					throw ex;
				}
			}
		}

		if (attempt >= MONITOR_CHECK_ATTEMPTS || status != "Failed")
		{
			throw new Exception("Number of attempts to wait for Simple Catalog import job to finish exceeded");
		}
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Simple Catalog Import failed as expected");
		Console.WriteLine("**********************************************");

		//click the failed process 

		Console.WriteLine("expand row 1 of the  monitor ");
		//Click the Error Correction link on the Simple Catalog Import process in the monitor tab
		//expand the process row by clicking on the row or any td in the row
		await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(1)").ClickAsync(locatorClickOptions);

		//assert go to error link in column 6 of row 2
		//#detail-9575705 > td > div > div.process-detail.bg-danger > div:nth-child(5) > p > a

		//https://portal.hubwoo.com/srvs/CatalogManager/GoToErRep?cid=62376&sid=77418
		Console.WriteLine("click the goto error correction link in the detail row for the failed simple catalog import job");
		//itemListContainer has a mainrow and a detail row

		if (Environment == "PROD")
		{
			await Expect(Page.Locator("#itemListContainer > tr:nth-child(2) > td > div.detail-wrapper > div.process-detail.bg-danger > div:nth-child(5) > p > a > strong")).ToContainTextAsync("Error Correction");

			await Page.Locator("#itemListContainer > tr:nth-child(2) > td > div.detail-wrapper > div.process-detail.bg-danger > div:nth-child(5) > p > a").ClickAsync(locatorClickOptions);

			await Expect(Page.Locator($"#{TCO1_CATALOG_SELECTOR}Chevron3")).ToHaveAttributeAsync("class", "error-correction active", new LocatorAssertionsToHaveAttributeOptions { Timeout = 60000 });
			await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}tab3_link\"]")).ToContainTextAsync("Error Correction (2)");
		}

		if (Environment == "QA")
		{
			await Expect(Page.Locator("#itemListContainer > tr:nth-child(2) > td > div.detail-wrapper > div.process-detail.bg-danger > div:nth-child(5) > p > a > strong")).ToContainTextAsync("Error Correction");

			await Page.Locator("#itemListContainer > tr:nth-child(2) > td > div.detail-wrapper > div.process-detail.bg-danger > div:nth-child(5) > p > a").ClickAsync(locatorClickOptions);

			await Expect(Page.Locator($"#{TCO1_CATALOG_SELECTOR}Chevron3")).ToHaveAttributeAsync("class", "error-correction active", new LocatorAssertionsToHaveAttributeOptions { Timeout = 60000 });
			await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}tab3_link\"]")).ToContainTextAsync("Error Correction (2)");
		}


		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}


	[Test, Order(9)]
	[Category("CMBTests")]
	async public Task TC09_CMS_Download_Catalog_File()
	{
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179307
		//prod cms smoke test 179311
		Console.WriteLine("TC09_CMS_Download_Catalog_File");

		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 60000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 60000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 60000 };
		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 60000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 60000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 60000 };
		LocatorTextContentOptions locatorTextContentOptions = new LocatorTextContentOptions { Timeout = 60000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 60000 };
		LocatorClickOptions manualRefreshClickOptions = new LocatorClickOptions { Force = true, Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOptionMonitor = new LocatorAssertionsToContainTextOptions { Timeout = 30000 };
		//SUPPLIER_USER1_LOGIN = "EPAM_TS2";
		//SUPPLIER_USER1_PASSWORD = "xsw23edc";
		string url = PORTAL_LOGIN;  //https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F (prod)

		await Page.SetViewportSizeAsync(1600, 900);
		Console.WriteLine("SetViewportSizeAsync(1600, 900)");
		await Page.GotoAsync(url, pageGotoOptions);
		Console.WriteLine("Waiting for " + url);
		int loginAttempt = 0;
		bool loginScreenRendered = false;
		while (loginScreenRendered == false && loginAttempt < 10)
		{
			try
			{
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loginScreenRendered = true;
			}
			catch
			{
				Console.WriteLine("login screen , attempt " + loginAttempt.ToString());
				loginAttempt++;
			}
		}

		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		DateTime thisTestStarted = DateTime.UtcNow;
		Console.WriteLine("this test started: " + thisTestStarted.ToLongTimeString());
		Console.WriteLine("cookie consent");
		var isCookieConsentVisible = await Page.Locator("#cookie-consent-dialog").IsVisibleAsync();
		if (isCookieConsentVisible)
		{
			await Page.GetByRole(AriaRole.Button, new() { Name = "Accept all" }).ClickAsync(locatorClickOptions);
		}

		Console.WriteLine("logging in: " + SUPPLIER_USER1_LOGIN);
		await Page.GetByPlaceholder("Enter your user name").FillAsync(SUPPLIER_USER1_LOGIN);
		await Page.GetByPlaceholder("Enter your password").FillAsync(SUPPLIER_USER1_PASSWORD);
		await Page.Locator("#signInButtonId").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//wait for page to load https://portal.hubwoo.com/main/
		Console.WriteLine("waiting for " + PORTAL_MAIN_URL);
		Boolean cmsDashboardLoaded = false;
		int cmbDashboardAttempts = 0;
		while (cmsDashboardLoaded == false && cmbDashboardAttempts < 10)
		{
			try
			{
				await Page.WaitForURLAsync(PORTAL_MAIN_URL, pageWaitForUrlOptions);
				cmsDashboardLoaded = true;
			}
			catch
			{
				cmbDashboardAttempts++;
				Console.WriteLine("navigating to portal main , attempt " + cmbDashboardAttempts.ToString());
			}
		}

		await Page.WaitForSelectorAsync("//side-bar-item[@name='Home']", new PageWaitForSelectorOptions { Timeout = 60000 });
		if (Environment == "PROD")
		{
			await Expect(Page.Locator("#notificationsWidget")).ToContainTextAsync("Notifications");
		}

			(await GetSideBarHeaderTextAsync()).Should().Contain("The Business Network");
		Console.WriteLine(Page.Url);
		//click catalogs tab
		Console.WriteLine("navigate to catalog home page");

		await Page.GotoAsync(CMS_CATALOG_HOME_URL, pageGotoOptions);
		await Page.WaitForURLAsync(CMS_CATALOG_HOME_URL, pageWaitForUrlOptions);

		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("My Catalogs");
		Console.WriteLine(Page.Url);

		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "My Catalogs" })).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("filter catalogs via customer catalog id");
		await Page.GetByLabel("Customer ID").FillAsync(TC01_CUSTOMER_ID);//TC01_CUSTOMER_ID = "TESTCUSTCDO-0001";

		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("My Catalogs");
		await Task.Delay(4000);//fails when this is removed

		await Expect(Page.GetByText(TC01_CUSTOMERNAME)).ToBeVisibleAsync();
		Console.WriteLine("expand and upload");
		await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Show more" })).ToBeVisibleAsync(locatorVisibleAssertion);
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Task.Delay(2000);

		Console.WriteLine("click download template chevron");
		await Page.GetByRole(AriaRole.Link, new() { Name = "Download Template" }).ClickAsync(locatorClickOptions);//62376_tab1_link

		Console.WriteLine("create a new template");
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}ddlLanguage\"]").SelectOptionAsync(new[] { "en" });
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}tab1\"] form div").Filter(new() { HasText = "Format Excel 2007 (.xlsx)" }).Locator("div").ClickAsync(locatorClickOptions);
		//how to select the productive version
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}ddlVersion\"]").SelectOptionAsync(new SelectOptionValue { Index = 3 });
		await Page.GetByRole(AriaRole.Link, new() { Name = "Create Template" }).ClickAsync(locatorClickOptions);

		DateTime jobStarted = DateTime.Now;
		Console.WriteLine("job created " + jobStarted.ToLongDateString());

		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}createTemplateMessage\"]")).ToContainTextAsync("The creation of your template is in process and will be exported");
		/////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor TEMPLATE EXPORT JOB");
		Console.WriteLine("**********************************************");
		bool monitorPageRendered = false;
		int loadMonitorPageAttempts = 0;
		while (monitorPageRendered == false && loadMonitorPageAttempts < 10)
		{
			try
			{
				await Page.GotoAsync(CMS_MONITOR_URL, pageGotoOptions);
				Console.WriteLine("waiting for : " + CMS_MONITOR_URL);
				await Page.WaitForURLAsync(CMS_MONITOR_URL, pageWaitForUrlOptions);
				Console.WriteLine(Page.Url);
				monitorPageRendered = true;
			}
			catch (Exception ex)
			{
				loadMonitorPageAttempts++;
				Console.WriteLine("Issue navigating to cmb monitor Page, attempt " + loadMonitorPageAttempts.ToString());
				Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
				Console.WriteLine("exception: " + ex.Message);
			}
		}

		await Page.Locator("#ddlRefreshTime").SelectOptionAsync(new[] { "0" });//dont want to autorefresh, do it after each attempt timesout
		Console.WriteLine(Page.Url);
		await Page.GetByLabel("Customer ID:").FillAsync(TC01_CUSTOMER_ID);
		await Page.GetByLabel("Process Type:").SelectOptionAsync(new[] { CMS_MONITOR_PROCESS_FILTER_TEMPLATE_EXPORT });  //template export 31
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);

		//assert refresh monitor button is visible
		await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" })).ToBeVisibleAsync();

		Console.WriteLine("Manually Refresh monitor");
		await Page.Locator("#contentWrapper").GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
		await Page.WaitForLoadStateAsync();
		//wait for ajax call to complete

		//read first row of the itemListContainer, tbody that has the id #itemListContainer
		Console.WriteLine("assert first of monitor list row has a Template Export job");
		//process the monitor page
		DateTime today = DateTime.Now;
		string CurrentDate = $"{today.Day}/{today.Month}/{today.Year}";
		var date = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(3)").TextContentAsync(locatorTextContentOptions);
		int firstBracket = date.IndexOf("(");
		string actionDate = date.Substring(0, firstBracket).Trim();   //remove characters after the first (  e.g. 4/17/2024 (3:38 PM)
		Console.WriteLine("date for last template export job :" + date);

		if (CurrentDate != actionDate)
		{
			Console.WriteLine("action date for last template export job different from expected: " + CurrentDate + " actual: " + date);
		}

		//expect first row in table to have new process
		await Expect(Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)")).ToContainTextAsync("Template Export", locatorToContainTextOption);
		//get process and status of the item in row 1 of the table
		var status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
		var process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
		var startedFrom = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(3)").TextContentAsync();
		Console.WriteLine("status: " + status);

		if (status == "Finished OK" || status == "Failed")
		{
			Console.WriteLine("Status is Finished/Failed , Manually Refresh monitor");
			await Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
			await Page.WaitForLoadStateAsync();

			await Task.Delay(4000);

			await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" })).ToBeVisibleAsync(locatorVisibleAssertion);
			//get process and status of the item in row 1 of the table
			status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
			process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
			Console.WriteLine("status: " + status);
		}

		if (status == "Finished OK")
		{
			Console.WriteLine("Template Export status succeeded after refresh, taking a screenshot ");
			//take screenshot after clicking on failed status
			DateTime dateNow = DateTime.Now;
			string CurrentDate1 = $"{dateNow.Year}{dateNow.Month}{dateNow.Day}{dateNow.Hour}{dateNow.Minute}";
			await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").ClickAsync(locatorClickOptions);
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC09_CMS_Download_Catalog_Failure1_" + CurrentDate1 + ".png"
			});
		}

		int attempt = 0;
		while (attempt <= MONITOR_CHECK_ATTEMPTS)
		{
			try
			{
				Console.WriteLine("Manually Refresh monitor");
				attempt++;
				await Page.Locator("#contentWrapper").GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
				await Page.WaitForLoadStateAsync();

				await Task.Delay(4000);

				Console.WriteLine("Waiting for Template Export: " + attempt.ToString());
				//get process and status of the item in row 1 of the table
				process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
				status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
				Console.WriteLine("status: " + status);
				Console.WriteLine("process: " + process);
				var processid = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(1) > a").TextContentAsync();
				Console.WriteLine("processid:" + processid);
				startedFrom = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(3)").TextContentAsync();
				Console.WriteLine("current job started at " + startedFrom);
				//date time string  format displayed in cms monitor is different to cmb monitor
				//but this is due to the language profile selection for the 2 different users
				//English (United States) => 6/5/2024 (11:34 AM)  => M/d/yyyy (h:mm tt)
				//English (United kingdom) => 05/06/2024 (11:34) => dd/MM/yyyy (HH:mm)
				//cmb user at the time of writing has English USA = 	4/27/2024 (11:12 AM)  	5/4/2024 (10:06 AM)   4/30/2024 (9:36 PM) => m/d/yyyy (hh:mm tt)
				//cms user at the time of writing has English UK = 02/05/2024 (19:33)  27/04/2024 (09:50)    ==> dd/MM/yyyy (HH:mm)

				DateTime currentProcessStarted = DateTime.Now;
				try
				{
					currentProcessStarted = DateTime.ParseExact(startedFrom, "dd/MM/yyyy (HH:mm)", CultureInfo.InvariantCulture);
				}
				catch
				{
					currentProcessStarted = DateTime.ParseExact(startedFrom, "M/d/yyyy (h:mm tt)", CultureInfo.InvariantCulture);
				}
				//ui time of local pc test run on (currentProcessStarted) , different to the time on the server (thisTestStarted), so remove 5 minutes
				if (process == "Template Export" && status == "Finished OK" && currentProcessStarted > thisTestStarted.AddMinutes(-8))
				{
					break;
				}
				else
				{
					Console.WriteLine("still waiting, don't break...");
					Console.WriteLine("currentProcessStarted: " + currentProcessStarted.ToLongTimeString());
					Console.WriteLine("thisTestStarted.AddMinutes(-8)) " + thisTestStarted.AddMinutes(-8).ToLongTimeString());
				}
				await Expect(Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div")).ToContainTextAsync("Finished OK", locatorToContainTextOptionMonitor);
			}
			catch (Exception ex)
			{
				Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
				Console.WriteLine(ex.Message);
				status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
				Console.WriteLine("status: " + status);
				process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
				Console.WriteLine("PROCESS: " + process);
				if (attempt == 5 || attempt == 10 || attempt == 15 || attempt == 20 || attempt == 25)
				{
					Console.WriteLine("attempt: " + attempt.ToString() + " reloading monitor page and resetting filter");
					//sometimes the monitor screen just locks up and can only be reactivated by reloading the page
					monitorPageRendered = false;
					loadMonitorPageAttempts = 0;
					while (monitorPageRendered == false && loadMonitorPageAttempts < 10)
					{
						try
						{
							await Page.GotoAsync(CMS_MONITOR_URL, pageGotoOptions);
							Console.WriteLine("waiting for : " + CMS_MONITOR_URL);
							await Page.WaitForURLAsync(CMS_MONITOR_URL, pageWaitForUrlOptions);
							Console.WriteLine(Page.Url);
							monitorPageRendered = true;
						}
						catch (Exception exception)
						{
							loadMonitorPageAttempts++;
							Console.WriteLine("Issue navigating to cmb monitor Page, attempt " + loadMonitorPageAttempts.ToString());
							Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
							Console.WriteLine("exception: " + exception.Message);
						}
					}
					await Page.GetByLabel("Customer ID:").FillAsync(TC01_CUSTOMER_ID);
					await Page.GetByLabel("Process Type:").SelectOptionAsync(new[] { CMS_MONITOR_PROCESS_FILTER_TEMPLATE_EXPORT });  //template export 31
					await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
					await Task.Delay(3000);
				}
				if (attempt >= MONITOR_CHECK_ATTEMPTS || status == "Failed")
				{
					Console.WriteLine("Template Export failed");
					throw ex;
				}
			}
		}

		if (attempt >= MONITOR_CHECK_ATTEMPTS || status != "Finished OK")
		{
			throw new Exception("Number of attempts to wait for Template Export job to finish exceeded");
		}
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Template Export succeeded");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("Go to dashboard , is there a new download link?");
		await Page.GotoAsync(CMS_CATALOG_HOME_URL, pageGotoOptions);
		await Page.WaitForURLAsync(CMS_CATALOG_HOME_URL, pageWaitForUrlOptions);

		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("My Catalogs");
		Console.WriteLine(Page.Url);

		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "My Catalogs" })).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("filter catalogs via customer catalog id");
		await Page.GetByLabel("Customer ID").FillAsync(TC01_CUSTOMER_ID);//TC01_CUSTOMER_ID = "TESTCUSTCDO-0001";

		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("My Catalogs");
		await Task.Delay(4000);//fails when this is removed

		//TCO1_CATALOG_SELECTOR = "\\36 2376_" (PROD)
		await Expect(Page.GetByText(TC01_CUSTOMERNAME)).ToBeVisibleAsync();
		Console.WriteLine("expand and upload");
		await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Show more" })).ToBeVisibleAsync(locatorVisibleAssertion);
		//await Page.GetByRole(AriaRole.Link, new() { Name = "Show more" }).ClickAsync(locatorClickOptions);
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Task.Delay(2000);

		Console.WriteLine("click download template chevron");
		await Page.GetByRole(AriaRole.Link, new() { Name = "Download Template" }).ClickAsync(locatorClickOptions);//62376_tab1_link
		Console.WriteLine("Refresh the download list");
		await Page.GetByText("Refresh", new() { Exact = true }).ClickAsync(locatorClickOptions);
		//example selectors
		//#\36 2376_DownloadFilesContent > li:nth-child(1) > a  download type
		//#\36 2376_DownloadFilesContent > li:nth-child(1) > span   date
		await Expect(Page.Locator($"#{TCO1_CATALOG_SELECTOR}DownloadFilesContent > li:nth-child(1) > a")).ToContainTextAsync("SCF Export");

		var downloadlinkdate = await Page.Locator($"#{TCO1_CATALOG_SELECTOR}DownloadFilesContent > li:nth-child(1) > span").TextContentAsync(locatorTextContentOptions);
		int firstSpace = date.IndexOf(" ");
		string downloadlinkDate = downloadlinkdate.Substring(1, (firstBracket - 1)).Trim();   //remove characters from (18/04/2024 18:31:05)  to get 18/04/2024

		//assert downloadlinkDate  and CurrentDate dates match

		//download the file
		var waitForDownloadTask = Page.WaitForDownloadAsync();

		//get link e.g https://portal.hubwoo.com/srvs/omnicontent/templatearchive/9574769_SCF_77418_62376_295.1_2024.04.18_file.zip

		var link = await Page.Locator($"#{TCO1_CATALOG_SELECTOR}DownloadFilesContent > li:nth-child(1) > a").GetAttributeAsync("href");

		Console.WriteLine("Download " + link);
		await Page.Locator($"#{TCO1_CATALOG_SELECTOR}DownloadFilesContent > li:nth-child(1) > a").ClickAsync(locatorClickOptions);

		var download = await waitForDownloadTask;
		var fileName = downloadPath + "TC09_" + download.SuggestedFilename;

		Console.WriteLine("File downloaded to " + fileName);

		// Wait for the download process to complete and save the downloaded file somewhere
		await download.SaveAsAsync(fileName);
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(10)]
	[Category("CMBTests")]
	async public Task TC10_CMB_Custom_Landing_Page_Management()
	{
		//prod smoke test cmb 179383
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179308
		Console.WriteLine("TC10_CMB_Custom_Landing_Page_Management");
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 60000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 60000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 60000 };
		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOptionMonitor = new LocatorAssertionsToContainTextOptions { Timeout = 30000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 60000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 60000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 60000 };
		LocatorTextContentOptions locatorTextContentOptions = new LocatorTextContentOptions { Timeout = 60000 };
		LocatorClickOptions manualRefreshClickOptions = new LocatorClickOptions { Force = true, Timeout = 60000 };
		string url = PORTAL_LOGIN;
		await Page.SetViewportSizeAsync(1600, 900);
		await SignInPortal(PORTAL_MAIN_URL, BUYER_USER1_LOGIN, BUYER_USER1_PASSWORD);
		await Page.WaitForSelectorAsync("//side-bar-item[@name='Home']", new PageWaitForSelectorOptions { Timeout = 60000 });
		await Expect(Page.Locator("#notificationsWidget")).ToContainTextAsync("Notifications");
		(await GetSideBarHeaderTextAsync()).Should().Contain("The Business Network");

		if (Environment == "PROD")
		{
			(await GetTopBarUserTextAsync()).Should().Contain("Buyer Admin EPAM");
		}

		await Page.Locator("//side-bar-item-group[@name='Administration']").ClickAsync(locatorClickOptions);//todo replace menu dependency
																																																				//Console.WriteLine("Waiting for " + BUYER_ADMIN_HOME);
																																																				//await Page.WaitForURLAsync(BUYER_ADMIN_HOME, pageWaitForUrlOptions);

		//https://portal.hubwoo.com/main/contactmanagement/Default.aspx

		await Page.Locator("//side-bar-item-group[@name='Catalog Manager']").ClickAsync(locatorClickOptions);//todo replace menu dependency
		await Page.Locator("//side-bar-item[@name='Landing Page Management']").ClickAsync(locatorClickOptions);//todo replace menu dependency
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 60000 }); //Allow max of 1min to fininish loading 

		Console.WriteLine("Waiting for " + BUYER_ADMIN_LANDING_PAGE_URL);
		await Page.WaitForURLAsync(BUYER_ADMIN_LANDING_PAGE_URL, pageWaitForUrlOptions);
		//wait for page  https://portal.hubwoo.com/srvs/BuyerCatalogs/admin/LandingPage
		DateTime today = DateTime.Now;
		string CurrentDate = $"{today.Day}_{today.Month}_{today.Year}_{today.Hour}_{today.Minute}";

		await Page.GetByRole(AriaRole.Link, new() { Name = "Create New Landing Page" }).ClickAsync(locatorClickOptions);
		//Calling out popup not involve any network 
		//this page is very slow to load the landing page ddl
		await Task.Delay(5000);

		//assert popup
		await Expect(Page.Locator("#uiNewLandingPage").GetByText("Create New Landing Page")).ToBeVisibleAsync();
		await Expect(Page.Locator("#newName")).ToBeVisibleAsync();
		await Expect(Page.Locator("#uiNewLandingPage").GetByText("Save")).ToBeVisibleAsync();
		await Page.Locator("#newName").ClickAsync(locatorClickOptions);

		await Page.Locator("#newName").FillAsync("PW_AUTO_" + CurrentDate);

		await Page.Locator("#newDescription").FillAsync("description");
		await Page.Locator("#uiNewLandingPage").GetByText("Save").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(2000);
		//CONFIRM POPUP DISMISSED AND THAT NEW PAGE EXISTS IN THE DROP DOWN  availablePage

		await Expect(Page.Locator("#availablePage")).ToContainTextAsync("PW_AUTO_" + CurrentDate);

		//select the newly saved page
		await Page.Locator("#availablePage").SelectOptionAsync(new[] { "PW_AUTO_" + CurrentDate });
		//assert correct description is displayed
		await Expect(Page.GetByLabel("Description:")).ToHaveValueAsync("description");

		//select a view
		Console.WriteLine("SELECT VIEW " + TC10_SELECTED_VIEW);
		await Page.Locator("#selectedView").SelectOptionAsync(new[] { TC10_SELECTED_VIEW });//#selectedView
		Console.WriteLine("Click Configure Landing page");
		await Page.Locator("#configureButton").ClickAsync(locatorClickOptions);//#configureButton
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//wait for https://econtent.hubwoo.com/catalog/search5/showMenu.action (PROD)

		//on qa goes to https://portal.qa.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=SVVIEW1&VIEW_PASSWD=q0E2Aft3PQy18&USER_ID=SVB-0001ba&BRANDING=search5&LANGUAGE=EN&COUNTRY=US&HOOK_URL=https://portal.qa.hubwoo.com/srvs/BuyerCatalogs/admin/LandingPage?&LANDINGPAGEADMIN=1&LP=65&MIMEPATH=\\cc-hubwoo.net\econtent_QA\attachment\
		//exact url depends on the url configured for the companys search engine in PDS.dbo.OC_SearchEngines

		Console.WriteLine("Waiting for " + SEARCH_CONFIGURE_LANDING_URL);
		await Task.Delay(7000);


		//assert view 
		if (Environment == "QA")//USES SV ENGINE, URL = https://search.qa.hubwoo.com/catalog/ AND VIEW SVVIEW1
		{
			//assert add box
			await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Add new Box" })).ToBeVisibleAsync();

			//await Expect(Page.Locator("#mainTable")).ToContainTextAsync("View: " + TC10_SELECTED_VIEW);//#maintable only in old search UI
			await Expect(Page.GetByRole(AriaRole.Strong)).ToContainTextAsync("User: SVB-0001ba | View: SVVIEW1");
		}

		if (Environment == "PROD") //USES ENGINE FOCS-V2-CDO , URL = https://econtent.hubwoo.com/catalog/ (OLD SEARCH UI ) AND VIEW TESTCOE01
		{
			//assert add box
			await Expect(Page.Locator("#groupsContainer")).ToContainTextAsync("Add new Box");
			await Expect(Page.GetByRole(AriaRole.Strong)).ToContainTextAsync("User: EPAM_TC-0001 | View: TESTCOE01"); //Think that's a UI change which the automation is fixed for qa but not prod
																																																								//await Expect(Page.Locator("#mainTable")).ToContainTextAsync("View: TESTCOE01");
																																																								//await Expect(Page.Locator("#mainTable")).ToContainTextAsync("User: EPAM_TC-0001");
																																																								//THIS SHOULD BE USING NEW SEARCH UI, BUT THERE IS ONLY 1 ENGINE USED FOR MANY MANY BUYERS
																																																								//await Expect(Page.Locator("#mainTable")).ToContainTextAsync("View: " + TC10_SELECTED_VIEW);//NOTE #mainTable ONLY PART OF OLD SEARCH UI MARKUP!!
		}

		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(11)]
	[Category("CMBTests")]
	async public Task TC11_CMB_Create_User()
	{
		Console.WriteLine("TC11_CMB_Create_User");
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179308
		//prod cmb smoke test 179381
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 60000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 60000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 60000 };
		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOptionMonitor = new LocatorAssertionsToContainTextOptions { Timeout = 30000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 60000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 60000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 60000 };
		LocatorTextContentOptions locatorTextContentOptions = new LocatorTextContentOptions { Timeout = 60000 };
		LocatorClickOptions manualRefreshClickOptions = new LocatorClickOptions { Force = true, Timeout = 60000 };
		string url = PORTAL_LOGIN;
		await Page.SetViewportSizeAsync(1600, 900);
		await SignInPortal(PORTAL_MAIN_URL, BUYER_USER1_LOGIN, BUYER_USER1_PASSWORD);
		//await Page.WaitForSelectorAsync("//side-bar-item[@name='Home']", new PageWaitForSelectorOptions { Timeout = 60000 });
		await Expect(Page.Locator("#notificationsWidget")).ToContainTextAsync("Notifications");
		(await GetSideBarHeaderTextAsync()).Should().Contain("The Business Network");

		if (Environment == "PROD")
		{
			(await GetTopBarUserTextAsync()).Should().Contain("Buyer Admin EPAM");
		}

		await Page.Locator("//side-bar-item-group[@name='Administration']").ClickAsync(locatorClickOptions);//todo replace menu dependency
		await Page.WaitForTimeoutAsync(500);
		await Page.Locator("//side-bar-item-group[@name='Catalog Manager']").ClickAsync(locatorClickOptions);//todo replace menu dependency
		await Page.WaitForTimeoutAsync(500);
		try
		{
			await Page.Locator("//side-bar-item[@name='Edit Users']").ClickAsync(locatorClickOptions);//todo replace menu dependency
			Console.WriteLine("Await " + BUYER_ADMIN_EDIT_USERS);
			await Page.WaitForURLAsync(BUYER_ADMIN_EDIT_USERS, pageWaitForUrlOptions);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);///portal.hubwoo.com/main/contactmanagement/Default.aspx
		}
		catch (TimeoutException)
		{
			ReloadIfStacktrace(Page, false);
		}
		Console.WriteLine("Waiting for " + BUYER_ADMIN_EDIT_USERS);

		//wait for page https://portal.hubwoo.com/srvs/omnicontent/BuyerManageUsers.aspx
		DateTime today = DateTime.Now;
		string CurrentDate = $"{today.Day}_{today.Month}_{today.Year}_{today.Hour}_{today.Minute}";
		await Page.WaitForTimeoutAsync(1000);
		Console.WriteLine("Add New User");
		await Page.GetByRole(AriaRole.Link, new() { Name = "Add new User" }).ClickAsync(locatorClickOptions);
		Console.WriteLine("Waiting for " + BUYER_ADMIN_CREATE_USER_URL);///omnicontent/BuyerAdminCreateUser.aspx
		await Page.WaitForURLAsync(BUYER_ADMIN_CREATE_USER_URL, pageWaitForUrlOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//add new user

		//wait for page



		if (Environment == "PROD")
		{
			NEW_USERLOGIN = "ProdR" + CurrentDate;
		}

		if (Environment == "QA")
		{
			NEW_USERLOGIN = "QAR" + CurrentDate;
		}

		await Page.Locator("#ctl00_MainContent_TextBox1").FillAsync(NEW_USERLOGIN);//login
		await Page.Locator("#ctl00_MainContent_TextBox3").FillAsync("test user");//first name
		await Page.Locator("#ctl00_MainContent_TextBox2").FillAsync(NEW_USERLOGIN);//surname
		await Page.Locator("#ctl00_MainContent_TextBox4").FillAsync("omnicontent+" + CurrentDate + "TestUser@gmail.com");//email
		await Page.Locator("#ctl00_MainContent_TextBox5").FillAsync(NEW_USERLOGIN + "TestUser!");//password1
		await Page.Locator("#ctl00_MainContent_TextBox6").FillAsync(NEW_USERLOGIN + "TestUser!");//password2
		await Page.GetByLabel("Buyer", new() { Exact = true }).CheckAsync();
		await Page.GetByRole(AriaRole.Link, new() { Name = "Save" }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(3000);
		//should be on https://portal.hubwoo.com/srvs/omnicontent/BuyerManageUsers.aspx?company=TESTCUSTCDO-0001

		SignOut();
		await Task.Delay(3000);

		await SignInPortal(PORTAL_MAIN_URL + "Admin/MyProfile?takeTo=https:%2F%2Fportal.hubwoo.com%2Fmain%2F", NEW_USERLOGIN, NEW_USERLOGIN + "TestUser!");

		Console.WriteLine("Waiting for " + BUYER_ADMIN_EDIT_PROFILE_URL);

		//assert on edit profile page https://portal.hubwoo.com/main/Admin/MyProfile?takeTo=https:%2F%2Fportal.hubwoo.com%2Fsrvs%2FDefault.aspx
		await Expect(Page.Locator("#mainCover")).ToContainTextAsync("Edit Profile");
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Edit Profile" }).GetByRole(AriaRole.Strong)).ToBeVisibleAsync();
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Administration" })).ToBeVisibleAsync();

		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(12)]
	[Category("CMBTests")]
	async public Task TC12_CMB_Edit_New_User()
	{
		Console.WriteLine("TC12_CMB_Edit_New_User");
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179308
		//prod cmb smoke test 179382
		//note this test in dependent upon TC11, the test param NEW_USERLOGIN is instantiated in that test!!
		////////////////////////////////////////////////////////
		//  FOR TESTING
		//NEW_USERLOGIN = "QAR18_6_2024_9_6";
		///////////////////////////////////////////////////////
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 60000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 60000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 60000 };
		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOptionMonitor = new LocatorAssertionsToContainTextOptions { Timeout = 30000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 60000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 60000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 60000 };
		LocatorTextContentOptions locatorTextContentOptions = new LocatorTextContentOptions { Timeout = 60000 };
		LocatorClickOptions manualRefreshClickOptions = new LocatorClickOptions { Force = true, Timeout = 60000 };
		string url = PORTAL_LOGIN;
		await Page.SetViewportSizeAsync(1600, 900);
		await SignInPortal(PORTAL_MAIN_URL, BUYER_USER1_LOGIN, BUYER_USER1_PASSWORD);
		await Page.WaitForSelectorAsync("//side-bar-item[@name='Home']", new PageWaitForSelectorOptions { Timeout = 60000 });
		await Expect(Page.Locator("#notificationsWidget")).ToContainTextAsync("Notifications");

		if (Environment == "PROD")
		{
			(await GetTopBarUserTextAsync()).Should().Contain("Buyer Admin EPAM");
		}
		await Page.WaitForTimeoutAsync(500);
		await Page.Locator("//side-bar-item-group[@name='Administration']").ClickAsync(locatorClickOptions);//todo replace menu dependency
		await Page.WaitForTimeoutAsync(500);
		await Page.Locator("//side-bar-item-group[@name='Catalog Manager']").ClickAsync(locatorClickOptions);//todo replace menu dependency
		await Page.WaitForTimeoutAsync(500);
		try
		{
			await Page.Locator("//side-bar-item[@name='Edit Users']").ClickAsync(locatorClickOptions);//todo replace menu dependency
			Console.WriteLine("Await " + BUYER_ADMIN_EDIT_USERS);
			await Page.WaitForURLAsync(BUYER_ADMIN_EDIT_USERS, pageWaitForUrlOptions);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);///portal.hubwoo.com/main/contactmanagement/Default.aspx
		}
		catch (TimeoutException)
		{
			ReloadIfStacktrace(Page, false);
		}

		DateTime today = DateTime.Now;
		string CurrentDate = $"{today.Day}_{today.Month}_{today.Year}_{today.Hour}_{today.Minute}NEW";

		Console.WriteLine("search for new user  " + NEW_USERLOGIN);
		await Page.Locator("#ctl00_MainContent_FilterControl1_ctl00_TextBox4").FillAsync(NEW_USERLOGIN);
		//#ctl00_MainContent_FilterControl1_btnSearch
		//await Page.GetByRole(AriaRole.Link, new() { Name = "Search", Exact = true }).ClickAsync(locatorClickOptions);
		await Page.Locator("#ctl00_MainContent_FilterControl1_btnSearch").ClickAsync(locatorClickOptions);

		await Task.Delay(4000);

		//assert only 1 result
		await Expect(Page.Locator("#ctl00_MainContent_FilterControl1_ComplexGridView1_ctl01_cgvLabelPager")).ToContainTextAsync("(1 items found)");
		await Page.GetByRole(AriaRole.Link, new() { Name = "Edit", Exact = true }).ClickAsync(locatorClickOptions);

		await Task.Delay(4000);

		//assert on edit page https://portal.hubwoo.com/srvs/omnicontent/BuyerAdminEditUser.aspx?userid=AF1U7hAKJ6xLJJ7ScQVBZBwYNYuzEJ919bz8ooqpahDW
		await Expect(Page.Locator("#ctl00_BreadCrumpContent_Label10")).ToContainTextAsync("Edit User");
		await Expect(Page.Locator("#ctl00_MainContent_lblCompanyName")).ToContainTextAsync(NEW_USERLOGIN);

		//update first name
		Console.WriteLine("Edit first name for new user  " + NEW_USERLOGIN);

		if (Environment == "PROD")
		{
			await Page.Locator("#ctl00_MainContent_TextBox2").FillAsync("ProdR" + CurrentDate);//first name
		}

		if (Environment == "QA")
		{
			await Page.Locator("#ctl00_MainContent_TextBox2").FillAsync("QAR" + CurrentDate);//first name
		}

		await Page.GetByRole(AriaRole.Link, new() { Name = "Save" }).ClickAsync(locatorClickOptions);

		await Task.Delay(4000);
		//redirected to 
		//https://portal.hubwoo.com/srvs/omnicontent/BuyerManageUsers.aspx
		await Expect(Page).ToHaveURLAsync(BUYER_ADMIN_EDIT_USERS, new PageAssertionsToHaveURLOptions { Timeout = 60000 });


		//logout
		Console.WriteLine("Log Out");
		SignOut();

		Console.WriteLine("Login as new user " + CurrentDate);
		await Task.Delay(2000);
		await SignInPortal(PORTAL_MAIN_URL + "Admin/MyProfile?takeTo=https:%2F%2Fportal.hubwoo.com%2Fmain%2F", NEW_USERLOGIN, NEW_USERLOGIN + "TestUser!");
		await Page.WaitForSelectorAsync("//side-bar-item[@name='Home']", new PageWaitForSelectorOptions { Timeout = 60000 });

		//assert first name changed in menu option
		if (Environment == "PROD")
		{
			Console.WriteLine("assert first name has been updated to ProdR" + CurrentDate);
		}

		if (Environment == "QA")
		{
			Console.WriteLine("assert first name has been updated to QAR" + CurrentDate);
		}

		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(13)]
	[Category("CMBTests")]
	async public Task TC13_CMB_Download_Classification_Template()
	{
		//page load time/spinner makes this test very flaky
		Console.WriteLine("TC13_CMB_Download_Template");
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179308
		//prod cmb smoke tests 179376
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 60000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 60000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 60000 };
		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOptionMonitor = new LocatorAssertionsToContainTextOptions { Timeout = 30000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 60000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 60000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 60000 };
		LocatorTextContentOptions locatorTextContentOptions = new LocatorTextContentOptions { Timeout = 60000 };
		LocatorClickOptions manualRefreshClickOptions = new LocatorClickOptions { Force = true, Timeout = 60000 };
		string url = PORTAL_LOGIN;
		await Page.SetViewportSizeAsync(1600, 900);
		await SignInPortal(PORTAL_MAIN_URL, BUYER_USER1_LOGIN, BUYER_USER1_PASSWORD);
		await Expect(Page.Locator("#notificationsWidget")).ToContainTextAsync("Notifications");
		(await GetSideBarHeaderTextAsync()).Should().Contain("The Business Network");
		if (Environment == "PROD")
		{
			(await GetTopBarUserTextAsync()).Should().Contain("Buyer Admin EPAM");
		}
		Console.WriteLine("Goto download");
		//https://portal.hubwoo.com/srvs/BuyerCatalogs/export/index
		await GoWithErrWrap(BUYER_ADMIN_DOWNLOAD_URL, 60);
		////////////////////////////
		//expand new download panel
		////////////////////////////
		await Page.GetByRole(AriaRole.Link, new() { Name = "New Download" }).ClickAsync(locatorClickOptions);

		Console.WriteLine("download classification template");
		await Page.GetByLabel("Template Type:").SelectOptionAsync(new[] { "classifications" });
		//set type to xlsx

		//assert classifications export is in the template type 
		await Expect(Page.GetByLabel("Template Type:")).ToBeVisibleAsync();
		await Expect(Page.GetByLabel("Template Type:")).ToHaveValueAsync("classifications");
		//EXCEL_2007
		Console.WriteLine("select excel 2007 export format");
		if (Environment == "QA")
		{
			await Page.Locator("#uiExportTemplateFormat").SelectOptionAsync(new[] { "EXCEL_2007" }, new LocatorSelectOptionOptions { Force = true, Timeout = 60000 });
		}

		if (Environment == "PROD")
		{
			await Page.Locator("#uiExportTemplateFormat").SelectOptionAsync(new[] { "EXCEL_2007" }, new LocatorSelectOptionOptions { Force = true, Timeout = 60000 });
		}

		await Page.GetByText("Create new Export Template").ClickAsync(locatorClickOptions);
		DateTime jobStarted = DateTime.Now;
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		Console.WriteLine("job created " + jobStarted.ToLongDateString());

		WaitForElementToBeHidden(Page, "#loadingScreen");

		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor CLASSIFICATION EXPORT");
		Console.WriteLine("**********************************************");
		await GoWithErrWrap(CMB_MONITOR_URL, 60);
		await SetManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Classification Export", jobStarted, "", TC01_CUSTOMERNAME, "Finished OK");

		Console.WriteLine("**********************************************");
		Console.WriteLine("Classification Export Completed");
		Console.WriteLine("**********************************************");

		//return to download
		Console.WriteLine("Go back to downloads page -> reports");  //is this on reporting or downloads
		await GoWithErrWrap(BUYER_ADMIN_DOWNLOAD_URL, 60);
		DateTime today = DateTime.Today;
		string CurrentDate = $"{today.Month}_{today.Day}_{today.Year}";
		//filer using type of classification export
		await Page.GetByLabel("Type:", new() { Exact = true }).SelectOptionAsync(new[] { "classifications" });

		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);

		await Task.Delay(5000);

		//download the file
		var waitForDownloadTask = Page.WaitForDownloadAsync();
		//get link
		//e.g. https://portal.hubwoo.com/srvs/omnicontent/templatearchive/9572990_classifications.zip

		var link = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(8) > a").GetAttributeAsync("href");

		Console.WriteLine("Download " + link);
		await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(8) > a").First.ClickAsync(locatorClickOptions);

		var download = await waitForDownloadTask;

		var fileName = downloadPath + "TC13_" + CurrentDate + download.SuggestedFilename;

		Console.WriteLine("File downloaded to " + fileName);
		//unzip

		// Wait for the download process to complete and save the downloaded file somewhere
		await download.SaveAsAsync(fileName);

		Console.WriteLine("unzip file " + fileName);
		//unzip the file to downloadPath 
		string excelFileName = $"{downloadPath}{ExtractZipFile(fileName, downloadPath)}";


		Console.WriteLine("use ClosedXML to validate contents of file: " + excelFileName);
		//assert contents of xlsx
		Console.WriteLine("assert contents of excel file " + excelFileName);
		try
		{
			using var xlWorkbook = new XLWorkbook(excelFileName);
			var ws1 = xlWorkbook.Worksheet(1);
			if (Environment == "PROD")
			{
				var a1Title = ws1.Cell("A1").GetValue<string>();
				Console.WriteLine("cell A1:" + a1Title);
				Assert.That(a1Title.Trim().Contains("TESTCUSTCDO-0001 - used Classification-Codes "));

				var c1Title = ws1.Cell("C1").GetValue<string>();
				Console.WriteLine("cell C1:" + c1Title);
				Assert.That(c1Title.Trim().Contains("Classification System"));

				// var a2 = ws1.Cell("A2").GetValue<string>();
				// Console.WriteLine("cell A2:" + a2);
				// Assert.That(a2.Trim().Contains("TESTSUPCDO2_77418_62376_"));
			}
			if (Environment == "QA")
			{
				var a1Title = ws1.Cell("A1").GetValue<string>();
				Console.WriteLine("cell A1:" + a1Title);
				Assert.That(a1Title.Trim().Contains("SVB-0001 - used Classification-Codes"));

				var c1Title = ws1.Cell("C1").GetValue<string>();
				Console.WriteLine("cell C1:" + c1Title);
				Assert.That(c1Title.Trim().Contains("Classification System"));

				var classsystem = ws1.Cell("C2").GetValue<string>();
				Console.WriteLine("cell C2" + classsystem);
				Assert.That(classsystem.Trim().Contains("UNSPSC-11.2"));

				var a2 = ws1.Cell("A2").GetValue<string>();
				Console.WriteLine("cell A2:" + a2);
				Assert.That(a2.Trim().Contains("SVS1_237593_63045_"));
			}
		}
		catch (Exception e)
		{
			Console.WriteLine("exception asserting contents of classifications  template file: " + e.Message);
		}
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(14)]
	[Category("CMBTests")]
	async public Task TC14_CMB_DownLoad_Classification_Report()
	{
		Console.WriteLine("**********************************************");
		Console.WriteLine("TC14_CMB_DownLoadClassificationReport");
		Console.WriteLine("**********************************************");
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179308
		//prod smoke test cmb 179375
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 60000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 60000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 60000 };
		LocatorClickOptions clickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOptionMonitor = new LocatorAssertionsToContainTextOptions { Timeout = 30000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 60000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 60000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 60000 };
		LocatorTextContentOptions locatorTextContentOptions = new LocatorTextContentOptions { Timeout = 60000 };
		LocatorClickOptions manualRefreshClickOptions = new LocatorClickOptions { Force = true, Timeout = 60000 };
		string url = PORTAL_LOGIN;
		await Page.SetViewportSizeAsync(1600, 900);
		await SignInPortal(PORTAL_MAIN_URL, BUYER_USER1_LOGIN, BUYER_USER1_PASSWORD);
		await Expect(Page.Locator("#notificationsWidget")).ToContainTextAsync("Notifications");
		(await GetSideBarHeaderTextAsync()).Should().Contain("The Business Network");

		if (Environment == "PROD")
		{
			(await GetTopBarUserTextAsync()).Should().Contain("Buyer Admin EPAM");
		}
		Console.WriteLine("Goto reporting");
		//https://portal.hubwoo.com/srvs/BuyerCatalogs/export/index


		///https://portal.hubwoo.com/srvs/BuyerCatalogs/reporting/index
		await GoWithErrWrap(BUYER_ADMIN_REPORTING_URL, 60);
		////////////////////////////////////////////
		//expand the reporting panel
		///////////////////////////////////////////

		Console.WriteLine("expand Create report Panel");
		await Page.GetByRole(AriaRole.Link, new() { Name = "Create Report" }).ClickAsync(clickOptions);

		Console.WriteLine("download classification report");
		await Task.Delay(3000);

		await Page.GetByLabel("Reports:").SelectOptionAsync(new[] { "ClassificationList" });//requires that on ContentAdmin/AdminReporting Classification report is one of the Selected Reports

		await Task.Delay(2000);
		await Page.Locator("#uiSupplierForClassificationReportInput").ClickAsync(locatorClickOptions);
		await Page.Locator("#uiSupplierForClassificationReportInput").FillAsync(DOWNLOAD_REPORT_SUPPLIERNAME);
		await Task.Delay(3000);

		//need to click away from the supplier ddl so that the create report button becomes active
		await Page.GetByText("Search Results").ClickAsync();
		await Page.Locator("#uiSupplierForClassificationReportInput").ClickAsync(locatorClickOptions);

		await Page.Locator("#uiButtonCreateRport").ClickAsync(new LocatorClickOptions { Force = true, Timeout = 60000 });//note the incorrectly named id!!!
		DateTime jobStarted = DateTime.Now;
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(3000);
		Console.WriteLine("job created " + jobStarted.ToLongDateString());
		await Expect(Page.Locator("#uiProcessCreatedMessage")).ToContainTextAsync("The creation of your template / report is in process and will be exported. Please refresh the screen (press F5) after a few seconds or after you receive a notification via email to download your template / report below.For more detailed process information please click on Monitor.");
		await Task.Delay(2000);
		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor REPORTING JOB");
		Console.WriteLine("**********************************************");
		await GoWithErrWrap(CMB_MONITOR_URL, 60);
		ManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Reporting job", jobStarted, "", TC01_CUSTOMERNAME, "Finished OK");
		Console.WriteLine("**********************************************");
		Console.WriteLine("Reporting Job  Completed");
		Console.WriteLine("**********************************************");
		//return to reporting
		Console.WriteLine("Go back to reporting page");
		await GoWithErrWrap(BUYER_ADMIN_REPORTING_URL, 60);
		Console.WriteLine("search for ClassificationList report download");

		await Page.GetByLabel("Type:", new() { Exact = true }).SelectOptionAsync(new[] { "ClassificationList" });
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(3000);

		//download the file
		var waitForDownloadTask = Page.WaitForDownloadAsync();
		//get link
		//https://portal.hubwoo.com/srvs/omnicontent/templatearchive/21218745_classificationlist_report.zip
		Console.WriteLine("get the download link ");
		//the catalog version column was removed from the itemListContainer, href now in 7th column
		var link = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(7) > a").GetAttributeAsync("href");
		DateTime today = DateTime.Today;
		string CurrentDate = $"{today.Month}_{today.Day}_{today.Year}";
		Console.WriteLine("Download " + link);
		await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(7) > a").First.ClickAsync(locatorClickOptions);

		var download = await waitForDownloadTask;

		var fileName = downloadPath + "TC14_" + CurrentDate + download.SuggestedFilename;

		Console.WriteLine("File downloaded to " + fileName);
		//unzip

		// Wait for the download process to complete and save the downloaded file somewhere
		await download.SaveAsAsync(fileName);

		Console.WriteLine("unzip file " + fileName);
		//unzip the file to downloadPath 
		string csvFileName = $"{downloadPath}{ExtractZipFile(fileName, downloadPath)}";

		Console.WriteLine("use TinyCSVParser to validate contents of file: " + csvFileName);

		Console.WriteLine("assert contents of csv file " + csvFileName);
		try
		{
			CsvParserOptions csvParserOptions = new CsvParserOptions(true, ';');
			CsvClassificationMapping csvMapper = new CsvClassificationMapping();
			CsvParser<csvClassification> csvParser = new CsvParser<csvClassification>(csvParserOptions, csvMapper);

			var result = csvParser
																							.ReadFromFile(csvFileName, Encoding.ASCII)
																							.ToList();

			Assert.That(result.Count > 0);
			if (Environment == "QA")
			{
				//9 RESULTS FOR QA
				Console.WriteLine("assert first data row is 11111809;\"Ball clay\";\"3\"");
				Assert.That("11111809" == result[0].Result.ClassificationCode);
				Assert.That("Ball clay" == result[0].Result.Classification);
				Assert.That(3 == result[0].Result.Count);

				Console.WriteLine("assert second data row is 13101906;\"Thermoset polyurethane (PUR)\";\"1\"");

				Assert.That("13101906" == result[1].Result.ClassificationCode);
				Assert.That("Thermoset polyurethane (PUR)" == result[1].Result.Classification);
				Assert.That(1 == result[1].Result.Count);
			}
			if (Environment == "PROD")
			{
				//2 results on prod
				Console.WriteLine("assert first data row is 32151201;\"Alkali organometallic hydrocarbons (lab)\";\"7\"");
				Assert.That("32151201" == result[0].Result.ClassificationCode);
				Assert.That("Alkali organometallic hydrocarbons (lab)" == result[0].Result.Classification);
				Assert.That(7 == result[0].Result.Count);

				Console.WriteLine("assert second data row is 32151202;\"Earth alkali organolmetallic hydrocarbons (lab)\";\"1\"");

				Assert.That("32151202" == result[1].Result.ClassificationCode);
				Assert.That("Earth alkali organolmetallic hydrocarbons (lab)" == result[1].Result.Classification);
				Assert.That(1 == result[1].Result.Count);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine("exception asserting contents of classifications  template file: " + e.Message);
		}
		Console.WriteLine("**********************************************");
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
		Console.WriteLine("**********************************************");
	}


	[Test, Order(15)]
	[Category("CMBTests")]
	async public Task TC15_CMB_Archive_Catalog()
	{

		//this is one of the least meaningful and annoying tests
		//it takes ages to render the history dialog as there are 300+ versions of the cataog
		/*
 Precondition: 
	Archiving feature is enabled in CMA for the buying company TESTCUSCDO1
	IN CMA, edit company, under Premium Features ensure the Archiving feature has a catalog version number other than No
	Several catalog version have been released to search 
*/
		Console.WriteLine("**********************************************");
		Console.WriteLine("TC15_CMB_Archive_Catalog");
		Console.WriteLine("**********************************************");
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179308
		//prod cmb smoke tests 179374
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 60000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 60000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 60000 };
		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 60000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOptionMonitor = new LocatorAssertionsToContainTextOptions { Timeout = 30000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 60000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 60000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 60000 };
		LocatorTextContentOptions locatorTextContentOptions = new LocatorTextContentOptions { Timeout = 60000 };
		LocatorClickOptions manualRefreshClickOptions = new LocatorClickOptions { Force = true, Timeout = 60000 };
		string url = PORTAL_LOGIN;
		await Page.SetViewportSizeAsync(1600, 900);
		await SignInPortal(PORTAL_MAIN_URL, BUYER_USER1_LOGIN, BUYER_USER1_PASSWORD);
		await Expect(Page.Locator("#notificationsWidget")).ToContainTextAsync("Notifications");
		(await GetSideBarHeaderTextAsync()).Should().Contain("The Business Network");
		if (Environment == "PROD")
		{
			(await GetTopBarUserTextAsync()).Should().Contain("Buyer Admin EPAM");
		}
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Buyer Dashboard");
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buyer Dashboard" })).ToBeVisibleAsync(locatorVisibleAssertion);
		await CMBFilter(SHOW_HISTORY_SUPPLIERNAME, "");
		Console.WriteLine("click cog wheel menu");

		try
		{
			//note force =  true does not override the fact that an element is hidden!!!
			//TC04_CATALOG_SELECTOR_ID = "\\37 7418";
			await Page.EvalOnSelectorAsync($"#{TC04_CATALOG_SELECTOR_ID}_allTasks_catalog > div > div.settings > a.dropdown-toggle", "el => el.click()");
			//https://playwright.dev/dotnet/docs/api/class-page#page-eval-on-selector
			//https://github.com/microsoft/playwright-dotnet/issues/923
			//here we want to click the hidden li item as cannot get the menu to display using playwright, this method should be used with care as all actionability checks are ignored
			//and we are emulating a Javascript click event on the element referenced by the selector
			Console.WriteLine("select Show History menu option");
			await Task.Delay(2000);
			await Page.EvalOnSelectorAsync($"#{TC04_CATALOG_SELECTOR_ID}_allTasks_catalog > div > div.settings > ul.dropdown-menu > li > a.view-catalog-history", "el => el.click()");
		}
		catch
		{
			Console.WriteLine("Cannot click cog wheel menu!!");
		}

		//wait for loadingScreen to disappear
		Console.WriteLine("waiting for loadingScreen to disappear it takes 2 mins 20 seconds on prod");
		WaitForElementToBeHidden(Page, "#loadingScreen");

		//wait for loading screen to disappear takes like 2mins 20 seconds!!!!!
		Console.WriteLine("assert show history popup is visible");
		int showhistoryattempt = 1;
		bool historypopupvisible = false;
		while (showhistoryattempt < 5 && historypopupvisible == false)
		{
			try
			{
				Console.WriteLine("waiting for show history popup to be visible");
				await Expect(Page.Locator("#versionHistory")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60000 });
				historypopupvisible = true;
			}
			catch
			{
				Console.WriteLine("waiting for show history popup to be visible, attempt:" + showhistoryattempt.ToString());
				showhistoryattempt++;
			}
		}

		if (showhistoryattempt >= 10 && historypopupvisible == false)
		{
			//throw exception
			throw new Exception("Number of attempts expired whilst waiting for show history dialog ");
		}

		await Expect(Page.GetByText("Set-Live Restored Version")).ToBeVisibleAsync();

		int row = 1;
		int rowFirstRowWithReleasedCatalog = 0;
		var state = "";
		var action = "";
		//#divVersionHistoryContent > table > tbody > tr:nth-child(1)
		var totalVersionRows = await Page.Locator("#divVersionHistoryContent > table > tbody > tr").CountAsync();
		Console.WriteLine("Restore most recently released catalog version");
		state = await Page.Locator($"#divVersionHistoryContent > table > tbody > tr:nth-child({row}) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
		while (row < totalVersionRows)
		{
			row++;
			state = await Page.Locator($"#divVersionHistoryContent > table > tbody > tr:nth-child({row}) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
			action = await Page.Locator($"#divVersionHistoryContent > table > tbody > tr:nth-child({row}) > td:nth-child(9)").TextContentAsync(locatorTextContentOptions);
			if (state == "Released" && action == "Restore version")
			{
				break;
			}
		}

		if (state == "Released" && action == "Restore version")
		{
			//now get the version for use later
			rowFirstRowWithReleasedCatalog = row;
			var version = await Page.Locator($"#divVersionHistoryContent > table > tbody > tr:nth-child({row}) > td:nth-child(1)").TextContentAsync(locatorTextContentOptions);
			Console.WriteLine("Checking restore link for version " + version);
			action = await Page.Locator($"#divVersionHistoryContent > table > tbody > tr:nth-child({row}) > td:nth-child(9)").TextContentAsync(locatorTextContentOptions);
			Console.WriteLine("action link text = " + action);
			CATALOG_RESTORE_VERSION = version;

			if (action == "Restore version")
			{
				//click the restore link
				Console.WriteLine("Clicking Restore version link for version " + version);
				await Task.Delay(2000);
				if (Environment == "PROD")
				{
					await Page.Locator($"#divVersionHistoryContent > table > tbody > tr:nth-child({row}) > td:nth-child(9)").ClickAsync(locatorClickOptions);
				}

				if (Environment == "QA")
				{
					await Page.Locator($"#divVersionHistoryContent > table > tbody > tr:nth-child({row}) > td:nth-child(9) > a").ClickAsync(locatorClickOptions);
				}
			}
			DateTime jobStart = DateTime.Now;
			await Page.WaitForURLAsync(CMB_MONITOR_URL);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Page.WaitForTimeoutAsync(1000);
			//return to monitor
			Console.WriteLine("**********************************************");
			Console.WriteLine("Go to monitor ARCHIVE JOB");
			Console.WriteLine("**********************************************");
			await GoWithErrWrap(CMB_MONITOR_URL, 60);
			await SetManualRefresh();
			await MonitorProcessStatueAsync(Page, "", "Archive job", jobStart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
			Console.WriteLine("**********************************************");
			Console.WriteLine("Archive job Completed");
			Console.WriteLine("**********************************************");
			await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
			await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Buyer Dashboard");
			await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buyer Dashboard" })).ToBeVisibleAsync(locatorVisibleAssertion);
			await CMBFilter(SHOW_HISTORY_SUPPLIERNAME, "");
			try
			{
				//note force =  true does not override the fact that an element is hidden!!!
				//TC04_CATALOG_SELECTOR_ID = "\\37 7418";
				await Page.EvalOnSelectorAsync($"#{TC04_CATALOG_SELECTOR_ID}_allTasks_catalog > div > div.settings > a.dropdown-toggle", "el => el.click()");
				//https://playwright.dev/dotnet/docs/api/class-page#page-eval-on-selector
				//https://github.com/microsoft/playwright-dotnet/issues/923
				//here we want to click the hidden li item as cannot get the menu to display using playwright, this method should be used with care as all actionability checks are ignored
				//and we are emulating a Javascript click event on the element referenced by the selector
				Console.WriteLine("select Show History menu option");
				await Task.Delay(2000);
				await Page.EvalOnSelectorAsync($"#{TC04_CATALOG_SELECTOR_ID}_allTasks_catalog > div > div.settings > ul.dropdown-menu > li > a.view-catalog-history", "el => el.click()");
			}
			catch
			{
				Console.WriteLine("Cannot click cog wheel menu!!");
			}

			//wait for loadingScreen to disappear
			Console.WriteLine("waiting for loadingScreen to disappear it takes 2 mins 20 seconds on prod");
			WaitForElementToBeHidden(Page, "#loadingScreen");

			//wait for loading screen to disappear takes like 2mins 20 seconds!!!!!
			Console.WriteLine("assert show history popup is visible");
			showhistoryattempt = 1;
			historypopupvisible = false;
			int timelimit = 10; //Make it a bit easier to adjust time limit to check popup
			while (showhistoryattempt < timelimit && historypopupvisible == false)
			{
				try
				{
					Console.WriteLine("waiting for show history popup to be visible");
					await Expect(Page.Locator("#versionHistory")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60000 });
					historypopupvisible = true;
				}
				catch
				{
					Console.WriteLine("waiting for show history popup to be visible, attempt:" + showhistoryattempt.ToString());
					showhistoryattempt++;
				}
			}

			if (showhistoryattempt >= timelimit && historypopupvisible == false)
			{
				//throw exception
				Console.WriteLine("Number of attempts expired whilst waiting for show history dialog ");
				throw new Exception("Number of attempts expired whilst waiting for show history dialog ");
			}

			Console.WriteLine("Assert version history popup title ");
			await Expect(Page.GetByText("Set-Live Restored Version")).ToBeVisibleAsync();

			//get the action text for the row for the version that was previously restored
			//CATALOG_RESTORE_VERSION
			//rowFirstRowWithReleasedCatalog  use this if we assume no other version histroy has been added since we just performed the restore above

			Console.WriteLine("Row which was previously restored was the version in row:  " + rowFirstRowWithReleasedCatalog.ToString());
			Console.WriteLine("Assert the the action column for version " + CATALOG_RESTORE_VERSION + " that was restored previously now has 'Show' and 'Release version into production' links");
			//check the links for version, iterate rows until we have the version number we expect, are there 2 links Show and Release version into production
			await Expect(Page.Locator($"#divVersionHistoryContent > table > tbody > tr:nth-child({rowFirstRowWithReleasedCatalog}) > td:nth-child(9)")).ToContainTextAsync("Show");
			await Expect(Page.Locator($"#divVersionHistoryContent > table > tbody > tr:nth-child({rowFirstRowWithReleasedCatalog}) > td:nth-child(9)")).ToContainTextAsync("Release version into production", new LocatorAssertionsToContainTextOptions { IgnoreCase = true, Timeout = 60000 });

			Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
		}
		else
		{
			Console.WriteLine("Couldn't find released catalog in version history ");
			throw new Exception("Couldn't find released catalog in version history");
		}
	}
}