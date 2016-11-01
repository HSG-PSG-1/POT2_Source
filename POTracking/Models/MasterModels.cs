﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Data.Linq.Mapping;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Text.RegularExpressions;
using POT.Services;
using HSG.Helper;

namespace POT.Models
{
    [DuplicateMatchAndRequiredForMASTERAttribute("Code", "CodeOLD")]
    [MetadataType(typeof(MastersMetadata))]
    [Table] //HT: Required to bind extra properties while Dynamic LINQ: http://geekswithblogs.net/michelotti/archive/2008/04/20/121437.aspx
    public partial class Master
    {
        [Column(Name = "ID")]
        public int ID { get; set; }
        public static MasterService.Table Tbl { get; set; }
        [Column(Name = "Code")]
        public string Code { get; set; }
        [Column(Name = "Code")]
        public string CodeOLD { get; set; }
        [Column(Name = "Description")]
        public string Description { get; set; }
        [Column(Name = "SortOrder")]
        public int SortOrder { get; set; }
        [Column(Name = "CanDelete")]
        public bool CanDelete { get; set; }
        [Column(Name = "LastModifiedBy")]
        public int LastModifiedBy { get; set; }
        [Column(Name = "Name")]//HT: Make sure the SELECT statement has it !
        public string LastModifiedByVal { get; set; }
        [Column(Name = "LastModifiedDate")]
        public DateTime LastModifiedDate { get; set; }

        //HT: Special column only for MasterDefect
        [Column(Name = "Category")]
        public string Category { get; set; }

        public bool _Updated { get; set; }
        
        public bool _Added { get; set; }
        public bool _Deleted { get; set; }

        public const string delRefChkMsg = "One or more entry marked for delete is being referred by another entity. So cannot delete it.";
        public const string insCodeDuplicateMsg = "One or more new entries have duplicate code.";
    }

    public class MastersMetadata
    {
        [DisplayName("Code")]
        [Required(ErrorMessage = Defaults.RequiredMsg)]
        [StringLength(80, ErrorMessage = Defaults.MaxLengthMsg)] // required here so that the CustomTextbox StringLengthAttribute can infer it        
        public string Code { get; set; }

        [DisplayName("Description")]
        [StringLength(250, ErrorMessage = Defaults.MaxLengthMsg)]
        public string Description { get; set; }

        [DisplayName("Sort Order")]
        [Required(ErrorMessage = Defaults.RequiredMsg)]
        public int SortOrder { get; set; }

        [DisplayName("Last Modified By")]
        public string LastModifiedBy { get; set; }

        [DisplayName("Last Modified Date")]
        public DateTime LastModifiedDate { get; set; }
    }

    //HT: We need both Required and Duplicate check so better to keep it within the Master function
    public partial class RoleRights : Master
    {
        public POT.DAL.UserRole RoleData { get; set; }
        //Delete after further review for future: public bool rightsChanged { get; set; }
        public new bool _Updated
        {
            get { return base._Updated /*|| rightsChanged*/; }
        }
        
        //DON'T forget to set CodeOLD and SortOrderOLD in FetchAll because it won't be bound using [Column(Name =
        //that is because we're NOT fetching it in a List<Master>
    }
}

/*namespace POT.DAL
{
    public partial class UserRole
    {
        public string Code
        {
            get { return Title; }
            set { Title = value; }
        }
    }
}*/
