using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Data.Linq.SqlClient;
using POT.DAL;
using HSG.Helper;
using Webdiyer.WebControls.Mvc;

namespace POT.Services
{
    public class StatusHistoryService : _ServiceBase
    {
        #region Variables & Constructor
        public StatusHistoryService() : base() { ;}
        public StatusHistoryService(POTmodel dbcExisting) : base(dbcExisting) { ;}
        #endregion

        #region Search / Fetch
        public List<vw_StatusHistory_Usr> FetchAll(int POID)
        {
            // Fetch all status history records for a PO
            IQueryable<vw_StatusHistory_Usr> vw = from vw_s in dbc.vw_StatusHistory_Usrs where vw_s.POID == POID orderby vw_s.LastModifiedDate select vw_s;

            List<vw_StatusHistory_Usr> records = vw.ToList();

            if (records == null || records.Count == 0)
                return new List<vw_StatusHistory_Usr>();
            else
                return records;
        }
        #endregion

        #region Add / Edit / Delete
        public void Add(POStatusHistory sObj, bool doSubmitChanges)
        {// Add status history record

            //Set last modified fields
            sObj.LastModifiedBy = _SessionUsr.ID;
            sObj.LastModifiedDate = DateTime.Now;

            dbc.POStatusHistories.InsertOnSubmit(sObj);

            if (doSubmitChanges) dbc.SubmitChanges();
        }

        public bool UpdatePOStatus(int POID1, int OldStatusID, int NewStatusID)
        {
            //int AssignTo = Defaults.Integer;
            if (POID1 <= Defaults.Integer || NewStatusID == Defaults.Integer || OldStatusID == NewStatusID)
                return false;

            else // Update
            {
                POHeader pObj = (from p in dbc.POHeaders where p.ID == POID1 select p).SingleOrDefault<POHeader>();
                if (pObj.ID <= Defaults.Integer) return false;
                pObj.OrderStatusID = NewStatusID; pObj.LastModifiedBy = _SessionUsr.ID; pObj.LastModifiedDate = DateTime.Now;

                Add(new POStatusHistory()
                {
                    POID = POID1,
                    NewStatusID = NewStatusID,
                    OldStatusID = OldStatusID
                }, true);
            }

            return true;
        }
        #endregion
    }
}