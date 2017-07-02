using Microsoft.Scs.Test.RiskTools.RulePerf.DAL;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Scs.Test.RiskTools.RulePerfUnitTest
{
    
    
    /// <summary>
    ///This is a test class for ConnectionStringHelperTest and is intended
    ///to contain all ConnectionStringHelperTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ConnectionStringHelperTest
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
        ///A test for Server
        ///</summary>
        [TestMethod()]
        public void ServerTest()
        {
            ConnectionStringHelper target = new ConnectionStringHelper("Server=(local);Database=master;Trusted_Connection=True;Connection Timeout=15"); // TODO: Initialize to an appropriate value
            string expected = "localhost";
            string actual;
            target.Server = expected;
            actual = target.Server;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void TestTest()
        {
            string input = "2=3";
            int index = input.IndexOf('=');
            if (index >= 0)
            {
                string key = input.Substring(0, index);
                string value = input.Substring(index + 1);

                Assert.AreEqual("2", key);
                Assert.AreEqual("3", value);
            }
            else
            {
                string key = input;
                string value = "";

                Assert.AreEqual(input, key);
                Assert.AreEqual("", value);
            }
        }

        /// <summary>
        ///A test for ConnectionString
        ///</summary>
        [TestMethod()]
        public void ConnectionStringTest()
        {
            string connectionString = "Server=(local);Database=master;Trusted_Connection=True;Connection Timeout=15";
            ConnectionStringHelper target = new ConnectionStringHelper(connectionString); // TODO: Initialize to an appropriate value
            string actual;
            actual = target.ConnectionString;
            Assert.AreEqual(connectionString, actual);
        }
    }
}
