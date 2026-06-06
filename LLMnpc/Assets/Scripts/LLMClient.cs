using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace LLMNpc
{
    // 13.4 생성 계층
    // OpenAI 호환 엔드포인트로 POST 한다.
    // 클라우드(OpenAI 등)와 로컬(LM Studio/Ollama)은 같은 코드 / config 의 base_url 만 다르다.
    public class LLMClient : MonoBehaviour
    {
        public IEnumerator Send(LLMConfig cfg, ChatMessage[] messages,
                                Action<string> onSuccess, Action<string> onError)
        {
            var requestObj = new ChatRequest
            {
                model = cfg.model,
                messages = messages,
                max_tokens = cfg.max_tokens,
                temperature = cfg.temperature
            };

            string json = JsonUtility.ToJson(requestObj);
            byte[] body = Encoding.UTF8.GetBytes(json);

            using (var req = new UnityWebRequest(cfg.base_url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                // 클라우드는 API 키 필요 / 로컬(LM Studio·Ollama)은 보통 비워둠
                if (!string.IsNullOrEmpty(cfg.api_key))
                    req.SetRequestHeader("Authorization", "Bearer " + cfg.api_key);

                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"HTTP {req.responseCode} / {req.error}\n{req.downloadHandler.text}");
                    yield break;
                }

                onSuccess?.Invoke(req.downloadHandler.text);
            }
        }
    }
}
