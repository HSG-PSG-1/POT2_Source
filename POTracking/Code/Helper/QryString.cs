using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using POT.DAL;
using POT.Models;
using POT.Services;
using System.Web.Security;

namespace HSG.Helper
{
    // This class mostly includes SortColumn related QueryString members
    public class QryString
    {
        public const string _sidx = "sidx", _sord = "sord", asc = "asc", desc = "desc", _sidx2 = "sidx2", _sord2 = "sord2";

        public static string sidx
        {
            get
            {
                return HttpContext.Current.Request.QueryString[_sidx]??"";
            }
        }

        public static string sord
        {
            get
            {
                return HttpContext.Current.Request.QueryString[_sord]??(sidx.Length>0?asc:"");
            }
        }

        public static string sidx2
        {
            get
            {
                return HttpContext.Current.Request.QueryString[_sidx2]??"";
            }
        }

        public static string sord2
        {
            get
            {
                return HttpContext.Current.Request.QueryString[_sord2] ?? (sidx.Length > 0 ? asc : "");
            }
        }

        public static string NewSort
        {
            get
            {
                return string.IsNullOrEmpty((sidx ?? "").ToString()) ? "" : sidx + " " + sord;
            }            
        }

        public static string OldSort
        {
            get
            {
                return string.IsNullOrEmpty((sidx2 ?? "").ToString()) ? "" : sidx2 + " " + sord2;
            }
        }        
        /*
         How to access controller to make the session data controller / page specific
         htmlHelper.ViewContext.Controller.ToString() = POT.Controllers.DashboardController
         htmlHelper.ViewContext.Controller.ControllerContext.RouteData.Values["controller"] = Dashboard
         new POT.Controllers.DashboardController().ToString() = POT.Controllers.DashboardController
        */        
    }
}
