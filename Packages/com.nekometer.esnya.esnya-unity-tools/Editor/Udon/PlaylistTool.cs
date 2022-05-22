using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace EsnyaFactory
{
    public class PlaylistTool : EditorWindow
    {
        public enum TargetMode
        {
            IwaSync3,
            // USharpVideo,
        }

        [Serializable]
        public class PlaylistItemSnippetThumbanil
        {
            public string url;
            public int width, height;
        }

        [Serializable]
        public class PlaylistItemSnippetThumbanils
        {
            public PlaylistItemSnippetThumbanil default_, medium, high, standard, maxres;
        }

        [Serializable]
        public class PlaylistItemSnippetResourceId
        {
            public string kind, videoId;
        }


        [Serializable]
        public class PlaylistItemSnippet
        {
            public string publishedAt;
            public string channelId;
            public string title;
            public string description;
            public PlaylistItemSnippetThumbanils thumbanils;
            public string channelTitle;
            public string playlistId;
            public int position;
            public PlaylistItemSnippetResourceId resourceId;
            public string videoWonerChannelTitle;
            public string videoOwnerChannelId;
        }


        [Serializable]
        public class PlaylistItem
        {
            public string kind, etag, id;
            public PlaylistItemSnippet snippet;
        }

        [Serializable]
        public class PlaylistItemsResultPageInfo
        {
            public int totalReslts, resultsPerPage;
        }


        [Serializable]
        public class PlaylistItemsResult
        {
            public string kind, etag;
            public PlaylistItem[] items;
            public PlaylistItemsResultPageInfo pageInfo;
        }

        [MenuItem("EsnyaTools/Playlist Tool")]
        private static void ShowWindow()
        {
            var window = GetWindow<PlaylistTool>();
            window.Show();
        }

        public string playlistId = "";
        public TargetMode targetMode;
        public MonoBehaviour target;


        private void OnEnable()
        {
            titleContent = new GUIContent("Playlist Tool");
        }

        private void OnGUI()
        {
            var settings = EsnyaUdonToolsSettings.Instance;
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                settings.youtubeApiKey = EditorGUILayout.PasswordField("Api Key", settings.youtubeApiKey);
                if (scope.changed) settings.Save();
            }

            playlistId = EditorGUILayout.TextField("Playlist URL", playlistId);

            targetMode = (TargetMode)EditorGUILayout.EnumPopup("Target Mode", targetMode);
            target = EditorGUILayout.ObjectField("Playlist Object", target, typeof(MonoBehaviour), true) as MonoBehaviour;

            var isValidTarget = targetMode == TargetMode.IwaSync3 ? target.GetType().Name == "Playlist" : false;
            using (new EditorGUI.DisabledGroupScope(!isValidTarget))
            {
                if (GUILayout.Button("Generate Playlist"))
                {
                    GeneratePlaylist(settings.youtubeApiKey);
                }
            }
        }

        async private void GeneratePlaylist(string youtubeApiKey)
        {
            var items = await GetPlaylistItems(youtubeApiKey);
            if (items == null) return;

            if (targetMode == TargetMode.IwaSync3)
            {
                var serialized = new SerializedObject(target);
                var tracks = serialized.FindProperty("tracks");
                tracks.arraySize = items.Length;

                for (var i = 0; i < items.Length; i++)
                {
                    var track = tracks.GetArrayElementAtIndex(i);
                    var item = items[i];

                    track.FindPropertyRelative("title").stringValue = item.snippet.title;
                    track.FindPropertyRelative("url").stringValue = $"https://www.youtube.com/watch?v={item.snippet.resourceId.videoId}";
                }

                serialized.ApplyModifiedProperties();
            }
        }

        async private Task<PlaylistItem[]> GetPlaylistItems(string youtubeApiKey)
        {
            using (var client = new HttpClient())
            {
                if (playlistId.StartsWith("https://"))
                {
                    playlistId = new Regex("list=([^&? ]+)").Match(playlistId).Groups[1].Value;
                }
                var res = await client.GetAsync($"https://www.googleapis.com/youtube/v3/playlistItems?part=snippet&playlistId={playlistId}&maxResults=50&key={youtubeApiKey}");

                if (!res.IsSuccessStatusCode)
                {
                    EditorUtility.DisplayDialog(res.StatusCode.ToString(), await res.Content.ReadAsStringAsync(), "Close");
                    return null;
                }

                return JsonUtility.FromJson<PlaylistItemsResult>(await res.Content.ReadAsStringAsync()).items;
            }
        }
    }
}
