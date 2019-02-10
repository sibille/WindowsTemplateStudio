﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Templates.Core.Gen;
using Microsoft.Templates.Core.Helpers;

namespace Microsoft.Templates.Core.Locations
{
    public class TemplatesContent
    {
        private const string TemplatesFolderName = "Templates";

        public event EventHandler<ProgressEventArgs> NewVersionAcquisitionProgress;

        public event EventHandler<ProgressEventArgs> GetContentProgress;

        public string TemplatesFolder { get; private set; }

        public Version WizardVersion { get; private set; }

        public TemplatesSource Source { get; private set; }

        public string LatestContentFolder => GetContentFolder();

        public TemplatesContentInfo Current { get; private set; }

        public List<TemplatesContentInfo> All { get; private set; }

        public TemplatesContentInfo Latest { get => GetLatestContent(); }

        public TemplatesContent(string workingFolder, string sourceId, Version wizardVersion, TemplatesSource source, string tengineCurrentContent)
        {
            if (source is RemoteTemplatesSource)
            {
                TemplatesFolder = Path.Combine(workingFolder, TemplatesFolderName, sourceId, source.Platform, source.Language);
            }
            else
            {
                TemplatesFolder = Path.Combine(workingFolder, TemplatesFolderName, sourceId);
            }

            LoadAvailableContents();

            Source = source;
            SetCurrentContent(tengineCurrentContent, wizardVersion);

            WizardVersion = wizardVersion;
        }

        public bool Exists()
        {
            if (Current != null)
            {
                return Directory.Exists(Current.Path);
            }
            else
            {
                return false;
            }
        }

        public bool RequiresContentUpdate()
        {
            if (Current != null)
            {
                return Current.Version < Latest.Version;
            }
            else
            {
                return false;
            }
        }

        public bool IsNewVersionAvailable(out Version version)
        {
            version = Source.Config?.ResolvePackage(WizardVersion, Source.Platform, Source.Language)?.Version;
            if (Current != null && !Current.Version.IsNull() && Directory.Exists(Current.Path))
            {
                return Current.Version < version && (WizardVersion != version);
            }
            else
            {
                return WizardVersion < version;
            }
        }

        public bool IsWizardUpdateAvailable(out Version version)
        {
            version = null;

            if (Current != null && Source.Config?.Latest?.WizardVersions != null)
            {
                var highestWizardVersion = Source.Config.Latest.WizardVersions.OrderByDescending(t => t.Major).ThenByDescending(t => t.Minor).FirstOrDefault();
                var result = WizardVersion.Major < highestWizardVersion?.Major || ((WizardVersion.Major == highestWizardVersion?.Major) && (WizardVersion.Minor < highestWizardVersion?.Minor));

                if (result == true)
                {
                    version = new Version(highestWizardVersion.Major, highestWizardVersion.Minor);
                }

                return result;
            }
            else
            {
                return false;
            }
        }

        public async Task GetNewVersionContentAsync(CancellationToken ct)
        {
            try
            {
                var latestPackage = Source.Config.ResolvePackage(WizardVersion, Source.Platform, Source.Language);

                Source.NewVersionAcquisitionProgress += OnNewVersionAcquisitionProgress;
                Source.GetContentProgress += OnGetContentProgress;

                await Source.AcquireAsync(latestPackage, ct);

                if (latestPackage.LocalPath != null)
                {
                    TemplatesContentInfo content = await Source.GetContentAsync(latestPackage, TemplatesFolder, ct);

                    var alreadyExists = All.Where(p => p.Version == latestPackage.Version).FirstOrDefault();
                    if (alreadyExists != null)
                    {
                        All.Remove(alreadyExists);
                    }

                    if (content != null)
                    {
                        Current = content;
                        All.Add(content);
                    }
                }
            }
            finally
            {
                Source.NewVersionAcquisitionProgress -= OnNewVersionAcquisitionProgress;
                Source.GetContentProgress -= OnGetContentProgress;
            }
        }

        private void OnNewVersionAcquisitionProgress(object sender, ProgressEventArgs eventArgs)
        {
            NewVersionAcquisitionProgress?.Invoke(this, eventArgs);
        }

        private void OnGetContentProgress(object sender, ProgressEventArgs eventArgs)
        {
            GetContentProgress?.Invoke(this, eventArgs);
        }

        internal TemplatesPackageInfo ResolveInstalledContent()
        {
            TemplatesPackageInfo installedPackage = null;
            if (Source is RemoteTemplatesSource)
            {
                var mstxFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "InstalledTemplates", $"{Source.Platform}.{Source.Language}.Templates.mstx");

                if (File.Exists(mstxFilePath))
                {
                    installedPackage = new TemplatesPackageInfo()
                    {
                        Name = Path.GetFileName(mstxFilePath),
                        LocalPath = mstxFilePath,
                        WizardVersions = new List<Version>() { GenContext.GetWizardVersionFromAssembly() },
                    };
                }
            }
            else
            {
                installedPackage = LocalTemplatesSource.VersionZero;
            }

            return installedPackage;
        }

        internal async Task GetInstalledContentAsync(TemplatesPackageInfo packageInfo, CancellationToken ct)
        {
            try
            {
                Source.GetContentProgress += OnGetContentProgress;

                var package = await Source.GetContentAsync(packageInfo, TemplatesFolder, ct);
                if (package != null)
                {
                    Current = package;
                    All.Add(package);
                }
            }
            finally
            {
                Source.GetContentProgress -= OnGetContentProgress;
            }
        }

        public void Purge()
        {
            if (Directory.Exists(TemplatesFolder))
            {
                var di = new DirectoryInfo(TemplatesFolder);

                foreach (var sdi in di.EnumerateDirectories().Where(d => d.FullName != Current.Path))
                {
                    Version.TryParse(sdi.Name, out Version v);
                    if (!v.IsNull() && v < Current.Version)
                    {
                        Fs.SafeDeleteDirectory(sdi.FullName, false);
                    }
                }
            }
        }

        private string GetContentFolder()
        {
            var latestVersion = new Version(0, 0, 0, 0);
            string latestContent = Path.Combine(TemplatesFolder, "0.0.0.0");

            if (Directory.Exists(TemplatesFolder))
            {
                var di = new DirectoryInfo(TemplatesFolder);

                foreach (DirectoryInfo sdi in di.EnumerateDirectories())
                {
                    Version.TryParse(sdi.Name, out Version v);

                    if (v >= latestVersion)
                    {
                        latestVersion = v;
                        latestContent = sdi.FullName;
                    }
                }
            }

            return latestContent;
        }

        private void LoadAvailableContents()
        {
            All = new List<TemplatesContentInfo>();

            if (Directory.Exists(TemplatesFolder))
            {
                var di = new DirectoryInfo(TemplatesFolder);

                foreach (DirectoryInfo sdi in di.EnumerateDirectories())
                {
                    Version.TryParse(sdi.Name, out Version v);

                    if (!v.IsNull())
                    {
                        TemplatesContentInfo t = new TemplatesContentInfo()
                        {
                            Path = sdi.FullName,
                            Version = v,
                            Date = sdi.CreationTime,
                        };

                        All.Add(t);
                    }
                }
            }
        }

        private TemplatesContentInfo GetLatestContent()
        {
            return All.OrderByDescending(c => c.Version).ThenByDescending(c => c.Date).FirstOrDefault();
        }

        private void SetCurrentContent(string tengineCurrentContent, Version wizardVersion)
        {
            if (!string.IsNullOrEmpty(tengineCurrentContent))
            {
                Current = All.Where(c => c.Path.Equals(tengineCurrentContent, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            }
            else
            {
                var mainWizardVersion = $"{wizardVersion.Major.ToString()}.{wizardVersion.Minor.ToString()}";
                Current = All.Where(c => c.MainVersion.Equals(mainWizardVersion, StringComparison.OrdinalIgnoreCase)).OrderByDescending(c => c.Version).ThenByDescending(c => c.Date).FirstOrDefault();
            }
        }

        internal void RefreshContentFolder(string tengineCurrentContent)
        {
            Current = All.Where(c => c.Path.Equals(tengineCurrentContent, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }
    }
}
