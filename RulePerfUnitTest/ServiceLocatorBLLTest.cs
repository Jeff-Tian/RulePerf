using Microsoft.Scs.Test.RiskTools.RulePerf.BLL;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.Scs.Test.RiskTools.RulePerf.Model;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Scs.Test.RiskTools.RulePerfUnitTest
{
    
    
    /// <summary>
    ///This is a test class for ServiceLocatorBLLTest and is intended
    ///to contain all ServiceLocatorBLLTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ServiceLocatorBLLTest
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
        ///A test for ParseFromPlainList
        ///</summary>
        [TestMethod()]
        [DeploymentItem("ServiceLists.txt")]
        public void ParseFromPlainListTest()
        {
            string list = File.ReadAllText(@"ServiceLists.txt");
            int expectedCount = 73;
            List<ServiceLocatorModel> actual;
            actual = ServiceLocatorBLL.ParseFromPlainList(list);
            Assert.AreEqual(expectedCount, actual.Count);
        }
    }
}
