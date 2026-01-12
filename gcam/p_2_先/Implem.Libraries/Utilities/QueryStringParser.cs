using System;
using System.Collections.Specialized;
using System.Net;

namespace Implem.Libraries.Utilities
{
    public static class QueryStringParser
    {
        public static NameValueCollection Parse(string query)
        {
            var nvc = new NameValueCollection();
            if (string.IsNullOrEmpty(query)) return nvc;
            var q = query;
            if (q.StartsWith("?")) q = q.Substring(1);
            foreach (var part in q.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var idx = part.IndexOf('=');
                if (idx >= 0)
                {
                    var name = WebUtility.UrlDecode(part.Substring(0, idx));
                    var value = WebUtility.UrlDecode(part.Substring(idx + 1));
                    nvc.Add(name, value);
                }
                else
                {
                    var name = WebUtility.UrlDecode(part);
                    nvc.Add(name, string.Empty);
                }
            }
            return nvc;
        }
    }
}
