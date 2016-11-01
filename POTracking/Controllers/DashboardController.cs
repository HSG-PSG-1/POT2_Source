using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using POT.DAL;
using POT.Services;
using HSG.Helper;
//using StackExchange.Profiling;

namespace POT.Controllers
{
    //[CompressFilter] - DON'T
    [IsAuthorize(IsAuthorizeAttribute.Rights.NONE)]//Special case for some dirty session-abandoned pages and hacks
    public partial class DashboardController : BaseController
    {
        const int fetchInitialPages = 2;
        string view = _Session.IsOnlyVendor ? "ListVendor" : "ListInternal";

        public DashboardController() : //HT: Make sure this is initialized with default constructor values!
            base(Config.DashboardPageSize, DashboardService.sortOn, Filters.list.Dashboard) { ;}

        #region List Grid Excel
        //[CompressFilter] - DON'T
        public ActionResult List(int? index, string qData)
        {
            index = index ?? 0;
            //Special case: Set the filter back if it existed so that if the user "re-visits" the page he gets the previous filter (unless reset or logged off)
            searchOpts = _Session.Search[Filters.list.Dashboard];//new vw_PO_Dashboard();

            populateData(true);
            ViewData["gridPageSize"] = gridPageSize; // Required to adjust pagesize for grid            

            // No need to return view - it'll fetched by ajax in partial rendering
            return View((object)FetchDataAndGetFirstPage(0, String.Empty)); // SO : 5635383/illegal-characters-in-path
        }

        #region Will need GET (for AJAX) & Post

        [CacheControl(HttpCacheability.NoCache)]//Don't mention GET or post as this is required for both!
        public ContentResult POListKO(int? index, string qData, bool? fetchAll)
        {            
            //Make sure searchOpts is assigned to set ViewState
            vw_PO_Dashboard oldSearchOpts = (vw_PO_Dashboard)searchOpts;

            System.Web.Script.Serialization.JavaScriptSerializer jsSerializer =
                new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = Int32.MaxValue }; //Json(new { records = result, search = oldSearchOpts }, JsonRequestBehavior.AllowGet);
            var jsonDataSet = new ContentResult
            {
                Content = jsSerializer.Serialize(new { records = Session["dashData"], search = oldSearchOpts }),
                ContentType = "application/json",
                //ContentEncoding = 
            };
            // MVC 4 : jsonResult.MaxJsonLength = int.MaxValue;
            return jsonDataSet;
        }

        [HttpPost]
        [SkipModelValidation]//HT: Use with CAUTION only meant for POSTBACK search Action
        public ActionResult POListKO(vw_PO_Dashboard searchObj, string doReset, string orderBy, bool? fetchAll)
        {
            searchOpts = (doReset == "on") ? new vw_PO_Dashboard() : searchObj; // Set or Reset Search-options
            populateData(false);// Populate ddl Viewdata

            orderBy = reFormatSort(orderBy);

            _Session.POIDs = new DashboardService().SearchPOIDKO(searchObj, orderBy);

            return Json(true);// WE just need to set it in the session
        }

        private string reFormatSort(string orderBy)
        {
            //Ensure that Orderby has the correcy field (not the custom field so need to replace)
            //Ensure that Orderby has the correcy field (not the custom field so need to replace)
            orderBy = orderBy.Replace("POno", "PONumber").Replace("OrdStat", "OrderStatus").Replace("Vndr", "VendorName").Replace("Brand", "BrandName")
                .Replace("Cmts", "CommentsExist").Replace("Files", "FilesHExist").Replace("POdt", "PODate").Replace("Ship", "ShipToCity");
            //orderBy = orderBy.Replace("PODateOnly", "PODate").Replace("ETDOnly", "ETD").Replace("ETAOnly", "ETA");

            return orderBy;
        }

        [HttpPost]
        [SkipModelValidation]
        public ActionResult SetSearchOpts(vw_PO_Dashboard searchObj)
        {
            if (searchObj != null)
            {//Called only to set filter via ajax
                searchOpts = searchObj;
                return Json(true);
            }
            return Json(false);
        }

        #endregion

        [HttpPost]
        [SkipModelValidation]
        public ActionResult Excel()
        {
            //HttpContext context = ControllerContext.HttpContext.CurrentHandler;
            //Essense of : http://stephenwalther.com/blog/archive/2008/06/16/asp-net-mvc-tip-2-create-a-custom-action-result-that-returns-microsoft-excel-documents.aspx
            this.Response.Clear();
            this.Response.AddHeader("content-disposition", "attachment;filename=" + "Dashboard_" + _SessionUsr.ID + ".xls");
            this.Response.Charset = "";
            this.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            this.Response.ContentType = "application/vnd.ms-excel";

            //DON'T do the following
            //this.Response.Write(content);
            //this.Response.End();

            populateData(false);
            var result = new DashboardService().Search(sortExpr, 1, gridPageSize, (vw_PO_Dashboard)searchOpts, true, _Session.IsOnlyVendor);

            searchOpts = new vw_PO_Dashboard();
            populateData(false);

            return View("Excel", result);
        }

        [HttpGet]
        public ActionResult Excel(string dummy)
        { // special case handling for sessiontimeout while loading excel download or user somehow trying to access the excel directly. SO : 16658020
            return RedirectToAction("List", "Dashboard");
        }

        /*public ActionResult ExcelPDF()
        {   
            populateData(false);
            List<vw_PO_Dashboard> printView = new DashboardService().Search(sortExpr, 1, gridPageSize, (vw_PO_Dashboard)searchOpts, true, _Session.IsOnlyVendor);
            
            string GUID = _SessionUsr.ID.ToString();
            return new ReportManagement.StandardPdfRenderer().BinaryPdfData(this, "Dashboard" + GUID, "Excel", printView);
        }*/
        #endregion

        #region Dialog Actions
        //[AccessPO("POID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Comments(int POID)
        {
            return View(new CommentService().Search(POID, null));
        }        

        //[AccessPO("POID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Files(int POID)
        {
            return View(new POFileService().Search(POID, null));
        }
        
        #endregion

        #region Extra Functions

        public void populateData(bool fetchOtherData)
        {
            //using (MiniProfiler.Current.Step("Populate lookup Data"))
            {
                vw_PO_Dashboard searchOptions = (vw_PO_Dashboard)(searchOpts);
                if (_Session.IsOnlyVendor)
                {//Set the Vendor filter
                    searchOptions.VendorID = _SessionUsr.OrgID;
                    searchOptions.VendorName = _SessionUsr.OrgName;
                }

                if (fetchOtherData)
                {
                    ViewData["Status"] = new LookupService().GetLookup(LookupService.Source.Status);
                    ViewData["UserList"] = new LookupService().GetLookup(LookupService.Source.User);
                }
            }
        }

        public string FetchDataAndGetFirstPage(int? index, string qData)
        {
            base.SetTempDataSort(ref index);// Set TempDate, Sort & index
            //Make sure searchOpts is assigned to set ViewState
            vw_PO_Dashboard oldSearchOpts = (vw_PO_Dashboard)searchOpts;
            searchOpts = new vw_PO_Dashboard();
            populateData(false); //Set the Vendor filter

            index = (index > 0) ? index + 1 : index; // paging starts with 2

            var result = from vw_u in new DashboardService().SearchKO(
                sortExpr, index, gridPageSize, (vw_PO_Dashboard)searchOpts, true, _Session.IsOnlyVendor) // fetchAll ?? false
                         select new
                         {
                             ID = vw_u.ID,
                             POno = vw_u.PONumber,
                             OrdStat = vw_u.OrderStatusID,
                             AssignTo = vw_u.AssignTo,
                             //VendorID = vw_u.VendorID,
                             Vndr = vw_u.VendorName,
                             Brand = vw_u.BrandName,
                             Status = vw_u.Status,
                             Cmts = vw_u.CommentsExist,
                             Files = vw_u.FilesHExist,
                             POdt = vw_u.PODateOnly,
                             ETA = vw_u.ETAOnly,
                             ETD = vw_u.ETDOnly,
                             Ship = vw_u.ShipToCity
                         };
            Session["dashData"] = result.Skip(gridPageSize * fetchInitialPages).ToList();
            //return Json(new { records = result, search = oldSearchOpts }, JsonRequestBehavior.AllowGet);
            System.Web.Script.Serialization.JavaScriptSerializer jsSerializer =
                new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = Int32.MaxValue }; //Json(new { records = result, search = oldSearchOpts }, JsonRequestBehavior.AllowGet);

            return jsSerializer.Serialize(new { records = result.Take(gridPageSize * fetchInitialPages), search = oldSearchOpts });
        }

        #endregion
    }
}
