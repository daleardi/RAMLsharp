﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RAMLSharp.Models
{
    public class RAMLModel
    {
        public string Title { get; set; }
        public Uri BaseUri { get; set; }
        public string Version { get; set; }
        public string DefaultMediaType { get; set; }
        public string Description { get; set; }
        public IList<RouteModel> Routes { get; set; }

        public override string ToString()
        {
            var RAML = new StringBuilder();
            RAML = SetRamlRoot(RAML);
            RAML = SetRamlBody(RAML);
            return RAML.ToString();
        }
        
        #region private methods
        private readonly string _newLine = Environment.NewLine;
        
        private StringBuilder SetRamlRoot(StringBuilder RAML)
        {
            //It is faster to use string concat here since we are not looping.
            var UriScheme = BaseUri == null ? "HTTP" : BaseUri.Scheme.ToUpper();
            var rootString = "#%RAML 0.8" + _newLine
                             + "---" + _newLine
                             + "title: " + Title + _newLine
                             + "baseUri: " + BaseUri + _newLine
                             + "protocols: [" + UriScheme + "]" + _newLine;

            if (!string.IsNullOrEmpty(Version))
            {
                rootString += "version: " + Version + _newLine;
            }

            if (!string.IsNullOrEmpty(DefaultMediaType))
            {
                rootString += "mediaType: " + DefaultMediaType + _newLine;
            }

            if(!string.IsNullOrEmpty(Description))
            {
                rootString += "documentation: " + _newLine
                + "  - title: The Description of the API" + _newLine
                + "    content: |" + _newLine
                + "      " + Description + _newLine;
            }

            return RAML.Append(rootString);
        }

        private StringBuilder SetRamlBody(StringBuilder RAML)
        {
            if (Routes == null || Routes.Count == 0) return RAML;

            var routeGrouping = Routes.OrderBy(p => p.UrlTemplate)
                                      .GroupBy(p => p.UrlTemplate, p => p, (key, item) => new { Resource = key, Verbs = item });

            foreach (var urls in routeGrouping)
            {
                RAML = SetResources(RAML, urls);
                RAML = urls.Verbs.Aggregate(RAML, SetRoutes);
            }

            return RAML;
        }

        private StringBuilder SetResources(StringBuilder RAML, dynamic urls)
        {
            RAML.Append("/")
                .Append(urls.Resource)
                .AppendLine(":");
            return RAML;
        }

        private StringBuilder SetRoutes(StringBuilder RAML, RouteModel route)
        {
            RAML = SetUriParameters(RAML, route);
            RAML = SetHttpVerb(RAML, route);
            RAML = SetDescription(RAML, route);
            RAML = SetRequest(RAML, route);
            RAML = SetHeaders(RAML, route);
            RAML = SetParameters(RAML, route);
            RAML = SetResponses(RAML, route);
            return RAML;
        }

        
        
        private StringBuilder SetHttpVerb(StringBuilder RAML, RouteModel route)
        {
            RAML.AppendFormat("  {0}:{1}", route.Verb, _newLine);
            return RAML;
        }

        private StringBuilder SetDescription(StringBuilder RAML, RouteModel route)
        {
            RAML.AppendFormat("    description: {0}{1}", route.Description, _newLine);
            return RAML;
        }

        private StringBuilder SetRequest(StringBuilder RAML, RouteModel route)
        {
            if (route.BodyParameters == null || route.BodyParameters.Count == 0 ) return RAML;
            
            RAML.AppendFormat("    body:{0}", _newLine);
            RAML.AppendFormat("      {0}: {1}", route.RequestContentType, _newLine);
            RAML.AppendFormat("        formParameters: {0}", _newLine);

            foreach (var r in route.BodyParameters)
            {
                RAML.AppendFormat("          {0}: {1}", r.Name, _newLine);
                RAML.AppendFormat("            description  : {0}{1}", r.Description, _newLine);
                RAML.AppendFormat("            type  : {0}{1}", r.Type.ToRamlType(), _newLine);
                RAML.AppendFormat("            required  : {0}{1}", r.IsRequired.ToString().ToLower(), _newLine);
                RAML.AppendFormat("            example  : {0}{1}", r.Example, _newLine);
            }
            return RAML;
        }

        private StringBuilder SetHeaders(StringBuilder RAML, RouteModel route)
        {
            if (route.Headers == null || route.Headers.Count <= 0) return RAML;
            
            RAML.AppendFormat("    headers:{0}", _newLine);
            return route.Headers.Aggregate(RAML, SetHeaderDetails);
        }

        private StringBuilder SetHeaderDetails(StringBuilder RAML, RequestHeaderModel header)
        {
            RAML.AppendFormat("      {0}: {1}", header.Name, _newLine);
            RAML.AppendFormat("        displayName: {0}{1}", header.Name, _newLine);
            RAML.AppendFormat("        example: {0}{1}", header.Example, _newLine);
            RAML.AppendFormat("        type: {0}{1}", header.Type.ToRamlType(), _newLine);
            RAML = SetHeaderMinMax(RAML, header);
            RAML.AppendFormat("        description: {0}{1}", header.Description, _newLine);
            return RAML;
        }

        private StringBuilder SetHeaderMinMax(StringBuilder RAML, RequestHeaderModel header)
        {
            if (header.Type.ToRamlType() != "number" && header.Type.ToRamlType() != "integer") return RAML;
            
            RAML.AppendFormat("        minimum: {0}{1}", header.Minimum, _newLine);
            RAML.AppendFormat("        maximum: {0}{1}", header.Maximum, _newLine);
            return RAML;
        }

        private StringBuilder SetParameters(StringBuilder RAML, RouteModel route)
        {
            if (route.QueryParameters == null || route.QueryParameters.Count == 0) return RAML;
            
            RAML.AppendFormat("    queryParameters: {0}", _newLine);
            foreach (var parameters in route.QueryParameters)
            {
                RAML.AppendFormat("        {0}: {1}", parameters.Name, _newLine);
                RAML.AppendFormat("          type: {0}{1}", parameters.Type.ToRamlType(), _newLine);
                RAML.AppendFormat("          required: {0}{1}", parameters.IsRequired.ToString().ToLower(), _newLine);
                RAML.AppendFormat("          description: {0}{1}", parameters.Description, _newLine);
                RAML.AppendFormat("          example: |{1}            {0}{1}", parameters.Example, _newLine);
            }
            return RAML;
        }

        private StringBuilder SetUriParameters(StringBuilder RAML, RouteModel route)
        {
            if (route.UriParameters == null || route.UriParameters.Count <= 0) return RAML;

            RAML.AppendFormat("  uriParameters: {0}", _newLine);
            foreach (var parameters in route.UriParameters)
            {
                RAML.AppendFormat("      {0}: {1}", parameters.Name, _newLine);
                RAML.AppendFormat("        type: {0}{1}", parameters.Type.ToRamlType(), _newLine);
                RAML.AppendFormat("        required: {0}{1}", parameters.IsRequired.ToString().ToLower(), _newLine);
                RAML.AppendFormat("        description: {0}{1}", parameters.Description, _newLine);
                RAML.AppendFormat("        example: |{1}            {0}{1}", parameters.Example, _newLine);
            }
            return RAML;
        }

        private StringBuilder SetResponses(StringBuilder RAML, RouteModel route)
        {
            if (route.Responses == null || route.Responses.Count <= 0) return RAML;
           
            RAML.AppendFormat("    responses:{0}", _newLine);
            foreach (var response in route.Responses)
            {
                RAML.AppendFormat("      {0}:{1}", (int)response.StatusCode, _newLine);
                RAML.AppendFormat("        body:{0}", _newLine);
                RAML.AppendFormat("          {0}: {1}", response.ContentType, _newLine);
                RAML.AppendFormat("            schema: {0}{1}", response.Schema, _newLine);
                RAML.AppendFormat("            example: |{1}                {0}{1}", response.Example, _newLine);
            }
            return RAML;
        }

        #endregion

    }
}