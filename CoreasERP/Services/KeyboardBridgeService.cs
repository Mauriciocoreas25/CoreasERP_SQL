using Microsoft.JSInterop;

namespace CoreasERP.Services;

public static class KeyboardBridgeService
{
    public static event Action? OnF1Pressed;
    public static event Action? OnF2Pressed;
    public static event Action? OnF6Pressed;
    public static event Action? OnF10Pressed;
    public static event Action? OnSpacePressed;
    public static event Action? OnEnterPressed;
    public static event Action<int>? OnFunctionKeyPressed;
    public static event Action<string>? OnShortcutPressed;
    public static event Action<string>? OnBarcodeScanned;

    [JSInvokable("DispatchGlobalFunctionKey")]
    public static void DispatchGlobalFunctionKey(int fNum)
    {
        OnFunctionKeyPressed?.Invoke(fNum);
        if (fNum == 1) OnF1Pressed?.Invoke();
        if (fNum == 2) OnF2Pressed?.Invoke();
        if (fNum == 6) OnF6Pressed?.Invoke();
        if (fNum == 10) OnF10Pressed?.Invoke();
    }

    [JSInvokable("DispatchGlobalShortcut")]
    public static void DispatchGlobalShortcut(string shortcut)
    {
        if (!string.IsNullOrWhiteSpace(shortcut))
        {
            OnShortcutPressed?.Invoke(shortcut.Trim());
        }
    }

    [JSInvokable("DispatchGlobalF1")]
    public static void DispatchGlobalF1()
    {
        OnF1Pressed?.Invoke();
        OnFunctionKeyPressed?.Invoke(1);
    }

    [JSInvokable("DispatchGlobalF2")]
    public static void DispatchGlobalF2()
    {
        OnF2Pressed?.Invoke();
        OnFunctionKeyPressed?.Invoke(2);
    }

    [JSInvokable("DispatchGlobalF10")]
    public static void DispatchGlobalF10()
    {
        OnF10Pressed?.Invoke();
        OnFunctionKeyPressed?.Invoke(10);
    }

    [JSInvokable("DispatchGlobalSpace")]
    public static void DispatchGlobalSpace()
    {
        OnSpacePressed?.Invoke();
    }

    [JSInvokable("DispatchGlobalEnter")]
    public static void DispatchGlobalEnter()
    {
        OnEnterPressed?.Invoke();
    }

    [JSInvokable("DispatchBarcodeScanned")]
    public static void DispatchBarcodeScanned(string barcode)
    {
        if (!string.IsNullOrWhiteSpace(barcode))
        {
            OnBarcodeScanned?.Invoke(barcode.Trim());
        }
    }
}
