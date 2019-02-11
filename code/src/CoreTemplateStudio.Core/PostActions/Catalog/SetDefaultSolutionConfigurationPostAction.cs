﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.Templates.Core.Gen;

namespace Microsoft.Templates.Core.PostActions.Catalog
{
    public class SetDefaultSolutionConfigurationPostAction : PostAction
    {
        private const string Configuration = "Debug";
        private const string Platform = "x86";
        private readonly string uwpProjectGuid = "A5A43C5B-DE2A-4C0C-9213-0A381AF9435A";

        internal override void ExecuteInternal()
        {
            GenContext.ToolBox.Shell.SetDefaultSolutionConfiguration(Configuration, Platform, uwpProjectGuid);
        }
    }
}
