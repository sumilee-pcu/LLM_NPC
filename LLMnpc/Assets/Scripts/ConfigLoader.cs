using System.IO;
using UnityEngine;

namespace LLMNpc
{
    // 13.3 설정 로드
    // StreamingAssets/config.json 을 읽는다. (실제 API 키가 들어가므로 git 에는 올리지 않는다)
    // 학생은 config.cloud.example.json(또는 config.local.example.json)을
    // config.json 으로 복사한 뒤 본인 값으로 수정한다.
    public static class ConfigLoader
    {
        public static LLMConfig Load()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "config.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<LLMConfig>(json);
            }

            Debug.LogWarning(
                "[ConfigLoader] config.json 이 없습니다: " + path + "\n" +
                "Assets/StreamingAssets/config.cloud.example.json 을 복사해\n" +
                "config.json 으로 만들고 API 키/모델을 입력하세요. (기본값으로 실행합니다)");
            return new LLMConfig();
        }
    }
}
