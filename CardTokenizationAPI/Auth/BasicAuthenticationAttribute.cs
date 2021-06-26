using Microsoft.Owin.Security.OAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace CardTokenizationAPI.Auth
{
    public class BasicAuthenticationAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            //if (actionContext.Request.Headers.Authorization == null)
            //{
            //    actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
            //}
            //else
            //{
                //do client and secret authentication here for token generation
                //string authenticationToken = actionContext.Request.Headers.Authorization.Parameter;
                //string decodedAuthenticationToken = Encoding.UTF8.GetString(Convert.FromBase64String(authenticationToken));
                //string[] usernamePasswordArray = decodedAuthenticationToken.Split(':');
                //string username = usernamePasswordArray[0];
                //string password = usernamePasswordArray[1];
                //if (ClientValidation.Login(username, password))
                if (ClientValidation.AuthenticateClient(actionContext.Request))
                {
                    //Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity(username), null);
                    return;
                }
                else
                {
                    actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
                }
            //}
        }

        private bool AuthorizeRequest(HttpActionContext actionContext)
        {
            var isAuthorized = false;
            isAuthorized = ClientValidation.AuthenticateClient(actionContext.Request);
            return isAuthorized;
        }
    }
}