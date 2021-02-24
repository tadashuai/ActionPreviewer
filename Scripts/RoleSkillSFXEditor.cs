#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.IO;

namespace ActionPreviewer
{
	[CustomEditor( typeof( RoleSkillSFX ) )]
	public class RoleSkillSFXEditor : Editor
	{
		private static RoleSkillSFX m_RSS;
		private static ReorderableList m_ReorderableList;

		private void OnEnable()
		{
			m_RSS = target as RoleSkillSFX;
			ReadFromJSON( m_RSS );

			m_ReorderableList = new ReorderableList( m_RSS.m_RoleSkill, typeof( RoleSkillSFX.PrefabSkillID ) );

			m_ReorderableList.drawElementCallback = ( rect, index, isActive, isFocused ) =>
			{
				var element = m_RSS.m_RoleSkill[ index ];
				float y = rect.yMin + 1f;
				element.m_RolePrefab = ( GameObject ) EditorGUI.ObjectField( new Rect( rect.xMin, y, rect.width, 16f ), new GUIContent( element.m_RolePrefab == null ? string.Format( "player{0}", index ) : element.m_RolePrefab.name, element.m_RolePrefab == null ? "只能添加有Animation组件的预制体" : "" ), element.m_RolePrefab, typeof( GameObject ), false );
				y += 16f;

				if ( element.m_RolePrefab != null )
				{
					Animation anim = element.m_RolePrefab.GetComponent<Animation>();
					if ( anim == null )
						element.m_RolePrefab = null;
					else
					{
						if ( element.m_InitCount == 0 )
						{
							foreach ( AnimationState state in anim )
							{
								element.m_Animations.Add( state.clip );
								element.m_SkillIDList.Add( 0 );
								element.m_InitCount += 1;
							}
						}
					}

					element.isFoldout = EditorGUI.Foldout( new Rect( rect.xMin, y, rect.width, 16f ), element.isFoldout, "Animation SkillID" );
					if ( element.isFoldout )
					{
						y += 16f;
						EditorGUI.BeginChangeCheck();
						int count = element.m_Animations != null ? element.m_Animations.Count : 0;
						count = EditorGUI.DelayedIntField( new Rect( rect.xMin + 16f, y, rect.width - 16f, 16f ), "Size", count );

						if ( EditorGUI.EndChangeCheck() )
						{
							m_RSS.CheckListCount( element.m_SkillIDList, count, element.m_InitCount );
							m_RSS.CheckListCount( element.m_Animations, count, element.m_InitCount );
						}

						for ( int i = 0; i < count; i++ )
						{
							y += 16f;
							if ( i < element.m_InitCount )
								EditorGUI.ObjectField( new Rect( rect.xMin + 16f, y, rect.width - 100f, 16f ), string.Format( "Element{0}", i ), element.m_Animations[ i ], typeof( AnimationClip ), false );
							else
								element.m_Animations[ i ] = ( AnimationClip ) EditorGUI.ObjectField( new Rect( rect.xMin + 16f, y, rect.width - 100f, 16f ), string.Format( "Element{0}", i ), element.m_Animations[ i ], typeof( AnimationClip ), false );
							element.m_SkillIDList[ i ] = EditorGUI.IntField( new Rect( rect.xMax - 80f, y, 80f, 16f ), element.m_SkillIDList[ i ] );
						}
					}
				}
			};

			m_ReorderableList.drawHeaderCallback = ( rect ) =>
			{
				EditorGUI.LabelField( rect, "Role Prefabs & Skill ID List" );
			};

			m_ReorderableList.elementHeightCallback = ( index ) =>
			{
				float height = 20f;
				var element = m_RSS.m_RoleSkill[ index ];
				if ( element.m_RolePrefab != null )
				{
					height += 16f;
					if ( element.isFoldout )
					{
						height += 16f * ( element.m_SkillIDList.Count + 1 );
					}
				}

				return height;
			};
		}

		public override void OnInspectorGUI()
		{
			EditorGUI.BeginChangeCheck();

			serializedObject.Update();
			m_ReorderableList.DoLayoutList();
			serializedObject.ApplyModifiedProperties();

			GUILayout.FlexibleSpace();
			GUILayout.Space( 10 );
			if ( GUILayout.Button( "Clear the whole List", "LargeButton" ) )
			{
				if ( EditorUtility.DisplayDialog( "警告", "是否确定清空列表中所有数据", "确定", "取消" ) )
				{
					m_RSS.m_RoleSkill.Clear();
				}
			}

			if ( EditorGUI.EndChangeCheck() )
			{
				SaveAsJSON();
			}
		}

		public static void ImportPlayer( GameObject obj )
		{
			Animation anim = obj.GetComponent<Animation>();
			if ( anim != null )
			{
				ReorderableList.defaultBehaviours.DoAddButton( m_ReorderableList );
				m_RSS.m_RoleSkill[ m_RSS.m_RoleSkill.Count - 1 ].m_RolePrefab = obj;
				foreach ( AnimationState state in anim )
				{
					m_RSS.m_RoleSkill[ m_RSS.m_RoleSkill.Count - 1 ].m_Animations.Add( state.clip );
					m_RSS.m_RoleSkill[ m_RSS.m_RoleSkill.Count - 1 ].m_SkillIDList.Add( 0 );
					m_RSS.m_RoleSkill[ m_RSS.m_RoleSkill.Count - 1 ].m_InitCount += 1;
				}
			}
		}

		public void SaveAsJSON()
		{
			string json = JsonUtility.ToJson( m_RSS );
			string path = System.Environment.GetFolderPath( System.Environment.SpecialFolder.MyDocuments ).Replace( "\\", "/" ) + "/ActionPreviewer";
			if ( !Directory.Exists( path ) )
				Directory.CreateDirectory( path );
			File.WriteAllText( path + "/RoleSkillSFXsSo.json", json, System.Text.Encoding.UTF8 );
		}

		public static void ReadFromJSON( RoleSkillSFX rss )
		{
			string path = System.Environment.GetFolderPath( System.Environment.SpecialFolder.MyDocuments ).Replace( "\\", "/" ) + "/ActionPreviewer/RoleSkillSFXsSo.json";
			if ( File.Exists( path ) )
			{
				StreamReader sr = new StreamReader( path );
				string json = sr.ReadToEnd();
				JsonUtility.FromJsonOverwrite( json, rss );
				sr.Close();
			}
			AssetDatabase.Refresh();
		}

		public class RoleSkillSFXEditorWindow : EditorWindow
		{
			[MenuItem( "SceneEditor/Action Previewer/Import Role Prefabs", false, 0 )]
			static void OpenWindow()
			{
				EditorWindow window = GetWindow<RoleSkillSFXEditorWindow>();
				window.titleContent.text = "Import Role Prefabs";
				window.Show();
			}

			public List<GameObject> prefabList = new List<GameObject>();

			SerializedObject serObj;
			SerializedProperty serProp;

			private void OnEnable()
			{
				serObj = new SerializedObject( this );
				serProp = serObj.FindProperty( "prefabList" );
			}

			private void OnGUI()
			{
				serObj.Update();
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serProp, true );
				if ( EditorGUI.EndChangeCheck() )
				{
					serObj.ApplyModifiedProperties();
				}

				if ( GUILayout.Button( "Import Role Prefabs" ) )
				{
					if ( prefabList.Count > 0 )
					{
						for ( int i = 0; i < prefabList.Count; i++ )
						{
							ImportPlayer( prefabList[ i ] );
						}
					}

					ReadFromJSON( m_RSS );
					var so = AssetDatabase.LoadAssetAtPath<RoleSkillSFX>( "Assets/Samples/ActionPreviewer/RoleSkillSFXsSo.asset" );
					if ( so != null )
						Selection.activeObject = so;
				}
			}
		}
	}
}
#endif