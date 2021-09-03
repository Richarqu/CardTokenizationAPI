using CardTokenizationAPI.Dtos;
using Newtonsoft.Json;
using Sterling.MSSQL;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace CardTokenizationAPI.TokenProvider
{
    public class DoTokenization
    {
        public RequestDeviceBindingResp DoRequestDevice(RequestDeviceBinding request)
        {
            var response = new RequestDeviceBindingResp();
            var bankService = new BankSoap.banksSoapClient();
            try
            {
                new ErrorLog($"RequestDeviceBinding request is: {JsonConvert.SerializeObject(request)}");
                string year = DateTime.Now.Year.ToString();
                string month = DateTime.Now.Month.ToString();
                string exp = year.Substring(year.Length - 2, 2) + month;
                string pan = IBS_Decrypt(request.issuerCardRefId);
                string maxSeqNr = GetMaxSeqNr(pan, exp);
                string query = $"select a.pan as pan,expiry_date,hold_rsp_code,card_status,customer_id from pc_cards where pan = '{pan}' and expiry_date > '{exp}' and seq_nr = '{maxSeqNr}'";
               
                var data = GetData(query);
                bool hasRows = data.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
                if (hasRows)
                {
                    int cnt = data.Tables[0].Rows.Count;
                    if (cnt > 0)
                    {
                        for (int j = 0; j < cnt; j++)
                        {
                            DataRow dr = data.Tables[0].Rows[j];
                            string cardStatus = dr["card_status"].ToString();
                            string holdResp = dr["hold_rsp_code"].ToString();

                            //Get customerid details from core banking to populate table below.
                            string custID = dr["customer_id"].ToString().Trim();
                            var customerInfo = bankService.getCustomrInfo(custID);
                            string phoneNo = customerInfo.Tables[0].Rows[0]["ContactMobile1"].ToString().Trim();
                            string emailAdd = customerInfo.Tables[0].Rows[0]["ContactEmail"].ToString().Trim();

                            if (cardStatus == "1" && string.IsNullOrEmpty(holdResp) || cardStatus == "1" && holdResp == "NULL")
                            {

                                //since the eligibility has already been done, get the pan from the issuer card ref id
                                //check the card tokenization table for the deviceBindingReference
                                string queryCheck = $"select * FROM [EchanelsT24].[dbo].[CardTokenizationData] where IssuerCardRefID = '{request.issuerCardRefId}' and WalletProviderID = '{request.walletProviderId}' and DeviceBindingRef = '{request.deviceBindingReference}'";
                                var tokenData = GetTokenizationRecs(queryCheck);
                                //and DeviceBindingRef = '{request.deviceBindingReference}'
                                bool hasTokenRows = tokenData.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
                                if (hasTokenRows)
                                {
                                    int cnts = tokenData.Tables[0].Rows.Count;
                                    if (cnts > 0)
                                    {
                                        //this means the devicebindingref already exist
                                        //check the status of the card before responding
                                        for (int k = 0; k < cnts; k++)
                                        {
                                            //This means the device has already been bound to the card
                                            DataRow drs = tokenData.Tables[0].Rows[0];
                                            if (drs["Status"].ToString() == "Active")
                                            {
                                                //return success
                                                //device binding is successful
                                                var idVMetList = FormIdvMethodList(phoneNo, emailAdd);
                                                response = new RequestDeviceBindingResp()
                                                {
                                                    errorMessage = "Successful",
                                                    responseCode = 200,
                                                    idvMethodList = idVMetList,
                                                    levelOfTrust = "yellow"
                                                };
                                                string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/RequestDeviceBinding','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','200','Successful','','','{request.walletProviderId}')";
                                                //change walletProviderID to card info
                                                int insertReqData = InsertTokenizationRec(insRequestData);
                                                new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                            }
                                            else
                                            {
                                                response = new RequestDeviceBindingResp()
                                                {
                                                    responseCode = 159,
                                                    errorMessage = "Card is suspended"
                                                };
                                                string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/RequestDeviceBinding','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','159','Card is suspended','','','{request.walletProviderId}')";
                                                //change walletProviderID to card info
                                                int insertReqData = InsertTokenizationRec(insRequestData);
                                                new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");  
                                            }
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    //do device binding for the device.
                                    //before device bining is done, the card eligibility check would have been done. Which is why the record has issuer card reference ID.
                                    //Just update the existing record with the device binding reference
                                    bool isCardActive = cardStatus == "1" ? true : false;
                                    string updQuery = $"UPDATE [EchanelsT24].[dbo].[CardTokenizationData] set DeviceBindingRef = '{request.deviceBindingReference}', IsCardActive = {isCardActive}, Status = 'Active', LevelOfTrust = 'yellow' where IssuerCardRefID = '{request.issuerCardRefId}' and WalletProviderID = '{request.walletProviderId}' and DeviceBindingRef = '' and ID in (select top 1 ID from [EchanelsT24].[dbo].[CardTokenizationData] where DeviceBindingRef = '')";
                                    int updt = UpdateTokenizationTable(updQuery);
                                    if(updt > 0)
                                    {
                                        //device binding is successful
                                        var idVMetList = FormIdvMethodList(phoneNo, emailAdd);
                                        response = new RequestDeviceBindingResp()
                                        {
                                            errorMessage = "Successful",
                                            responseCode = 200,
                                            idvMethodList = idVMetList,
                                            levelOfTrust = "yellow"
                                        };
                                        string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/RequestDeviceBinding','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','200','Successful','','','{request.walletProviderId}')";
                                        //change walletProviderID to card info
                                        int insertReqData = InsertTokenizationRec(insRequestData);
                                        new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                    }
                                    else
                                    {
                                        response = new RequestDeviceBindingResp()
                                        {
                                            errorMessage = "Operation failed",
                                            responseCode = 911
                                        };
                                        string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/RequestDeviceBinding','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','911','Operation failed','','','')";
                                        int insertReqData = InsertTokenizationRec(insRequestData);
                                        new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                    }
                                    break;
                                }
                            }
                            else
                            {
                                response = new RequestDeviceBindingResp()
                                {
                                    responseCode = 159,
                                    errorMessage = "Card is suspended"
                                };
                                string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/CheckCardEligibility','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','159','Card is suspended','','','{request.walletProviderId}')";
                                //change walletProviderID to card info
                                int insertReqData = InsertTokenizationRec(insRequestData);
                                new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                break;
                            }
                        }
                    }
                    else
                    {
                        response = new RequestDeviceBindingResp()
                        {
                            errorMessage = "Invalid FPAN",
                            responseCode = 166
                        };
                        string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/RequestDeviceBinding','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','166','Invalid FPAN','','','Wallet Provider ID - {request.walletProviderId}')";
                        int insertReqData = InsertTokenizationRec(insRequestData);
                        new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                    }
                }
                else
                {
                    response = new RequestDeviceBindingResp()
                    {
                        errorMessage = "Invalid FPAN",
                        responseCode = 166
                    };
                    string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/RequestDeviceBinding','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','166','Invalid FPAN','','','Wallet Provider ID - {request.walletProviderId}')";
                    int insertReqData = InsertTokenizationRec(insRequestData);
                    new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                }
            }
            catch (Exception ex)
            {
                new ErrorLog(ex);
            }
            return response;
        }
        private string GetMaxSeqNr(string pan, string exp)
        {
            string seqNr = string.Empty;
            string query = $"select max(seq_nr) as seq_nr from pc_cards where pan = '{pan}' and expiry_date = '{exp}'"; 
            var data = GetData(query);
            bool hasRows = data.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
            if (hasRows)
            {
                int cnt = data.Tables[0].Rows.Count;
                if (cnt > 0)
                {
                    for (int j = 0; j < cnt; j++)
                    {
                        DataRow dr = data.Tables[0].Rows[j];
                        seqNr = dr["seq_nr"].ToString();
                    }
                }
            }
            return seqNr;
        }
        private string GetFEPCardStatusByIssuerRefID(string issuerRefID)
        {
            string status = string.Empty;
            try
            {
                string year = DateTime.Now.Year.ToString();
                string month = DateTime.Now.Month.ToString();
                string exp = year.Substring(year.Length - 2, 2) + month;
                string query = $"select * from pc_cards where pan = '{issuerRefID}' and expiry_date >= '{exp}'";
                var data = GetData(query);
                bool hasRows = data.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
                if (hasRows)
                {
                    int cnt = data.Tables[0].Rows.Count;
                    //int colCnt = data.Tables[0].Columns.Count;
                    if (cnt > 0)
                    {
                        for (int j = 0; j < cnt; j++)
                        {
                            DataRow dr = data.Tables[0].Rows[j];
                            if (dr["card_status"].ToString() == "1" && string.IsNullOrEmpty(dr["hold_rsp_code"].ToString()))
                            {
                                status = "Active";
                                break;
                            }
                            else
                            {
                                status = "Inactive";
                            }
                        }
                    }
                }
            }catch(Exception ex)
            {
                new ErrorLog($"Error in method GetFEPCardStatusByIssuerRefID - {ex}");
                status = "";
            }
            return status;
        }
        public CardEligibilityResp DoEligibilityCheck(CardEligibilityReq request)
        {
            var response = new CardEligibilityResp();
            try
            {
                //remember to check the cardtokenizationdata table if record exists before
                //if it does, update the required parameters based on what was returned from postcard
                var bankService = new BankSoap.banksSoapClient();
                var cardService = new CardService.CardsSoapClient();
                new ErrorLog($"DoEligibilityCheck request recieved is: {JsonConvert.SerializeObject(request)}");

                var cardInfo = JsonConvert.DeserializeObject<JsonCipheredCardInfo>(request.cipheredCardInfo);
                string exp = cardInfo.exp.Substring(cardInfo.exp.Length - 2, 2) + cardInfo.exp.Substring(0, 2);
                string maxSeqNr = GetMaxSeqNr(cardInfo.fpan, exp);
                string query = $"select a.pan as pan,expiry_date,hold_rsp_code,card_status,customer_id from pc_cards where pan = '{cardInfo.fpan}' and expiry_date = '{exp}' and seq_nr = '{maxSeqNr}'";
                string issuerCardRefID = string.Empty;
                issuerCardRefID = Encrypt(cardInfo.fpan);
                var data = GetData(query);
                bool hasRows = data.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
                if (hasRows)
                {
                    int cnt = data.Tables[0].Rows.Count;
                    if (cnt > 0)
                    {
                        for (int j = 0; j < cnt; j++)
                        {
                            DataRow dr = data.Tables[0].Rows[j];
                            string cardStatus = dr["card_status"].ToString();
                            string holdResp = dr["hold_rsp_code"].ToString();

                            //Get customerid details from core banking to populate table below.
                            string custID = dr["customer_id"].ToString().Trim();
                            var customerInfo = bankService.getCustomrInfo(custID);
                            string phoneNo = customerInfo.Tables[0].Rows[0]["ContactMobile1"].ToString().Trim();
                            string emailAdd = customerInfo.Tables[0].Rows[0]["ContactEmail"].ToString().Trim();
                            bool isActive = cardStatus == "1" ? true : false;
                            if (cardStatus == "1" && string.IsNullOrEmpty(holdResp) || cardStatus == "1" && holdResp == "NULL")
                            {
                                var getCvv2 = cardService.GenerateCVV2(cardInfo.fpan, exp);
                                if (!string.IsNullOrEmpty(cardInfo.cvv))
                                {
                                    //compare the cvv2 of the card
                                    if (getCvv2 == cardInfo.cvv)
                                    {
                                        //return success
                                        response = new CardEligibilityResp()
                                        {
                                            errorMessage = "Successful",
                                            responseCode = 200,
                                            issuerCardRefId = issuerCardRefID,
                                            productId = cardInfo.fpan.Substring(0, 6)
                                        };
                                        //Insert record into the Card Tokenization Data table
                                        string insQuery = $"INSERT INTO [dbo].[CardTokenizationData] VALUES ('{cardInfo.fpan}','{issuerCardRefID}','',{Convert.ToInt32(exp)},'Yellow','Active','{phoneNo}','{emailAdd}',{isActive},'{holdResp}','{request.captureMethod}', '{request.walletProviderId}','','')";
                                        int insertData = InsertTokenizationRec(insQuery);
                                        new ErrorLog($"{DateTime.Now}: Response - {insertData} returned for insert query: {insQuery}.");

                                        string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{issuerCardRefID}','','api/CheckCardEligibility','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','200','Successful','Yellow','','{JsonConvert.SerializeObject(cardInfo)}')";
                                        int insertReqData = InsertTokenizationRec(insRequestData);
                                        new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                    }
                                    else
                                    {
                                        response = new CardEligibilityResp()
                                        {
                                            errorMessage = "Invalid CVV2",
                                            responseCode = 161
                                        };

                                        string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{issuerCardRefID}','','api/CheckCardEligibility','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','161','Invalid CVV2','','','{JsonConvert.SerializeObject(cardInfo)}')";
                                        int insertReqData = InsertTokenizationRec(insRequestData);
                                        new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                    }
                                }
                                else
                                {
                                    response = new CardEligibilityResp()
                                    {
                                        errorMessage = "Successful",
                                        responseCode = 200,
                                        issuerCardRefId = issuerCardRefID,
                                        productId = cardInfo.fpan.Substring(0, 6)
                                    };
                                    //Insert record into the Card Tokenization Data table
                                    string insQuery = $"INSERT INTO [dbo].[CardTokenizationData] VALUES ('{cardInfo.fpan}','{issuerCardRefID}','',{Convert.ToInt32(exp)},'Yellow','Active','{phoneNo}','{emailAdd}',{isActive},'{holdResp}','{request.captureMethod}', '{request.walletProviderId}','','')";
                                    int insertData = InsertTokenizationRec(insQuery);
                                    new ErrorLog($"{DateTime.Now}: Response - {insertData} returned for insert query: {insQuery}.");

                                    string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{issuerCardRefID}','','api/CheckCardEligibility','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','200','Successful','Yellow','','{JsonConvert.SerializeObject(cardInfo)}')";
                                    int insertReqData = InsertTokenizationRec(insRequestData);
                                    new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                }
                            }
                            else
                            {
                                response = new CardEligibilityResp()
                                {
                                    errorMessage = "Card is suspended",
                                    responseCode = 159
                                };
                                string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{issuerCardRefID}','','api/CheckCardEligibility','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','159','Card is suspended','','','{JsonConvert.SerializeObject(cardInfo)}')";
                                int insertReqData = InsertTokenizationRec(insRequestData);
                                new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                            }
                        }
                    }
                    else
                    {
                        response = new CardEligibilityResp()
                        {
                            errorMessage = "Invalid FPAN",
                            responseCode = 166
                        };
                        string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{issuerCardRefID}','','api/CheckCardEligibility','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','166','Invalid FPAN','','','{JsonConvert.SerializeObject(cardInfo)}')";
                        int insertReqData = InsertTokenizationRec(insRequestData);
                        new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                    }
                }
                else
                {
                    response = new CardEligibilityResp()
                    {
                        errorMessage = "Invalid FPAN",
                        responseCode = 166
                    };
                    string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{issuerCardRefID}','','api/CheckCardEligibility','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','166','Invalid FPAN','','','{JsonConvert.SerializeObject(cardInfo)}')";
                    int insertReqData = InsertTokenizationRec(insRequestData);
                    new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                }
            }
            catch (Exception ex)
            {
                new ErrorLog(ex);
                response = new CardEligibilityResp()
                {
                    errorMessage = "Unexpected server error",
                    responseCode = 921
                };
                string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/CheckCardEligibility','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','921','Unexpected server error','','','')";
                int insertReqData = InsertTokenizationRec(insRequestData);
                new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
            }
            new ErrorLog($"Response returned for DoEligibilityCheck - {JsonConvert.SerializeObject(response)}");
            return response;
        }
        public CardDigitizationResp DoCardDigitization(RequestCardDigitization request)
        {
            var response = new CardDigitizationResp();
            try
            {
                var bankService = new BankSoap.banksSoapClient();
                var idMethodList = new List<IdvMethodList>();
                var cardService = new CardService.CardsSoapClient();
                new ErrorLog($"DoCardDigitization request recieved is: {JsonConvert.SerializeObject(request)}");

                var cardInfo = JsonConvert.DeserializeObject<JsonCipheredCardInfo>(request.cipheredCardInfo);
                //the cipheredCardinfo needs to be deciphered before getting the cardInfo data above. Skipped to allow for testing
                string exp = cardInfo.exp.Substring(cardInfo.exp.Length - 2, 2) + cardInfo.exp.Substring(0, 2);

                //Check status of card on the Card Tokenization table to be sure it has not been blocked from doing tokenization and the device not blocked
                //string issuerCardRefID = string.Empty;
                //issuerCardRefID = Encrypt(cardInfo.fpan);
                string queryCheck = $"select * FROM [EchanelsT24].[dbo].[CardTokenizationData] where IssuerCardRefID = '{request.issuerCardRefId}' and WalletProviderID = '{request.walletProviderId}'";
                //should devicebindingreference be included in the query
                var tokenData = GetTokenizationRecs(queryCheck);
                bool hasTokenRows = tokenData.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
                if (hasTokenRows)
                {
                    int cnts = tokenData.Tables[0].Rows.Count;
                    if (cnts > 0)
                    {
                        //here, check the status. if active, proceed otherwise, report level of trust to be red
                        DataRow drs = tokenData.Tables[0].Rows[0];
                        if (drs["Status"].ToString() == "Active")
                        {
                            //this select query should be able to pick just one record. record with max seq nr
                            string query = $"select * from pc_cards a with (nolock) inner join pc_customers b with (nolock) on a.customer_id = b.customer_id where pan = '{cardInfo.fpan}' and expiry_date = '{exp}'"; // and card_status =  1 and hold_rsp_code is null
                            var data = GetData(query);
                            bool hasRows = data.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
                            if (hasRows)
                            {
                                int cnt = data.Tables[0].Rows.Count;
                                if (cnt > 0)
                                {
                                    for (int j = 0; j < cnt; j++)
                                    {
                                        DataRow dr = data.Tables[0].Rows[j];
                                        //if card status/hold response is changed, remember to update the card tokenization table 
                                        if (dr["card_status"].ToString() == "1" && string.IsNullOrEmpty(dr["hold_rsp_code"].ToString()) || dr["card_status"].ToString() == "1" && dr["hold_rsp_code"].ToString() == "NULL")
                                        {
                                            //Get customerid details from core banking to populate table below.
                                            string custID = dr["customer_id"].ToString().Trim();
                                            var customerInfo = bankService.getCustomrInfo(custID);
                                            string phoneNo = customerInfo.Tables[0].Rows[0]["ContactMobile1"].ToString().Trim();
                                            string emailAdd = customerInfo.Tables[0].Rows[0]["ContactEmail"].ToString().Trim();

                                            var getCvv2 = cardService.GenerateCVV2(cardInfo.fpan, exp);
                                            if (!string.IsNullOrEmpty(cardInfo.cvv))
                                            {
                                                //compare the cvv2 of the card
                                                if (getCvv2 == cardInfo.cvv)
                                                {
                                                    //if enrolment exist, update record

                                                    var cardDetails = new Carddetails()
                                                    {
                                                        cardholderName = dr["c1_name_on_card"].ToString(),
                                                        fpanBin = cardInfo.fpan.Substring(0, 6),
                                                        fpanLastDigits = cardInfo.fpan.Substring(cardInfo.fpan.Length - 5, 5),
                                                        fpanExpiryDate = cardInfo.exp.Substring(cardInfo.exp.Length - 2, 2) + "/" + cardInfo.exp.Substring(0, 2)
                                                    };
                                                    var idVMetList = FormIdvMethodList(phoneNo, emailAdd);
                                                    response = new CardDigitizationResp()
                                                    {
                                                        errorMessage = "Successful",
                                                        responseCode = 200,
                                                        issuerCardRefId = cardInfo.fpan,
                                                        productId = cardInfo.fpan.Substring(0, 6),
                                                        cardDetails = cardDetails,
                                                        levelOfTrust = "yellow",
                                                        idvMethodList = idVMetList
                                                    };
                                                    string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/DoCardDigitization','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','200','Successful','Yellow','','{JsonConvert.SerializeObject(cardInfo)}')";
                                                    int insertReqData = InsertTokenizationRec(insRequestData);
                                                    new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                                }
                                                else
                                                {
                                                    response = new CardDigitizationResp()
                                                    {
                                                        errorMessage = "Invalid CVV2",
                                                        responseCode = 161
                                                    };
                                                    string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/DoCardDigitization','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','161','Invalid CVV2','','','{JsonConvert.SerializeObject(cardInfo)}')";
                                                    int insertReqData = InsertTokenizationRec(insRequestData);
                                                    new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                                }
                                            }
                                            else
                                            {
                                                var cardDetails = new Carddetails()
                                                {
                                                    cardholderName = dr["c1_name_on_card"].ToString(),
                                                    fpanBin = cardInfo.fpan.Substring(0, 6),
                                                    fpanLastDigits = cardInfo.fpan.Substring(cardInfo.fpan.Length - 5, 5),
                                                    fpanExpiryDate = cardInfo.exp.Substring(cardInfo.exp.Length - 2, 2) + "/" + cardInfo.exp.Substring(0, 2)
                                                };
                                                var idVMetList = FormIdvMethodList(phoneNo, emailAdd);
                                                response = new CardDigitizationResp()
                                                {
                                                    errorMessage = "Successful",
                                                    responseCode = 200,
                                                    issuerCardRefId = cardInfo.fpan,
                                                    productId = cardInfo.fpan.Substring(0, 6),
                                                    cardDetails = cardDetails,
                                                    levelOfTrust = "yellow",
                                                    idvMethodList = idVMetList
                                                };
                                                //Insert record into the Card Tokenization transaction table

                                                string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/DoCardDigitization','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','200','Successful','Yellow','','{JsonConvert.SerializeObject(cardInfo)}')";
                                                int insertReqData = InsertTokenizationRec(insRequestData);
                                                new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                            }
                                        }
                                        else
                                        {
                                            string holdResp = dr["hold_rsp_code"].ToString();
                                            bool isCardActive = dr["card_status"].ToString() == "1" ? true : false;
                                            //update CardTokenizationData table with card status
                                            string updQuery = $"UPDATE [EchanelsT24].[dbo].[CardTokenizationData] set HotlistStatus = '{holdResp}', IsCardActive = {isCardActive}, Status = 'Inactive', LevelOfTrust = 'red' where IssuerCardRefID = '{request.issuerCardRefId}' and WalletProviderID = '{request.walletProviderId}'";
                                            //int updt = UpdateTokenizationTable(holdResp, isCardActive, "Inactive", "red", request.issuerCardRefId, request.walletProviderId);
                                            int updt = UpdateTokenizationTable(updQuery);
                                            new ErrorLog($"DoCardDigitization - Update response received for records holdResp - {holdResp}, isCardActive - {isCardActive}, Inactive, red, issuerCardREfId - {request.issuerCardRefId}, walletProviderID - {request.walletProviderId} is: {updt}");
                                            response = new CardDigitizationResp()
                                            {
                                                errorMessage = "Card is suspended",
                                                responseCode = 159
                                            };
                                            string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/DoCardDigitization','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','159','Card is suspended','','','{JsonConvert.SerializeObject(cardInfo)}')";
                                            int insertReqData = InsertTokenizationRec(insRequestData);
                                            new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                        }
                                    }
                                }
                                else
                                {
                                    response = new CardDigitizationResp()
                                    {
                                        errorMessage = "Invalid FPAN",
                                        responseCode = 166
                                    };
                                    string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/DoCardDigitization','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','166','Invalid FPAN','','','')";
                                    int insertReqData = InsertTokenizationRec(insRequestData);
                                    new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                }
                            }
                            else
                            {
                                response = new CardDigitizationResp()
                                {
                                    errorMessage = "Invalid FPAN",
                                    responseCode = 166
                                };
                                string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/DoCardDigitization','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','166','Invalid FPAN','','','')";
                                int insertReqData = InsertTokenizationRec(insRequestData);
                                new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                            }
                            //break so as not to pick another record
                            //it is assumed here that the walletProviderID is only added once
                            //otherwise, a count needs to be added to the query
                        }
                        else
                        {
                            //tokenized card exists but the status is not active. 
                            response = new CardDigitizationResp()
                            {
                                errorMessage = "Successful",
                                responseCode = 200,
                                issuerCardRefId = cardInfo.fpan,
                                productId = cardInfo.fpan.Substring(0, 6),
                                cardDetails = null,
                                levelOfTrust = "red",
                                idvMethodList = null
                            };
                            string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/DoCardDigitization','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','200','Successful','Red','','{JsonConvert.SerializeObject(cardInfo)}')";
                            int insertReqData = InsertTokenizationRec(insRequestData);
                            new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                        }
                    }
                    else
                    {
                        //check eligibility is yet to happen
                        response = new CardDigitizationResp()
                        {
                            errorMessage = "Operation failed",
                            responseCode = 911
                        };
                        string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/DoCardDigitization','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','911','Operation failed','','','')";
                        int insertReqData = InsertTokenizationRec(insRequestData);
                        new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                    }
                }  
            }
            catch (Exception ex)
            {
                new ErrorLog(ex);
                response = new CardDigitizationResp()
                {
                    errorMessage = "Unexpected server error",
                    responseCode = 921
                };
                string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/DoCardDigitization','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','921','Unexpected server error','','','')";
                int insertReqData = InsertTokenizationRec(insRequestData);
                new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
            }
            new ErrorLog($"Response returned for DoCardDigitization - {JsonConvert.SerializeObject(response)}");
            return response;
        }
        public SendOTPResponse SendOTP(SendOTPRequest request)
        {
            var response = new SendOTPResponse();
            try
            {
                //use issuer card reference number and walletproviderID to get customer's pan record from the card digitization table and confirm if the digitization is still valid before proceeding.
                //also check postcard to be sure about the status of the card.
                string year = DateTime.Now.Year.ToString();
                string exp = DateTime.Now.Month.ToString("00") + year.Substring(year.Length - 2, 2);
                string pan = IBS_Decrypt(request.issuerCardRefId);
                string queryCheck = $"select * FROM [EchanelsT24].[dbo].[CardTokenizationData] where IssuerCardRefID = '{request.issuerCardRefId}' and Status = 'Active' and WalletProviderID = '{request.walletProviderId}'";
                var tokenData = GetTokenizationRecs(queryCheck);
                bool hasTokenRows = tokenData.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
                if (hasTokenRows)
                {
                    int cnts = tokenData.Tables[0].Rows.Count;
                    if (cnts > 0)
                    {
                        for (int k = 0; k < cnts; k++)
                        {
                            string query = $"select * from pc_cards a with (nolock) inner join pc_customers b with (nolock) on a.customer_id = b.customer_id where pan = '{pan}' and expiry_date > '{exp}'"; // and card_status =  1 and hold_rsp_code is null
                            var data = GetData(query);
                            bool hasRows = data.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
                            if (hasRows)
                            {
                                int cnt = data.Tables[0].Rows.Count;
                                if (cnt > 0)
                                {
                                    for (int j = 0; j < cnt; j++)
                                    {
                                        DataRow dr = data.Tables[0].Rows[j];
                                        //if card status/hold response is changed, remember to update the card tokenization table 
                                        if (dr["card_status"].ToString() == "1" && string.IsNullOrEmpty(dr["hold_rsp_code"].ToString()))
                                        {
                                            //compare masked phone number and email received to be sure it tallies with what is stored. 
                                            //send OTP to customer now.
                                            //string doGenerateOtpAndMailByPhoneNumber(string phoneNumber, string nuban, int Appid, string destinationEmail, string sourceEmail, string subject)
                                            //var appId = 59;
                                            //var appId = 18;
                                            response = new SendOTPResponse()
                                            {
                                                errorMessage = "Successful",
                                                responseCode = 200,
                                                responseMessage = "OTP sent successfully"
                                            };
                                            string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/SendOTP','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','200','Successful','','','{request.walletProviderId}')";
                                            int insertReqData = InsertTokenizationRec(insRequestData);
                                            new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                        }
                                        else
                                        {
                                            response = new SendOTPResponse()
                                            {
                                                errorMessage = "Invalid method ID",
                                                responseCode = 501
                                            };
                                            string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/SendOTP','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','501','Invalid method ID','','','{request.walletProviderId}')";
                                            int insertReqData = InsertTokenizationRec(insRequestData);
                                            new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                        }
                                    }
                                }
                                else
                                {
                                    response = new SendOTPResponse()
                                    {
                                        errorMessage = "Operation failed",
                                        responseCode = 911
                                    };
                                    string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/SendOTP','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','911','Operation failed','','','{request.walletProviderId}')";
                                    int insertReqData = InsertTokenizationRec(insRequestData);
                                    new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                }
                            }
                            else
                            {
                                response = new SendOTPResponse()
                                {
                                    errorMessage = "Operation failed",
                                    responseCode = 911
                                };
                                string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/SendOTP','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','911','Operation failed','','','{request.walletProviderId}')";
                                int insertReqData = InsertTokenizationRec(insRequestData);
                                new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                            }
                        }
                    }
                    else
                    {
                        response = new SendOTPResponse()
                        {
                            errorMessage = "Operation failed",
                            responseCode = 911
                        };
                        string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/SendOTP','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','911','Operation failed','','','{request.walletProviderId}')";
                        int insertReqData = InsertTokenizationRec(insRequestData);
                        new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                    }
                }
                else
                {
                    response = new SendOTPResponse()
                    {
                        errorMessage = "Operation failed",
                        responseCode = 911
                    };
                    string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/SendOTP','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','911','Operation failed','','','{request.walletProviderId}')";
                    int insertReqData = InsertTokenizationRec(insRequestData);
                    new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                }
            }
            catch (Exception ex)
            {
                response = new SendOTPResponse()
                {
                    responseCode = 921,
                    errorMessage = "Unexpected server error"
                };
                new ErrorLog(ex);
                string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/SendOTP','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','921','Unexpected server error','','','')";
                int insertReqData = InsertTokenizationRec(insRequestData);
                new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
            }
            new ErrorLog($"Response returned for SendOTP - {JsonConvert.SerializeObject(response)}");
            return response;
        }
        public VirtualCardChangeResp DoVirtualCardChange(VirtualCardChangeReq request)
        {
            var response = new VirtualCardChangeResp();
            try 
            {
                new ErrorLog($"VirtualCardChangeResp request is: {JsonConvert.SerializeObject(request)}");
                //query the card tokenization data table to confirm the status of the tokenized card.
                //where record does not exist and action is activate, do a request card digitization 
                string queryCheck = $"select * FROM [EchanelsT24].[dbo].[CardTokenizationData] where IssuerCardRefID = '{request.issuerCardRefId}' and WalletProviderID = '{request.walletProviderId}'";
                var tokenData = GetTokenizationRecs(queryCheck);
                bool hasTokenRows = tokenData.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
                if (hasTokenRows)
                {
                    int cnts = tokenData.Tables[0].Rows.Count;
                    if (cnts > 0)
                    {
                        //if count is greater than 0, update required status.
                        int updt = 0;
                        switch (request.action)
                        {
                            case "ACTIVATE":
                                //set card status to active
                                //should have token present and update status
                                string updQuery = $"UPDATE [EchanelsT24].[dbo].[CardTokenizationData] set Status = 'active' where IssuerCardRefID = '{request.issuerCardRefId}' and WalletProviderID = '{request.walletProviderId}'";
                                updt = UpdateTokenizationTable(updQuery);
                                break;
                            case "SUSPEND":
                                //set card status to inactive
                                string updQuery1 = $"UPDATE [EchanelsT24].[dbo].[CardTokenizationData] set Status = 'inactive' where IssuerCardRefID = '{request.issuerCardRefId}' and WalletProviderID = '{request.walletProviderId}'";
                                updt = UpdateTokenizationTable(updQuery1);
                                break;
                            case "RESUME":
                                //set card status to active
                                string updQuery2 = $"UPDATE [EchanelsT24].[dbo].[CardTokenizationData] set Status = 'active' where IssuerCardRefID = '{request.issuerCardRefId}' and WalletProviderID = '{request.walletProviderId}'";
                                updt = UpdateTokenizationTable(updQuery2);
                                break;
                            case "DELETE":
                                //delete tokenization record from tokenization table
                                string delQuery = $"DELETE FROM [EchanelsT24].[dbo].[CardTokenizationData] where IssuerCardRefID = '{request.issuerCardRefId}' and WalletProviderID = '{request.walletProviderId}'";
                                updt = DeleteTokenizationRec(delQuery);
                                break;
                            case "UPDATE":
                                //update pan value to whatever is sent. Where is the pan sent?
                                break;
                            case "ERASE":
                                //delete tokenization record from tokenization table
                                string delQuery1 = $"DELETE FROM [EchanelsT24].[dbo].[CardTokenizationData] where IssuerCardRefID = '{request.issuerCardRefId}' and WalletProviderID = '{request.walletProviderId}'";
                                updt = DeleteTokenizationRec(delQuery1);
                                break;
                            default:
                                //do nothing and respond failure
                                break;
                        }
                        if(updt > 0)
                        {
                            response = new VirtualCardChangeResp()
                            {
                                errorMessage = "Successful",
                                responseCode = 200,
                                responseMessage = "Virtual card change notification received Successfully."
                            };
                            string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/DoVirtualCardChange','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','200','Successful','','','{request.walletCardRefId}')";
                            int insertReqData = InsertTokenizationRec(insRequestData);
                            new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                        }
                        else
                        {
                            new ErrorLog($"Update response in method DoVirtualCardChange is: {updt} for update request {JsonConvert.SerializeObject(request)}");
                            response = new VirtualCardChangeResp()
                            {
                                responseCode = 911,
                                errorMessage = "Operation failed"
                            };
                            string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/DoVirtualCardChange','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','911','Operation failed','','','')";
                            int insertReqData = InsertTokenizationRec(insRequestData);
                            new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                        }
                    }
                    else
                    {
                        response = new VirtualCardChangeResp()
                        {
                            responseCode = 911,
                            errorMessage = "Operation failed"
                        };
                        string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/DoVirtualCardChange','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','911','Operation failed','','','')";
                        int insertReqData = InsertTokenizationRec(insRequestData);
                        new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                    }
                }
                else
                {
                    response = new VirtualCardChangeResp()
                    {
                        responseCode = 911,
                        errorMessage = "Operation failed"
                    };
                    string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/DoVirtualCardChange','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','911','Operation failed','','','')";
                    int insertReqData = InsertTokenizationRec(insRequestData);
                    new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                }
            }
            catch (Exception ex)
            {
                response = new VirtualCardChangeResp()
                {
                    responseCode = 921,
                    errorMessage = "Unexpected server error"
                };
                new ErrorLog(ex);
                string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/DoVirtualCardChange','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','921','Unexpected server error','','','')";
                int insertReqData = InsertTokenizationRec(insRequestData);
                new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
            }
            new ErrorLog($"Response returned for DoVirtualCardChange - {JsonConvert.SerializeObject(response)}");
            return response;
        }
        public Idvmethodlist[] FormIdvMethodList (string phoneNo, string email)
        {
            try
            {
                if (string.IsNullOrEmpty(phoneNo) && !string.IsNullOrEmpty(email))
                {
                    //email is populated, phone no is empty
                    var splitEmail = email.Split('@');
                    string maskedEmail = $"*****{splitEmail[0].Substring(splitEmail[0].Length - 3, 3)}{splitEmail[1]}";
                    return new Idvmethodlist[] { new Idvmethodlist { id = "1", value = maskedEmail, type = "Email Address" } };
                }
                else if (!string.IsNullOrEmpty(phoneNo) && string.IsNullOrEmpty(email))
                {
                    //email is empty, phone no is populated
                    string maskedPhoneNo = $"********{phoneNo.Substring(phoneNo.Length - 3, 3)}";
                    return new Idvmethodlist[] { new Idvmethodlist { id = "1", value = maskedPhoneNo, type = "Phone Number" } };
                }
                else if (string.IsNullOrEmpty(phoneNo) && string.IsNullOrEmpty(email))
                {
                    //email is empty, phone no is empty
                    return null;
                }
                else
                {
                    //email is not empty, phone no is not empty
                    var splitEmail = email.Split('@');
                    string maskedEmail = $"*****{splitEmail[0].Substring(splitEmail[0].Length - 3, 3)}{splitEmail[1]}";
                    string maskedPhoneNo = $"********{phoneNo.Substring(phoneNo.Length - 3, 3)}";
                    return new Idvmethodlist[] { new Idvmethodlist { id = "1", value = maskedPhoneNo, type = "Phone Number" }, new Idvmethodlist { id = "2", value = maskedEmail, type = "Email Address" } };
                }
            }
            catch (Exception ex)
            {
                new ErrorLog($"Exception in method FormIdvMethodList - {ex}");
                return null;
            }
        }
        public GetIDnVMethodListResp GetIDnVMethodList(GetIDnVMethodListReq request)
        {
            var response = new GetIDnVMethodListResp();
            try
            {
                var bankService = new BankSoap.banksSoapClient();
                //get email and phone number from core banking.
                //check card tokenization table email and phone number to be sure the records are still correct
                //otherwise, update card tokenization table with what was gotten from core banking
                //afterwards, send response for request
                string pan = IBS_Decrypt(request.issuerCardRefId);

                new ErrorLog($"GetIDnVMethodList request is: {JsonConvert.SerializeObject(request)}");
                string year = DateTime.Now.Year.ToString();
                string month = DateTime.Now.Month.ToString();
                string exp = year.Substring(year.Length - 2, 2) + month;
                string query = $"select * from pc_cards where pan = '{pan}' and expiry_date >= '{exp}'";
                var data = GetData(query);
                bool hasRows = data.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
                if (hasRows)
                {
                    int cnt = data.Tables[0].Rows.Count;
                    if (cnt > 0)
                    {
                        for (int j = 0; j < cnt; j++)
                        {
                            DataRow dr = data.Tables[0].Rows[j];
                            if (dr["card_status"].ToString() == "1" && string.IsNullOrEmpty(dr["hold_rsp_code"].ToString()))
                            {
                                string custID = dr["customer_id"].ToString().Trim();
                                var customerInfo = bankService.getCustomrInfo(custID);
                                //check its not empty
                                if (customerInfo.Tables[0].Rows.Count > 0)
                                {
                                    string phoneNo = customerInfo.Tables[0].Rows[0]["ContactMobile1"].ToString().Trim();
                                    string emailAdd = customerInfo.Tables[0].Rows[0]["ContactEmail"].ToString().Trim();
                                    //get data on tokenization table and compare. If not same, update email and phone number then respond on successful update.
                                    string queryCheck = $"select * FROM [EchanelsT24].[dbo].[CardTokenizationData] where IssuerCardRefID = '{request.issuerCardRefId}' and WalletProviderID = '{request.walletProviderId}'";
                                    var tokenData = GetTokenizationRecs(queryCheck);
                                    bool hasTokenRows = tokenData.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
                                    if (hasTokenRows)
                                    {
                                        int cnts = tokenData.Tables[0].Rows.Count;
                                        if (cnts > 0)
                                        {
                                            //here, check the status. if active, proceed otherwise, report level of trust to be red
                                            DataRow drs = tokenData.Tables[0].Rows[0];
                                            string tokenizedPhone = drs["PhoneNumber"].ToString().Trim();
                                            string tokenizedEmail = drs["EmailAddress"].ToString().Trim();
                                            
                                            if(phoneNo == tokenizedPhone && emailAdd == tokenizedEmail)
                                            {
                                                //populate the idnvmethodlist
                                                var idVMetList = FormIdvMethodList(phoneNo, emailAdd);
                                                response = new GetIDnVMethodListResp()
                                                {
                                                    errorMessage = "Successful",
                                                    responseCode = 200,
                                                    idvMethodList = idVMetList
                                                };
                                                //idvMethodList = new Idvmethodlist[] { new Idvmethodlist { id = "1", value = phoneNo, type = "Phone Number" }, new Idvmethodlist { id = "2", value = emailAdd, type = "Email Address" } }
                                                string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/GetIDnVMethodListResp','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','200','Successful','','','{request.walletProviderId}')";
                                                int insertReqData = InsertTokenizationRec(insRequestData);
                                                new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                            }
                                            else
                                            {
                                                //update the record in the tokenization data table to the email and phone number from core banking
                                                string updQuery = $"UPDATE [EchanelsT24].[dbo].[CardTokenizationData] set PhoneNumber = '{phoneNo}', EmailAddress = '{emailAdd}' where IssuerCardRefID = '{request.issuerCardRefId}' and WalletProviderID = '{request.walletProviderId}'";
                                                int upd = UpdateTokenizationTable(updQuery);
                                                if(upd > 0)
                                                {
                                                    //return necessary info
                                                    var idVMetList = FormIdvMethodList(phoneNo, emailAdd);
                                                    response = new GetIDnVMethodListResp()
                                                    {
                                                        errorMessage = "Successful",
                                                        responseCode = 200,
                                                        idvMethodList = idVMetList
                                                    };
                                                    //idvMethodList = new Idvmethodlist[] { new Idvmethodlist { id = "1", value = phoneNo, type = "Phone Number" }, new Idvmethodlist { id = "2", value = emailAdd, type = "Email Address" } }
                                                    string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('{request.issuerCardRefId}','','api/GetIDnVMethodListResp','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','200','Successful','','','{request.walletProviderId}')";
                                                    int insertReqData = InsertTokenizationRec(insRequestData);
                                                    new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                                }
                                                else
                                                {
                                                    new ErrorLog($"Update response in method GetIDnVMethodList is: {upd} for update request {JsonConvert.SerializeObject(request)}");
                                                    response = new GetIDnVMethodListResp()
                                                    {
                                                        responseCode = 911,
                                                        errorMessage = "Operation failed"
                                                    };
                                                    string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/GetIDnVMethodList','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','911','Operation failed','','','')";
                                                    int insertReqData = InsertTokenizationRec(insRequestData);
                                                    new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            response = new GetIDnVMethodListResp()
                                            {
                                                responseCode = 510,
                                                errorMessage = "Cardholder verification declined"
                                            };
                                            string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/GetIDnVMethodList','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','510','Cardholder verification declined','','','')";
                                            int insertReqData = InsertTokenizationRec(insRequestData);
                                            new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                        }
                                    }
                                    else
                                    {
                                        response = new GetIDnVMethodListResp()
                                        {
                                            responseCode = 510,
                                            errorMessage = "Cardholder verification declined"
                                        };
                                        string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/GetIDnVMethodList','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','510','Cardholder verification declined','','','')";
                                        int insertReqData = InsertTokenizationRec(insRequestData);
                                        new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                    }
                                }
                                else
                                {
                                    response = new GetIDnVMethodListResp()
                                    {
                                        responseCode = 510,
                                        errorMessage = "Cardholder verification declined"
                                    };
                                    string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/GetIDnVMethodList','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','510','Cardholder verification declined','','','')";
                                    int insertReqData = InsertTokenizationRec(insRequestData);
                                    new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                                }


                            }
                            else
                            {
                                response = new GetIDnVMethodListResp()
                                {
                                    responseCode = 510,
                                    errorMessage = "Cardholder verification declined"
                                };
                                string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/GetIDnVMethodList','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','510','Cardholder verification declined','','','')";
                                int insertReqData = InsertTokenizationRec(insRequestData);
                                new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                            }
                        }
                    }
                    else
                    {
                        response = new GetIDnVMethodListResp()
                        {
                            responseCode = 510,
                            errorMessage = "Cardholder verification declined"
                        };
                        string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/GetIDnVMethodList','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','510','Cardholder verification declined','','','')";
                        int insertReqData = InsertTokenizationRec(insRequestData);
                        new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                    }
                }
                else
                {
                    response = new GetIDnVMethodListResp()
                    {
                        responseCode = 510,
                        errorMessage = "Cardholder verification declined"
                    };
                    string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/GetIDnVMethodList','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','510','Cardholder verification declined','','','')";
                    int insertReqData = InsertTokenizationRec(insRequestData);
                    new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
                }
            }
            catch (Exception ex)
            {
                new ErrorLog(ex);
                response = new GetIDnVMethodListResp()
                {
                    responseCode = 921,
                    errorMessage = "Unexpected server error"
                };
                new ErrorLog(ex);
                string insRequestData = $"INSERT INTO [dbo].[CardTokenizationTransactions] VALUES ('','','api/GetIDnVMethodList','{JsonConvert.SerializeObject(request)}','{JsonConvert.SerializeObject(response)}','921','Unexpected server error','','','')";
                int insertReqData = InsertTokenizationRec(insRequestData);
                new ErrorLog($"{DateTime.Now}: Response - {insertReqData} returned for insert query: {insRequestData}.");
            }
            new ErrorLog($"Response returned for GetIDnVMethodList - {JsonConvert.SerializeObject(response)}");
            return response;
        }
        private DataSet GetData(string query)
        {
            DataSet records;
            try
            {
                FEPConn conn = new FEPConn(query);
                records = conn.query("recs");
            }
            catch(Exception ex)
            {
                records = null;
                new ErrorLog(ex);
            }
            return records;
        }
        public DataSet GetTokenizationRecs(string sql)
        {
            var recs = new DataSet();
            //string sql = "SELECT * FROM CardRequest WHERE STATUSFLAG = '10' ";
            //string sql = "SELECT * FROM CardRequest WHERE ID = '1189368' ";
            Connect cn = new Connect("CardApp");
            cn.Persist = true;
            cn.SetSQL(sql);
            recs = cn.Select();
            cn.CloseAll();

            return recs;
        }
        public int DeleteTokenizationRec(string sql)
        {
            int del;
            Connect cn = new Connect("CardApp");
            cn.Persist = true;
            cn.SetSQL(sql);
            del = cn.Delete();
            cn.CloseAll();
            return del;
        }
        public int UpdateTokenizationTable(string sql)
        {
            int upd;
            //string sql = $"UPDATE [EchanelsT24].[dbo].[CardTokenizationData] set HotlistStatus = '{hotlistStat}', IsCardActive = {isCardActive}, Status = '{status}', LevelOfTrust = '{levelOfTrust}' where IssuerCardRefID = '{issuerCardRefID}' and WalletProviderID = '{walletProviderID}'";
            Connect cn = new Connect("CardApp");
            cn.Persist = true;
            cn.SetSQL(sql);
            upd = cn.Update();
            cn.CloseAll();
            return upd;
        }
        private int InsertTokenizationRec(string sql)
        {
            //INSERT INTO [dbo].[CardTokenizationData] VALUES (@Pan,@IssuerCardRefID,@VirtualCardID,@CardExpiryDate,@LevelOfTrust,@Status,@PhoneNumber,@EmailAddress,@IsCardActive,@HotlistStatus,@CaptureMethod, @WalletProviderID,@WalletCardRefID)
            //INSERT INTO [dbo].[CardTokenizationTransactions] VALUES (@IssuerCardRefID,@VirtualCardID,@EndPointCalled,@RequestData,@ResponseData,@ResponseCode,@ErrorMessage,@LevelOfTrust,@IDVMethodList,@CardDetails)
            int ins = 0;
            string isql = sql;
            new ErrorLog("Sql to excecute is ==> " + isql);
            Connect un = new Connect("CardApp")
            {
                Persist = true
            };
            un.SetSQL(isql);
            ins = un.Update();
            un.CloseAll();
            new ErrorLog(ins + "rows inserted");
            return ins;
        }
        public static string Encrypt(String val)
        {
            var pp = string.Empty;
            try
            {
                MemoryStream ms = new MemoryStream();
                //string rsp = "";
                try
                {
                    var sharedkeyval = "000000010000001000000011000001010000011100001011000011010001000100010010000100010000110100001011000001110000001100000100000010000000000100000010000000110000010100000111000010110000110100001101";
                    sharedkeyval = BinaryToString(sharedkeyval);
                    var sharedvectorval = "0000000100000010000000110000010100000111000010110000110100000011";
                    sharedvectorval = BinaryToString(sharedvectorval);
                    //sharedvectorval = BinaryToString(sharedvectorval);
                    byte[] sharedkey = System.Text.Encoding.GetEncoding("utf-8").GetBytes(sharedkeyval);
                    byte[] sharedvector = System.Text.Encoding.GetEncoding("utf-8").GetBytes(sharedvectorval);

                    TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
                    byte[] toEncrypt = Encoding.UTF8.GetBytes(val);

                    CryptoStream cs = new CryptoStream(ms, tdes.CreateEncryptor(sharedkey, sharedvector), CryptoStreamMode.Write);
                    cs.Write(toEncrypt, 0, toEncrypt.Length);
                    cs.FlushFinalBlock();
                    pp = Convert.ToBase64String(ms.ToArray());
                }
                catch (Exception ex)
                {
                    new ErrorLog(ex);
                    Console.WriteLine("Error encrypting pan " + pp.Substring(0, 6) + " * ".PadLeft(pp.Length - 10, '*') + pp.Substring(pp.Length - 4, 4) + " is " + ex.ToString());
                    pp = val;
                }
            }
            catch (Exception ex)
            {
                new ErrorLog(ex);
            }
            return pp;
        }
        public static string IBS_Decrypt(string val)
        {
            var pp = string.Empty;
            try
            {
                var sharedkeyval = "000000010000001000000011000001010000011100001011000011010001000100010010000100010000110100001011000001110000001100000100000010000000000100000010000000110000010100000111000010110000110100001101";
                sharedkeyval = BinaryToString(sharedkeyval);
                var sharedvectorval = "0000000100000010000000110000010100000111000010110000110100000011";
                sharedvectorval = BinaryToString(sharedvectorval);
                byte[] sharedkey = Encoding.GetEncoding("utf-8").GetBytes(sharedkeyval);
                byte[] sharedvector = Encoding.GetEncoding("utf-8").GetBytes(sharedvectorval);
                TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
                byte[] toDecrypt = Convert.FromBase64String(val);
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, tdes.CreateDecryptor(sharedkey, sharedvector), CryptoStreamMode.Write);
                cs.Write(toDecrypt, 0, toDecrypt.Length);
                cs.FlushFinalBlock();
                pp = Encoding.UTF8.GetString(ms.ToArray());
            }
            catch (Exception ex)
            {
                new ErrorLog(ex);
                pp = val;
            }
            return pp;
        }
        private static string BinaryToString(string binary)
        {
            if (string.IsNullOrEmpty(binary))
                throw new ArgumentNullException("binary");

            if ((binary.Length % 8) != 0)
                throw new ArgumentException("Binary string invalid (must divide by 8)", "binary");

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < binary.Length; i += 8)
            {
                string section = binary.Substring(i, 8);
                int ascii = 0;
                try
                {
                    ascii = Convert.ToInt32(section, 2);
                }
                catch
                {
                    throw new ArgumentException("Binary string contains invalid section: " + section, "binary");
                }
                builder.Append((char)ascii);
            }
            return builder.ToString();
        }
    }
}