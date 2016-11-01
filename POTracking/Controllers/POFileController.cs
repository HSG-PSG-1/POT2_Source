using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using POT.DAL;
using POT.Services;
using HSG.Helper;

namespace POT.Controllers
{ // http://knockoutmvc.com/Home/QuickStart

    //[CompressFilter] - don't use it here
    //[IsAuthorize(IsAuthorizeAttribute.Rights.NONE)]//Special case for some dirty session-abandoned pages and hacks
    public partial class POController : BaseController
    {
        #region File Header Actions

        [HttpPost]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")] //SO: 2570051/error-returning-ajax-in-ie7
        public ActionResult FilePostKO(int POID, /*string POGUID,*/ POFile FileHdrObj)
        { 
            HttpPostedFileBase hpFile = Request.Files["FileNameNEW"];
            bool success = true;
            string result = "";// "Uploaded " + hpFile.FileName + "(" + hpFile.ContentLength + ")";

            #region New file upload

            if ((FileHdrObj.FileNameNEW ?? FileHdrObj.FileName) != null)
            {//HT Delete old\existing file? For Async need to wait until final commit

                #region Old code (make sure the function 'ChkAndSavePOFile' does all of it)
                //string docName = string.Empty;
                //FileIO.result uploadResult = SavePOFile(Request.Files["FileNameNEW"], ref docName, POID, true);

                //if (uploadResult != FileIO.result.successful)
                //    if (uploadResult == FileIO.result.duplicate)
                //        ModelState.AddModelError("FileName", "Duplicate file found");
                //    else
                //        ModelState.AddModelError("FileName", "Unable to upload file");
                #endregion
                // FileHdrObj.FileName = NOT required now
                ChkAndSavePOFile("FileNameNEW", POID, FileHdrObj.POGUID);
                success = (ModelState["FileName"].Errors.Count() < 1);
            }

            #endregion
            result = !success ? ("Unable to upload file - " + ModelState["FileName"].Errors[0].ErrorMessage) : "";

            //Taconite XML
            return this.Content(Defaults.getTaconiteResult(success,
                Defaults.getOprResult(success, result), "fileOprMsg",
                "fileUploadResponse('" + FileHdrObj.CodeStr + "'," + success.ToString().ToLower() + "," + FileHdrObj.ID + ")"), "text/xml");
        }

        [SkipModelValidation]
        [AccessPO("POID")]
        [HttpPost]
        public ActionResult FileKODelete(int POID, string POGUID,[FromJson] POFile delFH)
        {//Call this ONLY when you need to actually delete the file
            bool proceed = false;
            if (delFH != null)
                proceed = FileIO.DeletePOFile(POID, POGUID, delFH.FileName);
            
            //Taconite XML
            return this.Content(Defaults.getTaconiteRemoveTR(proceed,
                Defaults.getOprResult(proceed, "Unable to delete file"), "fileOprMsg"), "text/xml");
        }

        [SkipModelValidation]
        [HttpPost]
        public ActionResult Upload(int POID, HttpPostedFileBase file, [FromJson] POFile FileHdrObj)
        {          
            bool success = true;
            string result = "";

            #region New file upload

            if ((FileHdrObj.FileNameNEW ?? FileHdrObj.FileName) != null)
            {
                #region Old code (make sure the function 'ChkAndSaveClaimFile' does all of it)
                //string docName = string.Empty;
                //FileIO.result uploadResult = SaveClaimFile(Request.Files["FileNameNEW"], ref docName, ClaimID, true);

                //if (uploadResult != FileIO.result.successful)
                //    if (uploadResult == FileIO.result.duplicate)
                //        ModelState.AddModelError("FileName", "Duplicate file found");
                //    else
                //        ModelState.AddModelError("FileName", "Unable to upload file");
                #endregion
                FileHdrObj.FileName = System.IO.Path.GetFileName(FileHdrObj.FileName); // Ensure its file name and not path!
                ChkAndSavePOFile("FileNameNEW", POID, FileHdrObj.POGUID);
                success = ((ModelState["FileName"] ?? new ModelState()).Errors.Count() < 1); // We won't have (initially) - ModelState["FileName"]
            }

            #endregion
            result = !success ? ("Unable to upload file - " + ModelState["FileName"].Errors[0].ErrorMessage) : "";

            string sepr = "~~~";
            return Content((success ? "1" : "0") + sepr + Defaults.getOprResult(success, result) + sepr + FileHdrObj.ID + sepr + FileHdrObj.CodeStr);
            //Url.Content(@"~\Content\" + fileUp.FileName));
        }
        #endregion

        public FileVM GetFileKOModel(int POID, string POGUID)
        {
            //Set File object
            POFile newObj = new POFile()
            {
                ID = -1,
                _Added = true,
                POID = POID,
                POGUID = POGUID,
                UploadedBy = _SessionUsr.Email,//UserName,
                LastModifiedBy = _SessionUsr.ID,
                LastModifiedDate = DateTime.Now,
                UploadDate = DateTime.Now,
                UserID = _SessionUsr.ID,
                FileName = "",
                FileNameNEW = ""
            };

            List<POFile> files = new List<POFile>();
            FileVM vm = new FileVM()
            {
                FileToAdd = newObj,
                EmptyFileHeader = newObj,
                AllFiles = (new POFileService().Search(POID, null))
            };
            // Lookup data
            vm.FileTypes = new LookupService().GetLookup(LookupService.Source.POFileType);

            return vm;
        }

        #region Extra Actions and functions to get code for file download

        /// <summary>
        /// Check and Save PO File being uploaded. Set error in ModelState if any issue
        /// </summary>
        /// <param name="hpFileKey">HttpPost file browser control Id</param>
        /// <param name="POId">PO Id</param>
        /// <param name="PODetailId">PO Detail Id</param>
        /// <param name="upMode">FileIO.mode (Async or Sync & Header  or Detail)</param>
        /// <returns>File upload name</returns>
        void ChkAndSavePOFile(string hpFileKey, int POId, string POGUID, int? PODetailId = null)
        {
            HttpPostedFileBase hpFile = Request.Files[hpFileKey];
            string otherIssue = "Unable to upload file";
            FileIO.result uploadResult = FileIO.UploadAndSave(hpFile, POId, POGUID, ref otherIssue, PODetailId);

            #region Add error in case of an Upload issue

            switch (uploadResult)
            {
                case FileIO.result.duplicate:
                    ModelState.AddModelError("FileName", "Duplicate file found"); break;
                case FileIO.result.noextension:
                    ModelState.AddModelError("FileName", "File must have an extension"); break;
                case FileIO.result.contentLength:
                    ModelState.AddModelError("FileName", string.Format("File size cannot exceed {0}MB", Config.MaxFileSizMB)); break;
                case FileIO.result.successful: break;
                default://Any other issue
                    otherIssue = otherIssue.Replace("'", "\"").Replace("\\", "\\\\"); // to avoid conflict with scripting
                    ModelState.AddModelError("FileName", otherIssue); break;
            }

            #endregion
        }

        // Get Header File
        [ValidateInput(false)] // SO: 2673850/validaterequest-false-doesnt-work-in-asp-net-4
        public ActionResult GetFile()
        {
            try
            {
                string code = "";
                try { code = Request.QueryString.ToString(); }
                catch (HttpRequestValidationException httpEx)
                { code = Request.RawUrl.Split(new char[] { '?' })[1]; }//SPECIAL CASE for some odd codes!

                string[] data = DecodeQSforFile(code);
                string filename = data[0];

                #region SPECIAL CASE for Async uploaded file
                if (string.IsNullOrEmpty(filename))
                { // Can't use HttpUtility.UrlDecode in CodeStr property 
                    //- because it'll create issues with string.format and js function calls so handle in GetFile
                    data = DecodeQSforFile(HttpUtility.UrlDecode(code));
                    filename = data[0];
                }
                #endregion
                int POID = int.Parse(data[1]);
                string POGUID = (POID > 0 && data.Length < 3)? "" : data[2];//This must parse correctly (if ID > 0 means existingso no need for GUID)
                
                //Send file stream for download
                return SendFile(POID, POGUID, filename);
            }
            catch (Exception ex) { ViewData["Message"] = "File not found"; return View("DataNotFound"); }
        }
        
        // Send file stream for download
        private ActionResult SendFile(int POID, string POGUID, string filename, int? poDetailId = null)
        {
            try
            {
                string filePath = FileIO.GetPOFilePath(POID, POGUID, filename, poDetailId, false);

                if (System.IO.File.Exists(filePath))//AppDomain.CurrentDomain.BaseDirectory 
                    /*System.IO.Path.GetFileName(filePath)//return File("~/" + filePath, "Content-Disposition: attachment;", filename); */
                    return File(filePath, "Content-Disposition: attachment;", filename);
                else/*Invalid or deleted file (from Log)*/
                { ViewData["Message"] = "File not found"; return View("DataNotFound"); }

            }
            catch (Exception ex) { return View(); }
        }

        /// <summary>
        /// Decode querystring for file download link
        /// </summary>
        /// <param name="code">string to be decoded</param>
        /// <returns>array of string</returns>
        private string[] DecodeQSforFile(string code)
        {
            if (string.IsNullOrEmpty(code)) return new string[] { };
            // IMP: Make sure to encode & decode URL otherwise browser will try to do it and might parse wrong
            code = HttpUtility.UrlDecode(code); // Decoding twice creates issue for certain codes
            return Crypto.EncodeStr(code, false).Split(new char[] { POFile.sep[0] });
        }

        #endregion
    }
}

namespace POT.DAL
{
    public class FileVM
    {
        public POFile EmptyFileHeader { get; set; }
        public POFile FileToAdd { get; set; }
        public List<POFile> AllFiles { get; set; }
        public IEnumerable FileTypes { get; set; }
    }
}