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
    public class DashboardService : _ServiceBase
    {
        #region Variables
        
        public readonly vw_PO_Dashboard emptyView = new vw_PO_Dashboard() { ID = Defaults.Integer };
        public const string sortOn = "PONumber DESC", sortOn1 = "PONumber DESC";
        
        #endregion

        public List<vw_PO_Dashboard> Search(string orderBy, int? pgIndex, int pageSize, vw_PO_Dashboard das, bool isExcelReport, bool applyLocFilter)
        {
            orderBy = string.IsNullOrEmpty(orderBy) ? sortOn : orderBy;

            using (dbc)
            {
                IQueryable<vw_PO_Dashboard> dasQ;
                #region Special case for customer (apply accessible location filter)
                if (!applyLocFilter)
                    dasQ = (from vw_u in dbc.vw_PO_Dashboards select vw_u);
                else // only for customer
                    dasQ = (from vw_u in dbc.vw_PO_Dashboards 
                            //join ul in dbc.UserLocations on new { LocID = vw_u.ShipToLocationID } equals new { LocID = ul.LocID }
                            //where ul.UserID == _SessionUsr.ID
                            select vw_u );
                #endregion

                //Get filters - if any
                dasQ = PrepareQuery(dasQ, das);
                // Apply Sorting, Pagination and return PagedList

                #region Sort and Return result
                //Special case to replace Customproperty with original (for ShipToLoc)
                // For better implementation: SO: 2241643/how-to-use-a-custom-property-in-a-linq-to-entities-query
                orderBy = (orderBy ?? "").Replace("ShipToLocAndCode", "ShipToLoc");

                if (isExcelReport)
                    return dasQ.OrderBy(orderBy).ToList<vw_PO_Dashboard>();
                else
                    return dasQ.OrderBy(orderBy).ToPagedList(pgIndex ?? 1, pageSize);

                /* Apply pagination and return - kept for future ref
                return dasQ.OrderBy(orderBy).Skip(pgIndex.Value).Take(pageSize).ToList<vw_PO_Dashboard>(); 
                */
                #endregion
            }
        }

        public List<vw_PO_Dashboard> SearchKO(string orderBy, int? pgIndex, int pageSize, vw_PO_Dashboard das, bool isExcelReport, bool applyLocFilter)
        {
            orderBy = string.IsNullOrEmpty(orderBy) ? sortOn : orderBy;

            using (dbc)
            {
                IQueryable<vw_PO_Dashboard> dasQ;
                #region Special case for customer (apply accessible location filter)
                if (!applyLocFilter)
                    dasQ = (from vw_u in dbc.vw_PO_Dashboards select vw_u);
                else // only for customer
                    dasQ = (from vw_u in dbc.vw_PO_Dashboards
                            //join ul in dbc.UserLocations on new { LocID = vw_u.ShipToLocationID } equals new { LocID = ul.LocID }
                            //where ul.UserID == _SessionUsr.ID
                            select vw_u);
                #endregion

                //Get filters - if any
                dasQ = PrepareQuery(dasQ, das);
                // Apply Sorting, Pagination and return PagedList

                #region Sort and Return result
                //Special case to replace Customproperty with original (for ShipToLoc)
                // For better implementation: SO: 2241643/how-to-use-a-custom-property-in-a-linq-to-entities-query
                orderBy = (orderBy ?? "").Replace("ShipToLocAndCode", "ShipToLoc");

                if (isExcelReport)
                    return dasQ.OrderBy(orderBy).ToList<vw_PO_Dashboard>();
                else /* Apply pagination and return - kept for future ref */
                    return dasQ.OrderBy(orderBy).Skip(pgIndex.Value * pageSize).Take(pageSize).ToList<vw_PO_Dashboard>(); 
                
                #endregion
            }
        }

        public List<int> SearchPOIDKO(vw_PO_Dashboard das, string orderBy)
        {
            orderBy = string.IsNullOrEmpty(orderBy) ? sortOn : orderBy;
            using (dbc)
            {
                IQueryable<vw_PO_Dashboard> dasQ;
                dasQ = (from vw_u in dbc.vw_PO_Dashboards select vw_u);
                
                //Get filters - if any
                dasQ = PrepareQuery(dasQ, das);
                return dasQ.OrderBy(orderBy).Select(r => r.ID).ToList();
            }
        }
        
        public static IQueryable<vw_PO_Dashboard> PrepareQuery(IQueryable<vw_PO_Dashboard> dasQ, vw_PO_Dashboard das)
        {
            #region Append WHERE clause if applicable

            // MUST be done at VIEW level in SQL - dasQ = dasQ.Where(o => o.Archived == false);

            if (!string.IsNullOrEmpty(das.PONumbers))// Filter for multiple PO No.s
            {
                dasQ = dasQ.Where(o => SqlMethods.Like(o.PONumber.ToLower(), "%" + das.PONumbers.ToLower() + "%"));

                /* Kept for future ref - if (das.PONumbers.IndexOf(",") < 1)
                    dasQ = dasQ.Where(o => SqlMethods.Like(o.PONumber.ToString(), "%" + das.PONumbers + "%"));                
                else
                    dasQ = dasQ.Where(o => das.PONumbers.Contains(o.PONumber)); ERROR !!! */
            }
            
            if (das.BrandID > 0) dasQ = dasQ.Where(o => o.BrandID == das.BrandID);
            else if (!string.IsNullOrEmpty(das.BrandName))
                dasQ = dasQ.Where(o => SqlMethods.Like(o.BrandName.ToLower(), "%" + das.BrandName.ToLower() + "%"));
            
            if (das.OrderStatusID > 0) dasQ = dasQ.Where(o => o.OrderStatusID == das.OrderStatusID);
            else if (!string.IsNullOrEmpty(das.Status))
                dasQ = dasQ.Where(o => SqlMethods.Like(o.Status.ToLower(), das.Status.ToLower()));

            if (das.AssignTo > 0) dasQ = dasQ.Where(o => o.AssignTo == das.AssignTo);
            if (das.VendorID > 0) dasQ = dasQ.Where(o => o.VendorID == das.VendorID);

            if (!string.IsNullOrEmpty(das.ShipToCity)) dasQ = dasQ.Where
               (o => SqlMethods.Like(o.ShipToCity.ToLower(), "%" + das.ShipToCity.ToLower() + "%"));

            #region Apply date filter
            //http://www.filamentgroup.com/lab/date_range_picker_using_jquery_ui_16_and_jquery_ui_css_framework/
            if (das.PODateFrom.HasValue) dasQ = dasQ.Where(o => o.PODate.Value.Date >= das.PODateFrom_SQL.Value.Date);
            if (das.PODateTo.HasValue) dasQ = dasQ.Where(o => o.PODate.Value.Date <= das.PODateTo_SQL.Value.Date);
            if (das.ETAFrom.HasValue) dasQ = dasQ.Where(o => o.Eta.Value.Date >= das.ETAFrom_SQL.Value.Date);
            if (das.ETATo.HasValue) dasQ = dasQ.Where(o => o.Eta.Value.Date <= das.ETATo_SQL.Value.Date);
            if (das.ETDFrom.HasValue) dasQ = dasQ.Where(o => o.Etd.Value.Date >= das.ETDFrom_SQL.Value.Date);
            if (das.ETDTo.HasValue) dasQ = dasQ.Where(o => o.Etd.Value.Date <= das.ETDTo_SQL.Value.Date);

            #endregion

            #endregion

            #region Special case for Asia: Operations

            int[] orgs = new int[] { Config.VendorIDDeestone, Config.VendorIDDeestoneLtd, Config.VendorIDSvizz, Config.VendorIDSiamtruck };

            if (_Session.IsAsiaOperations) // SO : 183791
                dasQ = dasQ.Where(o => !orgs.Contains(o.VendorID ?? -1)); // (Internal) Asia operations role : can see all PO’s except Deestone and Svizz-One.
            else if (_Session.IsAsiaVendor)
                dasQ = dasQ.Where(o => orgs.Contains(o.VendorID ?? -1)); // (Vendor) Asia Vendor role : can see ONLY PO’s for both Deestone and Svizz-One.

            #endregion

            return dasQ;
        }
    }
}
