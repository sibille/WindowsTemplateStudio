﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Reflection;
using EnvDTE;
using EnvDTE80;
using Microsoft.Templates.Core;
using Microsoft.Templates.UI.Resources;
using Microsoft.Templates.UI.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Templates.UI.VisualStudio
{
    internal class VsOutputPane
    {
        private const string TemplatesPaneGuid = "45480fff-0658-42e1-97f0-82cac23603aa";
        private OutputWindowPane _pane;

        public VsOutputPane()
        {
            _pane = GetOrCreatePane(Guid.Parse(TemplatesPaneGuid), true, false);

            if (_pane != null)
            {
                _pane.Activate();
            }
        }

        public void Write(string data)
        {
            _pane.OutputString(data);
        }

        private static OutputWindowPane GetOrCreatePane(Guid paneGuid, bool visible, bool clearWithSolution)
        {
            OutputWindowPane result = null;

            try
            {
                if (ServiceProvider.GlobalProvider.GetService(typeof(DTE)) is DTE2 dte)
                {
                    result = GetUwpPane(dte, paneGuid);

                    if (result == null)
                    {
                        CreateUwpPane(paneGuid, visible, clearWithSolution, StringRes.WindowsTemplateStudio);
                        result = GetUwpPane(dte, paneGuid);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Exception creating Visual Studio Output window pane. {ex.ToString()}");
            }

            return result;
        }

        private static void CreateUwpPane(Guid paneGuid, bool visible, bool clearWithSolution, string title)
        {
            SafeThreading.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsOutputWindow output = ServiceProvider.GlobalProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;

            // Create a new pane.
            int createRetVal = output.CreatePane(
                ref paneGuid,
                title,
                Convert.ToInt32(visible),
                Convert.ToInt32(clearWithSolution));

            output.GetPane(ref paneGuid, out var pane);
            pane.OutputString($"Windows Template Studio {DateTime.Now.FormatAsFullDateTime()}\n");
            pane.OutputString($"Version: {GetVersion()}\n");
            pane.OutputString($">\n");
        }

        private static string GetVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            return assembly.GetName().Version.ToString();
        }

        private static OutputWindowPane GetUwpPane(DTE2 dte,  Guid uwpOutputPaneGuid)
        {
            OutputWindowPanes panes = dte.ToolWindows.OutputWindow.OutputWindowPanes;
            OutputWindowPane result = null;

            foreach (OutputWindowPane p in panes)
            {
                if (Guid.Parse(p.Guid).ToString() == uwpOutputPaneGuid.ToString())
                {
                    result = p;
                }
            }

            return result;
        }
    }
}
