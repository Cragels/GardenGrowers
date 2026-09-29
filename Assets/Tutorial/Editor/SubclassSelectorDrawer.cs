using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GardenGrowers.Core;
using UnityEditor;
using UnityEngine;

namespace GardenGrowers.EditorTools
{
    /// <summary>Draws the dropdown on [SubclassSelector] fields so you can pick a condition or action.</summary>
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public class SubclassSelectorDrawer : PropertyDrawer
    {
        private sealed class TypeMenu
        {
            public Type[] types;
            public GUIContent[] labels;
        }

        private static readonly Dictionary<Type, TypeMenu> Menus = new Dictionary<Type, TypeMenu>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            Type baseType = GetFieldType(property);
            TypeMenu menu = baseType != null ? GetMenu(baseType) : null;
            if (menu != null)
            {
                var popupRect = new Rect(
                    position.x + EditorGUIUtility.labelWidth + 2f,
                    position.y,
                    position.width - EditorGUIUtility.labelWidth - 2f,
                    EditorGUIUtility.singleLineHeight);

                int current = CurrentIndex(property, menu);
                int picked = EditorGUI.Popup(popupRect, current, menu.labels);
                if (picked != current)
                {
                    property.managedReferenceValue = picked <= 0 ? null : Activator.CreateInstance(menu.types[picked]);
                    property.isExpanded = picked > 0;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndProperty();
        }

        private static int CurrentIndex(SerializedProperty property, TypeMenu menu)
        {
            string full = property.managedReferenceFullTypename; // "Assembly Namespace.Type"
            if (string.IsNullOrEmpty(full)) return 0;
            for (int i = 1; i < menu.types.Length; i++)
            {
                Type type = menu.types[i];
                if (full == type.Assembly.GetName().Name + " " + type.FullName) return i;
            }
            return 0;
        }

        private static Type GetFieldType(SerializedProperty property)
        {
            string typeName = property.managedReferenceFieldTypename; // "Assembly Namespace.Type"
            if (string.IsNullOrEmpty(typeName)) return null;
            int split = typeName.IndexOf(' ');
            if (split < 0) return null;
            string assembly = typeName.Substring(0, split);
            string className = typeName.Substring(split + 1);
            return Type.GetType(className + ", " + assembly);
        }

        private static TypeMenu GetMenu(Type baseType)
        {
            if (Menus.TryGetValue(baseType, out TypeMenu cached)) return cached;

            List<Type> types = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(t => !t.IsAbstract && !t.IsGenericType && !typeof(UnityEngine.Object).IsAssignableFrom(t)
                            && t.IsDefined(typeof(SerializableAttribute), false)
                            && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(Label)
                .ToList();

            var menu = new TypeMenu
            {
                types = new Type[types.Count + 1],
                labels = new GUIContent[types.Count + 1]
            };
            menu.labels[0] = new GUIContent("(None)");
            for (int i = 0; i < types.Count; i++)
            {
                menu.types[i + 1] = types[i];
                menu.labels[i + 1] = new GUIContent(Label(types[i]));
            }

            Menus[baseType] = menu;
            return menu;
        }

        private static string Label(Type type)
        {
            var path = type.GetCustomAttribute<MenuPathAttribute>();
            if (path != null) return path.Path;
            string name = type.Name.Replace("Condition", string.Empty).Replace("Action", string.Empty);
            return ObjectNames.NicifyVariableName(name);
        }
    }
}
