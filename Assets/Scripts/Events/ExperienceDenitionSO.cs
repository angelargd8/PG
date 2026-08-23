using UnityEngine;

[CreateAssetMenu(
    fileName = "ExperienceDefinition",
    menuName = "Scriptable Objects/Experience/Experience Definition"
)]
public sealed class ExperienceDefinitionSO : ScriptableObject
{
    [Header("Identification")]
    [SerializeField] private string experienceId;

    [SerializeField] private string displayName;


    [Header("Scenes")]
    [SerializeField]
    private ExperienceSceneDefinitionSO[] scenes;


    public string ExperienceId =>
        experienceId;

    public string DisplayName =>
        displayName;

    public int SceneCount =>
        scenes != null
            ? scenes.Length
            : 0;


    public ExperienceSceneDefinitionSO GetScene(
        int index
    )
    {
        if (scenes == null ||
            index < 0 ||
            index >= scenes.Length)
        {
            return null;
        }

        return scenes[index];
    }


    public int GetSceneIndex(
        ExperienceSceneDefinitionSO scene
    )
    {
        if (scene == null ||
            scenes == null)
        {
            return -1;
        }


        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i] == scene)
            {
                return i;
            }
        }


        return -1;
    }
}