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
    public partial class DashboardController : BaseController
    {       
        #region List Grid Excel JQG)
        
        public ActionResult ListJQG()
        {
            //ViewData["gridData"] = TempData["gridData"]??"";
            return View();
        }

        [CacheControl(HttpCacheability.NoCache)]//Don't mention GET or post as this is required for both!
        public ContentResult POListjqg()
        {
            //Make sure searchOpts is assigned to set ViewState
            vw_PO_Dashboard oldSearchOpts = (vw_PO_Dashboard)searchOpts;
            searchOpts = new vw_PO_Dashboard();
            populateData(false); //Set the Vendor filter

            string orderBy = reFormatSort(sortExpr);

            var result = from vw_u in new DashboardService().SearchKO(
                orderBy, null, gridPageSize, (vw_PO_Dashboard)searchOpts, true, _Session.IsOnlyVendor)
                         select new
                         {
                             ID = vw_u.ID,
                             POno = vw_u.PONumber,
                             OrdStat = vw_u.OrderStatusID,
                             AssignTo = vw_u.AssignTo,
                             //VendorID = vw_u.VendorID,
                             Vndr = vw_u.VendorName,
                             Brand = vw_u.BrandName,
                             Status = vw_u.OrderStatusID,//Status,
                             Cmts = vw_u.CommentsExist,
                             Files = vw_u.FilesHExist,
                             POdt = vw_u.PODateOnly,
                             ETA = vw_u.ETAOnly,
                             ETD = vw_u.ETDOnly,
                             Ship = vw_u.ShipToCity
                         };
            
            System.Web.Script.Serialization.JavaScriptSerializer jsSerializer =
                new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = Int32.MaxValue }; //Json(new { records = result, search = oldSearchOpts }, JsonRequestBehavior.AllowGet);

            var jsonDataSet = new ContentResult
            {
                Content = jsSerializer.Serialize(result.Take(2900)),
                ContentType = "application/json",
                //ContentEncoding = 
            };
            // MVC 4 : jsonResult.MaxJsonLength = int.MaxValue;
            return jsonDataSet;
        }

        [HttpPost]
        [SkipModelValidation]
        [ValidateInput(false)]
        public ActionResult ExportGridConfig(string txtGridConf)
        {
            TempData["gridData"] = txtGridConf;
            return RedirectToAction("ListJQG");
        }
        #endregion

        #region Dialog Actions
        //[AccessPO("POID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public JsonResult CommentsJQG(int POID)
        {
            return Json(new CommentService().Search(POID, null), JsonRequestBehavior.AllowGet);
        }        

        //[AccessPO("POID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public JsonResult FilesJQG(int POID)
        {
            return Json(new POFileService().Search(POID, null), JsonRequestBehavior.AllowGet);
        }
        
        #endregion

        #region Extra Functions

        
        #endregion
    }
}
