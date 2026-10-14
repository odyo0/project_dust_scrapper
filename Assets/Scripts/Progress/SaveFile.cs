using System;
using System.IO;
using UnityEngine;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 저장 파일 한 개를 읽고 쓰기만 한다(무엇을 언제 저장할지는 Profile이 정한다).
    /// 쓰는 도중에 꺼져도 옛 파일이 깨지지 않게, 옆 파일에 다 쓴 뒤 바꿔 끼운다.
    /// </summary>
    public static class SaveFile
    {
        const string FileName = "profile.json";

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>저장 파일을 읽는다. 없거나 읽을 수 없으면 null.</summary>
        public static ProfileData Read()
        {
            string path = FilePath;
            if (!File.Exists(path)) return null;
            try
            {
                return JsonUtility.FromJson<ProfileData>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogError($"저장 파일을 읽지 못했다({path}): {e.Message}");
                return null;
            }
        }

        public static void Write(ProfileData data)
        {
            string path = FilePath;
            string temp = path + ".tmp";
            try
            {
                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            catch (Exception e)
            {
                Debug.LogError($"저장하지 못했다({path}): {e.Message}");
            }
        }
    }
}
