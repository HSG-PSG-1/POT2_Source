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
    public class DetailService : _ServiceBase
    {
        #region Variables & Constructor
        
        public DetailService() : base() {;}
        public DetailService(POTmodel dbcExisting) : base(dbcExisting) { ;}
        public readonly PODetail newObj = new PODetail();

        #endregion

        #region Search / Fetch

        public List<vw_POLine> Search(int POID, int? userID)
        {
            //using (dbc)//HT: DON'T coz we're sending IQueryable
            IQueryable<vw_POLine> cQuery = from c in dbc.vw_POLines
                         where c.POID == POID
                         orderby c.ID // make sure its Id otherwise the sort order will differ from what was entered
                         select c;
            return cQuery.ToList();
        }
                
        //PODetail Transform(PODetail c, string ItemCode, string Defect)
        //{
        //    return c.Set(c1 => { c1.ItemCode = ItemCode; c1.Defect = Defect; });
        //}

        #endregion                
        
    }
}
