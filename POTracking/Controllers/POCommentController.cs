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
        [HttpPost]
        public JsonResult CommentsKOEmail(int POID, /*string POGUID,*/ int AssignTo, string PONumber, [FromJson] POComment CommentObj)
        {
            bool sendMail = (POID > Defaults.Integer && AssignTo != _SessionUsr.ID);// No need to send mail if its current user
            bool selfNotif = !sendMail;
            string msg = sendMail ? "Email queued for new comment" : "Self notification : No email queued";
            try
            {
                #region Check and send email
                if (sendMail)
                {// No need to send mail if its current user
                    string UserEmail = new UserService().GetUserEmailByID(AssignTo);
                    sendMail = MailManager.AssignToMail(PONumber, CommentObj.Comment1,
                        POID, UserEmail, (_SessionUsr.UserName), true);
                }
                #endregion
            }
            catch (Exception ex) { sendMail = false; msg = ex.Message; }

            HttpContext.Response.Clear(); // to avoid debug email content from rendering !
            return Json(new { sendMail, msg, selfNotif }, JsonRequestBehavior.AllowGet);
        }

        public CommentVM GetCommentKOModel(int POID, string POGUID, int AssignTo)
        {
            //Set Comment object
            POComment newObj = new POComment()
            {
                ID = -1,
                _Added = true,
                POID = POID,
                POGUID = POGUID,
                CommentBy = _SessionUsr.Email,//UserName,
                LastModifiedBy = _SessionUsr.ID,
                LastModifiedDate = DateTime.Now,
                PostedOn = DateTime.Now,
                UserID = _SessionUsr.ID
            };

            CommentVM vm = new CommentVM()
            {
                CommentToAdd = newObj,
                EmptyComment = newObj,
                AllComments = new CommentService().Search(POID, null),
                AssignTo = AssignTo
            };

            vm.Users = new LookupService().GetLookup(LookupService.Source.User);

            return vm;
        }
    }
}

namespace POT.DAL
{
    public class CommentVM
    {
        public POComment EmptyComment { get; set; }
        public POComment CommentToAdd { get; set; }
        public List<POComment> AllComments { get; set; }
        public IEnumerable Users { get; set; }
        public int? AssignTo { get; set; }
    }
}