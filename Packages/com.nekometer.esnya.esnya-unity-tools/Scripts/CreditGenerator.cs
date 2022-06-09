using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace EsnyaFactory {
  public class CreditGenerator : MonoBehaviour {
    public TMP_Text targetText;
    public string fileNames = "LICENSE OFL";
    [TextArea] public string prefix = "Credits:";
    [TextArea] public string suffix = "";
    [TextArea] public string format = "  %name% [%license%]: %copyright%";
    [HideInInspector] public List<string> files;
  }
}
