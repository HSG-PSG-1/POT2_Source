using System;
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
    public class MasterService : _ServiceBase
    {
        #region Variables & Constructor
        public const string sortOn = "SortOrder";
        private const string selectSQL =
            "SELECT m.*, u.[Email] as [Name] FROM Master{0} as m LEFT OUTER JOIN Users as u ON m.LastModifiedBy = u.ID ORDER BY {1} sortOrder";
        // {1} is for special sorting required by MasterDefect
        private const string deleteSQL = "DELETE FROM Master{0} WHERE ID = {1}";

        //[TypeConverter(typeof(EnumToStringUsingDescription))]
        //Pending R&D for better Enum toString: http://stackoverflow.com/questions/796607/how-do-i-override-tostring-in-c-enums
        [Serializable]
        public enum Table : int
        {
            Carrier,
            Container_Type,
            File_Type,
            Status
        }

        Table masterType;

        public MasterService(Table? _masterType)
        {
            if (_masterType != null) masterType = _masterType.Value;
        }

        #endregion

        #region Search / Fetch

        public List<Master> FetchAll()
        {
            // Fetch All for a particular Master table (this.masterType)
            //http://marlongrech.wordpress.com/2008/03/01/how-to-get-data-dynamically-from-you-linq-to-sql-data-context/

            string masterDefectSort = "";
            using (dbc)
            {
                IEnumerable<Master> mQuery = dbc.ExecuteQuery<Master>(
                    string.Format(selectSQL, masterType.ToString().Replace("_", ""), masterDefectSort));
                //Be careful with the table name !!!

                List<Master> result = mQuery.ToList<Master>();
                //CodeOLD will be populated because of [Column(Name = "Code")]

                #region Add an empty record for Add new
                result.Add(new Master()
                {
                    _Added = false,
                    ID = -1,
                    Code = "[Code-1]",//Required otherwise it'll be considered as ModelState error !
                    SortOrder = result.Count + 1,//To make sure the js sort doesn't mess up like 0
                    LastModifiedBy = Defaults.Integer,//Make sure this is set to 0
                    LastModifiedByVal = _SessionUsr.Email,//UserName,
                    LastModifiedDate = DateTime.Now,
                    CanDelete = true
                });
                #endregion

                return result;
            }
        }

        public List<Master> FetchAllCached()
        {
            object cachedData = _SessionLookup.MasterData[masterType];
            if (cachedData == null || ((List<Master>)cachedData).Count < 1)
            {
                List<Master> data = FetchAll();
                _SessionLookup.MasterData[masterType] = data;
                return data;
            }
            else
                return ((List<Master>)cachedData);
        }

        #endregion

        #region Add / Edit / Delete & Bulk

        public void Add(Master mObj)
        {
            mObj.CanDelete = true;//double-make-sure that new record is not marked as undeletable
            object updateObj = GetTableObj(mObj, false);

            #region Insert On Submit
            #region Attach the object as modified
            switch (masterType)
            {
                case Table.Carrier: dbc.MasterCarriers.InsertOnSubmit((MasterCarrier)updateObj); break;
                case Table.File_Type: dbc.MasterFileTypes.InsertOnSubmit((MasterFileType)updateObj); break;
                case Table.Container_Type: dbc.MasterContainerTypes.InsertOnSubmit((MasterContainerType)updateObj); break;
                case Table.Status: dbc.MasterStatus.InsertOnSubmit((MasterStatus)updateObj); break;
                //dbc.MasterActivities.InsertOnSubmit(new MasterActivity(){ID=mObj.ID, Code=mObj.Code, SortOrder=mObj.SortOrder, 
                //LastModifiedBy=mObj.LastModifiedBy, LastModifiedDate=mObj.LastModifiedDate});
            }
            #endregion
            #endregion

            //dbc.SubmitChanges();
        }

        public void Update(Master mObj)
        {
            //Set lastmodified fields
            mObj.LastModifiedBy = _SessionUsr.ID;
            mObj.LastModifiedDate = DateTime.Now;

            if (mObj.ID <= Defaults.Integer) // Insert
                return;//HT:SPECIAL CASE: W've handled Add separately so we skip //AddMasterEntry(mObj);

            else // Update
            {
                object updateObj = GetTableObj(mObj, true);

                #region Attach the object as modified
                switch (masterType)
                {
                    case Table.Carrier: dbc.MasterCarriers.Attach((MasterCarrier)updateObj); break;
                    case Table.File_Type: dbc.MasterFileTypes.Attach((MasterFileType)updateObj); break;
                    case Table.Container_Type: dbc.MasterContainerTypes.Attach((MasterContainerType)updateObj); break;
                    case Table.Status: dbc.MasterStatus.Attach((MasterStatus)updateObj); break;
                    //case Table.Activity: dbc.MasterActivities.Attach((MasterActivity)updateObj); break;
                    //dbc.MasterActivities.Attach(new MasterActivity(){ID=mObj.ID, Code=mObj.Code, SortOrder=mObj.SortOrder, 
                    //LastModifiedBy=mObj.LastModifiedBy, LastModifiedDate=mObj.LastModifiedDate});
                }
                #endregion

                dbc.Refresh(System.Data.Linq.RefreshMode.KeepCurrentValues, updateObj);//Optimistic-concurrency (simplest solution)

                //dbc.SubmitChanges();
            }
        }

        public void Delete(Master mObj)
        {
            //HT: CAUTION: Make sure references are checked
            #region Attach the object as modified
            switch (masterType)
            {
                case Table.Carrier: dbc.MasterCarriers.DeleteOnSubmit(dbc.MasterCarriers.Single
                    (c => c.CanDelete && c.ID == mObj.ID)); break;
                case Table.File_Type: dbc.MasterFileTypes.DeleteOnSubmit(dbc.MasterFileTypes.Single
                    (c => c.CanDelete && c.ID == mObj.ID)); break;
                case Table.Container_Type: dbc.MasterContainerTypes.DeleteOnSubmit(dbc.MasterContainerTypes.Single
                (c => c.CanDelete && c.ID == mObj.ID)); break;
                case Table.Status: dbc.MasterStatus.DeleteOnSubmit(dbc.MasterStatus.Single
                                    (c => c.CanDelete && c.ID == mObj.ID)); break;
                //case Table.Activity: dbc.MasterActivities.DeleteOnSubmit(dbc.MasterActivities.Single
                //    (c =>c.CanDelete && c.ID == mObj.ID)); break;
            }
            #endregion

            //dbc.SubmitChanges();
        }

        public void BulkAddEditDel(List<Master> items)
        {
            // Cleanup newly added & deleted records
            items.RemoveAll(i => (i._Added || i.ID < Defaults.Integer) && i._Deleted); // Remove newly inserted records
            using (dbc)
            {
                dbc.Connection.Open();

                var txn = dbc.Connection.BeginTransaction();
                dbc.Transaction = txn;
                //Execution requires the command to have a transaction when the connection assigned to the
                //command is in a pending local transaction. The Transaction property of the command has not been initialized.

                try
                {
                    foreach (Master item in items)
                    {
                        #region Perform Db operations
                        item.LastModifiedBy = _SessionUsr.ID;
                        item.LastModifiedDate = DateTime.Now;

                        if (item._Added && !item._Deleted) // Because we're NOT removing the deleted items
                            Add(item);
                        else if (item._Deleted && item.CanDelete)//double check the can-delete flag
                            Delete(item);//Make sure Ref check brfore Delete is done
                        else if (item._Updated)//Make sure update is LAST
                            Update(item);
                        #endregion
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
                    //Invalidate cache entry
                    _SessionLookup.MasterData.Remove(masterType);
                }
                #endregion
            }
        }

        #endregion

        #region Extra functions

        /// <summary>
        /// Returns specific Master object wrapped as object as per - masterType
        /// </summary>
        /// <param name="mObj">Master object</param>
        /// <returns>Specific Master object wrapped as object</returns>
        public object GetTableObj(Master mObj, bool IsAdd)
        {
            switch (masterType)
            {
                case Table.Carrier:
                    return new MasterCarrier()
                    {
                        ID = mObj.ID,
                        Code = mObj.Code,
                        SortOrder = mObj.SortOrder,
                        Description = mObj.Description,
                        LastModifiedBy = mObj.LastModifiedBy,
                        LastModifiedDate = mObj.LastModifiedDate,
                        CanDelete = IsAdd || mObj.CanDelete // Can't allow for add!
                    };
                case Table.Container_Type:
                    return new MasterContainerType()
                    {
                        ID = mObj.ID,
                        Code = mObj.Code,
                        SortOrder = mObj.SortOrder,
                        Description = mObj.Description,
                        LastModifiedBy = mObj.LastModifiedBy,
                        LastModifiedDate = mObj.LastModifiedDate,
                        CanDelete = IsAdd || mObj.CanDelete
                    };
                case Table.File_Type:
                    return new MasterFileType()
                    {
                        ID = mObj.ID,
                        Code = mObj.Code,
                        SortOrder = mObj.SortOrder,
                        Description = mObj.Description,
                        LastModifiedBy = mObj.LastModifiedBy,
                        LastModifiedDate = mObj.LastModifiedDate,
                        CanDelete = IsAdd || mObj.CanDelete
                    };
                case Table.Status:
                    return new MasterStatus()
                    {
                        ID = mObj.ID,
                        Code = mObj.Code,
                        SortOrder = mObj.SortOrder,
                        Description = mObj.Description,
                        LastModifiedBy = mObj.LastModifiedBy,
                        LastModifiedDate = mObj.LastModifiedDate,
                        CanDelete = IsAdd || mObj.CanDelete
                    };
                default: return null;
            }
        }

        public static string formatTitle(string title)
        {
            return title.Replace("_", " ");
        }

        public bool IsCodeDuplicate(string code)
        {// Check if Title is Duplicate (NOTE - here we still don't know which master object has been invoked)

            //HT: Special case for Role
            if (!_Session.MasterTbl.HasValue) return new SecurityService().IsCodeDuplicate(code);

            switch (_Session.MasterTbl)//masterType - can't use it because its not available while validation
            {
                case Table.Carrier: return (dbc.MasterCarriers.Where(m => m.Code.ToUpper() == code.ToUpper()).Count() > 0);
                case Table.Container_Type: return (dbc.MasterContainerTypes.Where(m => m.Code.ToUpper() == code.ToUpper()).Count() > 0);
                case Table.File_Type: return (dbc.MasterFileTypes.Where(m => m.Code.ToUpper() == code.ToUpper()).Count() > 0);
                case Table.Status: return (dbc.MasterStatus.Where(m => m.Code.ToUpper() == code.ToUpper()).Count() > 0);
                default: return false;
            }
        }

        public override bool IsReferred(Object oObj)
        {//This can be expensive so make sure it is done only once (i.e. at present it is handled in controller)
            Master mObj = (Master)oObj;

            if (!mObj.CanDelete) return true;//If the 

            bool referred = false;
            switch (masterType)
            {
                case Table.Carrier:
                    referred = (dbc.POHeaders.Where(m => m.CarrierID == mObj.ID).Count() > 0);
                    return referred;
                case Table.Container_Type:
                    referred = (dbc.POHeaders.Where(m => m.ContainerTypeId == mObj.ID).Count() > 0);
                    return referred;
                case Table.File_Type:
                    return (dbc.POFiles.Where(m => m.FileType == mObj.ID).Count() > 0);
                case Table.Status:
                    #region Check Status ref in PO

                    referred = (dbc.POHeaders.Where(m => m.OrderStatusID == mObj.ID).Count() > 0);
                    if (!referred)//Check Status ref in StatusHistory
                        referred = (dbc.POStatusHistories.Where(m => m.NewStatusID == mObj.ID || m.OldStatusID == mObj.ID).Count() > 0);
                    return referred;

                    #endregion

                //For future
                //case Table.Activity://Check Activity ref in ActivityHistory
                //    return (dbc.ActivityHistories.Where(m => m.ActivityID == mObj.ID).Count() > 0);
            }
            return false;
        }

        #endregion
    }
}
