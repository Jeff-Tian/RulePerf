using Microsoft.Scs.Test.RiskTools.RulePerf.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace RulePerfUnitTest
{
    
    
    /// <summary>
    ///This is a test class for RemoteExeStepTest and is intended
    ///to contain all RemoteExeStepTest Unit Tests
    ///</summary>
    [TestClass()]
    public class RemoteExeStepTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for ParseCommandArguments
        ///</summary>
        [TestMethod()]
        [DeploymentItem("RulePerf.exe")]
        public void ParseCommandArgumentsTest()
        {
            RemoteExeStep_Accessor target = new RemoteExeStep_Accessor(); // TODO: Initialize to an appropriate value
            //string command = @"RemoteExeStep /RemoteMachine:EXRKRKUTLBEP01 /RemoteCommand:C:\RulePerf\RulePerf.exe SyncProductSettingsStep /ServerAssignmentFilePath:""\\bedtransfer\transfer\v-jetian\EXRK.xml"" /SQLConnectionTimeout:""300"" /SQLCommandTimeout:""300"" /TransferFolder:""\\transfer\transfer\v-jetian"" /BedtransferFolder:""\\bedtransfer\transfer\v-jetian"" /Environment:""Bed:EXRK"" /ResultLogPath:""\\bedtransfer\transfer\v-jetian\SyncProductSettingsStepInner.rslt"" /RemoteTimeout:""00:10:00"" /Domain:""FAREAST"" /DomainUserName:""v-jetian"" /DomainPassword:****** /EmailToList:""v-jetian@microsoft.com"" /EmailCCList:""v-jetian@microsoft.com"" /EmailUserName:"""" /EmailPassword:"""" /DeployReferencedList:""RiskEmailSender.dll"" /SyncSettingsCommand:""cmd /C C:\RulePerf\RiskConfigAutoPropTool.exe -ImportMachine:EXRKRKUTLBEP01 -ImportPath:C:\RulePerf\RiMEConfig.xml"" /DownloadRiMEConfigReferencedList:""\\bedtransfer\transfer\ruleperf\RiskConfigAutoPropTool.exe;\\bedtransfer\transfer\RulePerf\RiskConfigImportExport.dll;\\bedtransfer\transfer\v-jetian\RiMEConfig.xml"" /CommandsUserName:""Administrator"" /CommandsPassword:""#Bugsfor$"" /CommandsDomain:""EXRK"" /RemoteUserName:Administrator /RemotePassword:#Bugsfor$ /RemoteDomain:EXRK /ResultLogPath:\\bedtransfer\transfer\v-jetian\SyncProductSettingsStep.rslt /RemoteTimeout:00:10:00";

            string command = "ruleperf.exe testStep /a:b /c:d";
            command += " /e:\"cmd /C\"";

            Console.WriteLine("Command: ");
            Console.WriteLine(command);

            char switchChar = '/'; // TODO: Initialize to an appropriate value
            string[] expected = new string[] { "ruleperf.exe testStep", "a:b", "c:d" , "e:\"cmd /C\""};
            string[] actual;
            actual = target.ParseCommandArguments(command, switchChar);

            Console.WriteLine("\r\n\r\nAcutal:\r\n");
            for (int i = 0; i < actual.Length; i++)
            {
                Console.WriteLine(actual[i]);
            }

            Assert.AreEqual(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }
    }
}
