using System;
using System.Configuration;
using System.Web;
using POT.Services;

namespace HSG.Helper
{
    public class Config
    {
        public static System.Collections.Hashtable ConfigSettings
        {
            get { return (System.Collections.Hashtable)(HttpContext.Current.Session["ConfigSettings"]); }
            set { HttpContext.Current.Session["ConfigSettings"] = value; }
        }

        #region WEB.CONFIG Properties

        #region Root / Path / Download url / Email Temp dir

        /// Get the path of root directory
        /// <summary>
        /// Property used to get the path of root directory
        /// </summary>
        /// 
        public static string RootPath
        {
            get
            {
                return HttpContext.Current.Request.PhysicalApplicationPath; //+ FileIO.dSep;
            }
        }

        /// Get the path of upload file
        /// <summary>
        /// Property used to get the path of upload file
        /// </summary>
        public static string UploadPath
        {
            get
            {
                return RootPath + ConfigurationManager.AppSettings.Get("uploadPath");// +FileIO.webPathSep;
            }
        }

        /// Get the path for download Url
        /// <summary>
        /// Property used to get the path for download Url
        /// </summary>
        public static string DownloadUrl
        {
            get
            {
                return ConfigurationManager.AppSettings.Get("downloadUrl");
            }
        }
                
        /// Path of email Template with reference to the roor directory
        /// <summary>
        /// Property used to get the path of email Template with reference to the roor directory
        /// </summary>
        /// 
        public static string EmailTemplatePathByRefToRoot
        {
            get
            {
                return ConfigurationManager.AppSettings.Get("emailTemplatePathByRefToRoot");
            }
        }

        #endregion

        /// Max upload file size in MB
        /// <summary>
        /// Max upload file size in MB (Default 20mb)
        /// </summary>
        public static int MaxFileSizMB
        {
            get
            {
                int sizeMB;

                try { sizeMB = int.Parse(ConfigurationManager.AppSettings.Get("MaxFileSizMB")); }
                catch { sizeMB = 20; }

                return sizeMB;
            }
        }      

        ///Flag to debug(display as text on page) the emails sent bt web app
        /// <summary>
        /// Flag to debug(display as text on page) the emails sent bt web app
        /// </summary>
        public static bool DebugMail
        {
            get
            {
                bool flag = false;
                bool.TryParse(ConfigurationManager.AppSettings.Get("debugMail"), out flag);
                return flag;
            }
        }

        ///Flag to Nofity AssignTo user everytime his PO is updated
        /// <summary>
        ///Flag to Nofity AssignTo user everytime his PO is updated
        /// </summary>
        public static bool NofityAssignToEveryTime
        {
            get
            {
                bool flag = false;
                bool.TryParse(ConfigurationManager.AppSettings.Get("nofityAssignToEveryTime"), out flag);
                return flag;
            }
        }
                
        /// Application Error Email Notifiers
        /// <summary>
        /// Application Error Email Notifiers
        /// </summary>
        /// 
        public static string ApplicationErrorEmail
        {
            get
            {
                return ConfigurationManager.AppSettings.Get("applicationErrorEmail");
            }
        }

        /// Customer Code Length in Location Code
        /// <summary>
        /// Customer Code Length in Location Code
        /// </summary>
        public static int CustCodeLenInLocCode
        { get { return int.Parse(ConfigurationManager.AppSettings.Get("custCodeLenInLocCode")); } }

        /// VendorID for Deestone
        /// <summary>
        /// VendorID for Deestone
        /// </summary>
        public static int VendorIDDeestone
        {
            get
            {
                try { return int.Parse(ConfigurationManager.AppSettings.Get("vendorIDDeestone")); }
                catch (Exception ex) { return -1; }
            }
        }

        /// VendorID for Deestone Ltd
        /// <summary>
        /// VendorID for Deestone Ltd
        /// </summary>
        public static int VendorIDDeestoneLtd
        {
            get
            {
                try { return int.Parse(ConfigurationManager.AppSettings.Get("VendorIDDeestoneLtd")); }
                catch (Exception ex) { return -1; }
            }
        }

        /// VendorID for Svizz
        /// <summary>
        /// VendorID for Svizz
        /// </summary>
        public static int VendorIDSvizz
        {
            get
            {
                try { return int.Parse(ConfigurationManager.AppSettings.Get("VendorIDSvizz")); }
                catch (Exception ex) { return -1; }
            }
        }

        /// VendorID for Siamtruck Radial Company Ltd.
        /// <summary>
        /// VendorID for Siamtruck Radial Company Ltd.
        /// </summary>
        public static int VendorIDSiamtruck
        {
            get
            {
                try { return int.Parse(ConfigurationManager.AppSettings.Get("VendorIDSiamtruck")); }
                catch (Exception ex) { return -1; }
            }
        }

        #endregion //Properties

        #region Settings fetched from DB

        public static int RememberMeHours
        {
            get
            {
                int hours;
                //try { hours = int.Parse(ConfigurationManager.AppSettings.Get("RememberMeHours")); }
                try { hours = int.Parse(ConfigSettings[SettingService.settings.Remember_Me_Hours.ToString()].ToString()); }
                catch { hours = 50; }

                return hours;
            }
        }

        public static string ContactEmail
        {
            get
            {
                try { return ConfigSettings[SettingService.settings.Contact_Email.ToString()].ToString(); }
                catch (Exception ex) { return "contact@american-omni.com";/* Special use case for forgot pwd? */ }
            }
        }

        public static string ErrorLevelDetail
        {
            get { try { return ConfigSettings[SettingService.settings.Error_Detail_Level.ToString()].ToString(); } catch (Exception ex) { return "1"; } }/*Special case try catch to handle expired session*/
        }

        public static int DashboardPageSize 
        {
            get { try { return int.Parse(ConfigSettings[SettingService.settings.Dashboard_Page_Size.ToString()].ToString()); } catch (Exception ex) { return 10; } }/*Special case try catch to handle expired session*/
        }

        public static int UserListPageSize
        {
            get { try { return int.Parse(ConfigSettings[SettingService.settings.User_List_Page_Size.ToString()].ToString()); } catch (Exception ex) { return 10; } }/*Special case try catch to handle expired session*/
        }
        
        #region Default PO settings (from DB as well as web.config)

        public static int DefaultPOAssigneeId
        { get { return int.Parse(ConfigSettings[SettingService.settings.Default_PO_Assignee.ToString()].ToString()); } }

        public static int DefaultPOStatusId
        { get { return int.Parse(ConfigSettings[SettingService.settings.Default_PO_Status.ToString()].ToString()); } }

        //public static int DefaultPOCustOrgId
        //{ get { return int.Parse(ConfigSettings[SettingService.settings.Default_PO_Customer.ToString()].ToString()); } }        
        
        #endregion

        #endregion // Settings fetched from DB
    }
}