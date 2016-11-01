using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Data.Linq.SqlClient;
using POT.DAL;
using HSG.Helper;
using POT.Models;
using Webdiyer.WebControls.Mvc;

namespace POT.Services
{
    public class LookupService : _ServiceBase
    {
        #region Variables

        public enum Source
        {
            Internal,
            Vendor,//Make sure Org sequence and value is same as OrgService.OrgType
            Brand,
            Item,
            Item1,
            Status,
            Carrier,
            ContainerType,
            User,
            POFileType,
            Org,
            OrgType,
            Error_Detail_Level
        }

        #endregion

        /// <summary>
        /// Get lookup based on source passed and search for term
        /// </summary>
        /// <param name="src">LookupService.Source enum value</param>
        /// <param name="term">Term to be searched as contains search</param>
        /// /// <param name="extras">Any extra data passed (required for special cases like Item1)</param>
        /// <returns>IEnumerable LINQ query</returns>       
        public System.Collections.IEnumerable GetLookup(Source src, bool addEmpty = true, string term = null, string extras = null)
        {
            IQueryable results;
            bool noTerm = string.IsNullOrEmpty(term), noExtra = (string.IsNullOrEmpty(extras) || int.Parse(extras) <= Defaults.Integer);

            term = (term ?? "").ToLower();

            switch (src)
            {
                #region Orgs,OrgType, Item & Brand
                case Source.Org:
                    //src = noTerm ? Source.Customer : _Enums.ParseEnum<LookupService.Source>(extras);
                    results = new OrgService().GetOrgsByRoleId(int.Parse(extras), term);//GetOrgs(src, term);
                    break;
                case Source.OrgType:
                    results = from o in dbc.OrgTypes
                              orderby o.Code
                              select new { id = o.ID, value = o.Code };
                    break;
                case Source.Internal:
                case Source.Vendor:

                    #region Special case for Asia Vendor & Asia Operations user
                    if (_Session.IsOnlyVendor)
                        results = new List<POT.Models.Lookup>(){new Lookup(){ id = _SessionUsr.OrgID.ToString(),
                            value = _SessionUsr.OrgName }}.AsQueryable();
                    else if (_Session.IsAsiaVendor)
                        results = new List<POT.Models.Lookup>(){
                            new Lookup(){ id = Config.VendorIDSvizz.ToString(), value = "Svizz-One Corporation Ltd." },
                            new Lookup(){ id = Config.VendorIDDeestone.ToString(), value = "Deestone" },
                            new Lookup(){ id = Config.VendorIDDeestone.ToString(), value = "Deestone Limited" },
                            new Lookup(){ id = Config.VendorIDSiamtruck.ToString(), value = "Siamtruck Radial Company Ltd" }
                        }.AsQueryable();
                    #endregion
                    else
                        results = new OrgService().GetOrgs(src, term);
                    break;
                case Source.Item://HT: Almost obsolete for now
                    results = from i in dbc.MasterItems
                              where (noTerm || i.Code.ToLower().Contains(term))
                              orderby i.Code
                              select new { id = i.ID, value = i.Code };
                    break;
                case Source.Item1:
                    results = from i in dbc.MasterItems
                              where (noTerm || i.Code.ToLower().Contains(term) || i.Description.ToLower().Contains(term))
                               && (noExtra || i.BrandID == int.Parse(extras))
                              orderby i.Code
                              select new { id = i.ID, value = i.Code, descr = i.Description };
                    break;
                case Source.Brand:
                    results = from i in dbc.MasterBrands
                              where (noTerm || i.Code.ToLower().Contains(term))
                              orderby i.Code//not SortOrder because its not set during import
                              select new { id = i.ID, value = i.Code };

                    break;
                #endregion

                #region Master
                case Source.POFileType:
                    results = from i in dbc.MasterFileTypes
                              where (noTerm || i.Code.ToLower().Contains(term))
                              orderby i.SortOrder
                              select new { id = i.ID, value = i.Code };
                    break;
                case Source.Status:
                    results = from i in dbc.MasterStatus
                              where (noTerm || i.Description.ToLower().Contains(term))
                              orderby i.SortOrder
                              select new { id = i.ID, value = i.Description }; // Special case for Status (fetch description)
                    break;
                case Source.Carrier:
                    results = from i in dbc.MasterCarriers
                              where (noTerm || i.Code.ToLower().Contains(term))
                              orderby i.SortOrder
                              select new { id = i.ID, value = i.Code };
                    break;
                case Source.ContainerType:
                    results = from i in dbc.MasterContainerTypes
                              where (noTerm || i.Code.ToLower().Contains(term))
                              orderby i.SortOrder
                              select new { id = i.ID, value = i.Code };
                    break;

                #endregion

                #region Users
                case Source.User:
                    results = from i in dbc.Users
                              where (noTerm || i.Name.ToLower().Contains(term)) // i.Email.ToLower().Contains(term) ||
                              orderby i.Name
                              select new { id = i.ID, value = (i.Name ?? "") };
                    break;
                #endregion

                #region Others & default
                case Source.Error_Detail_Level:
                    return new List<Lookup>(){new Lookup(){ id = "0", value = "Summary" },
                    new Lookup(){ id = "1", value = "Detailed" }};
                    break;

                default: results = null; break;
                #endregion
            }

            #region Handle special case for no-records found
            if (results.Count() < 1 && addEmpty)
            {
                //CAUTION: make sure the select: & focus: functions are correctly configured!
                // Like : http://stackoverflow.com/questions/8663189/jquery-autocomplete-no-result-message
                List<Lookup> empty = new List<POT.Models.Lookup>(1);
                empty.Add(new POT.Models.Lookup() { value = "No results" });
                results = (from e in empty select e).AsQueryable();
            }
            #endregion

            return results;

        }

        // HT: Kept for future ref
        // public static T ConvertObj<T>(object obj) { return (T)obj; }
    }
}
