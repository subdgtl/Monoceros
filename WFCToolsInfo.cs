using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace WFCTools
{
    public class WFCToolsInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "WFC Tools";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return Properties.Resources.WFC;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "Supplemntal tools for Subdigital Wave Function Collapse solver.";
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
