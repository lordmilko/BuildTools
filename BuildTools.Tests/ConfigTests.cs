using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Management.Automation;
using BuildTools.Cmdlets;
using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests
{
    [TestClass]
    public class ConfigTests : BaseTest
    {
        #region Normal

        [TestMethod]
        public void ConfigTests_Feature_NotSpecified_Equals_All() => Test(null, Assert.IsNull);

        [TestMethod]
        public void ConfigTests_Feature_EmptyString_Equals_All() => Test("''", Assert.IsNull);

        [TestMethod]
        public void ConfigTests_Feature_EmptyArray_Equals_All() => Test("@()", v => Assert.AreEqual(0, v.Length));

        [TestMethod]
        public void ConfigTests_Feature_EmptyStringInArray_Equals_All() => Test("@('')", v => Assert.AreEqual(0, v.Length));

        [TestMethod]
        public void ConfigTests_Feature_SingleValue() => Test("'Build'", v => Assert.AreEqual(Feature.Build, v.Single()));

        [TestMethod]
        public void ConfigTests_Feature_SingleValue_InArray() => Test("@('Build')", v => Assert.AreEqual(Feature.Build, v.Single()));

        [TestMethod]
        public void ConfigTests_Feature_MultipleValues() => Test("'Build', 'Test'", v =>
        {
            Assert.AreEqual(2, v.Length);
            Assert.AreEqual(Feature.Build, v[0]);
            Assert.AreEqual(Feature.Test, v[1]);
        });

        [TestMethod]
        public void ConfigTests_Feature_MultipleValues_InArray() => Test("@('Build', 'Test')", v =>
        {
            Assert.AreEqual(2, v.Length);
            Assert.AreEqual(Feature.Build, v[0]);
            Assert.AreEqual(Feature.Test, v[1]);
        });

        #endregion
        #region Negation

        [TestMethod]
        public void ConfigTests_Feature_SingleNegatedValue()
        {
            Test("'~Build'", v =>
            {
                var expected = new[]
                {
                    Feature.System,
                    Feature.Dependency,
                    Feature.Test,
                    Feature.Coverage,
                    Feature.Package,
                    Feature.Version
                };

                Assert.AreEqual(expected.Length, v.Length);

                for (var i = 0; i < expected.Length; i++)
                    Assert.AreEqual(expected[i], v[i], $"Item at index {i} was not correct");
            });
        }

        [TestMethod]
        public void ConfigTests_Feature_SingleNegatedValue_InArray()
        {
            Test("@('~Build')", v =>
            {
                var expected = new[]
                {
                    Feature.System,
                    Feature.Dependency,
                    Feature.Test,
                    Feature.Coverage,
                    Feature.Package,
                    Feature.Version
                };

                Assert.AreEqual(expected.Length, v.Length);

                for (var i = 0; i < expected.Length; i++)
                    Assert.AreEqual(expected[i], v[i], $"Item at index {i} was not correct");
            });
        }

        [TestMethod]
        public void ConfigTests_Feature_MultipleNegatedValues()
        {
            Test("'~Build', '~Test'", v =>
            {
                var expected = new[]
                {
                    Feature.System,
                    Feature.Dependency,
                    Feature.Coverage,
                    Feature.Package,
                    Feature.Version
                };

                Assert.AreEqual(expected.Length, v.Length);

                for (var i = 0; i < expected.Length; i++)
                    Assert.AreEqual(expected[i], v[i], $"Item at index {i} was not correct");
            });
        }

        [TestMethod]
        public void ConfigTests_Feature_MultipleNegatedValues_InArray()
        {
            Test("@('~Build', '~Test')", v =>
            {
                var expected = new[]
                {
                    Feature.System,
                    Feature.Dependency,
                    Feature.Coverage,
                    Feature.Package,
                    Feature.Version
                };

                Assert.AreEqual(expected.Length, v.Length);

                for (var i = 0; i < expected.Length; i++)
                    Assert.AreEqual(expected[i], v[i], $"Item at index {i} was not correct");
            });
        }

        [TestMethod]
        public void ConfigTests_Feature_AllCancelOut_Equals_NoFeatures() => Test("@('Build', '~Build')", v => Assert.AreEqual(0, v.Length));

        [TestMethod]
        public void ConfigTests_Feature_DoubleNegation_Throws()
        {
            AssertEx.Throws<InvalidOperationException>(
                () => Test("'~~Build'", null),
                "Cannot process Feature value '~~Build': '~' was specified multiple times"
            );
        }

        [TestMethod]
        public void ConfigTests_Feature_Negation_InvalidValue_Throws()
        {
            AssertEx.Throws<PSInvalidCastException>(
                () => Test("'~foo'", null),
                "Specify one of the following enumerator names and try again:"
            );
        }

        [TestMethod]
        public void ConfigTests_Feature_Negation_IgnoresCase()
        {
            Test("'~build'", v =>
            {
                var expected = new[]
                {
                    Feature.System,
                    Feature.Dependency,
                    Feature.Test,
                    Feature.Coverage,
                    Feature.Package,
                    Feature.Version
                };

                Assert.AreEqual(expected.Length, v.Length);

                for (var i = 0; i < expected.Length; i++)
                    Assert.AreEqual(expected[i], v[i], $"Item at index {i} was not correct");
            });
        }

        [TestMethod]
        public void ConfigTests_Feature_TwoValues_OneCancelsOut_Equals_RemainingFeature() =>
            Test("@('Build', 'Test', '~Build')", v => Assert.AreEqual(Feature.Test, v.Single()));

        #endregion
        #region Feature + CommandKind

        //The logic we want to employ is that Feature immediately narrows the pool of commands, however CommandKind trumps Feature as long as it is specified.
        //If CommandKind is not specified, the explicit rules set by Feature win

        [TestMethod]
        public void ConfigTests_CommandAndFeature_Feature_WithoutCommand_LimitsToFeature()
        {
            Test("'Build'", null, v =>
            {
                Assert.IsTrue(v.HasCommand(typeof(InvokeBuild<>)));
                Assert.IsTrue(v.HasCommand(typeof(ClearBuild<>)));

                Assert.IsFalse(v.HasCommand(typeof(InvokeTest<>)));
            });
        }

        [TestMethod]
        public void ConfigTests_CommandAndFeature_Feature_WithSubsetCommand_LimitsToSubset()
        {
            Test("'Build'", "'InvokeBuild'", v =>
            {
                Assert.IsTrue(v.HasCommand(typeof(InvokeBuild<>)));
                Assert.IsTrue(v.HasCommand(typeof(ClearBuild<>)));

                Assert.IsFalse(v.HasCommand(typeof(InvokeTest<>)));
            });
        }

        [TestMethod]
        public void ConfigTests_CommandAndFeature_Feature_WithSupersetCommand_IncludesSuperset()
        {
            Test("'Build'", "'InvokeBuild', 'InvokeTest'", v =>
            {
                Assert.IsTrue(v.HasCommand(typeof(InvokeBuild<>)));
                Assert.IsTrue(v.HasCommand(typeof(ClearBuild<>)));

                Assert.IsTrue(v.HasCommand(typeof(InvokeTest<>)));
            });
        }

        [TestMethod]
        public void ConfigTests_CommandAndFeature_System_ImplicitFeatures_AllowsSystem() =>
            Test(null, "'InvokeBuild'", v => Assert.IsTrue(v.HasCommand(typeof(GetCommand<>))));

        [TestMethod]
        public void ConfigTests_CommandAndFeature_System_ExplicitNoFeatures_ExcludesSystem() =>
            Test("@()", "'InvokeBuild'", v => Assert.IsFalse(v.HasCommand(typeof(GetCommand<>))));

        [TestMethod]
        public void ConfigTests_CommandAndFeature_System_ExplicitSomeFeatures_AllowsSystem() =>
            Test("'Build'", "'InvokeTest'", v => Assert.IsTrue(v.HasCommand(typeof(GetCommand<>))));

        #endregion

        private void Test(string features, Action<Feature[]> validate) =>
            Test(features, null, v => validate(v.Features));

        private void Test(string features, string commands, Action<ProjectConfig> validate)
        {
            Test((MockFileSystemProvider fileSystem, MockPowerShellService powerShell, IProjectConfigProviderFactory factory) =>
            {
                var psd1 = "C:\\Root\\build\\Config.psd1";

                var psd1Contents = $@"
@{{
    Name = 'Foo'
    CmdletPrefix = 'Foo'
    SolutionName = 'Foo.sln'
    Copyright = 'foo, 2023'
    {(features == null ? string.Empty : $"Features = {features}")}
    {(commands == null ? string.Empty : $"Commands = {commands}")}
    PackageTypes = 'Redist'

    CoverageThreshold = 1
    PackageFiles = @{{
        Redist= 'Foo'
}}
}}";
                var invoker = new PowerShellInvoker();

                var hashtable = invoker.InvokeScript<Hashtable>(psd1Contents).Single();

                fileSystem.EnumerateFilesMap[("C:\\Root\\build", "*.sln", SearchOption.TopDirectoryOnly)] = new[]
                {
                    "C:\\Root\\build\\Root.sln"
                };

                fileSystem.DirectoryExistsMap["C:\\Root\\build\\src"] = true;

                fileSystem.FileExistsMap[psd1] = true;
                fileSystem.ReadFileTextMap[psd1] = psd1Contents;

                powerShell.InvokeScriptMap[psd1Contents] = hashtable;

                var provider = factory.CreateProvider("C:\\Root\\build");

                var result = provider.Config;

                validate(result);
            });
        }

        protected override void CreateServices(out ServiceCollection serviceCollection)
        {
            serviceCollection = new ServiceCollection
            {
                { typeof(IFileSystemProvider), typeof(MockFileSystemProvider) },
                { typeof(IProjectConfigProviderFactory), typeof(MockProjectConfigProviderFactory) },
                { typeof(IPowerShellService), typeof(MockPowerShellService) }
            };
        }
    }
}
