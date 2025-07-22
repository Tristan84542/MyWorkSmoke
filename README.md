# Introduction 
This repos contains the automations test to be run on the day of a releases, for Catalog Manager and Quick Quote only. For Search, please refer to search-test

# Getting Started
TODO: Guide users through getting your code up and running on their own system. In this section you can talk about:
1.	Installation process
2.	Software dependencies
3.	Latest releases
4.	API references

# Build and Test
Download and Build solution. No extra dependencies should be required

To run CM:
CM smoke automations make use of runsettings to parse environment parameters
A few runsettings has been configured:
- CM_PROD_Chrome.runsettings
- CM_PROD_Chrome_DEBUG.runsettings
- CM_QA_Chrome.runsettings

For normal run, use CM_PROD_Chrome.runsettings
Then select CMTestInstanceA, B & C
Note: 
1. Instance A was meant for FTP upload catalog & **parameters initialization** even FTP upload case is not automated
2. Run order is determined by the [Test, Order(x)] in code and not by the TC number
3. If need to run on other browser, please make a copy of runsettings and rename to target browser then change the value within. **Do not change the existing runsettings**

For retest run, use CM_PROD_Chrome_DEBUG.runsettings


# Contribute
TODO: Explain how other users and developers can contribute to make your code better. 

If you want to learn more about creating good readme files then refer the following [guidelines](https://docs.microsoft.com/en-us/azure/devops/repos/git/create-a-readme?view=azure-devops). You can also seek inspiration from the below readme files:
- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)
- [Chakra Core](https://github.com/Microsoft/ChakraCore)