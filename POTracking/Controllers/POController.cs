using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using POT.DAL;
using POT.Services;
using HSG.Helper;

namespace POT.Controllers
{
    //[CompressFilter] - don't use it here
    [IsAuthorize(IsAuthorizeAttribute.Rights.NONE)]//Special case for some dirty session-abandoned pages and hacks
    public partial class POController : BaseController
    {
        #region Actions for PO (Secured)

        [AccessPO("POID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Manage(int POID, string activeTab, bool? printPOAfterSave)
        {
            ViewData["oprSuccess"] = base.operationSuccess; //oprSuccess will be reset after this
            ViewData["printPOAfterSave"] = (TempData["printPOAfterSave"] ?? false);
            ViewData["activeTab"] = activeTab??"2";

            #region Edit mode

            #region Get PO view and check if its empty or archived - redirect

            POHeader po = new POService().GetPOInfoById(POID);// GetPOHeaderById

            #region Special case for invalid PO access

            if (po.ID <= Defaults.Integer && 
                (po.OrderStatusID == null || po.OrderStatusID == Defaults.Integer))
            {
                ViewData["Message"] = "PO not found";
                return View("DataNotFound"); /* deleted po accessed from Log*/
            }
            // In case an archived entry is accessed
            if (po.Archived)
            {
                ViewData["Message"] = "PO has been archived";
                return View("DataNotFound");
            }
            // For Future - return RedirectToAction("Archived", new { POID = POID });
            
            #endregion

            //Empty so invalid POID - go to Home
            if (po == new POService().emptyPO)//emptyView
                return RedirectToAction("List", "Dashboard");

            #endregion
            po.POGUID = System.Guid.NewGuid().ToString();
            po.AssignToIDold = po.AssignTo;
            
            return View(po);

            #endregion
        }

        [HttpPost]
        [AccessPO("POID")]
        public ActionResult Delete(int POID, string POGUID, string PONumber)
        {
            //http://www.joe-stevens.com/2010/02/16/creating-a-delete-link-with-mvc-using-post-to-avoid-security-issues/
            //http://stephenwalther.com/blog/archive/2009/01/21/asp.net-mvc-tip-46-ndash-donrsquot-use-delete-links-because.aspx
            //Anti-FK: http://blog.codeville.net/2008/09/01/prevent-cross-site-request-forgery-csrf-using-aspnet-mvcs-antiforgerytoken-helper/

            #region Delete PO & log activity

            new POService().Delete(new POHeader() { ID = POID });
            //Log Activity (before directory del and sesion clearing)
            new ActivityLogService(ActivityLogService.Activity.PODelete).Add(
                new ActivityHistory() { POID = POID, PONumber = PONumber });

            #endregion

            // Make sure the PREMANENT files are also deleted
            FileIO.DeleteDirectory(System.IO.Directory.GetParent(FileIO.GetPOFilesDirectory(POID, POGUID)).FullName);
            // Reset PO in session (no GUID cleanup needed - for NEW PO)
            //_Session.POsInMemory.Remove(POGUID); // Remove the Claim from session
            //
            return Redirect("~/Dashboard");
        }
                
        [HttpPost]
        public JsonResult CleanupTempUpload(int POID, string POGUID)
        {   // Unable to trigger action due to - e.returnValue = 'Make ..'; (frozen for now)
            // Make sure the temp files are also deleted
            _Session.ResetPOInSessionAndEmptyTempUpload(POID, POGUID);
            return new JsonResult() { Data = new { msg = "Temp file upload cleanup triggered." } };
        }

        [HttpPost]
        [AccessPO("POID")]
        public ActionResult Manage(int POID, bool isAddMode, [FromJson]POHeader poObj, [FromJson] IEnumerable<POComment> comments,
            [FromJson] IEnumerable<POFile> files, /*int OrderStatusIDold,*/string activeTab, bool? printPOAfterSave)
        {
            bool success = false;
            //return new JsonResult() { Data = new{ msg = "success"}};

            //HT: Note the following won't work now as we insert a record in DB then get it back in edit mode for Async edit
            //bool isAddMode = (poObj.ID <= Defaults.Integer); 

            poObj.setDatesFromStr(); // ensure that the timezone & cultural neutral dates are set as expected

            #region Perform operation proceed and set result

            string result = new POService().AsyncBulkAddEditDelKO(poObj, poObj.OrderStatusIDold ?? -1 /*OrderStatusIDold*/, comments, files);
            success = !string.IsNullOrEmpty(result);

            if (!success) { /*return View(poObj);*/}
            else //Log Activity based on mode
            {
                poObj.PONumber = result;// Set PO #
                ActivityLogService.Activity act = /*isAddMode ? ActivityLogService.Activity.POAdd :*/ ActivityLogService.Activity.POEdit;
                new ActivityLogService(act).Add(new ActivityHistory() { POID = poObj.ID, PONumber = poObj.PONumber.ToString() });
            }

            #endregion

            base.operationSuccess = success;//Set opeaon success
            //_Session.ClaimsInMemory.Remove(claimObj.ClaimGUID); // Remove the Claim from session
            //_Session.ResetPOInSessionAndEmptyTempUpload(poObj.POGUID); // reset because going back to Manage will automatically creat new session

            if (success)
                TempData["printPOAfterSave"] = printPOAfterSave.HasValue && printPOAfterSave.Value;

            #region Send AssignTo email if Old value is other then the new one (on hold / obsolete)
            //if (poObj.AssignTo > 0 && poObj.AssignTo != poObj.AssignToIDold)
            //    CommentService.SendEmail(POID, poObj.AssignTo.Value, poObj.PONumber, new POComment() { Comment1 = "(no comment)" });
            #endregion

            return RedirectToAction("Manage", new { POID = poObj.ID, activeTab = activeTab });
        }
        
        #endregion

        [AccessPO("POID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Print(int POID)
        {
            POInternalPrint printView = new POInternalPrint();

            List<POComment> comments = new List<POComment>();
            List<POFile> filesH = new List<POFile>();
            List<vw_POLine> items = new List<vw_POLine>();

            #region Fetch PO data and set Viewstate
            vw_POHeader vw = new POService().GetPOByIdForPrint(POID,
                ref comments, ref filesH, ref items, !_Session.IsAsiaVendor);

            vw.POGUID = System.Guid.NewGuid().ToString();

            //Set data in View
            ViewData["comments"] = comments;
            //ViewData["filesH"] = filesH; NOT needed yet
            ViewData["items"] = items;

            printView.view = vw;
            printView.comments = comments;
            //printView.filesH = filesH;
            printView.items = items;
            #endregion

            if (vw == null || vw.ID < 1)//Empty so invalid POID - go to Home
                return RedirectToAction("List", "Dashboard");

            if (vw.ID <= Defaults.Integer && vw.OrderStatusID == Defaults.Integer && vw.AssignTo == Defaults.Integer)
            { ViewData["Message"] = "PO not found"; return View("DataNotFound"); }// deleted po accessed from Log

            //Log Activity
            new ActivityLogService(ActivityLogService.Activity.POPrint).
                Add(new ActivityHistory() { POID = POID, PONumber = vw.PONumber.ToString() });

            return View(printView);
        }

        public ActionResult PrintPO(int POID, string PONumber)
        {
            //Log Activity
            new ActivityLogService(ActivityLogService.Activity.POPrint).Add(new ActivityHistory() { POID = POID, PONumber = PONumber });
            //Handled in KO
            return View();
        }

        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Info(int POID, string POGUID)
        {
            ViewData["POGUID"] = POGUID;
            return View(new POInfoKOModel() { Info = new POHeader() { ID = POID, POGUID = POGUID } });
        }

        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public JsonResult POEditKOViewModel(int POID, string POGUID)
        {// NEW consolidated viewmodel

            POKOViewModel povm = new POKOViewModel(); // Main consolidated viewmodel

            vw_POHeader vwPOHdr = new POService().GetPOHeaderById(POID);// To set GUID
            POHeader poHdr = new POService().GetPOInfoById(POID);

            
            poHdr.POGUID = POGUID; vwPOHdr.POGUID = POGUID;  

            vwPOHdr = doAddEditPopulateKO(vwPOHdr);

            povm.Header = vwPOHdr;
            povm.Lines = new DetailService().Search(POID, null);

            POInfoKOModel vmPOInfo = doAddEditPopulateInfoKO(poHdr);
            povm.Info = vmPOInfo.Info;

            // For dropdown
            povm.Carrier = vmPOInfo.Carrier;
            povm.Status = vmPOInfo.Status;
            povm.ContainerType = vmPOInfo.ContainerType;

            // Comments
            povm.Comments = GetCommentKOModel(POID, POGUID, poHdr.AssignTo ?? -1);

            // Files
            povm.Files = GetFileKOModel(POID, POGUID);

            // Status History
            povm.StatusHistory = new StatusHistoryService().FetchAll(POID);

            return Json(povm, JsonRequestBehavior.AllowGet);
        }

        #region Extra Functions (for PO actions)

        public vw_POHeader doAddEditPopulateKO(vw_POHeader poData)
        {
            poData.AssignTo = (poData.AssignTo ?? -1); // Special case for some unwanted records!
            poData.AssignToOld = (poData.AssignTo ?? -1);

            poData.POGUID = System.Guid.NewGuid().ToString(); // set unique identifier ID
            
            return poData;
        }

        public POInfoKOModel doAddEditPopulateInfoKO(POHeader poObj)
        {
            poObj.OrderStatusIDold = poObj.OrderStatusID;
            poObj.AssignToIDold = poObj.AssignTo ?? -1;

            POInfoKOModel vm = new POInfoKOModel()
            {
                Info = poObj,
                Carrier = new LookupService().GetLookup(LookupService.Source.Carrier),
                ContainerType = new LookupService().GetLookup(LookupService.Source.ContainerType),
                Status = new LookupService().GetLookup(LookupService.Source.Status)
            };

            return vm;
        }
        
        #endregion
    }
}

namespace POT.DAL
{    
    public class POInfoKOModel
    {
        public POHeader Info { get; set; }
        public IEnumerable Carrier { get; set; }
        public IEnumerable ContainerType { get; set; }
        public IEnumerable Status { get; set; }
    }

    public class POKOViewModel
    {
        public vw_POHeader Header { get; set; }
        public List<vw_POLine> Lines { get; set; }
        
        public string LinesOrderExtTotal { get { return Lines.Sum(l => l.OrderExtension ?? 0.00M).ToString("#0.00"); } }
        //public string TotalAmtInvoiced { get { return Lines.Sum(l => l.AmountInvoiced ?? 0.00M).ToString("#0.00"); } }
        public string OrderTotal { get { return Lines.Sum(l => l.QtyOrdered ?? 0).ToString(); } }

        public POHeader Info { get; set; }

        // For dropdown
        public IEnumerable Carrier { get; set; }
        public IEnumerable Status { get; set; }
        public IEnumerable ContainerType { get; set; }
        
        // Comments
        public CommentVM Comments { get; set; }

        //Files
        public FileVM Files { get; set; }

        public List<vw_StatusHistory_Usr> StatusHistory { get; set; }
    }
}