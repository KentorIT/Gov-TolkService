using System;

namespace Tolk.BusinessLogic.Utilities
{
    public static class UriExtensions
    {
        public static Uri BuildUri(this Uri uri, string path, string query = null)
        {
            UriBuilder builder = new UriBuilder(uri)
            {
                Path = path,
                Query = query
            };
            return builder.Uri;
        }
    }
}
