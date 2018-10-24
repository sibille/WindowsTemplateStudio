﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Templates.Core.Diagnostics;
using Microsoft.Templates.Core.Gen;
using Microsoft.Templates.Core.Resources;

namespace Microsoft.Templates.Core.PostActions.Catalog
{
    public class AddContextItemsToSolutionAndProjectPostAction : PostAction
    {
        internal override async Task ExecuteInternal()
        {
            GenContext.ToolBox.Shell.ShowStatusBarMessage(StringRes.StatusAddingProjects);

            var chrono = Stopwatch.StartNew();

            //GenContext.ToolBox.Shell.AddProjectsToSolution(GenContext.Current.Projects, usesAnyCpu: false);
            
            GenContext.ToolBox.Shell.AddProjectsAndNugetsToSolution(GenContext.Current.Projects, GenContext.Current.NugetReferences);
            //await GenContext.ToolBox.Shell.AddNugetToProjectsAsync(GenContext.Current.NugetReferences);
            GenContext.ToolBox.Shell.AddReferencesToProjects(GenContext.Current.ProjectReferences);
            GenContext.Current.ProjectMetrics[ProjectMetricsEnum.AddProjectToSolution] = chrono.Elapsed.TotalSeconds;
            chrono.Reset();

            GenContext.ToolBox.Shell.ShowStatusBarMessage(StringRes.StatusAddingItems);
            GenContext.ToolBox.Shell.AddItems(GenContext.Current.ProjectItems.ToArray());

            chrono.Stop();

            GenContext.Current.ProjectMetrics[ProjectMetricsEnum.AddFilesToProject] = chrono.Elapsed.TotalSeconds;
            GenContext.Current.ProjectItems.Clear();
        }
    }
}
