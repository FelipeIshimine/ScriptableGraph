using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ishimine.ScriptableGraph;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Subtegral.DialogueSystem.Runtime
{
    public class DialogueParser : MonoBehaviour
    {
        [SerializeField] private GraphContainer dialogue;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Button choicePrefab;
        [SerializeField] private Transform buttonContainer;
        

        private void Start()
        {
            var narrativeData = dialogue.nodeLinks.First(); //Entrypoint node
            ProceedToNarrative(narrativeData.TargetNodeGUID);
        }

        private void ProceedToNarrative(string narrativeDataGUID)
        {
            var text = dialogue.dialogueNodeData.Find(x => x.GUID == narrativeDataGUID).Content.ToString();
            var choices = dialogue.nodeLinks.Where(x => x.BaseNodeGUID == narrativeDataGUID);
            dialogueText.text = ProcessProperties(text);
            var buttons = buttonContainer.GetComponentsInChildren<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Destroy(buttons[i].gameObject);
            }

            foreach (var choice in choices)
            {
                var button = Instantiate(choicePrefab, buttonContainer);
                button.GetComponentInChildren<Text>().text = ProcessProperties(choice.PortName);
                button.onClick.AddListener(() => ProceedToNarrative(choice.TargetNodeGUID));
            }
        }

        private string ProcessProperties(string text)
        {
            foreach (var exposedProperty in dialogue.exposedProperties)
            {
                text = text.Replace($"[{exposedProperty.PropertyName}]", exposedProperty.PropertyValue);
            }
            return text;
        }
    }
}