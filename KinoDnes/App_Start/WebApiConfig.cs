﻿using System.Net.Http.Headers;
using System.Web.Http;

namespace KinoDnes
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute("KinoApi", "api/{controller}/{city}", new {id = RouteParameter.Optional}
                );

            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
        }
    }
}