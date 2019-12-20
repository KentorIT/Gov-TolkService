namespace Tolk.Web.Helpers
{
    public class ColumnDefinition
    {
        public bool IsIdColumn { get; set; }
        public string Name { get; set; }
        public string Data { get; set; }
        public string ColumnName { get; set; }
        public string Title { get; set; }
        public bool Sortable { get; set; }
        public bool SortOnWebServer { get; set; }
        public bool Visible { get; set; }
        public bool IsLeftCssClassName { get; set; }
        public bool IsOverrideClickLinkUrlColumn { get; set; }
        public static bool Searchable => false;
    }
}
