using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_ChaptItem : MonoBehaviour
{
    [SerializeField] private Image _selectedImg;
    [SerializeField] private TMP_Text _labelText;
    public int StageId { get; private set; }

    private void Awake()
    {
        SetSelected(false);
    }

    public void SetStageId(int stageId)
    {
        StageId = stageId;
    }

    public void SetSelected(bool selected)
    {
        if (_selectedImg != null)
        {
            _selectedImg.gameObject.SetActive(selected);
        }
    }

    public void SetLabel(string text)
    {
        if (_labelText != null)
        {
            _labelText.text = text;
        }
    }
}
