﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Templates.Core
{
    public static class ProgrammingLanguages
    {
        public const string CSharp = "C#";

        public const string VisualBasic = "VisualBasic";

        public static IEnumerable<string> GetAllLanguages()
        {
            yield return ProgrammingLanguages.CSharp;
            yield return ProgrammingLanguages.VisualBasic;
        }

        public static string GetShortProgrammingLanguage(string language)
        {
            switch (language)
            {
                case CSharp:
                    return "CS";
                case VisualBasic:
                    return "VB";
                default:
                    return language;
            }
        }
    }
}
