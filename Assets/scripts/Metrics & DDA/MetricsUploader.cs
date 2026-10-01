using System;
using System.Collections;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

[DisallowMultipleComponent]
public sealed class MetricsUploader :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private MetricsLogger _metricsLogger;

    [SerializeField]
    private MetricsSystem _metricsSystem;


    [Header("Events")]
    [SerializeField]
    private RunResultEventChannelSO
        _runResultReady;


    [Header("Upload")]
    [SerializeField]
    private string _uploadUrl;

    [SerializeField]
    private string _uploadToken;


    [Header("Optional UI")]
    [SerializeField]
    private TMP_Text _statusText;


    private bool _isUploading;


    private void OnEnable()
    {
        if (_runResultReady != null)
        {
            _runResultReady.Raised +=
                HandleRunResultReady;
        }
    }


    private void OnDisable()
    {
        if (_runResultReady != null)
        {
            _runResultReady.Raised -=
                HandleRunResultReady;
        }
    }


    private void Start()
    {
        StartCoroutine(
            RetryPendingLogs()
        );
    }


    private void HandleRunResultReady(
        RunResult result)
    {
        if (_metricsLogger == null ||
            _metricsSystem == null)
        {
            Debug.LogError(
                "[MetricsUploader] " +
                "Faltan referencias.",
                this
            );

            return;
        }

        _metricsLogger.FinalizeLog(
            _metricsSystem
        );

        SetStatus(
            "Métricas guardadas. Enviando..."
        );

        StartCoroutine(
            UploadCurrentLog()
        );
    }


    private IEnumerator UploadCurrentLog()
    {
        if (_isUploading)
        {
            yield break;
        }

        string filePath =
            _metricsLogger.FilePath;

        if (string.IsNullOrEmpty(filePath) ||
            !File.Exists(filePath))
        {
            yield break;
        }

        yield return UploadFile(
            filePath
        );
    }


    private IEnumerator RetryPendingLogs()
    {
        // Dejamos que termine Awake/Start
        // del resto de ExperienceCore.
        yield return null;

        string[] files =
            Directory.GetFiles(
                Application.persistentDataPath,
                "Metrics_*.txt"
            );

        foreach (string file in files)
        {
            // No subir el archivo actual
            // mientras todavía está en uso.
            if (_metricsLogger != null &&
                file ==
                _metricsLogger.FilePath)
            {
                continue;
            }

            if (HasBeenUploaded(file))
            {
                continue;
            }

            string content;

            try
            {
                content =
                    File.ReadAllText(file);
            }
            catch
            {
                continue;
            }

            // Solo reintentamos sesiones
            // que sabemos que terminaron.
            if (!content.Contains(
                "===== END OF SESSION ====="))
            {
                continue;
            }

            yield return UploadFile(
                file
            );
        }
    }


    private IEnumerator UploadFile(
        string filePath)
    {
        if (_isUploading)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(
            _uploadUrl))
        {
            Debug.LogError(
                "[MetricsUploader] " +
                "Upload URL no configurada.",
                this
            );

            yield break;
        }

        _isUploading = true;

        string content;

        try
        {
            content =
                File.ReadAllText(filePath);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[MetricsUploader] " +
                $"No se pudo leer el log: " +
                $"{exception.Message}",
                this
            );

            _isUploading = false;

            yield break;
        }


        MetricsUploadPayload payload =
            new MetricsUploadPayload
            {
                token =
                    _uploadToken,

                participantId =
                    ExtractParticipantId(
                        filePath
                    ),

                fileName =
                    Path.GetFileName(
                        filePath
                    ),

                appVersion =
                    Application.version,

                platform =
                    Application.platform.ToString(),

                content =
                    content
            };


        string json =
            JsonUtility.ToJson(
                payload
            );

        byte[] body =
            Encoding.UTF8.GetBytes(
                json
            );


        using UnityWebRequest request =
            new UnityWebRequest(
                _uploadUrl,
                UnityWebRequest.kHttpVerbPOST
            );

        request.uploadHandler =
            new UploadHandlerRaw(
                body
            );

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );


        yield return
            request.SendWebRequest();


        bool success =
            request.result ==
            UnityWebRequest.Result.Success;


        if (success)
        {
            MarkAsUploaded(
                filePath
            );

            Debug.Log(
                $"[MetricsUploader] " +
                $"Subido correctamente: " +
                $"{Path.GetFileName(filePath)}",
                this
            );

            SetStatus(
                "✓ Métricas enviadas"
            );
        }
        else
        {
            Debug.LogWarning(
                $"[MetricsUploader] " +
                $"No se pudo subir. " +
                $"Quedará pendiente. " +
                $"Error: {request.error}",
                this
            );

            SetStatus(
                "⚠ Guardadas localmente. " +
                "Pendientes de envío."
            );
        }


        _isUploading = false;
    }


    private bool HasBeenUploaded(
        string filePath)
    {
        return File.Exists(
            GetUploadedMarkerPath(
                filePath
            )
        );
    }


    private void MarkAsUploaded(
        string filePath)
    {
        File.WriteAllText(
            GetUploadedMarkerPath(
                filePath
            ),
            DateTime.Now.ToString("O")
        );
    }


    private string GetUploadedMarkerPath(
        string filePath)
    {
        return filePath +
            ".uploaded";
    }


    private string ExtractParticipantId(
        string filePath)
    {
        string fileName =
            Path.GetFileNameWithoutExtension(
                filePath
            );

        string[] pieces =
            fileName.Split('_');

        if (pieces.Length >= 2)
        {
            return pieces[1];
        }

        return
            ParticipantSession.ParticipantId;
    }


    private void SetStatus(
        string text)
    {
        if (_statusText != null)
        {
            _statusText.text =
                text;
        }
    }


    [Serializable]
    private sealed class
        MetricsUploadPayload
    {
        public string token;
        public string participantId;
        public string fileName;
        public string appVersion;
        public string platform;
        public string content;
    }
}