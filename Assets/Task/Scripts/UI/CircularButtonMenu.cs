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

    // Core state
    private int selectedButtonIndex = 2;
    private int lastOpenIndex = 2;
    private bool isPanelOpen;
    private bool isAnimating;

    // Cached components and data
    private Transform[] buttonTransforms;
    private Vector3[] initialScales;
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

        for (int i = 0; i < length; i++)
        {
            if (menuButtons[i] != null)
            {
                buttonTransforms[i] = menuButtons[i].transform;
                initialScales[i] = buttonTransforms[i].localScale;
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
        selectedButtonIndex = newSelectedIndex;
        UIManager.Instance.gamePlayPanel.heaingText.text = menuHeading[selectedButtonIndex];
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
        for (int i = 0; i < menuButtons.Length; i++)
        {
            Vector3 targetPos = targetPositions[i];
            float targetAngle = Mathf.Atan2(targetPos.y, targetPos.x) * Mathf.Rad2Deg;

            if (!isFullCircle)
            {
                // Force clockwise rotation for open state
                float angleDiff = targetAngle - animData[i].startAngle;

                // Normalize to [-180, 180]
                while (angleDiff > 180f) angleDiff -= 360f;
                while (angleDiff < -180f) angleDiff += 360f;

                // Force clockwise if would go counter-clockwise
                if (angleDiff > 0)
                    targetAngle = animData[i].startAngle + (angleDiff - 360f);
                else
                    targetAngle = animData[i].startAngle + angleDiff;
            }

            animData[i].targetAngle = targetAngle;
        }
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

    // Helper struct for animation data
    private struct ButtonAnimationData
    {
        public Vector3 startPosition;
        public Vector3 startScale;
        public float startAngle;
        public float targetAngle;
    }
}