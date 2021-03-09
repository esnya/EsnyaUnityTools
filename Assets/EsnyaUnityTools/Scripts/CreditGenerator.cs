using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace EsnyaFactory {
  public class CreditGenerator : MonoBehaviour {
    public TMP_Text targetText;
    public string fileNames = "LICENSE OFL";
    public string prefix = "Credits:";
    public string suffix = "";
    public string format = "  %name% [%license%]: %copyright%";
    [HideInInspector] public List<string> files;
  }
}
