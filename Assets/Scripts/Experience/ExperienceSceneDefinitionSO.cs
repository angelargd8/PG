using UnityEngine;

[CreateAssetMenu(
    fileName = "ExperienceScene",
    menuName = "Scriptable Objects/Experience/Scene Definition"
)]
public sealed class ExperienceSceneDefinitionSO : ScriptableObject
{
    [Header("Identification")]
    [SerializeField] private string sceneId;
    [SerializeField] private string displayName;

    [Header("Unity Scene")]
    [SerializeField] private string sceneName;


    public string SceneId => sceneId;

    public string DisplayName => displayName;

    public string SceneName => sceneName;
}