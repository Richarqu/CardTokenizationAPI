using CardTokenizationAPI.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace CardTokenizationAPI.Controllers
{
    public class AccountController : ApiController
    {
        [HttpGet]
        [BasicAuthentication]
        public HttpResponseMessage Get()
        {
            var requestHeader = this.Request.Headers;
            string userName = requestHeader.GetValues("client-id").FirstOrDefault(); //Thread.CurrentPrincipal.Identity.Name;


            using (CardTokenizationEntities entities = new CardTokenizationEntities())
            {
                if (userName != "")
                {
                    return Request.CreateResponse(HttpStatusCode.OK, entities.ClientApiCredentials.Select(x => x.ClientName).ToList());
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized);
                }
            }
        }
    }
}
