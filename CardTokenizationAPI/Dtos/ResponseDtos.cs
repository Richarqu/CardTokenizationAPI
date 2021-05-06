using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CardTokenizationAPI.Dtos
{

    public class SendOTPResponse
    {
        public int responseCode { get; set; }
        public string errorMessage { get; set; }
        public string responseMessage { get; set; }
    }

    public class RequestDeviceBindingResp
    {
        public string levelOfTrust { get; set; }
        public int responseCode { get; set; }
        public string errorMessage { get; set; }
        public Idvmethodlist[] idvMethodList { get; set; }
    }
    public class IdvMethodList
    {
        public string id { get; set; }
        public string type { get; set; }
        public string value { get; set; }
        public string source { get; set; }
        public string platform { get; set; }
    }
    public class CardEligibilityResp
    {
        public string issuerCardRefId { get; set; }
        public int responseCode { get; set; }
        public string errorMessage { get; set; }
        public string productId { get; set; }
    }
    public class ErrorResp
    {
        public string errorMessage { get; set; }
        public int responseCode { get; set; }
    }
    public class CardDigitizationResp
    {
        public int responseCode { get; set; }
        public string errorMessage { get; set; }
        public string issuerCardRefId { get; set; }
        public string productId { get; set; }
        public Carddetails cardDetails { get; set; }
        public string levelOfTrust { get; set; }
        public Idvmethodlist[] idvMethodList { get; set; }
    }

    public class Carddetails
    {
        public string cardholderName { get; set; }
        public string fpanLastDigits { get; set; }
        public string fpanBin { get; set; }
        public string fpanExpiryDate { get; set; }
    }

    public class Idvmethodlist
    {
        public string id { get; set; }
        public string type { get; set; }
        public string value { get; set; }
    }

    public class VirtualCardChangeResp
    {
        public int responseCode { get; set; }
        public string errorMessage { get; set; }
        public string responseMessage { get; set; }
    }

    public class GetIDnVMethodListResp
    {
        public int responseCode { get; set; }
        public string errorMessage { get; set; }
        public Idvmethodlist[] idvMethodList { get; set; }
    }
}