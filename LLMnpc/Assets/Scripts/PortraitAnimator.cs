using System.Collections;
using UnityEngine;

namespace LLMNpc
{
    // 캐릭터 연출: 등장(페이드인 + 아래에서 떠오름) + 은은한 idle 호흡 + 대사 시 살짝 팝.
    // 코드 기반 트윈(Animator 불필요).
    [RequireComponent(typeof(RectTransform))]
    public class PortraitAnimator : MonoBehaviour
    {
        [SerializeField] CanvasGroup canvasGroup;
        [Header("등장")]
        [SerializeField] float entranceTime = 0.5f;
        [SerializeField] float riseDistance = 60f;   // 아래에서 이만큼 떠오름
        [Header("idle 호흡")]
        [SerializeField] float idleAmplitude = 6f;
        [SerializeField] float idleSpeed = 1.6f;

        RectTransform rt;
        Vector2 basePos;
        float idlePhase;
        bool entered;

        void Awake()
        {
            rt = GetComponent<RectTransform>();
            basePos = rt.anchoredPosition;
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        void OnEnable()
        {
            StopAllCoroutines();
            StartCoroutine(Entrance());
        }

        IEnumerator Entrance()
        {
            entered = false;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            float t = 0f;
            while (t < entranceTime)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / entranceTime);
                if (canvasGroup != null) canvasGroup.alpha = k;
                rt.anchoredPosition = basePos + new Vector2(0f, (k - 1f) * riseDistance);
                yield return null;
            }
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            rt.anchoredPosition = basePos;
            entered = true;
        }

        void Update()
        {
            if (!entered) return;
            idlePhase += Time.deltaTime * idleSpeed;
            rt.anchoredPosition = basePos + new Vector2(0f, Mathf.Sin(idlePhase) * idleAmplitude);
        }

        // 대사/감정 변화 시 살짝 팝 (스케일 1 → 1.05 → 1)
        public void React()
        {
            if (isActiveAndEnabled) StartCoroutine(Pop());
        }

        IEnumerator Pop()
        {
            float t = 0f, dur = 0.22f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float s = 1f + Mathf.Sin((t / dur) * Mathf.PI) * 0.05f;
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }
    }
}
