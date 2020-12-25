// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace WFCToolset
{
    public class WFCToolsInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "WFC Toolset";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return WFCToolset.Properties.Resources.WFC;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "Subdigital's Wave Function Collapse toolset.";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("e02a564d-2a18-4990-bfc2-852fb04f9268");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Subdigital";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "www.sub.digital | info@sub.digital";
            }
        }
    }
}
