using System.Web.Mvc;

namespace jail.Models.Controls
{
    public class ModalPopupControlViewModel
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public string Template { get; set; }

        public string ModalPopupText { get; set; }

        public ViewDataDictionary TemplateViewData { get; set; }

        public string OkButtonText { get; set; }

        public string CloseButtonText { get; set; }

        public bool IsManagePopup { get; set; }

        public string BeforeSaveCallback { get; set; }

        public string AfterSaveCallback { get; set; }

        public string CloseId
        {
            get
            {
                return CloseButtonText == null ? "null" : string.Format("{0}CloseButton", Id);
            }
        }

        public string OkId
        {
            get
            {
                return OkButtonText == null ? "null" : string.Format("{0}OkButton", Id);
            }
        }

        public bool IsFooterExist
        {
            get
            {
                return OkButtonText != null || CloseButtonText != null;
            }
        }

        public ModalPopupControlViewModel(string id)
        {
            Id = id;
            ModalPopupText = string.Empty;
            IsManagePopup = false;
        }
    }
}