using Microsoft.Owin;
using Microsoft.Owin.Security.OAuth;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CardTokenizationAPI.Auth
{
    public class AuthenticateToken
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll);//enable cors origin requests

            //Configuring OAuthAuthorizationServer
            var myProvider = new BasicAuthenticationAttribute();//getting refrence of my provider

            //Defining OAuthAuthorizationServer Options
            OAuthAuthorizationServerOptions options = new OAuthAuthorizationServerOptions
            {
                AllowInsecureHttp = true,//do not allow in development environment
                TokenEndpointPath = new PathString("/token"),//this is path where user will get token,after getting validated
                                                             //AccessTokenExpireTimeSpan = TimeSpan.FromDays(1),
                AccessTokenExpireTimeSpan = TimeSpan.FromMinutes(10),
                Provider = myProvider
            };

            app.UseOAuthAuthorizationServer(options);
            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());

            //Register WebAPI congfig
            HttpConfiguration config = new HttpConfiguration();
            WebApiConfig.Register(config);
        }
}