function ShowImage(imghref) {
    window.Helper.ShowNotificationPopup('<img width="570px" height="auto" src="' + imghref + '" />', window.HelperItem.NotificationType.Image);
}

function EnableDarkTheme(){
    DarkReader.enable({ brightness: 100, contrast: 90, sepia: 10 });
    localStorage.setItem("theme", "dark");
    document.querySelector("#themeText").innerHTML = "Dark";
}
function DisableDarkTheme(){
    DarkReader.disable();
    localStorage.setItem("theme", "light");
    document.querySelector("#themeText").innerHTML = "Light";
}
function ToggleTheme(){
    // Check if Dark Reader is enabled.
    const isEnabled = DarkReader.isEnabled();
    // Stop watching for the system color scheme.
    // DarkReader.auto(false);
    if (isEnabled)
        DisableDarkTheme();
    else
        EnableDarkTheme();
}

jQuery(function ($) {
    $(document).ajaxStop(function () {
        $("#ajax_loader").hide();
    });
    $(document).ajaxStart(function () {
        $("#ajax_loader").show();
    });

    const currentTheme = localStorage.getItem("theme");
    // if (currentTheme === undefined){
    //     // Enable when the system color scheme is dark.
    //     DarkReader.auto({ brightness: 100, contrast: 90, sepia: 10 });
    // }
    // else 
    if (currentTheme === "dark") {
        EnableDarkTheme();
    }
    // Get the generated CSS of Dark Reader returned as a string.
    // const CSS = await DarkReader.exportGeneratedCSS();
});
