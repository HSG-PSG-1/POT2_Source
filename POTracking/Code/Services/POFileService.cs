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
    public class POFileService : _ServiceBase
    {
        #region Variables & Constructor
        
        public readonly POFile newObj = new POFile() { ID = Defaults.Integer };

        public POFileService() : base() {;}
        public POFileService(POTmodel dbcExisting) : base(dbcExisting) { ;}
        
        #endregion

        #region Search / Fetch

        public List<POFile> Search(int poID, int? userID)
        {
            //using (dbc)//HT: DON'T coz we're sending IQueryable
            IQueryable<POFile> cQuery = from f in dbc.POFiles                                            
                                            
                                            #region LEFT OUTER JOINs
                                                
                                                //LEFT OUTER JOIN For User
                                            join u in dbc.Users on new { UserID = f.UserID } equals
                                            new { UserID = u.ID } into u_join
                                            from u in u_join.DefaultIfEmpty()

                                            //LEFT OUTER JOIN For User
                                            join t in dbc.MasterFileTypes on
                                         new { TypID = f.FileType.Value } equals new { TypID = t.ID } into t_join
                                            from t in t_join.DefaultIfEmpty()
                                                
                                                #endregion

                                            where f.POID == poID
                                            orderby f.UploadDate descending
                                            select Transform(f, u.Email, t.Code, f.POID);

            return cQuery.ToList<POFile>();
        }

        POFile Transform(POFile f, string fileHeaderBy, string fileTypeTitle, int poID)
        {
            /*also set .PO = null to avoid issues during session serialization but persist POID as POID1 */
            return f.Set(f1 =>
            {
                f1.UploadedBy = fileHeaderBy; f1.FileTypeTitle = fileTypeTitle; f1.POGUID = f1.POID.ToString();
                f1.UploadDate = new DateTime(f1.UploadDate.Year, f1.UploadDate.Month, f1.UploadDate.Day);
                /*f1.PO = null; f1.POID1 = poID;
                 NOT needed because we've set the Association Access to Internal in the dbml*/});
        }
                
        public POFile GetPOFileById(int id)
        {
            using (dbc)
            {
                POFile cmt = (from f in dbc.POFiles where f.ID == id select f).SingleOrDefault<POFile>();                
                return cmt;
            }
        }

        #endregion
                
        #region Add / Edit / Delete & Bulk

        public int Add(POFile fileHeaderObj, bool doSubmit)
        {
            //Set lastmodified fields
            fileHeaderObj.LastModifiedBy = _SessionUsr.ID;
            fileHeaderObj.LastModifiedDate = DateTime.Now;
            
            dbc.POFiles.InsertOnSubmit(fileHeaderObj);
            if (doSubmit) dbc.SubmitChanges();

            return fileHeaderObj.ID; // Return the 'newly inserted id'
        }
                
        public int AddEdit(POFile fileHeaderObj, bool doSubmit)
        {
            fileHeaderObj.UploadDate = Defaults.getValidDate(fileHeaderObj.UploadDate); // special case to ensure valid SQLDate
            if (fileHeaderObj.ID <= Defaults.Integer) // Insert
                return Add(fileHeaderObj, doSubmit);

            else
            {
                #region Update
                //Set lastmodified fields
                fileHeaderObj.LastModifiedBy = _SessionUsr.ID;                
                fileHeaderObj.LastModifiedDate = DateTime.Now;
                
                dbc.POFiles.Attach(fileHeaderObj);//attach the object as modified
                dbc.Refresh(System.Data.Linq.RefreshMode.KeepCurrentValues, fileHeaderObj);//Optimistic-concurrency (simplest solution)
                #endregion

                if (doSubmit) //Make a FINAL submit instead of periodic updates
                   dbc.SubmitChanges();
            }

            return fileHeaderObj.ID;
        }
                
        public void Delete(POFile fileHeaderObj, bool doSubmit)
        {
            dbc.POFiles.DeleteOnSubmit(dbc.POFiles.Single(f => f.ID == fileHeaderObj.ID));
            if (doSubmit) dbc.SubmitChanges();
        }
        
        public void BulkAddEditDel(List<POFile> records, POHeader poObj, bool doSubmit, POTmodel dbcContext)
        {
            //OLD: if (poID <= Defaults.Integer) return; //Can't move forward if its a new PO entry
            #region NOTE
            // Perform Bulk Add, Edit & Del based on Object properties set in VIEW
            // MEANT ONLY FOR ASYNC BULK OPERATIONS
            // Handle transaction, error and final commit in Caller
            #endregion

            //using{dbc}, try-catch and transaction must be handled in callee function
            foreach (POFile item in records)
            {
                #region Perform Db operations
                item.POID = poObj.ID;
                item.LastModifiedBy = _SessionUsr.ID;
                item.LastModifiedDate = DateTime.Now;
                item.UploadDate = DateTime.Now; // double ensure dates are not null !

                //Special case handling for IE with KO - null becomes "null"
                if (item.Comment == "null") item.Comment = "";

                if (item._Deleted)
                    Delete(item, false);
                else if (item._Edited)//Make sure Delete is LAST
                    AddEdit(item, false);
                else if (item._Added)
                    Add(item, false);
                #endregion

                #region Log Activity (finally when the uploaded file data is entered in the DB
                if (item._Added || item._Edited)
                {
                    //Special case: Call the econd overload which has "doSubmit" parameter
                    new ActivityLogService(ActivityLogService.Activity.POFileUpload, dbcContext).Add(
                    new ActivityHistory() { FileName = item.FileName, POID = poObj.ID, PONumber = poObj.PONumber.ToString() },
                    doSubmit);
                }
                #endregion
            }
            if (doSubmit) dbc.SubmitChanges();//Make a FINAL submit instead of periodic updates
            //Move header files
            /*if (isNewPO)
                FileIO.MoveFilesFolderNewPOOrItem(poObj.ID, poObj.POGUID);
            else*/
            ProcessFiles(records, poObj.ID, poObj.POGUID);
        }

        #endregion

        #region Extra functions

        void ProcessFiles(List<POFile> records, int poID, string POGUID)
        {
            if (records == null || records.Count < 1) return;

            foreach (POFile item in records)
                if (item._Deleted)//Delete will always be for existing not Async (so use POID)
                    FileIO.DeletePOFile(poID, "", item.FileName);

            if(records.Count > 0) //finally copy all the files from H_Temp to H
                FileIO.StripGUIDFromPOFileName(poID, POGUID);
        }

        #endregion
        
    }
}
