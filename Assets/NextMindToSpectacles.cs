using UnityEngine;
using System.Net.WebSockets;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using System;

public class NextMindToSpectacles : MonoBehaviour
{
    [Header("WebSocket Settings")]
    public string serverUrl = "ws://localhost:3000";

    [Header("Visual Feedback")]
    public GameObject triggerObject;
    public Material triggeredMaterial;
    public Material normalMaterial;

    private ClientWebSocket webSocket;
    private CancellationTokenSource cancellationToken;
    private bool isConnected = false;

    async void Start()
    {
        Debug.Log("🧠 === EEG TO SPECTACLES BRIDGE STARTING ===");
        Debug.Log("📡 Server URL: " + serverUrl);

        // Setup visual feedback
        if (triggerObject && normalMaterial)
        {
            triggerObject.GetComponent<Renderer>().material = normalMaterial;
            Debug.Log("✅ Visual feedback object configured");
        }
        else
        {
            Debug.Log("⚠️ No visual feedback object assigned");
        }

        // Connect to WebSocket server
        Debug.Log("🔌 Attempting to connect to WebSocket server...");
        await ConnectToServer();
    }

    async Task ConnectToServer()
    {
        Debug.Log("🔍 Checking if server is available...");

        try
        {
            webSocket = new ClientWebSocket();
            cancellationToken = new CancellationTokenSource();

            Uri serverUri = new Uri(serverUrl);
            Debug.Log("🌐 Connecting to: " + serverUri);

            await webSocket.ConnectAsync(serverUri, cancellationToken.Token);

            isConnected = true;
            Debug.Log("🚀 ✅ SUCCESS! Connected to WebSocket server!");
            Debug.Log("📡 Ready to send neural triggers to Spectacles!");
        }
        catch (Exception ex)
        {
            Debug.Log("❌ Server connection failed: " + ex.Message);
            Debug.Log("⚠️ This is OK - EEG will still work locally");
            Debug.Log("💡 To connect to Spectacles, start the Node.js server first");
            isConnected = false;
        }
    }

    async Task SendWebSocketMessage(string message)
    {
        Debug.Log("📤 Attempting to send message: " + message);

        if (webSocket == null || webSocket.State != WebSocketState.Open)
        {
            Debug.Log("❌ WebSocket not connected - message not sent");
            Debug.Log("💡 Start Node.js server to enable Spectacles connection");
            return;
        }

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, cancellationToken.Token);
            Debug.Log("✅ SUCCESS! Message sent to Spectacles: " + message);
            Debug.Log("🔮 Spectacles should animate now!");
        }
        catch (Exception ex)
        {
            Debug.LogError("❌ Failed to send message: " + ex.Message);
        }
    }

    // THIS IS THE KEY METHOD - Call this from your EEG trigger!
    public void OnEEGTrigger()
    {
        Debug.Log("🧠⚡ === EEG NEURAL EVENT TRIGGERED! ===");
        Debug.Log("🎯 Processing neural trigger...");

        // Visual feedback
        Debug.Log("🎨 Starting visual feedback animation");
        StartCoroutine(FlashTriggerObject());

        // Send to Spectacles via WebSocket
        Debug.Log("📡 Sending neural trigger to Spectacles...");
        _ = SendWebSocketMessage("neural_event_triggered");

        Debug.Log("✅ EEG trigger processing complete!");
    }

    // For testing - manual trigger with spacebar
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnEEGTrigger();
        }

        // Reconnect if disconnected
        if (!isConnected && webSocket?.State != WebSocketState.Open)
        {
            _ = Task.Run(async () => await ConnectToServer());
        }
    }

    System.Collections.IEnumerator FlashTriggerObject()
    {
        if (triggerObject && triggeredMaterial)
        {
            triggerObject.GetComponent<Renderer>().material = triggeredMaterial;
            yield return new WaitForSeconds(0.5f);
            if (normalMaterial)
            {
                triggerObject.GetComponent<Renderer>().material = normalMaterial;
            }
        }
    }

    void OnApplicationQuit()
    {
        try
        {
            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Unity shutting down", cancellationToken.Token);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        cancellationToken?.Cancel();
    }
}