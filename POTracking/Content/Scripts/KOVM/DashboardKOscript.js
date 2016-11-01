$().ready(function () {
    doCollapse(); //If url has collapse        

    renderAutoComplete(autocompleteURL + "Brand", "#BrandID", "#BrandName");
    renderAutoComplete(autocompleteURL + "Vendor", "#VendorID", "#VendorName");

    // Configure Date picker plugin
    createToFromjQDTP("#PODateFrom", "#PODateTo");
    createToFromjQDTP("#ETDFrom", "#ETDTo");
    createToFromjQDTP("#ETAFrom", "#ETATo");
});

function setDTPdateForKO() {
    //Special case for dates handling and prevent JSON data conversion issues
    $("#PODateFrom").val($("#PODateFrom1").val()).trigger("change");
    $("#PODateTo").val($("#PODateTo1").val()).trigger("change");

    $("#ETAFrom").val($("#ETAFrom1").val()).trigger("change");
    $("#ETATo").val($("#ETATo1").val()).trigger("change");

    $("#ETDFrom").val($("#ETDFrom1").val()).trigger("change");
    $("#ETDTo").val($("#ETDTo1").val()).trigger("change");
}

function resetForm(btn) {
    var vendorName = vm_D.search.VendorName();
    var vendorID = vm_D.search.VendorID();

    clearForm(document.getElementById('frm'));
    document.getElementById('doReset').checked = true;
    resetDatepicker('#PODateFrom1, #PODateTo1');

    //trigger changePOStatusPost for KO binding notification
    vm_D.search.PONumbers(null);
    vm_D.search.OrderStatusID(0);
    vm_D.search.AssignTo(0);
    if (!isVendor) {
        vm_D.search.VendorName(null);
        vm_D.search.VendorID(0);
    }
    else {
        vm_D.search.VendorName(vendorName);
        vm_D.search.VendorID(vendorID);
    }
    vm_D.search.BrandName(null);
    vm_D.search.ShipToCity(null);

    vm_D.search.PODateFrom(null);
    vm_D.search.PODateTo(null);
    vm_D.search.ETAFrom(null);
    vm_D.search.ETATo(null);
    vm_D.search.ETDFrom(null);
    vm_D.search.ETDTo(null);
}

function showDialog(action, poID) {
    var containerID = "#dialog" + poID;
    $(containerID).dialog({
        modal: false,
        open: function () {
            $(this).html(loading);
            $(this).load(dashboardURL + action + '?POID=' + poID, function () { $("#NewStatusID").focus(); }); //+ '&Archived=' + archived
            /*setTimeout(function () { $('.ui-dialog-titlebar-close').blur(); }, 1);*/
        },
        height: 360,
        width: 650,
        title: action,
        close: function (event, ui) { //NOTE: Required so that multiple dialog controls can be references with the same id
            $(this).dialog("destroy").empty();
        } //Can't use .remove() as in SO: 6515052 so we empty the html.
    });
}

var lastTR = "#tblStatusHistory tbody>tr:last";
var oldStat = "#OldStatus", oldStatID = "#OldStatusID", newStat = "#NewStatus", newStatID = "#NewStatusID";
function changePOStatusPost(POId, OldStatusID, NewStatusID) {
    var data = {}; //Set data to be posted back
    data["POId"] = POId; data["OldStatusID"] = OldStatusID; data["NewStatusID"] = NewStatusID;

    var url = ChangePOStatusURL.replace('-99', POId);
    $.post(url, data);

    return false; // prevent any postback
}

var poObj = ""; //will be set whn the cell is clicked

function updateStatusHistory() {
    //clone and insert the tr after the table
    $(lastTR).clone(true).insertAfter(lastTR);
    //Update data
    $(lastTR).find('td:first').html(usrName);

    $(lastTR).find('td:first').next().html(todayDT);
    $(lastTR).find('td:last').prev().html($(oldStat).val());
    $(lastTR).find('td:last').html($(newStatID).children("option:selected").text());

    $(lastTR).effect('highlight', {}, 1000); //highlight

    //persist updated status in textbox
    $(oldStatID).val($(newStatID).val());
    $(oldStat).val($(newStatID).children("option:selected").text());

    /*update in Grid (NOTE: make sure 'poObj' is set when td is clicked*/

    $("#statusDIV" + poObj.ID).text($(oldStat).val()).trigger("change").effect('highlight', {}, 2000);
    poObj.Status = $(oldStat).val(); poObj.OrdStat = $(newStatID).val();
}

function excelPostback(e) {
    skipCommitChk = true;
    $.ajax({
        type: "POST",
        //contentType: "application/json; charset=utf-8",
        //dataType: "json",
        data: $("#frm").serialize(),
        url: setSearchOptsURL,
        success: function (data) { $("#frmExcel").submit(); }
    });
    return false; // to cause form postback
}
function openPrintDialog(POId) {
    if (POId > 0) return openWinScrollable(printPOURL.replace('-99', POId), 648, 838);
}

function doAJAXSubmit(frm) { vm_D.invokeSearch(1); return false; }

/* ============== ============== ============== ============== */

var vmDashboard = function () {
    var self = this;
    self.fields = ko.observableArray(); //jsondata
    self.pageSize = ko.observable(gridPageSize);
    self.pageIndex = ko.observable(0);
    self.cachedPagesTill = ko.observable(0);
    self.sortField = ko.observable("POno");
    self.sortOrderNxtAsc = ko.observable(true);
    self.search = ko.observable();
    self.invokeSearch = ko.observable(2);
    /*self.quickSearch =  ko.observable("");*/

    self.previousPage = function () {
        self.pageIndex(self.pageIndex() - 1);
        if (self.cachedPagesTill() < 1)
            self.cachedPagesTill(0);
        self.cachedPagesTill(self.cachedPagesTill() + 1);
    };
    self.nextPage = function () {
        self.pageIndex(self.pageIndex() + 1);
        //if(self.cachedPagesTill() < 1)
        //    updatePagedRows(self);
        self.cachedPagesTill(self.cachedPagesTill() - 1);
    };

    self.filteredRecords = ko.computed(function () {
        var s = self.invokeSearch(); self.invokeSearch(0);

        var s = self.search;

        var PONumbers = (s.PONumbers != null && s.PONumbers() != null && s.PONumbers() != "") ? s.PONumbers().toLowerCase() : null;
        var OrderStatusID = (s.OrderStatusID != null && s.OrderStatusID() != null && s.OrderStatusID() != "") ? s.OrderStatusID() : 0;
        var AssignTo = (s.AssignTo != null && s.AssignTo() != null && s.AssignTo() != "") ? s.AssignTo() : 0;
        var VendorName = (s.VendorName != null && s.VendorName() != null && s.VendorName() != "") ? s.VendorName().toLowerCase() : null;
        //var VendorID = (s.VendorID != null && s.VendorID() != null && s.VendorID() != "") ? s.VendorID() : 0;

        var BrandName = (s.BrandName != null && s.BrandName() != null && s.BrandName() != "") ? s.BrandName().toLowerCase() : null;
        var ShipToCity = (s.ShipToCity != null && s.ShipToCity() != null && s.ShipToCity() != "") ? s.ShipToCity().toLowerCase() : null;

        var PODateFrom = (s.PODateFrom != null && s.PODateFrom() != "") ? s.PODateFrom() : null;
        var PODateTo = (s.PODateTo != null && s.PODateTo() != "") ? s.PODateTo() : null;

        var ETAFrom = (s.ETAFrom != null && s.ETAFrom() != "") ? s.ETAFrom() : null;
        var ETATo = (s.ETATo != null && s.ETATo() != "") ? s.ETATo() : null;

        var ETDFrom = (s.ETDFrom != null && s.ETDFrom() != "") ? s.ETDFrom() : null;
        var ETDTo = (s.ETDTo != null && s.ETDTo() != "") ? s.ETDTo() : null;

        /*var quickSearch = (self.quickSearch != null && self.quickSearch() != null && self.quickSearch() != "")?self.quickSearch().toLowerCase():null;*/

        self.pageIndex(0);

        return ko.utils.arrayFilter(self.fields(), function (rec) {
            return (
                    (PONumbers == null || rec.POno.toLowerCase().toString().indexOf(PONumbers) > -1) &&
                    (OrderStatusID < 1 || rec.OrdStat == OrderStatusID) &&
                    (AssignTo < 1 || rec.AssignTo == AssignTo) && /*|| rec.AssignTo == null */

            (VendorName == null || (rec.Vndr != null && rec.Vndr.toLowerCase().indexOf(VendorName) > -1)) &&
                    //(VendorID < 1 || rec.VendorID == VendorID) &&

                    (BrandName == null || rec.Brand.toLowerCase().indexOf(BrandName) > -1) &&
                    (ShipToCity == null || rec.Ship.toLowerCase().indexOf(ShipToCity) > -1) &&
                    (PODateFrom == null || new Date(rec.POdt) >= new Date(PODateFrom)) &&
                    (PODateTo == null || new Date(rec.POdt) <= new Date(PODateTo)) &&
                    (ETAFrom == null || new Date(rec.ETA) >= new Date(ETAFrom)) &&
                    (ETATo == null || new Date(rec.ETA) <= new Date(ETATo)) &&
                    (ETDFrom == null || new Date(rec.ETD) >= new Date(ETDFrom)) &&
                    (ETDTo == null || new Date(rec.ETD) <= new Date(ETDTo))
                );
        });
        /*
        SO: 13229970/knockout-filtering
        return ko.utils.arrayFilter([item.FirstName().toLowerCase(), item.lastName().toLowerCase(), 
        item.email().toLowerCase(), item.company().toLowerCase()], function (str) { return str.indexOf(filter) != -1  }).length > 0;
        */
    });

    self.maxPageIndex = ko.computed(function () {//dependentObservable
        var s = self.invokeSearch(); self.invokeSearch(0);
        return Math.ceil(self.filteredRecords().length / self.pageSize()) - 1;
    });

    self.pagedRows = ko.computed(function () {//dependentObservable
        var size = self.pageSize();
        var start = self.pageIndex() * size;
        return self.filteredRecords().slice(start, start + size);
    });

    self.sortData = function (data, event, sort) {
        if ((self.sortField() == sort))
            self.sortOrderNxtAsc(!self.sortOrderNxtAsc());
        else
        { self.sortField(sort); self.sortOrderNxtAsc(false); }

        var sortOrder = self.sortOrderNxtAsc() ? -1 : 1; // Asc : Desc

        /*"click: function(data,event){fields.sort(function (l, r) { return l.Status > r.Status ? 1 : -1 })}"*/
        switch (sort) {
            case "POno":
                self.fields.sort(function (l, r) { return l.POno > r.POno ? 1 * sortOrder : -1 * sortOrder });
                break;
            case "POdt":
                self.fields.sort(function (l, r) { return new Date(l.POdt) > new Date(r.POdt) ? 1 * sortOrder : -1 * sortOrder }); // PODate
                break;
            case "Vndr": // Need to convert into string while comparison
                self.fields.sort(function (l, r) { return $.trim(l.Vndr) + "" > $.trim(r.Vndr) + "" ? 1 * sortOrder : -1 * sortOrder });
                break;
            case "Ship":
                self.fields.sort(function (l, r) { return $.trim(l.Ship) > $.trim(r.Ship) ? 1 * sortOrder : -1 * sortOrder });
                break;
            case "ETA":
                self.fields.sort(function (l, r) { return new Date(l.ETA) > new Date(r.ETA) ? 1 * sortOrder : -1 * sortOrder });
                break;
            case "ETD":
                self.fields.sort(function (l, r) { return new Date(l.ETD) > new Date(r.ETD) ? 1 * sortOrder : -1 * sortOrder });
                break;
            case "Brand":
                self.fields.sort(function (l, r) { return $.trim(l.Brand) > $.trim(r.Brand) ? 1 * sortOrder : -1 * sortOrder });
                break;
            case "Status":
                self.fields.sort(function (l, r) { return $.trim(l.Status) > $.trim(r.Status) ? 1 * sortOrder : -1 * sortOrder });
                break;
            case "Cmts":
                self.fields.sort(function (l, r) { return l.Cmts > r.Cmts ? 1 * sortOrder : -1 * sortOrder });
                break;
            case "Files":
                self.fields.sort(function (l, r) { return l.Files > r.Files ? 1 * sortOrder : -1 * sortOrder });
                break;
        }

        $(".header tr th").each(function (i) {
            $(this).html($(this).html().replace("▲", ""));
            $(this).html($(this).html().replace("▼", ""));
            /*$(this).html($(this).html().replace("&#9650;","&#9660;"));*/

            if ($(this).html().indexOf(vm_D.sortField()) > -1)
                $(this).html($(this).html() + " " + (vm_D.sortOrderNxtAsc() ? "▼" : "▲"));
        });

        self.pageIndex(0); self.cachedPagesTill(0);
    }
};

var vm_D;

function createKO() { //createKO_withAJAXFetchForFuture
    showDlg(true);
    //var data = $.parseJSON(initialJSONdata);
    vm_D = new vmDashboard();
    showDlg(false);
    //vm_D.POs = ko.observableArray(data); // Initial items
    vm_D.fields(initialJSONdata.records);
    vm_D.search = ko.mapping.fromJS(initialJSONdata.search); // Otherwise the search button will be needed
    //vm_D.quickSearch("");
    vm_D.invokeSearch(2);
    vm_D.pageSize(gridPageSize);
    ko.applyBindings(vm_D);

    $("#loading").hide();
    //pagedGrid.DisplayFields(data);            
    setDTPdateForKO();
    var progressElem = $('#progressCounter');
    // write something in #progressCounter , later will be changed to percentage
    progressElem.text(ListURL);

    $.ajax({
        type: 'GET',
        dataType: 'json',
        url: ListURL,
        cache: false,
        error: function (xhr, ajaxOptions, thrownError) { alert(xhr.responseText); alert(thrownError); },
        xhr: function () {
            var xhr = new window.XMLHttpRequest();
            //Download progress
            xhr.addEventListener("progress", function (evt) {
                console.log(evt.lengthComputable); // false
                if (evt.lengthComputable) {
                    var percentComplete = evt.loaded / evt.total;
                    //if (percentComplete * 100 > 50 && percentComplete * 100 < 60)alert("(" + evt.loaded + ":" + evt.total +")");
                    progressElem.html(Math.round(percentComplete * 100) + "% (" + evt.loaded + ":" + evt.total + ")");
                }
            }, false);
            return xhr;
        },
        beforeSend: function () { $('#loading').html(loading); alert("Look at the record count. Two pages data pre-loaded. Now loading the remaining data."); progressElem.show(); },
        //complete: function () { $("#loading").hide(); },
        success: function (data) {
            //$("#data").html("data receieved");
            ko.utils.arrayPushAll(vm_D.fields, data.records); //SO: 9758490
            vm_D.fields.valueHasMutated(); // http://www.strathweb.com/2012/07/knockout-js-pro-tips-working-with-observable-arrays/        
            progressElem.hide();
        }
    });
}

/*
function createKO() {
    showDlg(true);
    $.getJSON(ListURL, function (data) {
        vm_D = new vmDashboard();
        showDlg(false);
        //vm_D.POs = ko.observableArray(data); // Initial items
        vm_D.fields(data.records);
        vm_D.search = ko.mapping.fromJS(data.search); // Otherwise the search button will be needed
        //vm_D.quickSearch("");
        vm_D.invokeSearch(2);
        vm_D.pageSize(gridPageSize);
        ko.applyBindings(vm_D);
        ko.utils.arrayPushAll(vm_D.fields, data.records); //SO: 9758490
        vm_D.fields.valueHasMutated(); // http://www.strathweb.com/2012/07/knockout-js-pro-tips-working-with-observable-arrays/

        //pagedGrid.DisplayFields(data);            
        setDTPdateForKO();
    }); //.fail(function () {alert("Error loading data!");}); http://api.jquery.com/jquery.getjson/
}*/
/* KO based pagination */
function updatePagedRows(vm) {// Starts with : index=0
    ListURL = ListURL.replace("index=" + (vm.pageIndex() - 1), "index=" + vm.pageIndex());
    showDlg(true);
    $.getJSON(ListURL, function (data) {
        showDlg(false);
        //vm_D.POs = ko.observableArray(data); // Initial items
        //ko.applyBindings(vm_D);
        if (data != null)
            ko.utils.arrayForEach(data, function (item) {
                vm_D.fields.push(item);
            });
        else
            vm_D.pageIndex(0); //reset            
    });
}