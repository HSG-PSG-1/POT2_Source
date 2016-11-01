using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using POT.DAL;
using POT.Services;
using HSG.Helper;

namespace POT.Controllers
{
    public partial class DashboardController : BaseController
    {

        #region Actions for Status (Secured)

        [HttpPost]
        //[AccessPO("POID")]
        public ActionResult ChangePOStatus(int POID, int OldStatusID, int NewStatusID)
        {
            bool result = false; string msg = String.Empty;
            if (OldStatusID != NewStatusID)
            {
                result = new StatusHistoryService().UpdatePOStatus(POID, OldStatusID, NewStatusID);
                //Log Activity (before directory del and sesion clearing)
                new ActivityLogService(ActivityLogService.Activity.POEdit)
                    .Add(new ActivityHistory() { POID = POID, PONumber = POID.ToString() });
            }
            else // same status so no need to update
                msg = "No change, same status.";
            
            //Taconite XML
            return this.Content(Defaults.getTaconiteResult(result,
                Defaults.getOprResult(result, msg), "msgStatusHistory", "updateStatusHistory()"), "text/xml");
        }

        //[AccessPO("POID")]
        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Status(int POID, bool IsReadOnly = false)
        {
            //http://localhost:4915/PO/1/Status
            ViewData["IsReadOnly"] = IsReadOnly;
            // NOT need because in MAnage po we show it as readonly || _Session.PO.Archived;
            return View(new StatusHistoryService().FetchAll(POID));
        }

        #endregion

    }
}
