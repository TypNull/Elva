namespace Elva.MVVM.Model.Database.Saveable
{
    internal class ComicReference : SaveableOnlineData
    {
        public string ReferenceType { get; set; } = string.Empty;
        public string WebsiteID { get; set; } = string.Empty;
    }
}
