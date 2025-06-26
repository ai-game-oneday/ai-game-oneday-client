using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public static class WebGLInputHelper
{
    /// <summary>
    /// WebGL에서 안정적으로 InputField를 비활성화하는 헬퍼 메서드
    /// </summary>
    public static void DeactivateInputField(TMP_InputField inputField)
    {
        if (inputField == null) return;

        try
        {
            // 1. EventSystem을 통한 선택 해제
            if (EventSystem.current != null && 
                EventSystem.current.currentSelectedGameObject == inputField.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            // 2. 직접 비활성화
            inputField.DeactivateInputField();

            // 3. WebGL에서 추가 처리
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                // MonoBehaviour가 필요한 코루틴은 외부에서 실행해야 함
                inputField.StartCoroutine(ForceDeactivateCoroutine(inputField));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"InputField 비활성화 중 오류: {e.Message}");
        }
    }

    private static IEnumerator ForceDeactivateCoroutine(TMP_InputField inputField)
    {
        yield return null; // 한 프레임 대기
        
        if (inputField != null && inputField.isFocused)
        {
            // 강제로 포커스 해제
            EventSystem.current?.SetSelectedGameObject(null);
        }
    }
}