namespace ProjectReport.Models
{
    public class Report : BaseModel
    {
        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _status = "Draft";
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private int? _assignedToUserId;
        public int? AssignedToUserId
        {
            get => _assignedToUserId;
            set => SetProperty(ref _assignedToUserId, value);
        }
    }
}
