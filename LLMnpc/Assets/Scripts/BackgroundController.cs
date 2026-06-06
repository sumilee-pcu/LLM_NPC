using UnityEngine;
using UnityEngine.UI;

namespace LLMNpc
{
    // 13.4 게임 상태(장소)에 따라 배경을 자동 전환.
    // GameState.Location 키워드에 맞는 배경 스프라이트로 교체한다.
    public class BackgroundController : MonoBehaviour
    {
        [SerializeField] Image background;
        [Header("배경 스프라이트")]
        [SerializeField] Sprite campus;
        [SerializeField] Sprite classroom;
        [SerializeField] Sprite park;
        [SerializeField] Sprite sunset;
        [SerializeField] Sprite hallway;
        [SerializeField] Sprite cafe;

        public void SetByLocation(string location)
        {
            if (background == null || string.IsNullOrEmpty(location)) return;
            Sprite s = campus; // 기본
            if (location.Contains("교실")) s = classroom;
            else if (location.Contains("공원")) s = park;
            else if (location.Contains("노을") || location.Contains("방과후")) s = sunset;
            else if (location.Contains("복도")) s = hallway;
            else if (location.Contains("카페")) s = cafe;
            if (s != null) { background.sprite = s; background.type = Image.Type.Simple; }
        }
    }
}
