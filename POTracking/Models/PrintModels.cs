using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace POT.DAL
{
    public partial class POInternalPrint
    {
        public vw_POHeader view;
        public List<POComment> comments;
        public List<POFile> filesH;
        public List<vw_POLine> items;
    }
}

