﻿using Atlas_Web.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Diagnostics.Contracts;





namespace Atlas_Web.Helpers
{

    public static class UrlHelpers
    {
        [Pure]
        public static string SetParameters(HttpContext helper, IDictionary<string, string> parameters)
        {
            var qs = QueryHelpers.ParseQuery(helper.Request.QueryString.Value ?? "");
            // if we are changing type, we should remove all other filters.
            if (parameters.ContainsKey("type"))
            {
                foreach (var p in qs)
                {
                    if (p.Key != "type" && p.Key != "Query" && !(parameters["type"] == "reports" && p.Key == "report_type_text"))
                    {
                        qs.Remove(p.Key);
                    }

                }
                foreach (var p in parameters)
                {
                    // if we need an "uncheck" url, the pop the key.
                    if (qs.ContainsKey(p.Key) && qs[p.Key] == p.Value)
                    {
                        qs.Remove(p.Key);
                    }
                    else
                    {
                        qs[p.Key] = p.Value;
                    }
                }
            }
            else
            {
                foreach (var p in parameters)
                    // if we need an "uncheck" url, the pop the key.
                    if (qs.ContainsKey(p.Key) && qs[p.Key] == p.Value)
                    {
                        qs.Remove(p.Key);
                    }
                    else
                    {
                        qs[p.Key] = p.Value;
                        if (p.Key == "report_type_text")
                        {
                            qs["type"] = "reports";
                        }
                    }
            }
            return helper.Request.Path + QueryString.Create(qs);
        }

        [Pure]
        public static string SetSearchPageUrl(HttpContext helper, SolrAtlasParameters parameter, int pageIndex)
        {
            return SetParameters(helper, new Dictionary<string, string>
            {
                {nameof(SolrAtlasParameters.PageIndex), pageIndex.ToString()}
            });
        }
        [Pure]
        public static string SetSearchFacetUrl(HttpContext helper, SolrAtlasParameters parameter, string facet, string value)
        {
            return SetParameters(helper, new Dictionary<string, string>
            {
                {facet, value}
            });
        }

        [Pure]
        public static bool SetSearchFacetChecked(HttpContext helper, SolrAtlasParameters parameter, string facet, string value) => parameter.Filters.ContainsKey(facet) && parameter.Filters[facet].Contains(value);

        [Pure]
        public static bool HasFacet(HttpContext helper, SolrAtlasParameters parameter, string facet) => parameter.Filters.ContainsKey(facet);

    }
}
