using CardTokenizationAPI.Auth;
using CardTokenizationAPI.Dtos;
using CardTokenizationAPI.TokenProvider;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

namespace CardTokenizationAPI.Controllers
{
    //[BasicAuthentication]
    [RoutePrefix("CardToken")]
    public class CardTokenizationController : ApiController
    {
        [HttpPost]
        [ResponseType(typeof(RequestDeviceBindingResp))]
        [Route("api/RequestDeviceBinding")]
        public HttpResponseMessage RequestDeviceBinding([FromBody]RequestDeviceBinding value)
        {
            if (!ModelState.IsValid)
            {
                var error = new ErrorResp() { responseCode = 111, errorMessage = "Missing mandatory parameter" };
                return Request.CreateResponse(error);
            }
            //return 
            var doDeviceBind = new DoTokenization();
            new ErrorLog($"RequestDeviceBinding controller input: {JsonConvert.SerializeObject(value)}");
            var result = doDeviceBind.DoRequestDevice(value);
            new ErrorLog($"RequestDeviceBinding result: {JsonConvert.SerializeObject(result)}");
            //string val = JsonConvert.SerializeObject(result);
            if (result.responseCode == 200)
            {
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            else
            {
                var error = new ErrorResp()
                {
                    errorMessage = result.errorMessage,
                    responseCode = result.responseCode
                };
                return Request.CreateResponse(HttpStatusCode.BadRequest, error);
            }
        }

        [HttpPost]
        [ResponseType(typeof(CardEligibilityResp))]
        [Route("api/CheckCardEligibility")]
        public HttpResponseMessage CheckCardEligibility([FromBody] CardEligibilityReq value)
        {
            if (!ModelState.IsValid)
            {
                var error = new ErrorResp() { responseCode = 111, errorMessage = "Missing mandatory parameter" };
                return Request.CreateResponse(error);
            }
            var checkEligibility = new DoTokenization();
            new ErrorLog($"CheckCardEligibility controller input: {JsonConvert.SerializeObject(value)}");
            var result = checkEligibility.DoEligibilityCheck(value);
            new ErrorLog($"CheckCardEligibility result: {JsonConvert.SerializeObject(result)}");
            if (result.responseCode == 200)
            {
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            else
            {
                var error = new ErrorResp()
                {
                    errorMessage = result.errorMessage,
                    responseCode = result.responseCode
                };
                return Request.CreateResponse(HttpStatusCode.BadRequest, error);
            }
        }

        [HttpPost]
        [ResponseType(typeof(CardDigitizationResp))]
        [Route("api/CardDigitization")]
        public HttpResponseMessage DoCardDigitization([FromBody] RequestCardDigitization value)
        {
            if (!ModelState.IsValid)
            {
                var error = new ErrorResp() { responseCode = 111, errorMessage = "Missing mandatory parameter" };
                return Request.CreateResponse(error);
            }
            var doCardDigitization = new DoTokenization();
            new ErrorLog($"CardDigitization controller input: {JsonConvert.SerializeObject(value)}");
            var result = doCardDigitization.DoCardDigitization(value);
            new ErrorLog($"CardDigitization result: {JsonConvert.SerializeObject(result)}");
            if (result.responseCode == 200)
            {
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            else
            {
                var error = new ErrorResp()
                {
                    errorMessage = result.errorMessage,
                    responseCode = result.responseCode
                };
                return Request.CreateResponse(HttpStatusCode.BadRequest, error);
            }
        }

        [HttpPost]
        [ResponseType(typeof(VirtualCardChangeResp))]
        [Route("api/NotifyVirtualCardChange")]
        public HttpResponseMessage NotifyVirtualCardChange([FromBody] VirtualCardChangeReq value)
        {
            if (!ModelState.IsValid)
            {
                var error = new ErrorResp() { responseCode = 111, errorMessage = "Missing mandatory parameter" };
                return Request.CreateResponse(error);
            }
            var doCardDigitization = new DoTokenization();
            new ErrorLog($"NotifyVirtualCardChange controller input: {JsonConvert.SerializeObject(value)}");
            var result = doCardDigitization.DoVirtualCardChange(value);
            new ErrorLog($"NotifyVirtualCardChange result: {JsonConvert.SerializeObject(result)}");
            if (result.responseCode == 200)
            {
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            else
            {
                var error = new ErrorResp()
                {
                    errorMessage = result.errorMessage,
                    responseCode = result.responseCode
                };
                return Request.CreateResponse(HttpStatusCode.BadRequest, error);
            }
        }

        [HttpPost]
        [ResponseType(typeof(GetIDnVMethodListResp))]
        [Route("api/GetIDnVMethodList")]
        public HttpResponseMessage GetIDnVMethodList([FromBody] GetIDnVMethodListReq value)
        {
            if (!ModelState.IsValid)
            {
                var error = new ErrorResp() { responseCode = 111, errorMessage = "Missing mandatory parameter" };
                return Request.CreateResponse(error);
            }
            var doCardDigitization = new DoTokenization();
            new ErrorLog($"GetIDnVMethodList controller input: {JsonConvert.SerializeObject(value)}");
            var result = doCardDigitization.GetIDnVMethodList(value);
            new ErrorLog($"GetIDnVMethodList result: {JsonConvert.SerializeObject(result)}");
            if (result.responseCode == 200)
            {
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            else
            {
                var error = new ErrorResp()
                {
                    errorMessage = result.errorMessage,
                    responseCode = result.responseCode
                };
                return Request.CreateResponse(HttpStatusCode.BadRequest, error);
            }
        }

        [HttpPost]
        [ResponseType(typeof(SendOTPResponse))]
        [Route("api/SendOTP")]
        public HttpResponseMessage SendOTP([FromBody] SendOTPRequest value)
        {
            if (!ModelState.IsValid)
            {
                var error = new ErrorResp() { responseCode = 111, errorMessage = "Missing mandatory parameter" };
                return Request.CreateResponse(error);
            }
            var doCardDigitization = new DoTokenization();
            new ErrorLog($"SendOTP controller input: {JsonConvert.SerializeObject(value)}");
            var result = doCardDigitization.SendOTP(value);
            new ErrorLog($"SendOTP result: {JsonConvert.SerializeObject(result)}");
            if (result.responseCode == 200)
            {
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            else
            {
                var error = new ErrorResp()
                {
                    errorMessage = result.errorMessage,
                    responseCode = result.responseCode
                };
                return Request.CreateResponse(HttpStatusCode.BadRequest, error);
            }
        }
    }
}
