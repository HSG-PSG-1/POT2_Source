using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using System.IO;

namespace HSG.Helper
{
    public class FileIO
    {
        #region Variables

        public const char webPathSep = '/', fileNameSep = '~';
        public static string sep = ";", fileNameSearchPattern = "{0}" + fileNameSep + "*.*"; // must match GetFileName
        public static readonly char dirPathSep = System.IO.Path.DirectorySeparatorChar;

        public enum result
        {
            successful,
            emptyNoFile,
            fileUploadIssue,
            contentLength,
            duplicate,
            noextension
        }

        static string GetHD(bool? isHdr = true)
        {
            return (isHdr ?? true) ? "H" : "D";
        }

        #endregion

        #region Upload

        public static result UploadAndSave(HttpPostedFileBase upFile, int POID, string POGUID,ref string OtherIssue, int? DetailId)
        {
            #region Init variables

            string dir = (POID > 0) ? POID.ToString() : POGUID;
            string subDir = GetHD(!DetailId.HasValue);
            string fullPath = Config.UploadPath;
            bool hasDetail = (DetailId != null);
            result resultIO = result.emptyNoFile;
            string ext = Path.GetExtension(upFile.FileName);//Get extension
            string fileName = upFile.FileName;//Get only file name

            #endregion

            #region Issue with file name/ path / extension

            if (upFile == null || string.IsNullOrEmpty(upFile.FileName) || upFile.ContentLength < 1)
                return resultIO;
            else if (string.IsNullOrEmpty(ext))
                //Security (review in future)http://www.dreamincode.net/code/snippet1796.htm
                //if (ext.ToLower() == "exe" || ext.ToLower() == "ddl")
                return result.noextension;

            if (upFile.ContentLength > Config.MaxFileSizMB * 1024 * 1024)
                return result.contentLength;

            #endregion

            try
            {
                fullPath = CheckAndCreateDirectory(Config.UploadPath, dir, subDir, (hasDetail ? DetailId.ToString() : ""));
                // Gen doc name
                fileName = GetFileName(fileName, POID, POGUID, DetailId);
                // Check file duplication
                if (File.Exists(Path.Combine(fullPath, upFile.FileName)) || File.Exists(Path.Combine(fullPath, fileName))) // skip checkto allow the user to overwrite a new version
                    return result.duplicate;//Duplicate file exists!

                // All OK - so finally upload
                upFile.SaveAs(Path.Combine(fullPath, fileName));//Save or Overwrite the file
                //reset original filename

            }
            catch (Exception ex) 
            { 
                OtherIssue = ex.Message + " " + (ex.InnerException??new Exception()).Message; 
                return result.fileUploadIssue; 
            }

            return result.successful;
        }

        #endregion

        #region Check / Create / Delete Directory & File

        public static string CheckAndCreateDirectory(string uploadPath, params string[] directories)
        {
            foreach (string dir in directories)
            {
                if (string.IsNullOrEmpty(dir.Trim())) continue;
                uploadPath = Path.Combine(uploadPath, dir);
                if (!Directory.Exists(uploadPath))//Check and create directory
                    Directory.CreateDirectory(uploadPath);
            }
            return uploadPath;
        }

        /// <summary>
        /// Depth-first recursive delete, with handling for descendant 
        /// directories open in Windows Explorer.
        /// </summary>
        public static void DeleteDirectory(string path)
        {
            if (!Directory.Exists(path) || path == Config.UploadPath)
                return; // avoid worst cases

            string[] files = Directory.GetFiles(path);
            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string directory in Directory.GetDirectories(path))
                DeleteDirectory(directory);

            try { Directory.Delete(path, true); }
            catch (IOException) { Directory.Delete(path, true); }
            catch (UnauthorizedAccessException) { Directory.Delete(path, true); }
        }

        #endregion

        #region PO File specific functions
        /*public static string Merge(string uri1, string uri2)
        {
            uri1 = uri1.TrimEnd('/');
            uri2 = uri2.TrimStart('/');
            return string.Format("{0}/{1}", uri1, uri2);
        }*/

        public static string MergePath(string basePath, bool webURL, params string[] paths)
        {
            string fullPath = basePath;
            foreach (string dir in paths)
            {
                if (dir.Trim().Length > 0)
                {
                    if (!webURL)
                        fullPath = Path.Combine(fullPath, dir);
                    else // is web url
                        fullPath = fullPath + dirPathSep + dir;
                }
            }
            return fullPath;
        }

        public static string GetFileName(string FileName, int POID, string POGUID = "", int? PODetailID = null)
        {
            if ((FileName ?? "").Trim().Length == 0) return string.Empty;

            if (POID > 0 && (PODetailID ?? 1) > 0) // if its a new PO then NO need to change file name beause at the end we'll directly change the GID folder to ID
                FileName = (POGUID.Length > 0) ? (POGUID + fileNameSep + FileName) : FileName; // GUID is sent only if its web and temp

            return FileName;
        }

        public static string GetPOFilePath(int POID, string POGUID, string FileName, int? PODetailID = null, bool webURL = false)
        {
            if (string.IsNullOrEmpty(FileName) && webURL)
                return "#";

            string basePath = GetPOFilesDirectory(POID, POGUID, PODetailID, webURL);
            return MergePath(basePath, webURL, GetFileName(FileName, POID, POGUID, PODetailID));
        }

        public static string GetPOFilesDirectory(int POID, string POGUID, int? PODetailID = null, bool webURL = false)
        {
            string basePath = webURL ? Config.DownloadUrl : Config.UploadPath;
            string POdir = (POID > 0) ? POID.ToString() : POGUID;
            bool isHdr = !PODetailID.HasValue;
            string detailDir = !isHdr ? PODetailID.Value.ToString() : "";

            return MergePath(basePath, webURL, POdir, GetHD(isHdr), detailDir);
        }

        public static bool DeletePOFile(int POID, string POGUID, string fileName, int? PODetailID = null)
        {
            try
            {
                string FilePath = GetPOFilePath(POID, POGUID, fileName, PODetailID);

                if (File.Exists(FilePath))
                    File.Delete(FilePath);

                return true; // HT: If file doesn't exist - we need not worry to delete it!

            }
            catch { return false; }
        }

        #endregion

        #region Move / Cleanup / Get File download code

        public static void MoveFilesFolderNewPOorItem(int POID, string POGUID, int? OldPODetailID = null, int? PODetailID = null)
        {
            // If its a new PO - just need to rename GUID to NewID folder (same for new item folder) and rename PODetailId
            //  For H (invoke only once)
            //  For D (invoke for each PODetailId) : rename -1 to PODetailID
            string sourcePath = Directory.GetParent(GetPOFilesDirectory(0, POGUID)).FullName;

            if (Directory.Exists(sourcePath)) // GUID directory exists means its a new PO
            {
                string targetPath = Directory.GetParent(GetPOFilesDirectory(POID, "")).FullName;
                new DirectoryInfo(sourcePath).MoveTo(targetPath);
                if (!PODetailID.HasValue) return; // header so return
            }
            // Only for Detail files
            sourcePath = GetPOFilesDirectory(POID, "", OldPODetailID);
            if (OldPODetailID < 1) // POID < 1 is never possible because we always set the new POId in child objects
            {
                if (Directory.Exists(sourcePath))
                    new DirectoryInfo(sourcePath).MoveTo(GetPOFilesDirectory(POID, "", PODetailID));
                return;
            }
        }

        public static void StripGUIDFromPOFileName(int POID, string POGUID, int? OldPODetailID = null, int? PODetailID = null)
        {
            // If its a new PO use - MoveFilesFolderNewPOOrItem
            //  For H : rename each GUID_fileH.ext to fileH.ext
            //  For D : rename each GUID_fileD.ext to fileD.ext            

            string sourcePath = GetPOFilesDirectory(POID, "", PODetailID);
            // Only for existing H or D
            if (Directory.Exists(sourcePath))
            {
                foreach (FileInfo fi in GetFiles(sourcePath, POGUID))
                    fi.MoveTo(Path.Combine(sourcePath, fi.Name.Replace(POGUID + fileNameSep, "")));
            }
        }
        public static void CleanTempUpload(int POID, string POGUID)
        {
            string sourcePath = GetPOFilesDirectory(POID, POGUID);

            if (POID < 1) // new PO so delete GUID directory
            { DeleteDirectory(Directory.GetParent(sourcePath).FullName); return; }

            // Header temp cleanup
            if (Directory.Exists(sourcePath))
                foreach (FileInfo fi in GetFiles(sourcePath, POGUID))
                    fi.Delete();
            // Detail temp cleanup
            sourcePath = Path.Combine(Directory.GetParent(sourcePath).FullName, GetHD(false));
            if (!Directory.Exists(sourcePath)) return; // No 'D'etail folder
            foreach (string delPath in Directory.GetDirectories(sourcePath))
            {
                int PODetailID = 0;
                //same as dir - string delPath = Path.Combine(sourcePath, dir);
                if (Directory.Exists(delPath))
                {
                    string folderID = new DirectoryInfo(delPath).Name;
                    if (int.TryParse(folderID, out PODetailID) && PODetailID < 0)
                        DeleteDirectory(delPath); // delete temp directories like -1, -2
                    else
                        foreach (FileInfo fi in GetFiles(delPath, POGUID))
                            fi.Delete();
                }
            }
        }

        public static FileInfo[] GetFiles(string sourcePath, string POGUID)
        {
            string searchPattern = String.Format(fileNameSearchPattern, POGUID);
            if (Directory.Exists(sourcePath))
                return new DirectoryInfo(sourcePath).GetFiles(searchPattern);
            else
                return new FileInfo[] { }; // path NOT found so return empty
        }

        public static string getFileDownloadCode(string FileName, int POID, string POGUID)
        {
            string codeStr = FileName + sep + POID.ToString() + sep + POGUID.ToString();
            codeStr = HttpUtility.UrlEncode(Crypto.EncodeStr(codeStr.ToString(), true));
            // Make sure you do UrlEncode TWICE in code to get the code!!!
            return codeStr;
        }

        public static string getFileDownloadActionCode(string FileName, int POID, int? PODetailID)
        {
            bool isDetailFile = PODetailID.HasValue;

            System.Text.StringBuilder codeStr = new System.Text.StringBuilder(FileName + sep + POID.ToString());
            if (isDetailFile) codeStr.Append(sep + PODetailID.ToString());

            // Make sure you do UrlEncode TWICE in code to get the code!!!
            return (isDetailFile ? "GetFileD?" : "GetFile?") + HttpUtility.UrlEncode(Crypto.EncodeStr(codeStr.ToString(), true));
        }

        #endregion
    }
}
