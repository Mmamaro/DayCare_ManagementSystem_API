namespace DayCare_ManagementSystem_API.Models
{
    public class DBSettings
    {
        public string DocumentsMetadataCollection { get; set; }
        public string UsersCollection { get; set; }
        public string ApplicationsCollection { get; set; }
        public string RefreshTokenCollection { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string StudentsCollection { get; set; }
        public string maintenanceCollection { get; set; }
        public string DropOffPickUpEventsCollection { get; set; }
        public string UserAuditCollection { get; set; }
    }
}
