using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Text.RegularExpressions;
using POT.Services;
using HSG.Helper;

namespace POT.DAL
{
    [Serializable]
    public abstract class Opr
    {
        #region Variables & Properties

        public const string sep = ";";

        public bool _Added { get; set; }
        public bool _Edited { get; set; }
        public bool _Deleted { get; set; }
        // common property required for all the PO and its child objects
        public string POGUID { get; set; }

        #endregion

        // Set some required fields to proceed (mostly overridded in child class)
        public void setOpr(int ID)
        {// Default settings
            this._Added = (ID <= Defaults.Integer) && !(this._Deleted);
            this._Edited = (!this._Added) && !(this._Deleted);
        }
    }

    #region PO Model (& vw_PO_Master_User_Loc)

    [Serializable, System.Xml.Serialization.XmlRoot(ElementName = "POHeader", IsNullable = true)]
    [MetadataType(typeof(POHeaderMetadata))]
    public partial class POHeader
    {
        #region Extra Variables & Properties

        public List<POComment> aComments { get; set; }
        public List<PODetail> aLines { get; set; }
        public List<POFile> aFiles { get; set; }

        public string AssignToVal { get; set; }

        /*[JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime PODate { get; set; }
        public string PODateStr { get { return Defaults.formattedDate(PODate); } }*/

        // HT: Make sure this is a global neutral format like : dd-M-yy (i.e. 12-Mar-2014)
        public string PODateStr { get; set; }
        public string DateLcOpenedStr { get; set; }
        public string EtaStr { get; set; }
        public string EtdStr { get; set; }
        public string PoPlacedDateStr { get; set; }
        public string BLDateStr { get; set; }
        
        #region Extra dates for timezone handling
        public void setDatesFromStr()
        {
            //if (!string.IsNullOrEmpty(PODateStr) && this.PODate.HasValue) 
            //try { this.PODate = DateTime.Parse(PODateStr); }            catch (Exception ex) { ;}
            DateTime dt = DateTime.Today;
            if (PODate.HasValue && DateTime.TryParse(PODateStr, out dt)) PODate = dt;
            //HT: Now that we've ensured that if date is set to null / empty so will the dateStr so the below will do but still for precaution
            //if (DateTime.TryParse(PODateStr, out dt)) PODate = dt; 
            if (DateLcOpened.HasValue && DateTime.TryParse(DateLcOpenedStr, out dt)) DateLcOpened = dt;
            if (Eta.HasValue && DateTime.TryParse(EtaStr, out dt)) Eta = dt;
            if (Etd.HasValue && DateTime.TryParse(EtdStr, out dt)) Etd = dt;
            if (PoPlacedDate.HasValue && DateTime.TryParse(PoPlacedDateStr, out dt)) PoPlacedDate = dt;
            if (BLDate.HasValue && DateTime.TryParse(BLDateStr, out dt)) BLDate = dt;
        }
        /*
        public string PODate1 { get { return Defaults.formattedDate(PODate); } }
        public string DateLcOpened1 { get { return Defaults.formattedDate(DateLcOpened); } }
        public string Eta1 { get { return Defaults.formattedDate(Eta); } }
        public string Etd1 { get { return Defaults.formattedDate(Etd); } }
        public string PoPlacedDate1 { get { return Defaults.formattedDate(PoPlacedDate); } }
        public string BLDate1 { get { return Defaults.formattedDate(BLDate); } }
        */
        #endregion

        public int? OrderStatusIDold { get; set; }
        public int? AssignToIDold { get; set; }
        
        public string POGUID { get; set; } // common property required for all the PO and its child objects

        #endregion
    }    
    public partial class vw_POHeader
    {
        #region Extra Variables & Properties

        public string VendorAddress
        {
            get {
                return
                    (string.IsNullOrEmpty(VendorAddress1) ? "" : VendorAddress1+ "<br/>")  +
                    (string.IsNullOrEmpty(VendorAddress2) ? "" : VendorAddress2 + "<br/>") +
                    (string.IsNullOrEmpty(VendorAddress3) ? "" : VendorAddress3 + "<br/>") +
                    (string.IsNullOrEmpty(VendorCity) ? "" : VendorCity + " ") +
                    (string.IsNullOrEmpty(VendorState) ? "" : VendorState + "<br/>") +
                    (string.IsNullOrEmpty(VendorCountryName) ? "" : VendorCountryName);
            }
        }

        public string ShipToAddress
        {
            get
            {
                return
                    (string.IsNullOrEmpty(ShipToAddress1) ? "" : ShipToAddress1 + "<br/>") +
                    (string.IsNullOrEmpty(ShipToAddress2) ? "" : ShipToAddress2 + "<br/>") +
                    (string.IsNullOrEmpty(ShipToAddress3) ? "" : ShipToAddress3 + "<br/>") +
                    (string.IsNullOrEmpty(ShipToCity) ? "" : ShipToCity + " ") +
                    (string.IsNullOrEmpty(ShipToState) ? "" : ShipToState + " ") +
                    (string.IsNullOrEmpty(ShipToZipCode) ? "" : ShipToZipCode + " ") +
                    (string.IsNullOrEmpty(ShipToCountryCode) ? "" : ShipToCountryCode);
            }
        }

        public int AssignToOld { get; set; }
        public int OrderStatusIDold { get; set; }
        public string POGUID { get; set; }
        
        #endregion
    }

    public class POHeaderMetadata
    {
        [DisplayName("PO #")]
        [Required(ErrorMessage = "PO #" + Defaults.RequiredMsgAppend)]
        public string PONumber { get; set; }

        [DisplayName("Order Status")]
        [Required(ErrorMessage = "Order Status" + Defaults.RequiredMsgAppend)]
        public int OrderStatusID { get; set; }

        [DisplayName("Vendor")]
        [Required(ErrorMessage = "Vendor" + Defaults.RequiredMsgAppend)]
        public int VendorID { get; set; }

        [DisplayName("User")]
        [Required(ErrorMessage = "User" + Defaults.RequiredMsgAppend)]
        public int UserID { get; set; }

        [DisplayName("PO Date")]
        [Required(ErrorMessage = "PO Date" + Defaults.RequiredMsgAppend)]
        //[Range(typeof(DateTime), System.Data.SqlTypes.SqlDateTime.MinValue.ToString(), System.Data.SqlTypes.SqlDateTime.MaxValue.ToString())]//SO: 1406046
        [Range(typeof(DateTime), "1-Jan-1753", "31-Dec-9999")]
        public int PODate { get; set; }        
    }

    #endregion

    #region PODetail Model

    [Serializable]
    public partial class PODetail : Opr
    {
        #region Extra Variables & Properties

        public string ItemCode { get; set; }

        public string UnitCostStr { get { return (UnitCost.HasValue ? UnitCost.Value : 0.0m).ToString("#0.00"); } }
        public string OrderExtensionStr { get { return (OrderExtension.HasValue ? OrderExtension.Value : 0.0m).ToString("#0.00"); } }

        #endregion
    }

    #endregion

    #region Comment Model

    [MetadataType(typeof(CommentMetadata))]
    public partial class POComment : Opr
    {
        #region Extra Variables & Properties
        public string CommentBy { get; set; }
        //public string Comment1 { get; set; }
        #endregion

        /* Set some required fields to proceed
        public Comment setProp()
        {
            if (!_Deleted)
            {//set necessary fields for Add & Edit
                this.LastModifiedDate = DateTime.Now;
                this.CommentBy = _SessionUsr.UserName;
                this.UserID = _SessionUsr.ID;
                this.PostedOn = DateTime.Now;
            }

            return this;
        }

        /// <summary>
        /// Add, Edit or Delete
        /// </summary>
        /// <param name="aComments"> List of object</param>
        /// <returns>Updated list of objects</returns>
        public List<Comment> doOpr(List<Comment> aComments)
        {
            if (aComments == null) aComments = new List<Comment>();//When there're NO records

            int index = aComments.FindIndex(p => p.ID == this.ID);//SO: 361921/list-manipulation-in-c-using-linq
            base.setOpr(ID);//Set Add or Edit

            #region Set data as per Operation

            if (_Deleted)//Deleted =================
            {
                if (ID < 0) aComments.RemoveAt(index);//remove newly added
                else aComments[index]._Deleted = true; 
            }
            else if (_Edited)//Edited=================
            {
                aComments[index] = this;
            }
            else //Added(or Newly added is edited)================
            {
                #region (New record: we assign -ve POId to avoid conflicts)
                if (index < 0)
                {
                    ID = (aComments.Count > 0) ?  (aComments.Min(c => c.ID) - 1) : Defaults.Integer - 1;
                    while (ID >= 0) { ID = ID - 1; }//Make it < 0
                    aComments.Add(this);
                }
                #endregion
                else //Newly added is edited(we still maintain the flag until final commit)
                    aComments[index] = this;
            }

            #endregion

            return aComments;
        }
        /// <summary>
        /// Add items to list
        /// </summary>
        /// <param name="child">Session PO value</param>
        /// <param name="records">Items</param>
        /// <returns>Updated PO variable</returns>
        public static PO lstAsync(PO parent, List<Comment> records)
        {
            if (parent == null || records == null || records.Count < 1) return parent;

            parent.aComments.AddRange(records);

            return parent;
        }
        */
    }

    public class CommentMetadata
    {
        [DisplayName("Comment")]
        [Required(ErrorMessage = "Comment" + Defaults.RequiredMsgAppend)]
        // Based on : http://www.w3schools.com/SQl/sql_datatypes.asp
        [StringLength(4000, ErrorMessage = Defaults.MaxLengthMsg)]
        public string Comment1 { get; set; }

        [DisplayName("Comment By")]
        public string CommentBy { get; set; }
    }

    #endregion

    #region POFile Model

    [MetadataType(typeof(POFileMetadata))]
    public partial class POFile : Opr
    {
        #region Extra Variables & Properties
        
        /*string _POGUID;
        public string POGUID  { get; set; }
        {
            get
            { // Return _POGUID only for Async entries
                return string.IsNullOrEmpty(_POGUID) ? POID.ToString() : _POGUID;
            }
            set { _POGUID = value; }
        }*/

        public string UploadedBy { get; set; }
        public string FileNameNEW { get; set; }
        public string FileTypeTitle { get; set; }
        public string CodeStr
        {
            get
            {
                if (_Added) //string.Empty;
                    return HttpUtility.UrlEncode(HSG.Helper.Crypto.EncodeStr(FileName + sep + POID.ToString() + sep + POGUID, true));
                else
                    return HttpUtility.UrlEncode(HSG.Helper.Crypto.EncodeStr(FileName + sep + POID.ToString(), true));
            }
        } // Can't use HttpUtility.UrlDecode - because it'll create issues with string.format and js function calls so handle in GetFile
        
        public string FilePath
        { //HT: Usage: <a href='<%= Url.Content("~/" + item.FilePath) %>' target="_blank">
            get
            {
                return FileIO.GetPOFilePath(POID, POGUID, FileName, webURL: true);
            }
        }

        #endregion
    }

    public class POFileMetadata
    {
        [DisplayName("File")]
        /* HT: DON'T - we've handled it from within the controller along with a special case of Update
         [Required(ErrorMessage = "Select a file to be uploaded")] */
        public string FileName { get; set; }

        [DisplayName("Uploaded By")]
        public string UploadedBy { get; set; }

        [DisplayName("Type")]
        public string FileTypeTitle { get; set; }

        [Required(ErrorMessage = "File Type" + Defaults.RequiredMsgAppend)]
        public string FileType { get; set; }

        [StringLength(250, ErrorMessage = Defaults.MaxLengthMsg)]
        public string Comment { get; set; }
    }

    #endregion
}
