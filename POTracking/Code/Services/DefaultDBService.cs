using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Data.Linq.SqlClient;
using POT.DAL;
using HSG.Helper;
using POT.Models;

namespace POT.Services
{
    public class DefaultDBService : _ServiceBase
    {
        /*public static DefaultPO GetPO(int userID)
        {
            return new DefaultPO()
            {
                CustID = _Session.NewCustOrgId,
                AssignTo = Config.DefaultPOAssigneeId,
                ShipToLocID = 0,
                OrderStatusID = Config.DefaultPOStatusId,
                BrandID = 0
            };
        }*/

        public static RoleRights GetNewRoleRight(int userID, string usrName, int sortOrdr)
        {
            return new RoleRights()
                {
                    _Added = false,
                    ID = -1,
                    Code = "[Role-1]",//Required otherwise it'll be considered as ModelState error !
                    CodeOLD = "[Role-1]",
                    LastModifiedBy = userID,
                    LastModifiedByVal = usrName,
                    LastModifiedDate = DateTime.Now,
                    RoleData = new UserRole() { OrgTypeId = (int)OrgService.OrgType.Internal },
                    CanDelete = true,
                    SortOrder = sortOrdr
                };
        }
    }
}
