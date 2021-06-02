var Pages = {};
Pages.Controls = {};

var ModalPopupControl = function (modalId, closeId, okId, isManagePopup, beforeSaveDataCallback, afterSaveDataCallback) {
    this.ModalId = modalId;
    this.CloseId = closeId;
    this.OkId = okId;
    this.IsManagePopup = isManagePopup.toLowerCase() === "true";
    this.BeforeSaveDataCallback = eval(beforeSaveDataCallback);
    this.AfterSaveDataCallback = eval(afterSaveDataCallback);

    this.Init();
}

ModalPopupControl.prototype.Init = function () {
    var self = this;

    $("#" + this.OkId).click(function () {
        if (self.IsManagePopup) {
            self.SaveData();
        }
    });

    $("#" + this.CloseId).click(function () {
        self.Close();
    });

    $("#" + this.ModalId).on("hidden.bs.modal", function () {
        self.InitModalForm();
    });
};

ModalPopupControl.prototype.Close = function () {
    window.Helper.HidePopup(this.ModalId);
};

ModalPopupControl.prototype.Show = function (title, okButtonText) {

    window.Helper.ShowPopup(this.ModalId, title, okButtonText);
};

ModalPopupControl.prototype.SaveData = function () {
    var self = this;
    var form = $("#" + this.ModalId).find("form");
    var formUrl = form.attr("action");
    var formData = form.serialize();

    if (self.BeforeSaveDataCallback && typeof (self.BeforeSaveDataCallback) === "function") {
        self.BeforeSaveDataCallback();
    }

    $.ajax({
        url: formUrl,
        type: "POST",
        data: formData,
        success: function (response) {
            if (self.AfterSaveDataCallback && typeof (self.AfterSaveDataCallback) === "function") {
                self.AfterSaveDataCallback(response);
            }
        },
        error: function (response, status, error) {
            window.Helper.ShowError(error);
        }
    });
};

ModalPopupControl.prototype.InitModalForm = function () {
    var form = $("#" + this.ModalId).find("form")[0];
    $(form).find("input, textarea")
         .val("")
         .removeAttr("checked")
         .removeAttr("selected");
};
/* enter keypress */
$(document).keypress(function (e) {
    if (e.which === 13 || e.which === 27) {
        for (var modalName in window.Pages.Controls) {
            var modal = window.Pages.Controls[modalName];
            var isModalOpened = modal != null ? $("#" + modal.ModalId).is(":visible") : false;

            if (isModalOpened) {
                $("#" + modal.OkId).click();
                return false;
            }
        }
        return true;
    }
});