using System.Text;
using UnityEngine;

public static class ParticipantSession
{
    private static string _participantId =
        string.Empty;


    public static string ParticipantId =>
        _participantId;

    public static bool HasParticipantId =>
        !string.IsNullOrWhiteSpace(
            _participantId
        );


    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void ResetSession()
    {
        _participantId =
            string.Empty;
    }


    public static void SetParticipantId(
        string participantId)
    {
        _participantId =
            Sanitize(
                participantId
            );
    }


    public static string Sanitize(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        StringBuilder builder =
            new StringBuilder();

        foreach (char character in
            value.Trim())
        {
            if (char.IsLetterOrDigit(character) ||
                character == '-' ||
                character == '_')
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}