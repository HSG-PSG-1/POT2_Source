﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Reflection;
using HSG.Helper;
/*using StackExchange.Profiling;
using StackExchange.Profiling.MVCHelpers;*/
namespace POT
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            // Routes to ignore
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("favicon.ico");
            routes.IgnoreRoute("elmah.axd");

            #region Routes to Map
            
            routes.MapRoute(
                "UserId", // Route name
                "Users/{action}/{id}", // URL with parameters
                new { controller = "User", action = "List", id = UrlParameter.Optional }// Parameter defaults
            );

            routes.MapRoute("User", "Users/{action}", new { controller = "User", action = "List" });
            routes.MapRoute("PO_Default", "PO/{POID}/{action}", new { controller = "PO", POID = -1, action = "Manage?" });// (default action? because we dpn't want to hide it)            
            routes.MapRoute("Role", "Roles/{action}", new { controller = "Role", action = "Manage" });
            routes.MapRoute("Master_Default", "Master/Manage/{masterTbl}", new { controller = "Master", action = "Manage"});
            routes.MapRoute("Home", "Dashboard/{action}", new { controller = "Dashboard", action = "List" });
            routes.MapRoute("Activity", "Activity/{action}", new { controller = "Activity", action = "Log" });
            routes.MapRoute("Master_Manage", "Manage/{masterTbl}", new { controller = "Master", action = "Manage" });
            //routes.MapRoute("Reporting", "Reports/{action}/{reportStr}", new { controller = "Report", action = "Activity" });

            routes.MapRoute("Default","{controller}/{action}/{from}",new { controller = "Common", action = "Login", from = UrlParameter.Optional });
            #endregion
        }

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            //filters.Add(new HandleErrorAttribute());
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterRoutes(RouteTable.Routes);

            //http://msdn.microsoft.com/en-us/library/ff647070.aspx
            //Forms Authentication in ASP.NET 2.0

            #region HT: SPECIAL CODE TO PREVENT FCN FROM RESTARTING SESSION ON file/folder DELETE
            //DO NOT REMOVE OR CHANGE
            //Ref: http://connect.microsoft.com/VisualStudio/feedback/details/240686/asp-net-no-way-to-disable-appdomain-restart-when-deleting-subdirectory
            PropertyInfo p = typeof(System.Web.HttpRuntime).GetProperty("FileChangesMonitor", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            object o = p.GetValue(null, null);
            FieldInfo f = o.GetType().GetField("_dirMonSubdirs", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            object monitor = f.GetValue(o);
            MethodInfo m = monitor.GetType().GetMethod("StopMonitoring", BindingFlags.Instance | BindingFlags.NonPublic);
            m.Invoke(monitor, new object[] { });
            #endregion

            //HT: Kept for future ref (Model Binder) !!!
            //Custom model binder / identifier : http://forums.asp.net/p/1640342/4243825.aspx
            // ModelBinderConfig.Initialize();
            //ModelBinderProviders.Providers.RegisterBinderForType(typeof(IList<ProfileModel>), new CollectionModelBinder<ProfileModel>());

            #region for MiniProfiler
            /*
            GlobalFilters.Filters.Add(new ProfilingActionFilter());            
            RegisterGlobalFilters(GlobalFilters.Filters);

            var copy = ViewEngines.Engines.ToList();
            ViewEngines.Engines.Clear();
            foreach (var item in copy)
            {
                ViewEngines.Engines.Add(new ProfilingViewEngine(item));
            }
            */
            #endregion
        }

        protected void Session_Start(Object sender, EventArgs e)
        {
            if (Session.IsNewSession)
            {
                //the authentication cookie is intact
                if ((Request.Headers["Cookie"] != null) && (Request.Headers["Cookie"].IndexOf("ASP.NET_SessionId") >= 0))
                {
                    System.Web.Security.FormsAuthentication.SignOut();
                    Context.User = null;

                    //Response.Redirect(Request.ApplicationPath + "Common/Login");
                    // IMP: Don't use this or Session_End or it'll go into infinite loop
                }
            }
        }
                
        protected void Application_Error(object sender, EventArgs e)
        {//http://prideparrot.com/blog/archive/2012/5/exception_handling_in_asp_net_mvc

            var ex = Server.GetLastError();
            //HT: First get the error details (formatted into Sesion)
            string errMsgDetails = "", errMsg = Defaults.formatExceptionDetails(ex, ref errMsgDetails);
            _Session.ErrDetailsForELMAH = errMsg + errMsgDetails; // To be used by ELMAH & Err user-control

            var httpContext = ((MvcApplication)sender).Context;

            var currentRouteData = RouteTable.Routes.GetRouteData(new HttpContextWrapper(httpContext));
            var currentController = " ";
            var currentAction = " ";

            if (currentRouteData != null)
            {
                if (currentRouteData.Values["controller"] != null && !String.IsNullOrEmpty(currentRouteData.Values["controller"].ToString()))
                {
                    currentController = currentRouteData.Values["controller"].ToString();
                }

                if (currentRouteData.Values["action"] != null && !String.IsNullOrEmpty(currentRouteData.Values["action"].ToString()))
                {
                    currentAction = currentRouteData.Values["action"].ToString();
                }
            }            

            var controller = new POT.Controllers.ErrorController();
            var routeData = new RouteData();
            var action = "Error";

            if (ex is HttpException)
            {
                var httpEx = ex as HttpException;

                switch (httpEx.GetHttpCode())
                {
                    case 404:
                        action = "DataNotFound";
                        break;
                    case 5001:
                        httpContext.Response.Write(string.Format("Request / Fileupload length cannot exceed {0}MB", Config.MaxFileSizMB));
                        httpContext.Response.Flush();
                        httpContext.Response.End();
                        return;                        

                    // others if any

                    default:
                        action = "Error";
                        break;
                }
            }            

            httpContext.ClearError();
            httpContext.Response.Clear();
            httpContext.Response.StatusCode = ex is HttpException ? ((HttpException)ex).GetHttpCode() : 500;

            routeData.Values["controller"] = "Error";
            routeData.Values["action"] = action;

            controller.ViewData.Model = new HandleErrorInfo(ex, currentController, currentAction);
                //new ViewDataDictionary<HandleErrorInfo>(new HandleErrorInfo(ex, currentController, currentAction));

            ((IController)controller).Execute(new RequestContext(new HttpContextWrapper(httpContext), routeData));
        }

        protected void ErrorMail_Mailing(object sender, Elmah.ErrorMailEventArgs e)
        { // http://scottonwriting.net/sowblog/archive/2011/01/06/customizing-elmah-s-error-emails.aspx
                //Configure App error email 
            string physicalApplicationPath = e.Error.ServerVariables["APPL_PHYSICAL_PATH"]; // Because HttpContext is NULL
            // physicalApplicationPath  - Pass it for locations in which HttpContext is NULL 
            string errMsgDetails = "", errMsg = Defaults.formatExceptionDetails(e.Error.Exception, ref errMsgDetails);
            System.Net.Mail.MailMessage msg = MailManager.ConfigureApplicationErrorMail(errMsg + errMsgDetails, e.Mail, physicalApplicationPath); // e.Mail shud be configured     _Session.ErrDetailsForELMAH
                e.Mail.Priority = System.Net.Mail.MailPriority.High;
                e.Mail.Subject = e.Mail.Subject + " (ELMAH)";
                //e.Mail.CC.Add("mitchell@4guysfromrolla.com"); // Provide comma sep as per - http://msdn.microsoft.com/en-us/library/ms144695.aspx

            // Other ELMAH resources
            // http://code.google.com/p/elmah/wiki/MVC
            // http://dotnetslackers.com/articles/aspnet/ErrorLoggingModulesAndHandlers.aspx
        }

        #region For MiniProfiler

        protected void Application_BeginRequest()
        {
            /*if (Request.IsLocal)
            { MiniProfiler.Start(); } //or any number of other checks, up to you */
        }

        protected void Application_EndRequest()
        {
            /*MiniProfiler.Stop(); //stop as early as you can, even earlier with MvcMiniProfiler.
            MiniProfiler.Stop(discardResults: true);*/
        }

        #endregion

        /*For future migration to .Net v4.5
         * 
         * public void FormsAuthentication_OnAuthenticate(object sender, System.Web.Security.FormsAuthenticationEventArgs args)
        { // http://www.sitecore.net/Community/Technical-Blogs/John-West-Sitecore-Blog/Posts/2012/09/Object-of-type-System-Int32-cannot-be-converted-to-type-System-Web-Security-Cryptography-Purpose.aspx
            args.User = HttpContext.Current.User;
        }*/
    }
}