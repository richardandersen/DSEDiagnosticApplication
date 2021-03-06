﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using DSEDiagnosticLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSEDiagnosticLibrary.Tests
{
    [TestClass()]
    public class StringHelpersTests
    {
        [TestMethod()]
        public void DetermineProperObjectFormatTest()
        {
            object expected = System.Net.IPAddress.Parse("10.11.12.14");
            var actual = StringHelpers.DetermineProperObjectFormat("10.11.12.14", false, true, true, null, true);

            Assert.AreEqual(expected, actual);

            actual = StringHelpers.DetermineProperObjectFormat("/10.11.12.14", false, true, true, null, true);

            Assert.AreEqual(expected, actual);

            expected = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("10.11.12.14"), 1024);
            actual = StringHelpers.DetermineProperObjectFormat("10.11.12.14:1024", false, true, true, null, true);

            Assert.AreEqual(expected, actual);

            actual = StringHelpers.DetermineProperObjectFormat("/10.11.12.14:1024", false, true, true, null, true);

            Assert.AreEqual(expected, actual);

            expected = System.DateTime.Parse(@"12/25/1991 13:50:25.34");
            actual = StringHelpers.DetermineProperObjectFormat(@"12/25/1991 13:50:25.34", false, true, true, null, true);

            Assert.AreEqual(expected, actual);

            expected = System.DateTime.Parse(@"1991-12-25 13:50:25.34");
            actual = StringHelpers.DetermineProperObjectFormat(@"1991-12-25 13:50:25,34", false, true, true, null, true);

            Assert.AreEqual(expected, actual);

            expected = System.DateTime.Parse(@"2017-02-11 00:00:30.042");
            actual = StringHelpers.DetermineProperObjectFormat(@"2017-02-11 00:00:30,042", false, true, true, null, true);

            Assert.AreEqual(expected, actual);

            expected = 123.45m;
            actual = StringHelpers.DetermineProperObjectFormat(@"123.45", false, true, true, null, true);

            Assert.AreEqual(expected, actual);

            expected = -123.45m;
            actual = StringHelpers.DetermineProperObjectFormat(@"-123.45", false, true, true, null, true);

            Assert.AreEqual(expected, actual);

            expected = new DSEDiagnosticLibrary.UnitOfMeasure("23 ms");
            actual = StringHelpers.DetermineProperObjectFormat(@"23 ms", false, true, true, null, true);

            Assert.AreEqual(expected, actual);

            expected = new DSEDiagnosticLibrary.UnitOfMeasure(23, UnitOfMeasure.Types.MS | UnitOfMeasure.Types.Time);
            actual = StringHelpers.DetermineProperObjectFormat(@"23 ms", false, true, true, null, true);

            Assert.AreEqual(expected, actual);

            expected = "classname";
            actual = StringHelpers.DetermineProperObjectFormat("namespaceA.namespaceb.classname");

            Assert.AreEqual(expected, actual);

            expected = "classname";
            actual = StringHelpers.DetermineProperObjectFormat("classname");

            Assert.AreEqual(expected, actual);

            expected = "class";
            actual = StringHelpers.DetermineProperObjectFormat("namespaceb.class");

            Assert.AreEqual(expected, actual);

            expected = "clss";
            actual = StringHelpers.DetermineProperObjectFormat("clss");
            Assert.AreEqual(expected, actual);

            expected = "host.com";
            actual = StringHelpers.DetermineProperObjectFormat("host.com");
            Assert.AreEqual(expected, actual);

            expected = "host.us";
            actual = StringHelpers.DetermineProperObjectFormat("host.us");
            Assert.AreEqual(expected, actual);

            expected = "host.local";
            actual = StringHelpers.DetermineProperObjectFormat("host.local");
            Assert.AreEqual(expected, actual);

            expected = "host.LOCAL";
            actual = StringHelpers.DetermineProperObjectFormat("host.LOCAL");
            Assert.AreEqual(expected, actual);

            expected = (decimal) 0.1;
            actual = StringHelpers.DetermineProperObjectFormat("0.1");
            Assert.AreEqual(expected, actual);

            expected = "org.apache.cassandra.locator.SimpleSeedProvider{seeds=192.168.247.69}";
            actual = StringHelpers.DetermineProperObjectFormat((string) expected, false, false);
            Assert.AreEqual(expected, actual);

            expected = "org.apache.cassandra.locator.SimpleSeedProvider{seeds=192.168.247.69}";
            actual = StringHelpers.DetermineProperObjectFormat("{org.apache.cassandra.locator.SimpleSeedProvider{seeds=192.168.247.69}}");
            Assert.AreEqual(expected, actual);

            expected = "[1, 10.0.1.1, 2, 5, class]";
            actual = StringHelpers.DetermineProperObjectFormat("[1, 2, a.b.class, 10.0.1.1, 5]");
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void ParseSSTableFileIntoKSTableNamesTest()
        {
            // "/mnt/dse/data1/homeKS/homebase_tasktracking_ops_l3-737682f0599311e6ad0fa12fb1b6cb6e/homeKS-homebase_tasktracking_ops_l3-tmp-ka-15175-Data.db"
            // "/var/lib/cassandra/data/cfs/inode-76298b94ca5f375cab5bb674eddd3d51/cfs-inode.cfs_parent_path-tmp-ka-71-Data.db"
            // "/data/cassandra/data/usprodda/digitalasset_3_0_2-42613c599c943db2b2805c31c2d36acb/usprodda-digitalasset_3_0_2.digitalasset_3_0_2_id-tmp-ka-308-Data.db"
            // "/mnt/cassandra/data/data/keyspace1/digitalasset_3_0_2-980fca722ea611e78ff92901fcc7f505/.index2_tags/mb-1-big-Data.db"
            // "/var/lib/cassandra/data/Keyspace1/digitalasset_3_0_2/Keyspace1-digitalasset_3_0_2-jb-1-Data.db"
            // "/var/lib/cassandra/data/Keyspace1/digitalasset_3_0_2/Keyspace1-digitalasset_3_0_2.digitalasset_3_0_2_id-jb-1-Data.db"
            // "/d3/data/system/batches-919a4bc57a333573b03e13fc3f68b465/mc-62-big-Data.db"
            var result = DSEDiagnosticLibrary.StringHelpers.ParseSSTableFileIntoKSTableNames("/mnt/dse/data1/homeKS/homebase_tasktracking_ops_l3-737682f0599311e6ad0fa12fb1b6cb6e/homeKS-homebase_tasktracking_ops_l3-tmp-ka-15175-Data.db");

            Assert.AreEqual("homeKS", result.Item1);
            Assert.AreEqual("homebase_tasktracking_ops_l3", result.Item2);
            Assert.AreEqual("737682f0599311e6ad0fa12fb1b6cb6e", result.Item3);
            Assert.IsNull(result.Item4);

            result = DSEDiagnosticLibrary.StringHelpers.ParseSSTableFileIntoKSTableNames("/var/lib/cassandra/data/cfs/inode-76298b94ca5f375cab5bb674eddd3d51/cfs-inode.cfs_parent_path-tmp-ka-71-Data.db");

            Assert.AreEqual("cfs", result.Item1);
            Assert.AreEqual("cfs_parent_path", result.Item2);
            Assert.AreEqual("76298b94ca5f375cab5bb674eddd3d51", result.Item3);
            Assert.AreEqual("inode", result.Item4);

            result = DSEDiagnosticLibrary.StringHelpers.ParseSSTableFileIntoKSTableNames("/data/cassandra/data/usprodda/digitalasset_3_0_2-42613c599c943db2b2805c31c2d36acb/usprodda-digitalasset_3_0_2.digitalasset_3_0_2_id-tmp-ka-308-Data.db");

            Assert.AreEqual("usprodda", result.Item1);
            Assert.AreEqual("digitalasset_3_0_2_id", result.Item2);
            Assert.AreEqual("42613c599c943db2b2805c31c2d36acb", result.Item3);
            Assert.AreEqual("digitalasset_3_0_2", result.Item4);

            result = DSEDiagnosticLibrary.StringHelpers.ParseSSTableFileIntoKSTableNames("/mnt/cassandra/data/data/keyspace1/digitalasset_3_0_2-980fca722ea611e78ff92901fcc7f505/.index2_tags/mb-1-big-Data.db");

            Assert.AreEqual("keyspace1", result.Item1);
            Assert.AreEqual("index2_tags", result.Item2);
            Assert.AreEqual("980fca722ea611e78ff92901fcc7f505", result.Item3);
            Assert.AreEqual("digitalasset_3_0_2", result.Item4);

            result = DSEDiagnosticLibrary.StringHelpers.ParseSSTableFileIntoKSTableNames("/var/lib/cassandra/data/Keyspace1/digitalasset_3_0_2/Keyspace1-digitalasset_3_0_2-jb-1-Data.db");

            Assert.AreEqual("Keyspace1", result.Item1);
            Assert.AreEqual("digitalasset_3_0_2", result.Item2);
            Assert.AreEqual(string.Empty, result.Item3);
            Assert.AreEqual(null, result.Item4);

            result = DSEDiagnosticLibrary.StringHelpers.ParseSSTableFileIntoKSTableNames("/var/lib/cassandra/data/Keyspace1/digitalasset_3_0_2/Keyspace1-digitalasset_3_0_2.digitalasset_3_0_2_id-jb-1-Data.db");

            Assert.AreEqual("Keyspace1", result.Item1);
            Assert.AreEqual("digitalasset_3_0_2_id", result.Item2);
            Assert.AreEqual(string.Empty, result.Item3);
            Assert.AreEqual("digitalasset_3_0_2", result.Item4);

            result = DSEDiagnosticLibrary.StringHelpers.ParseSSTableFileIntoKSTableNames("/var/lib/cassandra/data/mc/digitalasset_3_0_2/mc-digitalasset_3_0_2.digitalasset_3_0_2_id-jb-1-Data.db");

            Assert.AreEqual("mc", result.Item1);
            Assert.AreEqual("digitalasset_3_0_2_id", result.Item2);
            Assert.AreEqual(string.Empty, result.Item3);
            Assert.AreEqual("digitalasset_3_0_2", result.Item4);

            result = DSEDiagnosticLibrary.StringHelpers.ParseSSTableFileIntoKSTableNames("/d3/data/system/batches-919a4bc57a333573b03e13fc3f68b465/mc-62-big-Data.db");

            Assert.AreEqual("system", result.Item1);
            Assert.AreEqual("batches", result.Item2);
            Assert.AreEqual("919a4bc57a333573b03e13fc3f68b465", result.Item3);
            Assert.AreEqual(null, result.Item4);

            result = DSEDiagnosticLibrary.StringHelpers.ParseSSTableFileIntoKSTableNames("/d3/data/system/batches-919a4bc57a333573b03e13fc3f68b465/mb-62-big-Data.db");

            Assert.AreEqual("system", result.Item1);
            Assert.AreEqual("batches", result.Item2);
            Assert.AreEqual("919a4bc57a333573b03e13fc3f68b465", result.Item3);
            Assert.AreEqual(null, result.Item4);

            result = DSEDiagnosticLibrary.StringHelpers.ParseSSTableFileIntoKSTableNames("/d3/data/aa/batches-919a4bc57a333573b03e13fc3f68b465/ac-62-big-Data.db");

            Assert.AreEqual("aa", result.Item1);
            Assert.AreEqual("batches", result.Item2);
            Assert.AreEqual("919a4bc57a333573b03e13fc3f68b465", result.Item3);
            Assert.AreEqual(null, result.Item4);

            result = DSEDiagnosticLibrary.StringHelpers.ParseSSTableFileIntoKSTableNames("/d3/data/system/batches-919a4bc57a333573b03e13fc3f68b465/.myIDX/mb-62-big-Data.db");

            Assert.AreEqual("system", result.Item1);
            Assert.AreEqual("myIDX", result.Item2);
            Assert.AreEqual("919a4bc57a333573b03e13fc3f68b465", result.Item3);
            Assert.AreEqual("batches", result.Item4);
        }
    }
}