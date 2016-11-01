var IsCCEditMode = false;
var NextNewCommentID = -1;
var commentsViewModel = function ()
{
    var self = this;
    self.emptyComment = "";

    self.commentToAdd = ko.observable();
    self.allComments = ko.observableArray();
    self.AssignTo = ko.observable();
    self.AssignToVal = ko.observable();

    self.setEdited = function (comment) {
        comment._Edited(!comment._Added());
        comment.PostedOn(Date111);
    };
    self.setEditedFlag = function (comment) {
        comment._Edited(!comment._Added());
        comment.LastModifiedDate(Date111);
        comment.PostedOn(Date111);
    };
    self.addComment = function (comment) {
        if (comment.Comment1() == null || comment.Comment1 == "") {/* http://knockoutjs.com/documentation/event-binding.html */
            showNOTY("Comment is a required field", false);
            return false;
        }
        else {
            if (!IsCCEditMode) {
                //comment.ID(NextNewCommentID);
                self.allComments.push(cloneObservable(comment));
                //self.allComments.push(ko.mapping.fromJS(cloneObservable(comment)));

                self.sendEmailPost(comment);

                NextNewCommentID = NextNewCommentID - 1;
                self.emptyComment.ID = NextNewCommentID; // NOT WORKING as expected

                self.cancelComment(comment); //commentToAdd(cloneObservable(self.emptyCommment));
            }
            else {//HT: if observable is set correctly nothing needs to be done
                /*var index = self.allComments.indexOf(comment);
                self.allComments.remove(comment);
                self.allComments.splice(index, 0, comment);*/
                ;
            }
            $('#tblComments').tableNav(); // for newly created TR
            setFocusEditableGrid("tblComments", false);
        }
        return true; /* because we need to ajax submit the form */
    };

    self.removeSelected = function (comment) {
        if (comment != null) {
            comment._Deleted(true);
            if (comment._Added()) {
                comment._Added(false);
                self.allComments.remove(comment);
            }
        }
    };

    self.unRemoveSelected = function (comment) {
        if (comment != null) // Prevent blanks and duplicates
        {
            comment._Deleted(false);
        }
    };


    self.cancelComment = function (comment) {
        IsCCEditMode = false;
        self.commentToAdd.ID(self.emptyComment.ID); //NextNewCommentID
        self.commentToAdd.Comment1(self.emptyComment.Comment1);
        // WON'T work !self.commentToAdd = ko.mapping.fromJS(self.emptyComment);        
    };

    self.sendEmailPost = function (comment) {
        var _AssignTo = $("#AssignTo").val();
        var _PONumber = $("#PONumber").val();
        var proceed = false;
        proceed = !(comment == null || _AssignTo == null || _PONumber == null);

        if (proceed) {
            $.post(commentsEmailURL, /*@Url.Action("CommentsKOEmail", "PO", new { POGUID = ViewData["POGUID"] })*/
                    {
                        CommentObj: ko.mapping.toJSON(comment),
                        AssignTo: _AssignTo,
                        PONumber: _PONumber
                    },
                    function (result) {
                        //alert(result); HT: we can notify user if a successful email was sent
                        if(!result.selfNotif)
                        showNOTY(result.msg, result.sendMail);
                        if (result.sendMail) {
                            $("#AssignToOLD").val(_AssignTo).trigger("change");
                        }
                    }
            );
        }
    };

    self.saveToServer = function () {
        ko.utils.postJson(location.href, { comments: ko.mapping.toJS(self.allComments) }); //ko.toJSON(self.allComments)
        return false;
    };
};
var viewModelComments = new commentsViewModel();
function createCommentsKO(data, AssignTo)
{
      
    if (data.CommentToAdd.ID != -1) 
    data.CommentToAdd.ID = NextNewCommentID;

    viewModelComments.emptyComment = data.EmptyComment; /* THIS SHUD NOT BE AN OBSERVABLE*/
    viewModelComments.AssignTo(data.AssignTo);   $("#AssignToOLD").val(data.AssignTo);
    viewModelComments.AssignToVal("");

    viewModelComments.commentToAdd = ko.mapping.fromJS(data.CommentToAdd);
    viewModelComments.allComments = ko.mapping.fromJS(data.AllComments); //, mapping);
    viewModelComments.Users = ko.mapping.fromJS(data.Users);

    ko.applyBindings(viewModelComments, document.getElementById("divComments"));
}

/*function doCmtDelPost(comment) {
    var data = {}; data[txtId] = txtVal;
    var url = CommentDeleteURL; //'@Html.Raw(Url.Action("CommentKODelete", "PO", new { POGUID = ViewData["POGUID"] }))';
    $.post(url, data);
    return false; // prevent any postback
}*/

/* HT:CAUTION - using document.ready causes duplicate call which replicates the binding! */
function setAssignedTo(ddl) {
    /*var ddlID = $(ddl).val();
    var ddlTXT = $(ddl).children("option").filter(":selected").text();
    $(ddl).parent().children("input:first").val(ddlTXT).trigger("change");*/

    setDDLtext(ddl, 'AssignToVal');
    $("#AssignTo1").val($(ddl).val()).trigger("change");

    //$("#AssignTo1").val(ddlID).trigger("change");
}
/* -------------------------- */
var IsFHEditMode = false;
var NextNewFileID = -1;
var filesHeaderModel = function () {
    var self = this;

    self.emptyFile = "";

    self.fileToAdd = ko.observable();
    self.allFiles = ko.observableArray(); // Initial items

    self.TriggerOpenWin = function (fileFH) {
        openWin(FileGetURL + fileFH.CodeStr(), 1, 1);
    }

    self.setEdited = function (file) {
        file._Edited(!file._Added()); //alert(ko.mapping.toJSON(ko.mapping.fromJS(new Date())()));
        file.LastModifiedDate(Date111); //UploadDate Date111 ko.mapping.fromJS(new Date())
    }
    self.setEditedFlag = function (file) {
        file._Edited(!file._Added());
        file.LastModifiedDate(Date111);
    }
    self.addFile = function (file) {
        try  
        {
        if (file.FileName == null || file.FileName == "") {
            //http://knockoutjs.com/documentation/event-binding.html
            showNOTY("Please select a file to upload", false); return false;
        }
        else {
            // SO: 857618/javascript-how-to-extract-filename-from-a-file-input-control
            file.FileName = file.FileName.split(/(\\|\/)/g).pop(); //file.FileName.replace("C:\\fakepath\\","");
            if (!IsFHEditMode) {
                file.ID = NextNewFileID;
                self.allFiles.push(ko.mapping.fromJS(cloneObservable(file)));
                NextNewFileID = NextNewFileID - 1;
                self.emptyFile.ID = NextNewFileID; // NOT WORKING as expected
            }
            else { /* Editmode Handled by KO */; }
            $('#tblFilesH').tableNav(); // for newly created TR
            setFocusEditableGrid("tblFilesH", false);
        }
        } catch (ex) { showNOTY(ex + ":" + ex.Message, false); }
        return true; // for ajax submit
    };

    self.removeSelected = function (file) {
        if (file == null) return false;

        if (!file._Added()) { file._Deleted(true); return false; }

        var data = {}; data["delFH"] = ko.mapping.toJSON(file);
        //var url = FileDeleteURL + 'POGUID=' + POGUID;
        $.post(FileDeleteURL, data, function (data, textStatus, jqXHR) {
            file._Deleted(true);
            file._Added(false);
            self.allFiles.remove(file);
        }
        );
    };

    self.unRemoveSelected = function (file) {
        if (file != null) {
            file._Deleted(false);
        }
    };


    self.cancelFile = function (file) {
        IsFHEditMode = false;
        self.fileToAdd(cloneObservable(self.emptyFile));
    };

    self.saveToServer = function () {
        ko.utils.postJson(location.href, { files: ko.mapping.toJS(self.allFiles) }); //ko.toJSON(self.allFiles)
        return false;
    }
};

var viewModelFH = new filesHeaderModel();
 function createFilesHeaderKO(data) {
    if (data.FileToAdd.ID != -1) data.FileToAdd.ID = NextNewFileID;

    viewModelFH.emptyFile = data.EmptyFileHeader; // THIS SHUD NOT BE AN OBSERVABLE
    viewModelFH.fileToAdd(data.FileToAdd);

     if (data.AllFiles != null)
         viewModelFH.allFiles = ko.mapping.fromJS(data.AllFiles);
     else
         viewModelFH.allFiles = ko.observableArray();

     viewModelFH.FileTypes = ko.mapping.fromJS(data.FileTypes);
     ko.applyBindings(viewModelFH, document.getElementById("divFiles"));
 }
