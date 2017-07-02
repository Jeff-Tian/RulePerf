using Microsoft.Scs.Test.RiskTools.RulePerf.BLL;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Scs.Test.RiskTools.RulePerfUnitTest
{
    
    
    /// <summary>
    ///This is a test class for DataFileInfoTest and is intended
    ///to contain all DataFileInfoTest Unit Tests
    ///</summary>
    [TestClass()]
    public class DataFileInfoTest
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
        ///A test for DataFileInfo Constructor
        ///</summary>
        [TestMethod()]
        public void DataFileInfoConstructorTest()
        {
            string dataFileName = "database.owner.table.txt"; // TODO: Initialize to an appropriate value
            DataFileInfo target = new DataFileInfo(dataFileName);
            Assert.AreEqual("database", target.Database);
            Assert.AreEqual("owner", target.Owner);
            Assert.AreEqual("table", target.Table);
            Assert.AreEqual(true, target.IsValid());
        }

        /// <summary>
        ///A test for IsValid
        ///</summary>
        [TestMethod()]
        public void IsValidTest()
        {
            string dataFileName = "database.owner.table"; // TODO: Initialize to an appropriate value
            DataFileInfo target = new DataFileInfo(dataFileName); // TODO: Initialize to an appropriate value
            bool expected = false; // TODO: Initialize to an appropriate value
            bool actual;
            actual = target.IsValid();
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Test Path's static methods
        /// </summary>
        [TestMethod()]
        public void PathTest()
        {
            string path = @"c:\test\suc\db.txt";
            string expected = @"c:\test\suc\Success\db.txt";
            string actual = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), "Success", System.IO.Path.GetFileName(path));
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for MarkAsDone
        ///</summary>
        [TestMethod()]
        public void MarkAsDoneTest()
        {
            string dataFileName = @"D:\CTPEnlist\cp-work\Private\tests\RiskManagement\Tools\RulePerf\RulePerf\bin\Debug\ExternalData1_R.dbo.global_message.txt";
            DataFileInfo target = new DataFileInfo(dataFileName); // TODO: Initialize to an appropriate value
            string expected = @"D:\CTPEnlist\cp-work\Private\tests\RiskManagement\Tools\RulePerf\RulePerf\bin\Debug\Success\ExternalData1_R.dbo.global_message.txt";
            string actual;
            actual = target.MarkAsDone();
            Assert.AreEqual(expected, actual);
        }
    }
}
