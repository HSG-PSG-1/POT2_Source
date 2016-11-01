using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Data.Linq.SqlClient;
using POT.DAL;
using POT.Models;
using HSG.Helper;
using Webdiyer.WebControls.Mvc;

namespace POT.Services
{
    public class SettingService : _ServiceBase
    {
        #region Variables

        public enum settings
        {
            Empty = 0,
            Default_PO_Assignee = 1,
            Default_PO_Status,
            Remember_Me_Hours,
            Error_Detail_Level,
            Dashboard_Page_Size,
            User_List_Page_Size,
            Contact_Email
        }

        #endregion

        #region Search / Fetch

        public List<Setting> FetchAll()
        {
            using (dbc)
            {
                return (from s in dbc.Settings
                        join u in dbc.Users on new { UserID = s.LastModifiedBy } equals new { UserID = u.ID } into msu
                        from usr in msu.DefaultIfEmpty() /* Make sure its left outer-join otherwise it'll not display records! */
                        select Transform(s, usr.Email)).ToList();
            }
        }

        public Hashtable FetchSettings()
        { 
            List<Setting> settingLst = FetchAll();

            Hashtable settings = new Hashtable(settingLst.Count);

            foreach (Setting s in settingLst)
                settings.Add(s.Name.Trim(), s.Value.Trim());//trim values to avoid issues

            return settings;
        }

        Setting Transform(Setting s, string userName)
        {
            return new Setting() { ID = s.ID, Name = s.Name, Value = s.Value, Description = s.Description,
                LastModifiedBy = s.LastModifiedBy, LastModifiedDate = s.LastModifiedDate,LastModifiedByVal = userName,
                SettingValue = new Setting.SettingVal(){ val = s.Value, setting = s.Name} };
        }

        public string GetContactEmail()
        {
            return dbc.Settings.SingleOrDefault
                (s => s.Name.ToLower() == settings.Contact_Email.ToString().ToLower()).Value;
                /*(from s in dbc.Settings
                    where s.Setting.ToLower() == settings.Contact_Email.ToString().ToLower()
                    select s.Value).SingleOrDefault().ToString();*/
        }

        #endregion

        #region Bulk Update

        public void Update(Setting sObj)
        {
            if (sObj.ID <= Defaults.Integer) // Insert
                return;//HT:SPECIAL CASE: W've handled Add separately so we skip //Add(rrObj);

            else // Update
            {                
                dbc.Settings.Attach(sObj);
                dbc.Refresh(System.Data.Linq.RefreshMode.KeepCurrentValues, sObj);
                
                //dbc.SubmitChanges(); Done later
            }
        }

        public void BulkUpdate(List<Setting> items)
        {
            using (dbc)
            {
                dbc.Connection.Open();                
                var txn = dbc.Connection.BeginTransaction();
                dbc.Transaction = txn;

                try
                {
                    foreach (Setting item in items)
                    {
                        item.LastModifiedBy = _SessionUsr.ID;
                        item.LastModifiedDate = DateTime.Now;

                        Update(item);
                    }
                    dbc.SubmitChanges();//Make a FINAL submit instead of periodic updates
                    txn.Commit();//Commit
                }
                #region  Rollback if error
                catch (Exception ex)
                {
                    txn.Rollback();
                    throw ex;
                }
                finally
                {
                    if (dbc.Transaction != null)
                        dbc.Transaction.Dispose();
                    dbc.Transaction = null;
                }
                #endregion
            }
        }

        #endregion
    }
}