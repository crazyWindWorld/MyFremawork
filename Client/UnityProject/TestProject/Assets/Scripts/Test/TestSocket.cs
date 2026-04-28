using System.Collections;
using System.Collections.Generic;
using LoginPB;
using NetFramework.Attributes;
using NetFramework.Core;
using UnityEngine;
using UnityEngine.UI;

public class TestSocket : MonoBehaviour
{
    public Button _loginBtn;
    public Text _logText;
    void Awake()
    {
        _logText.text = "等待登录";
        _loginBtn.onClick.AddListener(LoginReq);

        NetworkManager.Instance.Connect("127.0.0.1", 9000);
        NetHandlerGenerated.RegisterAll();
        ProtoCmds.RegisterAll();
    }
    // Start is called before the first frame update
    void Start()
    {

    }
    public void LoginReq()
    {
        _logText.text = "登录中";
        NetworkManager.Instance.Send(new LoginReq());

    }
    [NetMessageHandler(typeof(LoginRsp))]
    public static void LoginResp(LoginRsp resp)
    {
        Debug.Log($"登录成功: {resp.Result}");
    }
    // Update is called once per frame
    void Update()
    {

    }
}
