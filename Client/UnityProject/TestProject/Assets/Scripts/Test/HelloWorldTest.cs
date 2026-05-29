using UnityEngine;

public class HelloWorldTest : MonoBehaviour
{
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 200, 30), "Hello World");
    }
}