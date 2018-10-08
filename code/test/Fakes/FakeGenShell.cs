﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Templates.Core.Gen;

using Microsoft.VisualStudio.TemplateWizard;

namespace Microsoft.Templates.Fakes
{
    public class FakeGenShell : GenShell
    {
        private readonly Action<string> _changeStatus;
        private readonly Action<string> _addLog;
        private readonly Window _owner;

        private string _language;
        private string _platform;

        public string SolutionPath
        {
            get
            {
                if (GenContext.Current != null)
                {
                    return Path.Combine(Path.GetDirectoryName(GenContext.Current.DestinationPath), $"{GenContext.Current.ProjectName}.sln");
                }

                return null;
            }
        }

        public FakeGenShell(string platform, string language, Action<string> changeStatus = null, Action<string> addLog = null, Window owner = null)
        {
            _platform = platform;
            _language = language;
            _changeStatus = changeStatus;
            _addLog = addLog;
            _owner = owner;
        }

        public void SetCurrentLanguage(string language)
        {
            _language = language;
        }

        public void SetCurrentPlatform(string platform)
        {
            _platform = platform;
        }

        public override void AddItems(params string[] itemsFullPath)
        {
            if (itemsFullPath == null || itemsFullPath.Length == 0)
            {
                return;
            }

            var projects = ResolveProjectFiles(itemsFullPath, true);
            var notCoreProjects = projects.Where(f => !IsCoreProject(f.Key));

            foreach (var projectFile in notCoreProjects)
            {
                var msbuildProj = FakeMsBuildProject.Load(projectFile.Key);
                if (msbuildProj != null)
                {
                    foreach (var file in projectFile.Value)
                    {
                        msbuildProj.AddItem(file);
                    }

                    msbuildProj.Save();
                }
            }
        }

        public override void AddProjectsToSolution(List<string> projectPaths, bool usesAnyCpu)
        {
            foreach (var projectPath in projectPaths)
            {
                var msbuildProj = FakeMsBuildProject.Load(projectPath);
                var solutionFile = FakeSolution.LoadOrCreate(_platform, SolutionPath);

                solutionFile.AddProjectToSolution(_platform, msbuildProj.Name, msbuildProj.Guid, projectPath, usesAnyCpu);
            }
        }

        public override string GetActiveProjectNamespace()
        {
            return GenContext.Current.ProjectName;
        }

        public override void CleanSolution()
        {
        }

        public override void SaveSolution()
        {
        }

        public override void ShowStatusBarMessage(string message)
        {
            _changeStatus?.Invoke(message);
        }

        public override string GetActiveProjectGuid()
        {
            var projectFileName = FindProject(GenContext.Current.DestinationPath);

            if (string.IsNullOrEmpty(projectFileName))
            {
                throw new Exception($"There is not project file in {GenContext.Current.DestinationPath}");
            }

            var msbuildProj = FakeMsBuildProject.Load(projectFileName);
            return msbuildProj.Guid;
        }

        public override string GetActiveProjectTypeGuids()
        {
            var projectFileName = FindProject(GenContext.Current.DestinationPath);

            if (string.IsNullOrEmpty(projectFileName))
            {
                throw new Exception($"There is not project file in {GenContext.Current.DestinationPath}");
            }

            var msbuildProj = FakeMsBuildProject.Load(projectFileName);
            return msbuildProj.ProjectTypeGuids;
        }

        public override string GetActiveProjectName()
        {
            return GenContext.Current.ProjectName;
        }

        public override string GetActiveProjectPath()
        {
            return (GenContext.Current != null) ? GenContext.Current.DestinationPath : string.Empty;
        }

        public override string GetActiveProjectLanguage()
        {
            return _language;
        }

        protected override string GetSelectedItemPath()
        {
            return string.Empty;
        }

        private static string FindProject(string path)
        {
            return Directory.EnumerateFiles(path, "*proj", SearchOption.AllDirectories).FirstOrDefault();
        }

        public override void SetDefaultSolutionConfiguration(string configurationName, string platformName, string projectName)
        {
        }

        public override void ShowTaskList()
        {
        }

        public override void ShowModal(Window dialog)
        {
            dialog.Owner = _owner;
            dialog.ShowDialog();
        }

        public override void CancelWizard(bool back = true)
        {
            if (back)
            {
                throw new WizardBackoutException();
            }
            else
            {
                throw new WizardCancelledException();
            }
        }

        public override void WriteOutput(string data)
        {
            _addLog?.Invoke(data);
        }

        public override void CloseSolution()
        {
        }

        public override Guid GetVsProjectId()
        {
            Guid.TryParse(GetActiveProjectGuid(), out Guid guid);
            return guid;
        }

        public override void OpenItems(params string[] itemsFullPath)
        {
        }

        public override bool IsDebuggerEnabled()
        {
            return false;
        }

        public override bool IsBuildInProgress()
        {
            return false;
        }

        public override void OpenProjectOverview()
        {
        }

        public override void AddReferencesToProjects(Dictionary<string, List<string>> projectReferences)
        {
            var solution = FakeSolution.LoadOrCreate(_platform, SolutionPath);
            var projectGuids = solution.GetProjectGuids();

            foreach (var projectPath in projectReferences.Keys)
            {
                var parentProject = FakeMsBuildProject.Load(projectPath);

                foreach (var referenceToAdd in projectReferences[projectPath])
                {
                    var referenceProject = FakeMsBuildProject.Load(referenceToAdd);

                    var name = referenceProject.Name;
                    var guid = projectGuids[name];
                    parentProject.AddProjectReference(referenceToAdd, guid, name);
                }

                parentProject.Save();
            }
        }

        public override void AddNugetToProjectsAsync(List<NugetReference> nugetReferences)
        {
            throw new NotImplementedException();
        }

        private bool IsCoreProject(string projectPath)
        {
            return projectPath.EndsWith("Core.csproj") || projectPath.EndsWith("Core.vbproj");
        }
    }
}
