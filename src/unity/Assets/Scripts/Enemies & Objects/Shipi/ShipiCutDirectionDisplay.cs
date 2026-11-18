using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ShipiCutDirectionDisplay :
    MonoBehaviour,
    IExperienceRuntime
{
    [Header("References")]
    [SerializeField]
    private ShipiConveyDirector _conveyDirector;

    [SerializeField]
    private GameObject _displayRoot;

    [SerializeField]
    private Image _directionImage;


    [Header("Sprites")]
    [SerializeField]
    private Sprite _arrowSprite;

    [SerializeField]
    private Sprite _noCutSprite;


    private ShipiFood _displayedFood;


    public void BeginExperience()
    {
        if (!ValidateReferences())
        {
            return;
        }

        _conveyDirector.FoodEnteredCuttingPoint +=
            HandleFoodEnteredCuttingPoint;

        _conveyDirector.FoodLeftCuttingPoint +=
            HandleFoodLeftCuttingPoint;

        Hide();
    }


    public void EndExperience()
    {
        if (_conveyDirector != null)
        {
            _conveyDirector.FoodEnteredCuttingPoint -=
                HandleFoodEnteredCuttingPoint;

            _conveyDirector.FoodLeftCuttingPoint -=
                HandleFoodLeftCuttingPoint;
        }

        Hide();
    }


    private void HandleFoodEnteredCuttingPoint(
        ShipiFood food)
    {
        if (food == null)
        {
            return;
        }

        _displayedFood = food;

        ConfigureDirection(
            food.ExpectedDirection
        );

        _displayRoot.SetActive(
            true
        );
    }


    private void HandleFoodLeftCuttingPoint(
        ShipiFood food)
    {
        if (food != _displayedFood)
        {
            return;
        }

        Hide();
    }


    private void ConfigureDirection(
        ShipiCutDirection direction)
    {
        if (direction ==
            ShipiCutDirection.None)
        {
            _directionImage.sprite =
                _noCutSprite;

            _directionImage.rectTransform.localRotation =
                Quaternion.identity;

            return;
        }

        _directionImage.sprite =
            _arrowSprite;

        float angle =
            direction switch
            {
                ShipiCutDirection.LeftToRight =>
                    -90f,

                ShipiCutDirection.RightToLeft =>
                    90f,

                ShipiCutDirection.TopToBottom =>
                    180f,

                _ => 0f
            };

        _directionImage.rectTransform.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }


    private void Hide()
    {
        _displayedFood = null;

        if (_displayRoot != null)
        {
            _displayRoot.SetActive(
                false
            );
        }
    }


    private bool ValidateReferences()
    {
        if (_conveyDirector == null)
        {
            Debug.LogError(
                "[ShipiCutDirectionDisplay] " +
                "ConveyDirector no está asignado.",
                this
            );

            return false;
        }

        if (_displayRoot == null)
        {
            Debug.LogError(
                "[ShipiCutDirectionDisplay] " +
                "DisplayRoot no está asignado.",
                this
            );

            return false;
        }

        if (_directionImage == null)
        {
            Debug.LogError(
                "[ShipiCutDirectionDisplay] " +
                "DirectionImage no está asignado.",
                this
            );

            return false;
        }

        if (_arrowSprite == null ||
            _noCutSprite == null)
        {
            Debug.LogError(
                "[ShipiCutDirectionDisplay] " +
                "Faltan sprites.",
                this
            );

            return false;
        }

        return true;
    }
}