using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Reflection;
using POT.DAL;
using POT.Services;
using HSG.Helper;
using System.Text.RegularExpressions;

namespace POT.DAL
{
    [Serializable]
    public partial class vw_PO_Dashboard
    {
        #region Dates

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        //[DateRange(Defaults.minSQLDate.ToShortDateString(), Defaults.maxSQLDate.ToShortDateString())] - NOT needed because 
        //we use 'getValidDate' in functions interacting with DB
        public DateTime? PODateTo { get; set; }        
        public DateTime? PODateTo_SQL
        {// Check and return a valid SQL date
            get
            {
                if (PODateTo.HasValue) PODateTo = Defaults.getValidDate(PODateTo.Value);
                return PODateTo;
            }
        }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? PODateFrom { get; set; }
        public DateTime? PODateFrom_SQL
        {// Check and return a valid SQL date
            get
            {
                if (PODateFrom.HasValue) PODateFrom = Defaults.getValidDate(PODateFrom.Value);
                return PODateFrom;
            }
        }

        public string PODateOnly { get { return PODate.HasValue?PODate.Value.ToString(Defaults.dtFormat, Defaults.ci):""; } }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? ETAFrom { get; set; }
        public DateTime? ETAFrom_SQL
        {// Check and return a valid SQL date
            get
            {
                if (ETAFrom.HasValue) ETAFrom = Defaults.getValidDate(ETAFrom.Value);
                return ETAFrom;
            }
        }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? ETATo { get; set; }
        public DateTime? ETATo_SQL
        {// Check and return a valid SQL date
            get
            {
                if (ETATo.HasValue) ETATo = Defaults.getValidDate(ETATo.Value);
                return ETATo;
            }
        }

        public string ETAOnly { get { return Eta.HasValue ? Eta.Value.ToString(Defaults.dtFormat, Defaults.ci) : ""; } }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? ETDFrom { get; set; }
        public DateTime? ETDFrom_SQL
        {// Check and return a valid SQL date
            get
            {
                if (ETDFrom.HasValue) ETDFrom = Defaults.getValidDate(ETDFrom.Value);
                return ETDFrom;
            }
        }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? ETDTo { get; set; }
        public DateTime? ETDTo_SQL
        {// Check and return a valid SQL date
            get
            {
                if (ETDTo.HasValue) ETDTo = Defaults.getValidDate(ETDTo.Value);
                return ETDTo;
            }
        }

        public string ETDOnly { get { return Etd.HasValue ? Etd.Value.ToString(Defaults.dtFormat, Defaults.ci) : ""; } }

        #endregion
        public int? PONumber1 { get; set; }
        public string PONumbers { get; set; }
                
        //[Bindable(BindableSupport.No)]
        //public string ShipToLocAndCode { get { return Common.getLocationAndCode(this.ShipToLoc, this.ShipToCode); } }
    }
}

