#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ActionPreviewer
{
	[CustomEditor( typeof( ActionPreview ) )]
	public class ActionPreviewEditor : Editor
	{
		[MenuItem( "SceneEditor/Action Previewer/Instantiate ActionPreviewer", false, 30 )]
		static void CreatePreviewGo()
		{
			GameObject go = GameObject.Find( "ActionPreviewManager" );
			if ( go == null )
			{
				go = AssetDatabase.LoadAssetAtPath<GameObject>( "Assets/Samples/ActionPreviewer/ActionPreviewManager.prefab" );
				if ( go != null )
				{
					go = Instantiate( go );
					go.name = go.name.Replace( "(Clone)", "" );
					//EditorApplication.ExecuteMenuItem( "Edit/Play" );
				}
				else
				{
					Debug.LogError( "未找到文件 Assets/Samples/ActionPreviewer/ActionPreviewManager.prefab" );
					return;
				}
			}
			Selection.activeGameObject = go;
		}

		[MenuItem( "SceneEditor/Action Previewer/Locate ScriptableObject", false, 1 )]
		static void Locate()
		{
			Selection.activeObject = AssetDatabase.LoadAssetAtPath<RoleSkillSFX>( "Assets/Samples/ActionPreviewer/RoleSkillSFXsSo.asset" );
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			ActionPreview preview = target as ActionPreview;

			EditorGUILayout.BeginHorizontal();
			if ( GUILayout.Button( "Focus Position" ) )
			{
				if ( preview.m_bornPos != Vector3.zero )
				{
					GameObject go = new GameObject();
					go.transform.position = preview.m_bornPos;
					Selection.activeGameObject = go;
					SceneView.lastActiveSceneView.FrameSelected();
					DestroyImmediate( go );
					Selection.activeGameObject = preview.gameObject;
				}
			}
			if ( GUILayout.Button( "Cast Position" ) )
			{
				var view = SceneView.currentDrawingSceneView ?? SceneView.lastActiveSceneView;
				if ( view != null )
				{
					var camera = view.camera;
					if ( camera != null )
					{
						var ray = camera.ScreenPointToRay( new Vector3( view.position.width * 0.5f, view.position.height * 0.5f, 0.0f ) );
						RaycastHit hit;
						if ( Physics.Raycast( ray, out hit ) )
						{
							preview.m_bornPos = hit.point;
						}
					}
				}
			}
			EditorGUILayout.EndHorizontal();
		}
	}

	public class ActionPreviewerOptionWindow : EditorWindow
	{
		static EditorWindow window;

		[MenuItem( "SceneEditor/Action Previewer/Option", false, 50 )]
		static void OpenOptionWindow()
		{
			window = GetWindow<ActionPreviewerOptionWindow>();
			window.titleContent.text = "Action Previewer Option";
			window.minSize = new Vector2( 320f, 320f );
			window.Show();
		}

		GameObject apGo;
		SerializedObject serObj;
		SerializedProperty keyCode1, keyCode2, keyCode3, vfxPath;
		static string projectPath = string.Empty;

		private void OnEnable()
		{
			projectPath = System.IO.Directory.GetCurrentDirectory().Replace( "\\", "/" ) + "/";
			apGo = GameObject.Find( "ActionPreviewManager" );
			if ( apGo != null )
			{
				serObj = new SerializedObject( apGo.GetComponent<ActionPreview>() );
				keyCode1 = serObj.FindProperty( "ActionKeyCode" );
				keyCode2 = serObj.FindProperty( "RoleKeyCode" );
				keyCode3 = serObj.FindProperty( "SFXKeyCode" );
				vfxPath = serObj.FindProperty( "m_VFXFolderPath" );
			}
		}

		private void OnGUI()
		{
			if ( apGo != null )
			{
				EditorGUILayout.Space();
				serObj.Update();
				EditorGUI.BeginChangeCheck();

				GUILayout.Label( "KeyCodes", "sv_label_1" );
				EditorGUILayout.PropertyField( keyCode1, new GUIContent( "ActionCanvas KeyCode" ) );
				EditorGUILayout.PropertyField( keyCode2, new GUIContent( "RoleCanvas   KeyCode" ) );
				EditorGUILayout.PropertyField( keyCode3, new GUIContent( "SFXCanvas    KeyCode" ) );
				EditorGUILayout.Space();

				GUILayout.Label( "Others", "sv_label_2" );
				EditorGUILayout.TextField( new GUIContent( "VFX Folder Path" ), vfxPath.stringValue );
				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if ( GUILayout.Button( "Change Folder", GUILayout.MinWidth( 160f ) ) )
				{
					string path = EditorUtility.SaveFolderPanel( "选择文件夹", projectPath + "Assets", "" );
					vfxPath.stringValue = path.Replace( projectPath, "" ) + "/";
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space();
				if ( GUILayout.Button( "恢复默认设置" ) )
				{
					if ( EditorUtility.DisplayDialog( "警告", "是否确定恢复所有默认设置", "确定", "取消" ) )
					{
						keyCode1.enumValueIndex = 61;
						keyCode2.enumValueIndex = 60;
						keyCode3.enumValueIndex = 54;
						vfxPath.stringValue = "Assets/Resources/VFX/";
					}
				}

				if ( EditorGUI.EndChangeCheck() )
					serObj.ApplyModifiedProperties();
			}
			else
			{
				GUILayout.Space( 10 );
				GUIStyle style = new GUIStyle();
				style.alignment = TextAnchor.MiddleCenter;
				style.fontSize = 12;
				style.normal.textColor = Color.red;
				style.richText = true;
				GUILayout.Label( "There is no ActionPreviewManager in Hierarchy!", style );
			}
		}
	}
}
#endif