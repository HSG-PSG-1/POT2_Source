using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using POT.Services;
using POT.Models;
using HSG.Helper;
using POT.DAL;
using System.Text.RegularExpressions;

namespace POT.Controllers
{
    [IsAuthorize(IsAuthorizeAttribute.Rights.ManageSetting)]
    public partial class SettingController : BaseController
    {
        #region Bulk Manage

        [CacheControl(HttpCacheability.NoCache), HttpGet]
        public ActionResult Manage()
        {
            if (Response.IsRequestBeingRedirected) return View();//Access denied

            doEditPopulate();
            return View(new SettingService().FetchAll());
        }

        [HttpPost]
        public ActionResult Manage(List<Setting> settings)
        {
            foreach (Setting s in settings)
                s.Value = s.SettingValue.val;

            if (!ModelState.IsValid || ! validateSettings(settings))
            {
                ViewData["oprSuccess"] = false;//Don't use base.oprSuccess because it doesn't redirect!
                doEditPopulate();
                return View(settings);
            }
            else//All OK so go ahead
            { 
                new SettingService().BulkUpdate(settings);//Performs Add, Edit & Delete by chacking each item
                base.operationSuccess = true;
                //Log Activity - for future
                new ActivityLogService(ActivityLogService.Activity.SettingChange).
                Add(new POT.DAL.ActivityHistory());
            }
            //return and refresh
            return RedirectToAction("Manage");
        }

        #endregion

        #region Extra Functions

        private void doEditPopulate()
        {
            ViewData["oprSuccess"] = base.operationSuccess;//oprSuccess will be reset after this

            //Get from LookupService
            ViewData[SettingService.settings.Default_PO_Assignee.ToString()]
                = new LookupService().GetLookup(LookupService.Source.User,false);
            ViewData[SettingService.settings.Default_PO_Status.ToString()]
                = new LookupService().GetLookup(LookupService.Source.Status, false);
            ViewData[SettingService.settings.Error_Detail_Level.ToString()]
                = new LookupService().GetLookup(LookupService.Source.Error_Detail_Level, false);            
        }

        private bool validateSettings(List<Setting> settings)
        {
            string settingVal = "[{0}].SettingValue.val";
            string rangeMsg = "Setting value must be between {0} and {1}.";
            int pos = 0, min = 0, max = 0;
            Regex emailRegex = new Regex(@"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*", RegexOptions.Compiled);

            foreach (Setting s in settings)
            {   
                switch (s.SettingValue.settingEnum)
                {
                    #region Remember me hours
                    case SettingService.settings.Remember_Me_Hours:
                        min = 10; max = 100;
                        if (!checkRange(min, max, s.SettingValue.val))
                            ModelState.AddModelError(
                                string.Format(settingVal, pos.ToString()),
                                string.Format(rangeMsg, min.ToString(), max.ToString()));
                        break;
                    #endregion

                    #region Grid page size
                    case SettingService.settings.Dashboard_Page_Size:
                    case SettingService.settings.User_List_Page_Size:
                        min = 10; max = 1000;
                        if (!checkRange(min, max, s.SettingValue.val))
                        ModelState.AddModelError(
                            string.Format(settingVal,pos.ToString()),
                            string.Format(rangeMsg,min.ToString(), max.ToString()));
                        break;
                    #endregion

                    #region Email
                    case SettingService.settings.Contact_Email:
                        
                        Match match = emailRegex.Match(s.SettingValue.val);
                        if (!((match.Success && (match.Index == 0)) && (match.Length == s.SettingValue.val.Length)))
                            ModelState.AddModelError(
                            string.Format(settingVal, pos.ToString()), "Invalid email.");
                        break;
                    #endregion
                }
                pos++;
            }

            return ModelState.IsValid;
        }

        private bool checkRange(int min, int max, string val)
        {
            try { return (int.Parse(val.Trim()) >= min && int.Parse(val.Trim()) <= max); }
            catch (Exception ex) { return false; }
        }

        #endregion
    }
}
