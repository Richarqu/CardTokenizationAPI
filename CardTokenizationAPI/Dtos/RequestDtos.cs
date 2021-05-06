using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CardTokenizationAPI.Dtos
{

    public class SendOTPRequest
    {
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public string walletProviderId { get; set; }
        public Tokenrequestor tokenRequestor { get; set; }
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public string issuerCardRefId { get; set; }
        public string deviceBindingReference { get; set; }
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public string otpValue { get; set; }
        public string expirationDate { get; set; }
        public string otpMethodId { get; set; }
    }
    public class RequestDeviceBinding
    {
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public string deviceBindingReference { get; set; }
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public string virtualCardId { get; set; }
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public string walletProviderId { get; set; }
        public Tokenrequestor tokenRequestor { get; set; }
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public string issuerCardRefId { get; set; }
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public Walletuserinformation walletUserInformation { get; set; }
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public Deviceinformation deviceInformation { get; set; }
    }

    public class Tokenrequestor
    {
        public string id { get; set; }
        public string walletId { get; set; }
        public string merchantId { get; set; }
        public string name { get; set; }
    }

    public class Walletuserinformation
    {
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public string walletUserId { get; set; }
        public string emailHash { get; set; }
        public string maskedEmail { get; set; }
        public bool isAccountUsernamMatchCardName { get; set; }
        public string provisioningAttemptsOnDeviceIn24Hours { get; set; }
        public string walletDistinctCardholderNames { get; set; }
        public string walletAccountCountry { get; set; }
        public string suspendedCardsInAccount { get; set; }
        public string daysSinceLastAccountActivity { get; set; }
        public string numberOfActiveTokens { get; set; }
        public string deviceWithActiveTokens { get; set; }
        public string activeTokensOnAllDeviceForAccount { get; set; }
        public string daysSinceConsumerDataLastAccountChange { get; set; }
        public string numberOfTransactionsInLast12Months { get; set; }
        public string accountEmailLife { get; set; }
    }

    public class Deviceinformation
    {
        public string tokenStorageId { get; set; }
        public string tokenStorageType { get; set; }
        public string manufacturer { get; set; }
        public string brand { get; set; }
        public string model { get; set; }
        public string tac { get; set; }
        public string osVersion { get; set; }
        public string firmwareVersion { get; set; }
        public string phoneNumber { get; set; }
        public string fourLastDigitPhoneNumber { get; set; }
        public string deviceName { get; set; }
        public string deviceId { get; set; }
        public string androidIdLastTwo { get; set; }
        public string deviceParentId { get; set; }
        public string language { get; set; }
        public string deviceStateFlags { get; set; }
        public string serialNumber { get; set; }
        public string timeZone { get; set; }
        public string timeZoneSetting { get; set; }
        public string simSerialNumber { get; set; }
        public string IMEI { get; set; }
        public string phoneLostTime { get; set; }
        public string networkOperator { get; set; }
        public string networkType { get; set; }
    }

    public class CardEligibilityReq
    {
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public string cipheredCardInfo { get; set; }
        public string issuerCardRefId { get; set; }
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public string walletProviderId { get; set; }
        public Tokenrequestor tokenRequestor { get; set; }
        public string captureMethod { get; set; }
    }
    public class JsonCipheredCardInfo
    {
        public string fpan { get; set; }
        public string exp { get; set; }
        public string cvv { get; set; }
        public string additionalCardInfos { get; set; }
    }

    public class RequestCardDigitization
    {
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public string cipheredCardInfo { get; set; }
        public string issuerCardRefId { get; set; }
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public string walletProviderId { get; set; }
        public Tokenrequestor tokenRequestor { get; set; }
        public string walletCardRefId { get; set; }
        public string authenticationValue { get; set; }
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public Walletuserinformation walletUserInformation { get; set; }
        public Cardcaptureinformation cardCaptureInformation { get; set; }
        public Scoringinformation scoringInformation { get; set; }
        public Deviceinformation deviceInformation { get; set; }
        public bool tncStatus { get; set; }
        public string tncAcceptedDate { get; set; }
        public bool cvvValidated { get; set; }
    }

    public class Cardcaptureinformation
    {
        public string cardHolderName { get; set; }
        public string captureMethod { get; set; }
        public string deviceLocation { get; set; }
        public string sourceIp { get; set; }
        public Address address { get; set; }
        public string cardHolderIdType { get; set; }
        public string cardHolderIdNo { get; set; }
    }

    public class Address
    {
        public string addressStreetOne { get; set; }
        public string addressStreetTwo { get; set; }
        public string addressCity { get; set; }
        public string addressState { get; set; }
        public string addressZip { get; set; }
        public string addressCountry { get; set; }
    }

    public class Scoringinformation
    {
        public string levelOfTrust { get; set; }
        public string tspScore { get; set; }
        public string[] reasonCodes { get; set; }
        public string deviceScore { get; set; }
        public string deviceTenure { get; set; }
        public string deviceTokens { get; set; }
        public string deviceCountry { get; set; }
        public string devicePayJoinDate { get; set; }
        public string accountScore { get; set; }
        public string accountCreationDate { get; set; }
        public string accountLastUpdateDate { get; set; }
        public string userTenure { get; set; }
        public string userTokens { get; set; }
        public string userWallets { get; set; }
        public string userCountry { get; set; }
        public string userPayJoinDate { get; set; }
        public string phoneNumberScore { get; set; }
        public string walletScore { get; set; }
        public string walletTenure { get; set; }
        public string walletTransactions { get; set; }
        public string cardScore { get; set; }
        public string cardTenure { get; set; }
        public bool cardNewlyAdded { get; set; }
        public string levelOfTrustStandardVersion { get; set; }
    }

    public class VirtualCardChangeReq
    {
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public string issuerCardRefId { get; set; }
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public string virtualCardId { get; set; }
        public string walletCardRefId { get; set; }
        public string walletVirtualCardId { get; set; }
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public string walletProviderId { get; set; }
        public Tokenrequestor tokenRequestor { get; set; }
        public string tokenStorageId { get; set; }
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public bool isPrimary { get; set; }
        public string action { get; set; }
        public string deviceBindingReference { get; set; }
        public string tokenInfo { get; set; }
        public int errorCode { get; set; }
        public string source { get; set; }
    }

    public class GetIDnVMethodListReq
    {
        public string walletProviderId { get; set; }
        public Tokenrequestor tokenRequestor { get; set; }
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public string issuerCardRefId { get; set; }
        [Required(ErrorMessage = "Missing mandatory parameter")]
        public string virtualCardId { get; set; }
        public string deviceBindingReference { get; set; }
    }
}