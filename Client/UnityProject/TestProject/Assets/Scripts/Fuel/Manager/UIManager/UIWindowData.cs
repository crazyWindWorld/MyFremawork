using System;

namespace Manager.UIManager
{
    // Fix #7: removed WindowId property — UIWindow.WindowId is the source of truth (abstract
    // property on the window class). Having a duplicate writable WindowId here with no framework
    // enforcement created silent inconsistencies between the two values.
    [Serializable]
    public class UIWindowData
    {
    }
}
