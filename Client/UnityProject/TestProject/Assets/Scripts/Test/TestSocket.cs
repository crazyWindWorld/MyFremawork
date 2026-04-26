using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetFramework;
using UnityEngine;

public class TestSocket : MonoBehaviour
{
    // Start is called before the first frame update
    async void Start()
    {
        await NetworkManager.Instance.Connect(NetworkConfig.ProtocolType.Tcp,"127.0.0.1", 9000);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
