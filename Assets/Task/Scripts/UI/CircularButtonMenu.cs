using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CircularButtonMenu : MonoBehaviour
{
    [Header("Menu Configuration")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float closedRadius = 100f;
    [SerializeField] private float openRadius = 200f;

    [Header("Button References")]
    [SerializeField] private string[] menuHeading = new string[5];
    [SerializeField] private Button[] menuButtons = new Button[5];
    [SerializeField] private Transform centerPoint;

    [Header("Visual Settings")]
    [SerializeField] private float selectedButtonScale = 1.2f;
    [SerializeField] private float normalButtonScale = 1f;
    [SerializeField] private Sprite[] normalSprites = new Sprite[5];
    [SerializeField] private Sprite[] selectedSprites = new Sprite[5];

    // Core state
    private int selectedButtonIndex = 2;
    private int lastOpenIndex = 2;
    private bool isPanelOpen;
    private bool isAnimating;

    // Cached components and data
    private Transform[] buttonTransforms;
    private Vector3[] initialScales;
    private Image[] buttonImages;
    private readonly int buttonCount = 5;

    // Constants for optimization
    private const float START_ANGLE = 90f;
    private const float LEFT_CENTER_ANGLE = 180f;
    private const float FULL_CIRCLE = 360f;
    private const float HALF_CIRCLE = 180f;

    // Angle calculation cache
    private float openAngleStep;
    private float closedAngleStep;

    void Start()
    {
        InitializeMenu();
        CacheComponents();
        PreCalculateAngles();
        SetupButtonListeners();
        InitializeButtonSprites();
        CalculateInitialPositions(false);
    }

    void InitializeMenu()
    {
        if (centerPoint == null)
            centerPoint = transform;
    }

    void CacheComponents()
    {
        int length = menuButtons.Length;
        buttonTransforms = new Transform[length];
        initialScales = new Vector3[length];
        buttonImages = new Image[length];

        for (int i = 0; i < length; i++)
        {
            if (menuButtons[i] != null)
            {
                buttonTransforms[i] = menuButtons[i].transform;
                initialScales[i] = buttonTransforms[i].localScale;
                buttonImages[i] = menuButtons[i].GetComponent<Image>();
            }
        }
    }

    void PreCalculateAngles()
    {
        openAngleStep = HALF_CIRCLE / (buttonCount - 1);
        closedAngleStep = FULL_CIRCLE / buttonCount;
    }

    void SetupButtonListeners()
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] != null)
            {
                int buttonIndex = i; // Capture for closure
                menuButtons[i].onClick.AddListener(() => OnButtonClick(buttonIndex));
            }
        }
    }

    public void CalculateInitialPositions(bool open)
    {
        isPanelOpen = open;

        if (open)
        {
            selectedButtonIndex = lastOpenIndex;
            UIManager.Instance.gamePlayPanel.heaingText.text = menuHeading[selectedButtonIndex];
            StartCoroutine(AnimateCircularRotation());
        }
        else
        {
            UpdateAllButtonSprites(true);
            StartCoroutine(AnimateToFullCircle());
        }
    }

    public void OnButtonClick(int buttonIndex)
    {
        if (!isPanelOpen || isAnimating ||
            buttonIndex < 0 || buttonIndex >= menuButtons.Length ||
            buttonIndex == selectedButtonIndex)
            return;

        SelectButton(buttonIndex);
    }

    void SelectButton(int newSelectedIndex)
    {
        int previousSelectedIndex = selectedButtonIndex;
        selectedButtonIndex = newSelectedIndex;
        UIManager.Instance.gamePlayPanel.heaingText.text = menuHeading[selectedButtonIndex];
        // Update button visuals immediately
        UpdateButtonVisuals(previousSelectedIndex, selectedButtonIndex);
        StartCoroutine(AnimateCircularRotation());
    }

    IEnumerator AnimateCircularRotation()
    {
        yield return StartCoroutine(AnimateButtons(false));
    }

    IEnumerator AnimateToFullCircle()
    {
        isAnimating = true;
        lastOpenIndex = selectedButtonIndex;
        selectedButtonIndex = 2;

        yield return StartCoroutine(AnimateButtons(true));
    }

    IEnumerator AnimateButtons(bool isFullCircle)
    {
        isAnimating = true;

        // Cache starting states
        var animData = new ButtonAnimationData[menuButtons.Length];
        CacheStartingStates(animData);

        // Calculate targets
        Vector3[] targetPositions = CalculateCircularPositions(isFullCircle);
        CalculateTargetAngles(animData, targetPositions, isFullCircle);

        // Animate
        yield return StartCoroutine(PerformAnimation(animData, targetPositions, isFullCircle));

        // Set final states
        SetFinalStates(targetPositions, isFullCircle);

        isAnimating = false;
    }

    void CacheStartingStates(ButtonAnimationData[] animData)
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (buttonTransforms[i] != null)
            {
                Vector3 localPos = buttonTransforms[i].localPosition;
                animData[i] = new ButtonAnimationData
                {
                    startPosition = localPos,
                    startScale = buttonTransforms[i].localScale,
                    startAngle = Mathf.Atan2(localPos.y, localPos.x) * Mathf.Rad2Deg
                };
            }
        }
    }

       void CalculateTargetAngles(ButtonAnimationData[] animData, Vector3[] targetPositions, bool isFullCircle)
    {
        if (isFullCircle)
        {
            // NEW: For full circle, also determine rotation direction for consistent movement
            CalculateFullCircleTargetAngles(animData, targetPositions);
        }
        else
        {
            // For half circle, determine rotation direction based on selected button
            CalculateHalfCircleTargetAngles(animData, targetPositions);
        }
    }

  void CalculateFullCircleTargetAngles(ButtonAnimationData[] animData, Vector3[] targetPositions)
    {
        // Use the selected button as reference to determine rotation direction
        int referenceButtonIndex = lastOpenIndex;
        
        // Get the selected button's current angle and its target angle in full circle
        float selectedButtonCurrentAngle = animData[referenceButtonIndex].startAngle;
        
        // Calculate the target angle for the selected button in full circle
        // In full circle, button goes to its original index position
        float selectedButtonTargetAngle = START_ANGLE + (referenceButtonIndex * closedAngleStep);
        
        // Normalize current angle to [0, 360] range
        while (selectedButtonCurrentAngle < 0f) selectedButtonCurrentAngle += 360f;
        while (selectedButtonCurrentAngle >= 360f) selectedButtonCurrentAngle -= 360f;
        
        // Calculate the shortest path for selected button to its original position
        float selectedButtonAngleDiff = selectedButtonTargetAngle - selectedButtonCurrentAngle;
        while (selectedButtonAngleDiff > 180f) selectedButtonAngleDiff -= 360f;
        while (selectedButtonAngleDiff < -180f) selectedButtonAngleDiff += 360f;
        
        // Determine rotation direction based on selected button's shortest path
        bool rotateCounterClockwise = selectedButtonAngleDiff > 0;
        
        // Apply consistent rotation direction to all buttons
        for (int i = 0; i < menuButtons.Length; i++)
        {
            Vector3 targetPos = targetPositions[i];
            float targetAngle = Mathf.Atan2(targetPos.y, targetPos.x) * Mathf.Rad2Deg;
            float startAngle = animData[i].startAngle;
            float angleDiff = targetAngle - startAngle;
            
            // Normalize angle difference to [-180, 180] range
            while (angleDiff > 180f) angleDiff -= 360f;
            while (angleDiff < -180f) angleDiff += 360f;
            
            // Force same rotation direction as determined by the selected button
            if (rotateCounterClockwise && angleDiff < 0)
            {
                angleDiff += 360f;
            }
            else if (!rotateCounterClockwise && angleDiff > 0)
            {
                angleDiff -= 360f;
            }
            
            animData[i].targetAngle = startAngle + angleDiff;
        }
        
        Debug.Log($"Full Circle Animation: Selected Button {referenceButtonIndex} from {selectedButtonCurrentAngle:F1}° to {selectedButtonTargetAngle:F1}° - Rotating {(rotateCounterClockwise ? "Counter-Clockwise" : "Clockwise")}");
    }

    void CalculateHalfCircleTargetAngles(ButtonAnimationData[] animData, Vector3[] targetPositions)
    {
        // For half circle, determine rotation direction based on selected button
        float selectedButtonStartAngle = animData[selectedButtonIndex].startAngle;
        Vector3 selectedButtonTargetPos = targetPositions[selectedButtonIndex];
        float selectedButtonTargetAngle = Mathf.Atan2(selectedButtonTargetPos.y, selectedButtonTargetPos.x) * Mathf.Rad2Deg;

        // Calculate shortest path for selected button
        float selectedAngleDiff = selectedButtonTargetAngle - selectedButtonStartAngle;
        while (selectedAngleDiff > 180f) selectedAngleDiff -= 360f;
        while (selectedAngleDiff < -180f) selectedAngleDiff += 360f;

        // Determine rotation direction (positive = counter-clockwise, negative = clockwise)
        bool rotateCounterClockwise = selectedAngleDiff > 0;

        // Apply same rotation direction to all buttons
        for (int i = 0; i < menuButtons.Length; i++)
        {
            Vector3 targetPos = targetPositions[i];
            float targetAngle = Mathf.Atan2(targetPos.y, targetPos.x) * Mathf.Rad2Deg;
            float startAngle = animData[i].startAngle;
            float angleDiff = targetAngle - startAngle;

            // Normalize angle difference to [-180, 180] range
            while (angleDiff > 180f) angleDiff -= 360f;
            while (angleDiff < -180f) angleDiff += 360f;

            // Force same rotation direction as selected button
            if (rotateCounterClockwise && angleDiff < 0)
            {
                // Selected button goes counter-clockwise, force this button counter-clockwise too
                angleDiff += 360f;
            }
            else if (!rotateCounterClockwise && angleDiff > 0)
            {
                // Selected button goes clockwise, force this button clockwise too
                angleDiff -= 360f;
            }

            animData[i].targetAngle = startAngle + angleDiff;
        }

        Debug.Log($"Half Circle Animation: Rotating {(rotateCounterClockwise ? "Counter-Clockwise" : "Clockwise")}");
    }

    IEnumerator PerformAnimation(ButtonAnimationData[] animData, Vector3[] targetPositions, bool isFullCircle)
    {
        float elapsedTime = 0f;
        float radius = isFullCircle ? closedRadius : openRadius;

        while (elapsedTime < animationDuration)
        {
            float t = elapsedTime / animationDuration;
            float curveValue = animationCurve.Evaluate(t);

            UpdateButtonTransforms(animData, curveValue, radius, isFullCircle);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    void UpdateButtonTransforms(ButtonAnimationData[] animData, float curveValue, float radius, bool isFullCircle)
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (buttonTransforms[i] == null) continue;

            // Position
            float currentAngle = Mathf.Lerp(animData[i].startAngle, animData[i].targetAngle, curveValue);
            float radians = currentAngle * Mathf.Deg2Rad;

            Vector3 newPosition = new Vector3(
                Mathf.Cos(radians) * radius,
                Mathf.Sin(radians) * radius,
                0f
            );
            buttonTransforms[i].localPosition = newPosition;

            // Scale
            Vector3 targetScale = GetTargetScale(i, isFullCircle);
            buttonTransforms[i].localScale = Vector3.Lerp(animData[i].startScale, targetScale, curveValue);
        }
    }

    void SetFinalStates(Vector3[] targetPositions, bool isFullCircle)
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (buttonTransforms[i] != null)
            {
                buttonTransforms[i].localPosition = targetPositions[i];
                buttonTransforms[i].localScale = GetTargetScale(i, isFullCircle);
            }
        }

        // Update all button sprites based on circle state
        UpdateAllButtonSprites(isFullCircle);
    }

    Vector3 GetTargetScale(int index, bool isFullCircle)
    {
        if (isFullCircle)
            return initialScales[index] * normalButtonScale;

        return (index == selectedButtonIndex) ?
            initialScales[index] * selectedButtonScale :
            initialScales[index] * normalButtonScale;
    }

    Vector3[] CalculateCircularPositions(bool isFullCircle)
    {
        Vector3[] positions = new Vector3[menuButtons.Length];
        float radius = isFullCircle ? closedRadius : openRadius;
        float angleStep = isFullCircle ? closedAngleStep : openAngleStep;

        if (isFullCircle)
        {
            // Simple full circle positioning
            for (int i = 0; i < menuButtons.Length; i++)
            {
                float angle = START_ANGLE + (i * angleStep);
                positions[i] = GetPositionFromAngle(angle, radius);
            }
        }
        else
        {
            // Complex left-side arc with rotation
            int selectedTargetSlot = (menuButtons.Length - 1) / 2;
            int rotationSteps = selectedButtonIndex - selectedTargetSlot;

            for (int i = 0; i < menuButtons.Length; i++)
            {
                int newSlotIndex = (i - rotationSteps + menuButtons.Length) % menuButtons.Length;
                if (newSlotIndex >= menuButtons.Length)
                    newSlotIndex -= menuButtons.Length;

                float angle = START_ANGLE + (newSlotIndex * angleStep);
                positions[i] = GetPositionFromAngle(angle, radius);
            }
        }

        return positions;
    }

    Vector3 GetPositionFromAngle(float angleDegrees, float radius)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        return new Vector3(
            Mathf.Cos(radians) * radius,
            Mathf.Sin(radians) * radius,
            0f
        );
    }

    // Public API
    public void SetSelectedButton(int index)
    {
        if (index >= 0 && index < menuButtons.Length && !isAnimating)
        {
            SelectButton(index);
        }
    }
    public int GetSelectedButtonIndex() => selectedButtonIndex;
    public bool IsAnimating() => isAnimating;

    // Visual Update Methods
    private void UpdateButtonVisuals(int previousIndex, int newIndex)
    {
        // Update previous button to normal
        if (previousIndex >= 0 && previousIndex < menuButtons.Length)
        {
            SetButtonSprite(previousIndex, false);
        }

        // Update new button to selected
        if (newIndex >= 0 && newIndex < menuButtons.Length)
        {
            SetButtonSprite(newIndex, true);
        }
    }

    private void UpdateAllButtonSprites(bool isFullCircle = false)
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (isFullCircle)
            {
                // In full circle mode, all buttons use normal sprites
                SetButtonSprite(i, false);
            }
            else
            {
                // In half circle mode, only selected button uses selected sprite
                SetButtonSprite(i, i == selectedButtonIndex);
            }
        }
    }

    private void SetButtonSprite(int index, bool isSelected)
    {
        if (index < 0 || index >= buttonImages.Length || buttonImages[index] == null)
            return;

        Sprite[] targetSpriteArray = isSelected ? selectedSprites : normalSprites;

        if (index < targetSpriteArray.Length && targetSpriteArray[index] != null)
        {
            buttonImages[index].sprite = targetSpriteArray[index];
        }
    }

    // Initialize button sprites
    private void InitializeButtonSprites()
    {
        // Set initial state based on panel state
        UpdateAllButtonSprites(!isPanelOpen);
    }

    // Helper struct for animation data
    private struct ButtonAnimationData
    {
        public Vector3 startPosition;
        public Vector3 startScale;
        public float startAngle;
        public float targetAngle;
    }
}