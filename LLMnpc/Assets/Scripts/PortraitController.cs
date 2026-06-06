using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LLMNpc
{
    // 13.12 출력 계층 - 표정 표현
    // 2D 실습에서는 LLM 의 emotion 값에 따라 캐릭터 스프라이트를 교체한다.
    //
    // [에셋] Sutemo 무료 VN 스프라이트(PSD)에서 표정 5종을 PNG로 export 한 뒤
    //        아래 슬롯(neutral/happy/sad/angry/shy)에 인스펙터에서 할당한다.
    //        https://sutemo.itch.io/female-character  (라이선스: 상업 OK, 크레딧 권장)
    public class PortraitController : MonoBehaviour
    {
        [Header("캐릭터 일러스트 Image")]
        [SerializeField] Image portrait;

        [Header("표정 스프라이트 (Sutemo 등에서 export)")]
        [SerializeField] Sprite neutral;
        [SerializeField] Sprite happy;
        [SerializeField] Sprite sad;
        [SerializeField] Sprite angry;
        [SerializeField] Sprite shy;

        Dictionary<string, Sprite> map;

        void Awake()
        {
            map = new Dictionary<string, Sprite>
            {
                { "neutral", neutral }, { "happy", happy },
                { "sad", sad }, { "angry", angry }, { "shy", shy }
            };
        }

        public void SetEmotion(string emotion)
        {
            if (portrait == null) return;
            if (!string.IsNullOrEmpty(emotion) && map.TryGetValue(emotion, out var s) && s != null)
                portrait.sprite = s;
            else if (neutral != null)
                portrait.sprite = neutral; // 알 수 없는 값이면 기본 표정
        }
    }
}
