using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums;

public enum MethodType
{
    Ping = 1, //NoUse
    UserBalance = 2,
    OrderTransfer = 3,
    CalcTransfer = 4
}

/// <summary>
/// ErrorCode, Reference From Offical Spec.
/// </summary>
/// <remarks>
/// https://www.jsonrpc.org/specification
/// -32700           Parse error     ：Invalid JSON was received by the server. An error occurred on the server while parsing the JSON text.
/// -32600           Invalid Request ：The JSON sent is not a valid Request object.
/// -32601           Method not found：The method does not exist / is not available.
/// -32602           Invalid params  ：Invalid method parameter(s).
/// -32603           Internal error  ：Internal JSON-RPC error.
/// -32000 to -32099 Server error    ：Reserved for implementation-defined server-errors.
/// </remarks>
public enum BGErrorCode
{
    ParseError = -32700,
    InvalidRequest = -32600,
    MethodNotFound = -32601,
    InvalidParams = -32602,
    InternalError = -32603,
    ServerError = -32000
}

public enum BettransLC_BGTypeEnum
{
    OrderTransfer = 1,
    CalcTransfer = 2
}
