using System.IO;
using UnityEngine;

namespace LLMNpc
{
    // 13.7 메모리/상태 영속화
    // 채팅기록 + 호감도를 세이브 파일(JSON)로 저장/복원한다.
    // 저장 위치: Application.persistentDataPath (플랫폼별 사용자 데이터 폴더)
    public static class SaveSystem
    {
        static string PathFor(int slot)
            => Path.Combine(Application.persistentDataPath, $"save_slot{slot}.json");

        public static void Save(GameState state, int slot = 0)
        {
            string json = JsonUtility.ToJson(state.ToSave(), true);
            File.WriteAllText(PathFor(slot), json);
            Debug.Log($"[Save] {PathFor(slot)}");
        }

        public static GameState Load(int slot = 0)
        {
            string path = PathFor(slot);
            if (!File.Exists(path)) return null;
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            return GameState.FromSave(data);
        }
    }
}
