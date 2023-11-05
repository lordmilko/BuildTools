using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests
{
    public static class AssertEx
    {
        public static void AreEquivalent(object expected, object actual)
        {
            if (expected != null && actual != null)
            {
                var expectedType = expected.GetType();
                var actualType = actual.GetType();

                if (expectedType.IsArray && actualType.IsArray)
                {
                    var expectedArr = (Array)expected;
                    var actualArr = (Array)actual;

                    Assert.AreEqual(expectedArr.Length, actualArr.Length, "Array length was not correct");

                    for (var i = 0; i < expectedArr.Length; i++)
                    {
                        var leftItem = expectedArr.GetValue(i);
                        var rightItem = actualArr.GetValue(i);

                        if (leftItem == rightItem)
                            continue;

                        Assert.AreEqual(leftItem.ToString(), rightItem.ToString());
                    }

                    return;
                }

                if (expected is Hashtable ht1 && actual is Hashtable ht2)
                {
                    Assert.AreEqual(ht1.Keys.Count, ht2.Count, "Hashtable keys length was not correct");

                    var keys1 = ht1.Keys.Cast<object>().ToArray();
                    var keys2 = ht2.Keys.Cast<object>().ToArray();

                    for (var i = 0; i < keys1.Length; i++)
                        Assert.AreEqual(keys1[i], keys2[i], $"Key {i} was not correct");

                    foreach (var key in keys1)
                    {
                        var value1 = ht1[key];
                        var value2 = ht2[key];

                        AreEquivalent(value1, value2);
                    }

                    return;
                }

                if (expected is ScriptBlock sb1 && actual is ScriptBlock sb2)
                {
                    Assert.AreEqual(sb1.Ast.ToString(), sb2.Ast.ToString());

                    return;
                }
            }

            Assert.AreEqual(expected, actual);
        }

        public static void Throws<T>(Action action, string message, bool checkMessage = true) where T : Exception
        {
            try
            {
                action();

                Assert.Fail($"Expected an assertion of type {typeof(T)} to be thrown, however no exception occurred");
            }
            catch (T ex)
            {
                if (checkMessage)
                    Assert.IsTrue(ex.Message.Contains(message), $"Exception message '{ex.Message}' did not contain string '{message}'");
            }
            catch (Exception ex) when (!(ex is AssertFailedException))
            {
                throw;
            }
        }
    }
}
