using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vlq = MadsKristensen.EditorExtensions.Base64Vlq;

namespace WebEssentialsTests.Tests.Shared
{
    [TestClass]
    public class Base64Vlq
    {
        [TestMethod]
        public void VlqGeneratedLine()
        {
            var testString = @"AASA;EACI,aAAa,2BAAb;EACA,SAAS,kEAAT";
            var expected = new[] { 0, 1, 1, 1, 2, 2, 2 };
            var collection = Vlq.Decode(testString, "").ToArray();

            for (var i = 0; i < collection.Count(); ++i)
            {
                Assert.AreEqual(collection[i].GeneratedLine, expected[i]);
            }
        }

        [TestMethod]
        public void VlqGeneratedColumn()
        {
            var testString = @"AAIA,CAAC;EACG,WAAA";
            var expected = new[] { 0, 1, 2, 13 };
            var collection = Vlq.Decode(testString, "").ToArray();

            for (var i = 0; i < collection.Count(); ++i)
            {
                Assert.AreEqual(collection[i].GeneratedColumn, expected[i]);
            }
        }

        [TestMethod]
        public void VlqSourceLine()
        {
            var testString = @"AASA;EACI,aAAa,2BAAb;EACA,SAAS,kEAAT";
            var expected = new[] { 09, 10, 10, 10, 11, 11, 11 };
            var collection = Vlq.Decode(testString, "").ToArray();

            for (var i = 0; i < collection.Count(); ++i)
            {
                Assert.AreEqual(collection[i].OriginalLine, expected[i]);
            }
        }

        [TestMethod]
        public void VlqSourceColumn()
        {
            var testString = @"AAIA,CAAC;EACG,WAAA";
            var expected = new[] { 0, 1, 4, 4 };
            var collection = Vlq.Decode(testString, "").ToArray();

            for (var i = 0; i < collection.Count(); ++i)
            {
                Assert.AreEqual(collection[i].OriginalColumn, expected[i]);
            }
        }

        [TestMethod]
        public void VlqDecodedMap()
        {
            var testString = @"AASA;EACI,aAAa,2BAAb;EACA,SAAS,kEAAT";
            var expected = new[]
                           {
                                new[] { 00, 02, 15, 42, 02, 11, 77 },
                                new[] { 00, 01, 01, 01, 02, 02, 02 },
                                new[] { 00, 04, 17, 04, 04, 13, 04 },
                                new[] { 09, 10, 10, 10, 11, 11, 11 }
                           };
            var collection = Vlq.Decode(testString, "").ToArray();

            for (var i = 0; i < collection.Count(); ++i)
            {
                Assert.AreEqual(collection[i].GeneratedColumn, expected[0][i]);
                Assert.AreEqual(collection[i].GeneratedLine, expected[1][i]);
                Assert.AreEqual(collection[i].OriginalColumn, expected[2][i]);
                Assert.AreEqual(collection[i].OriginalLine, expected[3][i]);
            }
        }

        [TestMethod]
        public void VlqTransitivity()
        {
            for (var i = -255; i < 256; i++)
            {
                var reader = new StringReader(Vlq.Encode(i));
                var result = Vlq.VlqDecode(reader);

                Assert.AreEqual(i, result);
                Assert.AreEqual(-1, reader.Peek(), "Stream should be fully consumed");
            }
        }
    }
}
