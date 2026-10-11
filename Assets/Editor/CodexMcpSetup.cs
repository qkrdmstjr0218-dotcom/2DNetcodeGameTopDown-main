#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport.Transports;

public static class CodexMcpSetup
{
    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        if (EditorPrefs.GetBool("CodexMcpSetup.Configured", false)) return;
        EditorApplication.delayCall += Configure;
    }

    public static void Configure()
    {
        var config = EditorConfigurationCache.Instance;
        config.SetUseHttpTransport(false);
        var localUvx=Path.GetFullPath(Path.Combine(Application.dataPath,
            "../LocalTools/python-packages/bin/uvx.exe"));
        if(File.Exists(localUvx))config.SetUvxPathOverride(localUvx);
        StdioBridgeHost.StartAutoConnect();
        EditorPrefs.SetBool("CodexMcpSetup.Configured", true);
        Debug.Log("[Codex MCP] Configured local stdio bridge.");
    }
}
#endif
