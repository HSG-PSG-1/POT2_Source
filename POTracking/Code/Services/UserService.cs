using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;
using System.Linq.Dynamic;
using System.Data.Linq.SqlClient;
using POT.DAL;
using HSG.Helper;
using Webdiyer.WebControls.Mvc;

namespace POT.Services
{
    public class UserService : _ServiceBase
    {
        #region Variables
        
        public readonly vw_Users_Role_Org emptyView = 
            new vw_Users_Role_Org() { ID = Defaults.Integer };
        public readonly Users emptyUsr = new Users() { ID = Defaults.Integer};//Make sure UserLocations is reset
        public const string sortOn = "UserName ASC", sortOn1 = "UserName ASC"; // Default for secondary sort

        public const string orgTypeJS = "if(user.OrgTypeId() == 1)user.OrgType(\"Internal\"); else if(user.OrgTypeId() == 2)user.OrgType(\"Vendor\");";

        #endregion

        #region Login related

        public vw_Users_Role_Org Login(string email, string password)
        {
            vw_Users_Role_Org usr = emptyView;
            
            //using (dbc) HT: Coz we're using the data
            {
                usr = dbc.vw_Users_Role_Orgs.SingleOrDefault(u => u.Email.ToUpper() == email && u.Password == password);
                    /*(from u in dbc.vw_Users_Role_Orgs
                       where u.Email.ToUpper() == email && u.Password == password
                       select u).SingleOrDefault();*/
            }
            return usr ?? null; // emptyView will not compare correctly
        }

        public UserRole GetRoleRights(int RoleID)
        {
            UserRole roleRights = new UserRole();
            using (dbc) 
            {
                roleRights = dbc.UserRoles.SingleOrDefault(r => r.ID == RoleID);
                //(from r in dbc.UserRoles where r.ID == RoleID select r).SingleOrDefault();
                //roleRights.OrgType = null;
                //roleRights.Users = null;//to avoid issues with XMLSerializer
            }
            return roleRights;
        }

        #endregion

        #region Search / Fetch

        public PagedList<vw_Users_Role_Org> Search(string orderBy, int? pgIndex, int pageSize, vw_Users_Role_Org usr)
        {
            orderBy = string.IsNullOrEmpty(orderBy) ? sortOn : orderBy;

            using (dbc)
            {
                IQueryable<vw_Users_Role_Org> userQuery = (from vw_u in dbc.vw_Users_Role_Orgs select vw_u);
                //Get filters - if any
                userQuery = PrepareQuery(userQuery, usr);
                // Apply Sorting, Pagination and return PagedList
                return userQuery.OrderBy(orderBy).ToPagedList(pgIndex ?? 1, pageSize);

                /* Apply pagination and return
                return userQuery.Skip(startRow).Take(pageSize).ToList<vw_User_Org_UserRole>();
                */
            }
        }

        public List<vw_Users_Role_Org> SearchKO(vw_Users_Role_Org usr)//string orderBy, int? pgIndex, int pageSize, vw_Users_Role_Org usr, bool fetchAll)
        {
            //orderBy = string.IsNullOrEmpty(orderBy) ? sortOn : orderBy;

            using (dbc)
            {
                IQueryable<vw_Users_Role_Org> userQuery = (from vw_u in dbc.vw_Users_Role_Orgs select vw_u);
                //Get filters - if any
                userQuery = PrepareQuery(userQuery, usr);
                // Apply Sorting
                userQuery = userQuery.OrderBy(sortOn);//orderBy);
                // Apply pagination and return
                //if (fetchAll)
                    return userQuery.ToList<vw_Users_Role_Org>();
                //else
                //    return userQuery.Skip(pgIndex.Value).Take(pageSize).ToList<vw_Users_Role_Org>();
            }
        }

        public static IQueryable<vw_Users_Role_Org> PrepareQuery(IQueryable<vw_Users_Role_Org> userQuery, vw_Users_Role_Org usr)
        {
            #region Append WHERE clause if applicable

            if (!string.IsNullOrEmpty(usr.UserName)) userQuery = userQuery.Where(o => SqlMethods.Like(o.UserName.ToUpper(), usr.UserName.ToUpper()));
            //userQuery.Where(o => o.Name.ToUpper().Contains(userName));
            if (usr.RoleID > 0) userQuery = userQuery.Where(o => o.RoleID == usr.RoleID);
            else if (!string.IsNullOrEmpty(usr.RoleName))
                userQuery = userQuery.Where(o => SqlMethods.Like(o.RoleName.ToLower(), usr.RoleName.ToLower()));

            if (!string.IsNullOrEmpty(usr.Email))
                userQuery = userQuery.Where(o => SqlMethods.Like(o.Email.ToUpper(), usr.Email.ToUpper()));
            
            if (usr.OrgID > 0) userQuery = userQuery.Where(o => o.OrgID == usr.OrgID);
            if (!string.IsNullOrEmpty(usr.OrgName))
                userQuery = userQuery.Where(o => SqlMethods.Like(o.OrgName.ToUpper(), usr.OrgName.ToUpper()));
            
            #endregion

            return userQuery;
        }

        public Users GetUserById(int id, int OrgID)
        {
            #region Kept for Ref (Tried to load with Users.UserLocations)
            /*http://stackoverflow.com/questions/32433/select-from-multiple-table-using-linq
            DataLoadOptions dlo = new DataLoadOptions();
            //dlo.LoadWith<UserLocation>(v => v.CustomerLocation);
            //HT: SPECIAL CASE: Have added an association between vw_Users_Role_Org & UserLocation (also set PK in vw)
            //Ref: http://forums.asp.net/t/1514585.aspx
            dlo.LoadWith<vw_Users_Role_Org>(v => v.UserLocations);

            dbc.DeferredLoadingEnabled = false;
            dbc.LoadOptions = dlo; */
            #endregion

            using (dbc)
            {
                vw_Users_Role_Org vw_u = (from vw in dbc.vw_Users_Role_Orgs
                                          where vw.ID == id
                                          select vw).SingleOrDefault<vw_Users_Role_Org>();

                //emptyUsr.UserLocations = new EntitySet<UserLocation>();//To make sure it DOESN'T come from cache
                Users usr = emptyUsr;
                if (vw_u != null)
                    usr = new Users
                     {
                         ID = vw_u.ID,
                         Name = vw_u.UserName,                         
                         RoleID = vw_u.RoleID,
                         OrgID = vw_u.OrgID,
                         OrgType = vw_u.OrgTypeId ?? -1,
                         Email = vw_u.Email,
                         LastModifiedBy = vw_u.LastModifiedBy,
                         Password = vw_u.Password,
                         LastModifiedDate = vw_u.LastModifiedDate,
                         /* Set other special properties */
                         EmailOLD = vw_u.Email,
                         LastModifiedByVal = vw_u.LastModifiedByName,
                         OrgName = vw_u.OrgName,
                         OrgTypeName = vw_u.OrgType
                     };
                
                usr.OriOrgId = usr.OrgID;//Required ahead
                return usr;
            }
        }

        public string GetUserPWDByEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return string.Empty;
            return (from u in dbc.Users
                    where u.Email.ToLower() == email.ToLower()
                    select u.Password).SingleOrDefault<string>() ?? "";
        }

        public string GetUserEmailByID(int ID)
        {
            if (ID <= Defaults.Integer) return string.Empty;
            return (from u in dbc.Users
                    where u.ID == ID
                    select u.Email).SingleOrDefault<string>() ?? "";
        }
        
        #endregion

        #region Add / Edit / Delete

        public int Add(Users userObj)//, string LinkedLoc, string UnlinkedLoc)
        {
            //Set lastmodified fields
            userObj.LastModifiedBy = _SessionUsr.ID;
            userObj.LastModifiedDate = DateTime.Now;

            dbc.Users.InsertOnSubmit(userObj);
            dbc.SubmitChanges();

            return userObj.ID; // Return the 'newly inserted id'
        }

        public static Users GetObjFromVW(vw_Users_Role_Org usr)
        {
            return new Users() { ID = usr.ID, RoleID = usr.RoleID, OrgID = usr.OrgID, Name = usr.UserName, Email = usr.Email, 
                Password = usr.Password, LastModifiedBy = usr.LastModifiedBy, LastModifiedDate = DateTime.Now
             };
        }

        public int AddEdit(Users userObj)
        {
            int userID = userObj.ID;

            if (userID <= Defaults.Integer) // Insert
                userID = Add(userObj);

            else
            {
                #region Update
                
                //Set lastmodified fields
                userObj.LastModifiedBy = _SessionUsr.ID; userObj.LastModifiedByVal = _SessionUsr.Email;// UserName;
                userObj.LastModifiedDate = DateTime.Now;

                dbc.Users.Attach(userObj);//attach the object as modified
                dbc.Refresh(System.Data.Linq.RefreshMode.KeepCurrentValues, userObj);

                dbc.SubmitChanges();

                #endregion
            }

            #region If the user is current User's user then update the session for userAttributes
            
            if (_SessionUsr.ID == userObj.ID)
            {
                _SessionUsr.setUserSession(Login(userObj.Email, userObj.Password)); // HT: Probably no other way
                _Session.RoleRights = GetRoleRights(userObj.RoleID);
            }

            #endregion

            return userObj.ID;
        }

        public bool Delete(Users userObj)
        {
            dbc.Users.DeleteOnSubmit(dbc.Users.Single(c => c.ID == userObj.ID));
            dbc.SubmitChanges();
            // Following code is not working - they say 'optimistic concurrency' is not too gud in L2S
            //dbc.users.Attach(userObj);//attach the object to be deleted
            //dbc.users.DeleteOnSubmit(userObj);//delete the object
            //dbc.SubmitChanges();
            return true;
        }

        #endregion

        #region Extra functions

        public override bool IsReferred(Object oObj)
        {
            Users uObj = (Users)oObj;
            //Check if tis user has any POs or if he's assigned any pos
            bool referred = (dbc.POHeaders.Where(p => p.AssignTo == uObj.ID).Count() > 0);

            referred = referred || (dbc.POComments.Where(c => c.UserID == uObj.ID).Count() > 0);
            referred = referred || (dbc.POFiles.Where(f => f.UserID == uObj.ID).Count() > 0);            
            
            // For future usage - not really because tis just archvied history!
            //if (!referred) referred = (dbc.ActivityHistories.Where(u => u.UserID == uObj.ID).Count() > 0);
            
            return referred;
        }

        public bool IsUserEmailDuplicate(string userEmail)
        {
            return (UserEmailCount(userEmail) > 0);
        }

        public int UserEmailCount(string userEmail)
        {
            return dbc.Users.Where(u => u.Email.ToUpper() == userEmail.ToUpper()).Count();
        }

        #endregion        
    }
}
