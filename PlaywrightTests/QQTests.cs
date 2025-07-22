
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Packaging;
using FluentAssertions;

namespace PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class QQTests : PageTest
{
	////////////////////////////////////////////////////////////////////////////

	string Environment = "PROD";  //QA  / UAT  / PROD    SET TESTING ENVIRONMENT HERE, NOTE HAS NOT BEEN TESTED ON UAT

	////////////////////////////////////////////////////////////////////////////
	string downloadPath = "";
	/// BEFORE RUNNING PERFORM A SEARCH VISUAL STUIO SEARCH OF THIS FILE FOR THE TEXT = "PW_AUTO  ,  TO ENSURE THAT NO STATEMENTS USED TO ASSIGN VALUES FOR TESTING HAVE BEEN LEFT UNCOMMENTED OUT!!!!

	//also before running, unless you require headless mode, you may need to require to configure access to the runsettings file:
	//in test explorer
	//click the down arrow on the settings cog icon
	//select the configure Run settings option from the context menu
	//enable the auto detect run settings file option
	//then select the select solution wide run settings file and configure it to point to the file ..\catalog-manager\PlaywrightTests\PlaywrightTests\PlayWright.runsettings

	//The email tests in this class , assume that all emails are for a variant of the email address easyordertest@gmail.com, the easyordertest@gmail.com google account
	//email easyordertest@gmail.com
	//password: Qu1ck,.Qu0t3/.,
	//also see https://dev.azure.com/Proactis/eCat/_wiki/wikis/eCat.wiki/2808/Email-Boxes

	//has a project configured in the google cloud/developer console with an Oauth client configured and the gmail api enabled.
	//The credentials required by the gmail api service are located in the Credentials.json file located in the bin folder
	//(..\catalog-manager\PlaywrightTests\PlaywrightTests\bin\Debug\net7.0) for this project

	//you may need to copy the solution file ..\catalog-manager\credentials.json into your PlaywrightTests\bin\Debug\net7.0 or PlaywrightTests\bin\Debug\net8.0 folder for the email functionality to work.

	/*
		 {"installed":{"client_id":"1088739761220-ohpuqop5drhjfp9ko1bj4adedfagtheh.apps.googleusercontent.com",
		"project_id":"ecat-gmail-api",
		"auth_uri":"https://accounts.google.com/o/oauth2/auth",
		"token_uri":"https://oauth2.googleapis.com/token",
		"auth_provider_x509_cert_url":"https://www.googleapis.com/oauth2/v1/certs",
		"client_secret":"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",
		"redirect_uris":["http://localhost"]}}

		to begin codegen in vs terminal
		PS C:...\catalog-manager\PlaywrightTests> cd PlaywrightTests
		PS C:...\catalog-manager\PlaywrightTests> cd PlaywrightTests
		PS C:....\catalog-manager\PlaywrightTests\PlaywrightTests> cd bin\debug\net8.0
		PS C:...\catalog-manager\PlaywrightTests\PlaywrightTests\bin\debug\net8.0> pwsh playwright.ps1 codegen
	
		useful commands that can be executed in visual studio cli 
		--run firefox playwright recorder, NOTE first have to cd into the bin folder of this project!
		 pwsh playwright.ps1 codegen -b firefox
		 
	   pwsh playwright.ps1 codegen -b chromium --channel msedge

	   pwsh playwright.ps1 codegen -b chromium --channel chrome

		--run a specific test for a specific browser
		 dotnet test --filter Name="TC01_QQB_New_User_Login_Gets_Redirected_To_Profile_Page" --settings playwrightmsedge.runsettings

		 dotnet test --filter Name="TC01_QQB_New_User_Login_Gets_Redirected_To_Profile_Page" --settings playwrightfirefox.runsettings
		 
		 dotnet test --filter Category~QQTests -- Playwright.BrowserName=firefox

		 dotnet test --filter Name="TC01_QQB_New_User_Login_Gets_Redirected_To_Profile_Page" --settings playwrightchrome.runsettings
  
	   dotnet test --filter Category=QQTests1 -- Playwright.BrowserName=chromium Playwright.LaunchOptions.Channel=msedge

		 dotnet test --filter Category=QQTests2 -- Playwright.BrowserName=chromium Playwright.LaunchOptions.Channel=msedge

	   dotnet test --filter Category=QQTests3 -- Playwright.BrowserName=chromium Playwright.LaunchOptions.Channel=msedge
	*/

	/// <summary>
	/// /////////////////////////////////////////////////////////////////////////
	/// before running ensure that all test settings for request transaction id's have been commented out via searching for  = "PW_Auto_
	/// /////////////////////////////////////////////////////////////////////////
	/// </summary>
	/// 

	string _browserName = "";
	static string[] Scopes = { GmailService.Scope.GmailReadonly };//scopes for gmail api
	static string ApplicationName = "emailtester"; // Gmail API .NET Quickstart
	string testStartSecondsSinceEpoch = "";
	string testStarted = "";

	//tests 2 - 20
	string requestToRejectTransactionName = "";//PW_Auto_RequestToReject_ global variable so a request can be created in one test and referenced in another
	string requestToOfferTransactionName = "";//PW_Auto_RequestToOffer_
	string requestToOfferId = "";//id of request created for request rejection/duplication tests
	string requestToRejectId = "";//id of request created for offer rfc tests
	
	//tests 21 - 27
	string requestPositionFormTransactionName = "";////PW_Auto_RequestPositionPF_
	string requestIdPositionForm = "";//id of request created for position form tests

  //tests 28-33
	string requestPositionRequestFormTransactionName = "";////PW_Auto_RequestPositionRF_  used in tests 28 onwards
	string requestIdRequestForm = "";//id of request created for request form tests
																	
	//tests 34-39
	
	string requestSupplierClassificationTransactionName = "";//PW_Auto_Sup_Class_
	string requestIdSupplierChoosesClassification = "";//id of request created for supplier chooses classification tests
	string updatedrequestCount = "";

	string SEARCHURL = "";//main url for qqb testing where BUYER_CHOOSES_CLASSIFICATION = true
	string SEARCHURL2 = "";//to test BUYER_CHOOSES_CLASSIFICATION = false
	string NEW_USER_SEARCHURL = "";
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
	string TC_REQUEST_LIST = "Request list";
	string TC_REQUEST_RECEIVED_EMAIL_SUBJECT = "[QA][Quick Quote] You have received a new Quick Quote request";
	string TC_OFFER_RECEIVED_EMAIL_SUBJECT = "[QA][Quick Quote] You received an offer";
	string TC_REQUEST_FOR_CHANGE_EMAIL_SUBJECT = "[QA][Quick Quote] You've received request for change!";
	string TC_REQUEST_REJECTED_EMAIL_SUBJECT = "[Quick Quote] Request has been rejected!";
	string CONTENTADMIN_LOGIN = "";
	string CONTENTADMIN_PASSWORD = "";
	string SUPPLIER_USER1_LOGIN = "";
	string SUPPLIER_USER1_PASSWORD = "";
	string SUPPLIER_USER2_LOGIN = "";
	string SUPPLIER_USER2_PASSWORD = "";
	string TC01_ASSERT_VIEWNAME = "";
	string TC01_CLICK_SAVE = "";
	string TC01_ASSERT_BREADCRUMB = "";
	string TC01_LOCATION = "";
	string TC02_ASSERT_SUPPLIER1 = "";
	string TC02_ASSERT_SUPPLIER2 = "";
	string TC02_PRODUCT_GROUP = "";
	string TC02_DATASHEET_SELECTOR = "";
	string TC02_COMPANYID = "";//companyid for eo properties editing
	string TC02_COMPANY_NAME = "";
	string TC02_DEFAULT_CLASSIFICATON_CODE1 = "";
	string TC02_DEFAULT_CLASSIFICATON_CODE2 = "";
	string TC02_SELECT_SUPPLIER1_ID = "";
	string TC02_SELECT_SUPPLIER2_ID = "";
	string TC02_CLASS_CODE_LEVEL1 = "";
	string TC02_CLASS_CODE_LEVEL2 = "";
	string TC02_CLASS_CODE_LEVEL3 = "";
	string TC02_CLASS_CODE_LEVEL4 = "";

	string TC02_MANDATORY_SUPPLIER_DATASHEET1_SELECTOR = "";
	string TC02_MANDATORY_SUPPLIER_DATASHEET2_SELECTOR = "";
	string TC02_POPULATE_CLASSIFICATION_DATASHEET1_SELECTOR = "";
	string TC02_POPULATE_CLASSIFICATION_DATASHEET2_SELECTOR = "";
	string TC02_EMPTY_REQUEST_POSITIONS = "";
	string TC05_PDF_ASSERT_CUSTOMERNAME = "";
	string TC05_PDF_ASSERT_LOCATION = "";
	string TC06_LOCATION_ASSERT = "";
	string TC10_PDF_ASSERT_TOTAL_VALUE = "";
	string TC11_ASSET_POS_2_UNIT_PRICE = "2.00 EUR";
	string TC11_ASSET_POS_3_UNIT_PRICE = "3.00 EUR";
	string TC12_ASSERT_PDF_EMAIL = "";
	string TC21_FORM_DATASHEET1_SELECTOR = "";//prod With Form - same class system
	string TC21_EDIT_FORM1_OPTION = "";//testform1
	string TC28_FORM_DATASHEET2_SELECTOR = "";//prod With Form - form has own class system
	string TC28_CLASS_CODE_LEVEL1 = "";
	string TC28_CLASS_CODE_LEVEL2 = "";
	string TC28_CLASS_CODE_LEVEL3 = "";
	string TC28_CLASS_CODE_LEVEL4 = "";
	string TC28_CLASS_CODE = "";

	string TC34_COMPANYID = "";//used if cma required to find /edit company
	string TC34_COMPANY_NAME = "";
	string TC34_SIMPLE_DATASHEET_SELECTOR = "";
	string TC34_PRODUCT_GROUP = "";
	string TC34_ASSERT_SUPPLIER1 = "";
	string TC34_ASSERT_SUPPLIER2= "";
	string TC34_EMPTY_REQUEST_POSITIONS = "";
	string TC34_SELECT_SUPPLIER1_ID = "";
	string TC34_SELECT_SUPPLIER2_ID = "";

	string TC35_CLASS_CODE_LEVEL1 = "";
	string TC35_CLASS_CODE_LEVEL2 = "";
	string TC35_CLASS_CODE_LEVEL3 = "";
	string TC35_CLASS_CODE_LEVEL4 = "";

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
			SEARCHURL = "https://search.qa.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=1fmkV&VIEW_PASSWD=o4D5Bfi8W6XnY&USER_ID=fmkb&BRANDING=search5&LANGUAGE=EN&HOOK_URL=https://portal.qa.hubwoo.com/catalog/search5/customizings/defaultReceiver.jsp&ADMIN=1&COUNTRY=GB&EASYORDER=1";//fmkb
			SEARCHURL2 = "https://search.qa.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=SVVIEW1&VIEW_PASSWD=q0E2Aft3PQy18&USER_ID=SV&BRANDING=search5&LANGUAGE=EN&HOOK_URL=https://search.qa.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp&ADMIN=1&COUNTRY=GB&EASYORDER=1";//SV Buyer (SVB-0001) 
			NEW_USER_SEARCHURL = "https://search.qa.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=1fmkV&VIEW_PASSWD=o4D5Bfi8W6XnY&USER_ID=[USERNAME]&BRANDING=search5&LANGUAGE=EN&HOOK_URL=https://portal.qa.hubwoo.com/catalog/search5/customizings/defaultReceiver.jsp&ADMIN=1&COUNTRY=GB&EASYORDER=1";
			CONTENTADMIN_LOGIN = "epamcontentadmin";
			CONTENTADMIN_PASSWORD = "password1";
			TC_REQUEST_RECEIVED_EMAIL_SUBJECT = "[QA][Quick Quote] You have received a new Quick Quote request";
			TC_REQUEST_REJECTED_EMAIL_SUBJECT = "[QA][Quick Quote] Request has been rejected!";
			TC_OFFER_RECEIVED_EMAIL_SUBJECT = "[QA][Quick Quote] You received an offer";
			TC_REQUEST_FOR_CHANGE_EMAIL_SUBJECT = "[QA] [Quick Quote] You've received request for change! .email-template-customer/fmkb!";  //customized customer specific email message template
			TC_REQUEST_LIST = "Request list";
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
			SUPPLIER_USER1_LOGIN = "fmksqq";  //EasyOrderTest+fmksqq@gmail.com
			SUPPLIER_USER1_PASSWORD = "Qaw23edc!";

			SUPPLIER_USER2_LOGIN = "SVS1";//EasyOrderTest+SVS1user  EasyOrderTest+SV5
			SUPPLIER_USER2_PASSWORD = "Xsw23edc!";

			TC01_ASSERT_VIEWNAME = "1fmkV";
			TC01_CLICK_SAVE = "Save";
			TC01_ASSERT_BREADCRUMB = "Profile";
			TC01_LOCATION = "1370";  //london
			TC02_ASSERT_SUPPLIER1 = "fmks";
			TC02_ASSERT_SUPPLIER2 = "SV Supplier 1";
			TC02_PRODUCT_GROUP = "#img_27238";//QA Tests
			TC02_DATASHEET_SELECTOR = "#dsr_27245";//simple datasheet <input onclick="selectSupplier('')" type="radio" name="DataSheetRadio" id="dsr_27245" value="27245"> #dsr_27245
			TC02_MANDATORY_SUPPLIER_DATASHEET1_SELECTOR = "#dsr_27240";
			TC02_MANDATORY_SUPPLIER_DATASHEET2_SELECTOR = "#dsr_27241";
			TC02_COMPANYID = "fmkb";
			TC02_COMPANY_NAME = "fmkb";
			TC02_POPULATE_CLASSIFICATION_DATASHEET1_SELECTOR = "#dsr_27246";
			TC02_POPULATE_CLASSIFICATION_DATASHEET2_SELECTOR = "#dsr_27247";
			TC02_DEFAULT_CLASSIFICATON_CODE1 = "10101501";//Live Plant and Animal Material and Accessories and Supplies /Livestock / Cats
			TC02_DEFAULT_CLASSIFICATON_CODE2 = "10101502";//Live Plant and Animal Material and Accessories and Supplies /Livestock / Dogs
			TC02_SELECT_SUPPLIER1_ID = "4010405";//fmks
			TC02_SELECT_SUPPLIER2_ID = "4045657";//SV Supplier 1
			TC02_CLASS_CODE_LEVEL1 = "Expand Live Plant and Animal";
			TC02_CLASS_CODE_LEVEL2 = "Expand Live animals | (";
			TC02_CLASS_CODE_LEVEL3 = "Expand Livestock | ( 10101500";
			TC02_CLASS_CODE_LEVEL4 = "Cats | ( 10101501 )";
			TC02_EMPTY_REQUEST_POSITIONS = "5 ";
			TC05_PDF_ASSERT_CUSTOMERNAME = "fmkb";
			TC05_PDF_ASSERT_LOCATION = "123456 pvt Norway";
			TC06_LOCATION_ASSERT = "fmklocation";
			TC10_PDF_ASSERT_TOTAL_VALUE = "13.00";
			TC11_ASSET_POS_2_UNIT_PRICE = "2.00 EUR";
			TC11_ASSET_POS_3_UNIT_PRICE = "3.00 EUR";
			TC12_ASSERT_PDF_EMAIL = "EasyOrderTest+fmkbqq@gmail.com";

			TC21_FORM_DATASHEET1_SELECTOR = "#dsr_27248";//prod With Form - same class system
		
			TC21_EDIT_FORM1_OPTION = "309"; //testform1

			TC28_FORM_DATASHEET2_SELECTOR = "#dsr_27249";//prod With Form - form has own class system
			//TC2X_EDIT_FORM1_OPTION = ""; //testform2

			TC28_CLASS_CODE_LEVEL1 = "Expand Packing material | (";
			TC28_CLASS_CODE_LEVEL2 = "Expand Ampoule (packing";
			TC28_CLASS_CODE_LEVEL3 = "Expand Ampoule (glass,";
			TC28_CLASS_CODE_LEVEL4 = "OPC ampoule (glass, packing";  //20010101
			TC28_CLASS_CODE = "20010101";
			//GMAIL_EMAIL_ACCOUNT = "easyordertest@gmail.com";  //handles qa/uat/prod
			//GMAIL_PASSWORD = "Qu1ck,.Qu0t3/.,";
			//recovery email
			//ecatcmqq@gmail.com
			//Pr04actis

			TC34_COMPANYID = "SVB-0001";
			TC34_COMPANY_NAME = "SV Buyer";
			TC34_SIMPLE_DATASHEET_SELECTOR = "#dsr_27253";//Simple datasheet
			TC34_PRODUCT_GROUP = "#img_27252";// QA Tests
			TC34_ASSERT_SUPPLIER1 = "SV Supplier 1";
			TC34_ASSERT_SUPPLIER2 = "SV Supplier 2";
			TC34_EMPTY_REQUEST_POSITIONS = "5";
			TC34_SELECT_SUPPLIER1_ID = "4045657";
			TC34_SELECT_SUPPLIER2_ID = "4045785";

			TC35_CLASS_CODE_LEVEL1 = "Expand Live Plant and Animal";
			TC35_CLASS_CODE_LEVEL2 = "Expand Live animals | (";
			TC35_CLASS_CODE_LEVEL3 = "Expand Livestock | ( 10101500";
			TC35_CLASS_CODE_LEVEL4 = "Cats | ( 10101501 )";
		}

		if (Environment == "UAT")
		{
			//note none of this has been tested in uat, not sure the buyer.supplier accounts/datasheets/forms etc exist to test!!!!!!
			SEARCHURL = "";
			SEARCHURL2 = "";
			NEW_USER_SEARCHURL = "";
			CONTENTADMIN_LOGIN = "";
			CONTENTADMIN_PASSWORD = "";

			TC_REQUEST_RECEIVED_EMAIL_SUBJECT = "[Staging][Quick Quote] You have received a new Quick Quote request";
			TC_REQUEST_REJECTED_EMAIL_SUBJECT = "[Staging][Quick Quote] Request has been rejected!";
			TC_OFFER_RECEIVED_EMAIL_SUBJECT = "[Staging][Quick Quote] You received an offer";
			TC_REQUEST_FOR_CHANGE_EMAIL_SUBJECT = "[Staging][Quick Quote] You've received request for change!";

			TC_REQUEST_LIST = "Request list";
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
			SUPPLIER_USER1_LOGIN = "";
			SUPPLIER_USER1_PASSWORD = "";

			SUPPLIER_USER2_LOGIN = "";
			SUPPLIER_USER2_PASSWORD = "";

			TC01_ASSERT_VIEWNAME = "";
			TC01_CLICK_SAVE = "";
			TC01_ASSERT_BREADCRUMB = "";
			TC01_LOCATION = "";

			TC02_ASSERT_SUPPLIER1 = "";
			TC02_ASSERT_SUPPLIER2 = "";
			TC02_PRODUCT_GROUP = "#img_27238";
			TC02_DATASHEET_SELECTOR = "";
			TC02_COMPANY_NAME = "TESTCUSTCDO";
			TC02_DEFAULT_CLASSIFICATON_CODE1 = "21010101";
			TC02_DEFAULT_CLASSIFICATON_CODE2 = "21010101";//20010101
			TC02_SELECT_SUPPLIER1_ID = "869577";
			TC02_SELECT_SUPPLIER2_ID = "869578";
			TC02_COMPANYID = "";

			TC05_PDF_ASSERT_CUSTOMERNAME = "fmkb";
			TC05_PDF_ASSERT_LOCATION = "";
			TC06_LOCATION_ASSERT = "Mountain View, CA";
			TC10_PDF_ASSERT_TOTAL_VALUE = "13.00";
			TC11_ASSET_POS_2_UNIT_PRICE = "2.00 EUR";
			TC11_ASSET_POS_3_UNIT_PRICE = "3.00 EUR";

			TC12_ASSERT_PDF_EMAIL = "";
			TC21_FORM_DATASHEET1_SELECTOR = "#";//prod With Form - same class system
			TC21_EDIT_FORM1_OPTION = ""; //testform1

			TC28_FORM_DATASHEET2_SELECTOR = "#";//prod With Form - form has own class system
			TC28_CLASS_CODE_LEVEL1 = "Expand Packing material | (";
			TC28_CLASS_CODE_LEVEL2 = "Expand Ampoule (packing\"";
			TC28_CLASS_CODE_LEVEL3 = "Expand Ampoule (glass,";
			TC28_CLASS_CODE_LEVEL4 = "OPC ampoule (glass, packing";  //20010101
			TC28_CLASS_CODE = "20010101";

			TC34_COMPANYID = "SVB-0001";
			TC34_COMPANY_NAME = "SV Buyer";
			TC34_SIMPLE_DATASHEET_SELECTOR = "#";
			TC34_PRODUCT_GROUP = "#img_xxx?";// UAT Tests
			TC34_ASSERT_SUPPLIER1 = "";
			TC34_ASSERT_SUPPLIER2 = "";
			TC34_EMPTY_REQUEST_POSITIONS = "";
			TC34_SELECT_SUPPLIER1_ID = "";
			TC34_SELECT_SUPPLIER2_ID = "";
		}

		if (Environment == "PROD")
		{
			SEARCHURL = "https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&THEME=proactis&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp";//TESTCUSTCDO 4 for CMB upload
			SEARCHURL2 = "https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE05&VIEW_PASSWD=w4N7TtCc3g6A5&USER_ID=QQProdTest&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&THEME=proactis&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp";//TESTCUSTCDO 5 with Customer Classification
			NEW_USER_SEARCHURL = $"https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=[USERNAME]&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&THEME=proactis&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp";//TESTCUSTCDO 4 for CMB upload
			CONTENTADMIN_LOGIN = "wai-ho.leung@proactis.com";
			CONTENTADMIN_PASSWORD = "initpass7654321#";
			TC_REQUEST_RECEIVED_EMAIL_SUBJECT = "[Quick Quote] You have received a new Quick Quote request";
			TC_REQUEST_REJECTED_EMAIL_SUBJECT = "[Quick Quote] Request has been rejected!";
			TC_OFFER_RECEIVED_EMAIL_SUBJECT = "[Quick Quote] You received an offer";
			TC_REQUEST_FOR_CHANGE_EMAIL_SUBJECT = "[Quick Quote] You've received request for change!";
			TC_REQUEST_LIST = "Anfragen";
			PORTAL_LOGIN = "https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F";
			PORTAL_MAIN_URL = "https://portal.hubwoo.com/main/";
			CMA_ADMIN_COMPANY_FIND_URL = "https://portal.hubwoo.com/srvs/Contentadmin/AdminCompanyFind2007.aspx";
			PORTAL_LOGOUT = "https://portal.hubwoo.com/srvs/login/logout";
			QQB_REQUEST_LIST_URL = "https://portal.hubwoo.com/srvs/easyorder/RequestList2007.aspx";
			QQS_REQUEST_LIST_URL = "https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx";
			QQS_OFFER_DETAIL_URL = "https://portal.hubwoo.com/srvs/easyorder/OfferDetail2007.aspx";
			QQS_OFFER_DETAIL_URL_REGEX = "^https://portal.hubwoo.com/srvs/easyorder/OfferDetail2007.aspx";
			ADMIN_RELATIONEDIT_REGEX = "^https://portal.hubwoo.com/srvs/Contentadmin/AdminRelationEdit2007.aspx";
			ADMIN_EO_PROPERTIES_EDIT_REGEX = "^https://portal.hubwoo.com/srvs/ContentAdmin/AdminEOPropertiesEdit.aspx";
			CUSTOM_FORM_EDIT_REGEX = "^https://portal.hubwoo.com/srvs/easyorder/CustomForms.aspx";
			SUPPLIER_USER1_LOGIN = "EPAM_TS4";
			SUPPLIER_USER1_PASSWORD = "xsw23edc";
			SUPPLIER_USER2_LOGIN = "EPAM_TS4";
			SUPPLIER_USER2_PASSWORD = "xsw23edc";
			TC01_ASSERT_VIEWNAME = "TESTCOE04";
			TC01_CLICK_SAVE = "Speichern";//
			TC01_ASSERT_BREADCRUMB = "Profil";
			TC01_LOCATION = "4123";//hanover
			TC02_ASSERT_SUPPLIER1 = "TESTSUPCDO4";
			TC02_ASSERT_SUPPLIER2 = "TESTSUPCDO5";
			TC02_PRODUCT_GROUP = "#img_44521";//PROD Tests
			TC02_COMPANYID = "TESTCUSTCDO-0004";
			TC02_COMPANY_NAME = "TESTCUSTCDO 4 for CMB upload";
			TC02_DATASHEET_SELECTOR = "#dsr_44522";  //simple datasheet selector
			TC02_MANDATORY_SUPPLIER_DATASHEET1_SELECTOR = "#dsr_47742";
			TC02_MANDATORY_SUPPLIER_DATASHEET2_SELECTOR = "#dsr_47743";
			TC02_POPULATE_CLASSIFICATION_DATASHEET1_SELECTOR = "#dsr_47783";
			TC02_POPULATE_CLASSIFICATION_DATASHEET2_SELECTOR = "#dsr_47784";
			TC02_DEFAULT_CLASSIFICATON_CODE1 = "20010101"; //eclass 4.0  
			TC02_DEFAULT_CLASSIFICATON_CODE2 = "21010101"; //eclass 4.0
			TC02_SELECT_SUPPLIER1_ID = "869577"; //TESTSUPCDO4
			TC02_SELECT_SUPPLIER2_ID = "869578"; //TESTSUPCDO5
			TC02_CLASS_CODE_LEVEL1 = "Expand Construction";
			TC02_CLASS_CODE_LEVEL2 = "Expand Building construction";
			TC02_CLASS_CODE_LEVEL3 = "Expand Building construction";
			TC02_CLASS_CODE_LEVEL4 = "Shell | ( 22010101 )";
			TC02_EMPTY_REQUEST_POSITIONS = "2 ";

			TC05_PDF_ASSERT_CUSTOMERNAME = "TESTCUSTCDO 4 for CMB upload";
			TC05_PDF_ASSERT_LOCATION = "Mountain View, CA (CA)";
			TC06_LOCATION_ASSERT = "Mountain View, CA";
			TC10_PDF_ASSERT_TOTAL_VALUE = "13.00";
			TC11_ASSET_POS_2_UNIT_PRICE = "2.00 USD";
			TC11_ASSET_POS_3_UNIT_PRICE = "3.00 USD";
			TC12_ASSERT_PDF_EMAIL = "TESTCOE04QQ@gmail.com";
			TC21_FORM_DATASHEET1_SELECTOR = "#dsr_44523";//prod With Form - same class system
			TC21_EDIT_FORM1_OPTION = "578"; //testform1
			TC28_FORM_DATASHEET2_SELECTOR = "#dsr_44524";//prod With Form - form has own class system
			TC28_CLASS_CODE_LEVEL1 = "Expand Live Plant and Animal";
			TC28_CLASS_CODE_LEVEL2 = "Expand Live animals | (";
			TC28_CLASS_CODE_LEVEL3 = "Expand Livestock | ( 10101500";
			TC28_CLASS_CODE_LEVEL4 = "Cats | ( 10101501 )"; 
			TC28_CLASS_CODE = "10101501";

			TC34_COMPANY_NAME = "TESTCUSTCDO 5 with Customer Classification";
			TC34_COMPANYID = "TESTCUSTCDO-0005";
			TC34_SIMPLE_DATASHEET_SELECTOR = "#dsr_47959";
			TC34_PRODUCT_GROUP = "#img_47958"; // PROD Tests
			TC34_ASSERT_SUPPLIER1 = "TESTSUPCDO4";
			TC34_ASSERT_SUPPLIER2 = "TESTSUPCDO5";
			TC34_EMPTY_REQUEST_POSITIONS = "2";
			TC34_SELECT_SUPPLIER1_ID = "869577"; //TESTSUPCDO4
			TC34_SELECT_SUPPLIER2_ID = "869578"; //TESTSUPCDO5

			TC35_CLASS_CODE_LEVEL1 = "Expand Construction";
			TC35_CLASS_CODE_LEVEL2 = "Expand Building construction";
			TC35_CLASS_CODE_LEVEL3 = "Expand Building construction";
			TC35_CLASS_CODE_LEVEL4 = "Shell | ( 22010101 )";
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
			//downloadPath example C:\Sourcegit\ecat2023\catalog-manager\PlaywrightTests\PlaywrightTests\bin\Debug\net7.0\QATESTRESULTS\QQ";
			downloadPath = Path.Combine(directory, @"QATESTRESULTS\QQ\");
		}

		if (Environment == "UAT")
		{
			//downloadPath example C:\Sourcegit\ecat2023\catalog-manager\PlaywrightTests\PlaywrightTests\bin\Debug\net7.0\UATTESTRESULTS\QQ";
			downloadPath = Path.Combine(directory, @"UATTESTRESULTS\QQ\");
		}

		if (Environment == "PROD")
		{
			//downloadPath example  C:\Sourcegit\ecat2023\catalog-manager\PlaywrightTests\PlaywrightTests\bin\Debug\net7.0\PRODTESTRESULTS\QQ\
			downloadPath = Path.Combine(directory, @"PRODTESTRESULTS\QQ\");
			Console.WriteLine("downloadPath: " + downloadPath);
		}
	}


	[OneTimeTearDown]
  public void OneTimeTearDown()
	{
		//runs once after all tests have finished
		Console.WriteLine("OneTimeTearDown");
	}

	[SetUp]
	public void  SetUp()
	{
		Console.WriteLine("Test started " + testStarted);
		//runs before each test starts
		Console.WriteLine("SetUp");
		Console.WriteLine(Browser.BrowserType.Name);
		Console.WriteLine(Browser.BrowserType.ExecutablePath);
		Console.WriteLine("requestToRejectTransactionName :" + requestToRejectTransactionName);
		Console.WriteLine("requestToOfferTransactionName :" + requestToOfferTransactionName);
		Console.WriteLine("requestToRejectId :" + requestToRejectId);
		Console.WriteLine("requestToOfferId :" + requestToOfferId);

		Console.WriteLine("requestPositionFormTransactionName :" + requestPositionFormTransactionName);
		Console.WriteLine("requestIdPositionForm  :" + requestIdPositionForm);

		Console.WriteLine("requestPositionRequestFormTransactionName :" + requestPositionRequestFormTransactionName);
		Console.WriteLine("requestIdRequestForm  :" + requestIdRequestForm);

		Console.WriteLine("Environment :" + Environment);
		Console.WriteLine("email epoch after: " + testStartSecondsSinceEpoch);
		_browserName = Browser.BrowserType.Name;
	}

	[TearDown]
  public void TearDown()
	{
		//runs after each test finishes
		Console.WriteLine("TearDown");
		//delete any created requests
	}



	[Test, Order(1)]
	[Category("EmailTests")]
	public void TC01_Gmail_EmailTester_Setup()
	{
		Console.WriteLine("TC01 GMAIL Mailtester setup");
		//an initial email test so that any requirement for gmail login/mailtester googleapi app authentication is established early in the cycle
		//before starting this test ensure you have edge browser open
		//you may get a new tab open to a url h similar to https://accounts.google.com/o/oauth2/v2/auth/oauthchooseaccount?access_type=offline&code_challenge=iA3bc660WXg4DC47beoNPoHlv70sdtj1X6Ri4eaqJY0&code_challenge_method=S256&response_type=code&client_id=1088739761220-ohpuqop5drhjfp9ko1bj4adedfagtheh.apps.googleusercontent.com&redirect_uri=http%3A%2F%2F127.0.0.1%3A52332%2Fauthorize%2F&scope=https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fgmail.readonly&service=lso&o2v=2&ddm=0&flowName=GeneralOAuthFlow
		//requiring you to choose an email account, you should choose omnicontent@gmail.com

		Console.WriteLine("email : easyordertest@gmail.com");
		Console.WriteLine("password : Qu1ck,.Qu0t3/.,");
		Console.WriteLine("instantiate gmail api service");

		UserCredential credential;
		// Load client secrets.
		using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
		{
			/* The file token.json stores the user's access and refresh tokens, and is created
				automatically when the authorization flow completes for the first time. */
			string credPath = "token.json";
			credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.FromStream(stream).Secrets,
					Scopes,
					"user",
					CancellationToken.None,
					new FileDataStore(credPath, true)).Result;
			Console.WriteLine("Credential file saved to: " + credPath);
		}

		System.Threading.Thread.Sleep(2000);

		// Create Gmail API service.
		bool connected = false;
		GmailService? service = null;
		IList<Message> messages = new List<Message>();
		int connectionAttempt = 0;
		while (!connected && connectionAttempt < 10 && service == null)
		{
			try
			{
				service = new GmailService(new BaseClientService.Initializer
				{
					HttpClientInitializer = credential,
					ApplicationName = ApplicationName
				});
				UsersResource.MessagesResource.ListRequest requestMessage = service.Users.Messages.List("easyordertest@gmail.com");
				requestMessage.LabelIds = "INBOX";
				requestMessage.IncludeSpamTrash = false;
				requestMessage.Q = $"after:{1709650575} subject:({TC_OFFER_RECEIVED_EMAIL_SUBJECT})";
				Console.WriteLine("Q. = " + requestMessage.Q);
				messages = requestMessage.Execute().Messages;
				connected = true;
			}
			catch (Exception ex)
			{
				Console.WriteLine("gmail exception: " + ex.Message);
				connectionAttempt++;
				Console.WriteLine("gmail service instantiation attempt: " + connectionAttempt.ToString());
			}
		}
		Assert.That(messages.Count, Is.GreaterThan(0));//assume there are some emails with the subject "You received an offer"
		if (messages == null || messages.Count == 0)
		{
			Console.WriteLine("testing messages");
			Console.WriteLine(" message count 0");
		}
		else
		{
			Console.WriteLine("testing messages");
			Console.WriteLine("messages count: " + messages.Count.ToString());
		}
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(1)]

	[Category("QQTests")]
	async public Task TC01_QQB_New_User_Login_Gets_Redirected_To_Profile_Page()
	{
		/*
		 asearch url with a unique USER_ID for a user that has not previously been regisered should be redirected to the profile.aspx page
		NOTE: on prod the company used to perform the test renders the text in german on the profile page
		*/
		Console.WriteLine("logon as new user in new search UI");
		Console.WriteLine("1: QQB_New_User_Login_Gets_Redirected_To_Profile_Page");
		Console.WriteLine(Browser.BrowserType.Name);
		DateTime today = DateTime.Now;

		string CurrentDate = $"{today.Minute}_{today.Second}_{today.Day}_{today.Month}_{today.Year}";
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 180000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
		string userName = "User_" + _browserName + "_" + CurrentDate;
		string url = NEW_USER_SEARCHURL;
		url = url.Replace("[USERNAME]", userName);

		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loggedin = true;
			}
			catch (Exception ex)
			{
				attempts++;
				Console.WriteLine("exception: " + ex.Message);
				Console.WriteLine(DateTime.Now.ToString());
				Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
				//seeing a lot of errors of type
				/*
						Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
				*/
			}
		}
		if (loggedin == false && attempts >= 10)
		{
			DateTime timeRightNow = DateTime.Now;
			string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC01_LoginError_" + FileCurrentDate + ".png"
			});
		}

		Console.WriteLine("page: " + Page.Url);

		//assert search has a quick quote link for this view/company
		await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, locatorClickOptions);

		//assert visibility of profile breadcrumb
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToBeVisibleAsync(locatorVisibleAssertion);

		//await Expect(Page.FrameLocator("#qqFrame").ToHaveURLAsync(new Regex("https://portal.qa.hubwoo.com/srvs/easyorder/UserProperties2007.aspx"));
		//cannot do this because of iframe

		//var iframeElement = await Page.Locator("iframe").ElementHandleAsync();
		//var frame = await iframeElement.ContentFrameAsync();
		//if (frame != null && frame.Url != "")
		//{
		//	Console.WriteLine("qqFrame Url : " + frame.Url);
		//	//await frame.WaitForURLAsync("https://portal.hubwoo.com/srvs/easyorder/UserProperties2007.aspx");
		//}

		//assert view name is 1fmkv (qa) or coe04 (prod)
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblViewName")).ToContainTextAsync(TC01_ASSERT_VIEWNAME);

		//assert breadcrumb is profil
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync(TC01_ASSERT_BREADCRUMB);

		//complete user profile details

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbFirstName").FillAsync("playwright_automated_test");

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSureName").FillAsync(CurrentDate);

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbEMail").FillAsync(CurrentDate + "@test.com");
	
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbTelephone").FillAsync("123456");

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlLocation").SelectOptionAsync(new[] { TC01_LOCATION });//select london (qa), hanover (prod)

		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = TC01_CLICK_SAVE }).ClickAsync(locatorClickOptions);//click save

		//assert user is redirected to requestlist2007 page after saving profile
		//Anfragen ==  requests
		await Expect(Page.FrameLocator("#qqFrame").GetByText(TC_REQUEST_LIST, new() { Exact = true })).ToBeVisibleAsync(locatorVisibleAssertion);
		//assert url of frame, couldn't work out how to do that, url is not in the src of the iframe but in the #document?

		//navigate back to profile and confirm details have saved correctly
		Console.WriteLine("Expect breadcrumb to contain text: " + TC_REQUEST_LIST);
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync(TC_REQUEST_LIST);
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = TC01_ASSERT_BREADCRUMB }).ClickAsync(locatorClickOptions);

		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync(TC01_ASSERT_BREADCRUMB);
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbFirstName")).ToHaveValueAsync("playwright_automated_test");
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSureName")).ToHaveValueAsync(CurrentDate);
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbEMail")).ToHaveValueAsync(CurrentDate + "@test.com");
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbTelephone")).ToHaveValueAsync("123456");
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlLocation")).ToHaveValueAsync(TC01_LOCATION);

		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(2)]
	[Category("QQTests")]
	async public Task TC02_QQB_Create_A_Request()
	{
		/*
		 * test flip flops between different mandatory and non mandatory datasheets and ultimately creates a request with 3 request positions
		 */
		//try
		//{
			Console.WriteLine("2: QQB_2_Create_A_Request");
			//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
			//corresponds with test case 181256 "Create a request" in dev ops prod smoke tests
			//preconditions:
			/*
			Following QQ properties are ASSIGNED AND ENABLED
			BUYER_CHOOSES_CLASSIFICATION
			ENABLE_DATASHEET_CLASSIFICATION

			assume the classification group is eclass-4.0
			assume that the classification code configured with the datasheets corresponds with the test performed below
			*/
			List<string> propertiesToSetTrue = new List<string>();
			List<string> propertiesToSetFalse = new List<string>();

			////////////FOR TESTING ////////////////////////////////
			//propertiesToSetFalse.Add("MICHELIN_LOCAL_CODE_SUPPLIER_WARNING");
			//propertiesToSetFalse.Add("BUYER_CHOOSES_CLASSIFICATION");
			//propertiesToSetFalse.Add("ENABLE_DATASHEET_CLASSIFICATION");
			///////////////////////////
			///
			if (String.IsNullOrEmpty(_browserName))
			{
				_browserName = Browser.BrowserType.Name;
			}

			Console.WriteLine("Running test QQB_Create_A_Request... Setting EasyOrder Property Preconditions");
			//configure easy order property preconditions for the company with the catalogid TESTCUSTCDO-0004 i.e.
			//await QQB_ConfigureEasyOrderPropertiesForCompany(propertiesToSetTrue, propertiesToSetFalse, TC02_COMPANYID);//set BUYER_CHOOSES_CLASSIFICATION = false, ENABLE_DATASHEET_CLASSIFICATION = false

			//propertiesToSetFalse.Clear();
			propertiesToSetTrue.Add("BUYER_CHOOSES_CLASSIFICATION");
			propertiesToSetTrue.Add("ENABLE_DATASHEET_CLASSIFICATION");

			//await QQB_ConfigureEasyOrderPropertiesForCompany(propertiesToSetTrue, propertiesToSetFalse, TC02_COMPANYID);//set BUYER_CHOOSES_CLASSIFICATION = true, ENABLE_DATASHEET_CLASSIFICATION = true
			string url = SEARCHURL;
			DateTime today = DateTime.Now;
			string CurrentDate = $"{today.Year}{today.Month}{today.Day}{today.Hour}{today.Minute}";
			string transactionID = "PW_Auto_RequestToReject_" + _browserName + "_" + CurrentDate;//this transactionid will be used in other tests in this suite!
			string commentDate = $"{today.Year}{today.Month}{today.Day}";
			//string comment = CurrentDate;
			requestToRejectTransactionName = transactionID;//allows this request to be opened in another test and be referenced during teardown

			Console.WriteLine("Creating request...  " + requestToRejectTransactionName);
			PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 180000, State = WaitForSelectorState.Visible };
			LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
			LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };
			LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
			LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
			LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
			PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };
			PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };

			Console.WriteLine("login to search:" + DateTime.Now.ToLongTimeString());
			bool loggedin = false;
			int attempts = 0;
			while (loggedin == false && attempts < 10)
			{
				try
				{
					await Page.GotoAsync(url, pageGotoOptions);
					await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
					loggedin = true;
				}
				catch (Exception ex)
				{
					attempts++;
					Console.WriteLine("exception: " + ex.Message);
					Console.WriteLine(DateTime.Now.ToString());
					Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
					//seeing a lot of errors of type
					/*
							Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
					*/
				}
			}
			if (loggedin == false && attempts >= 10)
			{
				DateTime timeRightNow = DateTime.Now;
				string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
				await Page.ScreenshotAsync(new()
				{
					FullPage = true,
					Path = downloadPath + "TC02_LoginError_" + FileCurrentDate + ".png"
				});
			}

			Console.WriteLine("page: " + Page.Url);

			// Assert that the search has a Quick Quote link for this view/company
			await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

			var iframeElement = await Page.Locator("iframe").ElementHandleAsync();
			var frame = await iframeElement.ContentFrameAsync();
			if (frame != null && frame.Url != "")
			{
				Console.WriteLine("qqFrame Url : " + frame.Url);
				//await frame.WaitForURLAsync("https://portal.hubwoo.com/srvs/easyorder/RequestList2007.aspx");
			}

			await Task.Delay(3000);
			
			Console.WriteLine("wait for request list page");
			//assert on request list page quick filter

			//sometimes takes bloody ages for qq to load
			Console.WriteLine("waiting for loadingScreen to disappear");
			int attempt = 0;
			var isLoadingScreenVisible = await Page.Locator("#spin_modal_overlay").IsVisibleAsync();
			while (isLoadingScreenVisible && attempt < 10)
			{
				try
				{
					await Expect(Page.Locator("#spin_modal_overlay")).Not.ToBeVisibleAsync(locatorVisibleAssertion);
					isLoadingScreenVisible = false;
					Console.WriteLine("loadingScreen gone");
					break;
				}
				catch
				{
					attempt++;
					isLoadingScreenVisible = await Page.Locator("#spin_modal_overlay").IsVisibleAsync();
				}
			}

			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
			Console.WriteLine("wait for qqb request list");
			await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

			//get current request count
			var currentrequestCount = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount").TextContentAsync();

			//click create request redirected to DataSheetChoose.asp
			Console.WriteLine("click Create a request");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbCreateRequestTop").ClickAsync(locatorClickOptions);

			//////////////////Simple Datasheet
			///
			//how to perform WaitForURLAsync in iframe
			//iframeElement = await Page.Locator("iframe").ElementHandleAsync();
			iframeElement = await Page.Locator("#qqFrame").ElementHandleAsync();
			frame = await iframeElement.ContentFrameAsync();
			if (frame != null && frame.Url != "")
			{
				Console.WriteLine("qqFrame Url : " + frame.Url);
				//await frame.WaitForURLAsync("https://portal.hubwoo.com/srvs/easyorder/DataSheetChoose.aspx");
			}


			await Task.Delay(3000);
			

			//assert on datasheetchoose page
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Product Group", locatorToContainTextOption);

			//expand product group QA Tests (qa)  / PROD Tests (Prod) use the unique id on the image as a locator
			Console.WriteLine("expand product group");
			await Page.FrameLocator("#qqFrame").Locator(TC02_PRODUCT_GROUP).ClickAsync();

			//select simple datasheet
			//await Page.FrameLocator("#qqFrame").GetByLabel("Simple datasheet").CheckAsync();
			//fails because not unique 2 examples also on the suppliers tab of datasheet choose
			//should use the img id as it is unique either datasheet header.Id or supplier.Id
			//	<img id="img_<%=header.Id %>" src="./Design2007/img/icons/plus.jpg" alt="+" />
			Console.WriteLine("select simple datasheet");
			await Page.FrameLocator("#qqFrame").Locator(TC02_DATASHEET_SELECTOR).ClickAsync();

			//click choose
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Choose" }).ClickAsync(locatorClickOptions);

			//assert on requestcreate page via the request details breadcrumb

			Console.WriteLine("wait for request details page to load...");
			await Expect(Page.FrameLocator("#qqFrame").GetByText("Request Details")).ToBeVisibleAsync(locatorVisibleAssertion);

			//assert that the selected product group is simple datasheet
			//await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbProductGroup")).ToContainTextAsync("Simple datasheet");
			//wont work there is no innertext ,the data is stored in the value attribute of the control
			var readonlyInput = Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbProductGroup");

			await Expect(readonlyInput).ToBeDisabledAsync();

			var selectedDataSheet = await readonlyInput.GetAttributeAsync("value");

			Assert.That(selectedDataSheet == "Simple datasheet");

			//assert transactionid is empty
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbTransactionNumber")).ToBeEmptyAsync();

			//assert that no supplier selected
			await Expect(Page.FrameLocator("#qqFrame").Locator("#selectSelectedSuppliers")).ToBeEmptyAsync();

			//assert that available suppliers contains 2 suppliers
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repExistingSuppliers_ctl00_Option1")).ToContainTextAsync(TC02_ASSERT_SUPPLIER1);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repExistingSuppliers_ctl01_Option1")).ToContainTextAsync(TC02_ASSERT_SUPPLIER2);

			//assert there are 2 default empty request positions
			//5 default empty ositions in qa/ 2 empty default positions in prod
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblPositionCount")).ToContainTextAsync(TC02_EMPTY_REQUEST_POSITIONS);

			//click the change datasheet icon
			Console.WriteLine("change datasheet to MANDATORY SUPPLIER 1");
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Press here to select another" }).ClickAsync(locatorClickOptions);

			//////////////////////////////MANDATORY SUPPLIER 1

			//assert that user back on datasheet chooose page
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Product Group", locatorToContainTextOption);

			//expand prodtest product group
			await Page.FrameLocator("#qqFrame").Locator(TC02_PRODUCT_GROUP).ClickAsync();

			//select mandatory supplier1 datasheet
			//await Page.FrameLocator("#qqFrame").GetByLabel("Mandatory Supplier 1").CheckAsync();
			await Page.FrameLocator("#qqFrame").Locator(TC02_MANDATORY_SUPPLIER_DATASHEET1_SELECTOR).ClickAsync(locatorClickOptions);

			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Choose" }).ClickAsync(locatorClickOptions);//click choose

			//assert back on request details page
			await Expect(Page.FrameLocator("#qqFrame").GetByText("Request Details")).ToBeVisibleAsync(locatorVisibleAssertion);

			//assert datasheet collection is Mandatory Supplier 1
			readonlyInput = Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbProductGroup");

			await Expect(readonlyInput).ToBeDisabledAsync();

			selectedDataSheet = await readonlyInput.GetAttributeAsync("value");

			Assert.That(selectedDataSheet == "Mandatory Supplier 1");

			//assert selected supplier
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repSelectedSuppliers_ctl00_Option2")).ToContainTextAsync(TC02_ASSERT_SUPPLIER1);

			//assert available supplier
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repExistingSuppliers_ctl00_Option1")).ToContainTextAsync(TC02_ASSERT_SUPPLIER2);

			//assert that available suppliers does not contain TESTSUPCDO4
			//await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repExistingSuppliers_ctl00_Option1")).Not.ToContainTextAsync("TESTSUPCDO4");
			//assert on 1 empty position
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblPositionCount")).ToContainTextAsync("1 ");

			//////////////////MANDATORY SUPPLIER 2
			//reset datasheet
			Console.WriteLine("change datasheet to MANDATORY SUPPLIER 2");
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Press here to select another" }).ClickAsync(locatorClickOptions);

			//assert datasheet choose
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Product Group");

			//expand  datagroup
			await Page.FrameLocator("#qqFrame").Locator(TC02_PRODUCT_GROUP).ClickAsync(locatorClickOptions);


			//select mandatory supplier 2 datasheet
			//await Page.FrameLocator("#qqFrame").GetByLabel("Mandatory Supplier 2").CheckAsync();
			await Page.FrameLocator("#qqFrame").Locator(TC02_MANDATORY_SUPPLIER_DATASHEET2_SELECTOR).ClickAsync(locatorClickOptions);

			//click choose
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Choose" }).ClickAsync(locatorClickOptions);

			//assert request create page
			await Expect(Page.FrameLocator("#qqFrame").GetByText("Request Details")).ToBeVisibleAsync(locatorVisibleAssertion);

			//assert on 1 empty position
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblPositionCount")).ToContainTextAsync("1 ");

			//assert selected datagroup
			readonlyInput = Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbProductGroup");

			await Expect(readonlyInput).ToBeDisabledAsync();

			selectedDataSheet = await readonlyInput.GetAttributeAsync("value");

			Assert.That(selectedDataSheet == "Mandatory Supplier 2");

			//assert available supplier
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repExistingSuppliers_ctl00_Option1")).ToContainTextAsync(TC02_ASSERT_SUPPLIER1);

			//assert selected suppliers
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repSelectedSuppliers_ctl00_Option2")).ToContainTextAsync(TC02_ASSERT_SUPPLIER2);


			//assert classification popup icon present in request positions i.e. that the BUYER_CHOOSES_CLASSIFICATION setting is enabled and honoured by the UI
			//ctl00_MainContent_repRequestPosition_ctl01_ibShowClassification

			Console.WriteLine("Is ibShowClassification visible?");
			bool isClassificationIconVisible = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_ibShowClassification").IsVisibleAsync();
			if(!isClassificationIconVisible)
			{
				Console.WriteLine("ibShowClassification is not visible!");
				Console.WriteLine("check whether BUYER_CHOOSES_CLASSIFICATION is enabled for " + TC05_PDF_ASSERT_CUSTOMERNAME);
			}
			else
			{
				Console.WriteLine("ibShowClassification is visible?");
			}

			try
			{
				await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_ibShowClassification")).ToBeVisibleAsync(locatorVisibleAssertion);
			}
			catch
			{
				Console.WriteLine("ibShowClassification is not visible?");
				Console.WriteLine("check whether BUYER_CHOOSES_CLASSIFICATION is enabled for " + TC05_PDF_ASSERT_CUSTOMERNAME);
			}

			//assert textbox present for classification code
			//ctl00_MainContent_repRequestPosition_ctl01_tbEclassCode
			try
			{
				await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbEclassCode")).ToBeVisibleAsync(locatorVisibleAssertion);
			}
			catch
			{
				Console.WriteLine("tbEclassCode is not visible?");
				Console.WriteLine("check whether BUYER_CHOOSES_CLASSIFICATION is enabled for " + TC05_PDF_ASSERT_CUSTOMERNAME);
			}
			/*
			 when BUYER_CHOOSES_CLASSIFICATION = false
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_ibShowClassification")).ToHaveCountAsync(0);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbEclassCode")).ToHaveCountAsync(0);
			 */

			/////////////////// POPULATE CLASSIFICATION 1

			//reset datasheet
			Console.WriteLine("change datasheet to POPULATE CLASSIFICATION 1");
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Press here to select another" }).ClickAsync(locatorClickOptions);
			//assert datasheet choose
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Product Group");

			//expand datagroup
			await Page.FrameLocator("#qqFrame").Locator(TC02_PRODUCT_GROUP).ClickAsync(locatorClickOptions);

			//select datasheet: populate classification 1
			//await Page.FrameLocator("#qqFrame").GetByLabel("Populate Classification 1").CheckAsync();
			await Page.FrameLocator("#qqFrame").Locator(TC02_POPULATE_CLASSIFICATION_DATASHEET1_SELECTOR).ClickAsync(locatorClickOptions);

			//click choose ctl00_MainContent_lblSave
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblSave").ClickAsync(locatorClickOptions);

			//assert request create page
			await Expect(Page.FrameLocator("#qqFrame").GetByText("Request Details")).ToBeVisibleAsync(locatorVisibleAssertion);

			//assert on 1 empty position
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblPositionCount")).ToContainTextAsync("1 ");

			//assert selected datagroup ctl00_MainContent_tbProductGroup
			readonlyInput = Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbProductGroup");

			await Expect(readonlyInput).ToBeDisabledAsync();

			selectedDataSheet = await readonlyInput.GetAttributeAsync("value");

			Assert.That(selectedDataSheet == "Populate Classification 1");

			//assert available supplier
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repExistingSuppliers_ctl01_Option1")).ToContainTextAsync(TC02_ASSERT_SUPPLIER2);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repExistingSuppliers_ctl00_Option1")).ToContainTextAsync(TC02_ASSERT_SUPPLIER1);

			//assert selected suppliers is empty
			await Expect(Page.FrameLocator("#qqFrame").Locator("#selectSelectedSuppliers")).ToBeEmptyAsync();

			//assert prepopulated classification code in both empty request positions is 20010101 uat or qa
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbEclassCode")).ToHaveValueAsync(TC02_DEFAULT_CLASSIFICATON_CODE1);

			////////////////////////////////POPULATE CLASSIFICATION 2

			//reset datasheet
			Console.WriteLine("change datasheet to POPULATE CLASSIFICATION 2");
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Press here to select another" }).ClickAsync(locatorClickOptions);
			//assert datasheet choose
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Product Group");
			//expand product group
			await Page.FrameLocator("#qqFrame").Locator(TC02_PRODUCT_GROUP).ClickAsync(locatorClickOptions);

			//select datasheet: populate classification 2
			//await Page.FrameLocator("#qqFrame").GetByLabel("Populate Classification 2").CheckAsync();
			await Page.FrameLocator("#qqFrame").Locator(TC02_POPULATE_CLASSIFICATION_DATASHEET2_SELECTOR).ClickAsync(locatorClickOptions);

			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Choose" }).ClickAsync();
			//assert selected datagroup ctl00_MainContent_tbProductGroup
			readonlyInput = Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbProductGroup");

			await Expect(readonlyInput).ToBeDisabledAsync();

			selectedDataSheet = await readonlyInput.GetAttributeAsync("value");

			Assert.That(selectedDataSheet == "Populate Classification 2");

			//assert available supplier
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repExistingSuppliers_ctl00_Option1")).ToContainTextAsync(TC02_ASSERT_SUPPLIER1);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repExistingSuppliers_ctl01_Option1")).ToContainTextAsync(TC02_ASSERT_SUPPLIER2);

			//assert selected suppliers is empty
			await Expect(Page.FrameLocator("#qqFrame").Locator("#selectSelectedSuppliers")).ToBeEmptyAsync();
			//assert prepopulated classification code in the empty request position is 21010101

			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbEclassCode")).ToHaveValueAsync(TC02_DEFAULT_CLASSIFICATON_CODE2);

			//note the ui should not really allow user to type in a class code they should be forced to selected a code from the picker!!

			Console.WriteLine("change datasheet to POPULATE CLASSIFICATION 1");

			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Press here to select another" }).ClickAsync();

			//expand the required product group
			await Page.FrameLocator("#qqFrame").Locator(TC02_PRODUCT_GROUP).ClickAsync(locatorClickOptions);

			//await Page.FrameLocator("#qqFrame").GetByLabel("Populate Classification 1").CheckAsync();
			await Page.FrameLocator("#qqFrame").Locator(TC02_POPULATE_CLASSIFICATION_DATASHEET1_SELECTOR).ClickAsync(locatorClickOptions);

			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblSave").ClickAsync(locatorClickOptions);

			//assert back on request create page
			await Expect(Page.FrameLocator("#qqFrame").GetByText("Request Details")).ToBeVisibleAsync(locatorVisibleAssertion);

			//add transaction number and internal / external comments
			Console.WriteLine("complete request details");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbTransactionNumber").FillAsync(transactionID);

			//comment controls need to be clicked before textbox is available
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Add External Comments (" }).ClickAsync();
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbExternalComment").FillAsync("external comment " + commentDate);

			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Add Internal Comments" }).ClickAsync();
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbInternalComment").FillAsync("internal comment " + commentDate);

			//add both suppliers
			await Page.FrameLocator("#qqFrame").Locator("#selectExistingSuppliers").SelectOptionAsync(new[] { TC02_SELECT_SUPPLIER1_ID});
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync();
			await Page.FrameLocator("#qqFrame").Locator("#selectExistingSuppliers").SelectOptionAsync(new[] { TC02_SELECT_SUPPLIER2_ID });
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync(locatorClickOptions);

			//add additional request position
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbAddPositionBottom").ClickAsync(locatorClickOptions);
			//assert there are 2 positions now
			Console.WriteLine("assert there are 2 request positions");
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblPositionCount")).ToContainTextAsync("2 )");

			//add additional request position
			Console.WriteLine("Add an additional request position");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbAddPositionBottom").ClickAsync(locatorClickOptions);
			Console.WriteLine("assert there are now 3 request positions");
			//assert there are 3 request positions now
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblPositionCount")).ToContainTextAsync("3 )");

			//added above expect as the next line is skipped and performs the description input but doesn't perform the title input???
			//test runs differently if headed/headless and run vs debug
			Console.WriteLine("add short description pos1");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbShortDescription").FillAsync("item title pos1");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbLongDescription").FillAsync("item description pos1");

			//select classification code 22010101  construction technology|building technology|building construction (gen.)|shell
			Console.WriteLine("add classification pos1");
			var Page6 = await Page.RunAndWaitForPopupAsync(async () =>
			{
				await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_ibShowClassification").ClickAsync(locatorClickOptions);
			}, new PageRunAndWaitForPopupOptions { Timeout = 180000 });

			await Expect(Page6.Locator("#tbSearchField")).ToBeVisibleAsync(locatorVisibleAssertion);

			await Page6.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL1 }).ClickAsync(locatorClickOptions);
			await Page6.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL2 }).ClickAsync(locatorClickOptions);
			await Page6.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL3 }).ClickAsync(locatorClickOptions);
			await Page6.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL4 }).ClickAsync(locatorClickOptions);

			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbQuantity").FillAsync("1");

			//complete request position 2
			Console.WriteLine("complete offer pos2");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbShortDescription").FillAsync("item title pos2");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbLongDescription").FillAsync("item description pos2");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbQuantity").FillAsync("2");

			Console.WriteLine("add classification pos2");
			await Task.Delay(3000);

			var Page9 = await Page.RunAndWaitForPopupAsync(async () =>
			{
				await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_ibShowClassification").ClickAsync(locatorClickOptions);
			}, new PageRunAndWaitForPopupOptions { Timeout = 180000});

			await Page.WaitForTimeoutAsync(3000);

			await Expect(Page9.Locator("#tbSearchField")).ToBeVisibleAsync();
			await Page9.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL1 }).ClickAsync(locatorClickOptions);
			await Page9.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL2 }).ClickAsync(locatorClickOptions);
			await Page9.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL3 }).ClickAsync(locatorClickOptions);
			await Page9.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL4 }).ClickAsync(locatorClickOptions);

			await Page.WaitForTimeoutAsync(3000);

			//complete request position 3
			Console.WriteLine("complete offer pos3");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_tbShortDescription").FillAsync("item title pos3");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_tbLongDescription").FillAsync("item description pos3");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_tbQuantity").FillAsync("3");

			Console.WriteLine("add classification pos3");

			var Page11 = await Page.RunAndWaitForPopupAsync(async () =>
			{
				await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_ibShowClassification").ClickAsync(locatorClickOptions);
			}, new PageRunAndWaitForPopupOptions { Timeout = 180000 });

			await Page.WaitForTimeoutAsync(3000);

			await Expect(Page11.Locator("#tbSearchField")).ToBeVisibleAsync(locatorVisibleAssertion);
			await Page11.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL1 }).ClickAsync(locatorClickOptions);
			await Page11.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL2 }).ClickAsync(locatorClickOptions);
			await Page11.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL3 }).ClickAsync(locatorClickOptions);
			await Page11.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL4 }).ClickAsync(locatorClickOptions);

			if(Environment == "QA")
			{
				//need to select a shipping address
				await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlLocationShippingAddress").SelectOptionAsync(new[] { "1368" });//westgate ripon
				//note Michelin local code is not mandatory
			}
			Console.WriteLine("save request");
			//save request
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSaveRequestTop").ClickAsync(locatorClickOptions);

			//wait for page to refresh
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblStatus1").WaitForAsync(locatorWaitForOptions);

			//check request is saved
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblStatus1")).ToContainTextAsync("Successfully saved.");
			//return to request list
			Console.WriteLine("return to request list page");
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Requests" }).ClickAsync(locatorClickOptions);

			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);

			//is the request we just created in the list, search for it
			Console.WriteLine("search for " + requestToRejectTransactionName);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").ClickAsync(locatorClickOptions);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestToRejectTransactionName);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);

			//await Page.PauseAsync();//causes the playwright test inspector to launch even when not debugging and cause pause of the run
			//add delay
			await Task.Delay(3000);


			//wait for results page on request list page
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount").WaitForAsync(locatorWaitForOptions);

			//assert 1 result 
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");
			//assert search result matches with the transaction number we saved
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblTransactionNumber")).ToContainTextAsync("PW_Auto_RequestTo");
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblTransactionNumber")).ToContainTextAsync(transactionID);
		//	}
		//	catch(Exception ex)
		//	{
		//		Console.WriteLine("exception TC02_QQB_CreateARequest");
		//		Console.WriteLine(ex.Message);
		//		if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
		//		//screenshot
		//		await Page.ScreenshotAsync(new()
		//		{
		//			FullPage = true,
		//			Path = downloadPath + "TC02_" + requestToRejectTransactionName + "QQB_CreateARequest_Exception.png"
		//		});
		//		//note: the nunit test runner , the whole suite of tests does not stop when one fails which
		//		////appears to happen in the playwright test runner, so perhaps softassert is more required in node.js playwright??
		//		throw ex;
		//	}
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	//[TestCaseSource(nameof(Login))]
	//async public Task QQB_DuplicateARequest(LoginModel login)
	[Test, Order(3)]
	[Category("QQTests")]
	async public Task TC03_QQB_Duplicate_A_Request()
	{
		Console.WriteLine("3: QQB_Duplicate_A_Request");
		//test based on the devops smoke test with planid 181276
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303

		//this test assumes that the test CreateARequest has been executed and that the variable requestTransactionName1 has been populated
		//this test takes a request named PW_Auto_RequestToReject_browser_yyyymMMddhhmm created in test 2,  duplicates it,
		//renames the transaction number to PW_Auto_requestToOffer_browser_yyyymMMddhhmm
		//adds suppliers and saves the request
		//expected output: a new duplicated request with status created with transaction number PW_Auto_requestToOffer_browser_yyyymMMddhhmm

		//TODO COMMENT OUT LINE BELOW BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////
		///
		//requestToRejectTransactionName = "PW_Auto_RequestToReject_chromium_202412201435"; //for testing

		//////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("QQB_DuplicateARequest request... " + requestToRejectTransactionName);

		try
		{

			string url = SEARCHURL;

			Console.WriteLine("Duplicating request...  " + requestToRejectTransactionName);
			PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 180000, State = WaitForSelectorState.Visible };
			LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
			LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
			LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };
			LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
			LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
			PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };
			PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
			Console.WriteLine("login to search:" + DateTime.Now.ToLongTimeString());

			bool loggedin = false;
			int attempts = 0;
			while (loggedin == false && attempts < 10)
			{
				try
				{
					await Page.GotoAsync(url, pageGotoOptions);
					await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
					loggedin = true;
				}
				catch (Exception ex)
				{
					attempts++;
					Console.WriteLine("exception: " + ex.Message);
					Console.WriteLine(DateTime.Now.ToString());
					Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
					//seeing a lot of errors of type
					/*
							Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
					*/
				}
			}
			if (loggedin == false && attempts >= 10)
			{
				DateTime timeRightNow = DateTime.Now;
				string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
				await Page.ScreenshotAsync(new()
				{
					FullPage = true,
					Path = downloadPath + "TC03_LoginError_" + FileCurrentDate + ".png"
				});
			}

			Console.WriteLine("page: " + Page.Url);

			//assert search has a quick quote link for this view/company
			await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

			Console.WriteLine("wait for request list page");
			//assert on request list page
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
			await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

			//get current request count
			var currentrequestCount = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount").TextContentAsync();

			//search for the request created in test CreateRequest
			Console.WriteLine("search for " + requestToRejectTransactionName);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestToRejectTransactionName);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").ClickAsync();
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);
			await Page.WaitForLoadStateAsync(LoadState.Load);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			//wait for results
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
			await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);
			//assert only 1 result
			//this may fail during testing
			try
			{
				await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");
			}
			catch
			{
				Console.WriteLine("more than one search result for request... " + requestToRejectTransactionName);
			}

			//get the transaction id for the single result
			var originalRequestId = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblRequestID").TextContentAsync(new LocatorTextContentOptions { Timeout = 180000});
			requestToRejectId = originalRequestId;
			//duplicate the request by clicking the duplicate icon on the requests list page for the specific request row

			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			Console.WriteLine("duplicate request via the icon on the request list page");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_ibDuplicateRequest").ClickAsync();
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			//wait for page to refresh
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
			await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync();
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Request list");

			//assert there are 2 results
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("2");

			//get new requestid
			var duplicatedRequestId = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblRequestID").TextContentAsync();
			requestToOfferId = duplicatedRequestId;
			//reset search
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Reset" }).ClickAsync(locatorClickOptions);

			//assert requestcount now has been incremented
			updatedrequestCount = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount").TextContentAsync();

			//search for the new (duplicated) request id
			Console.WriteLine("search for duplicated request ");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(duplicatedRequestId);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "ID" });
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);
			await Page.WaitForLoadStateAsync(LoadState.Load);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			//wait for page to load
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Request list");
			await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync();
			//assert only 1 result, searching, should be only 1, but possibly no results if there is an error
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");

			//open duplicated request
			Console.WriteLine("edit duplicated request ");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_ibEditRequest").ClickAsync(locatorClickOptions);

			//wait for request details page to open
			await Expect(Page.FrameLocator("#qqFrame").GetByText("Request Details")).ToBeVisibleAsync();
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Request Details");

			Console.WriteLine("check contents of duplicated request ");
			//assert the details of the duplicated request are as expected
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbTransactionNumber")).ToBeVisibleAsync();
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbTransactionNumber")).ToHaveValueAsync(requestToRejectTransactionName);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_lblDescriptionShortCut")).ToContainTextAsync("item title pos1");
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_lblLongDescriptionCut")).ToContainTextAsync("item description pos1");
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_lblAmount")).ToContainTextAsync("1");
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_lblClassificationValue")).ToContainTextAsync(TC02_DEFAULT_CLASSIFICATON_CODE1);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_lblDescriptionShortCut")).ToContainTextAsync("item title pos2");
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_lblLongDescriptionCut")).ToContainTextAsync("item description pos2");
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_lblAmount")).ToContainTextAsync("2");
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_lblClassificationValue")).ToContainTextAsync(TC02_DEFAULT_CLASSIFICATON_CODE1);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_lblDescriptionShortCut")).ToContainTextAsync("item title pos3");
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_lblLongDescriptionCut")).ToContainTextAsync("item description pos3");
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_lblAmount")).ToContainTextAsync("3");
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_lblClassificationValue")).ToContainTextAsync(TC02_DEFAULT_CLASSIFICATON_CODE1, new LocatorAssertionsToContainTextOptions { Timeout = 180000 });

			//assert that the selected datasheet is as expected
			var readonlyInput = Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbProductGroup");

			await Expect(readonlyInput).ToBeDisabledAsync();

			var selectedDataSheet = await readonlyInput.GetAttributeAsync("value");

			Assert.That(selectedDataSheet == "Populate Classification 1");

			//no selected suppliers
			//await Page.PauseAsync();


			await Task.Delay(3000);
			

			Console.WriteLine("assert selected suppliers list is empty for the newly duplicated request");//this fails without a pause in qa???
			await Expect(Page.FrameLocator("#qqFrame").Locator("#selectSelectedSuppliers")).ToBeEmptyAsync(new LocatorAssertionsToBeEmptyOptions { Timeout = 180000 });

			if (Environment == "QA")
			{
				//need to select a shipping address
				Console.WriteLine("select shipping address, Qa only ");
				await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlLocationShippingAddress").SelectOptionAsync(new[] { "1368" });//westgate ripon
																																																																				 //note Michelin local code is not mandatory
			}

			DateTime today = DateTime.Now;
			string CurrentDate = $"{today.Year}{today.Month}{today.Day}{today.Hour}{today.Minute}";
			string transactionID = "PW_Auto_RequestToOffer_" + _browserName + "_" + CurrentDate;
			requestToOfferTransactionName = transactionID;
			//change the transaction number/id of the duplicated request to be PW_Auto_RequestToOffer_
			Console.WriteLine("update transaction number to  " + transactionID);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbTransactionNumber").FillAsync(transactionID);
			//note full transaction number must be < 50 characters in length
			//add the suppliers
			await Page.FrameLocator("#qqFrame").Locator("#selectExistingSuppliers").SelectOptionAsync(new[] { TC02_SELECT_SUPPLIER1_ID });
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync(locatorClickOptions);
			await Page.FrameLocator("#qqFrame").Locator("#selectExistingSuppliers").SelectOptionAsync(new[] { TC02_SELECT_SUPPLIER2_ID });
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync(locatorClickOptions);
			//save request
			Console.WriteLine("save " + transactionID);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSaveRequestTop").ClickAsync(locatorClickOptions);
			//assert it is saved
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblStatus1")).ToContainTextAsync("Successfully saved.");
		}
		catch(Exception ex)
		{
			Console.WriteLine("exception TC03_QQB_DuplicateRequest");
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
			//screenshot
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC03_" + requestToRejectTransactionName + "QQB_DuplicateARequest_Exception.png"
			});
			//note: the nunit test runner , the whole suite of tests does not stop when one fails which
			////appears to happen in the playwright test runner, so perhaps softassert is more required in node.js playwright??
			throw ex;
		}
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(4)]
	[Category("QQTests")]
	async public Task TC04_QQB_Send_A_Request_To_Suppliers()
	{
		/*
		Test based on the devops smoke test with planid 181277
		https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
		buyer sends a request to suppliers...
		preconditions Following QQ properties are disabled: WHY WHY WHY?????
		BUYER_CHOOSES_CLASSIFICATION - boolean ctl00_ctl00_MainContent_CenterContent_ddlBooleanValue but active required for previous test?
		ENABLE_DATASHEET_CLASSIFICATION - boolean ctl00_ctl00_MainContent_CenterContent_ddlBooleanValue but active required for previous test?
		DISABLE_REQUEST_POS_DEL  - boolean ctl00_ctl00_MainContent_CenterContent_ddlBooleanValue
		BUYER_ERP_ITEM_NUMBER_MANDATORY -  this property not even assigned for the buyer wtf?
		ENABLE_BUYER_ERP_ITEM_NUMBER - this property not even assigned for the buyer wtf?

		test objectives:
		A)publish the request created in step 2 (PW_Auto_RequestToReject_browser_yyyyMMddhhmm) via the publish icon in the request list grid
		check that its status is updated to requested

		B)publish the request created in step 3 (PW_Auto_RequestToOffer_browser_yyyyMMddhhmm) via the send button in the request details page

		expected output:2 requests have status updated to requested and are sent to the selected suppliers
		 */

		//****         TODO COMMENT OUT LINES BELOW BEFORE PROD TESTING           ****
		//////////////////////////////////////////////////////////////////////////////

		//requestToRejectTransactionName = "PW_Auto_RequestToReject_chromium_20241225959";
		//requestToOfferTransactionName = "PW_Auto_RequestToOffer_chromium_20241225102";

		//////////////////////////////////////////////////////////////////////////////

		try
		{
			Console.WriteLine("4: QQB_Send_A_Request_To_Suppliers");
			Console.WriteLine("requestToRejectId :" + requestToRejectId);
			Console.WriteLine("requestToOfferId :" + requestToOfferId);
			string url = SEARCHURL;

			Console.WriteLine("Sending requests  " + requestToRejectTransactionName + " And " + requestToOfferTransactionName);
			PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 180000, State = WaitForSelectorState.Visible };
			LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
			LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
			LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
			PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };
			PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
			LocatorWaitForOptions locatorWaitForOption = new LocatorWaitForOptions { Timeout = 180000 };
			LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
			Console.WriteLine("login to search:" + DateTime.Now.ToLongTimeString());
			bool loggedin = false;
			int attempts = 0;
			while (loggedin == false && attempts < 10)
			{
				try
				{
					await Page.GotoAsync(url, pageGotoOptions);
					await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
					loggedin = true;
				}
				catch (Exception ex)
				{
					attempts++;
					Console.WriteLine("exception: " + ex.Message);
					Console.WriteLine(DateTime.Now.ToString());
					Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
					//seeing a lot of errors of type
					/*
							Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
					*/
				}
			}
			if (loggedin == false && attempts >= 10)
			{
				DateTime timeRightNow = DateTime.Now;
				string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
				await Page.ScreenshotAsync(new()
				{
					FullPage = true,
					Path = downloadPath + "TC04_LoginError_" + FileCurrentDate + ".png"
				});
			}

			Console.WriteLine("page: " + Page.Url);

			//assert search has a quick quote link for this view/company
			await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

			var iframeElement = await Page.Locator("#qqFrame").ElementHandleAsync();
			var frame = await iframeElement.ContentFrameAsync();
			if (frame != null && frame.Url != "")
			{
				Console.WriteLine("qqFrame Url : " + frame.Url);
				//await frame.WaitForURLAsync("https://portal.hubwoo.com/srvs/easyorder/RequestList2007.aspx");
			}

			//assert on request list page
			Console.WriteLine("wait for request list page");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOption);

			await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Request list");
			////////////////////////////
			//publish "request to REJECT" on the requestlist page
			////////////////////////////
			//search for request to reject
			Console.WriteLine("Search For " + requestToRejectTransactionName);
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Reset" }).ClickAsync(locatorClickOptions);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestToRejectTransactionName);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });

			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);
			await Page.WaitForLoadStateAsync(LoadState.Load);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			if (Environment == "QA")
			{
				await Task.Delay(3000);
			}

			//assert on requestlist page
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOption);

			//wait for search results and check status of the first result
			iframeElement = await Page.Locator("#qqFrame").ElementHandleAsync();
			frame = await iframeElement.ContentFrameAsync();
			if (frame != null && frame.Url != "")
			{
				Console.WriteLine("qqFrame Url : " + frame.Url);
				//await frame.WaitForURLAsync("https://portal.hubwoo.com/srvs/easyorder/RequestList2007.aspx");
			}

			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToBeVisibleAsync(locatorVisibleAssertion);

			//assert there is only 1 result
			try
			{
				//soft assert, capture and report any issue
				await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");
			}
			catch
			{
				Console.WriteLine("More than one search result for request..." + requestToRejectTransactionName);
			}

			await Task.Delay(3000);

			//assert that the status of the request is created 
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblStatus")).ToContainTextAsync("Created");

			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			//publish by clicking the publish icon in the first row of the search results ctl00_MainContent_requestGridView_ctl02_ibPublishRequest
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_ibPublishRequest").ClickAsync(locatorClickOptions);
			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


			Console.WriteLine("About to publish " + requestToRejectTransactionName);
			//click publish again in the cover panel
			//////////////////////////////////////////////////////////////////////
			//this publish click is very flaky!!!!!!!

			await Task.Delay(3000);

			//click publish confirmation
			//ctl00_MainContent_requestGridView_ctl02_lbPublishRequest
			//make sure clicking linkbutton not the label ctl00_MainContent_requestGridView_ctl02_lblPublishRequest!!!
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lbPublishRequest").ClickAsync(locatorClickOptions);


			await Task.Delay(3000);
			

			//////////////////////////////////////////////////////////////////////
			Console.WriteLine("published " + requestToRejectTransactionName);

			iframeElement = await Page.Locator("#qqFrame").ElementHandleAsync();
			frame = await iframeElement.ContentFrameAsync();
			if (frame != null && frame.Url != "")
			{
				Console.WriteLine("qqFrame Url : " + frame.Url);
				//await frame.WaitForURLAsync("https://portal.hubwoo.com/srvs/easyorder/RequestList2007.aspx");
			}

			//assert on requestlist page
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOption);

			//request list is refreshed, but is the search result still relevant...?
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToBeVisibleAsync(locatorVisibleAssertion);

			//search for request to reject
			Console.WriteLine("search for " + requestToRejectTransactionName);
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Reset" }).ClickAsync(locatorClickOptions);

			await Task.Delay(3000);
			
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset")).ToBeVisibleAsync(locatorVisibleAssertion);

			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestToRejectTransactionName);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);


			await Task.Delay(3000);
			

			//assert on requestlist page
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOption);

			//assert 1 result
			try
			{
				//soft assert, capture and report any issue
				await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");
			}
			catch
			{
				Console.WriteLine("More than one search result for request..." + requestToRejectTransactionName);
			}

			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset")).ToBeVisibleAsync(locatorVisibleAssertion);

			//assert status of the request we just published is updated to the status of requested
			Console.WriteLine("1: assert status is now requested for " + requestToRejectTransactionName);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblStatus")).ToContainTextAsync("Requested");

			/////////////////////////////////////part 2 /////////////////////////////////////////////////////////////////////////////
			/////publish requesttooffer on the request create via he send button
			//reset search
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Reset" }).ClickAsync(locatorClickOptions);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset")).ToBeVisibleAsync(locatorVisibleAssertion);

			Console.WriteLine("1:Search For " + requestToOfferTransactionName);
			//search for request to offer, i.e. the request that was duplicated earlier in test 3
			//set transaction number  column search

			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestToOfferTransactionName);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Search" }).ClickAsync();
			Console.WriteLine("Just Searched For " + requestToOfferTransactionName);

			//assert on requestlist page
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOption);

			iframeElement = await Page.Locator("#qqFrame").ElementHandleAsync();
			frame = await iframeElement.ContentFrameAsync();
			if (frame != null && frame.Url != "")
			{
				Console.WriteLine("qqFrame Url : " + frame.Url);
				//await frame.WaitForURLAsync("https://portal.hubwoo.com/srvs/easyorder/RequestList2007.aspx");
			}

			//assert 1 result
			try
			{
				//soft assert, capture and report any issue
				await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");
			}
			catch
			{
				Console.WriteLine("More than one search result for request..." + requestToOfferTransactionName);
			}

			//assert we have found the correct result
			//is the request id as expected
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblRequestID")).ToContainTextAsync(requestToOfferId);
			//is the transaction id as expected
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblTransactionNumber")).ToContainTextAsync(requestToOfferTransactionName);

			/////////////////////////////////////////////////////////////////////////////
			//open/edit request to offer and send to supplier via requestdetails page
			/////////////////////////////////////////////////////////////////////////////
			//click the edit icon for the first result

			Console.WriteLine("about to edit " + requestToOfferTransactionName);

			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_ibEditRequest").ClickAsync(locatorClickOptions);

			//assert on request create page
			await Page.WaitForLoadStateAsync(LoadState.Load);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink").WaitForAsync(locatorWaitForOption);

			//assert we are editing the correct request
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbTransactionNumber")).ToHaveValueAsync(requestToOfferTransactionName);

			iframeElement = await Page.Locator("#qqFrame").ElementHandleAsync();
			frame = await iframeElement.ContentFrameAsync();
			if (frame != null && frame.Url != "")
			{
				Console.WriteLine("qqFrame Url : " + frame.Url);
				//await frame.WaitForURLAsync("https://portal.hubwoo.com/srvs/easyorder/RequestCreate.aspx");
			}

			//await for create request page to load ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToBeVisibleAsync(locatorVisibleAssertion);
			//assert
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Request Details");

			//send
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_hrefPublishTop").ClickAsync(locatorClickOptions);

			//assert cover message
			await Expect(Page.FrameLocator("#qqFrame").Locator("#divPublishTextInvitedTop")).ToContainTextAsync(TC02_ASSERT_SUPPLIER1);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#divPublishTextInvitedTop")).ToContainTextAsync(TC02_ASSERT_SUPPLIER2);

			//send button is displayed
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbPublishButtonTop")).ToBeVisibleAsync(locatorVisibleAssertion);

			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbPublishButtonTop").ClickAsync(locatorClickOptions);
			iframeElement = await Page.Locator("#qqFrame").ElementHandleAsync();
			frame = await iframeElement.ContentFrameAsync();
			if (frame != null && frame.Url != "")
			{
				Console.WriteLine("qqFrame Url : " + frame.Url);
				//await frame.WaitForURLAsync("https://portal.hubwoo.com/srvs/easyorder/RequestList2007.aspx");
			}
			//assert back on request list
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOption);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Request list");

			//search for request just sent
			Console.WriteLine("2: Search For " + requestToOfferTransactionName);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestToOfferTransactionName);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync();

			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOption);

			//assert only 1 result
			try
			{
				await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");
			}
			catch
			{
				Console.WriteLine("More than one search result for request..." + requestToOfferTransactionName);
			}
			//assert status is now requested
			Console.WriteLine("2: assert status is now requested for " + requestToOfferTransactionName);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblStatus")).ToContainTextAsync("Requested");
		}
		catch(Exception ex)
		{
			Console.WriteLine("Exception:" + requestToRejectTransactionName + "  " + ex.Message);
			if(ex.InnerException!= null) Console.WriteLine(ex.InnerException.Message);
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC04_" + requestToOfferTransactionName + "_Exception_QQB_SendARequest.png"
			});
			//throw ex;//you do lose the original line that causes the exception
		}
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	
	[Test, Order(5)]
	//[Ignore("not implemented yet")]
	[Category("QQTests")]
	async public Task TC05_QQB_Buyer_Download_Excel_And_Pdf_Request()
	{
		//test based on devops test id 181278  "Buyer download excel and pdf request"
		//for testing of individual tests rather than relying on having to run all tests in the suite
		//TODO COMMENT OUT LINE BELOW BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestToRejectTransactionName = "PW_Auto_RequestToReject_chromium_202412181423";

		//////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("5: QQB_Buyer_Download_Excel_And_Pdf_Request");
		string url = SEARCHURL;
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };

		Console.WriteLine("login to search:" + DateTime.Now.ToLongTimeString());
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loggedin = true;
			}
			catch (Exception ex)
			{
				attempts++;
				Console.WriteLine("exception: " + ex.Message);
				Console.WriteLine(DateTime.Now.ToString());
				Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
				//seeing a lot of errors of type
				/*
						Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
				*/
		}
		}
		if (loggedin == false && attempts >= 10)
		{
			DateTime timeRightNow = DateTime.Now;
			string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC05_LoginError_" + FileCurrentDate + ".png"
			});
		}

		Console.WriteLine("page: " + Page.Url);

		//assert search has a quick quote link for this view/company
		await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

		//assert on request list page

		Console.WriteLine("wait for request list page");

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync();

		//filter  for requests with name requestToRejectTransactionName
		//search for request to reject
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Reset" }).ClickAsync();
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestToRejectTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//assert on requestlist page
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);

		await Task.Delay(5000);
		//assert more than 1 result
		try
		{
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");
		}
		catch(Exception ex)
		{
			Console.WriteLine("incorrect number of search result for request " + requestToRejectTransactionName);
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
		}

		//wait for search results and check status of the first result
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToBeVisibleAsync(locatorVisibleAssertion);
		await Task.Delay(3000);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_ibDownloadArea").ClickAsync(locatorClickOptions);

		//await Page.PauseAsync();//causes the playwright test inspector to launch even when not debugging and cause pause of the run
		await Task.Delay(3000);
		var waitForDownloadTask = Page.WaitForDownloadAsync();
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Download the request as PDF File" }).First.ClickAsync(new()
		{
			//modifier allows the save as functionality, which makes the generator save to disk rather tha be rendered in a new tab
			Modifiers = new[] { KeyboardModifier.Alt },
			Timeout = 180000
		});

		var download = await waitForDownloadTask;

		var fileName = downloadPath + "TC05_" + requestToRejectTransactionName + download.SuggestedFilename;

		if(Environment == "QA")
		{
			await Task.Delay(3000);
		}

		// Wait for the download process to complete and save the downloaded file somewhere
		await download.SaveAsAsync(fileName);

		if (Environment == "QA")
		{
			await Task.Delay(3000);
		}

		if (Environment == "PROD")
		{
			await Task.Delay(6000);
		}

		//load pdf in another tab and screenshot it
		var page2 = await Page.RunAndWaitForPopupAsync(async () =>
		{
			//fails if more than one request file result in the request list on the page
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Download the request as PDF File" }).First.ClickAsync(locatorClickOptions);
		},new PageRunAndWaitForPopupOptions { Timeout = 180000});
		//await Page.PauseAsync();//causes the playwright test inspector to launch pauses run

		//download url
		var pdfUrl = page2.Url;
		try
		{
			Console.WriteLine("pdf downloaded from " + pdfUrl);
			Console.WriteLine("asserting contents of  " + fileName);
			using (PdfDocument pdf = PdfDocument.Open(fileName))
			{
				Page page = pdf.GetPage(1);
				if (page != null)
				{
					Console.WriteLine("Asserting contents of pdf");
					Assert.That(page.Text.Contains(TC05_PDF_ASSERT_CUSTOMERNAME));
					Assert.That(page.Text.Contains(TC05_PDF_ASSERT_LOCATION));
					Console.WriteLine("assert page.Text.Contains " + requestToRejectTransactionName);
					Assert.That(page.Text.Contains(requestToRejectTransactionName));
					Assert.That(page.Text.Contains("item title pos1"));
					Assert.That(page.Text.Contains("item description pos1"));
					Assert.That(page.Text.Contains("Populate Classification 1"));
				}
			}
		}
		catch(Exception ex)
		{
			Console.WriteLine("Issue asserting contents of pdf document " + pdfUrl);
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
		}

		//screenshots are blank when running this pause fixes it
		await Task.Delay(3000);

		await page2.ScreenshotAsync(new()
		{
			FullPage = true,
			Path = downloadPath + "TC05_" + requestToRejectTransactionName + "_QQB_BuyerDownloadExcelAndPdfRequest_pdf.png"
		});

		//await Page.PauseAsync();//causes the playwright test inspector to launch even when not debugging and cause pause of the run

		//download of an excel file is weird in chromium as it is running in incognito mode, you are shown a popup with a guid file name but no file is available when you open folder?
		try
		{
			//download excel
			///////////////////////////////////////////////////////////////////////////////////////////////////////////
			var waitForExcelDownloadTask = Page.WaitForDownloadAsync();
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Download the request as Excel File" }).First.ClickAsync(new()
			{
				//modifier allows the save as functionality, which makes the generator save to disk rather than be rendered in a new tab
				Modifiers = new[] { KeyboardModifier.Alt },
				Timeout = 180000
			});

			var excelDownload = await waitForExcelDownloadTask;

			// Wait for the download process to complete and save the downloaded file somewhere
			await excelDownload.SaveAsAsync(downloadPath + "TC05_" + requestToRejectTransactionName + excelDownload.SuggestedFilename);
		}
		catch (Exception ex)
		{
			Console.WriteLine("exception QQB_BuyerDownloadExcelAndPdfRequest");
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
		}

		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

[Test, Order(6)]
[Category("QQTests")]
async public Task TC06_QQS_Supplier_Checking_Request()
{
	//based on devops test case id 181280 "Supplier checking request" 
	//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
	//based on the filename of the requestToRejectTransactionName workout what the external comment should be
	//int _pos = requestToRejectTransactionName.LastIndexOf("_");
	//string commentDate = requestToRejectTransactionName.Substring(_pos + 1, (requestToRejectTransactionName.Length) - (_pos + 1));
	//rather than use the exact hour/minute setting on the request file just use todays date to set internal external comments so that by convention
	//we can check for more or less unique strings specific to a test run
	DateTime today = DateTime.Now;
	String commentDate = $"{today.Year}{today.Month}{today.Day}";

	Console.WriteLine("6: QQS_Supplier_Checking_Request");
	string url = PORTAL_LOGIN;
	PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };

	PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
	LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
	LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
	LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
	bool loggedin = false;
	int attempts = 0;
	while (loggedin == false && attempts < 10)
	{
		try
		{
			await Page.GotoAsync(url, pageGotoOptions);
			await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
			await Page.WaitForLoadStateAsync(LoadState.Load);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Page.GetByPlaceholder("Enter your user name").FillAsync(SUPPLIER_USER1_LOGIN);
			await Page.GetByPlaceholder("Enter your password").FillAsync(SUPPLIER_USER1_PASSWORD);
			await Page.Locator("#signInButtonId").IsEnabledAsync();
			await Page.Locator("#signInButtonId").ClickAsync(locatorClickOptions);
			loggedin = true;
		}
		catch (Exception e)
		{
			attempts++;
			Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
			//seeing a lot of errors of type
			/*
				Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F
				Call log:- navigating to "https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F", waiting until "load"
				*/
			Console.WriteLine(e.Message);
		}
	}
	Console.WriteLine("Page: " + Page.Url);

	await Page.WaitForLoadStateAsync(LoadState.Load);
	await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	await Page.WaitForSelectorAsync("//side-bar-item-group[@name='Opportunities']", new PageWaitForSelectorOptions { Timeout = 180000 });

	Console.WriteLine("Page: " + Page.Url);

	await Page.Locator("//side-bar-item-group[@name='Opportunities']").ClickAsync(locatorClickOptions);
	await Page.Locator("//side-bar-item[@name='Requests']").ClickAsync(locatorClickOptions);

	//wait for url
	await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions); //easyorder/SupplierRequestList2007.aspx

	//search by transaction name
	//reset
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync(locatorClickOptions);
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestToOfferTransactionName);
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync(locatorClickOptions);
	await Page.WaitForLoadStateAsync(LoadState.Load);
	await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

	await Task.Delay(6000);

	//wait for url
	await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);
	//assert 1 result
	await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager")).ToContainTextAsync("(1 items found)");

	//assert request name 
	await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_lblTransactionNumber")).ToContainTextAsync(requestToOfferTransactionName);
	//assert request status
	await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_lblStatus")).ToContainTextAsync("Requested");

	//click info icon  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibAdditionalInformation
	//open using selector ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibAdditionalInformation
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibAdditionalInformation").ClickAsync(locatorClickOptions);

	await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_lblLocationValue")).ToContainTextAsync(TC06_LOCATION_ASSERT);

	//toggle off the info panel 
	//close using ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibAdditionalInformation2
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibAdditionalInformation2").ClickAsync(locatorClickOptions);

	//click message to supplier (book) icon, toggle on
	//$("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibBuyerMessage").click()  //opens
	//$("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibBuyerMessage2").click() //closes
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibBuyerMessage").ClickAsync(locatorClickOptions);

	//assert external comment
	var comment = await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_tbBuyerMessage").TextContentAsync();
	await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_tbBuyerMessage")).ToContainTextAsync("external comment " + commentDate);
	Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

[Test, Order(7)]
[Category("QQTests")]
async public Task TC07_QQS_Supplier_Download_Request_Pdf()
{
	//supplier opens the request created by the buyer , created in earlier step in the test suite
	//for testing of individual tests rather than relying on having to run all tests in the suite

	//TODO COMMENT OUT LINE BELOW BEFORE PROD TESTING
	////////////////////////////////////////////////////////////////////////////////

	//requestToRejectTransactionName = "PW_Auto_RequestToOffer_chromium_202412181646";

	//////////////////////////////////////////////////////////////////////////////

	//test based on devops test case id 181289, planId 125397, suiteId 179303
	//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
	Console.WriteLine("7: QQS_Supplier_Download_Request_Pdf");
	string url = PORTAL_LOGIN;
	PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };

	PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
	LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
	LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
	LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
	bool loggedin = false;
	int attempts = 0;
	while (loggedin == false && attempts < 10)
	{
		try
		{
			await Page.GotoAsync(url, pageGotoOptions);
			await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
			await Page.WaitForLoadStateAsync(LoadState.Load);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Page.GetByPlaceholder("Enter your user name").FillAsync(SUPPLIER_USER1_LOGIN);
			await Page.GetByPlaceholder("Enter your password").FillAsync(SUPPLIER_USER1_PASSWORD);
			await Page.Locator("#signInButtonId").IsEnabledAsync();
			await Page.Locator("#signInButtonId").ClickAsync(locatorClickOptions);
			loggedin = true;
		}
		catch (Exception e)
		{
			attempts++;
			Console.WriteLine("Problems logging in, attempt:" + attempts.ToString());
			//seeing a lot of errors of type
			/*
				Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F
				Call log:- navigating to "https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F", waiting until "load"
				*/
			Console.WriteLine(e.Message);
		}
	}
	Console.WriteLine("Page: " + Page.Url);

	await Page.WaitForSelectorAsync("//side-bar-item-group[@name='Opportunities']", new PageWaitForSelectorOptions { Timeout = 180000 });
	Console.WriteLine("Page: " + Page.Url);
	var isCookieConsentVisible = await Page.Locator("#cookie-consent-dialog").IsVisibleAsync();
	if (isCookieConsentVisible)
	{
		await Page.GetByRole(AriaRole.Button, new() { Name = "Accept all" }).ClickAsync();
	}

	await Page.Locator("//side-bar-item-group[@name='Opportunities']").ClickAsync(locatorClickOptions);
	await Page.Locator("//side-bar-item[@name='Requests']").ClickAsync(locatorClickOptions);
		
	//wait for page to load
	await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

	//search by transaction name
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync(locatorClickOptions);
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestToRejectTransactionName);
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync(locatorClickOptions);
	await Page.WaitForLoadStateAsync(LoadState.Load);
	await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	//wait for results  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData
	//await Page.WaitForSelectorAsync("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData");
	await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

	//Assert 1 result
	await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager > span")).ToContainTextAsync("(1 items found)");
	//#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager > span
	//click the download pdf option
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibDownloadArea").ClickAsync(locatorClickOptions);

	//download pdf and assert contents
	var waitForDownloadTask = Page.WaitForDownloadAsync();
		//find the download link via the text
		await Task.Delay(3000);
		await Page.GetByRole(AriaRole.Link, new() { Name = "Download the request as PDF File" }).First.ClickAsync(new()
	{
		//modifier allows the save as functionality, which makes the generator save to disk rather tha be rendered in a new tab
		Modifiers = new[] { KeyboardModifier.Alt },
		Timeout = 180000
	});

	var download = await waitForDownloadTask;

	var fileName = downloadPath + "TC07_" + requestToRejectTransactionName + download.SuggestedFilename;

	// Wait for the download process to complete and save the downloaded file somewhere
	await download.SaveAsAsync(fileName);

	//click the pdf option
	var page1 = await Page.RunAndWaitForPopupAsync(async () =>
	{
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_hlRequestPdf").ClickAsync();
	}, new PageRunAndWaitForPopupOptions { Timeout = 180000 });
	//take screenshot
	//screenshot  pdf tab requestToRejectTransactionName.pdf
	await page1.ScreenshotAsync(new()
	{
		FullPage = true,
		Path = downloadPath + "TC07_" + requestToRejectTransactionName + "_QQS_SupplierDownloadRequestPdf_pdf.png"
	});

	//assert the contents of the pdf
	var pdfUrl = page1.Url;
	try
	{
		Console.WriteLine("pdf downloaded from " + pdfUrl);
		Console.WriteLine("asserting contents of  " + fileName);
		using (PdfDocument pdf = PdfDocument.Open(fileName))
		{
			Page page = pdf.GetPage(1);
			if (page != null)
			{
				Console.WriteLine("Asserting contents of pdf");
				Assert.That(page.Text.Contains(TC05_PDF_ASSERT_CUSTOMERNAME));
				Assert.That(page.Text.Contains(TC05_PDF_ASSERT_LOCATION));
				Assert.That(page.Text.Contains(requestToRejectTransactionName));
				Assert.That(page.Text.Contains("item title pos1"));
				Assert.That(page.Text.Contains("item description pos1"));
				Assert.That(page.Text.Contains("Populate Classification 1"));
			}
		}
	}
	catch (Exception ex)
	{
		Console.WriteLine("Issue asserting contents of pdf document " + pdfUrl);
		Console.WriteLine(ex.Message);
		if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
	}
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
}

[Test, Order(8)]
[Category("QQTests")]
async public Task TC08_QQS_Supplier_Reject_Request()
{
	//TODO COMMENT OUT LINE BELOW BEFORE PROD TESTING
	/////////////////////////////////////////////////////////////////////////////////

	//requestToRejectTransactionName = "PW_Auto_RequestToReject_chromium_202412201435";

	//////////////////////////////////////////////////////////////////////////////

	//supplier rejects a request (PW_Auto_RequestToReject_browser_yyyyMMddhhmm) created earlier in the test suite
	//test based on devops test case id 181294 planid = 125397 suiteId=179303 "Supplier reject request"
	//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
	Console.WriteLine("8: TC08_QQS_Supplier_Reject_Request");
	string url = PORTAL_LOGIN;
	PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };
	LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };
	PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
	LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
	LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
	LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
	bool loggedin = false;
	int attempts = 0;
	while (loggedin == false && attempts < 10)
	{
		try
		{
			await Page.GotoAsync(url, pageGotoOptions);
			await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
			await Page.WaitForLoadStateAsync(LoadState.Load);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Page.GetByPlaceholder("Enter your user name").FillAsync(SUPPLIER_USER1_LOGIN);
			await Page.GetByPlaceholder("Enter your password").FillAsync(SUPPLIER_USER1_PASSWORD);
			await Page.Locator("#signInButtonId").IsEnabledAsync();
			await Page.Locator("#signInButtonId").ClickAsync(locatorClickOptions);
			loggedin = true;
		}
		catch (Exception e)
		{
			attempts++;
			Console.WriteLine("Problems logging in, attempt:" + attempts.ToString());
			//seeing a lot of errors of type
			/*
				Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F
				Call log:- navigating to "https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F", waiting until "load"
				*/
			Console.WriteLine(e.Message);
		}
	}
	Console.WriteLine("Page: " + Page.Url);

	//wait for page to load https://portal.hubwoo.com/main/
	//await Page.WaitForURLAsync("https://portal.hubwoo.com/main/", pageWaitForUrlOptions);

	await Page.WaitForSelectorAsync("//side-bar-item-group[@name='Opportunities']", new PageWaitForSelectorOptions { Timeout = 180000 });
	Console.WriteLine("Page: " + Page.Url);
	var isCookieConsentVisible = await Page.Locator("#cookie-consent-dialog").IsVisibleAsync();
	if (isCookieConsentVisible)
	{
		await Page.GetByRole(AriaRole.Button, new() { Name = "Accept all" }).ClickAsync();
	}

	/*
	await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "The Business Network" })).ToBeVisibleAsync(locatorVisibleAssertion);
	Console.WriteLine("click opportunities tab");
	await Page.GetByRole(AriaRole.Link, new() { Name = "Opportunities" }).ClickAsync(locatorClickOptions);//todo replace menu dependency
	*/
	await Page.Locator("//side-bar-item-group[@name='Opportunities']").ClickAsync(locatorClickOptions);
	await Page.Locator("//side-bar-item[@name='Requests']").ClickAsync(locatorClickOptions);

	//wait for page to load
	await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);
	Console.WriteLine("search for " + requestToRejectTransactionName);
	//search by transaction name
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync();
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestToRejectTransactionName);
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync(locatorClickOptions);
	await Page.WaitForLoadStateAsync(LoadState.Load);
	await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//wait for results  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData
		//await Page.WaitForSelectorAsync("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData");
		await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

	//Assert 1 result
	await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager > span")).ToContainTextAsync("(1 items found)");

	//edit the request
	Console.WriteLine("edit " + requestToRejectTransactionName);
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibEdit").ClickAsync(locatorClickOptions);

	//WaitAsync();
	//wait for url https://portal.hubwoo.com/srvs/easyorder/OfferDetail2007.aspx?requestIdString=102411534

	await Page.Locator("#ctl00_MainContent_offerActionBarTop_lblActions").WaitForAsync(locatorWaitForOptions);

	//await Page.WaitForSelectorAsync("#ctl00_MainContent_offerActionBarTop_lblActions");
	//await Page.WaitForSelectorAsync("#ctl00_MainContent_offerActionBarTop_lblReject");

	await Task.Delay(3000);

	//assert we are editing the correct request
	await Expect(Page.Locator("#ctl00_MainContent_lblRequestTitle")).ToContainTextAsync(requestToRejectTransactionName);

	//reject 
	Console.WriteLine("reject request " + requestToRejectTransactionName);
	await Page.Locator("#ctl00_MainContent_offerActionBarTop_hrefReject").ClickAsync(locatorClickOptions);

	//enter reject comment
	await Page.Locator("#ctl00_MainContent_offerActionBarTop_tbRejectReason").FillAsync("rejected");

	//reject
	await Page.Locator("#ctl00_MainContent_offerActionBarTop_lbRejectOffer").ClickAsync(locatorClickOptions);

	//redirected back to list
	//wait for page to load
	Console.WriteLine("wait for supplierrequestlist page to load ");
	await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

	//search and confirm the request has been rejected
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync();

	Console.WriteLine("Search for " + requestToRejectTransactionName);
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestToRejectTransactionName);
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
	await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync(locatorClickOptions);
	await Page.WaitForLoadStateAsync(LoadState.Load);
	await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

	//wait for results  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData
	await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);
	await Page.WaitForSelectorAsync("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData");

	//Assert 1 result
	Console.WriteLine("assert 1 result");
	await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager")).ToContainTextAsync("(1 items found)");

	//assert status is now rejected
	Console.WriteLine("assert status is rejected");
	await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_lblStatus")).ToContainTextAsync("Rejected");

}

	[Test, Order(9)]
	//[Ignore("not implemented yet")]
	[Category("QQTests")]
	async public Task TC09_QQS_Supplier_Create_Offers()
	{
		/*
		  supplier EPAM_TS4 makes an offer on the request PW_Auto_RequestToOffer_yyymmdd
		*/

		
			//for testing of individual tests rather than relying on having to run all tests in the suite

			//TODO COMMENT OUT LINE BELOW BEFORE PROD TESTING
			//////////////////////////////////////////////////////////////////////////////

			//requestToOfferTransactionName = "PW_Auto_RequestToOffer_chromium_20255311243";

			//////////////////////////////////////////////////////////////////////////////
			
			//test based on devops test case id 181296 , plan id 125397, test suite id 179303 "Create offers"
			//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
			Console.WriteLine("9: TC09_QQS_Supplier_Create_Offers");
			string url = PORTAL_LOGIN;
			PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };

			PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
			LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
			LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
			LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
			bool loggedin = false;
			int attempts = 0;
			while (loggedin == false && attempts < 10)
			{
				try
				{
					await Page.GotoAsync(url, pageGotoOptions);
					await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				  await Page.WaitForLoadStateAsync(LoadState.Load);
					await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
					await Page.GetByPlaceholder("Enter your user name").FillAsync(SUPPLIER_USER1_LOGIN);
					await Page.GetByPlaceholder("Enter your password").FillAsync(SUPPLIER_USER1_PASSWORD);
					await Page.Locator("#signInButtonId").IsEnabledAsync();
					await Page.Locator("#signInButtonId").ClickAsync(locatorClickOptions);
					loggedin = true;
				}
				catch (Exception e)
				{
					attempts++;
					Console.WriteLine("Problems logging in, attempt:" + attempts.ToString());
					//seeing a lot of errors of type
					/*
						Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F
						Call log:- navigating to "https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F", waiting until "load"
						*/
					Console.WriteLine(e.Message);
				}
			}
			Console.WriteLine("Page: " + Page.Url);

		//wait for page to load https://portal.hubwoo.com/main/
		//await Page.WaitForURLAsync("https://portal.hubwoo.com/main/", pageWaitForUrlOptions);  //this fails for some reason
		try
		{
			await Page.WaitForSelectorAsync("//side-bar-item-group[@name='Opportunities']", new PageWaitForSelectorOptions { Timeout = 180000 });
			Console.WriteLine("Page: " + Page.Url);
			var isCookieConsentVisible = await Page.Locator("#cookie-consent-dialog").IsVisibleAsync();
			if (isCookieConsentVisible)
			{
				await Page.GetByRole(AriaRole.Button, new() { Name = "Accept all" }).ClickAsync();
			}

			await Page.Locator("//side-bar-item-group[@name='Opportunities']").ClickAsync(locatorClickOptions);
			await Page.Locator("//side-bar-item[@name='Requests']").ClickAsync(locatorClickOptions);

			//wait for page to load
			Console.WriteLine("wait for easyorder/SupplierRequestList2007.aspx");
			await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);  ///easyorder/SupplierRequestList2007.aspx

			Console.WriteLine("search for " + requestToOfferTransactionName);
			//search by transaction name
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync();
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestToOfferTransactionName);
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync();
			await Page.WaitForLoadStateAsync(LoadState.Load);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			await Task.Delay(3000);

			//wait for results  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData
			await Page.WaitForSelectorAsync("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData");

			//Assert 1 result
			await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager")).ToContainTextAsync("(1 items found)");

			//waitfor url https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx
			Console.WriteLine("wait for easyorder/SupplierRequestList2007.aspx");
			await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);
			//click edit 

			Console.WriteLine("edit " + requestToOfferTransactionName);
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibEdit").ClickAsync();

			DateTime today = DateTime.Now;
			string CurrentDate = $"{today.Year}{today.Month}{today.Day}";

			//assert status is requested
			await Expect(Page.Locator("#ctl00_MainContent_lblStatus")).ToContainTextAsync("Requested");

			//add offer reference
			Console.WriteLine("add offerreference");
			await Page.Locator("#ctl00_MainContent_tbReference").FillAsync("Offer_" + CurrentDate, new LocatorFillOptions { Timeout = 180000 });

			//internal comment
			Console.WriteLine("add internal comment");
			await Page.GetByRole(AriaRole.Link, new() { Name = "Internal comment" }).ClickAsync(locatorClickOptions);
			await Page.Locator("#ctl00_MainContent_tbInternalComment").FillAsync("supplier inter comment " + CurrentDate);

			//add comment for customer
			Console.WriteLine("add comment for customer");
			await Page.GetByRole(AriaRole.Link, new() { Name = "Comment for customer" }).ClickAsync(locatorClickOptions);
			await Page.Locator("#ctl00_MainContent_tbExternalComment").FillAsync("comment to customer " + CurrentDate);

			//reject position 1
			Console.WriteLine("reject pos1");
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_anchorRejectOfferPosition").ClickAsync(locatorClickOptions);

			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbRejectPositionComment").ClickAsync(locatorClickOptions);
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbRejectPositionComment").FillAsync("test");
			await Page.GetByRole(AriaRole.Cell, new() { Name = "item title pos1 item" }).Locator("#ibSaveRejectMessage").ClickAsync();
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_lbSaveRejectMessageFromMsg").ClickAsync();

			//add offer for pos 2
			Console.WriteLine("add offer pos2");
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_anchorEditCut").ClickAsync(locatorClickOptions);
			//add pos 2 info
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbArticleNumber").FillAsync("2");
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbDeliveryDays").FillAsync("2");
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbPricePerUnit_BeforeComma").FillAsync("2");
			//save pos 2
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_ibPositionCreationSave").ClickAsync(locatorClickOptions);
			//confirm save
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_lbPositionCreationSave").ClickAsync(locatorClickOptions);

			//add offer for pos 3
			Console.WriteLine("add offer pos3");
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl03_anchorEditCut").ClickAsync(locatorClickOptions);
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl03_tbArticleNumber").FillAsync("3");
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl03_tbDeliveryDays").FillAsync("3");
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl03_tbPricePerUnit_BeforeComma").FillAsync("3");
			//save position
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl03_ibPositionCreationSave").ClickAsync(locatorClickOptions);
			//confirm save
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl03_lbPositionCreationSave").ClickAsync(locatorClickOptions);

			//send
			Console.WriteLine("click send offer on bottom action bar");
			await Page.Locator("#ctl00_MainContent_offerActionBarBottom_lblSendAnchorColapsed").ClickAsync(locatorClickOptions);

			//await Page.Locator("#ctl00_MainContent_offerActionBarTop_divSendOffer").GetByRole(AriaRole.Link, new() { Name = "Send" }).ClickAsync(locatorClickOptions);
			//above fails
			//await Page.Locator("#ctl00_MainContent_offerActionBarBottom_divSendOffer").GetByRole(AriaRole.Link, new() { Name = "Send" }).ClickAsync(locatorClickOptions);
			if (Environment == "QA")
			{
				await Task.Delay(3000);
				await Page.ScreenshotAsync(new()
				{
					FullPage = true,
					Path = downloadPath + "TC09_" + requestToOfferTransactionName + "QQB_SupplierCreatesOffer_ClickSend.png",
					Timeout = 180000
				});
			}
			Console.WriteLine("click send offer confirmation");
			//await Page.Locator("#ctl00_MainContent_offerActionBarTop_lbSendOffer").ClickAsync(locatorClickOptions);
			await Page.Locator("#ctl00_MainContent_offerActionBarBottom_lbSendOffer").ClickAsync(locatorClickOptions);

			//wait for url https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx
			Console.WriteLine("wait for easyorder/SupplierRequestList2007.aspx");
			await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

			Console.WriteLine("search for " + requestToOfferTransactionName);
			//search for request , assert new status is sent
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync(locatorClickOptions);
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestToOfferTransactionName);
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync(locatorClickOptions);
			await Page.WaitForLoadStateAsync(LoadState.Load);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			Console.WriteLine("wait for easyorder/SupplierRequestList2007.aspx");
			//waitfor url https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx
			await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

			//wait for results  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData
			await Page.WaitForSelectorAsync("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData");

			//Assert 1 result
			await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager")).ToContainTextAsync("(1 items found)");
			Console.WriteLine("assert status is now sent");
			//assert status is now sent
			await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_lblStatus")).ToContainTextAsync("Sent");
		}
		catch(Exception ex)
		{
			Console.WriteLine("exception TC09_QQB_SupplierCreatesOffer");
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
			//screenshot
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC11_" + requestToOfferTransactionName + "QQB_SupplierCreatesOffer_Exception.png"
			});
			//note: the nunit test runner , the whole suite of tests does not stop when one fails which
			////appears to happen in the playwright test runner, so perhaps softassert is more required in node.js playwright??
			throw ex;
		}
	}

	[Test, Order(10)]
	//[Ignore("not implemented yet")]
	[Category("QQTests")]
	async public Task TC10_QQS_Supplier_Downloads_Offer_Pdf_And_Excel()
	{
		//for testing of individual tests rather than relying on having to run all tests in the suite
		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestToOfferTransactionName = "PW_Auto_RequestToOffer_chromium_20255311243";

		//////////////////////////////////////////////////////////////////////////////

		//test based on devops test case id 181443 , plan id 125397, test suite id 179303 "Create offers"
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
		Console.WriteLine("10: TC10_QQS_Supplier_Downloads_Offer_Pdf_And_Excel");
		string url = PORTAL_LOGIN;
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };

		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				await Page.WaitForLoadStateAsync(LoadState.Load);
				await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
				await Page.GetByPlaceholder("Enter your user name").FillAsync(SUPPLIER_USER1_LOGIN);
				await Page.GetByPlaceholder("Enter your password").FillAsync(SUPPLIER_USER1_PASSWORD);
				await Page.Locator("#signInButtonId").IsEnabledAsync();
				await Page.Locator("#signInButtonId").ClickAsync(locatorClickOptions);
				loggedin = true;
			}
			catch (Exception e)
			{
				attempts++;
				Console.WriteLine("Problems logging in, attempt:" + attempts.ToString());
				//seeing a lot of errors of type
				/*
					Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F
					Call log:- navigating to "https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F", waiting until "load"
					*/
				Console.WriteLine(e.Message);
			}
		}
		Console.WriteLine("Page: " + Page.Url);

		await Page.WaitForSelectorAsync("//side-bar-item-group[@name='Opportunities']", new PageWaitForSelectorOptions { Timeout = 180000 });
		Console.WriteLine("Page: " + Page.Url);
		var isCookieConsentVisible = await Page.Locator("#cookie-consent-dialog").IsVisibleAsync();
		if (isCookieConsentVisible)
		{
			await Page.GetByRole(AriaRole.Button, new() { Name = "Accept all" }).ClickAsync();
		}

		await Page.Locator("//side-bar-item-group[@name='Opportunities']").ClickAsync(locatorClickOptions);
		await Page.Locator("//side-bar-item[@name='Requests']").ClickAsync(locatorClickOptions);

		//wait for page to load
		Console.WriteLine("Wait for SupplierrequestList page");
		await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

		//search by transaction name
		Console.WriteLine("search for " + requestToOfferTransactionName);
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync();
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestToOfferTransactionName);
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync();
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await Task.Delay(3000);

		//wait for results  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData
		await Page.WaitForSelectorAsync("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData");

		//Assert 1 result
		await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager > span")).ToContainTextAsync("(1 items found)");
		//#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager > span
		
		//click the download option
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibDownloadArea").ClickAsync(locatorClickOptions);

		//click the offer pdf option
		/*
		 the markup for the offer pdf has the offerid appended, but there is no easy way to figure out what offer id a suppliers offer to a request has as the offer id is not actually displayed anywhere in the ui

		hlOfferPdf
		<a id="ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_hlOfferPdf102362444" title="Download the offer as PDF File" href="Generator.aspx?offId=102362444&amp;withOffer=true" target="_blank">
		<img title="Download the offer as PDF File" src="Design2007/img/contentTables/ico_1.gif" alt="Download the offer as PDF File"></a>

		hlOfferPdfText
    <a id="ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_hlOfferPdfText102362444" 
		title="Download the offer as PDF File" href="Generator.aspx?offId=102362444&amp;withOffer=true" target="_blank">
		Offer_2024222</a>
		*/

		var waitForDownloadTask = Page.WaitForDownloadAsync();

		//can we chain these together to search for link with the text "Download the offer as PDF File" and which contains id with 'hlOfferPdfText'????
		//https://playwright.dev/dotnet/docs/locators#matching-inside-a-locator

		//var pdfOfferlink1 =  Page.GetByRole(AriaRole.Link, new() { Name = "Download the offer as PDF File" }).And(Page.Locator("[id*='hlOfferPdfText']"));

		/*await pdfOfferlink1.First.ClickAsync(new()
		{
			//modifier allows the save as functionality, which makes the generator save to disk rather tha be rendered in a new tab
			Modifiers = new[] { KeyboardModifier.Alt }//may not work in other browsers
		}); */

		await Page.GetByRole(AriaRole.Link, new() { Name = "Download the offer as PDF File" }).First.ClickAsync(new()
		{
			//modifier allows the save as functionality, which makes the generator save to disk rather tha be rendered in a new tab
			Modifiers = new[] { KeyboardModifier.Alt },//may not work in other browsers
			Timeout = 180000
		});

		/*
		 Microsoft.Playwright.PlaywrightException : Error: strict mode violation: GetByRole(AriaRole.Link, new() { Name = "Download the offer as PDF File" }) resolved to 2 elements:
    1) <a target="_blank" title="Download the offer as PDF…>…</a> aka Locator                             ("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_hlOfferPdf100073434")
    2) <a target="_blank" title="Download the offer as PDF…>Download the offer as PDF File</a> aka Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_hlOfferPdfText100073434")
 
		*/

		var download = await waitForDownloadTask;
		var fileName = downloadPath + "TC10_" + requestToOfferTransactionName + download.SuggestedFilename;
		// Wait for the download process to complete and save the downloaded file somewhere
		await download.SaveAsAsync(fileName);

		//open the pdf doc to get a screenshot
		var page1 = await Page.RunAndWaitForPopupAsync(async () =>
		{
			await Page.GetByRole(AriaRole.Link, new() { Name = "Download the offer as PDF File" }).First.ClickAsync(locatorClickOptions);
		}, new PageRunAndWaitForPopupOptions { Timeout = 180000 });

		await Task.Delay(3000);
		DateTime today = DateTime.Now;
		string CurrentDate = $"{today.Year}{today.Month}{today.Day}";
		//assert the contents of the offer pdf
		var pdfUrl = page1.Url;
		try
		{
			Console.WriteLine("pdf downloaded from " + pdfUrl);
			Console.WriteLine("asserting contents of  " + fileName);
			using (PdfDocument pdf = PdfDocument.Open(fileName))
			{
				Page page = pdf.GetPage(1);
				if (page != null)
				{
					Console.WriteLine("Asserting contents of pdf");
					//assert reference is correct
					Assert.That(page.Text.Contains("Offer_" + CurrentDate));
					Assert.That(page.Text.Contains(requestToOfferTransactionName));
					Assert.That(page.Text.Contains(TC02_ASSERT_SUPPLIER1));
					Assert.That(page.Text.Contains(TC10_PDF_ASSERT_TOTAL_VALUE));

					//pos 1 was rejected so should not be in the offer pdf
					Assert.That(page.Text, Does.Not.Contain("pos1"));
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("Issue asserting contents of pdf document " + pdfUrl);
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
		}
		
		//take screenshot
		//screenshot  pdf tab requestToRejectTransactionName.pdf
		await page1.ScreenshotAsync(new()
		{
			FullPage = true,
			Path = downloadPath + "TC10_" + requestToOfferTransactionName + "_QQS_SupplierDownloadsOfferPdfAndExcel.png"
		});

		//download excel
		///////////////////////////////////////////////////////////////////////////////////////////////////////////
		var waitForExcelDownloadTask = Page.WaitForDownloadAsync();
		await Page.GetByRole(AriaRole.Link, new() { Name = "Download the offer as Excel File" }).First.ClickAsync(new()
		{
			//modifier allows the save as functionality, which makes the generator save to disk rather than be rendered in a new tab
			Modifiers = new[] { KeyboardModifier.Alt },
			Timeout = 180000
		});

		var excelDownload = await waitForExcelDownloadTask;
		
		// Wait for the download process to complete and save the downloaded file somewhere
		await excelDownload.SaveAsAsync(downloadPath + "TC10_" + requestToOfferTransactionName + excelDownload.SuggestedFilename);
	}

	[Test, Order(11)]
	[Category("QQTests")]
	async public Task TC11_QQB_Buyer_Checks_Request_Status()
	{

		//await Page.PauseAsync();
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
		/*
		Verify the status of request "Request to reject {yymmdd}" -> should be answered, entire request rejected
		Verify the status of request "request to offer {yymmdd}" -> should be answered but pos 1 rejected,pos2 and pos 3 have offers
		Confirm that the details of the offer are as expected (details match what the supplier added)
		*/

		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestToRejectTransactionName = "PW_Auto_RequestToReject_chromium_20255311241";
		//requestToOfferTransactionName = "PW_Auto_RequestToOffer_chromium_20255311243";

		//////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("11: QQB_Buyer_Checks_Request_Status");
		string url = SEARCHURL;
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 180000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };
		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };

		Console.WriteLine("login to search:" + DateTime.Now.ToLongTimeString());
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loggedin = true;
			}
			catch (Exception ex)
			{
				attempts++;
				Console.WriteLine("exception: " + ex.Message);
				Console.WriteLine(DateTime.Now.ToString());
				Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
				//seeing a lot of errors of type
				/*
						Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
				*/
			}
		}
		if (loggedin == false && attempts >= 10)
		{
			DateTime timeRightNow = DateTime.Now;
			string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC11_LoginError_" + FileCurrentDate + ".png"
			});
		}

		Console.WriteLine("page: " + Page.Url);

		//assert search has a quick quote link for this view/company
		await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

		//assert on request list page
		Console.WriteLine("wait for request list page");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//get current request count
		var currentrequestCount = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount").TextContentAsync();

		//search for the request created in test CreateRequest

		//reset search
		Console.WriteLine("reset search");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbResetFilter").ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		Console.WriteLine("search for " + requestToRejectTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestToRejectTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
		//await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		//wait for results

		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert 1 result
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");
		//check status
		Console.WriteLine("assert status is now answered for " + requestToRejectTransactionName);
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblStatus")).ToContainTextAsync("Answered");

		if(Environment == "QA")
		{
			await Task.Delay(5000);
		}

		//reset
		Console.WriteLine("reset search");
		//
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbResetFilter").ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		//search for offer
		Console.WriteLine("search for " + requestToOfferTransactionName);

		//this line is not being executed, why!!!!!!!!!!!
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestToOfferTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
		//await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//wait for results
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");

		Console.WriteLine("assert status is now answered for " + requestToOfferTransactionName);
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblStatus")).ToContainTextAsync("Answered");

		//assert that the title in the search is as expected
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblTransactionNumber")).ToContainTextAsync(requestToOfferTransactionName);


		//open request to offer
		//edit the request
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_ibEditRequest").ClickAsync(locatorClickOptions);

		//wait for request details page to open

		if(Environment == "QA")
		{
			await Task.Delay(3000);
		}

		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request Details")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert status is Answered
		await Expect(Page.FrameLocator("#qqFrame").Locator("#aspnetForm")).ToContainTextAsync("Answered");
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Answered")).ToBeVisibleAsync();

		Console.WriteLine("assert position 1 is rejected");

		await Expect(Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Cell, new() { Name = $"{TC02_ASSERT_SUPPLIER1} Rejected test" }).GetByRole(AriaRole.Strong)).ToBeVisibleAsync();

		await Expect(Page.FrameLocator("#qqFrame").Locator("#positions-table > tbody > tr:nth-child(3) > td:nth-child(3) > div > div:nth-child(1) > div:nth-child(2)")).ToContainTextAsync("Rejected");

		/*
		----------------------------
		position 2 details:
		short description: item title pos2
		long description: item description pos2

		Article No: 2
		Delivery Day(s):2
		Quantity/Unit: 2 Piece
		Price per unit: 2.00 USD
		----------------------------
		position 3 details:
		short description: item title pos3
		long description: item description pos3

		Article No: 3
		Delivery Day(s):3
		Quantity/Unit: 3 Piece
		Price per unit: 3.00 (EUR QA) (USD PROD)
		 */
		//expand pos2
		Console.WriteLine("expand offer 2");
		await Page.FrameLocator("#qqFrame").Locator(".bgGreen1 > .row-collapsed").First.ClickAsync();

		await Expect(Page.FrameLocator("#qqFrame").Locator("#positions-table > tbody > tr:nth-child(6) > td:nth-child(3) > div > div:nth-child(5) > div:nth-child(2) > div:nth-child(2)")).ToContainTextAsync("2");//article no

		await Expect(Page.FrameLocator("#qqFrame").Locator("#positions-table > tbody > tr:nth-child(6) > td:nth-child(3) > div > div:nth-child(5) > div:nth-child(2) > div:nth-child(6)")).ToContainTextAsync("2");//delivery days

		await Expect(Page.FrameLocator("#qqFrame").Locator("#positions-table > tbody > tr:nth-child(6) > td:nth-child(3) > div > div:nth-child(5) > div:nth-child(2) > div:nth-child(8)")).ToContainTextAsync("2 Piece");//quantity

		await Expect(Page.FrameLocator("#qqFrame").Locator("#positions-table > tbody > tr:nth-child(6) > td:nth-child(3) > div > div:nth-child(5) > div:nth-child(2) > div:nth-child(10)")).ToContainTextAsync(TC11_ASSET_POS_2_UNIT_PRICE);//unit price

		//expand pos3
		Console.WriteLine("expand offer 3");
		await Page.FrameLocator("#qqFrame").Locator("tr:nth-child(9) > td:nth-child(3) > div > div > .row-collapsed").ClickAsync();
		await Expect(Page.FrameLocator("#qqFrame").Locator("#positions-table > tbody > tr:nth-child(9) > td:nth-child(3) > div > div:nth-child(5) > div:nth-child(2) > div:nth-child(6)")).ToContainTextAsync("3");//delivery days 
		await Expect(Page.FrameLocator("#qqFrame").Locator("#positions-table > tbody > tr:nth-child(9) > td:nth-child(3) > div > div:nth-child(5) > div:nth-child(2) > div:nth-child(2)")).ToContainTextAsync("3");//article no 
		await Expect(Page.FrameLocator("#qqFrame").Locator("#positions-table > tbody > tr:nth-child(9) > td:nth-child(3) > div > div:nth-child(5) > div:nth-child(2) > div:nth-child(8)")).ToContainTextAsync("3 Piece");//quantity 
		await Expect(Page.FrameLocator("#qqFrame").Locator("#positions-table > tbody > tr:nth-child(9) > td:nth-child(3) > div > div:nth-child(5) > div:nth-child(2) > div:nth-child(10)")).ToContainTextAsync(TC11_ASSET_POS_3_UNIT_PRICE);//unit price
	}



	[Test, Order(12)]
	[Category("QQTests")]
	async public Task TC12_QQB_Buyer_Downloads_Offer_Pdf_And_Excel()
	{
		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestToOfferTransactionName = "PW_Auto_RequestToOffer_chromium_20255311243";

		//////////////////////////////////////////////////////////////////////////////

		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
		Console.WriteLine("12: QQB_Buyer_Downloads_Offer_Pdf_And_Excel");
		string url = SEARCHURL;
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 180000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };

		Console.WriteLine("login to search:" + DateTime.Now.ToLongTimeString());
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loggedin = true;
			}
			catch (Exception ex)
			{
				attempts++;
				Console.WriteLine("exception: " + ex.Message);
				Console.WriteLine(DateTime.Now.ToString());
				Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
				//seeing a lot of errors of type
				/*
						Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
				*/
			}
		}
		if (loggedin == false && attempts >= 10)
		{
			DateTime timeRightNow = DateTime.Now;
			string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC12_LoginError_" + FileCurrentDate + ".png"
			});
		}

		Console.WriteLine("page: " + Page.Url);

		//assert search has a quick quote link for this view/company
		await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, locatorClickOptions);

		//assert on request list page
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//get current request count
		var currentrequestCount = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount").TextContentAsync();

		//search for the request to offer

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestToOfferTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").ClickAsync();
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//wait for results
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert 1 result
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");

		//click download
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_ibDownloadArea").ClickAsync(locatorClickOptions);

		//await Page.PauseAsync();//causes the playwright test inspector to launch even when not debugging and cause pause of the run

		var waitForDownloadTask = Page.WaitForDownloadAsync();
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Download the offer as PDF File" }).First.ClickAsync(new()
		{
			//modifier allows the save as functionality, which makes the generator save to disk rather tha be rendered in a new tab
			Modifiers = new[] { KeyboardModifier.Alt },
			Timeout = 180000
		});

		var download = await waitForDownloadTask;

		var fileName = downloadPath + "TC12_" + requestToOfferTransactionName + download.SuggestedFilename;

		// Wait for the download process to complete and save the downloaded file somewhere
		await download.SaveAsAsync(fileName);
		Console.WriteLine("file saved to: " + fileName);

		if (Environment == "QA")
		{
			await Task.Delay(3000);
		}

		if (Environment == "PROD")
		{
			await Task.Delay(6000);
		}

		var pdfUrl = "";
		try
		{
				//load pdf in another tab and screenshot it
				var page2 = await Page.RunAndWaitForPopupAsync(async () => //this commonly times out System.TimeoutException : Timeout 180000ms exceeded while waiting for event "Popup"
				{
				//fails if more than one request file result in the request list on the page
				await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Download the offer as PDF File" }).First.ClickAsync(locatorClickOptions);
			}, new PageRunAndWaitForPopupOptions { Timeout = 180000 });
			
			await Task.Delay(3000);
			DateTime today = DateTime.Now;
			string CurrentDate = $"{today.Year}{today.Month}{today.Day}";
			//download url
			pdfUrl = page2.Url;


			Console.WriteLine("pdf downloaded from " + pdfUrl);
			Console.WriteLine("asserting contents of  " + fileName);
			using (PdfDocument pdf = PdfDocument.Open(fileName))
			{
				Page page = pdf.GetPage(1);
				if (page != null)
				{
					Console.WriteLine("Asserting contents of pdf");
					Assert.That(page.Text.Contains(TC02_ASSERT_SUPPLIER1));
					Assert.That(page.Text.Contains(requestToOfferTransactionName));
					Assert.That(page.Text.Contains("item title pos2"));
					Assert.That(page.Text.Contains(TC12_ASSERT_PDF_EMAIL));
					Assert.That(page.Text.Contains("Reference"));
					//Assert.That(page.Text.Contains("Offer_" + CurrentDate));
					Assert.That(page.Text.Contains("item description pos3"));
					Assert.That(page.Text.Contains("Populate Classification 1"));
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("Issue asserting contents of pdf document " + pdfUrl);
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
		}

		//get excel
		try
		{
			//download excel
			///////////////////////////////////////////////////////////////////////////////////////////////////////////
			var waitForExcelDownloadTask = Page.WaitForDownloadAsync();
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Download the request as Excel File" }).First.ClickAsync(new()
			{
				//modifier allows the save as functionality, which makes the generator save to disk rather than be rendered in a new tab
				Modifiers = new[] { KeyboardModifier.Alt },
				Timeout = 180000
			});

			var excelDownload = await waitForExcelDownloadTask;

			// Wait for the download process to complete and save the downloaded file somewhere
			await excelDownload.SaveAsAsync(downloadPath + "TC12_" + requestToOfferTransactionName + excelDownload.SuggestedFilename);
		}
		catch (Exception ex)
		{
			Console.WriteLine("exception TC12_QQB_Buyer_Downloads_Offer_Pdf_And_Excel");
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
		}
	}


	[Test, Order(13)]
	[Category("QQTests")]
	async public Task TC13_QQB_Buyer_Makes_Change_Request()
	{
		//test based on devops test caseid 181458 : Buyer make change request
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
		/*
		  Open "PW_Auto_RequestToOffer {yymmdd}"
		  Expand the rejection in first offer
		  Click the request for change link
		  Enter comment "change request test {yymmdd}" and click save button
			Click send
		*/

		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestToRejectTransactionName = "PW_Auto_RequestToReject_chromium_20255311241";
		//requestToOfferTransactionName = "PW_Auto_RequestToOffer_chromium_20255311243";

		//////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("13: QQB_Buyer_Makes_Change_Request");
		string url = SEARCHURL;
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 180000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };
		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };

		Console.WriteLine("login to search:" + DateTime.Now.ToLongTimeString());
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loggedin = true;
			}
			catch (Exception ex)
			{
				attempts++;
				Console.WriteLine("exception: " + ex.Message);
				Console.WriteLine(DateTime.Now.ToString());
				Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
				//seeing a lot of errors of type
				/*
						Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
				*/
			}
		}
		if (loggedin == false && attempts >= 10)
		{
			DateTime timeRightNow = DateTime.Now;
			string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC13_LoginError_" + FileCurrentDate + ".png"
			});
		}

		Console.WriteLine("page: " + Page.Url);

		//assert search has a quick quote link for this view/company
		await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

		//assert on request list page
		Console.WriteLine("wait for request list page");

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//get current request count
		var currentrequestCount = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount").TextContentAsync();

		//search for the request to offer

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestToOfferTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//wait for results
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert 1 result
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");

		//assert current status is answered
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblStatus")).ToContainTextAsync("Answered");//i.e. and offer has been made

		//edit the request
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_ibEditRequest").ClickAsync(locatorClickOptions);

		//wait for request details page to open
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request Details")).ToBeVisibleAsync(locatorVisibleAssertion);
		//	await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Request Details");
		Console.WriteLine("expand the first position offer rejection");
		//expand the first position offer rejection
		//await Page.FrameLocator("#qqFrame").Locator(".row-collapsed").First.ClickAsync(locatorClickOptions); // not unique
		//3rd row of positions-table 
		await Page.FrameLocator("#qqFrame").Locator("#positions-table > tbody > tr:nth-child(3) > td:nth-child(3) > div > div:nth-child(1) > div.row-collapsed").ClickAsync(locatorClickOptions);
		Console.WriteLine("click the request for change button");
		//click the request for change button
		await Page.FrameLocator("#qqFrame").Locator("#positions-table > tbody > tr:nth-child(3) > td:nth-child(3) > div > div.bgBlue1 > button > img").ClickAsync(locatorClickOptions);

		//await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Cell, new() { Name = "TESTSUPCDO4 Rejected test" }).GetByRole(AriaRole.Strong).ClickAsync(locatorClickOptions);
		//await Page.FrameLocator("#qqFrame").GetByText("Rejected test").First.ClickAsync(locatorClickOptions);

		//assert modalpopup
		await Expect(Page.FrameLocator("#qqFrame").Locator("#RequestForChangeModal").GetByText("Request for change")).ToBeVisibleAsync(locatorVisibleAssertion);

		await Expect(Page.FrameLocator("#qqFrame").Locator("#RequestForChangeMessage")).ToBeVisibleAsync(locatorVisibleAssertion);

		DateTime today = DateTime.Now;
		string CurrentDate = $"{today.Year}{today.Month}{today.Day}";

		await Page.FrameLocator("#qqFrame").Locator("#RequestForChangeMessage").ClickAsync(locatorClickOptions);
		//set request for change message as "change request test yyymmdd"
		await Page.FrameLocator("#qqFrame").Locator("#RequestForChangeMessage").FillAsync("change request test" + CurrentDate);
		Console.WriteLine("save rfc");
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync(locatorClickOptions);

		//assert modal popup
		await Expect(Page.FrameLocator("#qqFrame").Locator("#SendRequestForChangeModal").GetByText("Request for change")).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("send");
		await Expect(Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Send" })).ToBeVisibleAsync(locatorVisibleAssertion);
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync(locatorClickOptions);
		Console.WriteLine("assert ChangeRequested status");
		//assert status on request details page has been modified to ChangeRequested
		await Expect(Page.FrameLocator("#qqFrame").Locator("#aspnetForm")).ToContainTextAsync("ChangeRequested");

		//go back to request list
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Requests" }).ClickAsync(locatorClickOptions);
		//wait for page

		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//reset search
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		//search for request to offer
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestToOfferTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);

		//wait for results
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert 1 result
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");

		//ASSERT STATUS IS ChangeRequested AS EXPECTED
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblStatus")).ToContainTextAsync("ChangeRequested");
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(14)]
	[Category("QQTests")]
	async public Task TC14_QQS_Supplier_Updates_Offer()
	{
		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestToRejectTransactionName = "PW_Auto_RequestToReject_chromium_20255311241";
		//requestToOfferTransactionName = "PW_Auto_RequestToOffer_chromium_20255311243";

		//////////////////////////////////////////////////////////////////////////////
		/*
			Open requestToOfferTransactionName yymmdd
			Expand position 1
			Fill offer with:
			Short description : Rej to Offer
			Long description : From rejected to position with offer
			Article No : R2O{yymmdd}
			Classification : select 20010101
			Delivery Days : 7
			Quantity : 7
			Unit : Liter
			Price per unit : 707
			Expiration date : today() + 28
			Click save button
		*/
		try
		{
			//assume that BUYER_CHOOSES_CLASSIFICATION is true here
			//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
			//based on devops test case id 181460 : Supplier update offer
			Console.WriteLine("14: TC14_QQS_Supplier_Updates_Offer");
			string url = PORTAL_LOGIN;
			PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };

			PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
			LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
			LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
			LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };

			bool loggedin = false;
			int attempts = 0;
			while (loggedin == false && attempts < 10)
			{
				try
				{
					await Page.GotoAsync(url, pageGotoOptions);
					await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
					await Page.WaitForLoadStateAsync(LoadState.Load);
					await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
					await Page.GetByPlaceholder("Enter your user name").FillAsync(SUPPLIER_USER1_LOGIN);
					await Page.GetByPlaceholder("Enter your password").FillAsync(SUPPLIER_USER1_PASSWORD);
					await Page.Locator("#signInButtonId").IsEnabledAsync();
					await Page.Locator("#signInButtonId").ClickAsync(locatorClickOptions);
					loggedin = true;
				}
				catch (Exception e)
				{
					attempts++;
					Console.WriteLine("Problems logging in, attempt:" + attempts.ToString());
					//seeing a lot of errors of type
					/*
						Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F
						Call log:- navigating to "https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F", waiting until "load"
						*/
					Console.WriteLine(e.Message);
				}
			}
			Console.WriteLine("Page: " + Page.Url);

			//wait for page to load https://portal.hubwoo.com/main/
			//await Page.WaitForURLAsync("https://portal.hubwoo.com/main/", pageWaitForUrlOptions);  //this fails for some reason

			await Page.WaitForSelectorAsync("//side-bar-item-group[@name='Opportunities']", new PageWaitForSelectorOptions { Timeout = 180000 });
			Console.WriteLine("Page: " + Page.Url);
			var isCookieConsentVisible = await Page.Locator("#cookie-consent-dialog").IsVisibleAsync();
			if (isCookieConsentVisible)
			{
				await Page.GetByRole(AriaRole.Button, new() { Name = "Accept all" }).ClickAsync();
			}

			await Page.Locator("//side-bar-item-group[@name='Opportunities']").ClickAsync(locatorClickOptions);
			await Page.Locator("//side-bar-item[@name='Requests']").ClickAsync(locatorClickOptions);

			//wait for page to load
			await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);
			Console.WriteLine("search for  " + requestToOfferTransactionName);
			//search by transaction name
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync(locatorClickOptions);
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestToOfferTransactionName);
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync(locatorClickOptions);
			await Page.WaitForLoadStateAsync(LoadState.Load);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			await Task.Delay(3000);

			//wait for results  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData
			await Page.WaitForSelectorAsync("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData");

			//Assert 1 result
			await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager")).ToContainTextAsync("(1 items found)");

			//waitfor url https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx
			await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

			Console.WriteLine("assert status is change requested");
			//assert status is ChangeRequested
			await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_lblStatus")).ToContainTextAsync("ChangeRequested");

			//click edit 
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibEdit").ClickAsync(locatorClickOptions);

			//expect first offer position to have text rejected
			await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_Label37")).ToContainTextAsync("Rejected");

			//expand the rejected offer
			Console.WriteLine("expand rejected offer");//ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_anchorOfferPositionCutRejected
			await Page.GetByRole(AriaRole.Link, new() { Name = "Rejected test" }).ClickAsync(locatorClickOptions);

			DateTime today = DateTime.Now;
			string CurrentDate = $"{today.Year}{today.Month}{today.Day}";
			await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_Label25")).ToContainTextAsync("change request test" + CurrentDate);

			//edit the position ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_ibCutRejectedEdit
			//await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_ibCutRejectedEdit").ClickAsync(locatorClickOptions);//fails?

			//getby text create offer position?
			Console.WriteLine("Create offer position");
			await Page.GetByRole(AriaRole.Button, new() { Name = "Create offer position" }).ClickAsync();

			//assert article no is visible
			await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_lblArticleEdit")).ToBeVisibleAsync(locatorVisibleAssertion);
			await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbDeliveryDays")).ToBeVisibleAsync(locatorVisibleAssertion);
			await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbAmount")).ToBeVisibleAsync(locatorVisibleAssertion);
			await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbShortDescription")).ToBeVisibleAsync(locatorVisibleAssertion);
			await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbLongDescription")).ToContainTextAsync("item description pos1");

			//complete offer details
			Console.WriteLine("complete offer position details");
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbShortDescription").FillAsync("Rej to Offer");
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbLongDescription").FillAsync("From rejected to position with offer");
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbArticleNumber").FillAsync("R2O" + CurrentDate);

			//classification is not enabled
			//assert classification is not shown?

			//delivery days
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbDeliveryDays").FillAsync("7");
			//quantity
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbAmount").FillAsync("7");
			//unit price
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbPricePerUnit_BeforeComma").FillAsync("707");

			LocatorSelectOptionOptions locatorSelectOptions = new LocatorSelectOptionOptions { Timeout = 180000 };
			//ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_ddlUnit
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_ddlUnit").SelectOptionAsync(new SelectOptionValue { Value = "LTR" }, locatorSelectOptions);//label = Liter

			Console.WriteLine("Set expiration date");

			//pick date 28 days from now
			DateTime expiryDate = today.AddDays(28);
			int expiryYear = expiryDate.Year;
			int expiryMonth = expiryDate.Month - 1;//zero index
			int expiryDay = expiryDate.Day;
			Console.WriteLine("Set expiration date to " + expiryDate.ToLongDateString());
			//click calendar
			//expiration date for the previously rejetced position #ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_divEditOfferPositionDetails > div:nth-child(1) > table > tbody > tr:nth-child(7) > td.OutTblForm > img
			await Page.GetByRole(AriaRole.Img, new() { Name = "..." }).ClickAsync(locatorClickOptions);

			await Page.Locator(".ui-datepicker-year").SelectOptionAsync(new SelectOptionValue { Value = expiryYear.ToString() }, locatorSelectOptions);

			await Page.Locator(".ui-datepicker-month").SelectOptionAsync(new SelectOptionValue { Value = expiryMonth.ToString() }, locatorSelectOptions);

			await Page.Locator("#ui-datepicker-div").Page.GetByRole(AriaRole.Link, new() { Name = expiryDay.ToString(), Exact = true }).ClickAsync(locatorClickOptions);

			//this fails if the day string is not unique e.g. 2 maps to 2, 12,20,21,22,23,24 etc so need to use the Exact option for the PageGetByRoleOptions

			Console.WriteLine("submit offer");
			//save 
			await Page.GetByRole(AriaRole.Button, new() { Name = "Submit" }).ClickAsync(locatorClickOptions);

			//save
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_lbSaveEditFromMsg").ClickAsync();

			//send
			//await Page.Locator("#ctl00_MainContent_offerActionBarTop_divSendOffer").GetByRole(AriaRole.Link, new() { Name = "Send" }).ClickAsync(locatorClickOptions);
			Console.WriteLine("click send offer on bottom action bar");
			await Page.Locator("#ctl00_MainContent_offerActionBarBottom_lblSendAnchorColapsed").ClickAsync(locatorClickOptions);

			if (Environment == "QA")
			{
				await Task.Delay(3000);
				await Page.ScreenshotAsync(new()
				{
					FullPage = true,
					Path = downloadPath + "TC09_" + requestToOfferTransactionName + "QQB_SupplierCreatesOffer_ClickSend.png",
					Timeout = 180000
				});
			}
			Console.WriteLine("click send offer confirmation");
			//offer is not valid you must select a classification code issue fixed in v24.2 see jira OC-9260

			//send confirmation
			//await Page.Locator("#ctl00_MainContent_offerActionBarTop_lbSendOffer").ClickAsync(locatorClickOptions);
			await Page.Locator("#ctl00_MainContent_offerActionBarBottom_lbSendOffer").ClickAsync(locatorClickOptions);
			//wait for page to load
			await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

			//search by transaction name
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync(locatorClickOptions);
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestToOfferTransactionName);
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync(locatorClickOptions);

			await Task.Delay(3000);

			//wait for results  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData
			await Page.WaitForSelectorAsync("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData");

			//Assert 1 result
			await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager")).ToContainTextAsync("(1 items found)");

			Console.WriteLine("assert status is now sent");
			//assert status is now sent
			await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_lblStatus")).ToContainTextAsync("Sent");
			Console.WriteLine("test step complete");
		}
		catch(Exception ex)
		{
			Console.WriteLine("exception TC02_QQB_CreateARequest");
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
			//screenshot
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC014_" + requestToOfferTransactionName + "QQS_Supplier_Updates_offer_Exception.png"
			});
			throw ex;
		}
	}


	[Test, Order(15)]
	[Category("QQTests")]
	async public Task TC15_QQB_Buyer_Order_Offer_Simple_Datasheet()
	{
        //test based on devops test caseid 181476 : Buyer order offer (simple datasheet)
        //https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
        /*
		 Open "request to offer {yymmdd}"
		 Select "All" from the drop down list right to header "Quantity"
		 Select "Order Position" from the drop down list "Actions for selected positions"
		 Click the ">" button next to drop down list
		*/

        //TODO COMMENT OUT BEFORE PROD TESTING
        //////////////////////////////////////////////////////////////////////////////

        //requestToOfferTransactionName = "PW_Auto_RequestToOffer_chromium_20257191515";
        //requestToOfferId = "102597803";

        //////////////////////////////////////////////////////////////////////////////

        Console.WriteLine("15: TC15_QQB_Buyer_Order_Offer_Simple_Datasheet");
		string url = SEARCHURL;
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 180000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
		LocatorAssertionsToBeCheckedOptions locatorCheckedAssertion = new LocatorAssertionsToBeCheckedOptions { Timeout = 180000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };
		Console.WriteLine("login to search:" + DateTime.Now.ToLongTimeString());
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loggedin = true;
			}
			catch (Exception ex)
			{
				attempts++;
				Console.WriteLine("exception: " + ex.Message);
				Console.WriteLine(DateTime.Now.ToString());
				Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
				//seeing a lot of errors of type
				/*
						Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
				*/
			}
		}

		if (loggedin == false && attempts >= 10)
		{
			DateTime timeNow = DateTime.Now;
		  string fileCurrentDate = $"{timeNow.Year}{timeNow.Month}{timeNow.Day}{timeNow.Hour}{timeNow.Minute}";
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC15_LoginError_" + fileCurrentDate + ".png"
			});
		}

		Console.WriteLine("page: " + Page.Url);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		if (await Page.Locator("#shoppingCartTitle").IsVisibleAsync())
		{
			await Page.Locator("//*[@data-testid='removeAllItems']").ClickAsync();
			await Task.Delay(TimeSpan.FromSeconds(1));
			await Page.Locator("//button[contains(@id, 'noty_button') and text()='OK']").ClickAsync();
		}

		//assert search has a quick quote link for this view/company
		await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

		//assert on request list page
		Console.WriteLine("wait for request list page");

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//get current request count
		var currentrequestCount = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount").TextContentAsync();

		//search for the request to offer

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestToOfferTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//wait for results
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert 1 result
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");

		//assert current status is answered

		try
		{
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblStatus")).ToContainTextAsync("Answered", new LocatorAssertionsToContainTextOptions { IgnoreCase = true, Timeout = 10000 });//i.e. and offer has been made
		}
		catch
		{
			//note: if the test is repeated will fail as state will be 'Item has been added to the Cart'!
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblStatus")).ToContainTextAsync("Item has been added to the Cart", new LocatorAssertionsToContainTextOptions {IgnoreCase = true, Timeout = 10000 });
		}

		//edit the request
		Console.WriteLine("Edit the request");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_ibEditRequest").ClickAsync(locatorClickOptions);

		//wait for request details page to open
		await Task.Delay(3000);
		Console.WriteLine("Assert request details visible");

		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request Details")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert status is Answered
		try
		{
			await Expect(Page.FrameLocator("#qqFrame").Locator("#aspnetForm")).ToContainTextAsync("Answered", new LocatorAssertionsToContainTextOptions { IgnoreCase = true, Timeout = 5000 });
			await Expect(Page.FrameLocator("#qqFrame").GetByText("Answered")).ToBeVisibleAsync( new LocatorAssertionsToBeVisibleOptions { Timeout = 5000});
		}
		catch
		{
			await Expect(Page.FrameLocator("#qqFrame").Locator("#aspnetForm")).ToContainTextAsync("Item has been added to the Cart", new LocatorAssertionsToContainTextOptions { IgnoreCase = true, Timeout = 10000 });
			await Expect(Page.FrameLocator("#qqFrame").GetByText("Item has been added to the Cart", new FrameLocatorGetByTextOptions { Exact = false })).ToBeVisibleAsync();
		}
		Console.WriteLine("assert checkboxes are visible");
		await Expect(Page.FrameLocator("#qqFrame").Locator("(//input[@type='checkbox'])[1]")).ToBeVisibleAsync(locatorVisibleAssertion);
		
		await Expect(Page.FrameLocator("#qqFrame").Locator("(//input[@type='checkbox'])[2]")).ToBeVisibleAsync(locatorVisibleAssertion);

		await Expect(Page.FrameLocator("#qqFrame").Locator("(//input[@type='checkbox'])[3]")).ToBeVisibleAsync(locatorVisibleAssertion);

		//Select "All" from the drop down list right to header "Quantity"
		Console.WriteLine("select all checkboxes option");
		await Page.FrameLocator("#qqFrame").Locator("select[name=\"position\"]").SelectOptionAsync(new[] { "all" });

		//assert that all 3 checkboxes are checked
		Console.WriteLine("assert that all 3 checkboxes are checked");
		await Expect(Page.FrameLocator("#qqFrame").Locator("(//input[@type='checkbox'])[1]")).ToBeCheckedAsync(locatorCheckedAssertion);

		await Expect(Page.FrameLocator("#qqFrame").Locator("(//input[@type='checkbox'])[2]")).ToBeCheckedAsync(locatorCheckedAssertion);

		await Expect(Page.FrameLocator("#qqFrame").Locator("(//input[@type='checkbox'])[3]")).ToBeCheckedAsync(locatorCheckedAssertion);

		//Select "Order Position" from the drop down list "Actions for selected positions"
		//Click the ">" button next to drop down list
		await Page.FrameLocator("#qqFrame").Locator("#ddlChooseAction").SelectOptionAsync(new[] { "4" });//order

		Console.WriteLine("load shopping cart");
		//click > button
		//await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = ">" }).ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("//*[@id='btnOrder']/preceding-sibling::button").ClickAsync(locatorClickOptions);

		//wait for page
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		DateTime timeRightNow = DateTime.Now;
		string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
		await Page.ScreenshotAsync(new()
		{
			FullPage = true,
			Path = downloadPath + "TC15_ShoppingCart_" + FileCurrentDate + ".png"
		});

		Console.WriteLine("assert shopping cart has 3 items");
		//await Expect(Page.Locator("#shoppingCartTitle")).ToContainTextAsync("Shopping Cart (3)");

		//await Expect(Page.GetByRole(AriaRole.Banner)).ToContainTextAsync("Shopping Cart");
		await Expect(Page.Locator("//*[@role='main']//h1[@id='shoppingCartTitle']")).ToContainTextAsync("Shopping Cart");
		//await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Transfer Shopping Cart" }).First).ToBeVisibleAsync(locatorVisibleAssertion);
		await Expect(Page.Locator("//button[contains(text(), 'Transfer Shopping Cart')]").First).ToBeVisibleAsync(locatorVisibleAssertion);

		Console.WriteLine("check shopping cart items");
		Console.WriteLine("assert title of first basket item is 'Rej to Offer'");

		await Expect(Page.Locator("(//td[@class='product-list__column product-list__title'])[1]")).ToContainTextAsync("Rej to Offer");

		Console.WriteLine("assert title of second basket item is 'item title pos2'");
		await Expect(Page.Locator("(//td[@class='product-list__column product-list__title'])[2]")).ToContainTextAsync("item title pos2");

		Console.WriteLine("assert title of third basket item is 'item title pos3'");
		await Expect(Page.Locator("(//td[@class='product-list__column product-list__title'])[3]")).ToContainTextAsync("item title pos3");
		
		//#searchResultsContent > div > div:nth-child(2) > table > tbody > tr:nth-child(1) > td.product-list__column.product-list__title  //Rej to Offer
		//#searchResultsContent > div > div:nth-child(2) > table > tbody > tr:nth-child(2) > td.product-list__column.product-list__title //item title pos2
		//#searchResultsContent > div > div:nth-child(2) > table > tbody > tr:nth-child(3) > td.product-list__column.product-list__title //item title pos3

		//assert supplier, doesn't work on prod, diference in the jsp files?
		if (Environment == "QA")
		{
			await Expect(Page.Locator(".product-list__column.product-list__supplier").First).ToContainTextAsync(TC02_ASSERT_SUPPLIER1);
		}
	}

	[Test, Order(16)]
	[Category("EmailTests")]
	public void TC16_QQB_New_Request_Email_For_Supplier()
	{
		//this is the first of the email tests, lets wait for 1 minute to allow all the emails to be sent/recieved
		try
		{
			//System.Threading.Thread.Sleep(180000);
		}
		catch { }
		//based on devops test plan id 181284 : New request email for supplier
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303

		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////
		
		//requestToOfferTransactionName = "PW_Auto_RequestToOffer_chromium_20255311640";
		//requestToRejectTransactionName = "PW_Auto_RequestToReject_chromium_20255311638";
		//requestToOfferId = "102701183";
		//requestToRejectId = "102701182";

		//testStartSecondsSinceEpoch = "1714032974";  //after: 1709650575  Tuesday, March 5, 2024 2:56:15 PM
		
		//////////////////////////////////////////////////////////////////////////////

		/*
		Check email easyordertest@gmail.com 

		check that suppliers have recieved emails for the 
		request created during this test suite i.e. the requests created in test steps tc02 and tc03
		requestToRejectId
		requestToOfferId
	  
		emails received on QA  (3 expected)
		---------------------
		EasyOrderTest+SVS1user -> [QA] [Quick Quote] You have received a new Quick Quote request -- (SV Supplier 1)
		EasyOrderTest+SV5 -> [QA] [Quick Quote] You have received a new Quick Quote request  -- (SV Supplier 1)
		EasyOrderTest+fmksqq -> [QA] [Quick Quote] You have received a new Quick Quote request -- fmks

		emails received on  Prod (1 expected) but 2 sent
		-------------------------
		EasyOrderTest+SupplierTS4@gmail.com ->[Quick Quote] You have received a new Quick Quote request GENERIC


		OmniContent+EPM_TS5@gmail.com - different email account not testing
		*/
		bool testPassed = true;
		Console.WriteLine("16: TC16_QQB_New_Request_Email_For_Supplier");
		Console.WriteLine("instantiate gmail api service");
		try
		{
			UserCredential credential;
			// Load client secrets.
			using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
			{
				/* The file token.json stores the user's access and refresh tokens, and is created
				 automatically when the authorization flow completes for the first time. */
				string credPath = "token.json";
				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
						GoogleClientSecrets.FromStream(stream).Secrets,
						Scopes,
						"user",
						CancellationToken.None,
						new FileDataStore(credPath, true)).Result;
				Console.WriteLine("Credential file saved to: " + credPath);
			}

			System.Threading.Thread.Sleep(5000);

			// Create Gmail API service.
			bool connected = false;
			GmailService? service = null;
			IList<Message> messages = new List<Message>();
			UsersResource.MessagesResource.ListRequest? requestMessage = null;
			int connectionAttempt = 0;
			while (!connected && connectionAttempt < 10)
			{
				Console.WriteLine("create gmail service, attempt: " + (connectionAttempt + 1).ToString());
				try
				{
					if (service == null || requestMessage == null)
					{
						service = new GmailService(new BaseClientService.Initializer
						{
							HttpClientInitializer = credential,
							ApplicationName = ApplicationName
						});
						requestMessage = service.Users.Messages.List("easyordertest@gmail.com");
						requestMessage = service.Users.Messages.List("easyordertest@gmail.com");
						requestMessage.LabelIds = "INBOX";
						requestMessage.IncludeSpamTrash = false;
						//Change Request message template has Request-number:
						//Offer received and new request templates have Request ID:
						//Request rejected template has Request-id:
						requestMessage.Q = $"after:{testStartSecondsSinceEpoch} AND subject:({TC_REQUEST_RECEIVED_EMAIL_SUBJECT}) AND {requestToOfferTransactionName}";
						//requestMessage.Q = $"after:{testStartSecondsSinceEpoch} AND subject:({TC_REQUEST_RECEIVED_EMAIL_SUBJECT}) AND {requestToOfferId.TOString()}";
						//is it better to search for requestToOfferTransactionName or requestid
					}
					Console.WriteLine("Q. " + requestMessage.Q);
					Console.WriteLine("read messages");
					messages = requestMessage.Execute().Messages;//often get exception here he SSL connection could not be established
					if (credential == null)
					{
						Console.WriteLine("credential is null");
					}
					connected = true;
				}
				catch (Exception ex)
				{
					Console.WriteLine("exception:" + DateTime.Now.ToLongTimeString());
					if (service == null)
					{
						Console.WriteLine("service is null");
					}

					if (credential == null)
					{
						Console.WriteLine("credentials are null");
					}
					Console.WriteLine("gmail exception: " + ex.Message);
					if(ex.InnerException!= null)
					{
						Console.WriteLine(ex.InnerException.Message);
					}

					connectionAttempt++;
					Console.WriteLine("gmail service instantiation attempt: " + connectionAttempt.ToString());
				}
			}
			Console.WriteLine("***************************************************");
			Console.WriteLine("search for new request received email messages for " + requestToOfferTransactionName + " ID: " + requestToOfferId);
			Console.WriteLine("***************************************************");
			//search for new request emails for request requestToOfferTransactionName
			if (Environment == "QA")
			{
				Console.WriteLine("expecting 3 new request received email messages for " + requestToOfferTransactionName + " ID: " + requestToOfferId);
			}

			if (Environment == "PROD")
			{
				Console.WriteLine("expecting 1 new request received email message for " + requestToOfferTransactionName + " ID: " + requestToOfferId);
			}

			if (messages == null || messages.Count == 0)
			{
				Console.WriteLine("No new request received email messages found for request: " + requestToOfferId);
				if(messages == null)
				{
					Console.WriteLine("messages null");
					testPassed = false;
				}
				else
				{
					Console.WriteLine("messages count  == 0");
					testPassed = false;
				}
			}
			else
			{
				Console.WriteLine("messages count:" + messages.Count.ToString());
				if (Environment == "QA")
				{
					//expected 3 messages on qa

					Console.WriteLine("expecting 3 new request received email messages for " + requestToOfferTransactionName + " ID: " + requestToOfferId);
					Console.WriteLine("found " + messages.Count.ToString() + " email message(s)");
					Assert.That(messages.Count == 3);
				}

				if (Environment == "PROD")
				{
					//expected 1, note one is in omnicontentuser+ email box
					Console.WriteLine("expecting 1 new request received email message for " + requestToOfferTransactionName + " ID: " + requestToOfferId);
					Console.WriteLine("found " + messages.Count.ToString() + " email message(s)");
					Assert.That(messages.Count == 1);
				}
			}
			Console.WriteLine("***************************************************");
			Console.WriteLine("search for new request received email messages for " + requestToRejectTransactionName + " ID: " + requestToRejectId);
			Console.WriteLine("***************************************************");
			//repeat for the second request requestToRejectTransactionName
			if (Environment == "QA")
			{
				Console.WriteLine("expecting 3 new request received email messages for " + requestToRejectTransactionName + " ID: " + requestToRejectId);
			}

			if (Environment == "PROD")
			{
				Console.WriteLine("expecting 1 new request received email message for " + requestToRejectTransactionName + " ID: " + requestToRejectId);
			}

			requestMessage.Q = $"after:{testStartSecondsSinceEpoch} AND subject:({TC_REQUEST_RECEIVED_EMAIL_SUBJECT}) AND {requestToRejectTransactionName}";
			Console.WriteLine("Q. " + requestMessage.Q);
			Console.WriteLine("read messages");
			messages = requestMessage.Execute().Messages;
			if (messages == null || messages.Count == 0)
			{
				Console.WriteLine("No new request received email messages found for request " + requestToRejectTransactionName + " ID: " + requestToRejectId);
				if (messages == null)
				{
					Console.WriteLine("messages null");
					testPassed = false;
					Console.WriteLine("testPassed :" + testPassed.ToString());
				}
				else
				{
					Console.WriteLine("messages count == 0");
					testPassed = false;
					Console.WriteLine("testPassed :" + testPassed.ToString());
				}
			}
			else
			{
				Console.WriteLine("messages count:" + messages.Count.ToString());
				if (Environment == "QA")
				{
					//expected 3 messages on qa

					Console.WriteLine("expecting 3 new request received email messages for " + requestToRejectTransactionName + " ID: " + requestToRejectId);
					Console.WriteLine("found " + messages.Count.ToString() + " email message(s)");
					Assert.That(messages.Count == 3);
				}

				if (Environment == "PROD")
				{
					//expected 1, note one is in omnicontentuser+ email box
					Console.WriteLine("expecting 1 new request received email message for " + requestToRejectTransactionName + " ID: " + requestToRejectId);
					Console.WriteLine("found " + messages.Count.ToString() + " email message(s)");
					Assert.That(messages.Count == 1);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null)
			{
				Console.WriteLine(ex.InnerException.Message);
			}
			throw ex;
		}
		Console.WriteLine("testPassed :" + testPassed.ToString());
		Assert.That(testPassed == true);
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(17)]
	[Category("EmailTests")]
	 public void TC17_QQB_New_Request_Rejection_Email_For_Buyer()
	{
		//based on devops test plan id 181295 : New request rejection email for buyer
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303

		/*
		 Check email easyordertest@gmail.com

		 QA
		 1) fmk s -> EasyOrderTest+fmkbqq -> [QA] [Quick Quote] Request has been rejected!

		 PROD
		 1) Supplier Account EPAM ->  easyordertest+TESTCOE04QQ -> [Quick Quote] Request has been rejected  

		*/

		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////
		/*
		//requestToOfferTransactionName = "PW_Auto_RequestToOffer_chromium_20244242114";
		//requestToRejectTransactionName = "PW_Auto_RequestToReject_chromium_20244242112";
		//requestToOfferId = "102451076";
		//requestToRejectId = "102451075";

		//testStartSecondsSinceEpoch = "1713988951";
		*/
		//////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("17: QQB_New_Request_Rejection_Email_For_Buyer");
		Console.WriteLine("instantiate gmail api service");
		try
		{
			UserCredential credential;
			// Load client secrets.
			using (var stream =
						 new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
			{
				/* The file token.json stores the user's access and refresh tokens, and is created
				 automatically when the authorization flow completes for the first time. */
				string credPath = "token.json";
				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
						GoogleClientSecrets.FromStream(stream).Secrets,
						Scopes,
						"user",
						CancellationToken.None,
						new FileDataStore(credPath, true)).Result;
				Console.WriteLine("Credential file saved to: " + credPath);
			}
      
			System.Threading.Thread.Sleep(2000);

			// Create Gmail API service.
			bool connected = false;
			GmailService? service = null;
			IList<Message> messages = new List<Message>();
			int connectionAttempt = 0;
			while (!connected && connectionAttempt < 10)
			{
				Console.WriteLine("create gmail service, attempt: " + (connectionAttempt + 1).ToString());
				try
				{
					if (service == null)
					{
						service = new GmailService(new BaseClientService.Initializer
						{
							HttpClientInitializer = credential,
							ApplicationName = ApplicationName
						});
					}
					UsersResource.MessagesResource.ListRequest requestMessage = service.Users.Messages.List("easyordertest@gmail.com");
					requestMessage.LabelIds = "INBOX";
					requestMessage.IncludeSpamTrash = false;
					//note the message templates have a slightly different wording around  the request, e.g.
					//Change Request message template has Request-number:
					//Offer received and new request templates have Request ID:
					//Request rejected template has Request-id:
					requestMessage.Q = $"after:{testStartSecondsSinceEpoch} subject:({TC_REQUEST_REJECTED_EMAIL_SUBJECT})  AND \"Request-id: {requestToRejectId}\"";
					Console.WriteLine("Q. = " + requestMessage.Q);
					messages = requestMessage.Execute().Messages;
					connected = true;
				}
				catch (Exception ex)
				{
					Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
					if (service == null)
					{
						Console.WriteLine("service is null");
					}

					if (credential == null)
					{
						Console.WriteLine("credentials are null");
					}
					Console.WriteLine("gmail exception: " + ex.Message);
					if (ex.InnerException != null)
					{
						Console.WriteLine(ex.InnerException.Message);
					}
					connectionAttempt++;
					Console.WriteLine("gmail service instantiation attempt: " + connectionAttempt.ToString());
				}
			}

			if (messages == null || messages.Count == 0)
			{
				Console.WriteLine("No email new request rejected message found for request " + requestToOfferId);
				if (messages == null)
				{
					Console.WriteLine("messages null");
					Assert.That(messages != null);
				}
				else
				{
					Console.WriteLine("messages count  == 0");
					Assert.That(messages.Count != 0);
				}
			}
			else
			{
				Console.WriteLine("messages count:" + messages.Count.ToString());
				if (Environment == "QA")
				{
					//expected 1 message on qa

					Console.WriteLine("expecting 1 request rejected email message for " + requestToRejectTransactionName + " ID: " + requestToRejectId);
					Console.WriteLine("found " + messages.Count.ToString() + " email message(s)");
					Assert.That(messages.Count == 1);
				}

				if (Environment == "PROD")
				{
					//expected 1, note one is in omnicontentuser+ email box
					Console.WriteLine("expecting 1 request rejected email message for " + requestToRejectTransactionName + " ID: " + requestToRejectId);
					Console.WriteLine("found " + messages.Count.ToString() + " email message(s)");
					Assert.That(messages.Count == 1);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("Error reading email messages :" + DateTime.Now.ToLongTimeString());
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null)
			{
				Console.WriteLine(ex.InnerException.Message);
			}
			throw ex;
		}
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());

	}

	[Test, Order(18)]
	[Category("EmailTests")]
	 public void TC18_QQB_New_Offer_Email_For_Buyer()
	{
		//based on devops test plan id 181442 : New offer email for buyer 
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303

		Console.WriteLine("18: QQB_New_Offer_Email_For_Buyer");
		/*
		 Check email easyordertest@gmail.com

		 QA  2 expected emails
		 1) fmks qq -> EasyOrderTest+fmkbqq --> [QA][Quick Quote] You received an offer
		 after change request/supp;ier updates offer for the initial request
		 2) fmks qq -> EasyOrderTest+fmkbqq --> [QA][Quick Quote] You received an offer

		 PROD
		 1) Supplier Account EPAM ->  easyordertest+TESTCOE04QQ -> [Quick Quote] You received an offer
		 after change request/supp;ier updates offer for the initial request
		 2) Supplier Account EPAM ->  easyordertest+TESTCOE04QQ -> [Quick Quote] You received an offer
*/

		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestToOfferTransactionName = "PW_Auto_RequestToOffer_chromium_202431290";
		//requestToOfferId = "102424135";
		//testStartSecondsSinceEpoch = "1710233374";  

		//////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("instantiate gmail api service");
		try
		{
			UserCredential credential;
			// Load client secrets.
			using (var stream =
						 new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
			{
				/* The file token.json stores the user's access and refresh tokens, and is created
				 automatically when the authorization flow completes for the first time. */
				string credPath = "token.json";
				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
						GoogleClientSecrets.FromStream(stream).Secrets,
						Scopes,
						"user",
						CancellationToken.None,
						new FileDataStore(credPath, true)).Result;
				Console.WriteLine("Credential file saved to: " + credPath);
			}

			System.Threading.Thread.Sleep(2000);

			// Create Gmail API service.
			bool connected = false;
			GmailService? service = null;
			IList<Message> messages = new List<Message>();
			int connectionAttempt = 0;
			while (!connected && connectionAttempt < 10)
			{
				Console.WriteLine("create gmail service, attempt: " + (connectionAttempt + 1).ToString());
				try
				{
					if (service == null)
					{
						service = new GmailService(new BaseClientService.Initializer
						{
							HttpClientInitializer = credential,
							ApplicationName = ApplicationName
						});
					}
					UsersResource.MessagesResource.ListRequest requestMessage = service.Users.Messages.List("easyordertest@gmail.com");
					requestMessage.LabelIds = "INBOX";
					requestMessage.IncludeSpamTrash = false;
					//note the message templates have a slightly different wording around  the request in the email body, e.g.
					//Change Request has Request-number:
					//Offer received and new request have Request ID:
					//Request rejected template has Request-id:
					requestMessage.Q = $"after:{testStartSecondsSinceEpoch} subject:({TC_OFFER_RECEIVED_EMAIL_SUBJECT})  AND \"Request ID: {requestToOfferId}\"";
					Console.WriteLine("Q. = " + requestMessage.Q);
					Console.WriteLine("expecting 2 offer received email message for " + requestToOfferTransactionName + " ID: " + requestToOfferId);
					messages = requestMessage.Execute().Messages;
					connected = true;
				}
				catch (Exception ex)
				{
					Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
					if (service == null)
					{
						Console.WriteLine("service is null");
					}

					if (credential == null)
					{
						Console.WriteLine("credentials are null");
					}
					Console.WriteLine("gmail exception: " + ex.Message);
					if (ex.InnerException != null)
					{
						Console.WriteLine(ex.InnerException.Message);
					}
					connectionAttempt++;
					Console.WriteLine("gmail service instantiation attempt: " + connectionAttempt.ToString());
				}
			}
			if (messages == null || messages.Count == 0)
			{
				Console.WriteLine("No offer email received message found for request " + requestToOfferId);
				if (messages == null)
				{
					Console.WriteLine("messages null");
					Assert.That(messages != null);
				}
				else
				{
					Console.WriteLine("messages count  == 0");
					Assert.That(messages.Count != 0);
				}
			}
			else
			{
				Console.WriteLine("messages count" + messages.Count.ToString());
				if (Environment == "QA")
				{
					//expected 2 emails
					Console.WriteLine("expecting 2 offer received email message for " + requestToOfferTransactionName + " ID: " + requestToOfferId);
					Console.WriteLine("found " + messages.Count.ToString() + " email message(s)");
					Assert.That(messages.Count == 2);
				}

				if (Environment == "PROD")
				{
					//expected 2 emails
					/*
					 You have received an offer for the following request:
						Request ID: 102423660
						Transaction ID: PW_Auto_RequestToOffer_chromium_20243111520
						Creation date: 3/11/2024
						Expiration: 3/18/2024
						This offer is from:
						Supplier: TESTSUPCDO4
						Sender: Supplier Account EPAM
						Reference: Offer_2024311
					*/
					Console.WriteLine("expecting 2 offer received email message for " + requestToOfferTransactionName + " ID: " + requestToOfferId);
					Console.WriteLine("found " + messages.Count.ToString() + " email message(s)");
					Assert.That(messages.Count == 2);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null)
			{
				Console.WriteLine(ex.InnerException.Message);
			}
			throw ex;
		}
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}


	[Test, Order(19)]

	[Category("EmailTests")]
	public void TC19_QQS_Change_Request_Email_For_Supplier()
	{
		//based on devops test plan id 181459 : Change request email for supplier
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
		/*
		 Check email easyordertest@gmail.com
		Email with title 
		""
		are received by 
		EasyOrderTest+SupplierTS4@gmail.com
		OmniContent+EPM_TS5@gmail.com
		*/
		Console.WriteLine("19: TC19_QQS_Change_Request_Email_For_Supplier");
		/*
	 Check email easyordertest@gmail.com
		 
		QA  2 or 3  expected emails test how many suppliers
		1) fmk b -> fmks qq -> [QA] [Quick Quote] You've received request for change! .email-template-customer/fmkb
		2) fmk b -> EasyOrderTest+SVS1user -> -> [QA] [Quick Quote] You've received request for change! .email-template-customer/fmkb
		**************************************************
		but only 1 email sent and it is to
		omnicontent+fmkscorrespondence@gmail.com
		**************************************************
		
		PROD
		1) HUBWOO Service Account -> EasyOrderTest+SupplierTS4 -> [Quick Quote] You've received request for change!
*/
		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestToOfferTransactionName = "PW_Auto_RequestToOffer_chromium_20242221840";
		//requestToRejectTransactionName = "PW_Auto_RequestToReject_chromium_20242221840";
		//requestToOfferId = "";

		//requestToRejectId = "1710155096";

		//testStartSecondsSinceEpoch = "1709650575";  //after: 1709650575  Tuesday, March 5, 2024 2:56:15 PM
		//////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("instantiate gmail api service");
		try
		{
			UserCredential credential;
			// Load client secrets.
			using (var stream =
						 new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
			{
				/* The file token.json stores the user's access and refresh tokens, and is created
				 automatically when the authorization flow completes for the first time. */
				string credPath = "token.json";
				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
						GoogleClientSecrets.FromStream(stream).Secrets,
						Scopes,
						"user",
						CancellationToken.None,
						new FileDataStore(credPath, true)).Result;
				Console.WriteLine("Credential file saved to: " + credPath);
			}

			System.Threading.Thread.Sleep(2000);

			// Create Gmail API service.
			GmailService? service = null;
			IList<Message> messages = new List<Message>();
			bool connected = false;
			int connectionAttempt = 0;
			while (!connected && connectionAttempt < 10)
			{
				Console.WriteLine("create gmail service, attempt: " + (connectionAttempt + 1).ToString());
				try
				{
					if (service == null)
					{
						service = new GmailService(new BaseClientService.Initializer
						{
							HttpClientInitializer = credential,
							ApplicationName = ApplicationName
						});
					}

					UsersResource.MessagesResource.ListRequest requestMessage = service.Users.Messages.List("easyordertest@gmail.com");
					requestMessage.LabelIds = "INBOX";
					requestMessage.IncludeSpamTrash = false;
					//note the message templates have a slightly different wording around  the request, e.g.
					//Change Request has Request-number:
					//Offer received and new request have Request ID:
					//Request rejected template has Request-id:
					requestMessage.Q = $"after:{testStartSecondsSinceEpoch} subject:({TC_REQUEST_FOR_CHANGE_EMAIL_SUBJECT})  AND \"Request-number: {requestToOfferId}\"";
					Console.WriteLine("Q. = " + requestMessage.Q);
					messages = requestMessage.Execute().Messages;
					connected = true;
				}
				catch (Exception ex)
				{
					Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
					if (service == null)
					{
						Console.WriteLine("service is null");
					}

					if (credential == null)
					{
						Console.WriteLine("credentials are null");
					}
					Console.WriteLine("gmail exception: " + ex.Message);
					if (ex.InnerException != null)
					{
						Console.WriteLine(ex.InnerException.Message);
					}
					connectionAttempt++;
					Console.WriteLine("gmail service instantiation attempt: " + connectionAttempt.ToString());
				}
			}

			if (messages == null || messages.Count == 0)
			{
				Console.WriteLine("No request for change message found for request" + requestToOfferId);
				
				if (messages == null)
				{
					Console.WriteLine("messages null");
					Assert.That(messages != null);
				}
				else
				{
					Console.WriteLine("messages count  == 0");
					Assert.That(messages.Count != 0);
				}
			}
			else
			{
				if (Environment == "QA")
				{
					//expected 1 message on qa

					Console.WriteLine("expecting 1 request for change email for request " + requestToOfferTransactionName + " ID: " + requestToOfferId);
					Console.WriteLine("found " + messages.Count.ToString() + " email message(s)");
					Assert.That(messages.Count == 1);
				}

				if (Environment == "PROD")
				{
					/*example email 
					example query:  after:1710169766 subject:([Quick Quote] You've received request for change!)  AND "Request-number: 102423660"
					[Quick Quote] You've received request for change!

					You have sent an offer via Quick Quote
					Request-number: 102423660
					Reference: Offer_2024311
					This offer was not accepted and a request for change has been sent to you.
					You can access this request via https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx
					 */
					//expected 1
					Console.WriteLine("expecting 1 request for change email for request " + requestToOfferTransactionName + " ID: " + requestToOfferId);
					Console.WriteLine("found " + messages.Count.ToString() + " email message(s)");
					Assert.That(messages.Count == 1);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null)
			{
				Console.WriteLine(ex.InnerException.Message);
			}
			throw ex;
		}
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(20)]

	[Category("EmailTests")]
	public void TC20_QQB_New_Offer_Email_For_Buyer_Change_Requested()
	{
		//based on devops test plan id 181472 : New offer for buyer (change requested)
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
		/*
		 Check email easyordertest@gmail.com
		Email with title [Quick Quote] You received an offer is received by 
		easyordertest+TESTCOE04QQ@gmail.com
		*/
		Console.WriteLine("20: TC20_QQB_New_Offer_Email_For_Buyer_Change_Requested");
		Console.Write("total email count for request id's" + requestToRejectId.ToString() + " And " + requestToOfferId.ToString());
		//duplicate of other test, can we disntinguish between the 2 emails before and after rfc?

		//could check all emails via subject: [QA][Quick Quote] AND (100102755 OR 100102754)  messages be 10
		try
		{
			UserCredential credential;
			// Load client secrets.
			using (var stream =
						 new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
			{
				/* The file token.json stores the user's access and refresh tokens, and is created
				 automatically when the authorization flow completes for the first time. */
				string credPath = "token.json";
				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
						GoogleClientSecrets.FromStream(stream).Secrets,
						Scopes,
						"user",
						CancellationToken.None,
						new FileDataStore(credPath, true)).Result;
				Console.WriteLine("Credential file saved to: " + credPath);
			}

			System.Threading.Thread.Sleep(4000);

			String QQ_ALL_EMAILS_FOR_TESTS_SUBJECT = "[QA][Quick Quote]";
			if (Environment == "PROD")
			{
				QQ_ALL_EMAILS_FOR_TESTS_SUBJECT = "[Quick Quote]";
				//example query after:1710169766 subject:([Quick Quote])  AND ("102423657" OR "102423660")
				Console.WriteLine("expecting 6 total email messages for requests");
				/*
				 Supplier Account EPAM ->   easyordertest+TESTCOE04QQ [Quick Quote] You received an offer
				  
				HUBWOO Service Account -> EasyOrderTest+SupplierTS4 [Quick Quote] You've received request for change!
				
				Supplier Account EPAM  -> easyordertest+TESTCOE04QQ [Quick Quote] You received an offer

				Supplier Account EPAM -> easyordertest+TESTCOE04QQ  [Quick Quote] Request has been rejected!

				HUBWOO Service Account -> EasyOrderTest+SupplierTS4 [Quick Quote] You have received a new Quick Quote request GENERIC

				HUBWOO Service Account -> EasyOrderTest+SupplierTS4 [Quick Quote] You have received a new Quick Quote request GENERIC

				*/
			}


			if (Environment == "QA")
			{
				Console.WriteLine("expecting 10 total email messages for requests");
				/*
				 *   
				 * example query after:1710158766 subject:([QA][Quick Quote])  AND ("100102779" OR "100102780")
					fmks qq -> EasyOrderTest+fmkbqq [QA] [Quick Quote] You received an offer

					fmk b -> EasyOrderTest+fmksqq [QA] [Quick Quote] You've received request for change! .email-template-customer/fmkb
					fmks qq -> EasyOrderTest+fmkbqq [QA] [Quick Quote] You received an offer

					fmks qq -> EasyOrderTest+fmkbqq [QA] [Quick Quote] Request has been rejected!

					fmk b -> EasyOrderTest+SVS1user [QA] [Quick Quote] You have received a new Quick Quote request

					fmk b -> EasyOrderTest+fmksqq [QA] [Quick Quote] You have received a new Quick Quote request

					fmk b -> EasyOrderTest+SV5 [QA] [Quick Quote] You have received a new Quick Quote request

					fmk b -> EasyOrderTest+SVS1user [QA] [Quick Quote] You have received a new Quick Quote request

					fmk b -> EasyOrderTest+SV5 [QA] [Quick Quote] You have received a new Quick Quote request

				  fmk b -> EasyOrderTest+fmksqq [QA] [Quick Quote] You have received a new Quick Quote request

				*/
			}

			// Create Gmail API service.
			bool connected = false;
			GmailService? service = null;
			IList<Message> messages = new List<Message>();
			int connectionAttempt = 0;
			while (!connected && connectionAttempt < 10)
			{
				Console.WriteLine("create gmail service, attempt: " + (connectionAttempt + 1).ToString());
				try
				{
					if (service == null)
					{
						service = new GmailService(new BaseClientService.Initializer
						{
							HttpClientInitializer = credential,
							ApplicationName = ApplicationName
						});
					}
					UsersResource.MessagesResource.ListRequest requestMessage = service.Users.Messages.List("easyordertest@gmail.com");
					requestMessage.LabelIds = "INBOX";
					requestMessage.IncludeSpamTrash = false;
					requestMessage.Q = $"after:{testStartSecondsSinceEpoch} subject:({QQ_ALL_EMAILS_FOR_TESTS_SUBJECT})  AND (\"{requestToRejectId}\" OR \"{requestToOfferId}\")";
					Console.WriteLine("Q. = " + requestMessage.Q);
					messages = requestMessage.Execute().Messages;
					connected = true;
				}
				catch (Exception ex)
				{
					Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
					Console.WriteLine("gmail exception: " + ex.Message);
					if (ex.InnerException != null)
					{
						Console.WriteLine(ex.InnerException.Message);
					}
					connectionAttempt++;
					Console.WriteLine("gmail service instantiation exception: " + connectionAttempt.ToString());
				}
			}
			if (messages == null || messages.Count == 0)
			{
				Console.WriteLine("No  email message found");
				if (messages == null)
				{
					Console.WriteLine("messages null");
					Assert.That(messages != null);
				}
				else
				{
					Console.WriteLine("messages count  == 0");
					Assert.That(messages.Count > 0);
				}
			}
			else
			{
				if (Environment == "QA")
				{
					//expected 10 messages on qa

					Console.WriteLine("expecting 10 total email messages for requests");
					Console.WriteLine("found " + messages.Count.ToString() + " email message(s)");
					Assert.That(messages.Count == 10);
				}

				if (Environment == "PROD")
				{
					//expected 6 messages on prod
					Console.WriteLine("expecting x offer received email message for " + requestToOfferTransactionName + " ID: " + requestToOfferId);
					Console.WriteLine("found " + messages.Count.ToString() + " email message(s)");
					Assert.That(messages.Count == 6);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null)
			{
				Console.WriteLine(ex.InnerException.Message);
			}
			throw ex;
		}
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}


	[Test, Order(21)]
	[Category("QQTests1")]
	async public Task TC21_QQB_Buyer_Create_Request_Position_Form()
	{

		//ASSUME that BUYER_CHOOSES_CLASSIFICATION is enabled for the buyer company!!
		//and that FORMS_POSITION_MANDATORY is false

		//test based on devops test caseid 181480:Buyer create request (position form)
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
		/*
		Click create request
		Select "PROD Tests"
		Select "With Form - same class system"
		Fill first position with details:
		Title : Position 1
		Description : Position 1 details
		Quantity : 10
		Fill second position with details:
		TItle : Position 2
		Description : Position 2 details
		Quantity 20
		Click the drop down list below "Edit form data"
		Select "TestForm1"
		Click the show form button next to drop down list
		Click the form button in popup
		Fill text field 1 with "Position form mandatory text"
		Check "Check box 1"
		Select "Radio option 2"
		Click save
		*/

		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestPositionFormTransactionName = "PW_Auto_RequestPositionPFchromium_2024351222";

		//////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("21: TC20_QQB_Buyer_Create_Request_Position_Form");
		string url = SEARCHURL;
		List<string> propertiesToSetTrue = new List<string>();
		List<string> propertiesToSetFalse = new List<string>();
		propertiesToSetTrue.Add("BUYER_CHOOSES_CLASSIFICATION");
		propertiesToSetFalse.Add("FORMS_POSITION_MANDATORY");
		await QQB_ConfigureEasyOrderPropertiesForCompany(propertiesToSetTrue, propertiesToSetFalse, TC02_COMPANYID);//set BUYER_CHOOSES_CLASSIFICATION = true

		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 180000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
		LocatorWaitForOptions locatorWaitForOption = new LocatorWaitForOptions { Timeout = 180000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };
		//create requestPositionFormTransactionName

		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };

		Console.WriteLine("login to search:" + DateTime.Now.ToLongTimeString());
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loggedin = true;
			}
			catch (Exception ex)
			{
				attempts++;
				Console.WriteLine("exception: " + ex.Message);
				Console.WriteLine(DateTime.Now.ToString());
				Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
				//seeing a lot of errors of type
				/*
						Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
				*/
			}
		}
		if (loggedin == false && attempts >= 10)
		{
			DateTime timeRightNow = DateTime.Now;
			string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC21_LoginError_" + FileCurrentDate + ".png"
			});
		}

		Console.WriteLine("page: " + Page.Url);

		//assert search has a quick quote link for this view/company
		await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

		// Wait for the iframe to be available in the DOM and visible
		await Page.WaitForSelectorAsync("iframe", new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached });
		var iframeElement = await Page.Locator("#qqFrame").ElementHandleAsync();
		var frame = await iframeElement.ContentFrameAsync();
		if (frame != null && frame.Url != "")
		{
			Console.WriteLine("qqFrame Url : " + frame.Url);
			//await frame.WaitForURLAsync("https://portal.hubwoo.com/srvs/easyorder/RequestList2007.aspx");
		}

		//this is extremely slow in qa
		if (Environment == "QA")
		{
			await Task.Delay(3000);
		}
		Console.WriteLine("wait for request list page");

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		Console.WriteLine("click Create a request");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbCreateRequestTop").ClickAsync(locatorClickOptions);

		///
		//how to perform WaitForURLAsync in iframe
		iframeElement = await Page.Locator("#qqFrame").ElementHandleAsync();
		frame = await iframeElement.ContentFrameAsync();
		if (frame != null && frame.Url != "")
		{
			Console.WriteLine("qqFrame Url : " + frame.Url);
			//await frame.WaitForURLAsync("https://portal.hubwoo.com/srvs/easyorder/DataSheetChoose.aspx");
		}

		if (Environment == "QA")
		{
			await Task.Delay(3000);
		}

		//assert on datasheetchoose page
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Product Group", locatorToContainTextOption);

		//expand product group QA Tests (qa)  / PROD Tests (Prod) use the unique id on the image as a locator
		Console.WriteLine("expand product group");
		await Page.FrameLocator("#qqFrame").Locator(TC02_PRODUCT_GROUP).ClickAsync(locatorClickOptions);

		//select With Form - same class system

		Console.WriteLine("select With Form - same class system datasheet");
		await Page.FrameLocator("#qqFrame").Locator(TC21_FORM_DATASHEET1_SELECTOR).ClickAsync(locatorClickOptions);

		//click choose
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Choose" }).ClickAsync(locatorClickOptions);

		//transactionid
		DateTime today = DateTime.Now;
		string CurrentDate = $"{today.Year}{today.Month}{today.Day}{today.Hour}{today.Minute}";
		string transactionID = "PW_Auto_RequestPositionPF" + _browserName + "_" + CurrentDate;//this transactionid will be used in other tests in this suite!
		string commentDate = $"{today.Year}{today.Month}{today.Day}";

		requestPositionFormTransactionName = transactionID;//allows this request to be opened in another test and be referenced during teardown

		Console.WriteLine("wait for request details page to load...");
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request Details")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert that the selected product group is simple datasheet
		//await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbProductGroup")).ToContainTextAsync("Simple datasheet");
		//wont work there is no innertext ,the data is stored in the value attribute of the control
		var readonlyInput = Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbProductGroup");

		await Expect(readonlyInput).ToBeDisabledAsync();

		var selectedDataSheet = await readonlyInput.GetAttributeAsync("value");

		Assert.That(selectedDataSheet == "With Form - same class system");

		//assert transactionid is empty
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbTransactionNumber")).ToBeEmptyAsync();

		//assert that no supplier selected
		await Expect(Page.FrameLocator("#qqFrame").Locator("#selectSelectedSuppliers")).ToBeEmptyAsync();

		//assert that available suppliers contains 2 suppliers
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repExistingSuppliers_ctl00_Option1")).ToContainTextAsync(TC02_ASSERT_SUPPLIER1);
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repExistingSuppliers_ctl01_Option1")).ToContainTextAsync(TC02_ASSERT_SUPPLIER2);
		//assert there are 2 default empty request positions
		//5 default empty positions in qa/ 2 empty default positions in prod
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblPositionCount")).ToContainTextAsync(TC02_EMPTY_REQUEST_POSITIONS);

		//assert classification popup icon present in request positions i.e. that the BUYER_CHOOSES_CLASSIFICATION setting is enabled and honoured by the UI
		//ctl00_MainContent_repRequestPosition_ctl01_ibShowClassification
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_ibShowClassification")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert textbox present for classification code
		//ctl00_MainContent_repRequestPosition_ctl01_tbEclassCode
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbEclassCode")).ToBeVisibleAsync(locatorVisibleAssertion);

		//add transaction number 
		Console.WriteLine("complete request details");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbTransactionNumber").FillAsync(transactionID);


		//if qa delete 3 positions
		if (Environment == "QA")
		{
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_ibRemovePositionEdit").ClickAsync();
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_lbRemovePositionEdit").ClickAsync();
			await Task.Delay(2000);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_ibRemovePositionEdit").ClickAsync();
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_lbRemovePositionEdit").ClickAsync();
			await Task.Delay(2000);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_ibRemovePositionEdit").ClickAsync();
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_lbRemovePositionEdit").ClickAsync();

		}

		await Task.Delay(3000);// the steps below down to classification selection are not actioned, if this pause is removed?

		//select both suppliers
		//add both suppliers
		await Page.FrameLocator("#qqFrame").Locator("#selectExistingSuppliers").SelectOptionAsync(new[] { TC02_SELECT_SUPPLIER1_ID });
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync();
		await Page.FrameLocator("#qqFrame").Locator("#selectExistingSuppliers").SelectOptionAsync(new[] { TC02_SELECT_SUPPLIER2_ID });
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync(locatorClickOptions);


		//if qa select shipping address
		if (Environment == "QA")
		{
			//need to select a shipping address
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlLocationShippingAddress").SelectOptionAsync(new[] { "1368" });//westgate ripon
			 //note Michelin local code is not mandatory
		}
		/////////////////////////////////////////////  POSITION 1

		//complete position 1
		Console.WriteLine("add short description pos1");


		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbShortDescription").FillAsync("Position 1");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbLongDescription").FillAsync("Position 1 details");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbQuantity").FillAsync("10");

		Console.WriteLine("add classification pos1");
		await Task.Delay(3000);

		var Page1 = await Page.RunAndWaitForPopupAsync(async () =>
		{
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_ibShowClassification").ClickAsync(locatorClickOptions);
		}, new PageRunAndWaitForPopupOptions { Timeout = 180000 });

		await Page.WaitForTimeoutAsync(3000);

		await Expect(Page1.Locator("#tbSearchField")).ToBeVisibleAsync();
		await Page1.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL1 }).ClickAsync(locatorClickOptions);
		await Page1.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL2 }).ClickAsync(locatorClickOptions);
		await Page1.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL3 }).ClickAsync(locatorClickOptions);
		await Page1.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL4 }).ClickAsync(locatorClickOptions);

		await Page.WaitForTimeoutAsync(3000);

		//save pos 1
		Console.WriteLine("save pos1");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_ibSaveRequestPosition").ClickAsync(locatorClickOptions);

		await Task.Delay(3000);

		/////////////////////////////////////////////  POSITION 2
		//will fail here if FORMS_POSITION_MANDATORY is true!!!!

		//complete position 2
		Console.WriteLine("add short description pos2");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbShortDescription").FillAsync("Position 2");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbLongDescription").FillAsync("Position 2 details");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbQuantity").FillAsync("20");

		Console.WriteLine("add classification pos2");
		var Page9 = await Page.RunAndWaitForPopupAsync(async () =>
		{
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_ibShowClassification").ClickAsync(locatorClickOptions);
		}, new PageRunAndWaitForPopupOptions { Timeout = 180000 });

		await Page.WaitForTimeoutAsync(3000);

		await Expect(Page9.Locator("#tbSearchField")).ToBeVisibleAsync();
		await Page9.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL1 }).ClickAsync(locatorClickOptions);
		await Page9.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL2 }).ClickAsync(locatorClickOptions);
		await Page9.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL3 }).ClickAsync(locatorClickOptions);
		await Page9.GetByRole(AriaRole.Link, new() { Name = TC02_CLASS_CODE_LEVEL4 }).ClickAsync(locatorClickOptions);

		await Page.WaitForTimeoutAsync(3000);

		////////////////////////////////////////////////////

		//assert that testform1 is only option in form drop down at pos 2
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_ddlPositionForms")).ToContainTextAsync("TestForm1");

		//select form testfrom1
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_ddlPositionForms").SelectOptionAsync(new[] { TC21_EDIT_FORM1_OPTION });//testform1

		await Task.Delay(3000);

		//click the edit form button  ctl00_MainContent_repRequestPosition_ctl02_ibShowForm for pos 2 
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_ibShowForm").ClickAsync(locatorClickOptions);

		await Task.Delay(3000);

		//click the form option on the confirmation popup
		//ctl00_MainContent_repRequestPosition_ctl02_lbShowForm
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_lbShowForm").ClickAsync(locatorClickOptions);

		await Task.Delay(3000);

		//assert on form editing page
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Form");

		//assert that the form labels are displayed
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repGroups_ctl00_repAttributes_ctl00_lblMandatory")).ToBeVisibleAsync(locatorVisibleAssertion);    //text field 1
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repGroups_ctl00_repAttributes_ctl01_lblAttributeLabel")).ToBeVisibleAsync(locatorVisibleAssertion);   //checkbox 1 is visible
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repGroups_ctl00_repAttributes_ctl02_lblAttributeLabel")).ToBeVisibleAsync(locatorVisibleAssertion);   //dropdown 1
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repGroups_ctl00_repAttributes_ctl03_lblAttributeLabel")).ToBeVisibleAsync(locatorVisibleAssertion);   //memo area 1 label
		//

		//complete form
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repGroups_ctl00_repAttributes_ctl00_tbTextBoxValue").FillAsync("Position form mandatory text");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repGroups_ctl00_repAttributes_ctl01_cbCheckBoxValue").CheckAsync();
		//not rendering any radio buttons?
		await Page.FrameLocator("#qqFrame").GetByLabel("Radio option 2").CheckAsync();

		//assert form save buton is visible
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSave")).ToBeVisibleAsync(locatorVisibleAssertion);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSave").ClickAsync(locatorClickOptions);

		//assert back on request details page
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToBeVisibleAsync(locatorVisibleAssertion);
		//assert
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Request Details");

		//save pos 2 ctl00_MainContent_repRequestPosition_ctl02_ibSaveRequestPosition
		Console.WriteLine("save pos2");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_ibSaveRequestPosition").ClickAsync(locatorClickOptions);


		//send
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_hrefPublishTop").ClickAsync(locatorClickOptions);

		//assert cover message
		await Expect(Page.FrameLocator("#qqFrame").Locator("#divPublishTextInvitedTop")).ToContainTextAsync(TC02_ASSERT_SUPPLIER1);
		await Expect(Page.FrameLocator("#qqFrame").Locator("#divPublishTextInvitedTop")).ToContainTextAsync(TC02_ASSERT_SUPPLIER2);

		await Task.Delay(5000);

		//send button is displayed
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_hrefPublishTop")).ToBeVisibleAsync(locatorVisibleAssertion);

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbPublishButtonTop").ClickAsync(locatorClickOptions);
		iframeElement = await Page.Locator("#qqFrame").ElementHandleAsync();
		frame = await iframeElement.ContentFrameAsync();
		if (frame != null && frame.Url != "")
		{
			Console.WriteLine("qqFrame Url : " + frame.Url);
			//await frame.WaitForURLAsync("https://portal.hubwoo.com/srvs/easyorder/RequestList2007.aspx");
		}
		//assert back on request list
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOption);
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Request list");

		//search for request just sent
		Console.WriteLine("Search For " + requestPositionFormTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestPositionFormTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);

		//assert only 1 result
		try
		{
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");
		}
		catch
		{
			Console.WriteLine("More than one search result for request..." + requestPositionFormTransactionName);
		}
		//assert status is now requested
		Console.WriteLine("assert status is now requested for " + requestPositionFormTransactionName);
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblStatus")).ToContainTextAsync("Requested");


		//get the requestid and update it for use in email tests
		var newRequestId = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblRequestID").TextContentAsync();
		requestIdPositionForm = newRequestId;
		Console.WriteLine("test finished");
	}

	[Test, Order(22)]
	[Category("QQTests1")]
	async public Task TC22_QQB_Buyer_Downloads_Request_Position_Form()
	{
		//test based on devops test caseid 181489 :Buyer download request (position form)
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
		/*
		Click download on request PW_Auto_RequestPositionPFchromium_yyyymmdd
		Click the link below Download the request as PDF file
		Click the link below Download the request as excel file
		*/

		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestPositionFormTransactionName = "PW_Auto_RequestPositionPFchromium_202412251054";

		//////////////////////////////////////////////////////////////////////////////

		//////////////////////////////////////////////////////////////////////////////
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
		//Open "request to offer {yymmdd}"
		Console.WriteLine("22: TC22_QQB_Buyer_Downloads_Request_Position_Form");
		Console.WriteLine("request" + requestPositionFormTransactionName);
		string url = SEARCHURL;
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 180000, State = WaitForSelectorState.Visible };

		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };

		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };

		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };

		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };

		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };

		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };

		Console.WriteLine("login to search:" + DateTime.Now.ToLongTimeString());
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loggedin = true;
			}
			catch (Exception ex)
			{
				attempts++;
				Console.WriteLine("exception: " + ex.Message);
				Console.WriteLine(DateTime.Now.ToString());
				Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
				//seeing a lot of errors of type
				/*
						Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
				*/
			}
		}
		if (loggedin == false && attempts >= 10)
		{
			DateTime timeRightNow = DateTime.Now;
			string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC22_LoginError_" + FileCurrentDate + ".png"
			});
		}

		//assert search has a quick quote link for this view/company
		await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

		//assert on request list page
		Console.WriteLine("wait for request list page");

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//get current request count
		var currentrequestCount = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount").TextContentAsync();

		//search for the request position form request

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestPositionFormTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//wait for results
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert 1 result
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");

		await Task.Delay(3000);
		//wait for search results and check status of the first result
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToBeVisibleAsync(locatorVisibleAssertion);

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_ibDownloadArea").ClickAsync(locatorClickOptions);

		//await Page.PauseAsync();//causes the playwright test inspector to launch even when not debugging and cause pause of the run

		await Task.Delay(3000);

		var waitForDownloadTask = Page.WaitForDownloadAsync();
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Download the request as PDF File" }).First.ClickAsync(new()
		{
			//modifier allows the save as functionality, which makes the generator save to disk rather tha be rendered in a new tab
			Modifiers = new[] { KeyboardModifier.Alt },
			Timeout = 180000
		});

		var download = await waitForDownloadTask;

		var fileName = downloadPath + "TC22_" + requestPositionFormTransactionName + download.SuggestedFilename;

		await Task.Delay(3000);
		
		// Wait for the download process to complete and save the downloaded file somewhere
		await download.SaveAsAsync(fileName);

		if (Environment == "QA")
		{
			await Task.Delay(3000);
		}

		if (Environment == "PROD")
		{
			await Task.Delay(6000);
		}

		//load pdf in another tab and screenshot it
		var page2 = await Page.RunAndWaitForPopupAsync(async () =>
		{
			//fails if more than one request file result in the request list on the page
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Download the request as PDF File" }).First.ClickAsync(locatorClickOptions);
		}, new PageRunAndWaitForPopupOptions { Timeout = 180000 });
		//await Page.PauseAsync();//causes the playwright test inspector to launch pauses run
		await Task.Delay(3000);
		//download url
		var pdfUrl = page2.Url;
		try
		{
			Console.WriteLine("pdf downloaded from " + pdfUrl);
			Console.WriteLine("asserting contents of  " + fileName);
			using (PdfDocument pdf = PdfDocument.Open(fileName))
			{
				Page page = pdf.GetPage(1);
				if (page != null)
				{
					Console.WriteLine("Asserting contents of pdf");
					Assert.That(page.Text.Contains(TC05_PDF_ASSERT_CUSTOMERNAME));
					Assert.That(page.Text.Contains(TC05_PDF_ASSERT_LOCATION));
					Assert.That(page.Text.Contains(requestPositionFormTransactionName));
					Assert.That(page.Text.Contains("Position 1"));
					Assert.That(page.Text.Contains("Position 1 details"));
					Assert.That(page.Text.Contains("With Form - same class system"));
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
			Console.WriteLine("Issue asserting contents of pdf document " + pdfUrl);
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
		}

		//screenshots are blank when running this pause fixes it
		await Task.Delay(3000);

		await page2.ScreenshotAsync(new()
		{
			FullPage = true,
			Path = downloadPath + "TC22_" + requestPositionFormTransactionName + "_QQB_BuyerDownloadExcelAndPdfRequest_pdf.png"
		});

		//await Page.PauseAsync();//causes the playwright test inspector to launch even when not debugging and cause pause of the run

		//download of an excel file is weird in chromium as it is running in incognito mode, you are shown a popup with a guid file name but no file is available when you open folder?
		try
		{
			//download excel
			///////////////////////////////////////////////////////////////////////////////////////////////////////////
			var waitForExcelDownloadTask = Page.WaitForDownloadAsync();
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Download the request as Excel File" }).First.ClickAsync(new()
			{
				//modifier allows the save as functionality, which makes the generator save to disk rather than be rendered in a new tab
				Modifiers = new[] { KeyboardModifier.Alt },
				Timeout = 180000
			});

			var excelDownload = await waitForExcelDownloadTask;

			// Wait for the download process to complete and save the downloaded file somewhere
			await excelDownload.SaveAsAsync(downloadPath + "TC22_" + requestPositionFormTransactionName + excelDownload.SuggestedFilename);
		}
		catch (Exception ex)
		{
			Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
			Console.WriteLine("exception TC22_QQB_BuyerDownloadExcelAndPdfRequest");
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
		}

	}

	[Test, Order(23)]
	[Category("QQTests1")]
	async public Task TC23_QQS_Supplier_Downloads_Request_Position_Form()
	{
		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestPositionFormTransactionName = "PW_Auto_RequestPositionPFchromium_202412251054";

		//////////////////////////////////////////////////////////////////////////////
		///
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
		//based on devops test case id 181491 : Supplier download request (position form)
		Console.WriteLine("23: TC23_QQS_Supplier_Downloads_Request_Position_Form");

		/*
		 * Click the download button of "Pos_form_{yymmdd}"
		 * Only PDF version of the request is available
		 * Click link "Pos_form_{yymmdd}"
		 * PDF is opened in new browser tab
			Request info and position details are correct
		*/
		Console.WriteLine("request " + requestPositionFormTransactionName);
		string url = PORTAL_LOGIN;
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };

		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 15)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				await Page.WaitForLoadStateAsync(LoadState.Load);
				await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
				await Page.GetByPlaceholder("Enter your user name").FillAsync(SUPPLIER_USER1_LOGIN);
				await Page.GetByPlaceholder("Enter your password").FillAsync(SUPPLIER_USER1_PASSWORD);
				await Page.Locator("#signInButtonId").IsEnabledAsync();
				await Page.Locator("#signInButtonId").ClickAsync(locatorClickOptions);
				loggedin = true;
			}
			catch (Exception e)
			{
				attempts++;
				Console.WriteLine(DateTime.Now.ToString());
				Console.WriteLine("Problems logging in, attempt:" + attempts.ToString());
				//seeing a lot of errors of type
				/*
					Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F
					Call log:- navigating to "https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F", waiting until "load"
					*/
				Console.WriteLine(e.Message);
			}
		}
		Console.WriteLine("Page: " + Page.Url);

		//wait for page to load https://portal.hubwoo.com/main/
		//await Page.WaitForURLAsync("https://portal.hubwoo.com/main/", pageWaitForUrlOptions);  //this fails for some reason

		await Page.WaitForSelectorAsync("//side-bar-item-group[@name='Opportunities']", new PageWaitForSelectorOptions { Timeout = 180000 });
		Console.WriteLine("Page: " + Page.Url);
		var isCookieConsentVisible = await Page.Locator("#cookie-consent-dialog").IsVisibleAsync();
		if (isCookieConsentVisible)
		{
			await Page.GetByRole(AriaRole.Button, new() { Name = "Accept all" }).ClickAsync();
		}

		await Page.Locator("//side-bar-item-group[@name='Opportunities']").ClickAsync(locatorClickOptions);
		await Page.Locator("//side-bar-item[@name='Requests']").ClickAsync(locatorClickOptions);

		//wait for page to load
		await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

		//search by transaction name
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync(locatorClickOptions);
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestPositionFormTransactionName);
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await Task.Delay(3000);

		//wait for results  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData
		await Page.WaitForSelectorAsync("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData");

		//waitfor url https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx
		await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

		//Assert 1 result
		await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager")).ToContainTextAsync("(1 items found)");

		//click the download pdf option
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibDownloadArea").ClickAsync(locatorClickOptions);
		await Task.Delay(3000);
		//download pdf and assert contents
		var waitForDownloadTask = Page.WaitForDownloadAsync();
		//find the download link via the text
		await Page.GetByRole(AriaRole.Link, new() { Name = "Download the request as PDF File" }).First.ClickAsync(new()
		{
			//modifier allows the save as functionality, which makes the generator save to disk rather tha be rendered in a new tab
			Modifiers = new[] { KeyboardModifier.Alt },
			Timeout = 180000
		});

		var download = await waitForDownloadTask;

		var fileName = downloadPath + "TC23_" + requestPositionFormTransactionName + download.SuggestedFilename;

		// Wait for the download process to complete and save the downloaded file somewhere
		await download.SaveAsAsync(fileName);

		//click the pdf option
		var page1 = await Page.RunAndWaitForPopupAsync(async () =>
		{
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_hlRequestPdf").ClickAsync();
		}, new PageRunAndWaitForPopupOptions { Timeout = 180000 });
		//take screenshot
		//screenshot  pdf tab requestToRejectTransactionName.pdf
		await page1.ScreenshotAsync(new()
		{
			FullPage = true,
			Path = downloadPath + "TC23_" + requestPositionFormTransactionName + "_QQS_SupplierDownloadRequestPdf_pdf.png"
		});

		//assert the contents of the pdf
		var pdfUrl = page1.Url;
		try
		{
			Console.WriteLine("pdf downloaded from " + pdfUrl);
			Console.WriteLine("asserting contents of  " + fileName);
			using (PdfDocument pdf = PdfDocument.Open(fileName))
			{
				Page page = pdf.GetPage(1);
				if (page != null)
				{
					Console.WriteLine("Asserting contents of pdf");
					Assert.That(page.Text.Contains(TC05_PDF_ASSERT_CUSTOMERNAME));
					Assert.That(page.Text.Contains(TC05_PDF_ASSERT_LOCATION));
					Assert.That(page.Text.Contains(requestPositionFormTransactionName));
					Assert.That(page.Text.Contains("Position 1"));
					Assert.That(page.Text.Contains("Position 1 details"));
					Assert.That(page.Text.Contains("With Form - same class system"));
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
			Console.WriteLine("Issue asserting contents of pdf document " + pdfUrl);
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
		}
	}


	[Test, Order(24)]
	[Category("QQTests1")]
	async public Task TC24_QQS_Supplier_Creates_Offer_Position_Form()
	{
		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestPositionFormTransactionName = "PW_Auto_RequestPositionPFchromium_202412251054";

		//////////////////////////////////////////////////////////////////////////////
		/*
		  Click the open form button in position 2, right to "TestForm1"
			Click the form button	

			Fill:
			Reference : pos_form_offer_{yymmdd}
			Comment for customer : Test with position form

			Create offer for position 1
			Short description : Pos form offer 1
			Long description : Position form offer 1
			Article No : pos_offer1
			Classification : 20010101
			Delivery date : 7
			Quantity : 77
			Price per unit : 107
			Expiration date : today + 28
			Save position

			Create offer for position 2
			Short description : Pos form offer 2
			Long description : Position form offer 2
			Article No : pos_offer2
			Classification : 20010102
			Delivery date : 14
			Quantity : 154
			Price per unit : 114
			Expiration date : today + 32
			Save position
		 */
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
		//based on devops test case id 181494 :Supplier create offer (position form)
		Console.WriteLine("24: TC24_QQS_Supplier_Creates_Offer_Position_Form");
		Console.WriteLine("request " + requestPositionFormTransactionName);
		string url = PORTAL_LOGIN;
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };

		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
		LocatorSelectOptionOptions locatorSelectOptions = new LocatorSelectOptionOptions { Timeout = 180000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				await Page.WaitForLoadStateAsync(LoadState.Load);
				await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
				await Page.GetByPlaceholder("Enter your user name").FillAsync(SUPPLIER_USER1_LOGIN);
				await Page.GetByPlaceholder("Enter your password").FillAsync(SUPPLIER_USER1_PASSWORD);
				await Page.Locator("#signInButtonId").IsEnabledAsync();
				await Page.Locator("#signInButtonId").ClickAsync(locatorClickOptions);
				loggedin = true;
			}
			catch (Exception e)
			{
				attempts++;
				Console.WriteLine("Problems logging in, attempt:" + attempts.ToString());
				//seeing a lot of errors of type
				/*
					Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F
					Call log:- navigating to "https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F", waiting until "load"
					*/
				Console.WriteLine(e.Message);
			}
		}
		Console.WriteLine("Page: " + Page.Url);

		//wait for page to load https://portal.hubwoo.com/main/
		//await Page.WaitForURLAsync("https://portal.hubwoo.com/main/", pageWaitForUrlOptions);  //this fails for some reason

		await Page.WaitForSelectorAsync("//side-bar-item-group[@name='Opportunities']", new PageWaitForSelectorOptions { Timeout = 180000 });
		Console.WriteLine("Page: " + Page.Url);
		var isCookieConsentVisible = await Page.Locator("#cookie-consent-dialog").IsVisibleAsync();
		if (isCookieConsentVisible)
		{
			await Page.GetByRole(AriaRole.Button, new() { Name = "Accept all" }).ClickAsync();
		}

		await Page.Locator("//side-bar-item-group[@name='Opportunities']").ClickAsync(locatorClickOptions);
		await Page.Locator("//side-bar-item[@name='Requests']").ClickAsync(locatorClickOptions);

		//wait for page to load
		await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

		//search by transaction name
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").IsVisibleAsync();
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestPositionFormTransactionName);
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await Task.Delay(3000);
		await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

		//wait for results  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData
		await Page.WaitForSelectorAsync("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData");

		//Assert 1 result
		await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager")).ToContainTextAsync("(1 items found)");

		//waitfor url https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx
		await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

		//assert status
		await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_lblStatus")).ToContainTextAsync("Requested");

		//click edit 
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibEdit").ClickAsync(locatorClickOptions);

		//await Page.WaitForURLAsync(QQS_OFFER_DETAIL_URL, pageWaitForUrlOptions);  //wont work due to querystrng parameter
		await Expect(Page).ToHaveURLAsync(new Regex(QQS_OFFER_DETAIL_URL_REGEX));

		//wait for offer details page to load
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page.Locator("#ctl00_MainContent_offerActionBarTop_lblActions").IsVisibleAsync();
		await Page.Locator("#ctl00_MainContent_offerActionBarTop_lblActions").WaitForAsync(locatorWaitForOptions);

		//set expiration date
		//pick date 28 days from now
		DateTime today = DateTime.Now;
		DateTime expiryDate = today.AddDays(28);
		int expiryYear = expiryDate.Year;
		int expiryMonth = expiryDate.Month - 1;//zero index
		int expiryDay = expiryDate.Day;

		string CurrentDate = $"{today.Year}{today.Month}{today.Day}";

		//assert status is requested
		await Expect(Page.Locator("#ctl00_MainContent_lblStatus")).ToContainTextAsync("Requested");

		//add comment for customer
		Console.WriteLine("add comment for customer");
		await Page.GetByRole(AriaRole.Link, new() { Name = "Comment for customer" }).ClickAsync(locatorClickOptions);
		await Page.Locator("#ctl00_MainContent_tbExternalComment").FillAsync("Test with position form");
		Console.WriteLine("edit form on pos2");
		//open edit form on pos 2
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_ibShowForm").ClickAsync(locatorClickOptions);

		Console.WriteLine("confirm form edit for pos2");
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_lbShowFormg").ClickAsync(locatorClickOptions);
		//wait for https://portal.qa.hubwoo.com/srvs/easyorder/CustomForms.aspx?formId=309&positionId=700723 to load

		await Expect(Page).ToHaveURLAsync(new Regex(CUSTOM_FORM_EDIT_REGEX));
		

		await Expect(Page.Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Form");

		//confirm details are correct
		//test group name
		await Expect(Page.Locator("#ctl00_MainContent_repGroups_ctl00_lblGroupName")).ToContainTextAsync("First test group");

		//mandatory text field
		await Expect(Page.Locator("#ctl00_MainContent_repGroups_ctl00_repAttributes_ctl00_lblReadOnlyValue")).ToContainTextAsync("Position form mandatory text");

		//checkbox 
		await Expect(Page.Locator("#ctl00_MainContent_repGroups_ctl00_repAttributes_ctl01_pAttribute")).ToBeVisibleAsync(locatorVisibleAssertion);

		//await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = "Radio option 2", Exact = true })).ToBeCheckedAsync(); //read only may fail

		//radio button
		await Expect(Page.Locator("#ctl00_MainContent_repGroups_ctl00_repAttributes_ctl04_radRadioButtonList_1")).ToBeVisibleAsync(locatorVisibleAssertion);

		//close form edit page
		Console.WriteLine("close form edit page");
		await Page.GetByRole(AriaRole.Link, new() { Name = "Close" }).ClickAsync();


		//wait for offer detail page  https://portal.qa.hubwoo.com/srvs/easyorder/OfferDetail2007.aspx
		//note queryparameters are lost when redirected from 
		await Page.WaitForURLAsync(QQS_OFFER_DETAIL_URL, pageWaitForUrlOptions); 

		//add offer for pos 1 ///////////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("add offer pos1");
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_anchorEditCut").ClickAsync(locatorClickOptions);
		Console.WriteLine("add offer pos1 details");
		//add pos 2 info
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbShortDescription").FillAsync("Pos form offer 1");//description
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbLongDescription").FillAsync("Position form offer 1");
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbDeliveryDays").FillAsync("7"); //
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbArticleNumber").FillAsync("pos_offer1");

		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbAmount").FillAsync("77"); //quantity

		//assert classification controls are not created!!
		//classification is performed by buyer
		await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_ibShowClassification")).ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = 180000 });
		await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbEclassCode")).ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = 180000});


		await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_lblEclassCode")).ToBeVisibleAsync(locatorVisibleAssertion);

		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbPricePerUnit_BeforeComma").FillAsync("107");


		//click calendar on the position 1 
		//expiration date
		Console.WriteLine("set expiration date on offer position 1");
		await Page.GetByRole(AriaRole.Img, new() { Name = "..." }).ClickAsync(locatorClickOptions);

		await Page.Locator(".ui-datepicker-year").SelectOptionAsync(new SelectOptionValue { Value = expiryYear.ToString() }, locatorSelectOptions);

		await Page.Locator(".ui-datepicker-month").SelectOptionAsync(new SelectOptionValue { Value = expiryMonth.ToString() }, locatorSelectOptions);

		await Page.Locator("#ui-datepicker-div").Page.GetByRole(AriaRole.Link, new() { Name = expiryDay.ToString(), Exact = true }).ClickAsync(locatorClickOptions);

		//save pos 1 
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_ibPositionCreationSave").ClickAsync(locatorClickOptions);
		//confirm save
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_lbPositionCreationSave").ClickAsync(locatorClickOptions);
		await Page.WaitForURLAsync(QQS_OFFER_DETAIL_URL, pageWaitForUrlOptions);
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//add offer for pos 2
		Console.WriteLine("add offer pos2");
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_anchorEditCut").ClickAsync(locatorClickOptions);
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbArticleNumber").FillAsync("pos_offer2");
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbDeliveryDays").FillAsync("14");
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbShortDescription").FillAsync("Pos form offer 2");
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbLongDescription").FillAsync("Position form offer 2");
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbAmount").FillAsync("154"); //quantity
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbPricePerUnit_BeforeComma").FillAsync("114");
		//save position
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_ibPositionCreationSave").ClickAsync(locatorClickOptions);
		//confirm save
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_lbPositionCreationSave").ClickAsync(locatorClickOptions);

		await Page.WaitForURLAsync(QQS_OFFER_DETAIL_URL, pageWaitForUrlOptions);

		//add offer reference
		Console.WriteLine("add offer reference");
		await Page.Locator("#ctl00_MainContent_tbReference").FillAsync("pos_form_offer_" + CurrentDate, new LocatorFillOptions { Timeout = 180000 });

		//send 
		Console.WriteLine("click send offer on bottom action bar");
		await Page.Locator("#ctl00_MainContent_offerActionBarBottom_lblSendAnchorColapsed").ClickAsync(locatorClickOptions);

		Console.WriteLine("click send offer confirmation");
		await Page.Locator("#ctl00_MainContent_offerActionBarBottom_lbSendOffer").ClickAsync(locatorClickOptions);

		//wait for url https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx
		Console.WriteLine("wait for easyorder/SupplierRequestList2007.aspx");
		await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

		Console.WriteLine("search for " + requestToOfferTransactionName);
		//search for request , assert new status is sent
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync(locatorClickOptions);
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestPositionFormTransactionName);
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync(locatorClickOptions);

		Console.WriteLine("wait for easyorder/SupplierRequestList2007.aspx");
		//waitfor url https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx
		await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

		//wait for results  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData
		await Page.WaitForSelectorAsync("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData");

		//Assert 1 result
		await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager")).ToContainTextAsync("(1 items found)");
		Console.WriteLine("assert status is now sent");
		//assert status is now sent
		await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_lblStatus")).ToContainTextAsync("Sent");
	}


	[Test, Order(25)]
	[Category("QQTests1")]
	async public Task TC25_QQB_Buyer_Order_Offer_Position_Form()
	{
		//test based on devops test caseid 181497 : Buyer order offer (posiiton form)
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
		/*
		Open "Pos_form_{yymmdd}"
		Select "All" from the drop down list right to header "Quantity"
		Select "Order Position" from the drop down list "Actions for selected positions"
		Click the ">" button next to drop down list
		*/

		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestPositionFormTransactionName = "PW_Auto_RequestPositionPFchromium_202412251054";

		//////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("25: TC25_QQB_Buyer_Order_Offer_Position_Form");
		Console.WriteLine("request" + requestPositionFormTransactionName);
		string url = SEARCHURL;
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 180000, State = WaitForSelectorState.Visible };

		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };

		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };

		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };

		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };

		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };

		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };

		Console.WriteLine("login to search:" + DateTime.Now.ToLongTimeString());
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loggedin = true;
			}
			catch (Exception ex)
			{
				attempts++;
				Console.WriteLine("exception: " + ex.Message);
				Console.WriteLine(DateTime.Now.ToString());
				Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
				//seeing a lot of errors of type
				/*
						Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
				*/
			}
		}
		if (loggedin == false && attempts >= 10)
		{
			DateTime timeRightNow = DateTime.Now;
			string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC25_LoginError_" + FileCurrentDate + ".png"
			});
		}

		Console.WriteLine("page: " + Page.Url);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		if (await Page.Locator("#shoppingCartTitle").IsVisibleAsync())
		{
			await Page.Locator("//*[@data-testid='removeAllItems']").ClickAsync();
			await Task.Delay(TimeSpan.FromSeconds(1));
			await Page.Locator("//button[contains(@id, 'noty_button') and text()='OK']").ClickAsync();
		}

		//assert search has a quick quote link for this view/company
		await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

		//assert on request list page
		Console.WriteLine("wait for request list page");

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//get current request count
		var currentrequestCount = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount").TextContentAsync();

		//search for the request to offer

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestPositionFormTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//wait for results
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert 1 result
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");

		//edit the request
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_ibEditRequest").ClickAsync(locatorClickOptions);
		if (Environment == "QA")
		{
			await Task.Delay(3000);
		}

		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request Details")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert status is Answered
		await Expect(Page.FrameLocator("#qqFrame").Locator("#aspnetForm")).ToContainTextAsync("Answered");
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Answered")).ToBeVisibleAsync();

		await Expect(Page.FrameLocator("#qqFrame").Locator("#aspnetForm")).ToContainTextAsync(requestPositionFormTransactionName);

		Console.WriteLine("assert checkboxes are visible");
		//assert checkboxes are visible
		await Expect(Page.FrameLocator("#qqFrame").Locator("(//input[@type='checkbox'])[1]")).ToBeVisibleAsync();

		await Expect(Page.FrameLocator("#qqFrame").Locator("(//input[@type='checkbox'])[2]")).ToBeVisibleAsync();

		//Select "All" from the drop down list right to header "Quantity"
		Console.WriteLine("select all checkboxes option");
		await Page.FrameLocator("#qqFrame").Locator("select[name=\"position\"]").SelectOptionAsync(new[] { "all" });
		//assert that all 2 checkboxes are checked

		await Expect(Page.FrameLocator("#qqFrame").Locator("(//input[@type='checkbox'])[1]")).ToBeCheckedAsync();

		await Expect(Page.FrameLocator("#qqFrame").Locator("(//input[@type='checkbox'])[2]")).ToBeCheckedAsync();


		//Select "Order Position" from the drop down list "Actions for selected positions"
		//Click the ">" button next to drop down list
		await Page.FrameLocator("#qqFrame").Locator("#ddlChooseAction").SelectOptionAsync(new[] { "4" });//order

		Console.WriteLine("load shopping cart");
		//click > button
		//await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = ">" }).ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("//*[@id='btnOrder']/preceding-sibling::button").ClickAsync(locatorClickOptions);

		//wait for page
		Console.WriteLine("assert shopping cart has 2 items");
		try
		{
			await Expect(Page.Locator("#shoppingCartTitle")).ToContainTextAsync("Shopping Cart (2)");
			//await Expect(Page.Locator("//*[@role='main']//h1")).ToContainTextAsync("Shopping Cart");
			await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Transfer Shopping Cart" }).First).ToBeVisibleAsync();

			Console.WriteLine("check shopping cart items");
			await Expect(Page.Locator("(//td[@class='product-list__column product-list__title'])[1]")).ToContainTextAsync("Pos form offer 1");

			await Expect(Page.Locator("(//td[@class='product-list__column product-list__title'])[2]")).ToContainTextAsync("Pos form offer 2");
		}
		catch { }

		//assert supplier
		if (Environment == "QA")
		{
			await Expect(Page.Locator(".product-list__column.product-list__supplier").First).ToContainTextAsync(TC02_ASSERT_SUPPLIER1);
		}

		//go back to search list check status is now item added to cart
		await Page.GotoAsync(url, pageGotoOptions);
		await Page.WaitForURLAsync(url, pageWaitForUrlOptions);

		//assert search has a quick quote link for this view/company
		await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestPositionFormTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);

		//wait for results
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert 1 result
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");

		//assert status of the request we just published is updated to the status of "Item has been added to the Cart"
		Console.WriteLine("assert status is now 'Item has been added to the Cart' for: " + requestPositionFormTransactionName);
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblStatus")).ToContainTextAsync("Item has been added to the Cart");

	}


	[Test, Order(26)]
	[Category("EmailTests")]
	public void TC26_QQB_New_Request_Email_For_Supplier_Position_Form()
	{

		//this is the second batch of email tests wait for 1 minute to allow email to be delivered
		try
		{
			System.Threading.Thread.Sleep(180000);
		}
		catch { }
		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestPositionFormTransactionName = "PW_Auto_RequestPositionForm_chromium_2024228122";

		//testStartSecondsSinceEpoch = "1709650575";  //after: 1709650575  Tuesday, March 5, 2024 2:56:15 PM

		//requestIdPositionForm = "";
		//////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("26: TC26_QQB_New_Request_Email_For_Supplier_Position_Form");
		Console.WriteLine("request" + requestPositionFormTransactionName);
		bool testPassed = true;
		Console.WriteLine("instantiate gmail api service");
		try
		{
			UserCredential credential;
			// Load client secrets.
			using (var stream =
						 new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
			{
				/* The file token.json stores the user's access and refresh tokens, and is created
				 automatically when the authorization flow completes for the first time. */
				string credPath = "token.json";
				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
						GoogleClientSecrets.FromStream(stream).Secrets,
						Scopes,
						"user",
						CancellationToken.None,
						new FileDataStore(credPath, true)).Result;
				Console.WriteLine("Credential file saved to: " + credPath);
			}

			// Create Gmail API service.
			bool connected = false;
			GmailService? service = null;
			IList<Message> messages = new List<Message>();
			int connectionAttempt = 0;
			while (!connected && connectionAttempt < 10)
			{
				Console.WriteLine("create gmail service, attempt: " + (connectionAttempt + 1).ToString());
				try
				{
					if (service == null)
					{
						service = new GmailService(new BaseClientService.Initializer
						{
							HttpClientInitializer = credential,
							ApplicationName = ApplicationName
						});
					}

					UsersResource.MessagesResource.ListRequest requestMessage = service.Users.Messages.List("easyordertest@gmail.com");
					requestMessage.LabelIds = "INBOX";
					requestMessage.IncludeSpamTrash = false;
					//Change Request message template has Request-number:
					//Offer received and new request templates have Request ID:
					//Request rejected template has Request-id:
					requestMessage.Q = $"after:{testStartSecondsSinceEpoch} AND subject:({TC_REQUEST_RECEIVED_EMAIL_SUBJECT}) AND {requestPositionFormTransactionName}";
					//is it better to search for requestPositionFormTransactionName
					Console.WriteLine("Q. " + requestMessage.Q);
					messages = requestMessage.Execute().Messages;	
					//search for new request emails for request requestPositionFormTransactionName
					connected = true;
				}
				catch (Exception ex)
				{
					Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
					if (service == null)
					{
						Console.WriteLine("service is null");
					}

					if (credential == null)
					{
						Console.WriteLine("credentials are null");
					}
					Console.WriteLine("gmail exception: " + ex.Message);
					if (ex.InnerException != null)
					{
						Console.WriteLine(ex.InnerException.Message);
					}
					connectionAttempt++;
					Console.WriteLine("gmail service instantiation attempt: " + connectionAttempt.ToString());
				}
			}
			if (Environment == "QA")
			{
				Console.WriteLine("expecting 3 new request received email messages for " + requestPositionFormTransactionName + " ID: " + requestIdPositionForm);
			}

			if (Environment == "PROD")
			{
				Console.WriteLine("expecting 1 new request received email message for " + requestPositionFormTransactionName + " ID: " + requestIdPositionForm);
			}

			if (messages == null || messages.Count == 0)
			{
				Console.WriteLine("No new request received email messages found for request: " + requestIdPositionForm);
				if (messages == null)
				{
					Console.WriteLine("messages null");
					testPassed = false;
				}
				else
				{
					Console.WriteLine("messages count == 0");
					testPassed = false;
				}
			}
			else
			{
				if (Environment == "QA")
				{
					//expected 3 messages on qa

					Console.WriteLine("expecting 3 new request received email messages for " + requestPositionFormTransactionName + " ID: " + requestIdPositionForm);
					Console.WriteLine("found " + messages.Count.ToString() + " email message(s)");
					Assert.That(messages.Count == 3);
				}

				if (Environment == "PROD")
				{
					//expected 1, note one is in omnicontentuser+ email box
					Console.WriteLine("expecting 1 new request received email message for " + requestPositionFormTransactionName + " ID: " + requestIdPositionForm);
					Console.WriteLine("found " + messages.Count.ToString() + " email message(s)");
					Assert.That(messages.Count == 1);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null)
			{
				Console.WriteLine(ex.InnerException.Message);
			}
			throw ex;
		}

		Assert.That(testPassed == true);
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

		[Test, Order(27)]
	[Category("EmailTests")]
	 public void TC27_QQB_New_Offer_Email_For_Buyer_Position_Form()
	{
		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////
		/*
	 Check email easyordertest@gmail.com

	 QA  1 expected email
	 1) fmks qq -> EasyOrderTest+fmkbqq --> [QA][Quick Quote] You received an offer


	 PROD
	 1) Supplier Account EPAM ->  easyordertest+TESTCOE04QQ -> [Quick Quote] You received an offer


*/
		//requestPositionFormTransactionName = "PW_Auto_RequestPositionForm_chromium_2024228122";
		//testStartSecondsSinceEpoch = "1709650575";  //after: 1709650575  Tuesday, March 5, 2024 2:56:15 PM

		//requestIdPositionForm = "";
		//////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("27: TC27_QQB_New_Offer_Email_For_Buyer_Position_Form");
		Console.WriteLine("request" + requestPositionFormTransactionName);

		try
		{
			UserCredential credential;
			// Load client secrets.
			using (var stream =
						 new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
			{
				/* The file token.json stores the user's access and refresh tokens, and is created
				 automatically when the authorization flow completes for the first time. */
				string credPath = "token.json";
				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
						GoogleClientSecrets.FromStream(stream).Secrets,
						Scopes,
						"user",
						CancellationToken.None,
						new FileDataStore(credPath, true)).Result;
				Console.WriteLine("Credential file saved to: " + credPath);
			}

			// Create Gmail API service.
			bool connected = false;
			GmailService? service = null;
			IList<Message> messages = new List<Message>();
			int connectionAttempt = 0;
			while (!connected && connectionAttempt < 10)
			{
				Console.WriteLine("create gmail service, attempt: " + (connectionAttempt + 1).ToString());
				try
				{
					if (service == null)
					{
						service = new GmailService(new BaseClientService.Initializer
						{
							HttpClientInitializer = credential,
							ApplicationName = ApplicationName
						});
					}

					UsersResource.MessagesResource.ListRequest requestMessage = service.Users.Messages.List("easyordertest@gmail.com");
					requestMessage.LabelIds = "INBOX";
					requestMessage.IncludeSpamTrash = false;
					//note the message templates have a slightly different wording around  the request in the email body, e.g.
					//Change Request has Request-number:
					//Offer received and new request have Request ID:
					//Request rejected template has Request-id:
					requestMessage.Q = $"after:{testStartSecondsSinceEpoch} subject:({TC_OFFER_RECEIVED_EMAIL_SUBJECT})  AND \"Request ID: {requestIdPositionForm}\"";
					Console.WriteLine("Q. = " + requestMessage.Q);
					messages = requestMessage.Execute().Messages;

					connected = true;
				}
				catch (Exception ex)
				{
					Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
					if (service == null)
					{
						Console.WriteLine("service is null");
					}

					if(credential == null)
					{
						Console.WriteLine("credentials are null");
					}
					Console.WriteLine("gmail exception: " + ex.Message);
					if (ex.InnerException != null)
					{
						Console.WriteLine(ex.InnerException.Message);
					}
					connectionAttempt++;
					Console.WriteLine("gmail service instantiation attempt: " + connectionAttempt.ToString());
				}
			}


			if (messages == null || messages.Count == 0)
			{
				Console.WriteLine("No offer email received message found for request" + requestPositionFormTransactionName);
				if (messages == null)
				{
					Console.WriteLine("messages null");
					Assert.That(messages != null);
				}
				else
				{
					Console.WriteLine("messages count  == 0");
					Assert.That(messages.Count != 0);
				}
			}
			else
			{
				if (Environment == "QA")
				{
					//expected 1 message on qa

					Console.WriteLine("expecting 1 offer received email message for " + requestPositionFormTransactionName + " ID: " + requestIdPositionForm);
					Console.WriteLine("found " + messages.Count.ToString() + " email message(s)");
					Assert.That(messages.Count == 1);
				}

				if (Environment == "PROD")
				{
					//expected 1, note one is in omnicontentuser+ email box
					Console.WriteLine("expecting 1 offer received email message for " + requestPositionFormTransactionName + " ID: " + requestIdPositionForm);
					Console.WriteLine("found " + messages.Count.ToString() + " email message(s)");
					Assert.That(messages.Count == 1);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null)
			{
				Console.WriteLine(ex.InnerException.Message);
			}
			throw ex;
		}
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(28)]
	[Category("QQTests2")]
	async public Task TC28_QQB_Buyer_Create_Request_Request_Form()
	{
		//test based on devops test caseid 181500 : Buyer create request (request form)
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
		/*
		Create new request with datasheet
   "With Form - form has own class system"

		after selecting datasheet and completing the form, the request has 1 partially prefilled request position
		*/

		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////
		//requestPositionRequestFormTransactionName = "";
		//////////////////////////////////////////////////////////////////////////////


		Console.WriteLine("28: TC28_QQB_Buyer_Create_Request_Request_Form");
		Console.WriteLine("request" + requestPositionRequestFormTransactionName);
		string url = SEARCHURL;
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 180000, State = WaitForSelectorState.Visible };

		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };

		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };

		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };

		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };

		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };

		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };

		Console.WriteLine("login to search:" + DateTime.Now.ToLongTimeString());
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loggedin = true;
			}
			catch (Exception ex)
			{
				attempts++;
				Console.WriteLine("exception: " + ex.Message);
				Console.WriteLine(DateTime.Now.ToString());
				Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
				//seeing a lot of errors of type
				/*
						Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
				*/
			}
		}
		if (loggedin == false && attempts >= 10)
		{
			DateTime timeRightNow = DateTime.Now;
			string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC28_LoginError_" + FileCurrentDate + ".png"
			});
		}

		Console.WriteLine("page: " + Page.Url);

		//assert search has a quick quote link for this view/company
		await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

		var iframeElement = await Page.Locator("#qqFrame").ElementHandleAsync();
		var frame = await iframeElement.ContentFrameAsync();
		if (frame != null && frame.Url != "")
		{
			Console.WriteLine("qqFrame Url : " + frame.Url);
			//await frame.WaitForURLAsync("https://portal.hubwoo.com/srvs/easyorder/RequestList2007.aspx");
		}

		if (Environment == "QA")
		{
			await Task.Delay(3000);
		}

		//assert on request list page
		Console.WriteLine("wait for request list page");

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//create request
		Console.WriteLine("click Create a request");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbCreateRequestTop").ClickAsync(locatorClickOptions);

		///
		//how to perform WaitForURLAsync in iframe
		iframeElement = await Page.Locator("#qqFrame").ElementHandleAsync();
		frame = await iframeElement.ContentFrameAsync();
		if (frame != null && frame.Url != "")
		{
			Console.WriteLine("qqFrame Url : " + frame.Url);
			//await frame.WaitForURLAsync("https://portal.hubwoo.com/srvs/easyorder/DataSheetChoose.aspx");
		}

		if (Environment == "QA")
		{
			await Task.Delay(3000);
		}

		//assert on datasheetchoose page
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Product Group", locatorToContainTextOption);

		//expand product group QA Tests (qa)  / PROD Tests (Prod) use the unique id on the image as a locator
		Console.WriteLine("expand product group");
		await Page.FrameLocator("#qqFrame").Locator(TC02_PRODUCT_GROUP).ClickAsync(locatorClickOptions);

		//select "With Form - form has own class system"

		Console.WriteLine("select 'With Form - form has own class system' datasheet");
		await Page.FrameLocator("#qqFrame").Locator(TC28_FORM_DATASHEET2_SELECTOR).ClickAsync(locatorClickOptions);

		//click choose
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Choose" }).ClickAsync(locatorClickOptions);

		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Form");
		//form edit page is displayed here or when radio buttn selected?
		/*
		 Fill text field 1 with "Header form mandatory text"
			Check "Check box 1"
			Select "Radio option 3"
			Click save
		 */

		await Expect(Page.FrameLocator("#qqFrame").GetByText("First test group")).ToBeVisibleAsync();
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Text field")).ToBeVisibleAsync();
		await Expect(Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Cell, new() { Name = "DropDown Box 1", Exact = true })).ToBeVisibleAsync();
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Memo area")).ToBeVisibleAsync();
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Radio buttons")).ToBeVisibleAsync();
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Date")).ToBeVisibleAsync();

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repGroups_ctl00_repAttributes_ctl00_tbTextBoxValue").FillAsync("Header form mandatory text");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repGroups_ctl00_repAttributes_ctl01_cbCheckBoxValue").CheckAsync();
		await Page.FrameLocator("#qqFrame").GetByLabel("Radio option 3").CheckAsync();


		//save the form ctl00_MainContent_lbSave
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSave").ClickAsync(locatorClickOptions);

		//transactionid
		DateTime today = DateTime.Now;
		string CurrentDate = $"{today.Year}{today.Month}{today.Day}{today.Hour}{today.Minute}";
		string transactionID = "PW_Auto_RequestForm" + _browserName + "_" + CurrentDate;//this transactionid will be used in other tests in this suite!
		string commentDate = $"{today.Year}{today.Month}{today.Day}";

		requestPositionRequestFormTransactionName = transactionID;//allows this request to be opened in another test and be referenced during teardown

		Console.WriteLine("wait for request details page to load...");
		Console.WriteLine("confirm that fields are prefilled");
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request Details")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert testform2
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblFormName")).ToContainTextAsync("TestForm2");

		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbLongDescription")).ToContainTextAsync("Please see the form for details.");

		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbShortDescription")).ToHaveValueAsync("Please see the form for details.");

		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbEclassCode")).ToBeEmptyAsync();

		//assert only 1 position
		Console.WriteLine("assert only 1 request position.");
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblPositionCount")).ToContainTextAsync("1 )");

		//assert classification icon visible
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_ibShowClassification")).ToBeVisibleAsync(locatorVisibleAssertion);
		
		var readonlyInput = Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbProductGroup");

		await Expect(readonlyInput).ToBeDisabledAsync();

		var selectedDataSheet = await readonlyInput.GetAttributeAsync("value");

		Assert.That(selectedDataSheet == "With Form - form has own class system");

		//assert transactionid is empty
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbTransactionNumber")).ToBeEmptyAsync();

		//assert that no supplier selected
		await Expect(Page.FrameLocator("#qqFrame").Locator("#selectSelectedSuppliers")).ToBeEmptyAsync();

		//assert that available suppliers contains 2 suppliers
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repExistingSuppliers_ctl00_Option1")).ToContainTextAsync(TC02_ASSERT_SUPPLIER1);
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repExistingSuppliers_ctl01_Option1")).ToContainTextAsync(TC02_ASSERT_SUPPLIER2);

		//assert textbox present for classification code
		//ctl00_MainContent_repRequestPosition_ctl01_tbEclassCode
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbEclassCode")).ToBeVisibleAsync(locatorVisibleAssertion);

		//add transaction number 
		Console.WriteLine("complete request details");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbTransactionNumber").FillAsync(transactionID);

		await Task.Delay(3000);

		//select both suppliers
		//add both suppliers
		await Page.FrameLocator("#qqFrame").Locator("#selectExistingSuppliers").SelectOptionAsync(new[] { TC02_SELECT_SUPPLIER1_ID });
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync();
		await Page.FrameLocator("#qqFrame").Locator("#selectExistingSuppliers").SelectOptionAsync(new[] { TC02_SELECT_SUPPLIER2_ID });
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync(locatorClickOptions);


		//if qa select shipping address
		if (Environment == "QA")
		{
			//need to select a shipping address
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlLocationShippingAddress").SelectOptionAsync(new[] { "1368" });//westgate ripon
			//note Michelin local code is not mandatory
		}
		/////////////////////////////////////////////  POSITION 1

		//complete position 1
		Console.WriteLine("add short description pos1");

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbShortDescription").FillAsync("Position 1");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbLongDescription").FillAsync("Position 1 details");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbQuantity").FillAsync("10");

		Console.WriteLine("add classification pos1");
		await Task.Delay(3000);

		var Page1 = await Page.RunAndWaitForPopupAsync(async () =>
		{
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_ibShowClassification").ClickAsync(locatorClickOptions);
		}, new PageRunAndWaitForPopupOptions { Timeout = 180000 });

		await Page.WaitForTimeoutAsync(3000);

		await Expect(Page1.Locator("#tbSearchField")).ToBeVisibleAsync();
		//todo this needs to be tested on prod
		await Page1.GetByRole(AriaRole.Link, new() { Name = TC28_CLASS_CODE_LEVEL1 }).ClickAsync(locatorClickOptions);
		await Page1.GetByRole(AriaRole.Link, new() { Name = TC28_CLASS_CODE_LEVEL2 }).ClickAsync(locatorClickOptions);
		await Page1.GetByRole(AriaRole.Link, new() { Name = TC28_CLASS_CODE_LEVEL3 }).ClickAsync(locatorClickOptions);
		await Page1.GetByRole(AriaRole.Link, new() { Name = TC28_CLASS_CODE_LEVEL4 }).ClickAsync(locatorClickOptions);

		await Page.WaitForTimeoutAsync(3000);

		//assert class code is correct
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbEclassCode")).ToHaveValueAsync(TC28_CLASS_CODE);

		//save pos 1
		Console.WriteLine("save pos1");
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_ibSaveRequestPosition").ClickAsync(locatorClickOptions);

		await Task.Delay(3000);

		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblStatus1")).ToContainTextAsync("Successfully saved.");

		//send
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_hrefPublishTop").ClickAsync(locatorClickOptions);

		//assert cover message
		//you have selected all suppliers from the current product group
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblPublishTextAllTop")).ToContainTextAsync("You have selected all suppliers from current product group");

		//send button is displayed
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbPublishButtonTop")).ToBeVisibleAsync(locatorVisibleAssertion);

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbPublishButtonTop").ClickAsync(locatorClickOptions);
		iframeElement = await Page.Locator("#qqFrame").ElementHandleAsync();
		frame = await iframeElement.ContentFrameAsync();
		if (frame != null && frame.Url != "")
		{
			Console.WriteLine("qqFrame Url : " + frame.Url);
			//await frame.WaitForURLAsync("https://portal.hubwoo.com/srvs/easyorder/RequestList2007.aspx");
		}
		//assert back on request list
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Request list");

		//search for request just sent
		Console.WriteLine("Search For " + requestPositionRequestFormTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestPositionRequestFormTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);

		//assert only 1 result
		try
		{
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");
		}
		catch
		{
			Console.WriteLine("More than one search result for request..." + requestPositionRequestFormTransactionName);
		}
		//assert status is now requested
		Console.WriteLine("assert status is now requested for " + requestPositionRequestFormTransactionName);
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblStatus")).ToContainTextAsync("Requested");


		//get the requestid and update it for use in email tests
		var newRequestId = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblRequestID").TextContentAsync();
		requestIdRequestForm = newRequestId;

		Console.WriteLine("test finished"); 
	}

	[Test, Order(29)]
	[Category("QQTests2")]
	async public Task TC29_QQB_Buyer_Download_Request_Request_Form()
	{
		//test based on devops test caseid 181501 : Buyer download request (request form)
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303

		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestPositionRequestFormTransactionName = "PW_Auto_RequestFormchromium_20241225151";

		//////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("29: TC29_QQB_Buyer_Download_Request_Request_Form");
		Console.WriteLine("request" + requestPositionRequestFormTransactionName);
		string url = SEARCHURL;
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 180000, State = WaitForSelectorState.Visible };

		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };

		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };

		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };

		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };

		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };

		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };

		Console.WriteLine("login to search:" + DateTime.Now.ToLongTimeString());
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loggedin = true;
			}
			catch (Exception ex)
			{
				attempts++;
				Console.WriteLine("exception: " + ex.Message);
				Console.WriteLine(DateTime.Now.ToString());
				Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
				//seeing a lot of errors of type
				/*
						Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
				*/
			}
		}
		if (loggedin == false && attempts >= 10)
		{
			DateTime timeRightNow = DateTime.Now;
			string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC29_LoginError_" + FileCurrentDate + ".png"
			});
		}

		Console.WriteLine("page: " + Page.Url);

		//assert search has a quick quote link for this view/company
		await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

		//assert on request list page
		Console.WriteLine("wait for request list page");

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//get current request count
		var currentrequestCount = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount").TextContentAsync();

		//search for the request to offer

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestPositionRequestFormTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);

		//wait for results
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//assert 1 result
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");

		//wait for search results and check status of the first result
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToBeVisibleAsync(locatorVisibleAssertion);

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_ibDownloadArea").ClickAsync(locatorClickOptions);

		//await Page.PauseAsync();//causes the playwright test inspector to launch even when not debugging and cause pause of the run
		await Task.Delay(3000);
		var waitForDownloadTask = Page.WaitForDownloadAsync();
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_hlRequestPdfText").First.ClickAsync(new()
		{
			//modifier allows the save as functionality, which makes the generator save to disk rather tha be rendered in a new tab
			Modifiers = new[] { KeyboardModifier.Alt },
			Timeout = 180000
		});

		var download = await waitForDownloadTask;

		var fileName = downloadPath + "TC29_" + requestPositionRequestFormTransactionName + download.SuggestedFilename;

		await Task.Delay(3000);
		

		// Wait for the download process to complete and save the downloaded file somewhere
		await download.SaveAsAsync(fileName);

		if (Environment == "QA")
		{
			await Task.Delay(3000);
		}

		if (Environment == "PROD")
		{
			await Task.Delay(6000);
		}

		//load pdf in another tab and screenshot it
		var page2 = await Page.RunAndWaitForPopupAsync(async () =>
		{
			//fails if more than one request file result in the request list on the page
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Download the request as PDF File" }).First.ClickAsync(locatorClickOptions);
		}, new PageRunAndWaitForPopupOptions { Timeout = 180000 });


		//download url
		var pdfUrl = page2.Url;
		try
		{
			Console.WriteLine("pdf downloaded from " + pdfUrl);
			Console.WriteLine("asserting contents of  " + fileName);
			using (PdfDocument pdf = PdfDocument.Open(fileName))
			{
				Page page = pdf.GetPage(1);
				if (page != null)
				{
					Console.WriteLine("Asserting contents of pdf");
					Assert.That(page.Text.Contains(TC05_PDF_ASSERT_CUSTOMERNAME));
					Assert.That(page.Text.Contains(TC05_PDF_ASSERT_LOCATION));
					Assert.That(page.Text.Contains(requestPositionRequestFormTransactionName));
					Assert.That(page.Text.Contains("Text field 1"));
					Assert.That(page.Text.Contains("Check box1"));
					Assert.That(page.Text.Contains("Check box1"));
					Assert.That(page.Text.Contains("Radio buttons 1"));
					Assert.That(page.Text.Contains("Date 1"));
					Assert.That(page.Text.Contains("Radio option 3"));
					Assert.That(page.Text.Contains("Header form mandatory text"));
					Assert.That(page.Text.Contains("With Form - form has own class system"));
					Assert.That(page.Text.Contains("This is just a label to be displayed and nothing to be done with it"));
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("Issue asserting contents of pdf document " + pdfUrl);
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
		}

		//screenshots are blank when running this pause fixes it
		await Task.Delay(3000);

		await page2.ScreenshotAsync(new()
		{
			FullPage = true,
			Path = downloadPath + "TC29_" + requestPositionRequestFormTransactionName + "_QQB_BuyerDownloadExcelAndPdfRequest_pdf.png"
		});

		//await Page.PauseAsync();//causes the playwright test inspector to launch even when not debugging and cause pause of the run
		await Task.Delay(3000);
		//download of an excel file is weird in chromium as it is running in incognito mode, you are shown a popup with a guid file name but no file is available when you open folder?
		try
		{
			//download excel
			///////////////////////////////////////////////////////////////////////////////////////////////////////////
			var waitForExcelDownloadTask = Page.WaitForDownloadAsync();
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Download the request as Excel File" }).First.ClickAsync(new()
			{
				//modifier allows the save as functionality, which makes the generator save to disk rather than be rendered in a new tab
				Modifiers = new[] { KeyboardModifier.Alt },
				Timeout = 180000
			});
			await Task.Delay(3000);
			var excelDownload = await waitForExcelDownloadTask;

			// Wait for the download process to complete and save the downloaded file somewhere
			await excelDownload.SaveAsAsync(downloadPath + "TC29_" + requestPositionRequestFormTransactionName + excelDownload.SuggestedFilename);
		}
		catch (Exception ex)
		{
			Console.WriteLine("exception TC29_QQB_BuyerDownloadExcelAndPdfRequest");
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
		}
	}

	

	[Test, Order(30)]
	[Category("QQTests2")]
	async public Task TC30_QQS_Supplier_Download_Request_Request_Form()
	{
		//test based on devops test caseid 181503 : Supplier download request (request form)
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303

		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestPositionRequestFormTransactionName = "PW_Auto_RequestFormchromium_20241225151";

		//////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("30: TC30_QQS_Supplier_Download_Request_Request_Form");
		Console.WriteLine("request" + requestPositionRequestFormTransactionName);
		string url = PORTAL_LOGIN;
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };

		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				await Page.WaitForLoadStateAsync(LoadState.Load);
				await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
				await Page.GetByPlaceholder("Enter your user name").FillAsync(SUPPLIER_USER1_LOGIN);
				await Page.GetByPlaceholder("Enter your password").FillAsync(SUPPLIER_USER1_PASSWORD);
				await Page.Locator("#signInButtonId").IsEnabledAsync();
				await Page.Locator("#signInButtonId").ClickAsync(locatorClickOptions);
				loggedin = true;
			}
			catch (Exception e)
			{
				attempts++;
				Console.WriteLine("Problems logging in, attempt:" + attempts.ToString());
				//seeing a lot of errors of type
				/*
					Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F
					Call log:- navigating to "https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F", waiting until "load"
					*/
				Console.WriteLine(e.Message);
			}
		}
		Console.WriteLine("Page: " + Page.Url);

		//wait for page to load https://portal.hubwoo.com/main/
		//await Page.WaitForURLAsync("https://portal.hubwoo.com/main/", pageWaitForUrlOptions);  //this fails for some reason

		await Page.WaitForSelectorAsync("//side-bar-item-group[@name='Opportunities']", new PageWaitForSelectorOptions { Timeout = 180000 });
		Console.WriteLine("Page: " + Page.Url);
		var isCookieConsentVisible = await Page.Locator("#cookie-consent-dialog").IsVisibleAsync();
		if (isCookieConsentVisible)
		{
			await Page.GetByRole(AriaRole.Button, new() { Name = "Accept all" }).ClickAsync();
		}

		await Page.Locator("//side-bar-item-group[@name='Opportunities']").ClickAsync(locatorClickOptions);
		await Page.Locator("//side-bar-item[@name='Requests']").ClickAsync(locatorClickOptions);

		//wait for page to load
		await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

		if (Environment == "QA")
		{
			await Task.Delay(3000);
		}

		//search by transaction name
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync(locatorClickOptions);
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestPositionRequestFormTransactionName);
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await Task.Delay(3000);

		//wait for results  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData
		await Page.WaitForSelectorAsync("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData");

		//waitfor url https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx
		await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

		//Assert 1 result
		await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager")).ToContainTextAsync("(1 items found)");

		//click the download pdf option
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibDownloadArea").ClickAsync(locatorClickOptions);
		await Task.Delay(3000);
		//download pdf and assert contents
		var waitForDownloadTask = Page.WaitForDownloadAsync();
		//find the download link via the text
		await Page.GetByRole(AriaRole.Link, new() { Name = "Download the request as PDF File" }).First.ClickAsync(new()
		{
			//modifier allows the save as functionality, which makes the generator save to disk rather tha be rendered in a new tab
			Modifiers = new[] { KeyboardModifier.Alt },
			Timeout = 180000
		});

		var download = await waitForDownloadTask;

		var fileName = downloadPath + "TC31_" + requestPositionRequestFormTransactionName + download.SuggestedFilename;

		// Wait for the download process to complete and save the downloaded file somewhere
		await download.SaveAsAsync(fileName);

		//click the pdf option
		var page1 = await Page.RunAndWaitForPopupAsync(async () =>
		{
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_hlRequestPdf").ClickAsync();
		}, new PageRunAndWaitForPopupOptions { Timeout = 180000 });
		//take screenshot
		//screenshot  pdf tab requestToRejectTransactionName.pdf
		await page1.ScreenshotAsync(new()
		{
			FullPage = true,
			Path = downloadPath + "TC30_" + requestPositionRequestFormTransactionName + "_QQS_SupplierDownloadRequestPdf_pdf.png"
		});

		//assert the contents of the pdf
		var pdfUrl = page1.Url;
		try
		{
			Console.WriteLine("pdf downloaded from " + pdfUrl);
			Console.WriteLine("asserting contents of  " + fileName);
			using (PdfDocument pdf = PdfDocument.Open(fileName))
			{
				Page page = pdf.GetPage(1);
				if (page != null)
				{
					Console.WriteLine("Asserting contents of pdf");
					Assert.That(page.Text.Contains(TC05_PDF_ASSERT_CUSTOMERNAME));
					Assert.That(page.Text.Contains(TC05_PDF_ASSERT_LOCATION));
					Assert.That(page.Text.Contains(requestPositionRequestFormTransactionName));
					Assert.That(page.Text.Contains("Text field 1"));
					Assert.That(page.Text.Contains("Check box1"));
					Assert.That(page.Text.Contains("Check box1"));
					Assert.That(page.Text.Contains("Radio buttons 1"));
					Assert.That(page.Text.Contains("Date 1"));
					Assert.That(page.Text.Contains("Radio option 3"));
					Assert.That(page.Text.Contains("Header form mandatory text"));
					Assert.That(page.Text.Contains("With Form - form has own class system"));
					Assert.That(page.Text.Contains("This is just a label to be displayed and nothing to be done with it"));
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("Issue asserting contents of pdf document " + pdfUrl);
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
		}
	}

	[Test, Order(31)]
	[Category("QQTests2")]
	async public Task TC31_QQS_Supplier_Create_Offer_Request_Form()
	{
		//test based on devops test caseid 181504 :Supplier create offer (request form)
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
		/*
		  Click the open form button right to "TestForm2", which is below "Time left"
		 
		  Assert form detail sare correct
		  Comment for customer : Test with Request form

			Create offer for position 1
			Short description : Req form offer 1
			Long description : Request form offer 1
			Article No : Req_offer1
			Classification : 10101501
			Delivery date : 7
			Quantity : 77
			Price per unit : 107
			Expiration date : today + 7
		 */
		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////
		//requestPositionRequestFormTransactionName = "PW_Auto_RequestFormchromium_20241225151";
		//////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("31: TC31_QQS_Supplier_Create_Offer_Request_Form");
		Console.WriteLine("request" + requestPositionRequestFormTransactionName);
		string url = PORTAL_LOGIN;
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };

		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
		LocatorSelectOptionOptions locatorSelectOptions = new LocatorSelectOptionOptions { Timeout = 180000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				await Page.WaitForLoadStateAsync(LoadState.Load);
				await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
				await Page.GetByPlaceholder("Enter your user name").FillAsync(SUPPLIER_USER1_LOGIN);
				await Page.GetByPlaceholder("Enter your password").FillAsync(SUPPLIER_USER1_PASSWORD);
				await Page.Locator("#signInButtonId").IsEnabledAsync();
				await Page.Locator("#signInButtonId").ClickAsync(locatorClickOptions);
				loggedin = true;
			}
			catch (Exception e)
			{
				attempts++;
				Console.WriteLine("Problems logging in, attempt:" + attempts.ToString());
				//seeing a lot of errors of type
				/*
					Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F
					Call log:- navigating to "https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F", waiting until "load"
					*/
				Console.WriteLine(e.Message);
			}
		}
		Console.WriteLine("Page: " + Page.Url);

		//wait for page to load https://portal.hubwoo.com/main/
		//await Page.WaitForURLAsync("https://portal.hubwoo.com/main/", pageWaitForUrlOptions);  //this fails for some reason

		await Page.WaitForSelectorAsync("//side-bar-item-group[@name='Opportunities']", new PageWaitForSelectorOptions { Timeout = 180000 });
		Console.WriteLine("Page: " + Page.Url);
		var isCookieConsentVisible = await Page.Locator("#cookie-consent-dialog").IsVisibleAsync();
		if (isCookieConsentVisible)
		{
			await Page.GetByRole(AriaRole.Button, new() { Name = "Accept all" }).ClickAsync();
		}

		await Page.Locator("//side-bar-item-group[@name='Opportunities']").ClickAsync(locatorClickOptions);
		await Page.Locator("//side-bar-item[@name='Requests']").ClickAsync(locatorClickOptions);

		//wait for page to load
		await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

		//search by transaction name
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync(locatorClickOptions);
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestPositionRequestFormTransactionName);
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await Task.Delay(3000);
		await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

		//wait for results  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData
		await Page.WaitForSelectorAsync("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData");

		//Assert 1 result
		await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager")).ToContainTextAsync("(1 items found)");

		//waitfor url https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx
		await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

		//assert status
		await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_lblStatus")).ToContainTextAsync("Requested");

		//click edit 
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibEdit").ClickAsync(locatorClickOptions);

		//await Page.WaitForURLAsync(QQS_OFFER_DETAIL_URL, pageWaitForUrlOptions);  //wont work due to querystrng parameter
		await Expect(Page).ToHaveURLAsync(new Regex(QQS_OFFER_DETAIL_URL_REGEX));

		//wait for offer details page to load
		await Page.Locator("#ctl00_MainContent_offerActionBarTop_lblActions").WaitForAsync(locatorWaitForOptions);
		//assrrt details about the opened request/offer

		await Expect(Page.Locator("#ctl00_MainContent_ibFormFlag")).ToBeVisibleAsync();
		await Expect(Page.Locator("#ctl00_MainContent_lblFormName")).ToContainTextAsync("TestForm2");//does this need to be parameterized?
		await Expect(Page.Locator("#ctl00_MainContent_lblProductGroupValue")).ToContainTextAsync("With Form - form has own class system");
		await Expect(Page.Locator("#ctl00_MainContent_lblRequestTitle")).ToContainTextAsync(requestPositionRequestFormTransactionName);

		Console.WriteLine("Open the request form");
		//open form button TestForm2
		await Page.Locator("#ctl00_MainContent_ibFormFlag").ClickAsync();

		//expect url https://portal.qa.hubwoo.com/srvs/easyorder/CustomForms.aspx?formId=310&requestIdString=100102728
		await Expect(Page).ToHaveURLAsync(new Regex(CUSTOM_FORM_EDIT_REGEX));

		//assert form breadcrumb
		await Expect(Page.Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToBeVisibleAsync();

		await Expect(Page.Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Form");

		//assert form page and form details
		await Expect(Page.Locator("#ctl00_MainContent_repGroups_ctl00_repAttributes_ctl00_lblReadOnlyValue")).ToContainTextAsync("Header form mandatory text");

		//radio option 3 is disabled and checked

		await Expect(Page.Locator("#ctl00_MainContent_repGroups_ctl00_repAttributes_ctl04_radRadioButtonList_2")).ToBeDisabledAsync();

		await Expect(Page.Locator("#ctl00_MainContent_repGroups_ctl00_repAttributes_ctl04_radRadioButtonList_2")).ToHaveAttributeAsync("checked", "checked");


		await Expect(Page.Locator("#ctl00_MainContent_repGroups_ctl00_repAttributes_ctl01_cbCheckBoxValue")).ToBeCheckedAsync();

		await Expect(Page.Locator("#ctl00_MainContent_repGroups_ctl00_repAttributes_ctl01_cbCheckBoxValue")).ToHaveAttributeAsync("checked","checked");

		await Expect(Page.Locator("#ctl00_MainContent_repGroups_ctl00_repAttributes_ctl01_cbCheckBoxValue")).ToBeCheckedAsync();

		//close the form
		//await Page.GetByRole(AriaRole.Link, new() { Name = "Close" }).ClickAsync();
		await Page.Locator("#ctl00_MainContent_lbCancelDirect").ClickAsync(locatorClickOptions);
		//wait for url https://portal.qa.hubwoo.com/srvs/easyorder/OfferDetail2007.aspx

		//note queryparameters are lost when redirected from 
		await Page.WaitForURLAsync(QQS_OFFER_DETAIL_URL, pageWaitForUrlOptions);

		//set expiration date
		//pick date 7 days from now
		DateTime today = DateTime.Now;
		DateTime expiryDate = today.AddDays(7);
		int expiryYear = expiryDate.Year;
		int expiryMonth = expiryDate.Month - 1;//zero index
		int expiryDay = expiryDate.Day;

		string CurrentDate = $"{today.Year}{today.Month}{today.Day}";

		//add offer reference
		Console.WriteLine("add offerreference");
		await Page.Locator("#ctl00_MainContent_tbReference").FillAsync("req_form_offer_" + CurrentDate, new LocatorFillOptions { Timeout = 180000 });

		//assert status is requested
		await Expect(Page.Locator("#ctl00_MainContent_lblStatus")).ToContainTextAsync("Requested");

		//add comment for customer
		Console.WriteLine("add comment for customer");
		await Page.GetByRole(AriaRole.Link, new() { Name = "Comment for customer" }).ClickAsync(locatorClickOptions);
		await Page.Locator("#ctl00_MainContent_tbExternalComment").FillAsync("Test with Request form");
		

		//add offer for pos 1 ///////////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("add offer pos1");
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_anchorEditCut").ClickAsync(locatorClickOptions);
		Console.WriteLine("add offer pos1 details");
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbShortDescription").FillAsync("Req form offer 1");//description
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbLongDescription").FillAsync("Request form offer 1");
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbDeliveryDays").FillAsync("7"); //
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbArticleNumber").FillAsync("Req_offer1");

		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbAmount").FillAsync("77"); //quantity

		//assert classification controls are not created!!
		//classification is performed by buyer
		await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_ibShowClassification")).ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = 180000 });
		await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbEclassCode")).ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = 180000 });
		
		//classification code readonly label is visible
		await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_lblEclassCode")).ToBeVisibleAsync(locatorVisibleAssertion);

		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbPricePerUnit_BeforeComma").FillAsync("107");

		//click calendar on the position 1 
		//expiration date
		Console.WriteLine("set expiration date on offer position 1");
		await Page.GetByRole(AriaRole.Img, new() { Name = "..." }).ClickAsync(locatorClickOptions);

		await Page.Locator(".ui-datepicker-year").SelectOptionAsync(new SelectOptionValue { Value = expiryYear.ToString() }, locatorSelectOptions);

		await Page.Locator(".ui-datepicker-month").SelectOptionAsync(new SelectOptionValue { Value = expiryMonth.ToString() }, locatorSelectOptions);

		await Page.Locator("#ui-datepicker-div").Page.GetByRole(AriaRole.Link, new() { Name = expiryDay.ToString(), Exact = true }).ClickAsync(locatorClickOptions);

		Console.WriteLine("save position 1");
		//save pos 1 
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_ibPositionCreationSave").ClickAsync(locatorClickOptions);
		//confirm save
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_lbPositionCreationSave").ClickAsync(locatorClickOptions);
		Console.WriteLine("confirm save position 1");
		
		//await Page.WaitForURLAsync(QQS_OFFER_DETAIL_URL, pageWaitForUrlOptions);

		//send 
		Console.WriteLine("click send offer on bottom action bar");
		await Page.Locator("#ctl00_MainContent_offerActionBarBottom_lblSendAnchorColapsed").ClickAsync(locatorClickOptions);

		Console.WriteLine("click send offer confirmation");
		await Page.Locator("#ctl00_MainContent_offerActionBarBottom_lbSendOffer").ClickAsync(locatorClickOptions);

		//wait for url https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx
		Console.WriteLine("wait for easyorder/SupplierRequestList2007.aspx");
		//await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);//this timesout for some reason?

		Console.WriteLine("search for " + requestToOfferTransactionName);
		//search for request , assert new status is sent
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync(locatorClickOptions);
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestPositionRequestFormTransactionName);
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync(locatorClickOptions);

		Console.WriteLine("wait for easyorder/SupplierRequestList2007.aspx");
		//waitfor url https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx
		await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

		//wait for results  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData
		await Page.WaitForSelectorAsync("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData");

		//Assert 1 result
		await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager")).ToContainTextAsync("(1 items found)");
		Console.WriteLine("assert status is now sent");
		//assert status is now sent
		await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_lblStatus")).ToContainTextAsync("Sent");
	}

	[Test, Order(32)]
	[Category("QQTests2")]
	async public Task TC32_QQB_Buyer_Order_Offer_Request_Form()
	{
		//test based on devops test caseid 181505 : Buyer order offer (request form)
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303

		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestPositionRequestFormTransactionName = "PW_Auto_RequestFormchromium_20241225151";

		//////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("32: TC32_QQB_Buyer_Order_Offer_Request_Form");
		Console.WriteLine("request" + requestPositionRequestFormTransactionName);
		string url = SEARCHURL;
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 180000, State = WaitForSelectorState.Visible };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };

		Console.WriteLine("login to search:" + DateTime.Now.ToLongTimeString());
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loggedin = true;
			}
			catch (Exception ex)
			{
				attempts++;
				Console.WriteLine("exception: " + ex.Message);
				Console.WriteLine(DateTime.Now.ToString());
				Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
				//seeing a lot of errors of type
				/*
						Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
				*/
			}
		}
		if (loggedin == false && attempts >= 10)
		{
			DateTime timeRightNow = DateTime.Now;
			string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC32_LoginError_" + FileCurrentDate + ".png"
			});
		}

		Console.WriteLine("page: " + Page.Url);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		if (await Page.Locator("#shoppingCartTitle").IsVisibleAsync())
		{
			await Page.Locator("//*[@data-testid='removeAllItems']").ClickAsync();
			await Task.Delay(TimeSpan.FromSeconds(1));
			await Page.Locator("//button[contains(@id, 'noty_button') and text()='OK']").ClickAsync();
		}

		//assert search has a quick quote link for this view/company
		await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

		//assert on request list page
		Console.WriteLine("wait for request list page");

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//get current request count
		var currentrequestCount = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount").TextContentAsync();

		//search for the request to offer

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestPositionRequestFormTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//wait for results
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert 1 result
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");

		//edit the request
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_ibEditRequest").ClickAsync(locatorClickOptions);
		if (Environment == "QA")
		{
			await Task.Delay(3000);
		}

		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request Details")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert status is Answered
		await Expect(Page.FrameLocator("#qqFrame").Locator("#aspnetForm")).ToContainTextAsync("Answered");
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Answered")).ToBeVisibleAsync(locatorVisibleAssertion);

		await Expect(Page.FrameLocator("#qqFrame").Locator("#aspnetForm")).ToContainTextAsync(requestPositionRequestFormTransactionName);

		Console.WriteLine("assert checkboxes are visible");
		//assert checkbox is visible
		await Expect(Page.FrameLocator("#qqFrame").Locator("(//input[@type='checkbox'])[1]")).ToBeVisibleAsync();

		//Select "All" from the drop down list right to header "Quantity"
		Console.WriteLine("select all checkboxes option");
		await Page.FrameLocator("#qqFrame").Locator("select[name=\"position\"]").SelectOptionAsync(new[] { "all" });
		//assert that the single checkbox is checked

		await Expect(Page.FrameLocator("#qqFrame").Locator("(//input[@type='checkbox'])[1]")).ToBeCheckedAsync();


		//Select "Order Position" from the drop down list "Actions for selected positions"
		//Click the ">" button next to drop down list
		await Page.FrameLocator("#qqFrame").Locator("#ddlChooseAction").SelectOptionAsync(new[] { "4" });//order

		Console.WriteLine("load shopping cart");
		//click > button
		//await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = ">" }).ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("//*[@id='btnOrder']/preceding-sibling::button").ClickAsync(locatorClickOptions);

		try
		{
			//wait for page
			Console.WriteLine("assert shopping cart has 1 items");
			await Expect(Page.Locator("#shoppingCartTitle")).ToContainTextAsync("Shopping Cart (1)");
			await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Transfer Shopping Cart" }).First).ToBeVisibleAsync();

			Console.WriteLine("check shopping cart items");
			await Expect(Page.Locator(".product-list__column.product-list__title").First).ToContainTextAsync("Req form offer 1");
		}
		catch { }
		//assert supplier
		if (Environment != "PROD")
		{
			await Expect(Page.Locator(".product-list__column.product-list__supplier").First).ToContainTextAsync(TC02_ASSERT_SUPPLIER1);
		}

		//go back to search list check status is now item added to cart
		await Page.GotoAsync(url, pageGotoOptions);
		await Page.WaitForURLAsync(url, pageWaitForUrlOptions);

		//assert search has a quick quote link for this view/company
		await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestPositionRequestFormTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);

		//wait for results
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert 1 result
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");

		//assert status of the request we just published is updated to the status of "Item has been added to the Cart"
		Console.WriteLine("assert status is now 'Item has been added to the Cart' for: " + requestPositionRequestFormTransactionName);
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblStatus")).ToContainTextAsync("Item has been added to the Cart");
	}

	[Test, Order(33)]
	[Category("EmailTests")]
	public void TC33_QQB_New_Request_Email_For_Supplier_Request_Form()
	{
		//this is the third batch of email tests, lets wait for 1 minute to allow the email to be delivered
		try
		{
			System.Threading.Thread.Sleep(180000);
		}
		catch { }
		//test based on devops test caseid 181490 : (Position form) New request email for supplier
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303

		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestPositionRequestFormTransactionName = "";
		//requestIdRequestForm = "";
		//testStartSecondsSinceEpoch = "1709650575";  //after: 1709650575  Tuesday, March 5, 2024 2:56:15 PM

		//////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("33: TC33_QQB_New_Request_Email_For_Supplier_Request_Form");
		Console.WriteLine("request" + requestPositionFormTransactionName);
		Console.WriteLine("request id " + requestIdRequestForm.ToString());

		bool testPassed = true;
		Console.WriteLine("instantiate gmail api service");
		try
		{
			UserCredential credential;
			// Load client secrets.
			using (var stream =
						 new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
			{
				/* The file token.json stores the user's access and refresh tokens, and is created
				 automatically when the authorization flow completes for the first time. */
				string credPath = "token.json";
				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
						GoogleClientSecrets.FromStream(stream).Secrets,
						Scopes,
						"user",
						CancellationToken.None,
						new FileDataStore(credPath, true)).Result;
				Console.WriteLine("Credential file saved to: " + credPath);
			}

			// Create Gmail API service.
			bool connected = false;
			GmailService? service = null;
			IList<Message> messages = new List<Message>();
			int connectionAttempt = 0;
			while (!connected && connectionAttempt < 10)
			{
				Console.WriteLine("create gmail service, attempt: " + (connectionAttempt + 1).ToString());
				try
				{
					if (service == null)
					{
						service = new GmailService(new BaseClientService.Initializer
						{
							HttpClientInitializer = credential,
							ApplicationName = ApplicationName
						});
					}

					UsersResource.MessagesResource.ListRequest requestMessage = service.Users.Messages.List("easyordertest@gmail.com");
					requestMessage.LabelIds = "INBOX";
					requestMessage.IncludeSpamTrash = false;
					//Change Request message template has Request-number:
					//Offer received and new request templates have Request ID:
					//Request rejected template has Request-id:
					requestMessage.Q = $"after:{testStartSecondsSinceEpoch} AND subject:({TC_REQUEST_RECEIVED_EMAIL_SUBJECT}) AND {requestPositionRequestFormTransactionName}";
					//is it better to search for requestPositionFormTransactionName
					Console.WriteLine("Q. " + requestMessage.Q);
					messages = requestMessage.Execute().Messages;
					connected = true;
				}
				catch (Exception ex)
				{
					Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
					if (service == null)
					{
						Console.WriteLine("service is null");
					}

					if (credential == null)
					{
						Console.WriteLine("credentials are null");
					}
					Console.WriteLine("gmail exception: " + ex.Message);
					if (ex.InnerException != null)
					{
						Console.WriteLine(ex.InnerException.Message);
					}
					connectionAttempt++;
					Console.WriteLine("gmail service instantiation attempt: " + connectionAttempt.ToString());
				}
			}

			//search for new request emails for request requestPositionFormTransactionName
			if (Environment == "QA")
			{
				Console.WriteLine("expecting 3 new request received email messages for " + requestPositionRequestFormTransactionName + " ID: " + requestIdRequestForm);
			}

			if (Environment == "PROD")
			{
				Console.WriteLine("expecting 1 new request received email message for " + requestPositionRequestFormTransactionName + " ID: " + requestIdRequestForm);
			}

			if (messages == null || messages.Count == 0)
			{
				Console.WriteLine("No new request received email messages found for request: " + requestPositionRequestFormTransactionName);
				if (messages == null)
				{
					Console.WriteLine("messages null");
					testPassed = false;
				}
				else
				{
					Console.WriteLine("messages count == 0");
					testPassed = false;
				}
			}
			else
			{
				if (Environment == "QA")
				{
					//expected 3 messages on qa

					Console.WriteLine("expecting 3 new request received email messages for " + requestPositionRequestFormTransactionName + " ID: " + requestIdRequestForm);
					Console.WriteLine("found " + messages.Count.ToString() + " email message(s)");
					Assert.That(messages.Count == 3);
				}

				if (Environment == "PROD")
				{
					//expected 1, note one is in omnicontentuser+ email box
					Console.WriteLine("expecting 1 new request received email message for " + requestPositionRequestFormTransactionName + " ID: " + requestIdRequestForm);
					Console.WriteLine("found " + messages.Count.ToString() + " email message(s)");
					Assert.That(messages.Count == 1);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null)
			{
				Console.WriteLine(ex.InnerException.Message);
			}
			throw ex;
		}

		Assert.That(testPassed == true);
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(34)]
	[Category("QQTests3")]
	async public Task TC34_QQB_Supplier_Chooses_Classification_Create_A_Request()
	{
		try
		{
			Console.WriteLine("34: TC34_QQB_Supplier_Chooses_Classification_Create_A_Request");
			//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
			//corresponds with test case 183701 "(Supplier pick classification) Create request" in dev ops qq prod smoke tests
			//preconditions:
			/*
			Following QQ properties are ASSIGNED AND disabled
			BUYER_CHOOSES_CLASSIFICATION
			ENABLE_DATASHEET_CLASSIFICATION
			*/
			List<string> propertiesToSetTrue = new List<string>();
			List<string> propertiesToSetFalse = new List<string>();

			////////////FOR TESTING ////////////////////////////////
			propertiesToSetFalse.Add("BUYER_CHOOSES_CLASSIFICATION");
			propertiesToSetFalse.Add("ENABLE_DATASHEET_CLASSIFICATION");
			///////////////////////////
			
			if (String.IsNullOrEmpty(_browserName))
			{
				_browserName = Browser.BrowserType.Name;
			}

			Console.WriteLine("Running test QQB_Supplier_Chooses_Classification... Setting EasyOrder Property Preconditions");
			//configure easy order property preconditions for the company with the catalogid TESTCUSTCDO-0004 i.e.
			//await QQB_ConfigureEasyOrderPropertiesForCompany(propertiesToSetTrue, propertiesToSetFalse, TC34_COMPANYID);//set BUYER_CHOOSES_CLASSIFICATION = false, ENABLE_DATASHEET_CLASSIFICATION = false

			string url = SEARCHURL2;
			DateTime today = DateTime.Now;
			string CurrentDate = $"{today.Year}{today.Month}{today.Day}{today.Hour}{today.Minute}";
			string transactionID = "PW_Auto_Sup_Class_" + _browserName + "_" + CurrentDate;//this transactionid will be used in other tests in this suite!
			string commentDate = $"{today.Year}{today.Month}{today.Day}";
			requestSupplierClassificationTransactionName = transactionID;//allows this request to be opened in another test and be refwrenced during teardown

			Console.WriteLine("Creating request...  " + requestToRejectTransactionName);
			PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 180000, State = WaitForSelectorState.Visible };
			LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
			LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };
			LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
			LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
			LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
			PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };
			PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };

			Console.WriteLine("login to search:" + DateTime.Now.ToLongTimeString());
			bool loggedin = false;
			int attempts = 0;
			while (loggedin == false && attempts < 10)
			{
				try
				{
					await Page.GotoAsync(url, pageGotoOptions);
					await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
					loggedin = true;
				}
				catch (Exception ex)
				{
					attempts++;
					Console.WriteLine("exception: " + ex.Message);
					Console.WriteLine(DateTime.Now.ToString());
					Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
					//seeing a lot of errors of type
					/*
							Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
					*/
				}
			}
			if (loggedin == false && attempts >= 10)
			{
				DateTime timeRightNow = DateTime.Now;
				string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
				await Page.ScreenshotAsync(new()
				{
					FullPage = true,
					Path = downloadPath + "TC34_LoginError_" + FileCurrentDate + ".png"
				});
			}

			Console.WriteLine("page: " + Page.Url);
			
			//assert search has a quick quote link for this view/company
			await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

			var iframeElement = await Page.Locator("#qqFrame").ElementHandleAsync();
			var frame = await iframeElement.ContentFrameAsync();
			if (frame != null && frame.Url != "")
			{
				Console.WriteLine("qqFrame Url : " + frame.Url);
				//await frame.WaitForURLAsync("https://portal.hubwoo.com/srvs/easyorder/RequestList2007.aspx");
			}

			//this is extremely slow in qa
			if (Environment == "QA")
			{
				await Task.Delay(3000);
			}
			Console.WriteLine("wait for request list page");
			//assert on request list page quick filter
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
			Console.WriteLine("wait for qqb request list");
			await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

			//get current request count
			var currentrequestCount = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount").TextContentAsync();

			//click create request redirected to DataSheetChoose.asp
			Console.WriteLine("click Create a request");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbCreateRequestTop").ClickAsync(locatorClickOptions);

			//////////////////Simple Datasheet

			iframeElement = await Page.Locator("#qqFrame").ElementHandleAsync();
			frame = await iframeElement.ContentFrameAsync();
			if (frame != null && frame.Url != "")
			{
				Console.WriteLine("qqFrame Url : " + frame.Url);
				//await frame.WaitForURLAsync("https://portal.hubwoo.com/srvs/easyorder/DataSheetChoose.aspx");
			}

			if (Environment == "QA")
			{
				await Task.Delay(3000);
			}

			//assert on datasheetchoose page
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Product Group", locatorToContainTextOption);

			//expand product group QA Tests (qa)  / PROD Tests (Prod) use the unique id on the image as a locator
			Console.WriteLine("expand product group");
			await Page.FrameLocator("#qqFrame").Locator(TC34_PRODUCT_GROUP).ClickAsync();

			//select simple datasheet
			//await Page.FrameLocator("#qqFrame").GetByLabel("Simple datasheet").CheckAsync();
			//fails because not unique 2 examples also on the suppliers tab of datasheet choose
			//should use the img id as it is unique either datasheet header.Id or supplier.Id
			//	<img id="img_<%=header.Id %>" src="./Design2007/img/icons/plus.jpg" alt="+" />
			Console.WriteLine("select simple datasheet");
			await Page.FrameLocator("#qqFrame").Locator(TC34_SIMPLE_DATASHEET_SELECTOR).ClickAsync();

			//click choose
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Choose" }).ClickAsync(locatorClickOptions);

			//assert on requestcreate page via the request details breadcrumb

			Console.WriteLine("wait for request details page to load...");
			await Expect(Page.FrameLocator("#qqFrame").GetByText("Request Details")).ToBeVisibleAsync(locatorVisibleAssertion);

			//assert that the selected product group is simple datasheet
			//await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbProductGroup")).ToContainTextAsync("Simple datasheet");
			//wont work there is no innertext ,the data is stored in the value attribute of the control
			var readonlyInput = Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbProductGroup");

			await Expect(readonlyInput).ToBeDisabledAsync();

			var selectedDataSheet = await readonlyInput.GetAttributeAsync("value");

			Assert.That(selectedDataSheet == "Simple datasheet");

			//assert transactionid is empty  this fails in qa, transaction id is autogenerated
			//await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbTransactionNumber")).ToBeEmptyAsync();

			//assert that no supplier selected
			await Expect(Page.FrameLocator("#qqFrame").Locator("#selectSelectedSuppliers")).ToBeEmptyAsync();

			//assert that available suppliers contains 2 suppliers
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repExistingSuppliers_ctl00_Option1")).ToContainTextAsync(TC34_ASSERT_SUPPLIER1);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repExistingSuppliers_ctl01_Option1")).ToContainTextAsync(TC34_ASSERT_SUPPLIER2);

			//assert the number of default empty request positions
			//5 default empty positions in qa/ 2 empty default positions in prod
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblPositionCount")).ToContainTextAsync(TC34_EMPTY_REQUEST_POSITIONS);
			Console.WriteLine("assert request positions");
			if (Environment == "QA")
			{
				//remove 2 positions
				await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl04_ibRemovePositionEdit").ClickAsync(locatorClickOptions);
				await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl04_lbRemovePositionEdit").ClickAsync(locatorClickOptions); 
				await Task.Delay(3000);

				await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl04_ibRemovePositionEdit").ClickAsync(locatorClickOptions);
				await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl04_lbRemovePositionEdit").ClickAsync(locatorClickOptions);
				await Task.Delay(3000);

				//assert position count is 3
				await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblPositionCount")).ToContainTextAsync("3");
			}

			if(Environment == "PROD")
			{
				//add additional request position
				await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbAddPositionBottom").ClickAsync(locatorClickOptions);
				await Task.Delay(3000);
				//assert there are 3 positions now
				await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblPositionCount")).ToContainTextAsync("3 )");

			}

			//assert selected suppliers is empty
			await Expect(Page.FrameLocator("#qqFrame").Locator("#selectSelectedSuppliers")).ToBeEmptyAsync();

			//add transaction number and internal / external comments
			Console.WriteLine("complete request details");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbTransactionNumber").FillAsync(transactionID);

			//comment controls need to be clicked before textbox is available
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Add External Comments (" }).ClickAsync();
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbExternalComment").FillAsync("external comment " + commentDate);

			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Add Internal Comments" }).ClickAsync();
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbInternalComment").FillAsync("internal comment " + commentDate);

			//add both suppliers
			await Page.FrameLocator("#qqFrame").Locator("#selectExistingSuppliers").SelectOptionAsync(new[] { TC34_SELECT_SUPPLIER1_ID });
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync();
			await Page.FrameLocator("#qqFrame").Locator("#selectExistingSuppliers").SelectOptionAsync(new[] { TC34_SELECT_SUPPLIER2_ID });
			await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync(locatorClickOptions);


			//added above expect as the next line is skipped and performs the description input but doesn't perform the title input???
			//test runs differently if headed/headless and run vs debug
			Console.WriteLine("add short description pos1");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbShortDescription").FillAsync("item title pos1");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbLongDescription").FillAsync("item description pos1");

			//assert that NO class textbox or show classification popup icon
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_ibShowClassification")).ToHaveCountAsync(0);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbEclassCode")).ToHaveCountAsync(0);

			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_ibShowClassification")).ToHaveCountAsync(0);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbEclassCode")).ToHaveCountAsync(0);

			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_ibShowClassification")).ToHaveCountAsync(0);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_tbEclassCode")).ToHaveCountAsync(0);

			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbQuantity").FillAsync("10");

			//complete offer position 2
			Console.WriteLine("complete offer pos2");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbShortDescription").FillAsync("item title pos2");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbLongDescription").FillAsync("item description pos2");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbQuantity").FillAsync("20");

			await Page.WaitForTimeoutAsync(3000);

			//complete offer position 3
			Console.WriteLine("complete offer pos3");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_tbShortDescription").FillAsync("item title pos3");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_tbLongDescription").FillAsync("item description pos3");
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repRequestPosition_ctl03_tbQuantity").FillAsync("30");

			await Page.WaitForTimeoutAsync(3000);
			
			if (Environment == "QA")
			{
				Console.WriteLine("Select shipping address");
				//need to select a shipping address
				await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlLocationShippingAddress").SelectOptionAsync(new[] { "1234" });//Bd Albert 1er, 98000 Monte Carlo, Mónaco
			}
			Console.WriteLine("save request");
			//save request
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSaveRequestTop").ClickAsync(locatorClickOptions);

			//wait for page to refresh
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblStatus1").WaitForAsync(locatorWaitForOptions);

			//check request is saved
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblStatus1")).ToContainTextAsync("Successfully saved.");

			//send to supplier
			Console.WriteLine("Send request to suppliers");
			//send
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_hrefPublishTop").ClickAsync(locatorClickOptions);

			//send button is displayed
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbPublishButtonTop")).ToBeVisibleAsync(locatorVisibleAssertion);

			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbPublishButtonTop").ClickAsync(locatorClickOptions);

			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Request list");

			//is the request we just created in the list, search for it
			Console.WriteLine("search for " + requestSupplierClassificationTransactionName);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").ClickAsync(locatorClickOptions);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestSupplierClassificationTransactionName);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);

			//await Page.PauseAsync();//causes the playwright test inspector to launch even when not debugging and cause pause of the run
			//add delay
			await Task.Delay(3000);


			//wait for results page on request list page
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount").WaitForAsync(locatorWaitForOptions);

			//assert 1 result 
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");
			//assert search result matches with the transaction number we saved
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblTransactionNumber")).ToContainTextAsync("PW_Auto_Sup_Class_");
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblTransactionNumber")).ToContainTextAsync(transactionID);

			//capture the requestIdSupplierChoosesClassification
			var newRequestId = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblRequestID").TextContentAsync();
			requestIdSupplierChoosesClassification = newRequestId;


			Console.WriteLine("new request id:" + requestIdSupplierChoosesClassification);
		}
		catch (Exception ex)
		{
			Console.WriteLine("exception TC34_QQB_Supplier_Chooses_Classification");
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
			//screenshot
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC34_" + requestSupplierClassificationTransactionName + "Supplier_Chooses_Classification_Exception.png"
			});
			//note: the nunit test runner , the whole suite of tests does not stop when one fails which
			////appears to happen in the playwright test runner, so perhaps softassert is more required in node.js playwright??
			throw ex;
		}
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(35)]
	[Category("QQTests3")]
	async public Task TC35_QQB_Supplier_Chooses_Classification_Buyer_Extends_Sent_Request()
	{
			Console.WriteLine("35: TC35_QQB_Supplier_Chooses_Classification_Buyer_Extends_Sent_Request");
			//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
			if (String.IsNullOrEmpty(_browserName))
			{
				_browserName = Browser.BrowserType.Name;
			}

			//////////////////////////////////////////////////////////////////////////////
			//TODO COMMENT OUT BEFORE PROD TESTING
			//requestSupplierClassificationTransactionName = "PW_Auto_Sup_Class_chromium_202412251743";
			//////////////////////////////////////////////////////////////////////////////
			string url = SEARCHURL2;
			DateTime today = DateTime.Now;
			Console.WriteLine("Extending request...  " + requestToRejectTransactionName);
			PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 180000, State = WaitForSelectorState.Visible };
			LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
			LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };
			LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
			LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
			LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
			PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };
			PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };

		Console.WriteLine("login to search:" + DateTime.Now.ToLongTimeString());
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loggedin = true;
			}
			catch (Exception ex)
			{
				attempts++;
				Console.WriteLine("exception: " + ex.Message);
				Console.WriteLine(DateTime.Now.ToString());
				Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
				//seeing a lot of errors of type
				/*
						Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
				*/
			}
		}
		if (loggedin == false && attempts >= 10)
		{
			DateTime timeRightNow = DateTime.Now;
			string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC35_LoginError_" + FileCurrentDate + ".png"
			});
		}

		Console.WriteLine("page: " + Page.Url);

		//assert search has a quick quote link for this view/company
		await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

		var iframeElement = await Page.Locator("#qqFrame").ElementHandleAsync();
			var frame = await iframeElement.ContentFrameAsync();
			if (frame != null && frame.Url != "")
			{
				Console.WriteLine("qqFrame Url : " + frame.Url);
				//await frame.WaitForURLAsync("https://portal.hubwoo.com/srvs/easyorder/RequestList2007.aspx");
			}

			//this is extremely slow in qa
			await Task.Delay(3000);
			
			Console.WriteLine("wait for request list page");
		//assert on request list page quick filter

		int attempt = 0;
		Boolean requestListVisible = false;
		while (requestListVisible == false && attempt < 5)
		{
			try
			{
				attempt++;
				Console.WriteLine("wait for request list page, attempt: " + attempt.ToString());
				await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
				requestListVisible = true;
			}
			catch (Exception ex)
			{
				Console.WriteLine(DateTime.Now.ToLongTimeString() + " exception: " + ex.Message);
			}
		}
			Console.WriteLine("wait for qqb request list");
			await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

			Console.WriteLine("search for " + requestSupplierClassificationTransactionName);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").ClickAsync(locatorClickOptions);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestSupplierClassificationTransactionName);
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);
			await Page.WaitForLoadStateAsync(LoadState.Load);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			//await Page.PauseAsync();//causes the playwright test inspector to launch even when not debugging and cause pause of the run
			//add delay
			await Task.Delay(3000);


			//wait for results page on request list page
			await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount").WaitForAsync(locatorWaitForOptions);

			//assert 1 result 
			await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");

		//edit
		Console.WriteLine("open request " + requestSupplierClassificationTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_ibEditRequest").ClickAsync(locatorClickOptions);
		//original expiration date is autoset to 7 days from when request was created

		//extend expiration date
		//extend date
		DateTime expiryDate = today.AddDays(28);
		int expiryYear = expiryDate.Year;
		int expiryMonth = expiryDate.Month - 1;//zero index
		int expiryDay = expiryDate.Day;
		Console.WriteLine("Set expiration date to " + expiryDate.ToLongDateString());
		//click calendar

		await Task.Delay(4000);

		await Page.FrameLocator("#qqFrame").Locator(".ui-datepicker-trigger").ClickAsync(locatorClickOptions);

		LocatorSelectOptionOptions locatorSelectOptions = new LocatorSelectOptionOptions { Timeout = 180000 };
		await Page.FrameLocator("#qqFrame").Locator(".ui-datepicker-year").SelectOptionAsync(new SelectOptionValue { Value = expiryYear.ToString() }, locatorSelectOptions);

		await Page.FrameLocator("#qqFrame").Locator(".ui-datepicker-month").SelectOptionAsync(new SelectOptionValue { Value = expiryMonth.ToString() }, locatorSelectOptions);

		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = expiryDay.ToString(), Exact = true }).ClickAsync(locatorClickOptions);

		//confirmation : do you wish to prolong the expiration of this request
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ConfirmationModal")).ToContainTextAsync("Do you wish to prolong the expiration date of this request?");
		Console.WriteLine("click yes on prolong confirmation dialog");
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Yes" }).ClickAsync(locatorClickOptions);

		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_SuccessMessage")).ToContainTextAsync("Expiration date successfully prolonged");
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Expiration date successfully")).ToBeVisibleAsync(locatorVisibleAssertion);

		//view history
		Console.WriteLine("view history");

		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request history")).ToBeVisibleAsync(locatorVisibleAssertion);
		await Page.FrameLocator("#qqFrame").GetByText("Request history").ClickAsync(locatorClickOptions);

		await Task.Delay(4000);
		//assert history panel displayed
		await Expect(Page.FrameLocator("#qqFrame").Locator("#HistoryPanel")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request created")).ToBeVisibleAsync(locatorVisibleAssertion);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request changed").First).ToBeVisibleAsync(locatorVisibleAssertion);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request sent")).ToBeVisibleAsync(locatorVisibleAssertion);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Expiration date of request prolonged").First).ToBeVisibleAsync(locatorVisibleAssertion);
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repHistory_ctl03_lblObjectTypeHeader")).ToBeVisibleAsync(locatorVisibleAssertion);

		//click the first history item create
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repHistory_ctl00_imgHistoryImage").ClickAsync(locatorClickOptions);

		//assert history item is displayed
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repHistory_ctl00_lblDatasheetValue")).ToBeVisibleAsync(locatorVisibleAssertion);
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_repHistory_ctl00_lblDatasheetValue")).ToContainTextAsync("Simple datasheet");

		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(36)]
	[Category("QQTests3")]
	async public Task TC36_QQS_Supplier_Chooses_Classification_Create_Offer()
	{

		//Does the supplier ,when creating offers, see the classification controls in each offerposition?
		//TODO COMMENT OUT LINE BELOW BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestSupplierClassificationTransactionName = "PW_Auto_Sup_Class_chromium_20257191631";

		//////////////////////////////////////////////////////////////////////////////
		///
		//test based on devops test case id 183702 , plan id 125397, test suite id 179303  : (Supplier pick classification) Create offer
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
		Console.WriteLine("36: TC36_QQS_Supplier_Chooses_Classification_Create_Offer");
		string url = PORTAL_LOGIN;
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };
		Console.WriteLine("new request id:" + requestIdSupplierChoosesClassification);
		Console.WriteLine("request :" + requestSupplierClassificationTransactionName);
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				await Page.WaitForLoadStateAsync(LoadState.Load);
				await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
				await Page.GetByPlaceholder("Enter your user name").FillAsync(SUPPLIER_USER1_LOGIN);
				await Page.GetByPlaceholder("Enter your password").FillAsync(SUPPLIER_USER1_PASSWORD);
				await Page.Locator("#signInButtonId").IsEnabledAsync();
				await Page.Locator("#signInButtonId").ClickAsync(locatorClickOptions);
				loggedin = true;
			}
			catch (Exception e)
			{
				attempts++;
				Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
				//seeing a lot of errors of type
				/*
				 Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F
				 Call log:- navigating to "https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F", waiting until "load"
				 */
				Console.WriteLine(e.Message);
			}
		}

		Console.WriteLine("Page: " + Page.Url);
		await Page.WaitForSelectorAsync("//side-bar-item-group[@name='Opportunities']", new PageWaitForSelectorOptions { Timeout = 180000 });
		Console.WriteLine("Page: " + Page.Url);
		var isCookieConsentVisible = await Page.Locator("#cookie-consent-dialog").IsVisibleAsync();
		if (isCookieConsentVisible)
		{
			await Page.GetByRole(AriaRole.Button, new() { Name = "Accept all" }).ClickAsync();
		}

		await Page.Locator("//side-bar-item-group[@name='Opportunities']").ClickAsync(locatorClickOptions);
		await Page.Locator("//side-bar-item[@name='Requests']").ClickAsync(locatorClickOptions);

		//wait for page to load
		Console.WriteLine("wait for easyorder/SupplierRequestList2007.aspx");

		await Expect(Page).ToHaveURLAsync(QQS_REQUEST_LIST_URL, new PageAssertionsToHaveURLOptions { Timeout = 180000 });
		//await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);  //easyorder/SupplierRequestList2007.aspx

		Console.WriteLine("search for " + requestSupplierClassificationTransactionName);
		//search by transaction name
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync();
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestSupplierClassificationTransactionName);
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(3000);

		//wait for results  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData
		await Page.WaitForSelectorAsync("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData");

		//Assert 1 result
		await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager")).ToContainTextAsync("(1 items found)");

		//waitfor url https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx
		Console.WriteLine("wait for easyorder/SupplierRequestList2007.aspx");
		await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);
		//click edit 

		Console.WriteLine("edit " + requestSupplierClassificationTransactionName);
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibEdit").ClickAsync();
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		DateTime today = DateTime.Now;
		string CurrentDate = $"{today.Year}{today.Month}{today.Day}";

		//assert status is requested
		await Expect(Page.Locator("#ctl00_MainContent_lblStatus")).ToContainTextAsync("Requested");

		//add offer reference
		Console.WriteLine("add offerreference");
		await Page.Locator("#ctl00_MainContent_tbReference").FillAsync("Offer_" + CurrentDate, new LocatorFillOptions { Timeout = 180000 });

		//internal comment
		Console.WriteLine("add internal comment");
		await Page.GetByRole(AriaRole.Link, new() { Name = "Internal comment" }).ClickAsync(locatorClickOptions);
		await Page.Locator("#ctl00_MainContent_tbInternalComment").FillAsync("supplier inter comment " + CurrentDate);

		//add comment for customer
		Console.WriteLine("add comment for customer");
		await Page.GetByRole(AriaRole.Link, new() { Name = "Comment for customer" }).ClickAsync(locatorClickOptions);
		await Page.Locator("#ctl00_MainContent_tbExternalComment").FillAsync("comment to customer " + CurrentDate);
		//reject position 1
		Console.WriteLine("reject pos1");
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_anchorRejectOfferPosition").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbRejectPositionComment").ClickAsync(locatorClickOptions);
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_tbRejectPositionComment").FillAsync("test");
		await Page.GetByRole(AriaRole.Cell, new() { Name = "item title pos1 item" }).Locator("#ibSaveRejectMessage").ClickAsync(locatorClickOptions);
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_lbSaveRejectMessageFromMsg").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//add offer for pos 2
		Console.WriteLine("add offer pos2");
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_anchorEditCut").ClickAsync(locatorClickOptions);
		//add pos 2 info
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbArticleNumber").FillAsync("2");
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbDeliveryDays").FillAsync("2");
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbPricePerUnit_BeforeComma").FillAsync("2");

		await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_ibShowClassification")).ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = 180000 });
		await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_tbEclassCode")).ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = 180000 });

		//select classification
		var Page6 = await Page.RunAndWaitForPopupAsync(async () =>
		{
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_ibShowClassification").ClickAsync(locatorClickOptions);
		}, new PageRunAndWaitForPopupOptions { Timeout = 180000 });

		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Expect(Page6.Locator("#tbSearchField")).ToBeVisibleAsync();

		await Page6.GetByRole(AriaRole.Link, new() { Name = TC35_CLASS_CODE_LEVEL1 }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page6.GetByRole(AriaRole.Link, new() { Name = TC35_CLASS_CODE_LEVEL2 }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page6.GetByRole(AriaRole.Link, new() { Name = TC35_CLASS_CODE_LEVEL3 }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page6.GetByRole(AriaRole.Link, new() { Name = TC35_CLASS_CODE_LEVEL4 }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//assert class code in edit box 10101501
		//save pos 2
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_ibPositionCreationSave").ClickAsync(locatorClickOptions);
		//confirm save
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl02_lbPositionCreationSave").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//add offer for pos 3
		Console.WriteLine("add offer pos3");
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl03_anchorEditCut").ClickAsync(locatorClickOptions);
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl03_tbArticleNumber").FillAsync("3");
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl03_tbDeliveryDays").FillAsync("3");
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl03_tbPricePerUnit_BeforeComma").FillAsync("3");

		await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl03_ibShowClassification")).ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = 180000 });
		await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl03_tbEclassCode")).ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = 180000 });


		//select classification
		var Page9 = await Page.RunAndWaitForPopupAsync(async () =>
		{
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl03_ibShowClassification").ClickAsync(locatorClickOptions);
		}, new PageRunAndWaitForPopupOptions { Timeout = 180000 });

		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page.WaitForTimeoutAsync(3000);

		await Expect(Page9.Locator("#tbSearchField")).ToBeVisibleAsync();
		await Page9.GetByRole(AriaRole.Link, new() { Name = TC35_CLASS_CODE_LEVEL1 }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page9.GetByRole(AriaRole.Link, new() { Name = TC35_CLASS_CODE_LEVEL2 }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page9.GetByRole(AriaRole.Link, new() { Name = TC35_CLASS_CODE_LEVEL3 }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page9.GetByRole(AriaRole.Link, new() { Name = TC35_CLASS_CODE_LEVEL4 }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await Page.WaitForTimeoutAsync(3000);

		//save position
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl03_ibPositionCreationSave").ClickAsync(locatorClickOptions);
		//confirm save
		await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl03_lbPositionCreationSave").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//send
		Console.WriteLine("click send offer on bottom action bar");
		await Page.Locator("#ctl00_MainContent_offerActionBarBottom_lblSendAnchorColapsed").ClickAsync(locatorClickOptions);

		Console.WriteLine("click send offer confirmation");
		await Page.Locator("#ctl00_MainContent_offerActionBarBottom_lbSendOffer").ClickAsync(locatorClickOptions);

		await Task.Delay(5000);

		//wait for url https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx
		Console.WriteLine("wait for easyorder/SupplierRequestList2007.aspx");
		await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

		Console.WriteLine("search for " + requestSupplierClassificationTransactionName);
		//search for request , assert new status is sent
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync(locatorClickOptions);
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestSupplierClassificationTransactionName);
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
		await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		Console.WriteLine("wait for easyorder/SupplierRequestList2007.aspx");
		//waitfor url https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx
		await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

		//wait for results  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData
		await Page.WaitForSelectorAsync("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData");

		//Assert 1 result
		await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager")).ToContainTextAsync("(1 items found)");
		Console.WriteLine("assert status is now sent");
		//assert status is now sent
		await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_lblStatus")).ToContainTextAsync("Sent");

		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(37)]
	[Category("QQTests3")]
	async public Task TC37_QQB_Supplier_Chooses_Classification_Buyer_Makes_Change_Request()
	{
		//test based on devops test caseid 183703 :(Supplier pick classification) Create change request
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
		/*

		*/

		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestSupplierClassificationTransactionName = "PW_Auto_Sup_Class_chromium_20257191631";

		//////////////////////////////////////////////////////////////////////////////
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303

		Console.WriteLine("37: TC37_QQB_Supplier_Chooses_Classification_Buyer_Makes_Change_Request");
		Console.WriteLine("new request id:" + requestIdSupplierChoosesClassification);
		Console.WriteLine("request :" + requestSupplierClassificationTransactionName);
		string url = SEARCHURL2;
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 180000, State = WaitForSelectorState.Visible };

		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };

		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };

		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };

		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };
		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };

		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };

		Console.WriteLine("login to search:" + DateTime.Now.ToLongTimeString());
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loggedin = true;
			}
			catch (Exception ex)
			{
				attempts++;
				Console.WriteLine("exception: " + ex.Message);
				Console.WriteLine(DateTime.Now.ToString());
				Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
				//seeing a lot of errors of type
				/*
						Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
				*/
			}
		}
		if (loggedin == false && attempts >= 10)
		{
			DateTime timeRightNow = DateTime.Now;
			string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC37_LoginError_" + FileCurrentDate + ".png"
			});
		}

		Console.WriteLine("page: " + Page.Url);

		//assert search has a quick quote link for this view/company
		await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

		//assert on request list page
		Console.WriteLine("wait for request list page");

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//get current request count
		var currentrequestCount = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount").TextContentAsync();

		//search for the request
		Console.WriteLine("search for " + requestSupplierClassificationTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestSupplierClassificationTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//wait for results
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert 1 result
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");

		//assert current status is answered
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblStatus")).ToContainTextAsync("Answered");//i.e. and offer has been made

		//edit the request
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_ibEditRequest").ClickAsync(locatorClickOptions);

		//wait for request details page to open
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request Details")).ToBeVisibleAsync(locatorVisibleAssertion);
		//	await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_BreadCrumpContent_breadCrump_repBreadCrumpLinks_ctl00_hlLink")).ToContainTextAsync("Request Details");
		Console.WriteLine("expand the first position offer rejection");
		//expand the first position offer rejection
		//await Page.FrameLocator("#qqFrame").Locator(".row-collapsed").First.ClickAsync(locatorClickOptions); // not unique
		//3rd row of positions-table 
		await Page.FrameLocator("#qqFrame").Locator("#positions-table > tbody > tr:nth-child(3) > td:nth-child(3) > div > div:nth-child(1) > div.row-collapsed").ClickAsync(locatorClickOptions);
		Console.WriteLine("click the request for change button");
		//click the request for change button
		await Page.FrameLocator("#qqFrame").Locator("#positions-table > tbody > tr:nth-child(3) > td:nth-child(3) > div > div.bgBlue1 > button > img").ClickAsync(locatorClickOptions);

		//assert modalpopup
		await Expect(Page.FrameLocator("#qqFrame").Locator("#RequestForChangeModal").GetByText("Request for change")).ToBeVisibleAsync(locatorVisibleAssertion);

		await Expect(Page.FrameLocator("#qqFrame").Locator("#RequestForChangeMessage")).ToBeVisibleAsync(locatorVisibleAssertion);

		DateTime today = DateTime.Now;
		string CurrentDate = $"{today.Year}{today.Month}{today.Day}";

		await Page.FrameLocator("#qqFrame").Locator("#RequestForChangeMessage").ClickAsync(locatorClickOptions);
		//set request for change message as "change request test yyymmdd"
		await Page.FrameLocator("#qqFrame").Locator("#RequestForChangeMessage").FillAsync("change request test" + CurrentDate);
		Console.WriteLine("save rfc");
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync(locatorClickOptions);

		//assert modal popup
		await Expect(Page.FrameLocator("#qqFrame").Locator("#SendRequestForChangeModal").GetByText("Request for change")).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("send rfc");
		await Expect(Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Send" })).ToBeVisibleAsync(locatorVisibleAssertion);
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync(locatorClickOptions);
		Console.WriteLine("assert ChangeRequested status");
		//assert status on request details page has been modified to ChangeRequested
		await Expect(Page.FrameLocator("#qqFrame").Locator("#aspnetForm")).ToContainTextAsync("ChangeRequested");

		//go back to request list
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Link, new() { Name = "Requests" }).ClickAsync(locatorClickOptions);
		//wait for page

		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//reset search
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		//search for request to offer
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestSupplierClassificationTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//wait for results
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert 1 result
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");

		//ASSERT STATUS IS ChangeRequested AS EXPECTED
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblStatus")).ToContainTextAsync("ChangeRequested");
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(38)]
	[Category("QQTests3")]
	async public Task TC38_QQS_Supplier_Chooses_Classification_Supplier_Updates_Offer()
	{
		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestSupplierClassificationTransactionName = "PW_Auto_Sup_Class_chromium_20257191631";

		//////////////////////////////////////////////////////////////////////////////

		try
		{
			//assume that BUYER_CHOOSES_CLASSIFICATION is true here
			//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303
			//based on devops test case id 183704 : (Supplier pick classification) Update offer
			Console.WriteLine("38: TC38_QQS_Supplier_Chooses_Classification_Supplier_Updates_Offer");
			Console.WriteLine("new request id:" + requestIdSupplierChoosesClassification);
			Console.WriteLine("request :" + requestSupplierClassificationTransactionName);
			string url = PORTAL_LOGIN;
			PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };

			PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
			LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
			LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
			LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
			await Page.GotoAsync(url, pageGotoOptions);
			await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
			//supplier has recieved new requests from buyer
			await Page.WaitForLoadStateAsync(LoadState.Load);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Page.GetByPlaceholder("Enter your user name").FillAsync(SUPPLIER_USER2_LOGIN);
			await Page.GetByPlaceholder("Enter your password").FillAsync(SUPPLIER_USER2_PASSWORD);
			await Page.Locator("#signInButtonId").IsEnabledAsync();
			await Page.Locator("#signInButtonId").ClickAsync(locatorClickOptions);

			//wait for page to load https://portal.hubwoo.com/main/
			//await Page.WaitForURLAsync("https://portal.hubwoo.com/main/", pageWaitForUrlOptions);  //this fails for some reason

			await Page.WaitForSelectorAsync("//side-bar-item-group[@name='Opportunities']", new PageWaitForSelectorOptions { Timeout = 180000 });

			await Page.Locator("//side-bar-item-group[@name='Opportunities']").ClickAsync(locatorClickOptions);
			await Page.Locator("//side-bar-item[@name='Requests']").ClickAsync(locatorClickOptions);

			await Task.Delay(2000);

			//wait for page to load
			await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);
			Console.WriteLine("search for  " + requestSupplierClassificationTransactionName);
			//search by transaction name
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync(locatorClickOptions);
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestSupplierClassificationTransactionName);
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync(locatorClickOptions);

			await Task.Delay(3000);

			//wait for results  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData
			await Page.WaitForSelectorAsync("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData");

			//Assert 1 result
			await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager")).ToContainTextAsync("(1 items found)");

			//waitfor url https://portal.hubwoo.com/srvs/easyorder/SupplierRequestList2007.aspx
			await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);

			Console.WriteLine("assert status is change requested");
			//assert status is ChangeRequested
			await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_lblStatus")).ToContainTextAsync("ChangeRequested");

			//click edit 
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_ibEdit").ClickAsync(locatorClickOptions);

			//expect first offer position to have text rejected
			await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_Label37")).ToContainTextAsync("Rejected");

			//expand the rejected offer
			Console.WriteLine("expand rejected offer");//ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_anchorOfferPositionCutRejected
			await Page.GetByRole(AriaRole.Link, new() { Name = "Rejected test" }).ClickAsync(locatorClickOptions);

			DateTime today = DateTime.Now;
			string CurrentDate = $"{today.Year}{today.Month}{today.Day}";
			await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_Label25")).ToContainTextAsync("change request test" + CurrentDate);

			//edit the position ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_ibCutRejectedEdit
			//await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_ibCutRejectedEdit").ClickAsync(locatorClickOptions);//fails?

			Console.WriteLine("Create offer position");//ctl00_MainContent_repRequestPosition_ctl01_divEditOfferCut
			await Page.GetByRole(AriaRole.Button, new() { Name = "Create offer position" }).ClickAsync();

			//assert article no is visible
			await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_lblArticleEdit")).ToBeVisibleAsync(locatorVisibleAssertion);
			await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbDeliveryDays")).ToBeVisibleAsync(locatorVisibleAssertion);
			await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbAmount")).ToBeVisibleAsync(locatorVisibleAssertion);
			await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbShortDescription")).ToBeVisibleAsync(locatorVisibleAssertion);
			await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbLongDescription")).ToContainTextAsync("item description pos1");

			//complete offer details
			Console.WriteLine("complete offer position details");
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbShortDescription").FillAsync("Rej to Offer");
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbLongDescription").FillAsync("From rejected to position with offer");
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbArticleNumber").FillAsync("R2O" + CurrentDate);


			//assert classification is shown

			await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_ibShowClassification")).ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = 180000 });
			await Expect(Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbEclassCode")).ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = 180000 });

			//select classification
			var Page6 = await Page.RunAndWaitForPopupAsync(async () =>
			{
				await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_ibShowClassification").ClickAsync(locatorClickOptions);
			}, new PageRunAndWaitForPopupOptions { Timeout = 180000 });
			await Expect(Page6.Locator("#tbSearchField")).ToBeVisibleAsync();

			await Page6.GetByRole(AriaRole.Link, new() { Name = TC35_CLASS_CODE_LEVEL1 }).ClickAsync(locatorClickOptions);
			await Page6.GetByRole(AriaRole.Link, new() { Name = TC35_CLASS_CODE_LEVEL2 }).ClickAsync(locatorClickOptions);
			await Page6.GetByRole(AriaRole.Link, new() { Name = TC35_CLASS_CODE_LEVEL3 }).ClickAsync(locatorClickOptions);
			await Page6.GetByRole(AriaRole.Link, new() { Name = TC35_CLASS_CODE_LEVEL4 }).ClickAsync(locatorClickOptions);

			//delivery days
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbDeliveryDays").FillAsync("7");
			//quantity
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbAmount").FillAsync("7");
			//unit price
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_tbPricePerUnit_BeforeComma").FillAsync("707");

			LocatorSelectOptionOptions locatorSelectOptions = new LocatorSelectOptionOptions { Timeout = 180000 };
			//ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_ddlUnit
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_ddlUnit").SelectOptionAsync(new SelectOptionValue { Value = "LTR" }, locatorSelectOptions);//label = Liter

			Console.WriteLine("Set expiration date");

			//pick date 28 days from now
			DateTime expiryDate = today.AddDays(28);
			int expiryYear = expiryDate.Year;
			int expiryMonth = expiryDate.Month - 1;//zero index
			int expiryDay = expiryDate.Day;
			Console.WriteLine("Set expiration date to " + expiryDate.ToLongDateString());
			//click calendar
			//expiration date for the previously rejetced position #ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_divEditOfferPositionDetails > div:nth-child(1) > table > tbody > tr:nth-child(7) > td.OutTblForm > img
			await Page.GetByRole(AriaRole.Img, new() { Name = "..." }).ClickAsync(locatorClickOptions);

			await Page.Locator(".ui-datepicker-year").SelectOptionAsync(new SelectOptionValue { Value = expiryYear.ToString() }, locatorSelectOptions);

			await Page.Locator(".ui-datepicker-month").SelectOptionAsync(new SelectOptionValue { Value = expiryMonth.ToString() }, locatorSelectOptions);

			//await Page.PauseAsync();

			await Page.Locator("#ui-datepicker-div").Page.GetByRole(AriaRole.Link, new() { Name = expiryDay.ToString(), Exact = true }).ClickAsync(locatorClickOptions);

			//this fails if the day string is not unique e.g. 2 maps to 2, 12,20,21,22,23,24 etc so need to use the Exact option for the PageGetByRoleOptions

			Console.WriteLine("submit offer");
			//save 
			await Page.GetByRole(AriaRole.Button, new() { Name = "Submit" }).ClickAsync(locatorClickOptions);

			//save
			await Page.Locator("#ctl00_MainContent_repRequestPosition_ctl01_repSupplierOfferPositions_ctl00_lbSaveEditFromMsg").ClickAsync();
			//bug here on prod, which has been fixed oc-9260
			//send
			//await Page.Locator("#ctl00_MainContent_offerActionBarTop_divSendOffer").GetByRole(AriaRole.Link, new() { Name = "Send" }).ClickAsync(locatorClickOptions);
			Console.WriteLine("click send offer on bottom action bar");
			await Page.Locator("#ctl00_MainContent_offerActionBarBottom_lblSendAnchorColapsed").ClickAsync(locatorClickOptions);

			if (Environment == "QA")
			{
				await Task.Delay(3000);
				await Page.ScreenshotAsync(new()
				{
					FullPage = true,
					Path = downloadPath + "TC38_" + requestSupplierClassificationTransactionName + "Supplier_Chooses_Classification_Supplier_Creates_Offer_ClickSend.png",
					Timeout = 180000
				});
			}
			Console.WriteLine("click send offer confirmation");
			
			//send confirmation
			//await Page.Locator("#ctl00_MainContent_offerActionBarTop_lbSendOffer").ClickAsync(locatorClickOptions);
			await Page.Locator("#ctl00_MainContent_offerActionBarBottom_lbSendOffer").ClickAsync(locatorClickOptions);
			//wait for page to load
			if (Environment == "QA")
			{
				await Page.WaitForURLAsync(QQS_REQUEST_LIST_URL, pageWaitForUrlOptions);
			}

			//search by transaction name
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").ClickAsync(locatorClickOptions);
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_tbSearchingKey").FillAsync(requestSupplierClassificationTransactionName);
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_ctl00_ddlChosenColumn").SelectOptionAsync(new[] { "RequestTransactionNumber" });
			await Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_btnSearch").ClickAsync(locatorClickOptions);
			await Page.WaitForLoadStateAsync(LoadState.Load);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			await Task.Delay(3000);

			//wait for results  ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData
			await Page.WaitForSelectorAsync("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData");

			//Assert 1 result
			await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl01_cgvLabelPager")).ToContainTextAsync("(1 items found)");

			Console.WriteLine("assert status is now sent");
			//assert status is now sent
			await Expect(Page.Locator("#ctl00_MainContent_fcAdvancedFilterControl_cgvRequestListData_ctl03_lblStatus")).ToContainTextAsync("Sent");
			Console.WriteLine("test step complete");
		}
		catch (Exception ex)
		{
			Console.WriteLine("exception TC38_QQB_Supplier_Chooses_Classification_Supplier_Updates_Offer");
			Console.WriteLine(ex.Message);
			if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);
			//screenshot
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC38_" + requestSupplierClassificationTransactionName + "QQB_Supplier_Chooses_Classification_Supplier_Updates_Offer_Exception.png"
			});
			throw ex;
		}
	}

	[Test, Order(39)]
	[Category("QQTests3")]
	async public Task TC39_QQB_Supplier_Chooses_Classification_Buyer_Orders_Offer()
	{
		//test based on devops test caseid 183705 : (Supplier pick classification) Order offer
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179303


		//TODO COMMENT OUT BEFORE PROD TESTING
		//////////////////////////////////////////////////////////////////////////////

		//requestSupplierClassificationTransactionName = "PW_Auto_Sup_Class_chromium_20257191631";

		//////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("39: TC39_QQB_Supplier_Chooses_Classification_Buyer_Orders_Offer");
		Console.WriteLine("new request id:" + requestIdSupplierChoosesClassification);
		Console.WriteLine("request :" + requestSupplierClassificationTransactionName);
		string url = SEARCHURL2;
		PageWaitForSelectorOptions waitOptions = new PageWaitForSelectorOptions { Timeout = 180000, State = WaitForSelectorState.Visible };

		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };

		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };

		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };

		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };

		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };

		LocatorClickOptions QQClickOptions = new LocatorClickOptions { Timeout = 90000 };
		LocatorWaitForOptions locatorWaitForOptions = new LocatorWaitForOptions { Timeout = 180000 };

		Console.WriteLine("login to search:" + DateTime.Now.ToLongTimeString());
		bool loggedin = false;
		int attempts = 0;
		while (loggedin == false && attempts < 10)
		{
			try
			{
				await Page.GotoAsync(url, pageGotoOptions);
				await Page.WaitForURLAsync(url, pageWaitForUrlOptions);
				loggedin = true;
			}
			catch (Exception ex)
			{
				attempts++;
				Console.WriteLine("exception: " + ex.Message);
				Console.WriteLine(DateTime.Now.ToString());
				Console.WriteLine("Problems logging in, attempt: " + attempts.ToString());
				//seeing a lot of errors of type
				/*
						Microsoft.Playwright.PlaywrightException : net::ERR_CONNECTION_RESET at https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE04&VIEW_PASSWD=n8NKf9k4RLr7g&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
				*/
			}
		}
		if (loggedin == false && attempts >= 10)
		{
			DateTime timeRightNow = DateTime.Now;
			string FileCurrentDate = $"{timeRightNow.Year}{timeRightNow.Month}{timeRightNow.Day}{timeRightNow.Hour}{timeRightNow.Minute}";
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC39_LoginError_" + FileCurrentDate + ".png"
			});
		}

		Console.WriteLine("page: " + Page.Url);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		if (await Page.Locator("#shoppingCartTitle").IsVisibleAsync())
		{
			await Page.Locator("//*[@data-testid='removeAllItems']").ClickAsync();
			await Task.Delay(TimeSpan.FromSeconds(1));
			await Page.Locator("//button[contains(@id, 'noty_button') and text()='OK']").ClickAsync();
		}

		//assert search has a quick quote link for this view/company
		await AssertAndClickQuickQuoteAsync(locatorToContainTextOption, QQClickOptions);

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//get current request count
		var currentrequestCount = await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount").TextContentAsync();

		//search for requestSupplierClassificationTransactionName
		Console.WriteLine("search for " + requestSupplierClassificationTransactionName);

		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_tbSearchingText").FillAsync(requestSupplierClassificationTransactionName);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").SelectOptionAsync(new[] { "TransactionNumber" });
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_ddlChosenColumn").ClickAsync(locatorClickOptions);
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lbSearch").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//wait for results
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblQuickFilterReset").WaitForAsync(locatorWaitForOptions);
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request list")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert 1 result
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_lblListResultCount")).ToContainTextAsync("1");

		//assert current status is answered
		//note: if repeated will fail as state wil be Item has been added to the cart!
		await Expect(Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_lblStatus")).ToContainTextAsync("Answered");//i.e. and offer has been made

		//edit the request
		await Page.FrameLocator("#qqFrame").Locator("#ctl00_MainContent_requestGridView_ctl02_ibEditRequest").ClickAsync(locatorClickOptions);

		//wait for request details page to open

		if (Environment == "QA")
		{
			await Task.Delay(3000);
		}

		await Expect(Page.FrameLocator("#qqFrame").GetByText("Request Details")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert status is Answered
		await Expect(Page.FrameLocator("#qqFrame").Locator("#aspnetForm")).ToContainTextAsync("Answered");
		await Expect(Page.FrameLocator("#qqFrame").GetByText("Answered")).ToBeVisibleAsync();

		await Expect(Page.FrameLocator("#qqFrame").Locator("#aspnetForm")).ToContainTextAsync(requestSupplierClassificationTransactionName);

		Console.WriteLine("assert checkboxes are visible");
		//assert checkboxes are visible
		await Expect(Page.FrameLocator("#qqFrame").Locator("(//input[@type='checkbox'])[1]")).ToBeVisibleAsync();

		await Expect(Page.FrameLocator("#qqFrame").Locator("(//input[@type='checkbox'])[2]")).ToBeVisibleAsync();

		await Expect(Page.FrameLocator("#qqFrame").Locator("(//input[@type='checkbox'])[3]")).ToBeVisibleAsync();


		//Select "All" from the drop down list right to header "Quantity"
		Console.WriteLine("select all checkboxes option");
		await Page.FrameLocator("#qqFrame").Locator("select[name=\"position\"]").SelectOptionAsync(new[] { "all" });

		//assert that all 3 checkboxes are checked

		await Expect(Page.FrameLocator("#qqFrame").Locator("(//input[@type='checkbox'])[1]")).ToBeCheckedAsync();

		await Expect(Page.FrameLocator("#qqFrame").Locator("(//input[@type='checkbox'])[2]")).ToBeCheckedAsync();

		await Expect(Page.FrameLocator("#qqFrame").Locator("(//input[@type='checkbox'])[3]")).ToBeCheckedAsync();


		//Select "Order Position" from the drop down list "Actions for selected positions"
		//Click the ">" button next to drop down list
		await Page.FrameLocator("#qqFrame").Locator("#ddlChooseAction").SelectOptionAsync(new[] { "4" });//order

		Console.WriteLine("load shopping cart");
		//click > button
		await Page.FrameLocator("#qqFrame").GetByRole(AriaRole.Button, new() { Name = ">" }).ClickAsync(locatorClickOptions);

		//wait for page
		Console.WriteLine("assert shopping cart has 3 items");
		await Expect(Page.Locator("#shoppingCartTitle")).ToContainTextAsync("Shopping Cart (3)");
		//await Expect(Page.Locator("//*[@role='main']//h1")).ToContainTextAsync("Shopping Cart");
		await Expect(Page.Locator("//button[contains(text(), 'Transfer Shopping Cart')]").First).ToBeVisibleAsync(locatorVisibleAssertion);

		Console.WriteLine("check shopping cart items");
		await Expect(Page.Locator("(//td[@class='product-list__column product-list__title'])[1]")).ToContainTextAsync("Rej to Offer");
		//is there an Nth option?
		await Expect(Page.Locator("(//td[@class='product-list__column product-list__title'])[2]")).ToContainTextAsync("item title pos2");
		await Expect(Page.Locator("(//td[@class='product-list__column product-list__title'])[3]")).ToContainTextAsync("item title pos3");

		//assert supplier filed in the baskte is populated with the supplier name
		//this next assert fails for both of my buyers on prod, but is populated in qa? why?
		if (Environment == "QA")
		{
			await Expect(Page.Locator(".product-list__column.product-list__supplier").First).ToContainTextAsync(TC34_ASSERT_SUPPLIER1);
		}
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}


	#region HelperFunctions
	/// <summary>
	/// 
	/// </summary>
	/// <param name="propertiesToSetTrue">List of easy order properties to set true</param>
	/// <param name="propertiesToSetFalse">list of easy order properties to set false</param>
	/// <param name="CatalogId">The exact catalog id of the company that the properties are associated with </param>
	/// <returns></returns>
	async public Task QQB_ConfigureEasyOrderPropertiesForCompany(List<string> propertiesToSetTrue, List<string> propertiesToSetFalse, string CatalogId)
	{
		LocatorClickOptions locatorClickOptions = new LocatorClickOptions { Timeout = 180000 };
		PageGotoOptions pageGotoOptions = new PageGotoOptions { Timeout = 180000 };
		PageWaitForURLOptions pageWaitForUrlOptions = new PageWaitForURLOptions { Timeout = 180000 };
		LocatorAssertionsToContainTextOptions locatorToContainTextOption = new LocatorAssertionsToContainTextOptions { Timeout = 180000 };
		LocatorAssertionsToBeVisibleOptions locatorVisibleAssertion = new LocatorAssertionsToBeVisibleOptions { Timeout = 180000 };
		await Page.GotoAsync(PORTAL_LOGIN, pageGotoOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page.WaitForURLAsync(PORTAL_LOGIN, pageWaitForUrlOptions);
		await Page.GetByPlaceholder("Enter your user name").FillAsync(CONTENTADMIN_LOGIN);
		await Page.GetByPlaceholder("Enter your password").FillAsync(CONTENTADMIN_PASSWORD);
		await Page.Locator("#signInButtonId").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//assert page
		//await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "The Business Network" })).ToBeVisibleAsync(locatorVisibleAssertion);
		await Expect(Page.Locator("//div[@class='panel-heading']/h4[text()='Company Profile']")).ToBeVisibleAsync(locatorVisibleAssertion);
		(await GetSideBarHeaderTextAsync()).Should().Contain("The Business Network");
		/*
		//NOTE: visual comparison of screenshots from a base"golden" image is not available in c# playwright but is in node.js version ?  https://playwright.dev/docs/test-snapshots
		*/

		//assert url https://portal.hubwoo.com/main/ not https://portal.hubwoo.com/main/Dashboard/
		await Expect(Page).ToHaveURLAsync(new Regex(PORTAL_MAIN_URL));

		//navigate to https://portal.hubwoo.com/srvs/Contentadmin/AdminCompanyFind2007.aspx
		await Page.GotoAsync(CMA_ADMIN_COMPANY_FIND_URL, pageGotoOptions);
		await Page.WaitForURLAsync(CMA_ADMIN_COMPANY_FIND_URL, pageWaitForUrlOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await Expect(Page).ToHaveURLAsync(new Regex(CMA_ADMIN_COMPANY_FIND_URL));

		//assert
		await Expect(Page.Locator("#ctl00_BreadCrumpContent_Label1")).ToBeVisibleAsync(locatorVisibleAssertion);
		await Expect(Page.Locator("#ctl00_BreadCrumpContent_Label1")).ToContainTextAsync("Find Company");
		//search for and edit the buyer company with catalogid 
		await Page.Locator("#ctl00_MainContent_inputSearchTermCustomerId").FillAsync(CatalogId);
		await Page.Locator("#ctl00_MainContent_LinkButton1").ClickAsync(locatorClickOptions);//search

		//assert 1 result but get 3 result when searchng qa for fmkb
		//await Expect(Page.Locator("#ctl00_MainContent_lblResultCount")).ToContainTextAsync("1");

		//assert the first result has catalogid we searched for
		await Expect(Page.Locator("#ctl00_MainContent_catalogTable")).ToContainTextAsync(CatalogId);

		//get result from column 2 of results table i.e. the company name
		//row 3, row 1 empty, row2 header, row 3 first result
		//#ctl00_MainContent_catalogTable > tbody > tr.ssrSearchResultsTableFilledRow
		//#ctl00_MainContent_catalogTable > tbody > tr:nth-child(3)
		//#ctl00_MainContent_catalogTable > tbody > tr.ssrSearchResultsTableFilledRow > td:nth-child(2) > a

		//string companyName = await Page.Locator("#ctl00_MainContent_catalogTable > tbody > tr.ssrSearchResultsTableFilledRow > td:nth-child(2) > a").InnerTextAsync();

		//edit the company found in the search results to enter cma

		//click the cell in the first result row which has the companyname in it
		//await Page.GetByRole(AriaRole.Link, new() { Name = TC02_COMPANY_NAME }).ClickAsync(locatorClickOptions);//fails strict rule as more than one link with the name fmkb

		await Page.Locator("#ctl00_MainContent_catalogTable > tbody > tr:nth-child(3) > td:nth-child(2) > a").ClickAsync(locatorClickOptions);
		//await Page.GetByRole(AriaRole.Link, new() { Name = "Edit" }).ClickAsync(); // this will not work if more than 1 result

		//assert on CMA homepage page AdminRelationEdit https://portal.hubwoo.com/srvs/Contentadmin/AdminRelationEdit2007.aspx?moId=ALNNh8bgWbireb99gDKyGOUPMOzDF_MwJ2jdrzlApYMr
		await Expect(Page.Locator("li").Filter(new() { HasText = "Edit Relations" })).ToBeVisibleAsync(locatorVisibleAssertion);

		//await Expect(Page).ToHaveURLAsync(new Regex("^https://portal.hubwoo.com/srvs/Contentadmin/AdminRelationEdit2007.aspx"));
		await Expect(Page).ToHaveURLAsync(new Regex(ADMIN_RELATIONEDIT_REGEX));

		Console.WriteLine("Current Page: " + Page.Url);
		await Page.GetByRole(AriaRole.Link, new() { Name = "Edit Properties" }).ClickAsync(locatorClickOptions);

		//assert on eo props page https://portal.hubwoo.com/srvs/ContentAdmin/AdminEOPropertiesEdit.aspx?moId=AGYtB_vh1l2OcnBI3IkrVGhiYXFBivSn73Zoi9UOYsBt
		//await Expect(Page).ToHaveURLAsync(new Regex("^https://portal.hubwoo.com/srvs/ContentAdmin/AdminEOPropertiesEdit.aspx"));
		await Expect(Page).ToHaveURLAsync(new Regex(ADMIN_EO_PROPERTIES_EDIT_REGEX));

		await Expect(Page.Locator("#ctl00_ctl00_MainContent_PageHeaderContent_Label1")).ToContainTextAsync("Edit Properties");
		//this page is slightly more complicated in that some properties have dedicated controls and even if a boolean type may not be using Locator("#ctl00_ctl00_MainContent_CenterContent_ddlBooleanValue")e.g. 
		//classification_group Locator("#ctl00_ctl00_MainContent_CenterContent_ddlClassificationGroup")
		//cutomizing_class Locator("#ctl00_ctl00_MainContent_CenterContent_ddlCustomizingClass")
		//datasheet_sort_order Locator("#ctl00_ctl00_MainContent_CenterContent_ddlDatasheetSortOrder")
		//default_tax Locator("#ctl00_ctl00_MainContent_CenterContent_ddlDefaultTax")  
		//default_unit_of_measurement  Locator("#ctl00_ctl00_MainContent_CenterContent_ddlDefaultUnitOfMeasurement")
		//default_user_language Locator("#ctl00_ctl00_MainContent_CenterContent_ddlLanguage")


		//following all use Locator("#ctl00_ctl00_MainContent_CenterContent_ddlBooleanValue")
		/*
		BUYER_CHOOSES_CLASSIFICATION
		CHOOSE_DATASHEET_SUPPLIER
		DISABLE_REQUEST_POS_DEL
		ENABLE_DATASHEET_CLASSIFICATION
		ENABLE_BUYER_ERP_ITEM_NUMBER
		ENABLE_SHIPPING_ADDRESS
		FORMS
		FORMS_EDIT_BUYER
		FORMS_POSITION
		FORMS_POSITION_MANDATORY
		LINK_REQUESTS_TO_SHIPPING_LOCATION
		MICHELIN_LOCAL_CODE_OFFER_PSITION
		MICHELIN_LOCAL_CODE_SUPPLIER_WARNING
		SHOW_SUPPLIER_DESCRIPTION
		*/

		/*
		 ctl00_ctl00_MainContent_CenterContent_valueField
		DEFAULT_LEADBUYER
		DEFAULT_REQUEST_POSITION_COUNT
		EXPIRATION_DATE_DIFFERENCE
		 */
		//locators can be used later and represent and are a way to find element(s) on the page at any moment


		//process the list of properties that should be configured to true
		foreach (string property in propertiesToSetTrue)
		{
			//is the property being handled in the list of available properties i.e. not assigned yet
			bool propertyIsAvailable = false;
			var availableprops = await Page.Locator("#ctl00_ctl00_MainContent_CenterContent_ddlPropertes").TextContentAsync();//returns list of available props needs to use await so rantocompletion status is achieved

			if (!string.IsNullOrEmpty(availableprops) && availableprops.Contains(property))
			{
				propertyIsAvailable = true;
			}

			//is the property in the list of configured properties, i.e. is the property already assigned?
			bool propertyIsAssigned = false;
			try
			{
				//the list of assigned properties are organised in separate nested tables in propertyDetailsPanel
				await Expect(Page.Locator("#ctl00_ctl00_MainContent_CenterContent_propertyDetailsPanel table").Filter(new() { HasText = $"{property} Values:" }).Nth(1)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 100 });
				propertyIsAssigned = true;
			}
			catch { }

			//if the item is an assigned one, select it
			//check it active
			//determine if ctl00_ctl00_MainContent_CenterContent_ddlBooleanValue is visible in a try catch

			if (propertyIsAssigned)
			{
				//select it
				await Page.GetByRole(AriaRole.Link, new() { Name = $"{property}" }).ClickAsync(locatorClickOptions);
			}
			else
			{
				if (propertyIsAvailable)//not assigned
				{
					//add it 
					await Page.Locator("#ctl00_ctl00_MainContent_CenterContent_ddlPropertes").SelectOptionAsync(new[] { property });
					await Page.Locator("#ctl00_ctl00_MainContent_CenterContent_ibAddProperty").ClickAsync(locatorClickOptions);
					//set value
				}
			}

			Console.WriteLine($"Setting {property} true for company  {TC02_COMPANY_NAME}[{CatalogId}] ");

			//TODO confirm if the property to be set is boolean or not based on above notes

			//assert the property has been selected
			await Expect(Page.Locator("#ctl00_ctl00_MainContent_CenterContent_lblSelectedProperty")).ToContainTextAsync($"{property}");
			//select the true option
			await Page.Locator("#ctl00_ctl00_MainContent_CenterContent_ddlBooleanValue").SelectOptionAsync(new[] { "true" });
			//save it
			await Page.Locator("#ctl00_ctl00_MainContent_CenterContent_lbSave").ClickAsync(locatorClickOptions);

			//assert it has been updated
			await Expect(Page.Locator("#ctl00_ctl00_MainContent_CenterContent_ddlBooleanValue")).ToHaveValueAsync("true");
		}


		//process the list of properties that should be configured to false
		foreach (string property in propertiesToSetFalse)
		{
			//is the property added to the list of configured properties?
			Console.WriteLine($"Setting {property} false for company {TC02_COMPANY_NAME}[{CatalogId}] ");
			//is the property being handled in the list of available properties i.e. not assigned yet
			bool propertyIsAvailable = false;
			var availableprops = await Page.Locator("#ctl00_ctl00_MainContent_CenterContent_ddlPropertes").TextContentAsync();//returns list of available props needs to use await so rantocompletion status is achieved

			if (!string.IsNullOrEmpty(availableprops) && availableprops.Contains(property))
			{
				propertyIsAvailable = true;
			}

			//is the property in the list of configured properties, i.e. is the property already assigned?
			bool propertyIsAssigned = false;
			try
			{
				//the list of assigned properties are organised in separate nested tables in propertyDetailsPanel
				await Expect(Page.Locator("#ctl00_ctl00_MainContent_CenterContent_propertyDetailsPanel table").Filter(new() { HasText = $"{property} Values:" }).Nth(1)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 100 });
				propertyIsAssigned = true;
			}
			catch { }

			//if the item is an assigned one, select it
			//check it active
			//determine if ctl00_ctl00_MainContent_CenterContent_ddlBooleanValue is visible in a try catch
			//TODO confirm if the property to be set is boolean or not based on above notes

			if (propertyIsAssigned)
			{
				//select it
				await Page.GetByRole(AriaRole.Link, new() { Name = $"{property}" }).ClickAsync(locatorClickOptions);
			}
			else 
			{
				if (propertyIsAvailable)//not assigned
				{
					//add it 
					await Page.Locator("#ctl00_ctl00_MainContent_CenterContent_ddlPropertes").SelectOptionAsync(new[] { property });
					await Page.Locator("#ctl00_ctl00_MainContent_CenterContent_ibAddProperty").ClickAsync();
					//set value
				}
			}

			//assert the property has been selected
			await Expect(Page.Locator("#ctl00_ctl00_MainContent_CenterContent_lblSelectedProperty")).ToContainTextAsync($"{property}");
			//select the true option
			await Page.Locator("#ctl00_ctl00_MainContent_CenterContent_ddlBooleanValue").SelectOptionAsync(new[] { "false" });
			//save it
			await Page.Locator("#ctl00_ctl00_MainContent_CenterContent_lbSave").ClickAsync(locatorClickOptions);

			//assert it has been updated
			await Expect(Page.Locator("#ctl00_ctl00_MainContent_CenterContent_ddlBooleanValue")).ToHaveValueAsync("false");
		}

		//logoff
		//await Page.Locator(selector: "#userMenu").ClickAsync();
		//await Page.GotoAsync("https://portal.hubwoo.com/srvs/login/logout", pageGotoOptions);
		await Page.GotoAsync(PORTAL_LOGOUT, pageGotoOptions);
	}

	async private Task AssertAndClickQuickQuoteAsync(LocatorAssertionsToContainTextOptions options, LocatorClickOptions clickOptions)
	{
		try
		{
			await Page.WaitForLoadStateAsync(LoadState.Load);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		}
		catch
		{
			// Ignoring any exceptions that might occur
		}

		// Define the locator for the Quick Quote link
		var quickQuoteLocator = Page.Locator("//side-bar-item[@name='Quick Quote']");

		// Assert that the Quick Quote link contains the expected text
		Console.WriteLine("Assert search has a Quick Quote link");
		await Expect(quickQuoteLocator).ToContainTextAsync("Quick Quote", options);

		// Click the Quick Quote link
		Console.WriteLine("Launch Quick Quotes");
		await quickQuoteLocator.ClickAsync(clickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page.MainFrame.WaitForSelectorAsync("#spin_modal_overlay", new FrameWaitForSelectorOptions { State = WaitForSelectorState.Detached });
		await Page.Locator("#results").IsVisibleAsync();
	}

	async private Task<string> GetSideBarHeaderTextAsync()
	{
		// Run JavaScript to access the target element and return its textContent
		var textContent = await Page.EvaluateAsync<string>(@"
        () => {
            // Step 1: Access the side-bar element
            const sideBar = document.querySelector('side-bar[product-name=\""The Business Network\""]');
            if (!sideBar) {
                return null; // side-bar element not found
            }

            // Step 2: Access the shadow root of the side-bar element
            const shadowRoot = sideBar.shadowRoot;
            if (!shadowRoot) {
                return null; // Shadow root of side-bar is null
            }

            // Step 3: Access an element inside the shadow root
            const targetElement = shadowRoot.querySelector('h2.proactis-logo__product-name');
            if (!targetElement) {
                return null; // Target element not found inside the shadow root
            }

            // Return the text content of the target element
            return targetElement.textContent;
        }
    ");

		return textContent; // Return the result to the caller
	}

	async private Task<string> GetTopBarUserTextAsync()
	{
		// Run JavaScript to access the target element and return its textContent
		var textContent = await Page.EvaluateAsync<string>(@"
        () => {
            // Step 1: Access the top-bar element
            const topBar = document.querySelector('top-bar-user-section');
            if (!topBar) {
                return null; // top-bar element not found
            }

            // Step 2: Access the shadow root of the top-bar element
            const shadowRoot = topBar.shadowRoot;
            if (!shadowRoot) {
                return null; // Shadow root of top-bar is null
            }

            // Step 3: Access an element inside the shadow root
            const targetElement = shadowRoot.querySelector('h3.topbar-user-section__user');
            if (!targetElement) {
                return null; // Target element not found inside the shadow root
            }

            // Return the text content of the target element
            return targetElement.textContent;
        }
    ");

		return textContent; // Return the result to the caller
	}


	#endregion
}