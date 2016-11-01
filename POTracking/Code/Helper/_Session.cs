using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using POT.DAL;
using POT.Models;
using POT.Services;
using System.Web.Security;

namespace HSG.Helper
{
    public class _Session
    {
        const string sep = ";";

        #region Object (class) containing session objects

        /* Obsolete - kept for future ref - public static UserSession Usr
        { // SO: 11955094 Go back to In proc session
            get
            {
                try
                {
                    string byteArrStr = HttpContext.Current.Session["UserObj"].ToString();
                    if (string.IsNullOrEmpty(byteArrStr))
                        return new UserService().emptyView;
                    return Serialization.Deserialize<vw_Users_Role_Org>(byteArrStr);
                }
                catch { return new UserService().emptyView; }
            }
            set {
                if (value == null) return;//Extra worst-case check
                //HttpContext.Current.Session["UserObj"] = Serialization.Serialize<vw_Users_Role_Org>(value);
                UserSession.setUserSession(value);
            }
        }*/

        public static UserRole RoleRights
        {
            get
            {
                UserRole rDataEmpty = new UserRole();
                try
                {
                    string byteArrStr = HttpContext.Current.Session["RoleRights"].ToString();
                    if (string.IsNullOrEmpty(byteArrStr))
                        return rDataEmpty;

                    return Serialization.Deserialize<UserRole>(byteArrStr);
                }
                catch { return rDataEmpty; }
            }
            set
            {
                if (value == null) return;//Extra worst-case check
                HttpContext.Current.Session["RoleRights"] = Serialization.Serialize<UserRole>(value);
            }
        }

        public static MasterService.Table? MasterTbl
        {
            get
            {
                if (HttpContext.Current.Session["MasterTbl"] == null) return null;
                else return _Enums.ParseEnum<MasterService.Table>(HttpContext.Current.Session["MasterTbl"]);
            }
            set { HttpContext.Current.Session["MasterTbl"] = value; }
        }

        public static Filters Search { get { return new Filters(); } }

        #endregion

        #region Security objects

        public static bool IsOrgInternal
        {
            get
            {
                try { return IsAdmin || (_SessionUsr.OrgTypeId == (int)OrgService.OrgType.Internal); }
                catch { return false; }
            }            
        }

        public static bool IsAdmin
        {
            get
            {
                try { return (_SessionUsr.RoleID == (int)SecurityService.Roles.Admin); }
                catch { return false; }
            }
        }

        /*public static bool IsVendor
        {
            get
            {
                try { return IsAdmin || (_SessionUsr.RoleID == (int)SecurityService.Roles.AsiaVendor); }
                catch { return false; }
            }
        }*/

        public static bool IsOnlyVendor
        {
            get
            {
                try { return (_SessionUsr.OrgTypeId == (int)OrgService.OrgType.Vendor && !IsAsiaVendor); }
                catch { return false; }
                //return false;
            }
        }

        public static bool IsAsiaVendor
        {
            get
            {
                try { return (_SessionUsr.RoleID == (int)SecurityService.Roles.AsiaVendor); }
                catch { return false; }
            }
        }

        public static bool IsAsiaOperations
        {
            get
            {
                try { return (_SessionUsr.RoleID == (int)SecurityService.Roles.AsiaOperations); }
                catch { return false; }
            }
        }

        #endregion
        
        #region PO related

        public static List<int> POIDs
        {
            get
            {
                object POIDs = HttpContext.Current.Session["POIDs"];
                if (POIDs != null)
                    return (List<int>)POIDs;
                else
                    return null;
            }
            set
            {
                if (value == null || value.Count < 1) return;//Extra worst-case check                
                HttpContext.Current.Session["POIDs"] = value;
            }

        }

        public static int POposition(int POID)
        {
            try { return POIDs.FindIndex(i => i == POID); }
            catch { return -1; }
        }

        //public static POs POsInMemory { get { return new POs(); } }

        public static void ResetPOInSessionAndEmptyTempUpload(int POID, string POGUID)
        { // Use POGUID to find the exact claim from
            if (!string.IsNullOrEmpty(POGUID)) // HT: ENSURE ClaimGUID is present
                FileIO.CleanTempUpload(POID, POGUID);

            //POsInMemory.Remove(POGUID); // Remove the PO from session
            //HttpContext.Current.Session.Remove("POObj");
        }

        #endregion

        #region Misc & functions

        public static string OldSort1
        {
            get
            {
                try { return (HttpContext.Current.Session["OldSort"] ?? "").ToString().Trim(); }
                catch { return ""; }//DON'T forget to trim
            }
            set
            {
                HttpContext.Current.Session["OldSort"] = value;
            }
        }

        public static string NewSort1
        {
            get
            {
                try { return (HttpContext.Current.Session["NewSort"] ?? "").ToString().Trim(); }
                catch { return ""; }//DON'T forget to trim
            }
            set
            {
                HttpContext.Current.Session["NewSort"] = value;
            }
        }

        public static bool IsValid()
        {/*See in future if need more deep validation 
            if (!string.IsNullOrEmpty((ctx.Session["UserObj"] ?? "").ToString()))
                return (_SessionUsr != new UserService().emptyView);

            return false;*/
            return _SessionUsr.ID > 0;
        }

        public static void Signout()
        {
            FormsAuthentication.SignOut();//HT: reset forms authentication!

            #region clear authentication cookie
            // Get all cookies with the same name
            string[] cookies = new string[] { Defaults.cookieName, Defaults.emailCookie, Defaults.passwordCookie };
            
            //Iterate for each cookie and remove
            foreach (string cookie in HttpContext.Current.Request.Cookies.AllKeys)
                if (!cookies.Contains(cookie))
                    HttpContext.Current.Request.Cookies.Remove(cookie);
            // Strange but it is needed to do it the second time
            foreach (string cookie in HttpContext.Current.Response.Cookies.AllKeys)
                if (!cookies.Contains(cookie))
                    HttpContext.Current.Response.Cookies.Remove(cookie);

            #endregion
            
            //Clear & Abandon session
            HttpContext.Current.Session.Clear();
            HttpContext.Current.Session.Abandon();
        }

        public static string ErrDetailsForELMAH
        {
            get
            {
                try { return (HttpContext.Current.Session["ErrDetailsForELMAH"] ?? "").ToString(); }
                catch { return ""; }
            }
            set
            {
                if (HttpContext.Current != null && HttpContext.Current.Session != null)
                    HttpContext.Current.Session["ErrDetailsForELMAH"] = value;
            }
        }

        public static string WebappVersion
        { // http://www.craftyfella.com/2010/01/adding-assemblyversion-to-aspnet-mvc.html
            get
            {
                if (string.IsNullOrEmpty((HttpContext.Current.Session["WebappVersion"] ?? "").ToString()))
                {
                    try
                    {
                        System.Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                        return version.Major + "." + version.Minor + "." + version.Build;
                    }
                    catch (Exception)
                    {
                        return "?.?.?";
                    }
                }
                else
                    return HttpContext.Current.Session["WebappVersion"].ToString();
            }
        }

        #endregion
    }

    /* Kept for future ref
    public class POs
    {
        //http://stackoverflow.com/questions/287928/how-do-i-overload-the-square-bracket-operator-in-c
        //http://msdn.microsoft.com/en-us/library/2549tw02.aspx

        // Indexer declaration. 
        // If index is out of range, the array will throw the exception.
        public POHeader this[string POGUID]
        {
            get
            {
                object data = HttpContext.Current.Session[POGUID];
                try
                {
                    if (string.IsNullOrEmpty(data.ToString()))//byteArrStr
                        return new POService().emptyPO;
                    return Serialization.Deserialize<POHeader>(data.ToString());//byteArrStr
                }
                catch { return new POService().emptyPO; }
                //foreach (PO clm in this)
                //    if (clm.POGUID == POGUID) return clm;
            }
            set
            {
                if (!string.IsNullOrEmpty(value.POGUID))
                    HttpContext.Current.Session[value.POGUID] = Serialization.Serialize<POHeader>(value);
            }
        }

        public void Remove(string POGUID)
        {
            HttpContext.Current.Session.Remove(POGUID);
        }
    }
*/

    [Serializable]
    public class Filters
    {//http://stackoverflow.com/questions/287928/how-do-i-overload-the-square-bracket-operator-in-c
        const string prefix = "ObjFor";
        public static readonly object empty = new object();
        public enum list
        {
            _None,
            Dashboard,
            ActivityLog,
            User
        }

        // Indexer declaration. 
        // If index is out of range, the array will throw the exception.
        public object this[Enum filterID]
        {
            get
            {
                object filterData = HttpContext.Current.Session[prefix + filterID.ToString()];
                try
                {
                    if (filterData == null || filterData == empty)
                    {
                        switch (_Enums.ParseEnum<list>(filterID))
                        {
                            case list.Dashboard: filterData = new DashboardService().emptyView; break;
                            case list.ActivityLog: filterData = new ActivityLogService(ActivityLogService.Activity.Login).emptyView; break;
                            case list.User: filterData = new UserService().emptyView; break;
                        }
                    }                 
                    return filterData;
                }
                catch { return null; }                
            }
            set
            {
                HttpContext.Current.Session[prefix + filterID.ToString()] = value;
            }
        }

        public void Remove(string filterID)
        {
            HttpContext.Current.Session.Remove(prefix + filterID);
        }
    }
}
