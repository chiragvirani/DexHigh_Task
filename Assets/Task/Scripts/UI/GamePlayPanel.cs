using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayPanel : MonoBehaviour
{
    public TextMeshProUGUI heaingText;
    public RectTransform targetButton; // Assign in Inspector
    public Vector2 openPosition = new Vector2(100, 100); // Target position when open
    public Vector2 openSize = new Vector2(200, 200);     // Target size when open
    public Vector2 closedPosition = new Vector2(0, 0);   // Target position when closed
    public Vector2 closedSize = new Vector2(100, 100);   // Target size when closed
    public float duration = 0.5f;

    private bool isPanelOpen = false;

    public CanvasGroup optionPanel;

    bool isStartAnimation = false;


    public CircularButtonMenu circularButtonMenu;

    void Start()
    {
        // Set main button to closed position and size
        if (targetButton != null)
        {
            targetButton.anchoredPosition = closedPosition;
            targetButton.sizeDelta = closedSize;
        }
        // Set option panel to invisible
        if (optionPanel != null)
        {
            optionPanel.alpha = 0f;
        }
        isPanelOpen = false;
        isStartAnimation = false;
    }

    public void OnButtonClick()
    {
        if (!isPanelOpen)
        {
            PanelOpen();
        }
        else
        {
            PanelClose();
        }
    }

    void PanelOpen()
    {
        if (isStartAnimation) return;
        isStartAnimation = true;
        circularButtonMenu.CalculateInitialPositions(true);
        // Open animation
        StartCoroutine(AnimationManager.Instance.MoveAnchoredPosition(
            targetButton, targetButton.anchoredPosition, openPosition, duration));
        StartCoroutine(AnimationManager.Instance.ChangeSize(
            targetButton, targetButton.sizeDelta, openSize, duration, () =>
            {
                Debug.Log("Open animation complete");
                isStartAnimation = false;
            }));
        StartCoroutine(AnimationManager.Instance.FadeCanvasGroup(optionPanel, 0, 1, duration));
        isPanelOpen = true;
    }

    void PanelClose()
    {
        if (isStartAnimation) return;
        isStartAnimation = true;
        circularButtonMenu.CalculateInitialPositions(false);
        // Close animation
        StartCoroutine(AnimationManager.Instance.MoveAnchoredPosition(
            targetButton, targetButton.anchoredPosition, closedPosition, duration));
        StartCoroutine(AnimationManager.Instance.ChangeSize(
            targetButton, targetButton.sizeDelta, closedSize, duration, () =>
            {
                Debug.Log("Close animation complete");
                isStartAnimation = false;
            }));
        StartCoroutine(AnimationManager.Instance.FadeCanvasGroup(optionPanel, 1, 0, duration));
        isPanelOpen = false;
    }
}

