namespace EsnyaFactory {
  using System;
  using System.Linq;
  using System.Collections.Generic;
  using System.Text.RegularExpressions;
  using UnityEngine;
  using UnityEngine.Animations;


  [CreateAssetMenu(fileName = "FBXAnimationConverter", menuName = "EsnyaTools/FBXAnimationConverter", order = 0)]
  public class FBXAnimationConverter : ScriptableObject {
    [Serializable]
    public class ConverterProfile {
      public string clipName = "$&_Converted";
      public string clipFilter = ".*";
      public string pathFilter = ".*";
      public string propertyFilter = ".*";

      public List<AnimationClip> GenerateClips(IEnumerable<AnimationClip> clips) {
        var clipRegex = new Regex(clipFilter, RegexOptions.IgnoreCase);
        var pathRegex = new Regex(pathFilter, RegexOptions.IgnoreCase);
        var propertyRegex = new Regex(propertyFilter, RegexOptions.IgnoreCase);
        return clips.Where(clip => clipRegex.Match(clip.name) != null).Select(clip => {
          var newClip = AnimationClip.Instantiate(clip);
          newClip.name = clipRegex.Replace(clip.name, clipName);
          return newClip;
        }).ToList();
      }
    }

    public GameObject source;
    public List<ConverterProfile> converterProfiles = new List<ConverterProfile>();

  }
}
