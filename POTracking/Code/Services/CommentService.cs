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
    public class CommentService : _ServiceBase
    {
        #region Variables & Constructor
        
        public readonly POComment newObj = new POComment() { ID = Defaults.Integer };
        
        public CommentService() : base() {;}
        public CommentService(POTmodel dbcExisting) : base(dbcExisting) { ;}

        #endregion

        #region Search / Fetch / Mail

        public List<POComment> Search(int poID, int? userID)
        {
            //using (dbc)//HT: DON'T coz we're sending IQueryable
            IQueryable<POComment> cQuery = from c in dbc.POComments
                                         join u in dbc.Users on new { UserID = c.UserID } equals new { UserID = u.ID } into u_join
                                         from u in u_join.DefaultIfEmpty()
                                         where c.POID == poID
                                         orderby c.PostedOn descending
                                         select Transform(c,u.Email, c.POID);

            //Append WHERE clause if applicable
            //if ((userID ?? 0) > 0) cQuery = cQuery.Where(o => o.UserID == userID.Value);

            return cQuery.ToList<POComment>();
        }

        POComment Transform(POComment c, string commentBy, int poID)
        {
            return c.Set(c1 =>
            {
                c1.CommentBy = commentBy; c1.PostedOn = new DateTime(c1.PostedOn.Year, c1.PostedOn.Month, c1.PostedOn.Day);
                /* IMP: NOTE - The following is NOT needed because we've set the Association Access to Internal in the dbml
                 c1.PO = null; c1.POID1 = poID; */
            });
        }
                
        public static bool SendEmail(int POID, int AssignTo, string PONumber, POComment CommentObj, ref string Err)
        {
            bool isSelfNotification = (AssignTo == _SessionUsr.ID);
            bool sendMail = (POID > Defaults.Integer && !isSelfNotification);// No need to send mail if its current user
            try
            {
                #region Check and send email
                if (sendMail)
                {// No need to send mail if its current user
                    string UserEmail = new UserService().GetUserEmailByID(AssignTo);
                    MailManager.AssignToMail(PONumber, CommentObj.Comment1, POID, UserEmail, (_SessionUsr.UserName), true);
                }
                #endregion
            }
            catch (Exception ex) { sendMail = false; Err = ex.Message + "<br/>" +
                (ex.InnerException??new Exception()).Message; }

            return sendMail;
        }

        #endregion

        #region Add / Edit / Delete & Bulk

        public int Add(POComment commentObj, bool doSubmit)
        {
            //triple ensure that the latest POComment.PostedOn date is NOT null
            //commentObj.PostedOn = DateTime.Now;
            //Set lastmodified fields
            commentObj.LastModifiedBy = _SessionUsr.ID;
            commentObj.LastModifiedDate = DateTime.Now;

            dbc.POComments.InsertOnSubmit(commentObj);
            if (doSubmit) dbc.SubmitChanges();

            return commentObj.ID; // Return the 'newly inserted id'
        }
                
        public int AddEdit(POComment commentObj, bool doSubmit)
        {
            if (commentObj.ID <= Defaults.Integer) // Insert
                return Add(commentObj, doSubmit);

            else // Update
            {
                //Set lastmodified fields
                commentObj.LastModifiedBy = _SessionUsr.ID;
                commentObj.LastModifiedDate = DateTime.Now;

                dbc.POComments.Attach(commentObj);//attach the object as modified
                dbc.Refresh(System.Data.Linq.RefreshMode.KeepCurrentValues, commentObj);//Optimistic-concurrency (simplest solution)
                if (doSubmit) dbc.SubmitChanges();
            }

            return commentObj.ID;
        }
                
        public void Delete(POComment commentObj, bool doSubmit)
        {
            dbc.POComments.DeleteOnSubmit(dbc.POComments.Single(c => c.ID == commentObj.ID && c.POID== commentObj.POID));
            if (doSubmit) dbc.SubmitChanges();
        }
        
        public void BulkAddEditDel(List<POComment> records, int CliamID, bool doSubmit)
        {
            #region NOTE
            /* Perform Bulk Add, Edit & Del based on Object properties set in VIEW
             MEANT ONLY FOR ASYNC BULK OPERATIONS
             Handle transaction, error and final commit in Callee 
                        
            //using{dbc}, try-catch and transaction must be handled in callee function
            //Also handle the final commit as follows:
            //dbc.SubmitChanges();//Make a FINAL submit instead of periodic updates
            //txn.Commit();//Commit
            */
            #endregion

            foreach (POComment item in records)
            {
                #region Perform Db operations
                item.POID = CliamID; //Required when adding new PO
                item.LastModifiedBy = _SessionUsr.ID;
                item.LastModifiedDate = DateTime.Now;
                item.PostedOn = DateTime.Now;// double ensure dates are not null !

                if (item._Deleted)
                    Delete(item, false);
                else if (item._Edited)//Make sure Delete is FIRST
                    AddEdit(item, false);
                else if (item._Added)
                    Add(item, false);
                #endregion
            }
            if (doSubmit) dbc.SubmitChanges();//Make a FINAL submit instead of periodic updates
            //txn.Commit();//Commit - Handled in parent caller routine
        }

        #endregion
    }
}