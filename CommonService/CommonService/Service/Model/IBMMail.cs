using System.Xml.Serialization;

namespace CommonService.Service.Model;

#region Config

public class IBMMail
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string RefreshToken { get; set; }
    public string BaseUrl { get; set; }
    public string OathPath { get; set; }
    public string AddContactPath { get; set; }
    public string SendPath { get; set; }
    public IEnumerable<CurrencySetting> CurrencySettingList { get; set; }
}
public class CurrencySetting
{
    public int CurrencyId { get; set; }
    public int MailingId { get; set; }
}

#endregion

#region Oauth Model

public class OauthResponse
{
    public string access_token { get; set; }
    public string token_type { get; set; }
    public string refresh_token { get; set; }
    public int expires_in { get; set; }
    public string error { get; set; }
    public string error_description { get; set; }
}

#endregion

#region Add Contact and Send Model

#region Add Contact Request

[XmlRoot("Envelope")]
public class AddContactRequest
{
    [XmlElement("Body")]
    public AddContactRequestBody Body { get; set; }

    public AddContactRequest()
    {

    }

    public AddContactRequest(int listId, int createdFrom, string userName, string email, string isoCode, string countryCode)
    {
        Body = new AddContactRequestBody
        {
            AddRecipient = new AddRecipient
            {
                ListId = listId,
                CreatedFrom = createdFrom,
                Columns = new List<IBMColumn>
                {
                    new IBMColumn { Name = "Email", Value = email },
                    new IBMColumn { Name = "Username", Value = userName },
                    new IBMColumn { Name = "Currency", Value = isoCode },
                    new IBMColumn { Name = "OddsType", Value = countryCode }
                }
            }
        };
    }
}

public class AddContactRequestBody
{
    [XmlElement("AddRecipient")]
    public AddRecipient AddRecipient { get; set; }
}

public class AddRecipient
{
    [XmlElement("LIST_ID")]
    public int ListId { get; set; }

    [XmlElement("CREATED_FROM")]
    public int CreatedFrom { get; set; }

    [XmlElement("COLUMN")]
    public List<IBMColumn> Columns { get; set; }
}

public class IBMColumn
{
    [XmlElement("NAME")]
    public string Name { get; set; }

    [XmlElement("VALUE")]
    public string Value { get; set; }
}


#endregion

#region Send Request

[XmlRoot("Envelope")]
public class SendRequest
{
    [XmlElement("Body")]
    public SendRequestBody Body { get; set; }

    public SendRequest()
    {

    }

    public SendRequest(int mailingId, string email)
    {
        Body = new SendRequestBody
        {
            SendMailing = new SendMailing
            {
                MailingId = mailingId,
                RecipientEmail = email
            }
        };
    }
}

public class SendRequestBody
{
    [XmlElement("SendMailing")]
    public SendMailing SendMailing { get; set; }
}

public class SendMailing
{
    [XmlElement("MailingId")]
    public int MailingId { get; set; }

    [XmlElement("RecipientEmail")]
    public string RecipientEmail { get; set; }

}

#endregion


#region Common Response

[XmlRoot("Envelope")]
public class AddContactAndSendResponse
{
    [XmlElement("Body")]
    public ResponseBody Body { get; set; }
}

public class ResponseBody
{
    [XmlElement("RESULT")]
    public ResponseResult Result { get; set; }

    [XmlElement("Fault")]
    public ResponseFault Fault { get; set; }
}

public class ResponseResult
{
    [XmlElement("SUCCESS")]
    public string Success { get; set; }

    [XmlElement("RecipientId")]
    public string RecipientId { get; set; }

    [XmlElement("ORGANIZATION_ID")]
    public string OrganizationId { get; set; }
}

public class ResponseFault
{
    [XmlElement("Request")]
    public string Request { get; set; }

    [XmlElement("FaultCode")]
    public string FaultCode { get; set; }

    [XmlElement("FaultString")]
    public string FaultString { get; set; }

    [XmlElement("detail")]
    public ResponseDetail Detail { get; set; }
}

public class ResponseDetail
{
    [XmlElement("error")]
    public ResponseError Error { get; set; }
}

public class ResponseError
{
    [XmlElement("errorid")]
    public string ErrorId { get; set; }

    [XmlElement("module")]
    public string Module { get; set; }

    [XmlElement("class")]
    public string Class { get; set; }

    [XmlElement("method")]
    public string Method { get; set; }
}

#endregion

#endregion

