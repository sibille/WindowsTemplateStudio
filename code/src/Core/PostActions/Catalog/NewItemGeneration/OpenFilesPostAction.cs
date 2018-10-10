﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Templates.Core.Gen;
using Microsoft.Templates.Core.Resources;
using System.Threading.Tasks;

namespace Microsoft.Templates.Core.PostActions.Catalog
{
    public class OpenFilesPostAction : PostAction
    {
        internal override async Task ExecuteInternal()
        {
            GenContext.ToolBox.Shell.ShowStatusBarMessage(StringRes.StatusOpeningItems);
            GenContext.ToolBox.Shell.OpenItems(GenContext.Current.FilesToOpen.ToArray());
            GenContext.Current.FilesToOpen.Clear();
            await Task.CompletedTask;
        }
    }
}
