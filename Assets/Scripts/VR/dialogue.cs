using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SimpleDialogue : MonoBehaviour
{
    public GameObject bubbleRoot;   // Le Panel (bulle)
    public TMP_Text bubbleText;     // Le texte TMP dans la bulle

    [TextArea(2, 4)]
    public List<string> lines = new List<string>();

    public KeyCode nextKey = KeyCode.E;

    int index = -1;
    bool isOpen = false;

    void Start() => Close();

    void Update()
    {
        if (!Input.GetKeyDown(nextKey)) return;

        if (!isOpen)
        {
            Open();
            NextLine();
        }
        else
        {
            NextLine();
        }
    }

    void Open()
    {
        isOpen = true;
        bubbleRoot.SetActive(true);
    }

    void Close()
    {
        isOpen = false;
        bubbleRoot.SetActive(false);
        bubbleText.text = "";
        index = -1;
    }

    void NextLine()
    {
        if (lines.Count == 0) { Close(); return; }

        index++;
        if (index >= lines.Count) { Close(); return; }

        bubbleText.text = lines[index];
    }
}
