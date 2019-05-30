#region Licence...

//----------------------------------------------
// The MIT License (MIT)
// Copyright (c) 2004-2018 Oleg Shilo
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//----------------------------------------------

#endregion Licence...

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using csscript;

namespace CSScriptLibrary
{
/// <summary>
    /// Information about the script parsing result.
    /// </summary>
    public class ScriptParsingResult
    {
        /// <summary>
        /// The packages referenced from the script with `//css_nuget` directive
        /// </summary>
        public string[] Packages;

        /// <summary>
        /// The referenced resources referenced from the script with `//css_res` directive
        /// </summary>
        public string[] ReferencedResources;

        /// <summary>
        /// The referenced assemblies referenced from the script with `//css_ref` directive
        /// </summary>
        public string[] ReferencedAssemblies;

        /// <summary>
        /// The namespaces imported with C# `using` directive
        /// </summary>
        public string[] ReferencedNamespaces;

        /// <summary>
        /// The namespaces that are marked as "to ignore" with `//css_ignore_namespace` directive
        /// </summary>
        public string[] IgnoreNamespaces;

        /// <summary>
        /// The compiler options specified with `//css_co` directive
        /// </summary>
        public string[] CompilerOptions;

        /// <summary>
        /// The directories specified with `//css_dir` directive
        /// </summary>
        public string[] SearchDirs;

        /// <summary>
        /// The precompilers specified with `//css_pc` directive
        /// </summary>
        public string[] Precompilers;

        /// <summary>
        /// All files that need to be compiled as part of the script execution.
        /// </summary>
        public string[] FilesToCompile;

        /// <summary>
        /// The time of parsing.
        /// </summary>
        public DateTime Timestamp = DateTime.Now;
    }
}