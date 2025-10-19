using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class CoreLibWrapper
{
#if UNITY_IOS && !UNITY_EDITOR
        const string DLL_NAME = "__Internal";
#else
    const string DLL_NAME = "corelib";
#endif

    [DllImport(DLL_NAME)]
    private static extern IntPtr GetCurrentTime();

    public static string GetTimeString()
    {
        IntPtr ptr = GetCurrentTime();
        return Marshal.PtrToStringAnsi(ptr);
    }
}