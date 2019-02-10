﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.Templates.Core.Gen;
using Microsoft.Templates.Core.Templates;

namespace Microsoft.Templates.Core.PostActions.Catalog
{
    // This is a Template Defined Post-Action with the following configuration in the template.
    //   "postActions": [
    //    {
    //        "description": "Have UWP app reference this project",
    //        "manualInstructions": [ ],
    //        "actionId": ""849AAEB8-487D-45B3-94B9-77FA74E83A012",
    //        "args": {
    //            "fileIndex" : "0",
    //            "projectPath": "Param_ProjectName\\Param_ProjectName.csproj"
    //        },
    //        "continueOnError": "true"
    //    }
    //  ]
    // Expected args:
    //    - fileIndex -> The file index from the primary outputs which is the project file that will be referenced by the other project.
    //    - otherProjectPath -> The path to the project where the refrence will be added.
    public class AddProjectReferencesToContextPostAction : TemplateDefinedPostAction
    {
        public const string Id = "849AAEB8-487D-45B3-94B9-77FA74E83A01";

        public override Guid ActionId { get => new Guid(Id); }

        private readonly Dictionary<string, string> _parameters;
        private readonly IReadOnlyList<ICreationPath> _primaryOutputs;
        private readonly string _destinationPath;

        public AddProjectReferencesToContextPostAction(string relatedTemplate, IPostAction templatePostAction, IReadOnlyList<ICreationPath> primaryOutputs, Dictionary<string, string> parameters, string destinationPath)
            : base(relatedTemplate, templatePostAction)
        {
            _parameters = parameters;
            _primaryOutputs = primaryOutputs;
            _destinationPath = destinationPath;
        }

        internal override void ExecuteInternal()
        {
            var parameterReplacements = new FileRenameParameterReplacements(_parameters);
            var projectPath = Path.Combine(_destinationPath, parameterReplacements.ReplaceInPath(Args["projectPath"]));

            int targetProjectIndex = int.Parse(Args["fileIndex"]);
            var referencedProject = Path.GetFullPath(Path.Combine(_destinationPath, _primaryOutputs[targetProjectIndex].GetOutputPath(_parameters)));

            var projectReference = new ProjectReference
            {
                Project = projectPath,
                ReferencedProject = referencedProject,
            };

            if (!GenContext.Current.ProjectInfo.ProjectReferences.Any(n => n.Equals(projectReference)))
            {
                GenContext.Current.ProjectInfo.ProjectReferences.Add(projectReference);
            }
        }
    }
}
