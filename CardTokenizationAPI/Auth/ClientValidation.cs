using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Text;
using System.Web;

namespace CardTokenizationAPI.Auth
{
    public class ClientValidation
    {

        public static bool Login(string username, string password)
        {
            using (CardTokenizationEntities cardToken = new CardTokenizationEntities())
            {
                return cardToken.ClientCredentials.Any(x => x.ClientName.Equals(username, StringComparison.OrdinalIgnoreCase) && x.ClientPassword == password);
            }
        }
        public static bool AuthenticateClient(HttpRequestMessage request)
        {
            var isAuthenticated = false;
            try
            {
                if (request != null)
                {
                    var client_id = GetHeaderValue(request, "client-id");
                    var client_key = GetHeaderValue(request, "client-key");
                    var endPoint = request.RequestUri.AbsolutePath;
                    var client_ip_address = GetClientIp(request);
                    //  LogHelper.Info("Got here");
                    var clientProfile = GetClientProfile(client_id, client_key, client_ip_address);

                    if (clientProfile != null)
                    {
                        if (clientProfile.IsDeleted)
                            isAuthenticated = false;
                        else if (clientProfile.Unrestricted)
                            isAuthenticated = true;
                        else
                            isAuthenticated = true;
                        //isAuthenticated = IsPermitted(clientProfile.clientId, endPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                isAuthenticated = false;
               Console.WriteLine(ex);

            }
            return isAuthenticated;
        }
        private static ClientApiCredential GetClientProfile(string clientId, string clientSecret, string clientIpAddress)
        {
            try
            {
                using (CardTokenizationEntities cardToken = new CardTokenizationEntities())
                {
                    return cardToken.ClientApiCredentials.Where(x => x.ClientId == clientId && x.ClientSecret.ToString() == clientSecret && x.ClientIPAddress.Contains(clientIpAddress)).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                return null;
            }
        }
        public static string GetHeaderValue(HttpRequestMessage request, string headerKey)
        {
            try
            {
                IEnumerable<string> headers = request.Headers.GetValues(headerKey);
                var headerValue = headers.FirstOrDefault();
                return headerValue;
            }
            catch(Exception ex)
            {
                string error = ex.Message;
                return string.Empty;
            }

        }
        public static string GetClientIp(HttpRequestMessage request = null)
        {
            string ClientIPAddress = string.Empty;

            try
            {
                if (request != null)
                {
                    if (request.Properties.ContainsKey("MS_HttpContext"))
                    {
                        ClientIPAddress = ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
                    }
                    else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
                    {
                        RemoteEndpointMessageProperty prop = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
                        ClientIPAddress = prop.Address;
                    }
                }

                else if (System.Web.HttpContext.Current != null)
                {
                    ClientIPAddress = System.Web.HttpContext.Current.Request.UserHostAddress;
                    if (string.IsNullOrEmpty(ClientIPAddress))
                    {
                        ClientIPAddress = System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

                    }
                    if (string.IsNullOrEmpty(ClientIPAddress))
                    {
                        ClientIPAddress = System.Web.HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
                    }
                }
                else
                {
                    var sb = new StringBuilder();
                    // Get the hostname
                    string myHost = Dns.GetHostName();
                    var myIPs = Dns.GetHostEntry(myHost);
                    foreach (var myIP in myIPs.AddressList)
                        if (!myIP.IsIPv6LinkLocal)
                            ClientIPAddress = myIP.ToString();

                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
            return ClientIPAddress;
        }
    }
}