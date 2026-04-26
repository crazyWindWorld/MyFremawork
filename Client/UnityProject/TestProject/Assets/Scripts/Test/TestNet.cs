
using CommonPB;
using LoginPB;
using NetFramework.Attributes;

public class TestNet
{
    [NetMessageHandler(typeof(PONG), typeof(PING))]
    public static void Pong(PONG msg, PING request)
    {
        
    }
    [NetMessageHandler(typeof(AcegoResetPwdRsp))]
    public static  void AcegoResetPwdRsp(AcegoResetPwdRsp msg)
    {

    }
}