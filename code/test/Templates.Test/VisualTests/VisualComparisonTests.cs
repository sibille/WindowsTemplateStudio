﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using WindowsTestHelpers;
using Microsoft.Templates.Core;
using OpenQA.Selenium.Appium.Windows;
using Xunit;
using Microsoft.Templates.Core.Helpers;

namespace Microsoft.Templates.Test
{
    [Collection("GenerationCollection")]
    public class VisualComparisonTests : BaseGenAndBuildTests
    {
        const string MenuBar = "MenuBar";

        public VisualComparisonTests(GenerationFixture fixture)
            : base(fixture)
        {
        }

        public static string[] AllTestablePages()
        {
            var result = new List<string>();

            result.AddRange(AllPagesThatSupportSimpleTesting());
            result.AddRange(AllPagesThatRequireExtraLogicForTesting());
            result.AddRange(AllPagesNotVisuallyTestable());

            return result.ToArray();
        }

        public static string[] AllVisuallyTestablePages()
        {
            var result = new List<string>();

            result.AddRange(AllPagesThatSupportSimpleTesting());
            result.AddRange(AllPagesThatRequireExtraLogicForTesting());

            return result.ToArray();
        }

        public static string[] AllPagesThatSupportSimpleTesting()
        {
            return new[]
            {
                "wts.Page.Blank",
                "wts.Page.Chart",
                "wts.Page.ContentGrid",
                "wts.Page.DataGrid",
                "wts.Page.Grid",
                "wts.Page.ImageGallery",
                "wts.Page.InkDraw",
                "wts.Page.InkDrawPicture",
                "wts.Page.InkSmartCanvas",
                "wts.Page.MasterDetail",
                "wts.Page.Settings",
                "wts.Page.TabbedPivot",
                "wts.Page.TabView",
                "wts.Page.TreeView",
                "wts.Page.TwoPaneView",
            };
        }

        public static string[] AllPagesThatRequireExtraLogicForTesting()
        {
            return new[]
            {
                "wts.Page.Camera",
                "wts.Page.WebView",
                "wts.Page.MediaPlayer",
            };
        }

        public static string[] AllPagesNotVisuallyTestable()
        {
            return new[]
            {
                "wts.Page.Map", // Map page cannot be relied on to load the same details on the screen (buildings, road names, etc.) and so cannot use screenshots to compare displayed output
            };
        }

        // Gets all the pages that are available (and testable) in both VB & C#
        public static IEnumerable<object[]> GetAllSinglePageAppsVbAndCsSimple()
        {
            var pagesThatSupportUiTesting = AllPagesThatSupportSimpleTesting();

            foreach (var page in pagesThatSupportUiTesting)
            {
                yield return new object[] { page };
            }
        }

        // Gets all the pages that are available (and testable) in both VB & C#
        public static IEnumerable<object[]> GetAllSinglePageAppsVbAndCsExtraLogic()
        {
            var pagesThatSupportUiTesting = AllPagesThatRequireExtraLogicForTesting();

            foreach (var page in pagesThatSupportUiTesting)
            {
                yield return new object[] { page };
            }
        }

        // Get all the pages in C# templates that are to be compared with the MVVMBasic version
        public static IEnumerable<object[]> GetAllSinglePageAppsCSharp()
        {
            //// To quickly test a single scenario
            ////yield return new object[] { "TabbedNav", "wts.Page.ImageGallery", new[] { "CodeBehind", "MVVMLight", "CaliburnMicro", "Prism" } };
            foreach (var projectType in GetAllProjectTypes())
            {

                var pagesThatSupportUiTesting = AllPagesThatSupportSimpleTesting();

                foreach (var page in pagesThatSupportUiTesting)
                {
                    var otherFrameworks = GetAdditionalCsFrameworks(projectType, page).Select(f => f[0].ToString()).ToArray();

                    yield return new object[] { projectType, page, otherFrameworks };
                }
            }
        }

        public static IEnumerable<object[]> GetAdditionalCsFrameworks(string projectType, string page = "")
        {
            //TODO: Remove this once Caliburn Micro Templates are done for MenuBar
            if (projectType == "MenuBar" || page == "wts.Page.TabView" || page == "wts.Page.TreeView" || page == "wts.Page.TwoPaneView")
            {
                foreach (var framework in new[] { "CodeBehind", "MVVMLight", "Prism" })
                {
                    yield return new object[] { framework };
                }
            }
            else
            {
                foreach (var framework in new[] { "CodeBehind", "MVVMLight", "CaliburnMicro", "Prism" })
                {
                    yield return new object[] { framework };
                }
            }

        }

        public static IEnumerable<object[]> GetAllFrameworksForBothVbAndCs()
        {
            foreach (var framework in new[] { "CodeBehind", "MVVMBasic", "MVVMLight" })
            {
                yield return new object[] { framework };
            }
        }

        public static IEnumerable<object[]> GetAllFrameworksAndLanguageCombinations()
        {
            yield return new object[] { "CodeBehind", ProgrammingLanguages.CSharp };
            yield return new object[] { "MVVMBasic", ProgrammingLanguages.CSharp };
            yield return new object[] { "MVVMLight", ProgrammingLanguages.CSharp };
            yield return new object[] { "CaliburnMicro", ProgrammingLanguages.CSharp };
            yield return new object[] { "Prism", ProgrammingLanguages.CSharp };
            yield return new object[] { "CodeBehind", ProgrammingLanguages.VisualBasic };
            yield return new object[] { "MVVMBasic", ProgrammingLanguages.VisualBasic };
            yield return new object[] { "MVVMLight", ProgrammingLanguages.VisualBasic };
        }

        public static IEnumerable<string> GetAllProjectTypes()
        {
            foreach (var projectType in new[] { "Blank", "SplitView", "TabbedNav", "MenuBar" })
            {
                yield return projectType;
            }
        }

        // Returned rectangles are measured in pixels from the top left of the image/window
        private string GetExclusionAreasForVisualEquivalencyTest(string projectType, string pageName)
        {
            switch (pageName)
            {
                case "wts.Page.Settings":
                    // Exclude the area at the end of the app name and also covering the version number
                    switch (projectType)
                    {
                        case "SplitView":
                            return "new[] { new ImageComparer.ExclusionArea(new Rectangle(480, 300, 450, 40), 1.25f) }";
                        case "TabbedNav":
                            return "new[] { new ImageComparer.ExclusionArea(new Rectangle(60, 350, 450, 40), 1.25f) }";
                        case "Blank":
                        default:
                            return "new[] { new ImageComparer.ExclusionArea(new Rectangle(60, 350, 450, 40), 1.25f) }";
                    }

                default:
                    return string.Empty;
            }
        }

        // Note. There are multiple theories defined here but could be combined in a single one.
        // However, it would be a lot tests being generated and some of the longer running and
        // more complicated options are susceptible to false negatives.
        // Splitting them up like this makes it easier to rerun and debug failed tests.
        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsVbAndCsSimple))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLaunchPageVisualAreTheSameInVbAndCs_Blank_MvvmBasic_Simple_Async(string page)
        {
            await EnsureLanguageLaunchPageVisualsAreEquivalentAsync("Blank", "MVVMBasic", page);
        }

        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsVbAndCsExtraLogic))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLaunchPageVisualAreTheSameInVbAndCs_Blank_MvvmBasic_ExtraLogic_Async(string page)
        {
            await EnsureLanguageLaunchPageVisualsAreEquivalentAsync("Blank", "MVVMBasic", page);
        }

        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsVbAndCsSimple))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLaunchPageVisualAreTheSameInVbAndCs_Blank_MvvmLight_Simple_Async(string page)
        {
            await EnsureLanguageLaunchPageVisualsAreEquivalentAsync("Blank", "MVVMLight", page);
        }

        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsVbAndCsExtraLogic))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLaunchPageVisualAreTheSameInVbAndCs_Blank_MvvmLight_ExtraLogic_Async(string page)
        {
            await EnsureLanguageLaunchPageVisualsAreEquivalentAsync("Blank", "MVVMLight", page);
        }

        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsVbAndCsSimple))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLaunchPageVisualAreTheSameInVbAndCs_Blank_CodeBehind_Simple_Async(string page)
        {
            await EnsureLanguageLaunchPageVisualsAreEquivalentAsync("Blank", "CodeBehind", page);
        }

        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsVbAndCsExtraLogic))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLaunchPageVisualAreTheSameInVbAndCs_Blank_CodeBehind_ExtraLogic_Async(string page)
        {
            await EnsureLanguageLaunchPageVisualsAreEquivalentAsync("Blank", "CodeBehind", page);
        }

        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsVbAndCsSimple))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLaunchPageVisualAreTheSameInVbAndCs_SplitView_MvvmBasic_Simple_Async(string page)
        {
            await EnsureLanguageLaunchPageVisualsAreEquivalentAsync("SplitView", "MVVMBasic", page);
        }

        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsVbAndCsExtraLogic))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLaunchPageVisualAreTheSameInVbAndCs_SplitView_MvvmBasic_ExtraLogic_Async(string page)
        {
            await EnsureLanguageLaunchPageVisualsAreEquivalentAsync("SplitView", "MVVMBasic", page);
        }

        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsVbAndCsSimple))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLaunchPageVisualAreTheSameInVbAndCs_SplitView_MvvmLight_Simple_Async(string page)
        {
            await EnsureLanguageLaunchPageVisualsAreEquivalentAsync("SplitView", "MVVMLight", page);
        }

        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsVbAndCsExtraLogic))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLaunchPageVisualAreTheSameInVbAndCs_SplitView_MvvmLight_ExtraLogic_Async(string page)
        {
            await EnsureLanguageLaunchPageVisualsAreEquivalentAsync("SplitView", "MVVMLight", page);
        }

        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsVbAndCsSimple))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLaunchPageVisualAreTheSameInVbAndCs_SplitView_CodeBehind_Simple_Async(string page)
        {
            await EnsureLanguageLaunchPageVisualsAreEquivalentAsync("SplitView", "CodeBehind", page);
        }

        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsVbAndCsExtraLogic))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLaunchPageVisualAreTheSameInVbAndCs_SplitView_CodeBehind_ExtraLogic_Async(string page)
        {
            await EnsureLanguageLaunchPageVisualsAreEquivalentAsync("SplitView", "CodeBehind", page);
        }

        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsVbAndCsSimple))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLaunchPageVisualAreTheSameInVbAndCs_TabbedNav_MvvmBasic_Simple_Async(string page)
        {
            await EnsureLanguageLaunchPageVisualsAreEquivalentAsync("TabbedNav", "MVVMBasic", page);
        }

        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsVbAndCsExtraLogic))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLaunchPageVisualAreTheSameInVbAndCs_TabbedNav_MvvmBasic_ExtraLogic_Async(string page)
        {
            await EnsureLanguageLaunchPageVisualsAreEquivalentAsync("TabbedNav", "MVVMBasic", page);
        }

        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsVbAndCsSimple))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLaunchPageVisualAreTheSameInVbAndCs_TabbedNav_MvvmLight_Simple_Async(string page)
        {
            await EnsureLanguageLaunchPageVisualsAreEquivalentAsync("TabbedNav", "MVVMLight", page);
        }

        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsVbAndCsExtraLogic))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLaunchPageVisualAreTheSameInVbAndCs_TabbedNav_MvvmLight_ExtraLogic_Async(string page)
        {
            await EnsureLanguageLaunchPageVisualsAreEquivalentAsync("TabbedNav", "MVVMLight", page);
        }

        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsVbAndCsSimple))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLaunchPageVisualAreTheSameInVbAndCs_TabbedNav_CodeBehind_Simple_Async(string page)
        {
            await EnsureLanguageLaunchPageVisualsAreEquivalentAsync("TabbedNav", "CodeBehind", page);
        }

        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsVbAndCsExtraLogic))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLaunchPageVisualAreTheSameInVbAndCs_TabbedNav_CodeBehind_ExtraLogic_Async(string page)
        {
            await EnsureLanguageLaunchPageVisualsAreEquivalentAsync("TabbedNav", "CodeBehind", page);
        }

        // There are tests with hardcoded projectType and framework values to make rerunning/debugging only some of the tests easier
        private async Task EnsureLanguageLaunchPageVisualsAreEquivalentAsync(string projectType, string framework, string page)
        {
            var genIdentities = new[] { page };

            ExecutionEnvironment.CheckRunningAsAdmin();
            WinAppDriverHelper.CheckIsInstalled();

            var app1Details = await SetUpProjectForUiTestComparisonAsync(ProgrammingLanguages.CSharp, projectType, framework, genIdentities, lastPageIsHome: true, createForScreenshots: true);
            var app2Details = await SetUpProjectForUiTestComparisonAsync(ProgrammingLanguages.VisualBasic, projectType, framework, genIdentities, lastPageIsHome: true, createForScreenshots: true);

            var noClickCount = 0;

            if (page == "wts.Page.Map")
            {
                noClickCount = 1;
            }
            else if (page == "wts.Page.Camera")
            {
                noClickCount = 2;
            }

            var longPause = page.EndsWith(".WebView") || page.EndsWith(".MediaPlayer");

            var testProjectDetails = SetUpTestProjectForInitialScreenshotComparison(app1Details, app2Details, GetExclusionAreasForVisualEquivalencyTest(projectType, page), noClickCount, longPause);

            var (testSuccess, testOutput) = RunWinAppDriverTests(testProjectDetails);

            // Note that failing tests will leave the projects behind, plus the apps and test certificates installed
            if (testSuccess)
            {
                UninstallAppx(app1Details.PackageFullName);
                UninstallAppx(app2Details.PackageFullName);

                RemoveCertificate(app1Details.CertificatePath);
                RemoveCertificate(app2Details.CertificatePath);

                // Parent of images folder also contains the test project
                Fs.SafeDeleteDirectory(Path.Combine(testProjectDetails.imagesFolder, ".."));
            }

            var outputMessages = string.Join(Environment.NewLine, testOutput);

            // A diff image is automatically created if the outputs differ
            if (Directory.Exists(testProjectDetails.imagesFolder)
             && Directory.GetFiles(testProjectDetails.imagesFolder, "*.*-Diff.png").Any())
            {
                Assert.True(
                    testSuccess,
                    $"Failing test images in {testProjectDetails.imagesFolder}{Environment.NewLine}{Environment.NewLine}{outputMessages}");
            }
            else
            {
                Assert.True(testSuccess, outputMessages);
            }
        }

        // Note. Visual Studio MUST be running as Admin to run this test.
        // Note that failing tests will leave the projects behind, plus the apps and test certificates installed
        [Theory]
        [MemberData(nameof(GetAllSinglePageAppsCSharp))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureFrameworkLaunchPageVisualsAreEquivalentAsync(string projectType, string page, string[] frameworks)
        {
            var genIdentities = new[] { page };

            var noClickCount = 0;

            if (page == "wts.Page.Map")
            {
                noClickCount = 1;
            }
            else if (page == "wts.Page.Camera")
            {
                noClickCount = 2;
            }

            ExecutionEnvironment.CheckRunningAsAdmin();
            WinAppDriverHelper.CheckIsInstalled();

            // MVVMBasic is considered the reference version. Compare generated apps with equivalent in other frameworks
            var refAppDetails = await SetUpProjectForUiTestComparisonAsync(ProgrammingLanguages.CSharp, projectType, "MVVMBasic", genIdentities, lastPageIsHome: true, createForScreenshots: true);

            var otherProjDetails = new VisualComparisonTestDetails[frameworks.Length];

            bool allTestsPassed = true;

            var outputMessages = string.Empty;

            for (int i = 0; i < frameworks.Length; i++)
            {
                string framework = frameworks[i];
                otherProjDetails[i] = await SetUpProjectForUiTestComparisonAsync(ProgrammingLanguages.CSharp, projectType, framework, genIdentities, lastPageIsHome: true, createForScreenshots: true);

                var testProjectDetails = SetUpTestProjectForInitialScreenshotComparison(refAppDetails, otherProjDetails[i], GetExclusionAreasForVisualEquivalencyTest(projectType, page), noClickCount);

                var (testSuccess, testOutput) = RunWinAppDriverTests(testProjectDetails);

                if (testSuccess)
                {
                    UninstallAppx(otherProjDetails[i].PackageFullName);

                    RemoveCertificate(otherProjDetails[i].CertificatePath);

                    // Parent of images folder also contains the test project
                    Fs.SafeDeleteDirectory(Path.Combine(testProjectDetails.imagesFolder, ".."));
                }
                else
                {
                    allTestsPassed = false;

                    if (Directory.Exists(testProjectDetails.imagesFolder)
                     && Directory.GetFiles(testProjectDetails.imagesFolder, "*.*-Diff.png").Any())
                    {
                        outputMessages += $"Failing test images in {testProjectDetails.imagesFolder}{Environment.NewLine}{Environment.NewLine}{outputMessages}";
                    }
                    else
                    {
                        outputMessages += $"{Environment.NewLine}{string.Join(Environment.NewLine, testOutput)}";
                    }
                }
            }

            if (allTestsPassed)
            {
                UninstallAppx(refAppDetails.PackageFullName);

                RemoveCertificate(refAppDetails.CertificatePath);
            }

            Assert.True(allTestsPassed, outputMessages.TrimStart());
        }

        // Note. Visual Studio MUST be running as Admin to run this test.
        // Note that failing tests will leave the projects behind, plus the apps and test certificates installed
        [Theory]
        [MemberData(nameof(GetAllFrameworksForBothVbAndCs))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLanguagesProduceIdenticalOutputForEachPageInNavViewAsync(string framework)
        {
            await EnsureLanguagesProduceIdenticalOutputForEachPageAsync(framework, "SplitView");
        }

        // Note. Visual Studio MUST be running as Admin to run this test.
        // Note that failing tests will leave the projects behind, plus the apps and test certificates installed
        [Theory]
        [MemberData(nameof(GetAllFrameworksForBothVbAndCs))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureLanguagesProduceIdenticalOutputForEachPageInMenuBarAsync(string framework)
        {
            await EnsureLanguagesProduceIdenticalOutputForEachPageAsync(framework, MenuBar);
        }

        private async Task EnsureLanguagesProduceIdenticalOutputForEachPageAsync(string framework, string projectType)
        {
            var genIdentities = AllPagesThatSupportSimpleTesting();

            ExecutionEnvironment.CheckRunningAsAdmin();
            WinAppDriverHelper.CheckIsInstalled();

            var app1Details = await SetUpProjectForUiTestComparisonAsync(ProgrammingLanguages.CSharp, projectType, framework, genIdentities);
            var app2Details = await SetUpProjectForUiTestComparisonAsync(ProgrammingLanguages.VisualBasic, projectType, framework, genIdentities);

            var pageExclusions = new Dictionary<string, string>();
            pageExclusions.Add("Settings", "new ImageComparer.ExclusionArea(new Rectangle(480, 360, 450, 40), 1.25f)");

            (string projectFolder, string imagesFolder) testProjectDetails;

            switch (projectType)
            {
                case MenuBar:
                    testProjectDetails = SetUpTestProjectForAllMenuBarPagesComparison(app1Details, app2Details, pageExclusions);
                    break;
                default:
                    testProjectDetails = SetUpTestProjectForAllNavViewPagesComparison(app1Details, app2Details, pageExclusions);
                    break;
            }

            var (testSuccess, testOutput) = RunWinAppDriverTests(testProjectDetails);

            // Note that failing tests will leave the projects behind, plus the apps and test certificates installed
            if (testSuccess)
            {
                UninstallAppx(app1Details.PackageFullName);
                UninstallAppx(app2Details.PackageFullName);

                RemoveCertificate(app1Details.CertificatePath);
                RemoveCertificate(app2Details.CertificatePath);

                // Parent of images folder also contains the test project
                Fs.SafeDeleteDirectory(Path.Combine(testProjectDetails.imagesFolder, ".."));
            }

            var outputMessages = string.Join(Environment.NewLine, testOutput);

            // A diff image is automatically created if the outputs differ
            if (Directory.Exists(testProjectDetails.imagesFolder)
                && Directory.GetFiles(testProjectDetails.imagesFolder, "DIFF-*.png").Any())
            {
                Assert.True(
                    testSuccess,
                    $"Failing test images in {testProjectDetails.imagesFolder}{Environment.NewLine}{Environment.NewLine}{outputMessages}");
            }
            else
            {
                Assert.True(testSuccess, outputMessages);
            }
        }

        // Note. Visual Studio MUST be running as Admin to run this test.
        // Note that failing tests will leave the projects behind, plus the apps and test certificates installed
        [Fact]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureCsFrameworksProduceIdenticalOutputForEachPageInNavViewAsync()
        {
            var genIdentities = AllPagesThatSupportSimpleTesting();

            ExecutionEnvironment.CheckRunningAsAdmin();
            WinAppDriverHelper.CheckIsInstalled();

            var app1Details = await SetUpProjectForUiTestComparisonAsync(ProgrammingLanguages.CSharp, "SplitView", "MVVMBasic", genIdentities);

            var errors = new List<string>();

            foreach (var framework in GetAdditionalCsFrameworks("SplitView"))
            {
                var app2Details = await SetUpProjectForUiTestComparisonAsync(ProgrammingLanguages.CSharp, "SplitView", framework[0].ToString(), genIdentities);

                var pageExclusions = new Dictionary<string, string>();
                pageExclusions.Add("Settings", "new ImageComparer.ExclusionArea(new Rectangle(480, 360, 450, 40), 1.25f)");

                var testProjectDetails = SetUpTestProjectForAllNavViewPagesComparison(app1Details, app2Details, pageExclusions);

                var (testSuccess, testOutput) = RunWinAppDriverTests(testProjectDetails);

                // Note that failing tests will leave the projects behind, plus the apps and test certificates installed
                if (testSuccess)
                {
                    UninstallAppx(app2Details.PackageFullName);

                    RemoveCertificate(app2Details.CertificatePath);

                    // Parent of images folder also contains the test project
                    Fs.SafeDeleteDirectory(Path.Combine(testProjectDetails.imagesFolder, ".."));
                }
                else
                {
                    errors.AddRange(testOutput);

                    // A diff image is automatically created if the outputs differ
                    if (Directory.GetFiles(testProjectDetails.imagesFolder, "DIFF-*.png").Any())
                    {
                        errors.Add($"Failing test images in {testProjectDetails.imagesFolder}");
                    }
                }
            }

            var outputMessages = string.Join(Environment.NewLine, errors);

            if (outputMessages.Any())
            {
                Assert.True(false, $"{Environment.NewLine}{Environment.NewLine}{outputMessages}");
            }
            else
            {
                UninstallAppx(app1Details.PackageFullName);
                RemoveCertificate(app1Details.CertificatePath);
            }
        }

        // Note. Visual Studio MUST be running as Admin to run this test.
        // Note that failing tests will leave the projects behind, plus the apps and test certificates installed
        [Fact]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureCsFrameworksProduceIdenticalOutputForEachPageInMenuBarAsync()
        {
            var genIdentities = AllPagesThatSupportSimpleTesting();

            ExecutionEnvironment.CheckRunningAsAdmin();
            WinAppDriverHelper.CheckIsInstalled();

            var app1Details = await SetUpProjectForUiTestComparisonAsync(ProgrammingLanguages.CSharp, MenuBar, "MVVMBasic", genIdentities);

            var errors = new List<string>();

            foreach (var framework in GetAdditionalCsFrameworks(MenuBar))
            {
                var app2Details = await SetUpProjectForUiTestComparisonAsync(ProgrammingLanguages.CSharp, MenuBar, framework[0].ToString(), genIdentities);

                var pageExclusions = new Dictionary<string, string>();
                pageExclusions.Add("Settings", "new ImageComparer.ExclusionArea(new Rectangle(480, 360, 450, 40), 1.25f)");

                var testProjectDetails = SetUpTestProjectForAllMenuBarPagesComparison(app1Details, app2Details, pageExclusions);

                var (testSuccess, testOutput) = RunWinAppDriverTests(testProjectDetails);

                // Note that failing tests will leave the projects behind, plus the apps and test certificates installed
                if (testSuccess)
                {
                    UninstallAppx(app2Details.PackageFullName);

                    RemoveCertificate(app2Details.CertificatePath);

                    // Parent of images folder also contains the test project
                    Fs.SafeDeleteDirectory(Path.Combine(testProjectDetails.imagesFolder, ".."));
                }
                else
                {
                    errors.AddRange(testOutput);

                    // A diff image is automatically created if the outputs differ
                    if (Directory.GetFiles(testProjectDetails.imagesFolder, "DIFF-*.png").Any())
                    {
                        errors.Add($"Failing test images in {testProjectDetails.imagesFolder}");
                    }
                }
            }

            var outputMessages = string.Join(Environment.NewLine, errors);

            if (outputMessages.Any())
            {
                Assert.True(false, $"{Environment.NewLine}{Environment.NewLine}{outputMessages}");
            }
            else
            {
                UninstallAppx(app1Details.PackageFullName);
                RemoveCertificate(app1Details.CertificatePath);
            }
        }

        [Theory]
        [MemberData(nameof(GetAllFrameworksAndLanguageCombinations))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureCanNavigateToEveryPageInNavViewWithoutErrorAsync(string framework, string language)
        {
            await EnsureCanNavigateToEveryPageWithoutErrorAsync(framework, language, "SplitView");
        }

        [Theory]
        [MemberData(nameof(GetAllFrameworksAndLanguageCombinations))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureCanNavigateToEveryPageInTabbedNavWithoutErrorAsync(string framework, string language)
        {
            await EnsureCanNavigateToEveryPageWithoutErrorAsync(framework, language, "TabbedNav");
        }

        [Theory]
        [MemberData(nameof(GetAllFrameworksAndLanguageCombinations))]
        [Trait("ExecutionSet", "ManualOnly")]
        [Trait("Type", "WinAppDriver")]
        public async Task EnsureCanNavigateToEveryPageWithMenuBarWithoutErrorAsync(string framework, string language)
        {
            if (framework == "CaliburnMicro")
            {
                // Caliburn does not yet support MenuBar projects
                // TODO: remove this when implement #3000
                return;
            }

            await EnsureCanNavigateToEveryPageWithoutErrorAsync(framework, language, "MenuBar");
        }

        private async Task EnsureCanNavigateToEveryPageWithoutErrorAsync(string framework, string language, string projectType)
        {
            var pageIdentities = AllTestablePages();

            ExecutionEnvironment.CheckRunningAsAdmin();
            WinAppDriverHelper.CheckIsInstalled();
            WinAppDriverHelper.StartIfNotRunning();

            VisualComparisonTestDetails appDetails = null;

            int pagesOpenedSuccessfully = 0;

            async Task ForOpenedPage(string pageName, WindowsDriver<WindowsElement> session)
            {
                if (pageName == "Map")
                {
                    // For location permission
                    if (await ClickYesOnPopUpAsync(session))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2)); // Allow page to load after accepting prompt
                        pagesOpenedSuccessfully++;
                    }
                    else
                    {
                        Assert.True(false, "Failed to click \"Yes\" on popup for Map permission.");
                    }
                }
                else if (pageName == "Camera")
                {
                    var cameraPermission = await ClickYesOnPopUpAsync(session); // For camera permission
                    var microphonePermission = await ClickYesOnPopUpAsync(session); // For microphone permission

                    if (cameraPermission && microphonePermission)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2)); // Allow page to load after accepting prompts
                        pagesOpenedSuccessfully++;
                    }
                    else
                    {
                        Assert.True(false, "Failed to click \"Yes\" on popups for Camera page permissions.");
                    }
                }
                else
                {
                    pagesOpenedSuccessfully++;
                }
            }


            try
            {
                appDetails = await SetUpProjectForUiTestComparisonAsync(language, projectType, framework, pageIdentities);

                using (var appSession = WinAppDriverHelper.LaunchAppx(appDetails.PackageFamilyName))
                {
                    appSession.Manage().Window.Maximize();

                    await Task.Delay(TimeSpan.FromSeconds(2));

                    if (projectType == "MenuBar")
                    {
                        var menuItems = appSession.FindElementsByClassName("Microsoft.UI.Xaml.Controls.MenuBarItem");

                        for (int i = menuItems.Count - 1; i >= 0; i--)
                        {
                            menuItems[i].Click(); // Open menu to count sub-items
                            var subItemCount = appSession.FindElementsByClassName("MenuFlyoutItem").Count;
                            menuItems[i].Click(); // Close menu so get in a consistent state

                            // Stepping through the MenuFlyoutItems is unreliable
                            for (int j = 0; j < subItemCount; j++)
                            {
                                menuItems[i].Click(); // Open the menu again

                                var subItems = appSession.FindElementsByClassName("MenuFlyoutItem");

                                if (subItems.Count > 0)
                                {
                                    var option = subItems[j].Text;

                                    // Don't close the app or the WinAppDriver will throw an error and the test will fail
                                    if (option != "Exit")
                                    {
                                        subItems[j].Click();
                                    }

                                    await Task.Delay(TimeSpan.FromMilliseconds(1500)); // Allow page to load and animations to complete

                                    if (option == "Settings")
                                    {
                                        // In a MenuBar, Settings is shown in a flyout - we must dismiss it to avoid confusing other logic that is looking at flyouts
                                        const byte Escape = 27; // From System.Windows.Forms.Keys

                                        VirtualKeyboard.KeyDown(Escape);
                                        VirtualKeyboard.KeyUp(Escape);
                                        await ForOpenedPage(subItems[j].Text, appSession);

                                    }
                                    else
                                    {
                                        await ForOpenedPage(subItems[j].Text, appSession);
                                    }
                                }

                            }
                        }
                    }
                    else
                    {
                        var menuItems = appSession.FindElementsByClassName("Microsoft.UI.Xaml.Controls.NavigationViewItem");

                        foreach (var menuItem in menuItems)
                        {
                            menuItem.Click();
                            Debug.WriteLine("Opened: " + menuItem.Text);

                            await Task.Delay(TimeSpan.FromMilliseconds(1500)); // Allow page to load and animations to complete

                            await ForOpenedPage(menuItem.Text, appSession);
                        }
                    }

                    // Don't leave the app maximized in case we want to open the app again.
                    // Some controls handle layout differently when the app is first opened maximized
                    VirtualKeyboard.RestoreMaximizedWindow();
                }
            }
            finally
            {
                if (appDetails != null)
                {
                    UninstallAppx(appDetails.PackageFullName);
                    RemoveCertificate(appDetails.CertificatePath);
                }

                WinAppDriverHelper.StopIfRunning();
            }

            var expectedPageCount = pageIdentities.Length + 1; // Add 1 for"Main page" added as well by default
            if (framework == "CaliburnMicro")
            {
                expectedPageCount = expectedPageCount - 3;
            }

            Assert.True(expectedPageCount == pagesOpenedSuccessfully, $"Not all pages were opened successfully. Expected {expectedPageCount} but got {pagesOpenedSuccessfully}.");
        }

        private async Task<bool> ClickYesOnPopUpAsync(WindowsDriver<WindowsElement> session)
        {
            await Task.Delay(TimeSpan.FromSeconds(1)); // Allow extra time for popup to be displayed

            var popups = session.FindElementsByAccessibilityId("Popup Window");

            if (popups.Count() == 1)
            {
                var yes = popups[0].FindElementsByName("Yes");

                if (yes.Count() == 1)
                {
                    yes[0].Click();
                    return true;
                }
            }

            return false;
        }

        private (bool Success, List<string> TextOutput) RunWinAppDriverTests((string projectFolder, string imagesFolder) testProjectDetails)
        {
            var result = false;

            WinAppDriverHelper.StartIfNotRunning();

            var buildOutput = Path.Combine(testProjectDetails.projectFolder, "bin", "Debug", "AutomatedUITests.dll");
            var runTestsScript = $"& \"C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Enterprise\\Common7\\IDE\\Extensions\\TestPlatform\\vstest.console.exe\" \"{buildOutput}\" ";

            // Test failures will be treated as an error but they are handled below
            var output = ExecutePowerShellScript(runTestsScript, outputOnError: true);

            var outputText = new List<string>();

            var testCount = -1;
            var passCount = -1;

            foreach (var outputLine in output)
            {
                var outputLineString = outputLine.ToString();

                outputText.Add(outputLineString);

                if (outputLineString.StartsWith("Total tests: ", StringComparison.OrdinalIgnoreCase))
                {
                    testCount = int.Parse(outputLineString.Substring(12).Trim());
                }
                else if (outputLineString.Trim().StartsWith("Passed: ", StringComparison.OrdinalIgnoreCase))
                {
                    passCount = int.Parse(outputLineString.Trim().Substring(7).Trim());

                    if (testCount > 0)
                    {
                        result = testCount == passCount;
                    }
                }
            }

            WinAppDriverHelper.StopIfRunning();

            return (result, outputText);
        }

        private (string projectFolder, string imagesFolder) SetUpTestProjectForInitialScreenshotComparison(VisualComparisonTestDetails app1Details, VisualComparisonTestDetails app2Details, string areasOfImageToExclude = null, int noClickCount = 0, bool longPause = false)
        {
            var rootFolder = $"{Path.GetPathRoot(Environment.CurrentDirectory)}UIT\\VIS\\{DateTime.Now:dd_HHmmss}\\";
            var projectFolder = Path.Combine(rootFolder, "TestProject");
            var imagesFolder = Path.Combine(rootFolder, "Images");

            Fs.EnsureFolder(rootFolder);
            Fs.EnsureFolder(projectFolder);
            Fs.EnsureFolder(imagesFolder);

            // Copy base project
            Fs.CopyRecursive(@"..\..\VisualTests\TestProjectSource", projectFolder, overwrite: true);

            // enable appropriate test
            var projFileName = Path.Combine(projectFolder, "AutomatedUITests.csproj");

            var projFileContents = File.ReadAllText(projFileName);

            var newProjFileContents = projFileContents.Replace(
                @"<!--<Compile Include=""Tests\LaunchBothAppsAndCompareInitialScreenshots.cs"" />-->",
                @"<Compile Include=""Tests\LaunchBothAppsAndCompareInitialScreenshots.cs"" />");

            File.WriteAllText(projFileName, newProjFileContents, Encoding.UTF8);

            // set AppInfo values
            var appInfoFileName = Path.Combine(projectFolder, "TestAppInfo.cs");

            var appInfoFileContents = File.ReadAllText(appInfoFileName);

            var newAppInfoFileContents = appInfoFileContents
                .Replace("***APP-PFN-1-GOES-HERE***", $"{app1Details.PackageFamilyName}!App")
                .Replace("***APP-PFN-2-GOES-HERE***", $"{app2Details.PackageFamilyName}!App")
                .Replace("***APP-NAME-1-GOES-HERE***", app1Details.ProjectName)
                .Replace("***APP-NAME-2-GOES-HERE***", app2Details.ProjectName)
                .Replace("***FOLDER-GOES-HERE***", imagesFolder)
                .Replace("public const int NoClickCount = 0;", $"public const int NoClickCount = {noClickCount};");

            if (longPause)
            {
                newAppInfoFileContents = newAppInfoFileContents.Replace("public const bool LongPauseAfterLaunch = false;", "public const bool LongPauseAfterLaunch = true;");
            }

            if (!string.IsNullOrWhiteSpace(areasOfImageToExclude))
            {
                newAppInfoFileContents = newAppInfoFileContents.Replace("new ImageComparer.ExclusionArea[0]", areasOfImageToExclude);
            }

            File.WriteAllText(appInfoFileName, newAppInfoFileContents, Encoding.UTF8);

            // build test project
            var restoreNugetScript = $"& \"{projectFolder}\\nuget.exe\" restore \"{projectFolder}\\AutomatedUITests.csproj\" -PackagesDirectory \"{projectFolder}\\Packages\"";
            ExecutePowerShellScript(restoreNugetScript);
            var buildSolutionScript = $"& \"C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Enterprise\\MSBuild\\Current\\Bin\\MSBuild.exe\" \"{projectFolder}\\AutomatedUITests.sln\" /t:Rebuild /p:RestorePackagesPath=\"{projectFolder}\\Packages\" /p:Configuration=Debug /p:Platform=\"Any CPU\"";
            ExecutePowerShellScript(buildSolutionScript);

            return (projectFolder, imagesFolder);
        }

        private (string projectFolder, string imagesFolder) SetUpTestProjectForAllNavViewPagesComparison(VisualComparisonTestDetails app1Details, VisualComparisonTestDetails app2Details, Dictionary<string, string> pageAreasToExclude = null)
        {
            var rootFolder = $"{Path.GetPathRoot(Environment.CurrentDirectory)}UIT\\VIS\\{DateTime.Now:dd_HHmmss}\\";
            var projectFolder = Path.Combine(rootFolder, "TestProject");
            var imagesFolder = Path.Combine(rootFolder, "Images");

            Fs.EnsureFolder(rootFolder);
            Fs.EnsureFolder(projectFolder);
            Fs.EnsureFolder(imagesFolder);

            // Copy base project
            Fs.CopyRecursive(@"..\..\VisualTests\TestProjectSource", projectFolder, overwrite: true);

            // enable appropriate test
            var projFileName = Path.Combine(projectFolder, "AutomatedUITests.csproj");

            var projFileContents = File.ReadAllText(projFileName);

            var newProjFileContents = projFileContents.Replace(
                @"<!--<Compile Include=""Tests\LaunchBothAppsAndCompareAllNavViewPages.cs"" />-->",
                @"<Compile Include=""Tests\LaunchBothAppsAndCompareAllNavViewPages.cs"" />");

            File.WriteAllText(projFileName, newProjFileContents, Encoding.UTF8);

            // set AppInfo values
            var appInfoFileName = Path.Combine(projectFolder, "TestAppInfo.cs");

            var appInfoFileContents = File.ReadAllText(appInfoFileName);

            var newAppInfoFileContents = appInfoFileContents
                .Replace("***APP-PFN-1-GOES-HERE***", $"{app1Details.PackageFamilyName}!App")
                .Replace("***APP-PFN-2-GOES-HERE***", $"{app2Details.PackageFamilyName}!App")
                .Replace("***APP-NAME-1-GOES-HERE***", app1Details.ProjectName)
                .Replace("***APP-NAME-2-GOES-HERE***", app2Details.ProjectName)
                .Replace("***FOLDER-GOES-HERE***", imagesFolder);

            if (pageAreasToExclude != null)
            {
                var replacement = string.Empty;

                foreach (var exclusion in pageAreasToExclude)
                {
                    replacement += $" {{ \"{exclusion.Key}\", {exclusion.Value} }},{Environment.NewLine}";
                }

                newAppInfoFileContents =
                    newAppInfoFileContents.Replace(
                        "PageSpecificExclusions = new Dictionary<string, ImageComparer.ExclusionArea>();",
                        $"PageSpecificExclusions = new Dictionary<string, ImageComparer.ExclusionArea>{{{replacement}}};");
            }

            File.WriteAllText(appInfoFileName, newAppInfoFileContents, Encoding.UTF8);

            // build test project
            var restoreNugetScript = $"& \"{projectFolder}\\nuget.exe\" restore \"{projectFolder}\\AutomatedUITests.csproj\" -PackagesDirectory \"{projectFolder}\\Packages\"";
            ExecutePowerShellScript(restoreNugetScript);
            var buildSolutionScript = $"& \"C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Enterprise\\MSBuild\\Current\\Bin\\MSBuild.exe\" \"{projectFolder}\\AutomatedUITests.sln\" /t:Rebuild /p:RestorePackagesPath=\"{projectFolder}\\Packages\" /p:Configuration=Debug /p:Platform=\"Any CPU\"";
            ExecutePowerShellScript(buildSolutionScript);

            return (projectFolder, imagesFolder);
        }

        private (string projectFolder, string imagesFolder) SetUpTestProjectForAllMenuBarPagesComparison(VisualComparisonTestDetails app1Details, VisualComparisonTestDetails app2Details, Dictionary<string, string> pageAreasToExclude = null)
        {
            var rootFolder = $"{Path.GetPathRoot(Environment.CurrentDirectory)}UIT\\VIS\\{DateTime.Now:dd_HHmmss}\\";
            var projectFolder = Path.Combine(rootFolder, "TestProject");
            var imagesFolder = Path.Combine(rootFolder, "Images");

            Fs.EnsureFolder(rootFolder);
            Fs.EnsureFolder(projectFolder);
            Fs.EnsureFolder(imagesFolder);

            // Copy base project
            Fs.CopyRecursive(@"..\..\VisualTests\TestProjectSource", projectFolder, overwrite: true);

            // enable appropriate test
            var projFileName = Path.Combine(projectFolder, "AutomatedUITests.csproj");

            var projFileContents = File.ReadAllText(projFileName);

            var newProjFileContents = projFileContents.Replace(
                @"<!--<Compile Include=""Tests\LaunchBothAppsAndCompareAllMenuBarPages.cs"" />-->",
                @"<Compile Include=""Tests\LaunchBothAppsAndCompareAllMenuBarPages.cs"" />");

            File.WriteAllText(projFileName, newProjFileContents, Encoding.UTF8);

            // set AppInfo values
            var appInfoFileName = Path.Combine(projectFolder, "TestAppInfo.cs");

            var appInfoFileContents = File.ReadAllText(appInfoFileName);

            var newAppInfoFileContents = appInfoFileContents
                .Replace("***APP-PFN-1-GOES-HERE***", $"{app1Details.PackageFamilyName}!App")
                .Replace("***APP-PFN-2-GOES-HERE***", $"{app2Details.PackageFamilyName}!App")
                .Replace("***APP-NAME-1-GOES-HERE***", app1Details.ProjectName)
                .Replace("***APP-NAME-2-GOES-HERE***", app2Details.ProjectName)
                .Replace("***FOLDER-GOES-HERE***", imagesFolder);

            if (pageAreasToExclude != null)
            {
                var replacement = string.Empty;

                foreach (var exclusion in pageAreasToExclude)
                {
                    replacement += $" {{ \"{exclusion.Key}\", {exclusion.Value} }},{Environment.NewLine}";
                }

                newAppInfoFileContents =
                    newAppInfoFileContents.Replace(
                        "PageSpecificExclusions = new Dictionary<string, ImageComparer.ExclusionArea>();",
                        $"PageSpecificExclusions = new Dictionary<string, ImageComparer.ExclusionArea>{{{replacement}}};");
            }

            File.WriteAllText(appInfoFileName, newAppInfoFileContents, Encoding.UTF8);

            // build test project
            var restoreNugetScript = $"& \"{projectFolder}\\nuget.exe\" restore \"{projectFolder}\\AutomatedUITests.csproj\" -PackagesDirectory \"{projectFolder}\\Packages\"";
            ExecutePowerShellScript(restoreNugetScript);
            var buildSolutionScript = $"& \"C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Enterprise\\MSBuild\\Current\\Bin\\MSBuild.exe\" \"{projectFolder}\\AutomatedUITests.sln\" /t:Rebuild /p:RestorePackagesPath=\"{projectFolder}\\Packages\" /p:Configuration=Debug /p:Platform=\"Any CPU\"";
            ExecutePowerShellScript(buildSolutionScript);

            return (projectFolder, imagesFolder);
        }

        private void RemoveCertificate(string certificatePath)
        {
            var uninstallCertificateScript = $"$dump = certutil.exe -dump {certificatePath}" + @"
ForEach ($i in $dump)
{
    if ($i.StartsWith(""Serial Number: ""))
            {
                $serialNumber = ($i -split ""Serial Number: "")[1]
                certutil -delstore TrustedPeople $serialNumber
                break
            }
        }";

            ExecutePowerShellScript(uninstallCertificateScript);
        }

        private void UninstallAppx(string packageFullName)
        {
            ExecutePowerShellScript($"Remove-AppxPackage -Package {packageFullName}");
        }

        private async Task<VisualComparisonTestDetails> SetUpProjectForUiTestComparisonAsync(string language, string projectType, string framework, IEnumerable<string> genIdentities, bool lastPageIsHome = false, bool createForScreenshots = false)
        {
            var result = new VisualComparisonTestDetails();

            var baseSetup = await SetUpComparisonProjectAsync(language, projectType, framework, genIdentities, lastPageIsHome: lastPageIsHome, includeMultipleInstances: false);

            result.ProjectName = baseSetup.ProjectName;

            if (createForScreenshots)
            {
                var pages = genIdentities.ToArray();

                if (pages.Count() == 1)
                {
                    var page = pages.First();

                    if (page == "wts.Page.MediaPlayer")
                    {
                        // Change auto-play to false so not trying to compare images of screen-shot with video playing
                        ReplaceInFiles("AutoPlay=\"True\"", "AutoPlay=\"False\"", baseSetup.ProjectPath, "*.xaml");
                    }
                }
            }

            // So building release version is fast
            ChangeProjectToNotUseDotNetNativeToolchain(baseSetup, language);

            ////Build solution in release mode  // Building in release mode creates the APPX and certificate files we need
            var solutionFile = $"{baseSetup.ProjectPath}\\{baseSetup.ProjectName}.sln";
            var buildSolutionScript = $"& \"C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Enterprise\\MSBuild\\Current\\Bin\\MSBuild.exe\" \"{solutionFile}\" /t:Restore,Rebuild /p:RestorePackagesPath=\"C:\\Packs\" /p:Configuration=Release /p:Platform=x86";
            ExecutePowerShellScript(buildSolutionScript);

            result.CertificatePath = InstallCertificate(baseSetup);

            // install appx
            var appxFile = $"{baseSetup.ProjectPath}\\{baseSetup.ProjectName}\\AppPackages\\{baseSetup.ProjectName}_1.0.0.0_x86_Test\\{baseSetup.ProjectName}_1.0.0.0_x86.msix";
            ExecutePowerShellScript($"Add-AppxPackage -Path {appxFile}");

            // get app package name
            var manifest = new XmlDocument();
            manifest.Load($"{baseSetup.ProjectPath}\\{baseSetup.ProjectName}\\Package.appxmanifest");
            var packageName = manifest.GetElementsByTagName("Package")[0].FirstChild.Attributes["Name"].Value;

            // get details from appx install
            var getPackageNamesScript = $"$PackageName = \"{packageName}\"" + @"
$packageDetails = Get-AppxPackage -Name $PackageName

$packageFamilyName = $packageDetails.PackageFamilyName
$packageFullName = $packageDetails.PackageFullName

Write-Output $packageFamilyName
Write-Output $packageFullName";

            var response = ExecutePowerShellScript(getPackageNamesScript);

            result.PackageFamilyName = response[0].ToString();
            result.PackageFullName = response[1].ToString();

            return result;
        }

        private void ReplaceInFiles(string find, string replace, string rootDirectory, string fileFilter)
        {
            foreach (var file in Directory.GetFiles(rootDirectory, fileFilter, SearchOption.AllDirectories))
            {
                // open, replace, overwrite
                var contents = File.ReadAllText(file);
                var newContent = contents.Replace(find, replace);
                File.WriteAllText(file, newContent);
            }
        }

        private string InstallCertificate((string SolutionPath, string ProjectName) baseSetup)
        {
            var cerFile = $"{baseSetup.SolutionPath}\\{baseSetup.ProjectName}\\AppPackages\\{baseSetup.ProjectName}_1.0.0.0_x86_Test\\{baseSetup.ProjectName}_1.0.0.0_x86.cer";

            ExecutePowerShellScript($"& certutil.exe -addstore TrustedPeople \"{cerFile}\"");

            return cerFile;
        }

        private void ChangeProjectToNotUseDotNetNativeToolchain((string SolutionPath, string ProjectName) baseSetup, string language)
        {
            var projFileName = $"{baseSetup.SolutionPath}\\{baseSetup.ProjectName}\\{baseSetup.ProjectName}.{GetProjectExtension(language)}";

            var projFileContents = File.ReadAllText(projFileName);

            var newProjFileContents = projFileContents.Replace(
                                                                "<UseDotNetNativeToolchain>true</UseDotNetNativeToolchain>",
                                                                "<UseDotNetNativeToolchain>false</UseDotNetNativeToolchain>");

            File.WriteAllText(projFileName, newProjFileContents, Encoding.UTF8);
        }

        private Collection<PSObject> ExecutePowerShellScript(string script, bool outputOnError = false)
        {
            using (var ps = System.Management.Automation.PowerShell.Create())
            {
                ps.AddScript(script);

                Collection<PSObject> psOutput = ps.Invoke();

                if (ps.Streams.Error.Count > 0)
                {
                    foreach (var errorRecord in ps.Streams.Error)
                    {
                        Debug.WriteLine(errorRecord.ToString());
                    }

                    // Some things (such as failing test execution) report an error but we still want the full output
                    if (!outputOnError)
                    {
                        throw new PSInvalidOperationException(ps.Streams.Error.First().ToString());
                    }
                }

                return psOutput;
            }
        }

        private class VisualComparisonTestDetails
        {
            public string CertificatePath { get; set; }

            public string PackageFamilyName { get; set; }

            public string PackageFullName { get; set; }

            public string ProjectName { get; set; }
        }
    }
}
