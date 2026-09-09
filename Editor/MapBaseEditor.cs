using UnityEditor;
using UnityEngine;

namespace CustomGeo
{
    [CustomEditor(typeof(MapBase), true)]
    [CanEditMultipleObjects]
    public class MapBaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var modeProp = serializedObject.FindProperty("tileGenerationMode");
            var mode = (TileGenerationMode)modeProp.enumValueIndex;

            SerializedProperty prop = serializedObject.GetIterator();
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (prop.name == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(prop, true);
                    continue;
                }

                if (prop.name == "minZoom" || prop.name == "maxZoom")
                    continue;

                if (prop.name == "zoom" || prop.name == "blocks")
                {
                    if (mode == TileGenerationMode.Blocks)
                        EditorGUILayout.PropertyField(prop, true);
                    continue;
                }

                if (prop.name == "tileCamera"
                    || prop.name == "viewPadding"
                    || prop.name == "maxVisibleTiles")
                {
                    if (mode == TileGenerationMode.OrthoCamera)
                        EditorGUILayout.PropertyField(prop, true);
                    continue;
                }

                EditorGUILayout.PropertyField(prop, true);

                if (prop.name == "tileGenerationMode" && mode == TileGenerationMode.OrthoCamera)
                {
                    DrawZoomRange();
                    EditorGUILayout.HelpBox(
                        "Blocks is ignored. Tiles fill the camera FOV at one zoom. " +
                        "Prefers the finest zoom; if that needs more tiles than Max Visible Tiles, drops to a coarser zoom (19 → 18). " +
                        "Switching back to a finer zoom requires ~75% of the budget so the view does not flicker at the limit.",
                        MessageType.Info);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawZoomRange()
        {
            var minProp = serializedObject.FindProperty("minZoom");
            var maxProp = serializedObject.FindProperty("maxZoom");

            EditorGUILayout.LabelField("Zoom Range (LOD)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            minProp.intValue = EditorGUILayout.IntField(minProp.intValue, GUILayout.Width(36));
            float min = minProp.intValue;
            float max = maxProp.intValue;
            EditorGUILayout.MinMaxSlider(ref min, ref max, 0f, 22f);
            maxProp.intValue = EditorGUILayout.IntField(Mathf.RoundToInt(max), GUILayout.Width(36));
            minProp.intValue = Mathf.RoundToInt(min);
            EditorGUILayout.EndHorizontal();

            minProp.intValue = Mathf.Clamp(minProp.intValue, 0, 22);
            maxProp.intValue = Mathf.Clamp(maxProp.intValue, 0, 22);
            if (minProp.intValue > maxProp.intValue)
            {
                int tmp = minProp.intValue;
                minProp.intValue = maxProp.intValue;
                maxProp.intValue = tmp;
            }

            int levels = maxProp.intValue - minProp.intValue;
            if (levels > 0)
            {
                int childCount = 1 << (2 * levels);
                EditorGUILayout.LabelField(
                    $"One zoom {minProp.intValue} tile covers {childCount} tiles at zoom {maxProp.intValue}.",
                    EditorStyles.miniLabel);
            }
        }
    }
}
