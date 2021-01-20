using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace EsnyaFactory
{
    public class Constants {
        public const int spacing = 8;
    }

    public class LabeledBox : Box {
        public LabeledBox(string label) {
            style.marginBottom = Constants.spacing;
            Add(new Label(label));
        }
    }

    public class FieldLabel : Label {
        public FieldLabel(string text) {
            this.text = text;
            style.marginTop = Constants.spacing;
        }
    }

    public class ExObjectField<T> : BaseField<T>, INotifyValueChanged<T> where T : Object
    {
        ObjectField objectField = new ObjectField();

        public ExObjectField() {
            objectField.objectType = typeof(T);
            Add(objectField);
        }

        public override T value {
            get => objectField.value as T;
            set => objectField.value = value;
        }
    }
}
