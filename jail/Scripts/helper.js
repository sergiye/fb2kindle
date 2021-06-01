function HelperItem() {};

window.HelperItem.prototype.ShowPopup = function (popupId, title, okButtonText) {
    $("#" + popupId).modal('show');

    if (title != null && !(title === "")) {
        $("#" + popupId).find(".modal-title").text(title);
    }

    if (okButtonText != null && !(okButtonText === "")) {
        $("#" + popupId).find(".modal-footer .btn-success").text(okButtonText);
    }

    this.OverlayAllOtherPopups(popupId);
};

window.HelperItem.prototype.HidePopup = function (popupId) {
    $("#" + popupId).modal('hide');
    this.RemovePopupOverlay();
};

window.HelperItem.prototype.OverlayAllOtherPopups = function (popupId) {
    var defaultIndexOfOverlayHolder = "1040";
    $("*[role=dialog]").not("#" + popupId).css("z-index", defaultIndexOfOverlayHolder);
    $("#" + popupId).css("z-index", "");
};

window.HelperItem.prototype.RemovePopupOverlay = function () {
    $("*[role=dialog]").css("z-index", "");
};

window.HelperItem.NotificationType = { "Success": 0, "Error": 1, "Warning": 2, "Custom": 3, "Image": 4 };
window.HelperItem.prototype.ShowNotificationPopup = function (msg, type, callback, customTitle) {
    var self = this;
    var title = "";
    var popup = $("#NotificationPopup");
    var popupContent = popup.find(".modal-content");
    var btn = popup.find("#NotificationPopupOkButton");
    this.ClearNotificationPopup(popupContent, btn);
    popup.find(".modal-header .am-close-spin").click(function () { if (callback) callback(); });
    btn.click(function () { self.HidePopup(popup[0].id); if (callback) callback(); });
    
    switch (type) {
        case window.HelperItem.NotificationType.Success:
            title = "Success";
            popupContent.addClass("success-modal");
            btn.addClass("btn-success");
            break;
        case window.HelperItem.NotificationType.Error:
            title = "Error";
            popupContent.addClass("error-modal");
            btn.addClass("btn-danger");
            break;
        case window.HelperItem.NotificationType.Warning:
            title = "Warning";
            popupContent.addClass("warning-modal state-modal");
            btn.addClass("btn-warning");
            break;
        case window.HelperItem.NotificationType.Custom:
            title = customTitle;
            break;
        case window.HelperItem.NotificationType.Image:
            title = "Image Preview";
            popupContent.addClass("success-modal");
            btn.addClass("btn-success");
            btn.text("Close");
            break;
    }

    popup.find(".modal-title").html(title);
    const body = popup.find(".modal-body");
    body.html(msg);
    body.animate({
        scrollTop: body[0].scrollHeight
    }, 500);
    
    $(document).keydown(function(event) {
        if (event.keyCode === 27) {
            self.HidePopup(popup[0].id);
            if (callback) {
                callback();
            }
        }
    });
    
    this.ShowPopup(popup[0].id);
};

window.HelperItem.prototype.ClearNotificationPopup = function (popupContent, popupBtn) {
    popupContent.removeClass("error-modal warning-modal success-modal state-modal");
    popupBtn.removeClass("btn-danger btn-success btn-warning");
};

window.HelperItem.prototype.ShowError = function (msg, callback) {
    window.HelperItem.prototype.ShowNotificationPopup(msg, window.HelperItem.NotificationType.Error, callback);
};

window.HelperItem.prototype.ShowSuccess = function (msg, callback) {
    window.HelperItem.prototype.ShowNotificationPopup(msg, window.HelperItem.NotificationType.Success, callback);
};

window.HelperItem.prototype.ShowWarning = function (msg, callback) {
    window.HelperItem.prototype.ShowNotificationPopup(msg, window.HelperItem.NotificationType.Warning, callback);
};

window.HelperItem.prototype.ShowCustomNotification = function (msg, customTitle, notificationType, callback) {
    if (notificationType == null) {
        notificationType = window.HelperItem.NotificationType.Custom;
    }
    window.HelperItem.prototype.ShowNotificationPopup(msg, notificationType, callback, customTitle);
};

window.HelperItem.GetUrlParam = function(name) {
  var results = new RegExp("[\?&amp;]" + name + "=([^&amp;#]*)").exec(window.location.href);
  return results[1] || 0;
}

window.HelperItem.prototype.GetBaseDomain = function() {
    var pathArray = location.href.split('/');
    var protocol = pathArray[0];
    var host = pathArray[2];
    var url = protocol + '//' + host;
    var baseContentUrl = $("body").attr("data-baseurl");

    if (!location.origin)
        location.origin = location.protocol + "//" + location.host;

    return location.origin + baseContentUrl;
};

window.HelperItem.prototype.delay = (function () {
    var timer = 0;
    return function (callback, ms) {
        clearTimeout(timer);
        timer = setTimeout(callback, ms);
    };
})();


window.Helper = new HelperItem();

String.prototype.endsWith = function (suffix) {
    return this.indexOf(suffix, this.length - suffix.length) !== -1;
};