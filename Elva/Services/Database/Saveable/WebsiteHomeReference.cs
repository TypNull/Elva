namespace Elva.Services.Database.Saveable
{
    internal enum ReferenceType
    {
        New,
        Recommended,
    }

    internal class WebsiteHomeReference : SaveableOnlineData
    {
        public string WebsiteID { get; set; } = string.Empty;

        public ReferenceType Type { get; set; } = ReferenceType.New;
    }
}
