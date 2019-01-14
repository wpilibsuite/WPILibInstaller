using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace WPILibInstaller.Test
{
    public class VersionTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] {"2", "1", true};
            yield return new object[] {"1", "2", false};
            yield return new object[] {"5.10.0", "5.9.0", true};
            yield return new object[] {"5.9.0", "5.9.0-1", true};
            yield return new object[] {"5.9.0", "5.9", true};
            yield return new object[] {"5.9.1", "5.9", true};
            yield return new object[] {"5.9", "5.9.0", false};
            yield return new object[] {"5.9", "5.9.1", false};
            yield return new object[] {"5.10.0", "5.9.0-1", true};
            yield return new object[] {"5.8.0", "5.9.0-1", false};
            yield return new object[] {"5.8.0-1", "5.8.0-2", false};
            yield return new object[] {"2019.1.1", "2019.1.1-beta-1", true};
            yield return new object[] {"2019.1.1-beta-2", "2019.1.1-beta-1", true};
            yield return new object[] {"2019.1.1-beta-2b", "2019.1.1-beta-2", true};
            yield return new object[] {"2019.1.1-beta-2b", "2019.1.1-beta-2a", true};
            yield return new object[] {"2019.1.1-beta-3", "2019.1.1-beta-2a", true};
            yield return new object[] {"2019.1.1-beta-3a", "2019.1.1-beta-2a", true};
            yield return new object[] {"2019.1.1-beta-3a", "2019.1.1-beta-3", true};
            yield return new object[] {"2019.1.1-beta-3", "2019.1.1-beta-3-pre5", true};
            yield return new object[] {"2019.1.1-beta-3-pre7", "2019.1.1-beta-3-pre6", true};
            yield return new object[] {"2019.1.1-beta-3a", "2019.1.1-beta-3-pre1", true};
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class EqualTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { "0", "0" };
            yield return new object[] { "0.0", "0.0" };
            yield return new object[] { "0.0.0", "0.0.0" };
            yield return new object[] { "0.0.0-0", "0.0.0-0" };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }


    public class VersionTests
    {
        [Theory]
        [ClassData(typeof(VersionTestData))]
        public void TestGreaterThen(string aStr, string bStr, bool result)
        {
            Version a = new Version(aStr);
            Version b = new Version(bStr);
            Assert.Equal(a > b, result);
        }

        [Theory]
        [ClassData(typeof(VersionTestData))]
        public void TestLessThen(string aStr, string bStr, bool result)
        {
            Version a = new Version(aStr);
            Version b = new Version(bStr);
            Assert.Equal(a < b, !result);
        }

        [Theory]
        [ClassData(typeof(VersionTestData))]
        public void TestGreaterThenInvert(string aStr, string bStr, bool result)
        {
            Version a = new Version(aStr);
            Version b = new Version(bStr);
            Assert.Equal(b < a, result);
        }

        [Theory]
        [ClassData(typeof(VersionTestData))]
        public void TestLessThenInvert(string aStr, string bStr, bool result)
        {
            Version a = new Version(aStr);
            Version b = new Version(bStr);
            Assert.Equal(b > a, !result);
        }

        [Theory]
        [ClassData(typeof(EqualTestData))]
        public void TestEqualGreaterThen(string aStr, string bStr)
        {
            Version a = new Version(aStr);
            Version b = new Version(bStr);
            Assert.False(a > b);
        }

        [Theory]
        [ClassData(typeof(EqualTestData))]
        public void TestEqualLessThen(string aStr, string bStr)
        {
            Version a = new Version(aStr);
            Version b = new Version(bStr);
            Assert.True(a < b);
        }

        [Theory]
        [ClassData(typeof(EqualTestData))]
        public void TestEqualGreaterThenInvert(string aStr, string bStr)
        {
            Version a = new Version(aStr);
            Version b = new Version(bStr);
            Assert.False(b > a);
        }

        [Theory]
        [ClassData(typeof(EqualTestData))]
        public void TestEqualLessThenInvert(string aStr, string bStr)
        {
            Version a = new Version(aStr);
            Version b = new Version(bStr);
            Assert.True(b < a);
        }
    }
}
