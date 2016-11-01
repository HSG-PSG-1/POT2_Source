using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Data.Linq.SqlClient;
using POT.Models;
using POT.DAL;
using HSG.Helper;
using Webdiyer.WebControls.Mvc;

namespace POT.Services
{
    public class POService : _ServiceBase
    {
        #region Variables & Constructor
        
        public readonly vw_POHeader emptyView = new vw_POHeader() {
            ID = Defaults.Integer, PODate = DateTime.Now, VendorID = _SessionUsr.OrgID, VendorName = _SessionUsr.OrgName };
        public readonly POHeader emptyPO = new POHeader()
        {
            ID = Defaults.Integer, 
            aComments= new List<POComment>(), aFiles=new List<POFile>(), aLines  = new List<PODetail>() }; // Add empty files, comments and lines to ensure not null handling

        public POService() : base() { ;}
        public POService(POTmodel dbcExisting) : base(dbcExisting) {;}
        
        #endregion

        #region Search / Fetch

        public vw_POHeader GetPOHeaderById(int id)
        {
            using (dbc)
            {
                vw_POHeader vw_c = (from vw in dbc.vw_POHeaders
                                  where vw.ID == id
                                    select vw).SingleOrDefault<vw_POHeader>();

                if (vw_c != null)
                {
                    vw_c.OrderStatusIDold = vw_c.OrderStatusID.Value;
                    vw_c.AssignToOld = (vw_c.AssignTo??-1);
                }
                else
                    vw_c = emptyView;

                return vw_c;
            }
        }

        public POHeader GetPOInfoById(int id)
        {
            //using (dbc) - HT: DON'T otherwise error : DataContext accessed after Dispose.
            {
                POHeader po = (from p in dbc.POHeaders
                                    where p.ID == id
                                    select p).SingleOrDefault<POHeader>();

                if (po != null)
                {
                    po.OrderStatusIDold = po.OrderStatusID.Value;
                    po.AssignToIDold = po.AssignTo??-1;
                }
                else
                    po = emptyPO;

                #region Nullify entities taht can cause serialization issue
                /*
                po.MasterCarrier = null;
                po.MasterContainerType = null;
                po.MasterOrderType = null;
                po.MasterShipVia = null;
                //po.MasterStatus = null;
                po.MasterTerm = null;
                po.MasterWarehouse = null;
                
                po.POComments  = null;
                po.PODetails = null;
                po.POFiles = null;
                po.POStatusHistories = null;

                po.Users = null;
                */
                #endregion

                return po;
            }
        }

        public vw_POHeader GetPOByIdForPrint(int poId,ref List<POComment> comments,
            ref List<POFile> filesH, ref List<vw_POLine> items, bool loadComments)
        {
            using (dbc)
            {
                vw_POHeader vw_c = (from vw in dbc.vw_POHeaders
                                                 where vw.ID == poId
                                    select vw).SingleOrDefault<vw_POHeader>();

                if (vw_c != null)
                    vw_c.OrderStatusIDold = vw_c.OrderStatusID.Value;
                else
                    vw_c = emptyView;

                // Load comments
                //if (loadComments) comments = new CommentService().Search(poId, null);//Only for non-customers
                // Load Files
                //filesH = new POFileService().Search(poId,null);
                items = new DetailService().Search(poId, null);

                return vw_c;
            }
        }

        #endregion

        #region Add / Edit / Delete / Archive / Add Default

        public int Edit(POHeader poObj, int StatusIDold, bool doSubmit)
        {
            //Set lastmodified fields
            poObj.LastModifiedBy = _SessionUsr.ID;
            poObj.LastModifiedDate = DateTime.Now;

            dbc.POHeaders.Attach(poObj);//attach the object as modified
            dbc.Refresh(System.Data.Linq.RefreshMode.KeepCurrentValues, poObj);//Optimistic-concurrency (simplest solution)

            #region If the Status has been changed then make entry in StatusHistory
            if (poObj.OrderStatusID != StatusIDold)
                new StatusHistoryService(dbc).Add(new POStatusHistory()
                {
                    POID = poObj.ID,
                    NewStatusID = poObj.OrderStatusID.Value,
                    OldStatusID = StatusIDold                    
                }, false);
            #endregion

            if (doSubmit)    dbc.SubmitChanges();
            // Set PO #
            //poObj.PONumber = poObj.ID;

            return poObj.ID;
        }

        public void Delete(POHeader poObj)
        {
            //HT: IMP: SP way of checking if an FK ref exists: http://stackoverflow.com/questions/5077423/sql-server-check-if-child-rows-exist
            dbc.POHeaders.DeleteOnSubmit(dbc.POHeaders.Single(c => c.ID == poObj.ID));
            //Delete PO Activities ???
            dbc.SubmitChanges();
        }

        #endregion

        #region Extra functions

        public bool AssignPO(int poId, int AssignTo)
        {
            if (poId <= Defaults.Integer || AssignTo == Defaults.Integer)
                return false;

            else
            {
                #region Update
                POHeader poObj = (from c in dbc.POHeaders where c.ID == poId select c).SingleOrDefault<POHeader>();

                if (poObj.ID <= Defaults.Integer) return false;

                poObj.AssignTo = AssignTo;
                //Set lastmodified fields
                poObj.LastModifiedBy = _SessionUsr.ID;
                poObj.LastModifiedDate = DateTime.Now;

                //dbc.POHeaders.Attach(cObj);//attach the object as modified NOT needed as we just fetched it and dbc is ALIVE
                dbc.SubmitChanges();
                #endregion
            }

            return true;
        }

        internal bool IsPOAccessible(int POId, int UserId, int OrgId)
        {
            if (_Session.IsAsiaVendor) // Special Case for Asia Vendor
                return (dbc.POHeaders.Where(c =>
                    (c.VendorID == Config.VendorIDDeestone || c.VendorID == Config.VendorIDDeestone || c.VendorID == Config.VendorIDSvizz || c.VendorID == Config.VendorIDSiamtruck)
                    && c.ID == POId).Count() > 0);
            else if (_Session.IsAsiaOperations) // Special Case for Asia Operations
                return (dbc.POHeaders.Where(c =>
                    (c.VendorID != Config.VendorIDDeestone && c.VendorID != Config.VendorIDDeestone && c.VendorID != Config.VendorIDSvizz && c.VendorID != Config.VendorIDSiamtruck)
                    && c.ID == POId).Count() > 0);
            // HT: Make sure this is AFTER AsiaOperations
            else if (_Session.IsOrgInternal) // It can be an OrgType = Internal (but NONE of the above)
                return true; // Because Org will be "Admin"
            else // For separate Vendor!
                return (dbc.POHeaders.Where(c => c.VendorID == OrgId && c.ID == POId).Count() > 0);
        }

        #endregion
    
        #region Add / Edit / Delete & Bulk
        
        public string AsyncBulkAddEditDelKO(POHeader poObj, int StatusIDold, 
            /*IEnumerable<PODetail> items,*/ IEnumerable<POComment> comments, IEnumerable<POFile> files)
        {
            //POHeader poObj = POService.GetPOObjFromVW(vwObj);
            using (dbc)//Make sure this dbc is passed and persisted
            {
                bool isNewPO = (poObj.ID <= Defaults.Integer);
                bool doSubmit = true;
                string Progress = "";

                #region Set Transaction
                
                dbc.Connection.Open();
                //System.Data.Common.DbTransaction 
                var txn = dbc.Connection.BeginTransaction();
                dbc.Transaction = txn;
                //ExecuteReader requires the command to have a transaction when the connection assigned to the
                //command is in a pending local transaction. The Transaction property of the command has not been initialized.
                #endregion

                #region Extra - set CommentExist & FileExist
                
                bool CommentExist =(comments != null && comments.Count() > 0);
                if(CommentExist)
                    CommentExist = comments.Where(c => !c._Deleted).ToList().Count > 0;
                
                bool FileExist = (files != null && files.Count() > 0);
                if (FileExist)
                    FileExist = files.Where(f => !f._Deleted).ToList().Count > 0;

                poObj.CommentsExist = CommentExist;
                poObj.FilesHExist = FileExist;

                #endregion

                try
                {
                    Progress = 
                        "PO (" + poObj.ID + ", " + poObj.POGUID + ", " + poObj.PODate.ToString() + ")";
                    //Update po
                    new POService(dbc).Edit(poObj, StatusIDold, true);//doSubmit must be TRUE
                    //IMP: Note: The above addedit will return updated POObj which will have PO Id

                    Progress = "Comments";//Process comments
                    if(comments != null && comments.Count() > 0)
                        new CommentService(dbc).BulkAddEditDel(comments.ToList(), poObj.ID, doSubmit);
                    Progress = "Files";//Process files (header) and files
                    if (files != null && files.Count() > 0)
                        new POFileService(dbc).BulkAddEditDel(files.ToList(), poObj, doSubmit, dbc);
                    Progress = "POdetails";//Process lines (and internally also process files(details)

                    // No need to cleanup the GUID folder or similar because now we rename
                    /*NOTE: For Async the Details files will have to be handled internally in the above function
                    if (poObj.ID.ToString() != poObj.POGUID && !string.IsNullOrEmpty(poObj.POGUID))//ensure there's NO confusion
                        FileIO.EmptyDirectory(System.IO.Path.Combine(Config.UploadPath, poObj.POGUID.ToString()));
                    */
                    if (!doSubmit) dbc.SubmitChanges();//Make a FINAL submit instead of periodic updates
                    txn.Commit();//Commit
                }
                #region  Rollback if error
                catch (Exception ex)
                {
                    txn.Rollback();
                    Exception exMore = new Exception(ex.Message + " After " + Progress);
                    // Make sure the temp files are also deleted
                    _Session.ResetPOInSessionAndEmptyTempUpload(poObj.ID, poObj.POGUID);

                    throw exMore;
                }
                finally
                {
                    if (dbc.Transaction != null)
                        dbc.Transaction.Dispose();
                    dbc.Transaction = null;
                }
                #endregion
            }
            
            return poObj.PONumber;//Return updated poobj
        }

        #endregion
    }
}
