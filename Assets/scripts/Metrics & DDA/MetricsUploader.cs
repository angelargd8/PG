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
    private VoidEventChannelSO
        _songFinished;


    [Header("Upload")]
    [SerializeField]
    private string _uploadUrl;

    [SerializeField]
    private string _uploadToken;

    [Min(1)]
    [SerializeField]
    private int _timeoutSeconds = 10;


    [Header("Optional UI")]
    [SerializeField]
    private TMP_Text _statusText;


    private bool _isUploading;
    private bool _currentSessionHandled;
    private bool _currentSessionUploadFinished;


    public bool CurrentSessionUploadFinished =>
        _currentSessionHandled &&
        _currentSessionUploadFinished;


    private void OnEnable()
    {
        if (_songFinished != null)
        {
            _songFinished.Raised +=
                HandleSongFinished;
        }
    }


    private void OnDisable()
    {
        if (_songFinished != null)
        {
            _songFinished.Raised -=
                HandleSongFinished;
        }
    }


    private void Start()
    {
        StartCoroutine(
            RetryPendingLogs()
        );
    }


    private void HandleSongFinished()
    {
        if (_currentSessionHandled)
        {
            return;
        }

        _currentSessionHandled = true;


        if (_metricsLogger == null ||
            _metricsSystem == null)
        {
            _currentSessionUploadFinished = true;

            return;
        }


        if (!_metricsLogger.IsLogging)
        {
            _currentSessionUploadFinished = true;

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
        while (_isUploading)
        {
            yield return null;
        }

        string filePath =
            _metricsLogger.FilePath;

        if (string.IsNullOrEmpty(filePath) ||
            !File.Exists(filePath))
        {
            _currentSessionUploadFinished = true;

            yield break;
        }

        bool success = false;

        yield return UploadFile(
            filePath,
            result => success = result
        );

        _currentSessionUploadFinished = true;

        if (success)
        {
            SetStatus(
                "✓ Métricas enviadas"
            );
        }
        else
        {
            SetStatus(
                "⚠ Guardadas localmente. " +
                "Pendientes de envío."
            );
        }
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


    private IEnumerator UploadFile(string filePath, Action<bool> completed = null)
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

            completed?.Invoke(false);

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

            completed?.Invoke(false);

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

        request.timeout = _timeoutSeconds;


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
        }


        _isUploading = false;

        completed?.Invoke(success);
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