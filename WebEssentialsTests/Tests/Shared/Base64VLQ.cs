using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Base64 = MadsKristensen.EditorExtensions;

namespace WebEssentialsTests.Tests.Shared
{
    [TestClass]
    public class Base64Vlq
    {
        [TestMethod]
        public void VlqGeneratedLine()
        {
            var testString = @"AASA;EACI,aAAa,2BAAb;EACA,SAAS,kEAAT";
            var expected = new[] { 1, 2, 2, 2, 3, 3, 3 };
            var collection = Base64.Base64Vlq.Decode(testString).ToArray();

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
            var collection = Base64.Base64Vlq.Decode(testString).ToArray();

            for (var i = 0; i < collection.Count(); ++i)
            {
                Assert.AreEqual(collection[i].GeneratedColumn, expected[i]);
            }
        }

        [TestMethod]
        public void VlqSourceLine()
        {
            var testString = @"AASA;EACI,aAAa,2BAAb;EACA,SAAS,kEAAT";
            var expected = new[] { 10, 11, 11, 11, 12, 12, 12 };
            var collection = Base64.Base64Vlq.Decode(testString).ToArray();

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
            var collection = Base64.Base64Vlq.Decode(testString).ToArray();

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
                                new[] { 01, 02, 02, 02, 03, 03, 03 },
                                new[] { 00, 04, 17, 04, 04, 13, 04 },
                                new[] { 10, 11, 11, 11, 12, 12, 12 }
                           };
            var collection = Base64.Base64Vlq.Decode(testString).ToArray();

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
                dynamic result = Base64.Base64Vlq.Base64VLQDecode(Base64.Base64Vlq.Encode(i));

                Assert.AreEqual(result.value, i);
                Assert.AreEqual(result.rest, "");
            }
        }
    }
}
