
using ClosedXML.Excel;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using System.Globalization;
using FluentAssertions;
using System.Linq;
using ClosedXML;

namespace PlaywrightTests;

[TestFixture]
public partial class CMS_CMB_Tests : PageTest
{
	[Test, Order(16)]
	[Category("CMBTests")]
	async public Task TC16_CMB_Enrichment_Datagroup_Assignment()
	{
		//making an assumption that the data group and user for this test start with the user being assigned to the data group!!
		Console.WriteLine("TC16_CMB_Enrichment_Datagroup_Assignment");
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179308
		//prod cmb smoke tests 179384
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
		await Page.GotoAsync(url, pageGotoOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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

		var isCookieConsentVisible = await Page.Locator("#cookie-consent-dialog").IsVisibleAsync();
		if (isCookieConsentVisible)
		{
			await Page.GetByRole(AriaRole.Button, new() { Name = "Accept all" }).ClickAsync(locatorClickOptions);
		}

		Console.WriteLine(Page.Url);
		DateTime thisTestStarted = DateTime.UtcNow;
		Console.WriteLine("this test started: " + thisTestStarted.ToLongTimeString());
		Console.WriteLine("LOGIN AS " + BUYER_USER1_LOGIN);
		await SignInPortal(PORTAL_MAIN_URL, BUYER_USER1_LOGIN, BUYER_USER1_PASSWORD);

		await Page.GotoAsync(DATA_GROUPS_USER_ASSIGNMENT_URL, pageGotoOptions);
		await Page.WaitForURLAsync(DATA_GROUPS_USER_ASSIGNMENT_URL, pageWaitForUrlOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		Console.WriteLine(Page.Url);

		Console.WriteLine("assert that the datagroup exists: " + DATAGROUP_NAME);
		await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = DATAGROUP_NAME })).ToBeVisibleAsync();


		await Expect(Page.Locator("#uiDataGroupContent")).ToContainTextAsync(DATAGROUP_NAME);

		var rowCount = await Page.Locator("#uiDataGroupContent > tr").CountAsync();
		var datagroup = "";
		int requiredRow = 0;
		for (int i = 1; i < rowCount; i++)
		{
			datagroup = await Page.Locator($"#uiDataGroupContent > tr:nth-child({i}) > td:nth-child(1)").TextContentAsync();
			if (datagroup == DATAGROUP_NAME)
			{
				requiredRow = i;
				break;
			}
		}

		if (requiredRow > 0)
		{
			Console.WriteLine("Click Assign Users");
			await Page.Locator($"#uiDataGroupContent > tr:nth-child({requiredRow}) > td:nth-child(5) > a:nth-child(3)").ClickAsync(locatorClickOptions);
		}
		else
		{
			Console.WriteLine("couldn't find the assign users link for the datagroup");
		}

		await Expect(Page.Locator("#uiUserAssignment")).ToBeVisibleAsync();
		//remove user  TC16_DATAGROUP_ASSIGNMENT_REMOVE_USER
		Console.WriteLine("Assert user we are about to remove is already assigned");
		//assert that the user we want to remove is actually already assigned
		await Expect(Page.Locator("#uiAddedUsers").Locator("option")).ToContainTextAsync(TC16_DATAGROUP_ASSIGNMENT_REMOVE_USER);
		await Page.Locator("#uiAddedUsers").SelectOptionAsync(new[] { TC16_DATAGROUP_ASSIGNMENT_REMOVE_USER });
		Console.WriteLine("Remove assigned user");
		await Page.Locator("#uiUserAssignmentForm a").First.ClickAsync(locatorClickOptions);

		Console.WriteLine("Save assignment change");//#uiUserAssignment > div > div > div.modal-footer > button.btn.btn-primary

		await Page.Locator("#uiUserAssignment > div > div > div.modal-footer > button.btn.btn-primary").ClickAsync(locatorClickOptions);
		//#uiSelectUsers

		Console.WriteLine("Goto download");
		await Page.GotoAsync(BUYER_ADMIN_DOWNLOAD_URL);//use goto instead of clicking download menu item
																									 //https://portal.hubwoo.com/srvs/BuyerCatalogs/export/index
		await Page.WaitForURLAsync(BUYER_ADMIN_DOWNLOAD_URL, pageWaitForUrlOptions);
		Console.WriteLine(BUYER_ADMIN_DOWNLOAD_URL);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		////////////////////////////////////
		//expand the download panel
		////////////////////////////////////
		await Page.GetByRole(AriaRole.Link, new() { Name = "New Download" }).ClickAsync(locatorClickOptions);

		Console.WriteLine("select customer enrichment template");

		await Page.GetByLabel("Template Type:").SelectOptionAsync(new[] { "cus_enrich" });

		await Task.Delay(3000);

		//wait for loadingScreen to disappear
		Console.WriteLine("waiting for loadingScreen to disappear");
		int attempt = 0;
		var isLoadingScreenVisible = await Page.Locator("#loadingScreen").IsVisibleAsync();
		while (isLoadingScreenVisible && attempt < 10)
		{
			try
			{
				await Expect(Page.Locator("#loadingScreen")).Not.ToBeVisibleAsync(locatorVisibleAssertion);
				isLoadingScreenVisible = false;
				break;
			}
			catch
			{
				attempt++;
				isLoadingScreenVisible = await Page.Locator("#loadingScreen").IsVisibleAsync();
			}
		}
		Console.WriteLine("assert data group is not available");
		await Expect(Page.Locator("#uiExportTemplateDataGroup")).Not.ToContainTextAsync(DATAGROUP_NAME);

		Console.WriteLine("Go to Data Group Assignment");
		//https://portal.hubwoo.com/srvs/BuyerCatalogs/admin/DataGroupUserAssignment
		await Page.GotoAsync(DATA_GROUPS_USER_ASSIGNMENT_URL);
		await Page.WaitForURLAsync(DATA_GROUPS_USER_ASSIGNMENT_URL, pageWaitForUrlOptions);

		Console.WriteLine(Page.Url);

		Console.WriteLine("assert that the datagroup exists: " + DATAGROUP_NAME);
		await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = DATAGROUP_NAME })).ToBeVisibleAsync();

		await Expect(Page.Locator("#uiDataGroupContent")).ToContainTextAsync(DATAGROUP_NAME);

		rowCount = await Page.Locator("#uiDataGroupContent > tr").CountAsync();
		datagroup = "";
		requiredRow = 0;
		for (int i = 1; i < rowCount; i++)
		{
			datagroup = await Page.Locator($"#uiDataGroupContent > tr:nth-child({i}) > td:nth-child(1)").TextContentAsync();
			if (datagroup == DATAGROUP_NAME)
			{
				requiredRow = i;
				break;
			}
		}

		if (requiredRow > 0)
		{
			Console.WriteLine("Click Assign Users");
			//#uiDataGroupContent > tr:nth-child(6) > td:nth-child(5) > a:nth-child(3)
			await Page.Locator($"#uiDataGroupContent > tr:nth-child({requiredRow}) > td:nth-child(5) > a:nth-child(3)").ClickAsync(locatorClickOptions);
		}
		else
		{
			Console.WriteLine("couldn't find the assign users link for the datagroup");
		}

		await Expect(Page.Locator("#uiUserAssignment")).ToBeVisibleAsync();
		//remove user  TC16_DATAGROUP_ASSIGNMENT_REMOVE_USER
		Console.WriteLine("Assert user we are about to add is not already assigned");
		//assert that the user we want to add is not actually already assigned
		await Expect(Page.Locator("#uiAddedUsers")).Not.ToContainTextAsync(TC16_DATAGROUP_ASSIGNMENT_REMOVE_USER);

		await Page.Locator("#uiSelectUsers").SelectOptionAsync(new[] { TC16_DATAGROUP_ASSIGNMENT_REMOVE_USER });
		Console.WriteLine("add assigned user");

		await Page.Locator("#uiUserAssignmentForm a").Nth(1).ClickAsync(locatorClickOptions);//click the + icon

		Console.WriteLine("Save assignment change");//#uiUserAssignment > div > div > div.modal-footer > button.btn.btn-primary

		await Page.Locator("#uiUserAssignment > div > div > div.modal-footer > button.btn.btn-primary").ClickAsync(locatorClickOptions);

		//go back to download , does the data group exist now in the available datagroups for the enrichment downoad template?

		Console.WriteLine("Goto download");
		await Page.GotoAsync(BUYER_ADMIN_DOWNLOAD_URL);
		//https://portal.hubwoo.com/srvs/BuyerCatalogs/export/index
		await Page.WaitForURLAsync(BUYER_ADMIN_DOWNLOAD_URL, pageWaitForUrlOptions);
		Console.WriteLine(BUYER_ADMIN_DOWNLOAD_URL);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		////////////////////////////////////////
		//expand the download panel
		////////////////////////////////////////
		await Page.GetByRole(AriaRole.Link, new() { Name = "New Download" }).ClickAsync(locatorClickOptions);

		Console.WriteLine("select customer enrichment template");

		await Page.GetByLabel("Template Type:").SelectOptionAsync(new[] { "cus_enrich" });

		await Task.Delay(3000);
		//wait for loadingScreen to disappear
		Console.WriteLine("waiting for loadingScreen to disappear");
		attempt = 0;
		isLoadingScreenVisible = await Page.Locator("#loadingScreen").IsVisibleAsync();
		while (isLoadingScreenVisible && attempt < 10)
		{
			try
			{
				await Expect(Page.Locator("#loadingScreen")).Not.ToBeVisibleAsync(locatorVisibleAssertion);
				isLoadingScreenVisible = false;
				break;
			}
			catch
			{
				attempt++;
				isLoadingScreenVisible = await Page.Locator("#loadingScreen").IsVisibleAsync();
			}
		}

		Console.WriteLine("assert data group is now available");
		await Expect(Page.Locator("#uiExportTemplateDataGroup")).ToContainTextAsync(DATAGROUP_NAME);
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}


	/*
		enrichment tests overview.

		enrichment upload and execution will only work for a user, if that user is assigned to the appropriate data_group (performed in CMB -> Catalog Manager -> Data Group User Management (srvs/BuyerCatalogs/admin/DataGroupUserAssignment).
	  A) TC16_CMB_Enrichment_Datagroup_Assignment - test the effect of adding and removing a user from the following datagroups:
	    1) Prod test 1key enrichment  (PROD test) - data group of TESTCUSTCDO 1
	    2) Qa test 1key enrichment (QA test) - data group of SV Buyer
	     on the ability to download datagroup data on the cmb downloads page.

		B) TC17_CMB_Upload_Enrichment uploads a 1 key enrichment data file and a 2key enrichment datafile.

		Import of enrichment files should increment the version number of the 2 datagroups:
		1)  
		QA  '1_Key_datagroup'  - data group of SV Buyer
		PROD '1_Key_datagroup' - data group of TESTCUSTCDO 1

		2)QA '2_Key_datagrouptest' - data group of SV Buyer
		PROD '2_Key_datagrouptest' - data group of TESTCUSTCDO 1

		the data_group version can be viewed via
		1) CMB -> Catalog Manager -> Data Groups User Management ->  srvs/BuyerCatalogs/admin/DataGroupUserAssignment see version column
		2) CMA -> Edit Enrichment -> Manage Datagroups button ->
		 
		 
		C) TC18_CMB_Download_Enrichment - downloads the enrichments files uploaded in test TC17

		D) TC23_CMB_Execute_Enrichment - executes the manual enrichments
		1) PROD 'Prod test 2key enrichment' 
	  2) QA 'Qa  test 2key enrichment'

	  also test the automatic enrichments
	  3) PROD 'Prod test 1key enrichment'
	  4)  QA 'Qa test 1key enrichment'
		*/

	[Test, Order(17)]
	[Category("CMBTests")]
	async public Task TC17_CMB_Upload_Enrichment()
	{

		//upload 1key and 2key enrichment files, should create multikey enrichment import and enrichment import tasks and increment version number displayed on 
		//CMB Admin -> Administration-> Catalog Manager -> Data Groups User Assignment -> srvs/BuyerCatalogs/admin/DataGroupUserAssignment
		//this test assumes that there are 2 datagroups on qa and prod named:
		//1_Key_datagroup
		//2_Key_datagrouptest
		//and that there are files named 1key_enrich_template_new.xlsx and 2key_multi_key_enrich_template.xlsx in the upload folder \catalog-manager\PlaywrightTests\PlaywrightTests\CMB\QA and \catalog-manager\PlaywrightTests\PlaywrightTests\CMB\PROD
		Console.WriteLine("TC17_CMB_Upload_Enrichment");
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179308
		//prod cmb smoke tests 179378
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
		int UPDATED_ONE_KEY_DATAGROUP_VERSION = 0;
		int UPDATED_TWO_KEY_DATAGROUP_VERSION = 0;
		DateTime today = DateTime.Now;
		string CurrentDate = $"{today.Month}/{today.Day}/{today.Year}";
		await Page.SetViewportSizeAsync(1920, 1280);
		await Page.GotoAsync(url, pageGotoOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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

		Console.WriteLine(Page.Url);
		DateTime thisTestStarted = DateTime.UtcNow;
		Console.WriteLine("this test started: " + thisTestStarted.ToLongTimeString());
		var isCookieConsentVisible = await Page.Locator("#cookie-consent-dialog").IsVisibleAsync();
		if (isCookieConsentVisible)
		{
			await Page.GetByRole(AriaRole.Button, new() { Name = "Accept all" }).ClickAsync(locatorClickOptions);
		}

		Console.WriteLine("LOGIN AS " + BUYER_USER1_LOGIN);
		await SignInPortal(PORTAL_MAIN_URL, BUYER_USER1_LOGIN, BUYER_USER1_PASSWORD);

		//click catalogs tab
		Console.WriteLine("click catalogs tab");

		Console.WriteLine("Waiting for " + CMB_CATALOG_HOME_URL);
		Boolean cmbDashboardRendered = false;
		int loadcmbDashboardAttempts = 0;
		while (cmbDashboardRendered == false && loadcmbDashboardAttempts < 10)
		{
			try
			{
				await Page.GotoAsync(CMB_CATALOG_HOME_URL, pageGotoOptions);
				await Page.WaitForURLAsync(CMB_CATALOG_HOME_URL, pageWaitForUrlOptions);
				Console.WriteLine(Page.Url);
				cmbDashboardRendered = true;
			}
			catch (Exception ex)
			{
				loadcmbDashboardAttempts++;
				Console.WriteLine("Issue navigating to cmb dashboard, attempt " + loadcmbDashboardAttempts.ToString());
				Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
				Console.WriteLine("exception: " + ex.Message);
			}
		}
		Console.WriteLine(Page.Url);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Buyer Dashboard");


		//determine current versions of the 2 data groups that will be imported in this test
		//1_Key_datagroup
		//2_Key_datagrouptest

		//GO TO CMB_DATA_GROUPS_USER_ASSIGNMENT
		Console.WriteLine("Go to " + CMB_DATA_GROUPS_USER_ASSIGNMENT);

		await Page.GotoAsync(CMB_DATA_GROUPS_USER_ASSIGNMENT, pageGotoOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page.WaitForURLAsync(CMB_DATA_GROUPS_USER_ASSIGNMENT, pageWaitForUrlOptions);

		//takes a while for the page to load the datagroup information

		await Task.Delay(6000);
		await Expect(Page.Locator("#uiDataGroupContent")).ToContainTextAsync("2_Key_datagrouptest");
		await Expect(Page.Locator("#uiDataGroupContent")).ToContainTextAsync("1_Key_datagroup");


		int row = 1;
		int One_Key_datagroupRow = 0;
		int Two_Key_datagrouptestRow = 0;
		string dataGroupName = "";
		string dataGroupVersion = "";
		var totalDataGroups = await Page.Locator("#uiDataGroupContent  > tr").CountAsync();
		while (row < totalDataGroups)
		{
			dataGroupName = await Page.Locator($"#uiDataGroupContent > tr:nth-child({row}) > td:nth-child(1)").TextContentAsync(locatorTextContentOptions);
			dataGroupVersion = await Page.Locator($"#uiDataGroupContent > tr:nth-child({row}) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
			if (dataGroupName == "1_Key_datagroup")
			{
				One_Key_datagroupRow = row;
				if (!String.IsNullOrEmpty(dataGroupVersion))
				{
					ONE_KEY_DATAGROUP_VERSION = int.Parse(dataGroupVersion);
				}
			}

			if (dataGroupName == "2_Key_datagrouptest")
			{
				Two_Key_datagrouptestRow = row;
				if (!String.IsNullOrEmpty(dataGroupVersion))
				{
					TWO_KEY_DATAGROUP_VERSION = int.Parse(dataGroupVersion);
				}
			}

			if (ONE_KEY_DATAGROUP_VERSION != 0 && TWO_KEY_DATAGROUP_VERSION != 0)
			{
				break;
			}
			row++;
		}

		Console.WriteLine("click catalogs tab");

		Console.WriteLine("Waiting for " + CMB_CATALOG_HOME_URL);
		cmbDashboardRendered = false;
		loadcmbDashboardAttempts = 0;
		while (cmbDashboardRendered == false && loadcmbDashboardAttempts < 10)
		{
			try
			{
				await Page.GotoAsync(CMB_CATALOG_HOME_URL, pageGotoOptions);
				await Page.WaitForURLAsync(CMB_CATALOG_HOME_URL, pageWaitForUrlOptions);
				Console.WriteLine(Page.Url);
				cmbDashboardRendered = true;
			}
			catch (Exception ex)
			{
				loadcmbDashboardAttempts++;
				Console.WriteLine("Issue navigating to cmb dashboard, attempt " + loadcmbDashboardAttempts.ToString());
				Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
				Console.WriteLine("exception: " + ex.Message);
			}
		}
		Console.WriteLine(Page.Url);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Buyer Dashboard");
		Console.WriteLine("Go to Upload");

		await Page.Locator("#btnShowUploadModal").ClickAsync(locatorClickOptions);

		await Expect(Page.Locator("#uiUploadModul")).ToBeVisibleAsync(locatorVisibleAssertion);

		//upload 1key_enrich_template_new.xlsx
		Console.WriteLine("select " + UPLOAD_ENRICHMENT_FILE1 + " file to upload");

		await Task.Delay(2000);

		await Page.Locator($"[id=\"uiFileSelect\"]").SetInputFilesAsync(new[] { TC01_CATALOG_FILE_PATH + UPLOAD_ENRICHMENT_FILE1 });

		//has file been added to the download list?
		await Expect(Page.Locator("#uiUploadedFileList > table > tbody > tr > td:nth-child(1)")).ToContainTextAsync(UPLOAD_ENRICHMENT_FILE1);
		Console.WriteLine("set filetype");
		await Page.Locator($"[id=\"{UPLOAD_ENRICHMENT_FILE1}_selectType\"]").SelectOptionAsync(new[] { "content" });

		Console.WriteLine("upload enrichment file");

		await Task.Delay(4000);

		await Page.GetByRole(AriaRole.Link, new() { Name = "Process files" }).ClickAsync(locatorClickOptions);

		DateTime jobStarted = DateTime.Now;
		Console.WriteLine("job created " + jobStarted.ToLongDateString());

		//goto monitor
		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor ENRICHMENT IMPORT");//href = "/srvs/BuyerCatalogs/monitor/MonitorBuyer"
		Console.WriteLine("**********************************************");

		bool monitorPageRendered = false;
		int loadMonitorPageAttempts = 0;
		while (monitorPageRendered == false && loadMonitorPageAttempts < 10)
		{
			try
			{
				await Page.GotoAsync(CMB_MONITOR_URL, pageGotoOptions);
				Console.WriteLine("waiting for : " + CMB_MONITOR_URL);
				await Page.WaitForLoadStateAsync(LoadState.Load);
				await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
				await Page.WaitForURLAsync(CMB_MONITOR_URL, pageWaitForUrlOptions);
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

		Console.WriteLine("manually refresh monitor");
		await Page.Locator("a.btn.btn-sm.btn-primary").ClickAsync(manualRefreshClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//filter
		await Page.Locator("#uiProcessType").SelectOptionAsync(new[] { CMB_MONITOR_PROCESS_FILTER_ENRICHMENT_IMPORT });  //enrichment import 23(prod)
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);

		//process Enrichment import on the monitor page
		/////////////////////////////////////////////////////////////////
		//note this job runs quickly so likely will be finished by the time the page loads!

		//could test that the started from day on the monitor matches todays date 
		var date = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(3)").TextContentAsync(locatorTextContentOptions);
		int firstBracket = date.IndexOf("(");
		string actionDate = date.Substring(0, firstBracket).Trim();   //remove characters after the first (  e.g. 4/17/2024 (3:38 PM)
		Console.WriteLine("date for last enrichment import job :" + date);

		if (CurrentDate != actionDate)
		{
			Console.WriteLine("action date for last enrichment import job different from expected: " + CurrentDate + " actual: " + date);
		}
		//new process Enrichment import created
		//expect first row in table to have new process
		var process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
		Console.WriteLine("process: " + process);
		await Expect(Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)")).ToContainTextAsync("Enrichment import", locatorToContainTextOption);
		//get process and status of the item in row 1 of the table
		var status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
		process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
		Console.WriteLine("status: " + status);

		if (status == "Finished OK" || status == "Failed")
		{
			Console.WriteLine("Status is Finished/Failed , Manually Refresh monitor");
			await Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
			await Page.WaitForLoadStateAsync(LoadState.Load);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			await Task.Delay(4000);

			await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" })).ToBeVisibleAsync(locatorVisibleAssertion);
			//get process and status of the item in row 1 of the table
			status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
			process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
			Console.WriteLine("process: " + process);
			Console.WriteLine("status: " + status);
		}

		if (status == "Failed" && process == "Enrichment import")
		{
			Console.WriteLine("Enrichment import status still failed after refresh, taking a screenshot ");
			//take screenshot after clicking on failed status
			DateTime dateNow = DateTime.Now;
			string CurrentDate1 = $"{dateNow.Year}{dateNow.Month}{dateNow.Day}{dateNow.Hour}{dateNow.Minute}";
			await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").ClickAsync(locatorClickOptions);
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC17_Enrichment_Import_Failure" + CurrentDate1 + ".png"
			});
		}
		//dont perform this check, job completes before monitor loads - await Expect(Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div")).ToContainTextAsync(new Regex(@"(\W|^)(Waiting to be processed|Currently processing)(\W|$)"));

		int attempt = 0;
		while (attempt <= MONITOR_CHECK_ATTEMPTS)
		{
			try
			{
				Console.WriteLine("Manually Refresh monitor");
				attempt++;
				//await Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
				await Page.Locator("a.btn.btn-sm.btn-primary").ClickAsync(manualRefreshClickOptions);
				await Page.WaitForLoadStateAsync(LoadState.Load);
				await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

				await Task.Delay(4000);

				Console.WriteLine("Waiting for Enrichment import : " + attempt.ToString());
				//get process and status of the item in row 1 of the table
				process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
				status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
				Console.WriteLine("status: " + status);
				Console.WriteLine("process: " + process);
				var processid = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(1) > a").TextContentAsync();
				Console.WriteLine("processid:" + processid);
				var startedFrom = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(3)").TextContentAsync();
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

				if (process == "Enrichment import" && status == "Finished OK" && currentProcessStarted > thisTestStarted.AddMinutes(-8))
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
							await Page.GotoAsync(CMB_MONITOR_URL, pageGotoOptions);
							Console.WriteLine("waiting for : " + CMB_MONITOR_URL);
							await Page.WaitForURLAsync(CMB_MONITOR_URL, pageWaitForUrlOptions);
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
					await Page.GetByLabel("Process Type:").SelectOptionAsync(new[] { CMB_MONITOR_PROCESS_FILTER_ENRICHMENT_IMPORT });  //enrichment import 23(prod)
					await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
					await Task.Delay(3000);
				}
				if (attempt >= MONITOR_CHECK_ATTEMPTS || status == "Failed")
				{
					Console.WriteLine("**********************************************");
					Console.WriteLine("Enrichment import failed");
					Console.WriteLine("**********************************************");
					throw ex;
				}
			}
		}

		if (attempt >= MONITOR_CHECK_ATTEMPTS || status != "Finished OK")
		{
			throw new Exception("Number of attempts to wait for Enrichment import job to finish exceeded");
		}
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Enrichment import completed");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////

		//goto buyercatalogs https://portal.hubwoo.com/srvs/BuyerCatalogs/
		//upload 2key_multi_key_enrich_template.xlsx  UPLOAD_ENRICHMENT_FILE2
		Console.WriteLine("Waiting for " + CMB_CATALOG_HOME_URL);
		cmbDashboardRendered = false;
		loadcmbDashboardAttempts = 0;
		while (cmbDashboardRendered == false && loadcmbDashboardAttempts < 10)
		{
			try
			{
				await Page.GotoAsync(CMB_CATALOG_HOME_URL, pageGotoOptions);
				await Page.WaitForLoadStateAsync(LoadState.Load);
				await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
				await Page.WaitForURLAsync(CMB_CATALOG_HOME_URL, pageWaitForUrlOptions);
				Console.WriteLine(Page.Url);
				cmbDashboardRendered = true;
			}
			catch (Exception ex)
			{
				loadcmbDashboardAttempts++;
				Console.WriteLine("Issue navigating to cmb dashboard, attempt " + loadcmbDashboardAttempts.ToString());
				Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
				Console.WriteLine("exception: " + ex.Message);
			}
		}

		Console.WriteLine("Go to Upload");

		await Page.Locator("#btnShowUploadModal").ClickAsync(locatorClickOptions);

		await Expect(Page.Locator("#uiUploadModul")).ToBeVisibleAsync(locatorVisibleAssertion);

		//uiFileSelect
		//upload 1key_enrich_template_new.xlsx
		Console.WriteLine("select " + UPLOAD_ENRICHMENT_FILE2 + " file to upload");
		await Task.Delay(2000);
		await Page.Locator($"[id=\"uiFileSelect\"]").SetInputFilesAsync(new[] { TC01_CATALOG_FILE_PATH + UPLOAD_ENRICHMENT_FILE2 });

		//has file been added to the download list?
		await Expect(Page.Locator("#uiUploadedFileList > table > tbody > tr > td:nth-child(1)")).ToContainTextAsync(UPLOAD_ENRICHMENT_FILE2);

		await Page.Locator($"[id=\"{UPLOAD_ENRICHMENT_FILE2}_selectType\"]").SelectOptionAsync(new[] { "content" });

		Console.WriteLine("upload MULTIKEY enrichment file");

		await Task.Delay(4000);

		await Page.GetByRole(AriaRole.Link, new() { Name = "Process files" }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//goto monitor
		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor MULTIKEY ENRICHMENT IMPORT");//href = "/srvs/BuyerCatalogs/monitor/MonitorBuyer"
		Console.WriteLine("**********************************************");

		monitorPageRendered = false;
		loadMonitorPageAttempts = 0;
		while (monitorPageRendered == false && loadMonitorPageAttempts < 10)
		{
			try
			{
				await Page.GotoAsync(CMB_MONITOR_URL, pageGotoOptions);
				Console.WriteLine("waiting for : " + CMB_MONITOR_URL);
				await Page.WaitForURLAsync(CMB_MONITOR_URL, pageWaitForUrlOptions);
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

		Console.WriteLine("manually refresh monitor");
		await Page.Locator("a.btn.btn-sm.btn-primary").ClickAsync(manualRefreshClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		//filter
		await Page.Locator("#uiProcessType").SelectOptionAsync(new[] { CMB_MONITOR_PROCESS_FILTER_MULTI_KEY_ENRICHMENT_IMPORT });  //multikey enrichment import 163(prod)
																																																															 //await Page.GetByLabel("Process Type:").SelectOptionAsync(new[] { CMB_MONITOR_PROCESS_FILTER_MULTI_KEY_ENRICHMENT_IMPORT });  //multikey enrichment import 163(prod)
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);

		//process Multikey Enrichment Import CMB_MONITOR_PROCESS_FILTER_MULTI_KEY_ENRICHMENT_IMPORT
		//////////////////////////////////////////////////////////////////

		//note this job runs quickly so likely will be finished by the time the page loads!

		//could test that the started from day on the monitor matches todays date 
		date = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(3)").TextContentAsync(locatorTextContentOptions);
		Console.WriteLine("date for 1st enrichment import job :" + date);

		if (CurrentDate != date)
		{
			Console.WriteLine("action date for last enrichment import job different from expected: " + CurrentDate + " actual: " + date);
		}
		//new process Enrichment import created
		//expect first row in table to have new process
		process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
		Console.WriteLine("process: " + process);
		await Expect(Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)")).ToContainTextAsync("Multikey Enrichment Import", locatorToContainTextOption);
		//get process and status of the item in row 1 of the table
		status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
		process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
		Console.WriteLine("status: " + status);

		if (status == "Finished OK" || status == "Failed")
		{
			Console.WriteLine("Status is Finished/Failed , Manually Refresh monitor");
			await Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
			await Page.WaitForLoadStateAsync(LoadState.Load);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" })).ToBeVisibleAsync(locatorVisibleAssertion);
			//get process and status of the item in row 1 of the table
			status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
			process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
			Console.WriteLine("process: " + process);
			Console.WriteLine("status: " + status);
		}

		if (status == "Failed" && process == "Multikey Enrichment Import")
		{
			Console.WriteLine("Multikey Enrichment Import status still failed after refresh, taking a screenshot ");
			//take screenshot after clicking on failed status

			string CurrentDate1 = $"{today.Year}{today.Month}{today.Day}{today.Hour}{today.Minute}";
			await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").ClickAsync(locatorClickOptions);
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC17_MultikeyEnrichment_Import_Failure" + CurrentDate1 + ".png"
			});
		}

		attempt = 0;
		while (attempt <= MONITOR_CHECK_ATTEMPTS)
		{
			try
			{
				Console.WriteLine("Manually Refresh monitor");
				attempt++;
				//await Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
				await Page.Locator("a.btn.btn-sm.btn-primary").ClickAsync(manualRefreshClickOptions);
				await Page.WaitForLoadStateAsync(LoadState.Load);
				await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

				await Task.Delay(4000);

				Console.WriteLine("Waiting for Multikey Enrichment Import : " + attempt.ToString());
				//get process and status of the item in row 1 of the table
				process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
				status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
				Console.WriteLine("status: " + status);
				Console.WriteLine("process: " + process);
				var processid = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(1) > a").TextContentAsync();
				Console.WriteLine("processid:" + processid);
				var startedFrom = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(3)").TextContentAsync();
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

				if (process == "Multikey Enrichment Import" && status == "Finished OK" && currentProcessStarted > thisTestStarted.AddMinutes(-8))
				{
					break;
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
							await Page.GotoAsync(CMB_MONITOR_URL, pageGotoOptions);
							Console.WriteLine("waiting for : " + CMB_MONITOR_URL);
							await Page.WaitForURLAsync(CMB_MONITOR_URL, pageWaitForUrlOptions);
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
					await Page.GetByLabel("Process Type:").SelectOptionAsync(new[] { CMB_MONITOR_PROCESS_FILTER_MULTI_KEY_ENRICHMENT_IMPORT });  //multikey enrichment import 163(prod)
					await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
					await Task.Delay(3000);
				}
				if (attempt >= MONITOR_CHECK_ATTEMPTS || status == "Failed")
				{
					Console.WriteLine("**********************************************");
					Console.WriteLine("multikey enrichment import failed");
					Console.WriteLine("**********************************************");
					throw ex;
				}
			}
		}

		if (attempt >= MONITOR_CHECK_ATTEMPTS || status != "Finished OK")
		{
			throw new Exception("Number of attempts to wait for Multikey Enrichment Import job to finish exceeded");
		}
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("multikey Enrichment import completed");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////


		Console.WriteLine("Go to " + CMB_DATA_GROUPS_USER_ASSIGNMENT);

		await Page.GotoAsync(CMB_DATA_GROUPS_USER_ASSIGNMENT, pageGotoOptions);
		await Page.WaitForURLAsync(CMB_DATA_GROUPS_USER_ASSIGNMENT, pageWaitForUrlOptions);

		//takes a while for the page to load the datagroup information

		await Task.Delay(6000);
		await Expect(Page.Locator("#uiDataGroupContent")).ToContainTextAsync("2_Key_datagrouptest");
		await Expect(Page.Locator("#uiDataGroupContent")).ToContainTextAsync("1_Key_datagroup");


		row = 1;
		totalDataGroups = await Page.Locator("#uiDataGroupContent  > tr").CountAsync();
		Console.WriteLine("Restore most recently released catalog version");

		while (row < totalDataGroups)
		{
			dataGroupName = await Page.Locator($"#uiDataGroupContent > tr:nth-child({row}) > td:nth-child(1)").TextContentAsync(locatorTextContentOptions);
			dataGroupVersion = await Page.Locator($"#uiDataGroupContent > tr:nth-child({row}) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
			if (dataGroupName == "1_Key_datagroup")
			{
				One_Key_datagroupRow = row;
				if (!String.IsNullOrEmpty(dataGroupVersion))
				{
					UPDATED_ONE_KEY_DATAGROUP_VERSION = int.Parse(dataGroupVersion);
				}
			}
			if (dataGroupName == "2_Key_datagrouptest")
			{
				Two_Key_datagrouptestRow = row;
				if (!String.IsNullOrEmpty(dataGroupVersion))
				{
					UPDATED_TWO_KEY_DATAGROUP_VERSION = int.Parse(dataGroupVersion);
				}
			}

			if (UPDATED_ONE_KEY_DATAGROUP_VERSION != 0 && UPDATED_TWO_KEY_DATAGROUP_VERSION != 0)
			{
				break;
			}
			row++;
		}
		Console.WriteLine("Assert that the datagroup version have been incremented ");

		Assert.That(UPDATED_ONE_KEY_DATAGROUP_VERSION > 0);
		Assert.That(UPDATED_TWO_KEY_DATAGROUP_VERSION > 0);

		Assert.That(ONE_KEY_DATAGROUP_VERSION > 0);
		Assert.That(TWO_KEY_DATAGROUP_VERSION > 0);

		Assert.That(UPDATED_ONE_KEY_DATAGROUP_VERSION == ONE_KEY_DATAGROUP_VERSION + 1);
		Assert.That(UPDATED_TWO_KEY_DATAGROUP_VERSION == TWO_KEY_DATAGROUP_VERSION + 1);

		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());

	}


	[Test, Order(18)]
	[Category("CMBTests")]
	async public Task TC18_CMB_Download_Enrichment()
	{
		Console.WriteLine("TC18_CMB_Download_Enrichment");
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179308
		//prod cmb smoke tests 179377
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
		await Page.SetViewportSizeAsync(1920, 1280);
		await Page.GotoAsync(url, pageGotoOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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

		Console.WriteLine(Page.Url);

		var isCookieConsentVisible = await Page.Locator("#cookie-consent-dialog").IsVisibleAsync();
		if (isCookieConsentVisible)
		{
			await Page.GetByRole(AriaRole.Button, new() { Name = "Accept all" }).ClickAsync(locatorClickOptions);
		}
		DateTime thisTestStarted = DateTime.UtcNow;
		Console.WriteLine("this test started: " + thisTestStarted.ToLongTimeString());
		Console.WriteLine("LOGIN AS " + BUYER_USER1_LOGIN);
		await SignInPortal(PORTAL_MAIN_URL, BUYER_USER1_LOGIN, BUYER_USER1_PASSWORD);

		//click catalogs tab
		Console.WriteLine("click catalogs tab");

		Console.WriteLine("Waiting for " + CMB_CATALOG_HOME_URL);
		Boolean cmbDashboardRendered = false;
		int loadcmbDashboardAttempts = 0;
		while (cmbDashboardRendered == false && loadcmbDashboardAttempts < 10)
		{
			try
			{
				await Page.GotoAsync(CMB_CATALOG_HOME_URL, pageGotoOptions);
				await Page.WaitForLoadStateAsync(LoadState.Load);
				await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
				await Page.WaitForURLAsync(CMB_CATALOG_HOME_URL, pageWaitForUrlOptions);
				Console.WriteLine(Page.Url);
				cmbDashboardRendered = true;
			}
			catch (Exception ex)
			{
				loadcmbDashboardAttempts++;
				Console.WriteLine("Issue navigating to cmb dashboard, attempt " + loadcmbDashboardAttempts.ToString());
				Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
				Console.WriteLine("exception: " + ex.Message);
			}
		}
		Console.WriteLine(Page.Url);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Buyer Dashboard");

		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buyer Dashboard" })).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("go to download");
		await Page.GotoAsync(BUYER_ADMIN_DOWNLOAD_URL, pageGotoOptions);//use goto instead of clicking download menu item
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		//https://portal.hubwoo.com/srvs/BuyerCatalogs/export/index
		await Page.WaitForURLAsync(BUYER_ADMIN_DOWNLOAD_URL, pageWaitForUrlOptions);
		Console.WriteLine(Page.Url);

		Console.WriteLine("download enrichment file");

		/////////////////////////////////////
		//expand the download panel
		/////////////////////////////////////
		await Page.GetByRole(AriaRole.Link, new() { Name = "New Download" }).ClickAsync(locatorClickOptions);

		await Page.GetByLabel("Template Type:").SelectOptionAsync(new[] { "cus_enrich" });
		Console.WriteLine("waiting for loadingScreen to disappear");
		int attempt = 0;
		var isLoadingScreenVisible = await Page.Locator("#loadingScreen").IsVisibleAsync();
		while (isLoadingScreenVisible && attempt < 10)
		{
			try
			{
				await Expect(Page.Locator("#loadingScreen")).Not.ToBeVisibleAsync(locatorVisibleAssertion);
				isLoadingScreenVisible = false;
				break;
			}
			catch
			{
				attempt++;
				isLoadingScreenVisible = await Page.Locator("#loadingScreen").IsVisibleAsync();
			}
		}
		//CUS62376_2_Key_datagrouptest
		await Page.Locator("#uiExportTemplateDataGroup").SelectOptionAsync(new[] { ENRICHMENT_DATAGROUP_DOWNLOAD_NAME });
		await Page.GetByText("Create new Export Template").ClickAsync(locatorClickOptions);

		DateTime jobStarted = DateTime.Now;
		Console.WriteLine("job created " + jobStarted.ToLongDateString());

		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor TEMPLATE EXPORT");//href = "/srvs/BuyerCatalogs/monitor/MonitorBuyer"
		Console.WriteLine("**********************************************");
		bool monitorPageRendered = false;
		int loadMonitorPageAttempts = 0;
		while (monitorPageRendered == false && loadMonitorPageAttempts < 10)
		{
			try
			{
				await Page.GotoAsync(CMB_MONITOR_URL, pageGotoOptions);
				await Page.WaitForLoadStateAsync(LoadState.Load);
				await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
				Console.WriteLine("waiting for : " + CMB_MONITOR_URL);
				await Page.WaitForURLAsync(CMB_MONITOR_URL, pageWaitForUrlOptions);
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

		Console.WriteLine("manually refresh monitor");
		await Page.Locator("a.btn.btn-sm.btn-primary").ClickAsync(manualRefreshClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await Task.Delay(4000);

		//filter
		await Page.GetByLabel("Process Type:").SelectOptionAsync(new[] { CMB_MONITOR_PROCESS_FILTER_TEMPLATE_EXPORT });  //template export  31(prod)
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
		DateTime today = DateTime.Now;
		string CurrentDate = $"{today.Month}/{today.Day}/{today.Year}";
		//monitor the process Template Export
		//////////////////////////////////////////////////////////////////
		//could test that the started from day on the monitor matches todays date 
		var date = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(3)").TextContentAsync(locatorTextContentOptions);
		int firstBracket = date.IndexOf("(");
		string actionDate = date.Substring(0, firstBracket).Trim();   //remove characters after the first (  e.g. 4/17/2024 (3:38 PM)
		Console.WriteLine("date for last enrichment import job :" + date);

		if (CurrentDate != actionDate)
		{
			Console.WriteLine("action date for last enrichment import job different from expected: " + CurrentDate + " actual: " + date);
		}
		//new process Enrichment import created
		//expect first row in table to have new process
		var process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
		Console.WriteLine("process: " + process);
		await Expect(Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)")).ToContainTextAsync("Template Export", locatorToContainTextOption);
		//get process and status of the item in row 1 of the table
		var status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
		process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
		Console.WriteLine("status: " + status);

		if (status == "Finished OK" || status == "Failed")
		{
			Console.WriteLine("Status is Finished/Failed , Manually Refresh monitor");
			await Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
			await Page.WaitForLoadStateAsync(LoadState.Load);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			await Task.Delay(4000);

			await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" })).ToBeVisibleAsync(locatorVisibleAssertion);
			//get process and status of the item in row 1 of the table
			status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
			process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
			Console.WriteLine("process: " + process);
			Console.WriteLine("status: " + status);
		}

		if (status == "Failed" && process.ToUpper() == "TEMPLATE EXPORT")
		{
			Console.WriteLine("Template Export status still failed after refresh, taking a screenshot ");
			//take screenshot after clicking on failed status

			string CurrentDate1 = $"{today.Year}{today.Month}{today.Day}{today.Hour}{today.Minute}";
			await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").ClickAsync(locatorClickOptions);
			await Page.ScreenshotAsync(new()
			{
				FullPage = true,
				Path = downloadPath + "TC18_Enrichment_Download_Failure1_" + CurrentDate1 + ".png"
			});
		}

		attempt = 0;
		while (attempt <= MONITOR_CHECK_ATTEMPTS)
		{
			try
			{
				Console.WriteLine("Manually Refresh monitor");
				attempt++;
				//await Page.GetByRole(AriaRole.Link, new() { Name = "Manual Refresh" }).ClickAsync(manualRefreshClickOptions);
				await Page.Locator("a.btn.btn-sm.btn-primary").ClickAsync(manualRefreshClickOptions);
				await Page.WaitForLoadStateAsync(LoadState.Load);
				await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

				await Task.Delay(4000);

				Console.WriteLine("Waiting for Template Export : " + attempt.ToString());
				//get process and status of the item in row 1 of the table
				process = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(2)").TextContentAsync(locatorTextContentOptions);
				status = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(6) > div").TextContentAsync(locatorTextContentOptions);
				Console.WriteLine("status: " + status);
				Console.WriteLine("process: " + process);
				var processid = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(1) > a").TextContentAsync();
				Console.WriteLine("processid:" + processid);
				var startedFrom = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(3)").TextContentAsync();
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

				if (process == "Template Export" && status == "Finished OK" && currentProcessStarted > thisTestStarted.AddMinutes(-8))
				{
					break;
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
							await Page.GotoAsync(CMB_MONITOR_URL, pageGotoOptions);
							Console.WriteLine("waiting for : " + CMB_MONITOR_URL);
							await Page.WaitForLoadStateAsync(LoadState.Load);
							await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
							await Page.WaitForURLAsync(CMB_MONITOR_URL, pageWaitForUrlOptions);
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
					Console.WriteLine(Page.Url);

					Console.WriteLine("manually refresh monitor");
					await Page.GetByLabel("Process Type:").SelectOptionAsync(new[] { CMB_MONITOR_PROCESS_FILTER_TEMPLATE_EXPORT });  //template export  31(prod)
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
		Console.WriteLine("Template Export completed");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("go to download");
		await Page.GotoAsync(BUYER_ADMIN_DOWNLOAD_URL, pageGotoOptions);
		//https://portal.hubwoo.com/srvs/BuyerCatalogs/export/index
		await Page.WaitForURLAsync(BUYER_ADMIN_DOWNLOAD_URL, pageWaitForUrlOptions);
		Console.WriteLine(Page.Url);

		////////////////////////////////////
		//expand the download panel
		////////////////////////////////////
		await Page.GetByRole(AriaRole.Link, new() { Name = "New Download" }).ClickAsync(locatorClickOptions);

		await Page.GetByLabel("Type:", new() { Exact = true }).SelectOptionAsync(new[] { "cus_enrich" });
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);

		Console.WriteLine("waiting for loadingScreen to disappear");
		//wait for loadingScreen to disappear
		attempt = 0;
		isLoadingScreenVisible = await Page.Locator("#loadingScreen").IsVisibleAsync();
		while (isLoadingScreenVisible && attempt < 10)
		{
			try
			{
				await Expect(Page.Locator("#loadingScreen")).Not.ToBeVisibleAsync(locatorVisibleAssertion);
				isLoadingScreenVisible = false;
				break;
			}
			catch
			{
				attempt++;
				isLoadingScreenVisible = await Page.Locator("#loadingScreen").IsVisibleAsync();
			}
		}

		Console.WriteLine("download enrichment file");
		var waitForDownloadTask = Page.WaitForDownloadAsync();
		//get link
		//https://portal.hubwoo.com/srvs/omnicontent/templatearchive/9573892_multi_key_enrich_exported.xlsx

		var link = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(8) > a").GetAttributeAsync("href");
		CurrentDate = $"{today.Month}_{today.Day}_{today.Year}";
		Console.WriteLine("Download " + link);
		await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(8) > a").First.ClickAsync(locatorClickOptions);

		var download = await waitForDownloadTask;

		var fileName = downloadPath + "TC18_" + CurrentDate + download.SuggestedFilename;

		Console.WriteLine("File downloaded to " + fileName);

		// Wait for the download process to complete and save the downloaded file somewhere
		await download.SaveAsAsync(fileName);

		//should we open the file and check the contents?
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}


	[Test, Order(19)]
	[Category("CMBTests")]
	async public Task TC18a_CMB_Customer_Upload_And_Release_Catalog()
	{
		//test assumes that the catalog file used in this test
		//PROD: xlsx_prod_catalog_SCF_prod_file_base_searchCheck.xlsx
		//QA: xlsx_qa_catalog_SCF_qa_file_base_searchCheck.xlsx
		//has been recently opened and saved so that the short description of one catalog item (11-015.9025) contains todays date time
		//have now updated the scf catalog files used in this test so that the short description of item 11-015-9025 has the short description AUTOMATION TEST TC18a
		/*
		 test: buyer uploads a catalog with a defined short description for one of the catalog items
		 catalog is released to buyer
		 buyer creates working version
		 buyer approves catalog items
		 buyer releases catalog to search, waits for set live to complete
		 confirm catalog is in production
		 naviagate to search, search for item and assert expected short description

		*/
		Console.WriteLine("TC18a_CMB_Customer_Upload_And_Release_Catalog");
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179308
		//prod cmb smoke tests 179370  Customer upload and release catalog
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
		//////////////////////////login 
		var simpleCatalogImportProcessId = "";
		await Page.SetViewportSizeAsync(1600, 900);
		Console.WriteLine("LOGIN AS " + BUYER_USER1_LOGIN);
		await SignInPortal(PORTAL_MAIN_URL, BUYER_USER1_LOGIN, BUYER_USER1_PASSWORD);

		DateTime today = DateTime.Now;
		string CurrentDate = $"{today.Month}/{today.Day}/{today.Year}";
		//click catalogs tab
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Buyer Dashboard");
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buyer Dashboard" })).ToBeVisibleAsync(locatorVisibleAssertion);

		//CUSTOMER_UPLOAD_AND_RELEASE_CATALOG_FILE  xlsx_prod_catalog_SCF_prod_file_base_checkroutine_supplier.xlsx
		Console.WriteLine("Go to Upload");
		await Page.Locator("#btnShowUploadModal").ClickAsync(locatorClickOptions);
		await Expect(Page.Locator("#uiUploadModul")).ToBeVisibleAsync(locatorVisibleAssertion);
		//upload xlsx_prod_catalog_SCF_prod_file_base_searchCheck.xlsx (prod)
		Console.WriteLine("select " + CUSTOMER_UPLOAD_AND_RELEASE_CATALOG_FILE + " file to upload");
		await Task.Delay(2000);
		await Page.Locator($"[id=\"uiFileSelect\"]").SetInputFilesAsync(new[] { TC01_CATALOG_FILE_PATH + CUSTOMER_UPLOAD_AND_RELEASE_CATALOG_FILE });

		//has file been added to the download list?
		//#uiUploadedFileList > table > tbody > tr > td:nth-child(1)
		await Expect(Page.Locator("#uiUploadedFileList > table > tbody > tr > td:nth-child(1)")).ToContainTextAsync(CUSTOMER_UPLOAD_AND_RELEASE_CATALOG_FILE);
		Console.WriteLine("set filetype");
		await Page.Locator($"[id=\"{CUSTOMER_UPLOAD_AND_RELEASE_CATALOG_FILE}_selectType\"]").SelectOptionAsync(new[] { "content" });
		await Task.Delay(2000);
		Console.WriteLine("upload catalog " + CUSTOMER_UPLOAD_AND_RELEASE_CATALOG_FILE);
		await Page.GetByRole(AriaRole.Link, new() { Name = "Process files" }).ClickAsync(locatorClickOptions);
		DateTime jobStarted = DateTime.Now;
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		Console.WriteLine("job created " + jobStarted.ToLongDateString());
		await Page.WaitForTimeoutAsync(2000);
		//confirm the uiUploadedFileList  Your upload files were placed in the process queue
		await Expect(Page.Locator("#uiUploadedFileList")).ToContainTextAsync("Your upload files were placed in the process queue", new LocatorAssertionsToContainTextOptions { Timeout = 60000 });

		//goto monitor
		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor SIMPLE CATALOG IMPORT");
		Console.WriteLine("**********************************************");
		await GoWithErrWrap(CMB_MONITOR_URL, 60);
		await SetManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Simple Catalog import", jobStarted, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");

		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Simple Catalog Import Completed");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////
		await Task.Delay(5000);
		ManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Release catalog", jobStarted, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");

		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Release catalog completed");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);

		//filter catalogs 
		Console.WriteLine("filter catalogs via supplier catalog id and status New Version Available");
		await CMBFilter("", TC04_SUPPLIER_ID);

		//TC04_CATALOG_SELECTOR = \\37 7418_";
		//TC04_CATALOG_SELECTOR_ID = "\\37 7418";
		//TC19_DASHBOARD_CATALOGID = "6 2376_77418"

		Console.WriteLine("assert catalog status is New Version Available");
		VerifyCatalogStatus(TC04_SUPPLIER_METACATID, "New Version available");
		Console.WriteLine("click show more");
		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_allTasks_btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		Console.WriteLine("assert that the supplier catalog chevron is active");
		//assert that supplier catalog chevron li nth-child(1) is active, i.e. has the class=active

		await Expect(Page.Locator($"#{TC04_CATALOG_SELECTOR_ID}_allTasks_navWizard > li.active")).ToContainTextAsync("Supplier Catalog", new LocatorAssertionsToContainTextOptions { Timeout = 60000, IgnoreCase = true });
		//#\37 7418_allTasks_navWizard > li:nth-child(1)

		Console.WriteLine("Assert Create working version available");
		Console.WriteLine("assert create working version button is visible");

		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_allTasks_tabSupplierCatalog\"]").GetByText("Create Working Version")).ToBeVisibleAsync();

		Console.WriteLine("assert reject catalog button is visible");//#\37 7418_allTasks_tabSupplierCatalog > div.catalog-actions.col-lg-7.col-md-7.col-sm-8 > div > div.pull-right > a.btn.btn-danger
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_allTasks_tabSupplierCatalog\"]").GetByText("Reject Catalog")).ToBeVisibleAsync();

		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("create working version");
		await Task.Delay(2000);
		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_allTasks_tabSupplierCatalog\"]").GetByText("Create Working Version").ClickAsync(locatorClickOptions);
		jobStarted = DateTime.Now;
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		Console.WriteLine("confirm status updated to Waiting for processing");
		await Task.Delay(2000);
		VerifyCatalogStatus(TC04_SUPPLIER_METACATID, "Wait for processing");


		Console.WriteLine("**********************************************");
		Console.WriteLine("go to monitor LOAD CATALOG");
		Console.WriteLine("**********************************************");
		await GoWithErrWrap(CMB_MONITOR_URL, 60);
		await SetManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Load Catalog", jobStarted, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Load Catalog now completed");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////

		Console.WriteLine("Go to dashboard what is the catalog status?");
		Console.WriteLine("Waiting for " + CMB_CATALOG_HOME_URL);
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
		Console.WriteLine("filter catalogs via supplier catalog id");
		await CMBFilter("", TC04_SUPPLIER_ID);
		await Task.Delay(2000);
		Console.WriteLine("assert catalog status is Catalog to approve"); //#\37 7418_allTasks_catalog > div > div.col-lg-10.col-md-9.col-sm-8 > div:nth-child(2) > div > h5:nth-child(2)
		VerifyCatalogStatus(TC04_SUPPLIER_METACATID, "Catalog to approve");

		Console.WriteLine("**********************************************");
		Console.WriteLine("about to approve items");
		Console.WriteLine("**********************************************");
		//filter catalogs 
		Console.WriteLine("click show more");
		try
		{ //Changed to user RunAndWaitForResponseAsync it will click the show more link and check the api call is successfully received
			await Page.RunAndWaitForResponseAsync(async () =>
			{
				await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_allTasks_btnShowMore\"]").ClickAsync(new LocatorClickOptions { Timeout = 100000 });
			}, response => response.Url.Contains("/srvs/BuyerCatalogs/GetWorkingVersionCatalogInfos") && response.Status == 200, new PageRunAndWaitForResponseOptions { Timeout = 60000 });
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		}
		catch (Exception e)
		{
			Console.WriteLine("exception clicking show more");
			Console.WriteLine(e.Message);
			Console.WriteLine("Reload page and click again");
			await Page.ReloadAsync();
			await Page.RunAndWaitForResponseAsync(async () =>
			{
				await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_allTasks_btnShowMore\"]").ClickAsync(new LocatorClickOptions { Timeout = 100000 });
			}, response => response.Url.Contains("/srvs/BuyerCatalogs/GetWorkingVersionCatalogInfos") && response.Status == 200, new PageRunAndWaitForResponseOptions { Timeout = 60000 });
		}

		Console.WriteLine("assert that the active chevron is approve items");//#\37 7418_allTasks_navWizard > li.active
		try
		{
			await Task.Delay(3000);
			await Expect(Page.Locator($"#{TC04_CATALOG_SELECTOR_ID}_allTasks_navWizard > li.active")).ToContainTextAsync("Approve Items");
		}
		catch (Exception e)
		{
			Console.WriteLine("exception checking for approve items");
			Console.WriteLine(e.Message);
			await Expect(Page.Locator($"#{TC04_CATALOG_SELECTOR_ID}_allTasks_navWizard > li.active")).ToContainTextAsync("Approve Items");
		}

		Console.WriteLine("assert view items button is visible");
		await Task.Delay(3000);
		try
		{
			await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_tabApproveItems\"]").GetByRole(AriaRole.Link, new() { Name = "Review Items" })).ToBeVisibleAsync(locatorVisibleAssertion);
		}
		catch (Exception e)
		{
			Console.WriteLine("exception when asserting approve items tab is visible");
			Console.WriteLine(e.Message);
			await Task.Delay(3000);
		}

		Console.WriteLine("click the approve items tab");
		try
		{
			await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_tabApproveItems\"]").GetByRole(AriaRole.Link, new() { Name = "Review Items" }).ClickAsync(new LocatorClickOptions { Force = true, Timeout = 100000 });
		}
		catch (Exception e)
		{
			Console.WriteLine("exception when trying to click the approve items tab");
			Console.WriteLine(e.Message);
			await Task.Delay(3000);
			await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_tabApproveItems\"]").GetByText("Review Items").ClickAsync(new LocatorClickOptions { Force = true, Timeout = 100000 });
		}
		//wait for uiitems table
		//#uiItems
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		Console.WriteLine("wait for uiitems: start " + DateTime.Now.ToLongTimeString());
		await Page.WaitForSelectorAsync("#uiItems", new PageWaitForSelectorOptions { Timeout = 60000 });
		Console.WriteLine("wait for uiitems: end " + DateTime.Now.ToLongTimeString());
		await Task.Delay(4000);

		Console.WriteLine("Page: " + Page.Url);
		//assert url like
		//await Expect(Page).ToHaveURLAsync(new Regex(TC04_APPROVE_ITEMS_REGEX));
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

		await Page.Locator("#uiTableAction").SelectOptionAsync(new[] { "approve_all" });
		await Task.Delay(3000);

		//check that the confirm button(uiSubmitAction) is now active after the approval action has been set
		Console.WriteLine("check that the confirm button(uiSubmitAction) is now active after the approval action has been set");

		await Expect(Page.Locator("#uiSubmitAction")).Not.ToBeDisabledAsync();

		//click confirm button
		Console.WriteLine("click the Confirm button (#uiSubmitAction)");
		await Task.Delay(2000);
		await Page.Locator("#uiSubmitAction").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(2000);
		WaitForElementToBeHidden(Page, "#loadingScreen");

		/////////////////////////////////            SUBMIT CATALOG                /////////////////////////////////

		Console.WriteLine("click the Submit Catalog link (#uiGoToReleaseTab)");
		await Page.Locator("#uiGoToReleaseTab").ClickAsync(locatorClickOptions);//doesn't submit but redirects user to dashboard with the release catalog chevron for the specific catalog in focus and active

		//user should get redirected to release chevron

		//should now be on dashboard with direct release button available
		await Task.Delay(4000);
		Console.WriteLine(Page.Url);
		await Expect(Page).ToHaveURLAsync(CMB_CATALOG_HOME_URL1);
		Console.WriteLine("on dashboard with release chevron active");
		await Expect(Page.GetByTitle("Direct Release")).ToBeVisibleAsync(locatorVisibleAssertion);

		//release catalog chevron visible

		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_navWizard\"]")).ToContainTextAsync("Release Catalog");
		Console.WriteLine("click direct release for catalog " + TC04_SUPPLIERNAME);
		await Task.Delay(2000);
		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_tabReleaseCatalog\"]").GetByTitle("Direct Release").ClickAsync(locatorClickOptions);
		jobStarted = DateTime.Now;
		await Task.Delay(2000);
		/////////////////////////////////     RELEASE CATALOG        /////////////////////////////////
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		//assert popup
		Console.WriteLine("direct release popup dialog displayed");
		await Expect(Page.Locator("#uiDirectRelease")).ToBeVisibleAsync(locatorVisibleAssertion);
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Direct Release" })).ToBeVisibleAsync(locatorVisibleAssertion);
		await Expect(Page.Locator("#uiDirectRelease")).ToContainTextAsync("OK");
		await Page.Locator("#uiDirectRelease").Locator("#uiDirectReleaseOk").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		//wait for dashboard
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buyer Dashboard" })).ToBeVisibleAsync(locatorVisibleAssertion);
		await Task.Delay(5000);

		//go to monitor
		Console.WriteLine("**********************************************");
		Console.WriteLine("go to monitor SET LIVE");
		Console.WriteLine("**********************************************");
		await GoWithErrWrap(CMB_MONITOR_URL, 60);
		await SetManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Set Live", jobStarted, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		//goto dashboard confirm status in production?
		////////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Catalog Released");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////
		//navigate to
		Console.WriteLine("Navigate to " + CMB_CATALOG_HOME_URL);
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);

		await Expect(Page).ToHaveURLAsync(CMB_CATALOG_HOME_URL);
		Console.WriteLine("return to dashboard assert catalog status is in production");

		//FILTER CATALOGS BY RELEASED STATUS
		await Page.GetByLabel("Status:").SelectOptionAsync(new[] { "released" });
		await Page.Locator("#uiSupplierId").FillAsync(TC04_SUPPLIER_ID);
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);

		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buyer Dashboard" })).ToBeVisibleAsync(locatorVisibleAssertion);

		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_catalog\"]")).ToContainTextAsync("In Production");

		//should now be on dashboard with direct release button available

		Console.WriteLine("navigate to search confirm catalog items have been updated by release");
		/*
		 manual test performs the following:
		Wait for 10 mins or until the Focs Export Task (incorrectly labelled as Fox export Task in the monitor) 
		in the Set Live Process completes 
 
		Open the following link:
 
		https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE05-05&VIEW_PASSWD=t3S4TcKp89Rqy&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp
 
		Click Suppliers
		Search with keyword "11-015.9025"	

		ONE result should be found with item name "Short description 11-015.9025 {DD/MM/YYYY}}"
		*/

		if (Environment == "PROD")
		//Wait 30s and hope indexing is donw
		{
			await Page.WaitForTimeoutAsync(30000);
			Console.WriteLine("go to https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE05-05&VIEW_PASSWD=t3S4TcKp89Rqy&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp");
			bool searchRendered = false;
			int searchPageAttempts = 0;
			while (searchRendered == false && searchPageAttempts < 10)
			{
				try
				{
					await Page.GotoAsync("https://newui.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=TESTCOE05-05&VIEW_PASSWD=t3S4TcKp89Rqy&USER_ID=HUBWOO&LANGUAGE=EN&COUNTRY=GB&EASYORDER=1&BRANDING=search5&HOOK_URL=https://newui.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp", pageGotoOptions);
					await Task.Delay(3000);
					Console.WriteLine("waiting for search");
					Console.WriteLine(Page.Url);
					searchRendered = true;
				}
				catch (Exception ex)
				{
					searchPageAttempts++;
					Console.WriteLine("Issue navigating to search, attempt " + searchPageAttempts.ToString());
					Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
					Console.WriteLine("exception: " + ex.Message);
				}
			}
			await Page.Locator("div[title='Suppliers']").ClickAsync(locatorClickOptions); //New navMenu
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Task.Delay(3000);
			await Page.GetByPlaceholder("Search for products by name,").FillAsync("11-015.9025");
			await Page.Locator("#goBtn").ClickAsync(locatorClickOptions);
			await Task.Delay(5000);
			await Expect(Page.GetByTestId("1d5e809108b50f0d252735d08e87fd11")).ToContainTextAsync("AUTOMATION TEST TC18a");
		}

		if (Environment == "QA")
		{
			Console.WriteLine("Wait 30s for indexing \n Then go to https://search.qa.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=SVVIEW1&VIEW_PASSWD=q0E2Aft3PQy18&USER_ID=SV&BRANDING=search5&LANGUAGE=EN&HOOK_URL=https://portal.qa.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp&ADMIN=1&COUNTRY=GB&EASYORDER=1");

			bool searchRendered = false;
			int searchPageAttempts = 0;
			while (searchRendered == false && searchPageAttempts < 10)
			{
				try
				{
					await Page.WaitForTimeoutAsync(30000);
					await Page.GotoAsync("https://search.qa.hubwoo.com/catalog/p3pLogin.jsp?VIEW_ID=SVVIEW1&VIEW_PASSWD=q0E2Aft3PQy18&USER_ID=SV&BRANDING=search5&LANGUAGE=EN&HOOK_URL=https://portal.qa.hubwoo.com/catalog/search5/customizings/default/oci_receiver.jsp&ADMIN=1&COUNTRY=GB&EASYORDER=1", pageGotoOptions);
					await Task.Delay(3000);
					Console.WriteLine("waiting for search");
					Console.WriteLine(Page.Url);
					searchRendered = true;
				}
				catch (Exception ex)
				{
					searchPageAttempts++;
					Console.WriteLine("Issue navigating to search, attempt " + searchPageAttempts.ToString());
					Console.WriteLine("exception: " + DateTime.Now.ToLongTimeString());
					Console.WriteLine("exception: " + ex.Message);
				}
			}

			await Page.Locator("div[title='Suppliers']").ClickAsync(locatorClickOptions); //New navMenu
			await Task.Delay(3000);
			await Page.GetByPlaceholder("Search for products by name,").FillAsync("11-015.9025");
			await Page.Locator("#goBtn").ClickAsync(locatorClickOptions);
			await Task.Delay(3000);
			await Expect(Page.GetByTestId("c938fa280435382ce9d4267d04d57de3")).ToContainTextAsync("AUTOMATION TEST TC18a");
		}

		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}


	[Test, Order(20)]
	[Category("CMBTests")]
	async public Task TC19_CMB_Supplier_Check_Routine()
	{
		/*
		  login as buyer admin
		  Precondition: Online Edit option is enabled for test company and test user account
			assumption that there is a supplier check routine that makes the short description field mandatory
			import catalog file with missing mandatory information for short description
			xlsx_prod_catalog_SCF_prod_file_base_checkroutine_supplier
			xlsx_qa_catalog_SCF_qa_file_base_checkroutine_supplier
			catalog import produces errors, supplier catalog chevron active with error
			error correction
			revalidation
			catalog release -> new version available
	  */
		Console.WriteLine("TC19_CMB_Supplier_Check_Routine");
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179308
		//prod cmb smoke tests 179371
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
		var simpleCatalogImportProcessId = "";
		await Page.SetViewportSizeAsync(1600, 900);
		await Page.GotoAsync(url, pageGotoOptions);
		Console.WriteLine("LOGIN AS " + BUYER_USER1_LOGIN);
		await SignInPortal(PORTAL_MAIN_URL, BUYER_USER1_LOGIN, BUYER_USER1_PASSWORD);

		DateTime today = DateTime.Now;
		string CurrentDate = $"{today.Month}/{today.Day}/{today.Year}";
		//click catalogs tab
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Buyer Dashboard");
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buyer Dashboard" })).ToBeVisibleAsync(locatorVisibleAssertion);

		//SUPPLIER_CHECK_ROUTINE_FILE  xlsx_prod_catalog_SCF_prod_file_base_checkroutine_supplier.xlsx
		//Note: The import file contains empty classification codes for 2 items, which we will update in a later step.
		Console.WriteLine("Go to Upload");
		await Page.Locator("#btnShowUploadModal").ClickAsync(locatorClickOptions);
		await Expect(Page.Locator("#uiUploadModul")).ToBeVisibleAsync(locatorVisibleAssertion);
		//upload xlsx_prod_catalog_SCF_prod_file_base_checkroutine_supplier.xlsx
		Console.WriteLine("select " + SUPPLIER_CHECK_ROUTINE_FILE + " file to upload");
		await Task.Delay(2000);
		await Page.Locator($"[id=\"uiFileSelect\"]").SetInputFilesAsync(new[] { TC01_CATALOG_FILE_PATH + SUPPLIER_CHECK_ROUTINE_FILE });
		//has file been added to the download list?
		//#uiUploadedFileList > table > tbody > tr > td:nth-child(1)
		await Expect(Page.Locator("#uiUploadedFileList > table > tbody > tr > td:nth-child(1)")).ToContainTextAsync(SUPPLIER_CHECK_ROUTINE_FILE);
		Console.WriteLine("set filetype");
		await Page.Locator($"[id=\"{SUPPLIER_CHECK_ROUTINE_FILE}_selectType\"]").SelectOptionAsync(new[] { "content" });
		await Task.Delay(2000);
		Console.WriteLine("Import " + SUPPLIER_CHECK_ROUTINE_FILE);
		await Page.GetByRole(AriaRole.Link, new() { Name = "Process files" }).ClickAsync(locatorClickOptions);
		DateTime jobstart = DateTime.Now;
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page.WaitForTimeoutAsync(2000);
		//confirm the uiUploadedFileList  Your upload files were placed in the process queue
		await Expect(Page.Locator("#uiUploadedFileList")).ToContainTextAsync("Your upload files were placed in the process queue", new LocatorAssertionsToContainTextOptions { Timeout = 60000 });

		//goto monitor
		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor SIMPLE CATALOG IMPORT");//href = "/srvs/BuyerCatalogs/monitor/MonitorBuyer"
		Console.WriteLine("**********************************************");
		await GoWithErrWrap(CMB_MONITOR_URL, 60);
		await SetManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Simple Catalog import", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Failed");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Simple Catalog Import failed as expected");
		Console.WriteLine("**********************************************");
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
		await CMBFilter("", TC04_SUPPLIER_ID);
		await Task.Delay(4000);
		//TC04_CATALOG_SELECTOR = \\37 7418_";
		//TC04_CATALOG_SELECTOR_ID = "\\37 7418";
		//TC19_DASHBOARD_CATALOGID = "6 2376_77418"

		Console.WriteLine("assert catalog status is Catalogs on error");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_allTasks_catalog\"]")).ToContainTextAsync("On Error");
		Console.WriteLine("click show more");
		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_allTasks_btnShowMore\"]").ClickAsync(locatorClickOptions);

		Console.WriteLine("assert supplier catalog chevron has 2 errors");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_allTasks_navWizard\"]")).ToContainTextAsync("Errors (2)");

		Console.WriteLine("assert that the supplier catalog chevron is active");
		//assert that supplier catalog chevron li nth-child(1) is active, i.e. has the class=active

		await Expect(Page.Locator($"#{TC04_CATALOG_SELECTOR_ID}_allTasks_navWizard > li:nth-child(1)")).ToHaveAttributeAsync("class", "active", new LocatorAssertionsToHaveAttributeOptions { Timeout = 60000 });

		Console.WriteLine("assert revalidate catalog button is disabled");//62376_77418_btnSupplierRevalidate class=btn btn-lg btn-success disabled  (prod)
																																			//63045_237593_btnSupplierRevalidate class=btn btn-lg btn-success disabled (qa)
		await Expect(Page.Locator($"#\\3{TC19_DASHBOARD_CATALOGID}_btnSupplierRevalidate")).ToHaveAttributeAsync("class", "btn btn-lg btn-success disabled", new LocatorAssertionsToHaveAttributeOptions { Timeout = 60000 });

		Console.WriteLine("click item view link for first error row");

		await Page.Locator($"#\\3{TC19_DASHBOARD_CATALOGID}_SupplierErrorReportItemsContent > table > tbody > tr > td:nth-child(7) > a").ClickAsync(locatorClickOptions);
		Console.WriteLine("ASSERT item view popup is displayed");
		await Expect(Page.Locator($"#\\3{TC19_DASHBOARD_CATALOGID}_uiItemView")).ToBeVisibleAsync(locatorVisibleAssertion);
		//assert popup dialog #\36 2376_77418_uiItemView

		if (Environment == "PROD")
		{
			//assert errorcolumn is classification code
			Console.WriteLine("assert errorcolumn is classification code");
			await Expect(Page.Locator($"[id=\"\\3{TC19_DASHBOARD_CATALOGID}_itemViewSupplierErrorColumnContent\"]")).ToContainTextAsync("Classification Code");
			Console.WriteLine("Correct both errors");
			await Page.Locator($"input[name=\"\\3{TC19_DASHBOARD_CATALOGID}_\\#_uiErrorDetailViewNewValue_\\#_10-020\\.5000_\\#_90005_\\#_-30001\"]").FillAsync(TC19_ERROR_CORRECTION_VALUE);
			await Page.Locator($"input[name=\"\\3{TC19_DASHBOARD_CATALOGID}_\\#_uiErrorDetailViewNewValue_\\#_10-020\\.5001_\\#_90005_\\#_-30001\"]").FillAsync(TC19_ERROR_CORRECTION_VALUE);
		}

		if (Environment == "QA")
		{
			//assert errorcolumn is classification code
			Console.WriteLine("assert errorcolumn is short description");
			await Expect(Page.Locator($"[id=\"\\3{TC19_DASHBOARD_CATALOGID}_itemViewSupplierErrorColumnContent\"]")).ToContainTextAsync("Short Description");
			Console.WriteLine("Correct both errors");
			//name="63045_237593_#_uiErrorDetailViewNewValue_#_11-015.5000_#_90012_#_-30001"
			//name="63045_237593_#_uiErrorDetailViewNewValue_#_11-015.9025_#_90012_#_-30001"
			await Page.Locator($"input[name=\"\\3{TC19_DASHBOARD_CATALOGID}_\\#_uiErrorDetailViewNewValue_\\#_11-015\\.5000_\\#_90012_\\#_-30001\"]").FillAsync("11-015.5000 Short Description");
			await Page.Locator($"input[name=\"\\3{TC19_DASHBOARD_CATALOGID}_\\#_uiErrorDetailViewNewValue_\\#_11-015\\.9025_\\#_90012_\\#_-30001\"]").FillAsync("11-015.9025 Short Description");
		}

		Console.WriteLine("Save All");
		await Page.Locator($"[id=\"\\3{TC19_DASHBOARD_CATALOGID}_saveAllSupplierItemViewDetails\"]").ClickAsync(locatorClickOptions);

		//takes a few seconds for the revalidate button to activate
		await Task.Delay(4000);

		Console.WriteLine("Click the Revalidate Catalog button");
		await Page.GetByRole(AriaRole.Link, new() { Name = "Revalidate Catalog" }).ClickAsync(locatorClickOptions);

		await Expect(Page.Locator($"[id=\"\\3{TC19_DASHBOARD_CATALOGID}_createRevalidationMessage\"]")).ToContainTextAsync("The revalidation of your catalog is in process.");
		Console.WriteLine("**********************************************");
		Console.WriteLine("go to monitor SIMPLE CATALOG IMPORT");
		Console.WriteLine("**********************************************");
		await GoWithErrWrap(CMB_MONITOR_URL, 60);
		ManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Simple Catalog import", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Simple catalog import now completed");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////
		await Page.WaitForTimeoutAsync(5000);
		await MonitorProcessStatueAsync(Page, "", "Release catalog", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("Release catalog completed");
		///////////////////////////////////////////////////////////////////////////////////////
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
		await CMBFilter("", TC04_SUPPLIER_ID);
		await Task.Delay(4000);
		Console.WriteLine("assert catalog status is New Version available"); //#\37 7418_allTasks_catalog > div > div.col-lg-10.col-md-9.col-sm-8 > div:nth-child(2) > div > h5:nth-child(2)
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_catalog\"]")).ToContainTextAsync("New Version available");
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());

	}

	[Test, Order(21)]
	[Category("CMBTests")]
	async public Task TC21_CMB_Customer_Check_Routine()
	{
		//upload a catalog that has an error that is identified by a buyer side check routine.
		//correct the error and revalidate, release to production
		//assumption is that we are starting with a catalog that is in the status new version available as per end of test TC19
		//assumption that there is a customer check routine that makes the long description field mandatory
		//buyer uploads catalog
		//is automatically released to buyer
		//buyer creates working version
		//error in long description
		Console.WriteLine("TC21_CMB_Customer_Check_Routine");
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179308
		//prod cmb smoke tests 179372
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
		await Page.GotoAsync(url, pageGotoOptions);
		await SignInPortal(PORTAL_MAIN_URL, BUYER_USER1_LOGIN, BUYER_USER1_PASSWORD);

		DateTime today = DateTime.Now;
		string CurrentDate = $"{today.Month}/{today.Day}/{today.Year}";
		//click catalogs tab
		Console.WriteLine("Waiting for " + CMB_CATALOG_HOME_URL);
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Buyer Dashboard");
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buyer Dashboard" })).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("Go to Upload");
		await Page.Locator("#btnShowUploadModal").ClickAsync(locatorClickOptions);
		await Expect(Page.Locator("#uiUploadModul")).ToBeVisibleAsync(locatorVisibleAssertion);
		//upload CUSTOMER_CHECK_ROUTINE_FILE = xlsx_prod_catalog_SCF_prod_file_base_checkroutine_customer.xlsx (prod)
		//Note: The import file contains empty long description for 1 catalog item, which we will update in a later step.
		Console.WriteLine("select " + CUSTOMER_CHECK_ROUTINE_FILE + " file to upload");
		await Task.Delay(2000);
		await Page.Locator($"[id=\"uiFileSelect\"]").SetInputFilesAsync(new[] { TC01_CATALOG_FILE_PATH + CUSTOMER_CHECK_ROUTINE_FILE });
		//has file been added to the download list?
		await Expect(Page.Locator("#uiUploadedFileList > table > tbody > tr > td:nth-child(1)")).ToContainTextAsync(CUSTOMER_CHECK_ROUTINE_FILE);
		Console.WriteLine("set filetype");
		await Page.Locator($"[id=\"{CUSTOMER_CHECK_ROUTINE_FILE}_selectType\"]").SelectOptionAsync(new[] { "content" });
		await Task.Delay(2000);
		Console.WriteLine("upload enrichment file");
		await Page.GetByRole(AriaRole.Link, new() { Name = "Process files" }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		//confirm the uiUploadedFileList  Your upload files were placed in the process queue
		await Page.WaitForTimeoutAsync(2000);
		await Expect(Page.Locator("#uiUploadedFileList")).ToContainTextAsync("Your upload files were placed in the process queue", new LocatorAssertionsToContainTextOptions { Timeout = 60000 });
		DateTime jobstart = DateTime.Now;
		//goto monitor - expectation New simple Catalog Import process and release catalog process complete with state "Finished OK"
		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor SIMPLE CATALOG IMPORT");//href = "/srvs/BuyerCatalogs/monitor/MonitorBuyer"
		Console.WriteLine("**********************************************");
		await GoWithErrWrap(CMB_MONITOR_URL, 60);
		await SetManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Simple Catalog import", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Simple Catalog Import succeeded");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////
		await Page.WaitForTimeoutAsync(5000);
		ManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Release catalog", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Release catalog completed");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("Go to dashboard what is the catalog status?");
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("CHECK THAT CATALOG STATUS IS NEW VERSION AVAILABLE");
		Console.WriteLine("**********************************************");
		await CMBFilter("", TC04_SUPPLIER_ID);
		await Task.Delay(4000);
		//TC04_CATALOG_SELECTOR = \\37 7418_";
		//TC04_CATALOG_SELECTOR_ID = "\\37 7418";
		//TC19_DASHBOARD_CATALOGID = "6 2376_77418"
		Console.WriteLine("assert catalog status is New Version available");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_allTasks_catalog\"]")).ToContainTextAsync("New Version available");
		Console.WriteLine("click show more");
		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_allTasks_btnShowMore\"]").ClickAsync(locatorClickOptions);
		Console.WriteLine("assert Supplier Catalog chevron if active");//#\37 7418_allTasks_navWizard > li.active
		await Expect(Page.Locator($"#{TC04_CATALOG_SELECTOR_ID}_allTasks_navWizard > li.active")).ToContainTextAsync("Supplier Catalog");

		Console.WriteLine("assert create working version button available");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_allTasks_tabSupplierCatalog\"]").GetByText("Create Working Version")).ToBeVisibleAsync();
		Console.WriteLine("assert reject catalog button is visible");//#\37 7418_allTasks_tabSupplierCatalog > div.catalog-actions.col-lg-7.col-md-7.col-sm-8 > div > div.pull-right > a.btn.btn-danger
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_allTasks_tabSupplierCatalog\"]").GetByText("Reject Catalog")).ToBeVisibleAsync();

		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("create working version");
		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_allTasks_tabSupplierCatalog\"]").GetByText("Create Working Version").ClickAsync(locatorClickOptions);
		jobstart = DateTime.Now;
		await Task.Delay(2000);
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("go to monitor LOAD CATALOG");
		Console.WriteLine("**********************************************");
		await GoWithErrWrap(CMB_MONITOR_URL, 60);
		await SetManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Load Catalog", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Load Catalog now completed");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
		await CMBFilter("", TC04_SUPPLIER_ID);
		await Task.Delay(4000);
		Console.WriteLine("assert catalog status is On Error"); //#\37 7418_allTasks_catalog > div > div.col-lg-10.col-md-9.col-sm-8 > div:nth-child(2) > div > h5:nth-child(2)
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_catalog\"]")).ToContainTextAsync("On Error");

		Console.WriteLine("Click Show More " + $"[id =\"{TC04_CATALOG_SELECTOR_ID}_allTasks_btnShowMore\"]");
		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_allTasks_btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		Console.WriteLine("assert revalidate catalog button is enabled");//#\37 7418_btnRevalidate class=btn btn-lg btn-success
		await Expect(Page.Locator($"#{TC04_CATALOG_SELECTOR_ID}_btnRevalidate")).ToHaveAttributeAsync("class", "btn btn-lg btn-success", new LocatorAssertionsToHaveAttributeOptions { Timeout = 60000 });

		if (Environment == "PROD")
		{
			Console.WriteLine("assert the error correction chevron is active and it has 1 error");

			await Expect(Page.Locator($"#{TC04_CATALOG_SELECTOR_ID}_allTasks_navWizard > li.active")).ToContainTextAsync("Error Correction (1)");
		}

		if (Environment == "QA")
		{
			Console.WriteLine("assert the error correction chevron is active and it has 2 errors");

			await Expect(Page.Locator($"#{TC04_CATALOG_SELECTOR_ID}_allTasks_navWizard > li.active")).ToContainTextAsync("Error Correction (2)");
		}

		Console.WriteLine("click item view link for first error row");//#\37 7418_ErrorReportItemsContent > table > tbody > tr > td:nth-child(7) > a
		await Page.Locator($"#{TC04_CATALOG_SELECTOR_ID}_ErrorReportItemsContent > table > tbody > tr > td:nth-child(7) > a").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(4000);
		Console.WriteLine("ASSERT item view popup is displayed");
		/* //wait for loadingScreen to disappear
				Console.WriteLine("waiting for loadingScreen to disappear");
				var attempt = 0;
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
		*/
		await Expect(Page.Locator($"#{TC04_CATALOG_SELECTOR_ID}_uiItemView")).ToBeVisibleAsync(locatorVisibleAssertion);//#\37 7418_uiItemView
																																																										//assert popup dialog #\37 7418_uiItemView
		if (Environment == "PROD")
		{
			//this markup is going to be specific to the error is specific to the test environ
			//assert errorcolumn is classification code
			Console.WriteLine("assert error column is Long Description");//    await Expect(Page.Locator("[id=\"\\37 7418_itemViewErrorColumnContent\"]")).ToContainTextAsync("Long Description");
			await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_itemViewErrorColumnContent\"]")).ToContainTextAsync("Long Description");

			Console.WriteLine("Correct the error");//name="77418_#_uiErrorDetailViewNewValue_#_10-020.5001_#_90014_#_-30001"
			await Page.Locator($"input[name=\"{TC04_CATALOG_SELECTOR_ID}_\\#_uiErrorDetailViewNewValue_\\#_10-020\\.5001_\\#_90014_\\#_-30001\"]").FillAsync(TC20_ERROR_CORRECTION_VALUE);
		}

		if (Environment == "QA")
		{
			//this markup is going to be specific to the error is specific to the test environ
			//assert errorcolumn is classification code
			Console.WriteLine("assert error column is Long Description");//    await Expect(Page.Locator("[id=\"\\37 7418_itemViewErrorColumnContent\"]")).ToContainTextAsync("Long Description");

			await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_itemViewErrorColumnContent\"]")).ToContainTextAsync("Long Description");

			Console.WriteLine("Correct the errors");//name="77418_#_uiErrorDetailViewNewValue_#_10-020.5001_#_90014_#_-30001"

			//name="237593_#_uiErrorDetailViewNewValue_#_11-015.5000_#_90014_#_-30001"	(qa)
			await Page.Locator($"input[name=\"{TC04_CATALOG_SELECTOR_ID}_\\#_uiErrorDetailViewNewValue_\\#_11-015\\.5000_\\#_90014_\\#_-30001\"]").FillAsync(TC20_ERROR_CORRECTION_VALUE);

			//name = "237593_#_uiErrorDetailViewNewValue_#_11-015.9025_#_90014_#_-30001"  (qa)
			await Page.Locator($"input[name=\"{TC04_CATALOG_SELECTOR_ID}_\\#_uiErrorDetailViewNewValue_\\#_11-015\\.9025_\\#_90014_\\#_-30001\"]").FillAsync(TC20_ERROR_CORRECTION_VALUE);
		}

		Console.WriteLine("Save All");//Locator("[id=\"\\37 7418_saveAllItemViewDetails\"]")
		await Page.RunAndWaitForResponseAsync(async () =>
		{
			await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_saveAllItemViewDetails\"]").ClickAsync(locatorClickOptions);
		}, response => response.Url.Contains("SaveNewErrorValuesInDetailView") && response.Status == 200, new PageRunAndWaitForResponseOptions { Timeout = 60000 });
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		//wait for loadingScreen to disappear
		/*Console.WriteLine("waiting for loadingScreen to disappear");
attempt = 0;
isLoadingScreenVisible = await Page.Locator("#loadingScreen").IsVisibleAsync();
while (isLoadingScreenVisible && attempt < 10)
{
try
{
await Expect(Page.Locator("#loadingScreen")).Not.ToBeVisibleAsync(locatorVisibleAssertion);
isLoadingScreenVisible = false;
break;
}
catch
{
attempt++;
isLoadingScreenVisible = await Page.Locator("#loadingScreen").IsVisibleAsync();
}
}*/
		await Task.Delay(3000);
		Console.WriteLine("Click the Revalidate Catalog button");
		//Replace original click action to include api call check
		await Page.RunAndWaitForResponseAsync(async () =>
		{
			await Page.GetByRole(AriaRole.Link, new() { Name = "Revalidate Catalog" }).ClickAsync(new LocatorClickOptions { Force = true, Timeout = 100000 });
			jobstart = DateTime.Now;
		}, response => response.Url.Contains("RevalidateCatalog") && response.Status == 200, new PageRunAndWaitForResponseOptions { Timeout = 60000 });
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(3000);
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_createRevalidationMessage\"]")).ToContainTextAsync("The revalidation of your catalog is in process.");
		Console.WriteLine("**********************************************");
		Console.WriteLine("go to monitor REVALIDATE CATALOG");
		Console.WriteLine("**********************************************");
		await GoWithErrWrap(CMB_MONITOR_URL, 60);
		ManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Revalidate catalog", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Revalidate catalog now completed");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
		Console.WriteLine("Go to dashboard what is the catalog status?");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("CHECK CATALOG STATUS IS NOW 'CATALOG TO APPROVE' ");
		Console.WriteLine("**********************************************");
		//filter catalogs 
		await CMBFilter("", TC04_SUPPLIER_ID);
		await Task.Delay(4000);
		Console.WriteLine("assert catalog status is Catalog to approve"); //#\37 7418_allTasks_catalog > div > div.col-lg-10.col-md-9.col-sm-8 > div:nth-child(2) > div > h5:nth-child(2)
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_allTasks_catalog\"]")).ToContainTextAsync("Catalog to approve");//#\37 7418_allTasks_catalog
		Console.WriteLine("click show more");
		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR_ID}_allTasks_btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		Console.WriteLine("assert that the active chevron is approve items");//#\37 7418_allTasks_navWizard > li.active
		await Expect(Page.Locator($"#{TC04_CATALOG_SELECTOR_ID}_allTasks_navWizard > li.active")).ToContainTextAsync("Approve Items");
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(22)]
	[Category("CMBTests")]
	async public Task TC22_CMB_Diffing_Report()
	{
		//supplier uploads base catalog and releases
		//buyer creates working version, approves and releases
		//supplier uploads updated catalog version and releases to buyer
		//buyer creates working version of updated catalog

		Console.WriteLine("TC22_CMB_Diffing_Report");
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179308
		//prod cmb smoke tests 179373
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
		DateTime thisTestStarted = DateTime.UtcNow;
		Console.WriteLine("this test started: " + thisTestStarted.ToLongTimeString());
		Console.WriteLine("Start with supplier uploading base version of catalog");
		// TESTSCUSTCDO 1
		string url = PORTAL_LOGIN;//https://portal.hubwoo.com/auth/login?ReturnUrl=%2Fmain%2F

		await Page.SetViewportSizeAsync(1600, 900);
		// Login to portal
		await SignInPortal(PORTAL_MAIN_URL, SUPPLIER_USER1_LOGIN, SUPPLIER_USER1_PASSWORD);
		await Expect(Page.Locator("#notificationsWidget")).ToContainTextAsync("Notifications");
		Console.WriteLine("Go Catalogs Dashboard");
		await GoWithErrWrap(CMS_CATALOG_HOME_URL, 60);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("My Catalogs");
		Console.WriteLine("filter catalogs via customer catalog id");
		await Page.GetByLabel("Customer ID").FillAsync(TC01_CUSTOMER_ID);
		//Search and make sure expected result are get
		await Page.RunAndWaitForResponseAsync(async () =>
		{
			await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
		}, response => response.Url.Contains("FilterCatalogs") && response.Status == 200);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(1000);//fails when this is removed
		await Expect(Page.GetByText(TC01_CUSTOMERNAME)).ToBeVisibleAsync();
		Console.WriteLine("click show more");
		await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Show more" })).ToBeVisibleAsync(locatorVisibleAssertion);
		//await Page.GetByRole(AriaRole.Link, new() { Name = "Show more" }).ClickAsync(locatorClickOptions);
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Task.Delay(2000);
		await Page.GetByRole(AriaRole.Link, new() { Name = "Upload Files" }).ClickAsync(locatorClickOptions);
		Console.WriteLine("File to upload: " + CMB_DIFFING_REPORT1);
		//TCO1_CATALOG_SELECTOR = "\\36 2376_";
		//TCO1_CATALOG_SELECTOR_ID = "36 2376_";
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}fileSelect\"]").SetInputFilesAsync(new[] { TC01_CATALOG_FILE_PATH + CMB_DIFFING_REPORT1 });
		//await Expect(Page.GetByRole(AriaRole.Table)).ToContainTextAsync(CMB_DIFFING_REPORT1);//#\36 2376_uploadFileList
		// CMB_DIFFING_REPORT1 = "xlsx_prod_catalog_SCF_prod_file_base.xlsx";
		// CMB_DIFFING_REPORT2 = "xlsx_prod_catalog_SCF_prod_file_updated.xlsx";
		//#\36 2376_xlsx_prod_catalog_SCF_prod_file_base\.zip_selectType
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}{CMB_DIFFING_REPORT1.Replace(".xlsx", ".zip")}_selectType\"]").SelectOptionAsync(new[] { "content" });
		Console.WriteLine("To process file");
		DateTime jobstart = DateTime.Now;
		await Task.Delay(2000);//fails when this is removed
		await Page.GetByRole(AriaRole.Link, new() { Name = "Process files" }).ClickAsync(locatorClickOptions);
		Console.WriteLine("******************************************************");
		Console.WriteLine("Go to monitor SIMPLE CATALOG IMPORT");
		Console.WriteLine("******************************************************");
		await GoWithErrWrap(CMS_MONITOR_URL, 60);
		await SetManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Simple Catalog import", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		////////////////////////////////////////////////////////////////////
		Console.WriteLine("******************************************************");
		Console.WriteLine("Catalog imported");
		Console.WriteLine("******************************************************");
		////////////////////////////////////////////////////////////////////

		////////////////////////////////////////////////////////
		//           SUPPLIER  RELEASES BASE CATALOG VERSION TO BUYER
		////////////////////////////////////////////////////////

		Console.WriteLine("navigate to catalog dashboard home " + CMS_CATALOG_HOME_URL);
		await GoWithErrWrap(CMS_CATALOG_HOME_URL, 60);
		//THIS SHOULD BE A NAVIGATE TO RATHER THAN A BUTTON CLICK ON THE MONITOR PAGE AS..
		//in test log see    <div class="modal-backdrop fade"></div> intercepts pointer events so click is not possible
		Console.WriteLine("filter catalogs via customer catalog id");
		await Page.GetByLabel("Customer ID").FillAsync(TC01_CUSTOMER_ID);
		Console.WriteLine("click search");
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
		await Task.Delay(6000);
		Console.WriteLine("click show more");
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(3000);
		//is the submit catalog link expanded?
		//sometimes get here and the upload files chevron is active??
		//click the submit chevron
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}tab4_link\"]").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(3000);
		//assert the div displaying the text 'Currently, this catalog is set to "Manual" Submit Mode.'  is visible
		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitModeText\"]")).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine($"assert [id={TCO1_CATALOG_SELECTOR}submitCat  button is visible");
		/*
		 the submit catalog chevron is not active why?
		 2024-03-21T20:53:16.551Z pw:api   locator resolved to <div id="62376_submitModeText">Currently, this catalog is set to "Manual" SubmitΓÇª</div>
		 2024-03-21T20:53:16.551Z pw:api   unexpected value "hidden" 
		 */
		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitCat\"]")).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("*****************************************************");
		Console.WriteLine("click SUBMIT catalog button");
		Console.WriteLine("*****************************************************");
		jobstart = DateTime.Now;
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitCat\"]").ClickAsync(locatorClickOptions);  //e.g. #\36 2376_submitCat
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		//release catalog  62376_submitCat
		await Task.Delay(4000);
		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitCatalogMessage\"]")).ToContainTextAsync("Your catalog was placed in the process queue and will be submitted to your customer. Please refresh your screen (press F5) in a few seconds or go to Monitor for detailed process information.");
		Console.WriteLine("******************************************************");
		Console.WriteLine("Go to monitor RELEASE CATALOG");//href = "/srvs/CatalogManager/monitor/MonitorSupplier"
		Console.WriteLine("******************************************************");
		await GoWithErrWrap(CMS_MONITOR_URL, 60);
		await SetManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Release catalog", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Catalog Released to buyer");
		Console.WriteLine("**********************************************");
		////////////////////////////////////////////////////////////////////
		//Logout
		await SignOut();
		//////////////////////////////////////////////////////////////
		//CMB LOGIN
		/////////////////////////////////////////////////////////////
		Console.WriteLine("LOGIN AS " + BUYER_USER1_LOGIN);
		await SignInPortal(PORTAL_MAIN_URL, BUYER_USER1_LOGIN, BUYER_USER1_PASSWORD);

		//click catalogs tab
		Console.WriteLine("Go Catalogs Dashboard");
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 120);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Buyer Dashboard");

		//////////////////////////////////////////////////////
		///  CREATE WORKING VERSION
		//////////////////////////////////////////////////////
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buyer Dashboard" })).ToBeVisibleAsync(locatorVisibleAssertion);

		Console.WriteLine("filter catalogs via supplier catalog id");
		await Page.Locator("#uiSupplierId").FillAsync(TC04_SUPPLIER_ID);
		await Page.GetByLabel("Status:").SelectOptionAsync(new[] { "newversion" });
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		Console.WriteLine("filter catalogs using supplier id " + TC04_SUPPLIER_ID);
		//note on prod filtering for supplier id testsupcdo2 results in 2 catalogs! so need to be aware of locator strictness
		//could sort by status of New version available also?
		//in which case we should expect contentAllTasks to contain only 1 <div class=row>
		var count = Page.Locator("#contentAllTasks > div[class=\"row\"]").CountAsync();

		Console.WriteLine("catalog rows on page 1 of the dashboard: " + count.Result.ToString());
		//could assert that this is 1 here?
		await Task.Delay(4000);

		//assert catalog exists
		Console.WriteLine("assert catalog exists");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_catalog\"]")).ToContainTextAsync(TC04_SUPPLIERNAME);

		//assert new version available
		Console.WriteLine("assert that the catalog status is new  version available");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_catalog\"]")).ToContainTextAsync("New Version available");

		Console.WriteLine("click show more");
		//await Expect(Page.Locator("[id=\"\\37 7418_allTasks_btnShowMore\"]")).ToBeVisibleAsync();
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_btnShowMore\"]")).ToBeVisibleAsync(locatorVisibleAssertion);

		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_btnShowMore\"]").ClickAsync(locatorClickOptions);

		/////////////////////////////////       BUYER CREATES WORKING VERSION                  /////////////////////////////////
		//click supplier CATALOG chevron
		await Page.Locator($"[href*=\"#{TC04_SUPPLIER_METACATID}_allTasks_tabSupplierCatalog\"]").ClickAsync(locatorClickOptions);
		Console.WriteLine("create working version");
		//create working version
		jobstart = DateTime.Now;
		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_tabSupplierCatalog\"]").GetByText("Create Working Version").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(3000);
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_catalog\"]")).ToContainTextAsync("Waiting for processing");

		//go to monitor
		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor LOAD CATALOG");//href = "/srvs/BuyerCatalogs/monitor/MonitorBuyer"
		Console.WriteLine("**********************************************");
		await GoWithErrWrap(CMB_MONITOR_URL, 60);
		await SetManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Load Catalog", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Catalog Loaded");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
		/////////////////////////////////       APPROVE CATALOG ITEMS    /////////////////////////////////
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Buyer Dashboard");

		//filter catalogs 
		Console.WriteLine("filter catalogs via supplier catalog id and status Catalog to approve");
		await Page.Locator("#uiSupplierId").FillAsync(TC04_SUPPLIER_ID);
		await Page.GetByLabel("Status:").SelectOptionAsync(new[] { "approved" });
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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

		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_tabApproveItems\"]").GetByRole(AriaRole.Link, new() { Name = "Review Items" }).ClickAsync(locatorClickOptions);
		//wait for uiitems table
		//#uiItems
		Console.WriteLine("wait for uiitems: start " + DateTime.Now.ToLongTimeString());
		await Page.WaitForSelectorAsync("#uiItems", new PageWaitForSelectorOptions { Timeout = 60000 });
		Console.WriteLine("wait for uiitems: end " + DateTime.Now.ToLongTimeString());
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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

		if (Environment == "PROD")
		{
			Console.WriteLine("Assert that the Confirm button (#uiSubmitAction) is disabled");
			await Expect(Page.Locator("#uiSubmitAction")).ToBeDisabledAsync();
		}

		Console.WriteLine("Assert that the Submit Catalog link (#uiGoToReleaseTab) is visible");
		await Expect(Page.Locator("#uiGoToReleaseTab")).ToBeVisibleAsync(locatorVisibleAssertion);

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

		/////////////////////////////////            SUBMIT CATALOG  TO SEARCH              /////////////////////////////////

		Console.WriteLine("click the Submit Catalog link (#uiGoToReleaseTab)");
		await Page.Locator("#uiGoToReleaseTab").ClickAsync(locatorClickOptions);//doesn't submit but redirects user to dashboard with the release catalog chevron for the specific catalog in focus and active

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
		//Slow down actions, too fast will break test
		await Page.WaitForTimeoutAsync(2000);
		jobstart = DateTime.Now;
		//wait for dashboard
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buyer Dashboard" })).ToBeVisibleAsync(locatorVisibleAssertion);

		Console.WriteLine("**********************************************");
		Console.WriteLine("go to monitor SET LIVE");
		Console.WriteLine("**********************************************");
		//go to monitor
		await GoWithErrWrap(CMB_MONITOR_URL, 60);
		ManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Set Live", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		//goto dashboard confirm status in prodution?
		////////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Set Live completed");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
		await Expect(Page).ToHaveURLAsync(CMB_CATALOG_HOME_URL);
		Console.WriteLine("return to dashboard assert catalog status is in production");

		//FILTER CATALOGS BY RELEASED STATUS
		await Page.GetByLabel("Status:").SelectOptionAsync(new[] { "released" });
		await Page.Locator("#uiSupplierId").FillAsync(TC04_SUPPLIER_ID);
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(2000);
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buyer Dashboard" })).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("assert that status is now 'In Production'");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_catalog\"]")).ToContainTextAsync("In Production");
		///Logout
		ReloadPageIfBackrop();
		await SignOut();

		///////////////////////////////////////////////////////////////////////////////////////////////
		///LOGIN AS SUPPLIER UPLOAD UPDATED CATALOG
		///////////////////////////////////////////////////////////////////////////////////////////////
		await SignInPortal(PORTAL_MAIN_URL, SUPPLIER_USER1_LOGIN, SUPPLIER_USER1_PASSWORD);
		Console.WriteLine("Go Catalogs Dashboard");
		await GoWithErrWrap(CMS_CATALOG_HOME_URL, 60);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("My Catalogs");
		Console.WriteLine(Page.Url);

		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "My Catalogs" })).ToBeVisibleAsync(locatorVisibleAssertion);
		///Filter customer
		Console.WriteLine("filter catalogs via customer catalog id");
		await Page.GetByLabel("Customer ID").FillAsync(TC01_CUSTOMER_ID);
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("My Catalogs");
		await Task.Delay(4000);//fails when this is removed
													 //Upload diffing catalogs
		await Expect(Page.GetByText(TC01_CUSTOMERNAME)).ToBeVisibleAsync();
		Console.WriteLine("click show more");
		await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Show more" })).ToBeVisibleAsync(locatorVisibleAssertion);
		//await Page.GetByRole(AriaRole.Link, new() { Name = "Show more" }).ClickAsync(locatorClickOptions);
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Task.Delay(2000);
		await Page.GetByRole(AriaRole.Link, new() { Name = "Upload Files" }).ClickAsync(locatorClickOptions);
		Console.WriteLine("select catalog file to upload: " + CMB_DIFFING_REPORT2);
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}fileSelect\"]").SetInputFilesAsync(new[] { TC01_CATALOG_FILE_PATH + CMB_DIFFING_REPORT2 });
		//await Expect(Page.GetByRole(AriaRole.Table)).ToContainTextAsync(CMB_DIFFING_REPORT2);
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}{CMB_DIFFING_REPORT2.Replace(".xlsx", ".zip")}_selectType\"]").SelectOptionAsync(new[] { "content" });
		Console.WriteLine("upload catalog file");
		await Task.Delay(1000);//fails when this is removed
		jobstart = DateTime.Now;
		await Page.GetByRole(AriaRole.Link, new() { Name = "Process files" }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(3000);
		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}uploadFileList\"]")).ToContainTextAsync("Your upload files were placed in the process queue. They will be processed as soon as possible.Please refresh your screen (press F5) in a few seconds or go to Monitor for detailed process information.");
		Console.WriteLine("******************************************************");
		Console.WriteLine("Go to monitor SIMPLE CATALOG IMPORT");
		Console.WriteLine("******************************************************");
		await GoWithErrWrap(CMS_MONITOR_URL, 60);
		ManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Simple Catalog import", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		////////////////////////////////////////////////////////////////////
		Console.WriteLine("******************************************************");
		Console.WriteLine("Catalog upload complete");
		Console.WriteLine("******************************************************");
		////////////////////////////////////////////////////////
		//  SUPPLIER RELEASES UPDATED CATALOG TO BUYER
		////////////////////////////////////////////////////////
		Console.WriteLine("navigate to catalog dashboard home " + CMS_CATALOG_HOME_URL);
		await GoWithErrWrap(CMS_CATALOG_HOME_URL, 60);
		///Filter customer TC01_CUSTOMER_ID
		CMSFilter("", TC01_CUSTOMER_ID);
		//The following guard will keep refresh dashboard until status become imported
		//Check catalog status, if not Imported then wait 1 sec then search again (don't know why catalog status is not imported sometime
		DateTime startTime = DateTime.Now;
		DateTime curTime = DateTime.Now;
		TimeSpan dur = curTime - startTime;
		while (await Page.Locator("div[id$=')_catalog']").GetByText("Imported").CountAsync() == 0 && dur <= TimeSpan.FromMinutes(2))
		{
			Console.WriteLine("No keyword Imported found on screen");
			await Page.ReloadAsync();
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Task.Delay(2000);
			curTime = DateTime.Now;
			dur = curTime - startTime;
		}

		Console.WriteLine("click show more");
		await Task.Delay(2000);
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(2000);

		//is the submit catalog link expanded?

		//sometimes get here and the upload files chevron is active??

		//click the submit chevron
		await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}tab4_link\"]").ClickAsync(locatorClickOptions);
		await Task.Delay(2000);
		//assert the div displaying the text 'Currently, this catalog is set to "Manual" Submit Mode.'  is visible
		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitModeText\"]")).ToBeVisibleAsync(locatorVisibleAssertion);

		Console.WriteLine($"assert [id={TCO1_CATALOG_SELECTOR}submitCat  button is visible");
		/*
		 the submit catalog chevron is not active why?
		 2024-03-21T20:53:16.551Z pw:api   locator resolved to <div id="62376_submitModeText">Currently, this catalog is set to "Manual" SubmitΓÇª</div>
		 2024-03-21T20:53:16.551Z pw:api   unexpected value "hidden" 
		 */

		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitCat\"]")).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("*****************************************************");
		Console.WriteLine("click SUBMIT catalog button");
		Console.WriteLine("*****************************************************");
		await Page.RunAndWaitForResponseAsync(async () =>
			{
				await Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitCat\"]").ClickAsync(locatorClickOptions);
			}, response => response.Url.Contains("ReleaseCatalog") && response.Status == 200);
		jobstart = DateTime.Now;
		await Task.Delay(4000);
		await Expect(Page.Locator($"[id=\"{TCO1_CATALOG_SELECTOR}submitCatalogMessage\"]")).ToContainTextAsync("Your catalog was placed in the process queue and will be submitted to your customer. Please refresh your screen (press F5) in a few seconds or go to Monitor for detailed process information.");
		Console.WriteLine("******************************************************");
		Console.WriteLine("Go to monitor RELEASE CATALOG");//href = "/srvs/CatalogManager/monitor/MonitorSupplier"
		Console.WriteLine("******************************************************");
		await GoWithErrWrap(CMS_MONITOR_URL, 60);
		ManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Release catalog", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Catalog Released");
		Console.WriteLine("**********************************************");
		////////////////////////////////////////////////////////////////////
		//   Logout CMS
		await SignOut();
		//////////////////////////////////////////////////////////////
		//CMB LOGIN, BUYER CREATES NEW WORKING VERSION OF UPDATED CATALOG AND VIEWS DIFFING INFO
		/////////////////////////////////////////////////////////////
		await SignInPortal(PORTAL_MAIN_URL, BUYER_USER1_LOGIN, BUYER_USER1_PASSWORD);
		Console.WriteLine("Go Catalogs Dashboard");
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Buyer Dashboard");

		//////////////////////////////////////////////////////
		Console.WriteLine("filter catalogs via supplier catalog id");
		await CMBFilter("", TC04_SUPPLIER_ID);
		//note on prod filtering for supplier id testsupcdo2 results in 2 catalogs! so need to be aware of locator strictness
		//could sort by status of New version available also?
		//in which case we should expect contentAllTasks to contain only 1 <div class=row>
		await (count = Page.Locator("#contentAllTasks > div[class=\"row\"]").CountAsync());

		Console.WriteLine("catalog rows on page 1 of the dashboard: " + count.Result.ToString());
		//could assert that this is 1 here?
		await Task.Delay(4000);

		//assert catalog exists
		Console.WriteLine("assert catalog exists");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_catalog\"]")).ToContainTextAsync(TC04_SUPPLIERNAME);

		//assert new version available
		Console.WriteLine("assert that the catalog status is new  version available");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_catalog\"]")).ToContainTextAsync("New Version available");

		Console.WriteLine("click show more");
		//await Expect(Page.Locator("[id=\"\\37 7418_allTasks_btnShowMore\"]")).ToBeVisibleAsync();
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_btnShowMore\"]")).ToBeVisibleAsync(locatorVisibleAssertion);

		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_btnShowMore\"]").ClickAsync(locatorClickOptions);
		//assert Supplier Catalog chevron is active
		Console.WriteLine("assert Supplier Catalog chevron is active");
		await Expect(Page.Locator($"#{TC04_CATALOG_SELECTOR}allTasks_navWizard > li.active")).ToContainTextAsync("Supplier Catalog");

		Console.WriteLine("assert view diffing report link is available");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_supplierCatalogDiffing\"]")).ToContainTextAsync("View Diffing Report");

		Console.WriteLine("assert download diffing report link is available");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_supplierCatalogDiffing\"]")).ToContainTextAsync("Download Diffing Report");

		//view diffing
		Console.WriteLine("view diffing report in ui");
		////////////////////////////////////////////////

		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_supplierCatalogDiffing\"]").GetByText("View Diffing Report").ClickAsync(locatorClickOptions);
		await Task.Delay(4000);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Diffing Report");

		Console.WriteLine("assert diffing report items in ui");
		if (Environment == "PROD")
		{
			var itemId = await Page.Locator("#mainRow-0 > td:nth-child(1) > a").TextContentAsync();
			Console.WriteLine("check for diff item on #mainRow-0: " + itemId);
			//expect supplier item number to be one of 01-081[.]9010|01-655[.]1000|02-570[.]1000|02-570[.]9020|10-020[.]5000|10-020[.]5001|11-015[.]5000|11-015[.]902
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
			var itemId = await Page.Locator("#mainRow-0 > td:nth-child(1) > a").TextContentAsync();
			Console.WriteLine("check for diff item on #mainRow-0: " + itemId);
			if (!QaSupplierItemIdValidator.IsExpectedQASupplierItemNumber(itemId))
			{
				throw new Exception($"unexpected supplier item number {itemId} in #mainRow-0");
			}

			itemId = await Page.Locator("#mainRow-1 > td:nth-child(1) > a").TextContentAsync();
			Console.WriteLine("check for diff item on #mainRow-1: " + itemId);
			if (!QaSupplierItemIdValidator.IsExpectedQASupplierItemNumber(itemId))
			{
				throw new Exception($"unexpected supplier item number {itemId} in #mainRow-1");
			}

		}


		////////////////////////////////////////////////
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Buyer Dashboard");
		Console.WriteLine("download diffing report");
		//download diffing report

		Console.WriteLine("click show more");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_btnShowMore\"]")).ToBeVisibleAsync(locatorVisibleAssertion);

		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Task.Delay(4000);
		Console.WriteLine("click the download diffing report link");
		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_supplierCatalogDiffing\"]").GetByText("Download Diffing Report").ClickAsync(locatorClickOptions);
		jobstart = DateTime.Now;
		Console.WriteLine("Goto Monitor Template Export");
		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor TEMPLATE EXPORT JOB");
		Console.WriteLine("**********************************************");
		await GoWithErrWrap(CMB_MONITOR_URL, 60);
		await Page.WaitForTimeoutAsync(1000);
		await SetManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Template Export", jobstart, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Template Export succeeded");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////
		await GoWithErrWrap(BUYER_ADMIN_DOWNLOAD_URL, 60);
		Console.WriteLine("go to download page");
		/////////////////////////////////////
		//expand the reporting panel
		////////////////////////////////////
		// await Page.GetByRole(AriaRole.Link, new() { Name = "New Download" }).ClickAsync(locatorClickOptions); //It does not look like needed 
		//Set filter to search diffing report
		//await Page.GetByLabel("Reports").SelectOptionAsync(new[] { "diffingreport" });
		await Page.Locator("//*[@id=\"uiTemplateTypeFilter\"]").SelectOptionAsync(new[] { "diffingreport" });
		await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
		//assert contents of excel using closedxml
		await Task.Delay(5000);
		//download the file
		var waitForDownloadTask = Page.WaitForDownloadAsync();
		//get link
		//e.g https://portal.hubwoo.com/srvs/omnicontent/templatearchive/21316290_TESTSUPCDO2_TESTCUSTCDO-0001_407.1_406.1_diffing_report.zip

		var link = await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(8) > a").GetAttributeAsync("href");

		Console.WriteLine("Download " + link);
		await Page.Locator("#itemListContainer > tr:nth-child(1) > td:nth-child(8) > a").First.ClickAsync(locatorClickOptions);

		var download = await waitForDownloadTask;

		var fileName = $"{downloadPath}TC22_{download.SuggestedFilename}";

		Console.WriteLine("File downloaded to " + fileName);

		// Wait for the download process to complete and save the downloaded file somewhere
		await download.SaveAsAsync(fileName);

		Console.WriteLine("unzip file " + fileName);
		//unzip the file to downloadPath 
		string excelFileName = $"{downloadPath}{ExtractZipFile(fileName, downloadPath)}";

		Console.WriteLine("use ClosedXML to validate contents of file: " + excelFileName);
		//assert contents of xlsx
		Console.WriteLine("assert contents of xlsx file " + excelFileName);
		try
		{
			using var xlWorkbook = new XLWorkbook(excelFileName);
			var ws1 = xlWorkbook.Worksheet(1);
			if (Environment == "PROD")
			{
				var itemid = ws1.Cell("A2").GetValue<string>();
				Console.WriteLine("assert itemd id cell A2:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"Unexpected item id {itemid} cell A2");
				}

				itemid = ws1.Cell("A3").GetValue<string>();
				Console.WriteLine("assert itemd id cell A3:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"Unexpected item id {itemid} cell A3");
				}

				itemid = ws1.Cell("A4").GetValue<string>();
				Console.WriteLine("assert itemd id cell A4:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"Unexpected item id {itemid} cell A4");
				}

				itemid = ws1.Cell("A5").GetValue<string>();
				Console.WriteLine("assert itemd id cell A5:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"Unexpected item id {itemid} cell A5");
				}

				itemid = ws1.Cell("A6").GetValue<string>();
				Console.WriteLine("assert itemd id cell A6:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"Unexpected item id {itemid} cell A6");
				}

				itemid = ws1.Cell("A7").GetValue<string>();
				Console.WriteLine("assert itemd id cell A7:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"Unexpected item id {itemid} cell A7");
				}

				itemid = ws1.Cell("A8").GetValue<string>();
				Console.WriteLine("assert itemd id cell A8:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"Unexpected item id {itemid} cell A8");
				}

				itemid = ws1.Cell("A9").GetValue<string>();
				Console.WriteLine("assert itemd id cell A9:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"Unexpected item id {itemid} cell A9");
				}

				itemid = ws1.Cell("A10").GetValue<string>();
				Console.WriteLine("assert itemd id cell A10:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"Unexpected item id {itemid} cell A10");
				}

				itemid = ws1.Cell("A11").GetValue<string>();
				Console.WriteLine("assert itemd id cell A11:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"Unexpected item id {itemid} cell A11");
				}

				itemid = ws1.Cell("A12").GetValue<string>();
				Console.WriteLine("assert itemd id cell A12:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"Unexpected item id {itemid} cell A12");
				}

				itemid = ws1.Cell("A13").GetValue<string>();
				Console.WriteLine("assert itemd id cell A13:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"Unexpected item id {itemid} cell A13");
				}

				itemid = ws1.Cell("A14").GetValue<string>();
				Console.WriteLine("assert itemd id cell A14:" + itemid);
				if (!ProdSupplierItemIdValidator.IsExpectedProdSupplierItemNumber(itemid))
				{
					throw new Exception($"Unexpected item id {itemid} cell A14");
				}

				itemid = ws1.Cell("A15").GetValue<string>();
				Console.WriteLine("assert itemd id cell A15:" + itemid);
				if (!itemid.Trim().Contains("01-081.9010") && !itemid.Trim().Contains("01-655.1000") && !itemid.Trim().Contains("02-570.1000") && !itemid.Trim().Contains("02-570.9020") && !itemid.Trim().Contains("10-020.5000") && !itemid.Trim().Contains("10-020.5001") && !itemid.Trim().Contains("11-015.5000") && !itemid.Trim().Contains("11-015.9025"))
				{
					throw new Exception("Unexpected itemd id cell A15");
				}

				itemid = ws1.Cell("A16").GetValue<string>();
				Console.WriteLine("assert itemd id cell A16 is empty:" + itemid);
				Assert.That(itemid, Is.EqualTo(""));

				itemid = ws1.Cell("A17").GetValue<string>();
				Console.WriteLine("assert itemd id cell A17 is empty:" + itemid);
				Assert.That(itemid, Is.EqualTo(""));

				itemid = ws1.Cell("A18").GetValue<string>();
				Console.WriteLine("assert itemd id cell A18 is empty:" + itemid);
				Assert.That(itemid, Is.EqualTo(""));

				var field = ws1.Cell("E1").GetValue<string>();
				Console.WriteLine("assert field column header E1:" + field);
				Assert.That(field.Trim() == "Field");

				field = ws1.Cell("E2").GetValue<string>();
				Console.WriteLine("assert field cell E2:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Picture") && !field.Trim().Contains("Type of Attachment 1"))
				{
					throw new Exception("Unexpected field value cell E2");
				}

				field = ws1.Cell("E3").GetValue<string>();
				Console.WriteLine("assert field cell E3:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Picture") && !field.Trim().Contains("Type of Attachment 1"))
				{
					throw new Exception("Unexpected field value cell E3");
				}

				field = ws1.Cell("E4").GetValue<string>();
				Console.WriteLine("assert field cell E2:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Picture") && !field.Trim().Contains("Type of Attachment 1"))
				{
					throw new Exception("Unexpected field value cell E2");
				}

				field = ws1.Cell("E2").GetValue<string>();
				Console.WriteLine("assert field cell E4:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Picture") && !field.Trim().Contains("Type of Attachment 1"))
				{
					throw new Exception("Unexpected field value cell E4");
				}

				field = ws1.Cell("E5").GetValue<string>();
				Console.WriteLine("assert field cell E5:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Picture") && !field.Trim().Contains("Type of Attachment 1"))
				{
					throw new Exception("Unexpected field value cell E5");
				}

				field = ws1.Cell("E6").GetValue<string>();
				Console.WriteLine("assert field cell E6:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Picture") && !field.Trim().Contains("Type of Attachment 1"))
				{
					throw new Exception("Unexpected field value cell E6");
				}

				field = ws1.Cell("E7").GetValue<string>();
				Console.WriteLine("assert field cell E7:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Picture") && !field.Trim().Contains("Type of Attachment 1"))
				{
					throw new Exception("Unexpected field value cell E7");
				}

				field = ws1.Cell("E8").GetValue<string>();
				Console.WriteLine("assert field cell E8:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Picture") && !field.Trim().Contains("Type of Attachment 1"))
				{
					throw new Exception("Unexpected field value cell E8");
				}

				field = ws1.Cell("E9").GetValue<string>();
				Console.WriteLine("assert field cell E9:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Picture") && !field.Trim().Contains("Type of Attachment 1"))
				{
					throw new Exception("Unexpected field value cell E9");
				}

				field = ws1.Cell("E10").GetValue<string>();
				Console.WriteLine("assert field cell E10:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Picture") && !field.Trim().Contains("Type of Attachment 1"))
				{
					throw new Exception("Unexpected field value cell E10");
				}

				field = ws1.Cell("E11").GetValue<string>();
				Console.WriteLine("assert field cell E11:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Picture") && !field.Trim().Contains("Type of Attachment 1"))
				{
					throw new Exception("Unexpected field value cell E11");
				}

				field = ws1.Cell("E12").GetValue<string>();
				Console.WriteLine("assert field cell E12:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Picture") && !field.Trim().Contains("Type of Attachment 1"))
				{
					throw new Exception("Unexpected field value cell E12");
				}

				field = ws1.Cell("E13").GetValue<string>();
				Console.WriteLine("assert field cell E13:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Picture") && !field.Trim().Contains("Type of Attachment 1"))
				{
					throw new Exception("Unexpected field value cell E13");
				}

				field = ws1.Cell("E14").GetValue<string>();
				Console.WriteLine("assert field cell E14:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Picture") && !field.Trim().Contains("Type of Attachment 1"))
				{
					throw new Exception("Unexpected field value cell E14");
				}

				field = ws1.Cell("E15").GetValue<string>();
				Console.WriteLine("assert field cell E15:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem") && !field.Trim().Contains("Short Description") && !field.Trim().Contains("Picture") && !field.Trim().Contains("Type of Attachment 1"))
				{
					throw new Exception("Unexpected field value cell E15");
				}

				/*
				var price = ws1.Cell("F13").GetValue<string>();
				Console.WriteLine("assert field id cell F13:" + price);
				Assert.That(price.Trim() == "55.185");*/

			}

			if (Environment == "QA")
			{
				var itemid = ws1.Cell("A2").GetValue<string>();
				Console.WriteLine("assert itemd id cell A2:" + itemid);
				if (!QaSupplierItemIdValidator.IsExpectedQASupplierItemNumber(itemid))
				{
					throw new Exception($"Unexpected item id {itemid} cell A2");
				}

				itemid = ws1.Cell("A3").GetValue<string>();
				Console.WriteLine("assert itemd id cell A3:" + itemid);
				if (!QaSupplierItemIdValidator.IsExpectedQASupplierItemNumber(itemid))
				{
					throw new Exception($"Unexpected item id {itemid} cell A3");
				}

				itemid = ws1.Cell("A4").GetValue<string>();
				Console.WriteLine("assert itemd id cell A4:" + itemid);
				if (!QaSupplierItemIdValidator.IsExpectedQASupplierItemNumber(itemid))
				{
					throw new Exception($"Unexpected item id {itemid} cell A4");
				}

				itemid = ws1.Cell("A5").GetValue<string>();
				Console.WriteLine("assert itemd id cell A5:" + itemid);
				if (!QaSupplierItemIdValidator.IsExpectedQASupplierItemNumber(itemid))
				{
					throw new Exception($"Unexpected item id {itemid} cell A5");
				}

				itemid = ws1.Cell("A6").GetValue<string>();
				Console.WriteLine("assert itemd id cell A6:" + itemid);
				if (!QaSupplierItemIdValidator.IsExpectedQASupplierItemNumber(itemid))
				{
					throw new Exception($"Unexpected item id {itemid} cell A6");
				}

				var field = ws1.Cell("E1").GetValue<string>();
				Console.WriteLine("assert field cell E1:" + field);
				if (!field.Trim().Contains("Field"))
				{
					throw new Exception("Unexpectedfield value cell E1");
				}

				field = ws1.Cell("E2").GetValue<string>();
				Console.WriteLine("assert field cell E2:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem"))
				{
					throw new Exception("Unexpected field value cell E2");
				}

				field = ws1.Cell("E3").GetValue<string>();
				Console.WriteLine("assert field cell E3:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem"))
				{
					throw new Exception("Unexpected field value cell E3");
				}

				field = ws1.Cell("E4").GetValue<string>();
				Console.WriteLine("assert field cell E4:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem"))
				{
					throw new Exception("Unexpected field value cell E4");
				}

				field = ws1.Cell("E5").GetValue<string>();
				Console.WriteLine("assert field cell E5:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem"))
				{
					throw new Exception("Unexpected field value cell E5");
				}

				field = ws1.Cell("E6").GetValue<string>();
				Console.WriteLine("assert field cell E6:" + field);
				if (!field.Trim().Contains("Long Description") && !field.Trim().Contains("Price 1") && !field.Trim().Contains("genericitem"))
				{
					throw new Exception("Unexpected field value cell E6");
				}
			}
		}
		catch (Exception e)
		{
			Console.WriteLine("exception asserting contents of diffing report file: " + e.Message);
		}
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
	}

	[Test, Order(23)]
	[Category("CMBTests")]

	async public Task TC23_CMB_Execute_Enrichment()
	{
		Console.WriteLine("TC23_CMB_Execute_Enrichment");

		//test assumes that the buyer has cmb upload via cms, so that a new version is created when the buyer uploads the enrichment catalog, which must then
		//have a working version created
		//https://dev.azure.com/Proactis/eCat/_testPlans/define?planId=125397&suiteId=179308
		//prod cmb smoke tests 179379
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
		DateTime today = DateTime.Now;
		DateTime thisTestStarted = DateTime.UtcNow;
		Console.WriteLine("this test started: " + thisTestStarted.ToLongTimeString());
		string CurrentDate = $"{today.Month}/{today.Day}/{today.Year}";
		await Page.SetViewportSizeAsync(1600, 900);
		await Page.GotoAsync(url, pageGotoOptions);
		Page.WaitForLoadStateAsync(LoadState.NetworkIdle).GetAwaiter().GetResult();
		await Task.Delay(1000);
		Console.WriteLine("Waiting for " + url);
		Console.WriteLine("LOGIN AS " + BUYER_USER1_LOGIN);
		await SignInPortal(PORTAL_MAIN_URL, BUYER_USER1_LOGIN, BUYER_USER1_PASSWORD);
		await Page.WaitForSelectorAsync("//side-bar-item[@name='Home']", new PageWaitForSelectorOptions { Timeout = 60000 });
		await Expect(Page.Locator("#notificationsWidget")).ToContainTextAsync("Notifications");
		(await GetSideBarHeaderTextAsync()).Should().Contain("The Business Network");

		//click catalogs tab
		Console.WriteLine("go to dashboard");
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Buyer Dashboard");
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buyer Dashboard" })).ToBeVisibleAsync(locatorVisibleAssertion);

		//Search for test supplier and reject catalog
		await Page.Locator("//*[@id='uiSupplierId']").FillAsync(TC04_SUPPLIER_ID);
		await Page.WaitForTimeoutAsync(1000);
		await Page.Locator("//*[@id='uiSearchCatalogs']").ClickAsync();
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page.WaitForTimeoutAsync(5000);
		VerifyCatalogStatus(TC04_SUPPLIER_METACATID, "New Version available");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_btnShowMore\"]")).ToBeVisibleAsync(locatorVisibleAssertion);
		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page.WaitForTimeoutAsync(2000);
		await Page.Locator($"[id=\"{TC04_SUPPLIER_METACATID}_allTasks_tabSupplierCatalog\"]").GetByText("Reject Catalog").ClickAsync();
		//*[@id="237593_allTasks_tabSupplierCatalog"]/div[2]/div/div[2]/a[2]
		await Expect(Page.Locator("//*[@id='uiRejectComment']")).ToHaveAttributeAsync("class", "modal fade in");
		await Page.Locator("//*[@id='uiRejectCommentText']").FillAsync("Reject for Enrichment Test");
		await Page.Locator("//*[@id='uiUpdateRejectCatalog']").ClickAsync();//Fire catalog rejection
		await Page.WaitForLoadStateAsync(LoadState.Load);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Page.WaitForTimeoutAsync(2000);
		await Page.Locator("//*[@id=\"uiCatalogRejectedMessage\"]/div/center/a").ClickAsync(); //This close popup but noticed the popup is not properly closed during automation
		await Page.WaitForTimeoutAsync(5000);
		var isPopupClosed = await Page.Locator("//*[@id='uiRejectComment']").IsHiddenAsync(); //Manually close the remaining popup after 5 second
		if (!isPopupClosed) {
			await Page.Locator("//*[@id=\"uiRejectComment\"]/div/div/div[1]/button").ClickAsync();
			await Page.WaitForTimeoutAsync(2000);
		}
		try
		{
			await Page.WaitForLoadStateAsync(LoadState.Load);
			await Page.WaitForTimeoutAsync(500);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Page.WaitForTimeoutAsync(2000);
		}
		catch (TimeoutException)
		{
			await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Page.WaitForTimeoutAsync(2000);
		}
		try
		{
			await Expect(Page.Locator("//*[@id=\"btnShowUploadModal\"]")).ToBeVisibleAsync();
		}
		catch (TimeoutException)
		{
			await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Page.WaitForTimeoutAsync(2000);
		}
		
		//Open upload widget
		
		await Page.Locator("#btnShowUploadModal").ClickAsync(locatorClickOptions);
		await Expect(Page.Locator("#uiUploadModul")).ToBeVisibleAsync(locatorVisibleAssertion);

		//upload 1key_enrich_template_new.xlsx
		Console.WriteLine("select " + UPLOAD_ENRICHMENT_FILE1 + " file to upload");
		await Task.Delay(2000);
		await Page.Locator($"[id=\"uiFileSelect\"]").SetInputFilesAsync(new[] { TC01_CATALOG_FILE_PATH + EXECUTE_ENRICHMENT_CATALOG_FILE });

		//has file been added to the download list?
		await Expect(Page.Locator("#uiUploadedFileList > table > tbody > tr > td:nth-child(1)")).ToContainTextAsync(EXECUTE_ENRICHMENT_CATALOG_FILE);
		Console.WriteLine("set filetype");
		await Page.Locator($"[id=\"{EXECUTE_ENRICHMENT_CATALOG_FILE}_selectType\"]").SelectOptionAsync(new[] { "content" });
		await Task.Delay(4000);
		Console.WriteLine("upload catalog file " + EXECUTE_ENRICHMENT_CATALOG_FILE);
		await Page.GetByRole(AriaRole.Link, new() { Name = "Process files" }).ClickAsync(locatorClickOptions);
		DateTime jobStarted = DateTime.Now;
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		//confirm file is being processed  #uiUploadedFileList > div
		await Task.Delay(3000);
		await Expect(Page.Locator("#uiUploadedFileList")).ToContainTextAsync("Your upload files were placed in the process queue", new LocatorAssertionsToContainTextOptions { Timeout = 60000 });
		Console.WriteLine("job created " + jobStarted.ToLongDateString());
		await Task.Delay(1000);
		//goto monitor
		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor Simple Catalog Import");//href = "/srvs/BuyerCatalogs/monitor/MonitorBuyer"
		Console.WriteLine("**********************************************");
		await GoWithErrWrap(CMB_MONITOR_URL, 60);
		await SetManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Simple Catalog import", jobStarted, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		////////////////////////////////////////////////////////////////////
		Console.WriteLine("******************************************************");
		Console.WriteLine("Catalog upload complete");
		Console.WriteLine("******************************************************");
		await Task.Delay(5000);
		//does this happen on both qa and prod?
		ManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Release catalog", jobStarted, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Catalog Released");
		Console.WriteLine("**********************************************");
		////////////////////////////////////////////////////////////////////
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
		await Expect(Page.Locator("#pageTitle")).ToContainTextAsync("Buyer Dashboard");
		await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buyer Dashboard" })).ToBeVisibleAsync(locatorVisibleAssertion);
		await CMBFilter("", TC04_SUPPLIER_ID);
		Console.WriteLine("filter catalogs using supplier id " + TC04_SUPPLIER_ID);
		//note on prod filtering for supplier id testsupcdo2 results in 2 catalogs! so need to be aware of locator strictness
		//could sort by status of New version available also?
		//in which case we should expect contentAllTasks to contain only 1 <div class=row>
		var count = Page.Locator("#contentAllTasks > div[class=\"row\"]").CountAsync();

		Console.WriteLine("catalog rows on page 1 of the dashboard: " + count.Result.ToString());
		//could assert that this is 1 here?
		await Task.Delay(4000);

		//assert catalog status
		VerifyCatalogStatus(TC04_SUPPLIER_METACATID, "New Version available");

		Console.WriteLine("click show more");
		//await Expect(Page.Locator("[id=\"\\37 7418_allTasks_btnShowMore\"]")).ToBeVisibleAsync();
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_btnShowMore\"]")).ToBeVisibleAsync(locatorVisibleAssertion);
		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		Console.WriteLine("job created " + jobStarted.ToLongDateString());
		////////////////// CREATE WORKING VERSION /////////////////////////////////
		//click supplier CATALOG chevron
		await Page.Locator($"[href*=\"#{TC04_SUPPLIER_METACATID}_allTasks_tabSupplierCatalog\"]").ClickAsync(locatorClickOptions);
		Console.WriteLine("*****************************************");
		Console.WriteLine("create working version");
		Console.WriteLine("*****************************************");
		//create working version
		await Task.Delay(6000);
		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_tabSupplierCatalog\"]").GetByText("Create Working Version").ClickAsync(locatorClickOptions);
		jobStarted = DateTime.Now;
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		VerifyCatalogStatus(TC04_SUPPLIER_METACATID, "Waiting for process");

		await Page.WaitForTimeoutAsync(1000);
		//go to monitor
		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor LOAD CATALOG");//href = "/srvs/BuyerCatalogs/monitor/MonitorBuyer"
		Console.WriteLine("**********************************************");
		await GoWithErrWrap(CMB_MONITOR_URL, 60);
		await SetManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Load Catalog", jobStarted, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Catalog Loaded");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("go to cmb dashboard");
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
		Console.WriteLine(Page.Url);

		//filter catalogs 
		await CMBFilter("", TC04_SUPPLIER_ID);
		await Task.Delay(2000);
		Console.WriteLine("assert catalog status is Catalogs to approve");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_catalog\"]")).ToContainTextAsync("Catalog to approve");
		Console.WriteLine("click show more");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_btnShowMore\"]")).ToBeVisibleAsync(locatorVisibleAssertion);
		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		//await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Review Items" })).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("assert that the active chevron is approve items");//#\37 7418_allTasks_navWizard > li.active
		await Expect(Page.Locator($"#{TC04_CATALOG_SELECTOR_ID}_allTasks_navWizard > li.active")).ToContainTextAsync("Approve Items");
		Console.WriteLine("Click the Enrichment Link");
		//        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Approve Items (381.1)" })).ToBeVisibleAsync();
		//await Expect(Page.Locator("[id=\"\\37 7418_allTasks_tabApproveItems\"]").GetByText("Enrichment")).ToBeVisibleAsync();
		Console.WriteLine("assert Enrichment link visible");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_tabApproveItems\"]").GetByText("Enrichment")).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("click the Enrichment Link on the Approve items chevron");//#\37 7418_allTasks_tabApproveItems > div.catalog-actions.col-lg-7.col-md-7.col-sm-8 > div > ul:nth-child(5) > li > a

		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_tabApproveItems\"]").GetByText("Enrichment").ClickAsync(locatorClickOptions);
		//        await Page.Locator("[id=\"\\37 7418_allTasks_tabApproveItems\"]").GetByText("Enrichment").ClickAsync(locatorClickOptions);
		//Console.WriteLine("Click the Enrichment Link on the cog wheel menu");//#\37 7418_allTasks_catalog > div > div.settings.open > ul > li:nth-child(4) > a


		Console.WriteLine("assert that the enrichment modal is displayed");//#uiManualEnrichments > div.modal-dialog
		await Expect(Page.Locator("#uiManualEnrichments > div.modal-dialog")).ToBeVisibleAsync(locatorVisibleAssertion);

		//assert selected is selected //#uiManualEnrichmentSelectionType
		await Expect(Page.Locator("#uiManualEnrichments")).ToContainTextAsync("Enrichment");


		if (Environment == "PROD")
		{
			await Expect(Page.Locator("#uiManualEnrichmentsContent")).ToContainTextAsync("Prod 2key mapping manual");
			await Page.Locator("#uiManualEnrichmentSelect_162841").CheckAsync();
		}

		if (Environment == "QA")
		{
			await Expect(Page.Locator("#uiManualEnrichmentsContent")).ToContainTextAsync("Qa 2key mapping manual");
			await Page.Locator("#uiManualEnrichmentSelect_29940").CheckAsync();
		}
		jobStarted = DateTime.Now;
		await Task.Delay(2000);
		Console.WriteLine("execute enrichment");
		await Page.RunAndWaitForResponseAsync(async () =>
		{
			await Page.Locator("#btnExecuteManualEnrichments").ClickAsync(locatorClickOptions);
		}, response => response.Url.Contains("ExecuteEnrichment") && response.Status == 200);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(1000);

		//go to monitor
		Console.WriteLine("**********************************************");
		Console.WriteLine("Go to monitor ENRICHMENT");//href = "/srvs/BuyerCatalogs/monitor/MonitorBuyer"
		Console.WriteLine("**********************************************");
		await GoWithErrWrap(CMB_MONITOR_URL, 60);
		await SetManualRefresh();
		await MonitorProcessStatueAsync(Page, "", "Enrichment", jobStarted, TC04_SUPPLIERNAME, TC01_CUSTOMERNAME, "Finished OK");
		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("Enrichment complete");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////

		//go to dashboard 

		///////////////////////////////////////////////////////////////////////////////////////
		Console.WriteLine("**********************************************");
		Console.WriteLine("approve catalogs approve items review enrichment");
		Console.WriteLine("**********************************************");
		///////////////////////////////////////////////////////////////////////////////////////
		await GoWithErrWrap(CMB_CATALOG_HOME_URL, 60);
		//filter catalogs 
		await CMBFilter("", TC04_SUPPLIER_ID);
		await Task.Delay(4000);

		Console.WriteLine("assert catalog status is Catalogs to approve");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_catalog\"]")).ToContainTextAsync("Catalog to approve");
		Console.WriteLine("click show more");
		await Expect(Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_btnShowMore\"]")).ToBeVisibleAsync(locatorVisibleAssertion);
		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_btnShowMore\"]").ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		//await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Review Items" })).ToBeVisibleAsync(locatorVisibleAssertion);
		Console.WriteLine("assert that the active chevron is approve items");//#\37 7418_allTasks_navWizard > li.active
		await Expect(Page.Locator($"#{TC04_CATALOG_SELECTOR_ID}_allTasks_navWizard > li.active")).ToContainTextAsync("Approve Items");

		Console.WriteLine("Review items");
		Console.WriteLine("click the Review Items button");//#\32 20716_allTasks_tabApproveItems > div.catalog-actions.col-lg-7.col-md-7.col-sm-8 > div > div.pull-right > a.btn.btn-success

		// Locator("[id=\"\\32 37593_allTasks_tabApproveItems\"]").GetByRole(AriaRole.Link, new() { Name = "Review Items" })

		await Page.Locator($"[id=\"{TC04_CATALOG_SELECTOR}allTasks_tabApproveItems\"]").GetByRole(AriaRole.Link, new() { Name = "Review Items" }).ClickAsync(locatorClickOptions);
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		//wait for uiitems table
		//#uiItems
		Console.WriteLine("wait for uiitems: start " + DateTime.Now.ToLongTimeString());
		await Page.WaitForSelectorAsync("#uiItems", new PageWaitForSelectorOptions { Timeout = 60000 });
		Console.WriteLine("wait for uiitems: end " + DateTime.Now.ToLongTimeString());
		//await Task.Delay(8000);

		Console.WriteLine("Page: " + Page.Url);
		//assert url like
		//https://portal.hubwoo.com/srvs/BuyerCatalogs/items/item-list?show=ACCEPTED_77418_62376&cid=62376&sid=77418&mode=approval&ignore=no
		await Expect(Page.GetByLabel("Action", new() { Exact = true })).ToBeVisibleAsync(locatorVisibleAssertion);
		await Expect(Page.GetByLabel("Comment:")).ToBeVisibleAsync(locatorVisibleAssertion);

		Console.WriteLine("Select enrichment column set"); //		
		await Page.Locator("#uiColumnSet").SelectOptionAsync(new[] { "default" });  //select default columnset first			
																																								//wait for uiitems table
		Console.WriteLine("wait for uiitems: start " + DateTime.Now.ToLongTimeString());
		await Page.WaitForSelectorAsync("#uiItems", new PageWaitForSelectorOptions { Timeout = 60000 });
		Console.WriteLine("wait for uiitems: end " + DateTime.Now.ToLongTimeString());
		await Task.Delay(8000);

		if (Environment == "PROD")
		{
			await Page.Locator("#uiColumnSet").SelectOptionAsync(new[] { CMB_REVIEW_ITEMS_COLUMNS_SET_ENRICHMENT });  //enrichment 1544
		}
		if (Environment == "QA")
		{
			await Page.Locator("#uiColumnSet").SelectOptionAsync(new[] { "940" });  //enrichment 940
		}

		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		//Handle backdrop issue after changing column set
		ReloadPageIfBackrop();
		//wait for uiitems table
		Console.WriteLine("wait for uiitems: start " + DateTime.Now.ToLongTimeString());
		await Page.WaitForSelectorAsync("#uiItems", new PageWaitForSelectorOptions { Timeout = 60000 });
		Console.WriteLine("wait for uiitems: end " + DateTime.Now.ToLongTimeString());
		await Task.Delay(6000);

		Console.WriteLine("assert that selected columnset is enrichment");
		await Expect(Page.Locator("#uiColumnSet")).ToHaveValueAsync(CMB_REVIEW_ITEMS_COLUMNS_SET_ENRICHMENT);
		//assert that expected values are present on the review items page
		Console.WriteLine("assert that expected values are present on the review items page");
		if (Environment == "PROD")
		{
			//assert column 10 in header has text 'Customer specific 19' //#uiItems > thead > tr > th:nth-child(10) > a
			//assert that column 4 has text 'Item ID'  //#uiItems > thead > tr > th:nth-child(4) > a
			Console.WriteLine("assert column 10 in header has text 'Customer specific 19'");
			WaitForElementToBeHidden(Page, "#loadingScreen");
			try
			{
				await Expect(Page.Locator("#uiItems > thead > tr > th:nth-child(10) > a")).ToContainTextAsync("Customer specific 19");
			}
			catch
			{
				Console.WriteLine("Customer specific 19 is not in column 10, has the column set been edited?");
			}

			Console.WriteLine("assert that column 4 has text 'Item ID'");
			try
			{
				await Expect(Page.Locator("#uiItems > thead > tr > th:nth-child(4) > a")).ToContainTextAsync("Item ID");
			}
			catch
			{
				Console.WriteLine("Item ID is not in column 4, has the column set been edited?");
			}

			var totalItemsRows = await Page.Locator("#uiItems  > tbody > tr").CountAsync();
			int row = 1;
			int checkrow1 = 0;
			int checkrow2 = 0;
			int checkrow3 = 0;
			var itemID = "";
			var enrichmentValue = "";
			while (row <= totalItemsRows)
			{
				//#uiItems > tbody > tr:nth-child(1) > td:nth-child(4)
				itemID = await Page.Locator($"#uiItems > tbody > tr:nth-child({row}) > td:nth-child(4)").TextContentAsync(locatorTextContentOptions);
				enrichmentValue = await Page.Locator($"#uiItems > tbody > tr:nth-child({row}) > td:nth-child(10)").TextContentAsync(locatorTextContentOptions);

				if (itemID == "02-570.1000")
				{
					//find row with item id 02-570.1000
					//assert customer specific 19 column has value 	Prod test 2 key
					Console.WriteLine("found row with item id 02-570.1000, row: " + row.ToString());
					Console.WriteLine("enrichment value: " + enrichmentValue);
					Console.WriteLine("assert customer specific 19 column has value Prod test 2 key");
					checkrow1 = row;
					Assert.That(enrichmentValue.Trim() == "Prod test 2 key");
				}

				if (itemID == "11-015.5000")
				{
					//find row item id 11-015.5000
					//assert customer specific 19 column has value 	Prod test 1key
					Console.WriteLine("found row item id 11-015.5000, row: " + row.ToString());
					Console.WriteLine("enrichment value: " + enrichmentValue);
					checkrow2 = row;
					Console.WriteLine("assert customer specific 19 column has value Prod test 1key");
					Assert.That(enrichmentValue.Trim() == "prod test 1key");
				}

				if (itemID == "11-015.9025")
				{
					//find row with item id 11-015.9025	
					//assert customer specific 19 column has value Prod test 2key
					Console.WriteLine("found row with item id 11-015.9025, row: " + row.ToString());
					Console.WriteLine("enrichment value: " + enrichmentValue);
					checkrow3 = row;

					Console.WriteLine("assert customer specific 19 column has value Prod test 2key");
					Assert.That(enrichmentValue.Trim() == "Prod test 2key");
				}

				if (checkrow1 != 0 && checkrow2 != 0 && checkrow3 != 0)
				{
					break;
				}
				row++;
			}
		}

		if (Environment == "QA")
		{
			//sort by short description
			await Page.GetByRole(AriaRole.Link, new() { Name = "Short Description" }).ClickAsync(locatorClickOptions);

			//filter by item id "123"
			await Page.GetByLabel("Item ID:").ClickAsync(locatorClickOptions);
			await Page.GetByLabel("Item ID:").FillAsync("123");
			await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync(locatorClickOptions);
			//assert column 10 in header has text 'Customer specific 19' //#uiItems > thead > tr > th:nth-child(10) > a
			//assert that column 4 has text 'Item ID'  //#uiItems > thead > tr > th:nth-child(4) > a
			Console.WriteLine("assert column 10 in header has text 'Customer specific 19'");
			WaitForElementToBeHidden(Page, "#loadingScreen");
			try
			{
				await Expect(Page.Locator("#uiItems > thead > tr > th:nth-child(10) > a")).ToContainTextAsync("Customer specific 19");
			}
			catch
			{
				Console.WriteLine("Customer specific 19 is not in column 10, has the column set been edited?");
			}

			Console.WriteLine("assert that column 4 has text 'Item ID'");
			try
			{
				await Expect(Page.Locator("#uiItems > thead > tr > th:nth-child(4) > a")).ToContainTextAsync("Item ID");
			}
			catch
			{
				Console.WriteLine("Item ID is not in column 4, has the column set been edited?");
			}

			var totalItemsRows = await Page.Locator("#uiItems  > tbody > tr").CountAsync();
			int row = 1;
			int checkrow1 = 0;
			int checkrow2 = 0;
			int checkrow3 = 0;
			var itemID = "";
			var enrichmentValue = "";
			while (row <= totalItemsRows)
			{
				//#uiItems > tbody > tr:nth-child(1) > td:nth-child(4)
				itemID = await Page.Locator($"#uiItems > tbody > tr:nth-child({row}) > td:nth-child(4)").TextContentAsync(locatorTextContentOptions);
				enrichmentValue = await Page.Locator($"#uiItems > tbody > tr:nth-child({row}) > td:nth-child(10)").TextContentAsync(locatorTextContentOptions);

				if (itemID == "IN123")
				{
					//find row with item id 02-570.1000
					//assert customer specific 19 column has value 	Prod test 2 key
					Console.WriteLine("found row with item id IN123, row: " + row.ToString());
					Console.WriteLine("enrichment value: " + enrichmentValue);
					Console.WriteLine("assert customer specific 19 column has value qa test 1key");
					checkrow1 = row;
					Assert.That(enrichmentValue.Trim() == "qa test 1key");
				}

				if (itemID == "LIKE123")
				{
					//find row item id 11-015.5000
					//assert customer specific 19 column has value 	Prod test 1key
					Console.WriteLine("found row item id LIKE123, row: " + row.ToString());
					Console.WriteLine("enrichment value: " + enrichmentValue);
					checkrow2 = row;
					Console.WriteLine("assert customer specific 19 column has value Qa test 2key");
					Assert.That(enrichmentValue.Trim() == "Qa test 2key");
				}

				if (itemID == "ORDER123")
				{
					//find row with item id 11-015.9025	
					//assert customer specific 19 column has value Prod test 2key
					Console.WriteLine("found row with item id ORDER123, row: " + row.ToString());
					Console.WriteLine("enrichment value: " + enrichmentValue);
					checkrow3 = row;

					Console.WriteLine("assert customer specific 19 column has value Qa test 2 key");
					Assert.That(enrichmentValue.Trim() == "Qa test 2 key");
				}

				if (checkrow1 != 0 && checkrow2 != 0 && checkrow3 != 0)
				{
					break;
				}
				row++;
			}
		}
		Console.WriteLine("test complete " + DateTime.Now.ToLongTimeString());
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
}