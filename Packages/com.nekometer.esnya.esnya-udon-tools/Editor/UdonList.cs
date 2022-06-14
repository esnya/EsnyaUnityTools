#if UDON
using System.ComponentModel;
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using VRC.Udon;
#if UNITY_2019
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace EsnyaFactory
{
    public class UdonList : EditorWindow
    {
#if UNITY_2019
        private class UdonListItem : VisualElement
        {
            private Label nameLabel = new Label(), sourceLabel = new Label();
            public UdonListItem()
            {
                style.display = DisplayStyle.Flex;
                style.flexDirection = FlexDirection.Row;

                nameLabel.style.flexGrow = 1.0f;
                nameLabel.style.flexBasis = 0;
                sourceLabel.style.flexGrow = 1.0f;
                sourceLabel.style.flexBasis = 0;
                Add(nameLabel);
                Add(sourceLabel);
            }

            public void BindUdonBehaviour(UdonBehaviour udonBehaviour)
            {
                nameLabel.text = udonBehaviour.gameObject.name;
                sourceLabel.text = udonBehaviour.programSource?.name ?? "(null)";
            }
        }

        private class UdonListView : VisualElement
        {
            private const int ItemHeight = 16;

            private int sort = -1;
            private bool desc;
            private UdonBehaviour[] udonList;
            private ListView listView;
            private ToolbarButton objectButton, sourceButton;

            public UdonListView()
            {
                udonList = FindObjectsOfType<UdonBehaviour>();

                style.flexGrow = 1.0f;

                var toolbar = new Toolbar();
                toolbar.style.paddingRight = 10;
                Add(toolbar);

                objectButton = new ToolbarButton(() => ToggleSort(0));
                objectButton.text = "GameObject";
                objectButton.style.flexGrow = 1.0f;
                objectButton.style.flexBasis = 0;
                toolbar.Add(objectButton);

                sourceButton = new ToolbarButton(() => ToggleSort(1));
                sourceButton.text = "Udon Program Source";
                sourceButton.style.flexGrow = 1.0f;
                sourceButton.style.flexBasis = 0;
                toolbar.Add(sourceButton);

                listView = new ListView(
                    udonList,
                    ItemHeight,
                    () => new UdonListItem(),
                    (e, i) => (e as UdonListItem)?.BindUdonBehaviour(udonList[i])
                );
                listView.selectionType = SelectionType.Multiple;
                listView.style.flexGrow = 1.0f;

                listView.onSelectionChanged += objects =>
                {
                    Selection.objects = objects.Select(o => (o as UdonBehaviour)?.gameObject).Where(o => o != null).ToArray();
                };
                Add(listView);

                UpdateToolbarText();
            }

            private void ToggleSort(int index)
            {
                if (sort == index) desc = !desc;
                else
                {
                    sort = index;
                    desc = false;
                }

                var sorted = udonList.OrderBy(udon => sort == 0 ? udon?.gameObject?.name : udon?.programSource?.name);
                udonList = (desc ? sorted.Reverse() : sorted).ToArray();
                listView.itemsSource = udonList;

                UpdateToolbarText();
            }

            private void UpdateToolbarText()
            {
                var sortMark = desc ? " ▼" : " ▲";
                objectButton.text = $"GameObject{(sort == 0 ? sortMark : "")}";
                sourceButton.text = $"Udon Program Source{(sort == 1 ? sortMark : "")}";
            }
        }

        [MenuItem("EsnyaTools/Udon List")]
        public static void ShowWindow()
        {
            var window = GetWindow<UdonList>();
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Udon List");
            rootVisualElement.Add(new UdonListView());
        }
#endif
    }
}
#endif
