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
    abstract class StateMachineGenerator {
        public string name;

        public abstract VisualElement GetPropertyElements();
        public abstract IEnumerable<Object> Generate(AnimatorController animatorController, AnimatorStateMachine stateMachine);
    }
}
