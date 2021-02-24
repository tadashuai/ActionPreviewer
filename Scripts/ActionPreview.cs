#if UNITY_EDITOR
using System.Collections.Generic;
using Common;
using Database;
using UniScene;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
#if USE_NATIVE_LUA
using LuaDatabase = NativeLua.LuaDatabase;
using LuaTasker = NativeLua.LuaTasker;
using LuaStartup = NativeLua.LuaStartup;
#else
using LuaDatabase = UniLuax.LuaDatabase;
using LuaTasker = UniLuax.LuaTasker;
using LuaStartup = UniLuax.LuaStartup;
#endif

namespace ActionPreviewer
{
	public class ActionPreview : MonoBehaviour
	{
		#region Role
		public GameObject m_mainRole;
		GameObject m_prefab;
		public string m_prefabPath;
		public float m_roleMoveSpeed = 5f;
		float m_standardRoleMoveSpeed = 5.0f;
		public Vector3 m_bornPos = Vector3.zero;
		Vector3 m_targetPos = Vector3.zero;
		float start_time, press_duration = .15f;
		bool isPress, isMove, isPressUp;
		#endregion

		#region Camera
		Camera m_mainCam;
		CameraController m_cameraController;
		Ray m_ray;
		RaycastHit m_hit;
		#endregion

		#region Animation
		Animation m_roleAnim;
		AnimationState currentState, lastState;
		int frameCount, curFrame;
		List<AnimationState> stateList = new List<AnimationState>();
		List<string> stateNameList = new List<string>();
		string currentStateName;
		bool checkNone = false;
		#endregion

		#region UI
		Button animationButtonSource, roleButtonSource;
		Dictionary<string, Button> animationButtons = new Dictionary<string, Button>();
		List<Button> roleSwitchButtons = new List<Button>();
		Button playTogetherButton;
		Button[] playTogetherButtons;
		Toggle playTogetherToggle;
		Transform playTogetherTransform;
		Slider animSlider;
		Text frameText;
		[HideInInspector] public bool isDrag;
		Text nonRoleWarningText;
		bool isRoleCanvasShow = false;
		bool isActionCanvasShow = false;
		bool isSFXCanvasShow = false;

		Button Level1Button;
		Button Level2Button;
		Button Level3Button;

		[HideInInspector] public KeyCode ActionKeyCode = KeyCode.P;
		[HideInInspector] public KeyCode RoleKeyCode = KeyCode.O;
		[HideInInspector] public KeyCode SFXKeyCode = KeyCode.I;
		#endregion

		#region SFX
		RoleSkillSFX m_RSS;
		RoleSkillSFX.PrefabSkillID m_PrefabSkillID;
		Transform m_SFXPrefabParent;
		GameObject lastLevel;
		SFXLevel m_SFXLevel = SFXLevel.level_1;
		Dictionary<string, int> skillIDDict = new Dictionary<string, int>();
		[HideInInspector] public string m_VFXFolderPath = "Assets/Resources/VFX/";

		public enum SFXLevel
		{
			level_1,
			level_2,
			level_3
		}
		#endregion

		private void Awake()
		{
			InitializeUI();
			InitializeCamera();
			InitializeSFX();
			CreateAnimationButtons();
			CreateRoleSwitchButtons();

			currentState = m_roleAnim[ m_roleAnim.clip.name ];
			m_standardRoleMoveSpeed = m_roleMoveSpeed;
			frameCount = ( int ) ( currentState.length * currentState.clip.frameRate );
			animSlider.maxValue = frameCount;
			animSlider.transform.parent.gameObject.GetComponent<Canvas>().targetDisplay = 1;
		}

		void InitializeUI()
		{
			animationButtonSource = GameObject.Find( "ActionCanvas/ActionsScrollView/Viewport/Content/Button" ).GetComponent<Button>();
			roleButtonSource = GameObject.Find( "RoleCanvas/ScrollView/Viewport/Content/Button" ).GetComponent<Button>();
			animSlider = GameObject.Find( "ActionCanvas/Slider" ).GetComponent<Slider>();
			frameText = GameObject.Find( "ActionCanvas/Slider/Text" ).GetComponent<Text>();
			nonRoleWarningText = GameObject.Find( "RoleCanvas/ScrollView/Viewport/WarningText" ).GetComponent<Text>();
			playTogetherTransform = GameObject.Find( "ActionCanvas/PlayTogetherScrollView/Viewport/Content" ).transform;
			playTogetherButton = GameObject.Find( "ActionCanvas/PlayTogetherScrollView/PlayTogetherButton" ).GetComponent<Button>();
			playTogetherTransform.parent.parent.gameObject.SetActive( false );
			playTogetherButton.onClick.AddListener( delegate
			{
				stateList.Clear();
				stateNameList.Clear();
				playTogetherButtons = playTogetherTransform.GetComponentsInChildren<Button>();
				if ( playTogetherButtons.Length > 0 )
				{
					for ( int i = 0; i < playTogetherButtons.Length; i++ )
					{
						string skillName = playTogetherButtons[ i ].name.TrimStart( ' ' );
						AnimationState tempState = m_roleAnim[ skillName ];
						AnimationState queue = null;
						int tempframeCount = ( int ) ( tempState.length * tempState.clip.frameRate );
						if ( i == 0 )
						{
							queue = m_roleAnim.PlayQueued( skillName, QueueMode.PlayNow );
							frameCount = tempframeCount;
						}
						else
						{
							queue = m_roleAnim.PlayQueued( skillName );
							frameCount += tempframeCount + 1;
						}
						stateList.Add( queue );
						stateNameList.Add( skillName );
					}
					animSlider.maxValue = frameCount;
				}
				else
					Debug.LogWarning( "列表为空!!!" );
			} );
			playTogetherToggle = GameObject.Find( "ActionCanvas/PlayTogetherToggle" ).GetComponent<Toggle>();
			playTogetherToggle.onValueChanged.AddListener( delegate ( bool isOn )
			{
				m_roleAnim.Stop();
				if ( isOn )
				{
					playTogetherTransform.parent.parent.gameObject.SetActive( true );
					animationButtons[ currentState.name ].gameObject.GetComponent<Image>().fillAmount = 1f;
					animationButtons[ currentState.name ].gameObject.GetComponent<Image>().color = Color.white;
				}
				else
				{
					playTogetherTransform.parent.parent.gameObject.SetActive( false );
					stateList.Clear();
					stateNameList.Clear();
					currentState = m_roleAnim[ m_roleAnim.clip.name ]; //TODO
					m_roleAnim.Play( m_roleAnim.clip.name );
					frameCount = ( int ) ( currentState.length * currentState.clip.frameRate );
					animSlider.maxValue = frameCount;
				}
			} );

			Level1Button = GameObject.Find( "SFXCanvas/Level1Button" ).GetComponent<Button>();
			Level2Button = GameObject.Find( "SFXCanvas/Level2Button" ).GetComponent<Button>();
			Level3Button = GameObject.Find( "SFXCanvas/Level3Button" ).GetComponent<Button>();
			Level1Button.onClick.AddListener( delegate
			{
				m_SFXLevel = SFXLevel.level_1;
				Level1Button.gameObject.GetComponent<Image>().color = Color.green;
				Level2Button.gameObject.GetComponent<Image>().color = Color.white;
				Level3Button.gameObject.GetComponent<Image>().color = Color.white;
			} );
			Level2Button.onClick.AddListener( delegate
			{
				m_SFXLevel = SFXLevel.level_2;
				Level1Button.gameObject.GetComponent<Image>().color = Color.white;
				Level2Button.gameObject.GetComponent<Image>().color = Color.green;
				Level3Button.gameObject.GetComponent<Image>().color = Color.white;
			} );
			Level3Button.onClick.AddListener( delegate
			{
				m_SFXLevel = SFXLevel.level_3;
				Level1Button.gameObject.GetComponent<Image>().color = Color.white;
				Level2Button.gameObject.GetComponent<Image>().color = Color.white;
				Level3Button.gameObject.GetComponent<Image>().color = Color.green;
			} );
		}

		void InitializeCamera()
		{
			m_cameraController = GameObject.Find( "Camera" ).GetComponent<CameraController>();
			if ( m_cameraController == null )
			{
				var camera = AssetDatabase.LoadAssetAtPath<GameObject>( "Assets/GameAssets/scene/prefab/Thinq/Camera.prefab" );
				Instantiate( camera );
				m_cameraController = camera.GetComponent<CameraController>();
			}
			if ( m_mainRole != null )
			{
				CreateRoleGameObject();
				var cc = m_mainRole.GetComponent<CharacterController>();
				if ( cc != null )
				{
					cc.enabled = false;
				}
				m_cameraController.m_target = m_mainRole.transform;
			}
			if ( m_cameraController.camera == null )
			{
				var camRoot = m_cameraController.gameObject.transform.Find( "CameraNode" );
				if ( camRoot != null )
				{
					if ( camRoot.childCount == 0 )
					{
						var camGo = Instantiate( new GameObject(), camRoot );
						camGo.name = "Camera";
						var cam = UnityUtils.RequireComponent<Camera>( camGo );
						cam.cullingMask = LayerMask.NameToLayer( "Everything" );
						m_cameraController.camera = cam;
					}
					else
					{
						var cam = camRoot.GetComponentInChildren<Camera>();
						m_cameraController.camera = cam;
					}
				}
			}
			m_mainCam = m_cameraController.camera;
		}

		void InitializeSFX()
		{
			ProjectPath.dbLocale = "China";
			if ( LuaTasker.hasInitialized == false || LuaDatabase.sharedInstance == null )
			{
				LuaStartup.ReInitialize();
			}
			Skill.EnsureLoaded();
			Skill.CacheAll();

			m_RSS = AssetDatabase.LoadAssetAtPath<RoleSkillSFX>( "Assets/Samples/ActionPreviewer/RoleSkillSFXsSo.asset" );
			if ( m_RSS != null )
			{
				RoleSkillSFXEditor.ReadFromJSON( m_RSS );
				for ( int i = 0; i < m_RSS.m_RoleSkill.Count; i++ )
				{
					if ( m_RSS.m_RoleSkill[ i ].m_RolePrefab.name == m_mainRole.name )
					{
						m_PrefabSkillID = m_RSS.m_RoleSkill[ i ];
						break;
					}
				}
				if ( m_PrefabSkillID == null )
				{
					Debug.LogWarning( "There is no this prefab's skill ID list in the Role Prefabs & Skill ID List!\nAssets/Samples/ActionPreviewer/RoleSkillSFXsSo.asset" );
				}
			}
			else
				Debug.LogError( "没有找到ScriptableObject文件 Assets/Samples/ActionPreviewer/RoleSkillSFXsSo.asset" );
			
		}

		void CreateRoleGameObject()
		{
			m_prefab = m_mainRole;
			var go = Instantiate( m_mainRole );
			if ( go != null )
			{
				var root = go.GetComponent<MonoBehaviourBinderRoot>();
				if ( root != null )
				{
					var binders = root.GetBinders();
					if ( binders != null )
					{
						for ( int i = 0; i < binders.Length; i++ )
						{
							if ( binders[ i ] != null )
							{
								DestroyImmediate( binders[ i ] );
							}
						}
					}
					DestroyImmediate( root );
				}
				go.SetActive( true );
				go.name = go.name.Replace( "(Clone)", "" );
				m_mainRole = go;
				if ( m_bornPos != Vector3.zero )
				{
					go.transform.position = m_bornPos;
					m_targetPos = go.transform.localPosition;
				}

			}
		}

		void CreateAnimationButtons()
		{
			skillIDDict.Clear();
			animationButtons.Clear();
			animationButtonSource.gameObject.SetActive( true );
			while ( animationButtonSource.transform.parent.childCount > 1 )
			{
				if ( animationButtonSource.transform.parent.GetChild( 0 ).name == animationButtonSource.gameObject.name )
					DestroyImmediate( animationButtonSource.transform.parent.GetChild( 1 ).gameObject );
				else
					DestroyImmediate( animationButtonSource.transform.parent.GetChild( 0 ).gameObject );
			}

			if ( m_mainRole != null )
			{
				if ( m_mainRole.GetComponent<Animation>() != null )
				{
					m_roleAnim = m_mainRole.GetComponent<Animation>();
					if ( m_PrefabSkillID != null )
					{
						bool isFounding = true;
						while ( isFounding )
						{
							isFounding = false;
							foreach ( AnimationState state in m_roleAnim )
							{
								isFounding = true;
								m_roleAnim.RemoveClip( state.clip );
							}
						}

						checkNone = false;
						for ( int i = 0; i < m_PrefabSkillID.m_Animations.Count; i++ )
						{
							if ( m_PrefabSkillID.m_Animations[ i ] != null )
								m_roleAnim.AddClip( m_PrefabSkillID.m_Animations[ i ], m_PrefabSkillID.m_Animations[ i ].name + "/" + m_PrefabSkillID.m_SkillIDList[ i ].ToString() );
							else
							{
								Debug.LogException( new NoneObjectException( string.Format( "{0}的第{1}个AnimationClip为空!!!", m_mainRole.name, i ) ), m_RSS );
								checkNone = true;
							}
						}
						if ( checkNone )
						{
							EditorApplication.ExecuteMenuItem( "Edit/Play" );
						}

						if ( m_roleAnim.GetClip( "idle/0" ) != null )
							m_roleAnim.clip = m_roleAnim.GetClip( "idle/0" );
						else
							m_roleAnim.clip = m_roleAnim.GetClip( m_PrefabSkillID.m_Animations[ 0 ].name );

						currentState = m_roleAnim[ m_roleAnim.clip.name ];
						m_roleAnim.Play( currentState.name );
					}
					int index = -1;
					foreach ( AnimationState state in m_roleAnim )
					{
						index += 1;
						int tempIndex = index;
						Button button = Instantiate( animationButtonSource, animationButtonSource.transform.parent );
						button.name = "  " + state.clip.name;
						button.transform.Find( "Name" ).GetComponent<Text>().text = button.name.Substring( 0, button.name.IndexOf( '/' ) );
						button.transform.Find( "Time" ).GetComponent<Text>().text = string.Format( "{0:0.00}", state.length );
						button.onClick.AddListener( delegate
						{
							if ( playTogetherToggle.isOn )
							{
								Button newBtn = Instantiate( button, playTogetherTransform );
								newBtn.name = button.name;
								newBtn.onClick.RemoveAllListeners();
								newBtn.onClick.AddListener( delegate
								{
									DestroyImmediate( newBtn.gameObject );
								} );
							}
							else
							{
								m_roleAnim.Play( state.name, PlayMode.StopAll );
								lastState = currentState;
								currentState = m_roleAnim[ state.name ];
								PlaySFX( button.name, m_PrefabSkillID.m_SkillIDList[ tempIndex ], m_SFXLevel );
								frameCount = ( int ) ( state.length * state.clip.frameRate );
								animSlider.maxValue = frameCount;
							}
						} );
						animationButtons.Add( state.name, button );
						skillIDDict.Add( state.clip.name, tempIndex );
					}
					animationButtonSource.gameObject.SetActive( false );
				}
				else
				{
					UDebug.LogError( "There's no animation attatched to the player!!!!!" );
				}
			}
			else
			{
				UDebug.LogError( "No player exist!!!!!!" );
			}
		}

		void CreateRoleSwitchButtons()
		{
			roleSwitchButtons.Clear();
			if ( m_prefab != null )
				m_prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot( m_prefab );
			if ( m_mainRole != null && m_prefabPath != null )
			{
				m_prefabPath = m_prefabPath.Substring( 0, m_prefabPath.LastIndexOf( '/' ) );
				string[] allPath = AssetDatabase.FindAssets( "t:Prefab", new string[] { m_prefabPath } );
				for ( int i = 0; i < allPath.Length; i++ )
				{
					string path = AssetDatabase.GUIDToAssetPath( allPath[ i ] );
					var obj = AssetDatabase.LoadAssetAtPath<GameObject>( path );
					if ( obj != null )
					{
						if ( obj.name == m_prefab.name )
							obj = m_prefab;
						if ( !m_RSS.Contains( obj ) )
							continue;

						Button button = Instantiate( roleButtonSource, roleButtonSource.transform.parent );
						button.name = "  " + obj.name;
						button.transform.Find( "Text" ).GetComponent<Text>().text = button.name;
						button.onClick.AddListener( delegate
						 {
							 if ( obj.name == m_mainRole.name )
								 return;

							 Vector3 pos = m_mainRole.transform.position;
							 Quaternion rot = m_mainRole.transform.rotation;
							 DestroyImmediate( m_mainRole.gameObject );
							 var go = Instantiate( obj );

							 if ( go != null )
							 {
								 var root = go.GetComponent<MonoBehaviourBinderRoot>();
								 if ( root != null )
								 {
									 var binders = root.GetBinders();
									 if ( binders != null )
									 {
										 for ( int k = 0; k < binders.Length; k++ )
										 {
											 if ( binders[ k ] != null )
											 {
												 DestroyImmediate( binders[ k ] );
											 }
										 }
									 }
									 DestroyImmediate( root );
								 }
								 var cache = go.GetComponent<ModelInfoCache>();
								 if ( cache != null )
								 {
									 DestroyImmediate( cache );
								 }
								 var colls = go.GetComponents<Collider>();
								 if ( colls != null )
								 {
									 for ( int k = 0; k < colls.Length; k++ )
									 {
										 colls[ k ].enabled = false;
									 }
								 }
								 go.SetActive( true );
								 go.name = go.name.Replace( "(Clone)", "" );
								 go.transform.position = pos;
								 go.transform.rotation = rot;
								 m_mainRole = go;
								 m_cameraController.m_target = m_mainRole.transform;
								 m_roleAnim = m_mainRole.GetComponent<Animation>();
								 CreateAnimationButtons();
								 bool isFoundSkillID = false;
								 for ( int k = 0; k < m_RSS.m_RoleSkill.Count; k++ )
								 {
									 if ( m_RSS.m_RoleSkill[ k ].m_RolePrefab.name == m_mainRole.name )
									 {
										 m_PrefabSkillID = m_RSS.m_RoleSkill[ k ];
										 isFoundSkillID = true;
										 break;
									 }
								 }
								 if ( !isFoundSkillID )
								 {
									 Debug.LogWarning( "There is no this prefab's skill ID list in the Role Prefabs & Skill ID List!\nAssets/Samples/ActionPreviewer/RoleSkillSFXsSo.asset" );
								 }
							 }
						 } );
						roleSwitchButtons.Add( button );
					}
				}
				roleButtonSource.gameObject.SetActive( false );
			}
		}

		private void Update()
		{
			if ( Input.GetKeyDown( ActionKeyCode ) )
			{
				isActionCanvasShow = !isActionCanvasShow;
				animSlider.transform.parent.gameObject.GetComponent<Canvas>().targetDisplay = isActionCanvasShow ? 0 : 1;
				if ( isActionCanvasShow )
				{
					m_targetPos = m_mainRole.transform.localPosition;
					m_mainRole.transform.localRotation = Quaternion.Euler( 0f, 105f, 0f );
					m_roleAnim.CrossFade( "idle/0" );
				}
			}
			if ( Input.GetKeyDown( RoleKeyCode ) )
			{
				isRoleCanvasShow = !isRoleCanvasShow;
				nonRoleWarningText.transform.parent.parent.parent.GetComponent<Canvas>().targetDisplay = isRoleCanvasShow ? 0 : 1;
				if ( isRoleCanvasShow && roleSwitchButtons != null )
					nonRoleWarningText.gameObject.SetActive( roleSwitchButtons.Count <= 0 );
			}
			if ( Input.GetKeyDown( SFXKeyCode ) )
			{
				isSFXCanvasShow = !isSFXCanvasShow;
				Level1Button.transform.parent.GetComponent<Canvas>().targetDisplay = isSFXCanvasShow ? 0 : 1;
			}

			if ( isActionCanvasShow )
				Preview();
			else
				Move();
		}

		void Move()
		{
			CheckMouseButtonEvent();
			MoveToTarget();
			ChangeAnim();
		}

		void ChangeAnim()
		{
			if ( m_roleAnim != null )
			{
				if ( isMove )
				{
					if ( m_roleAnim.IsPlaying( "idle/0" ) || !m_roleAnim.isPlaying )
						m_roleAnim.CrossFade( "run/0" );
					m_roleAnim[ "run/0" ].speed = m_roleMoveSpeed / m_standardRoleMoveSpeed;
				}
				else
				{
					if ( m_roleAnim.IsPlaying( "run/0" ) )
						m_roleAnim.CrossFade( "idle/0" );
					m_roleAnim[ "idle/0" ].speed = m_roleMoveSpeed / m_standardRoleMoveSpeed;
				}
			}
			else
			{
				Debug.LogError( "The character does not match the animation!" );
			}
		}

		void MoveToTarget()
		{
			if ( isMove )
			{
				if ( Vector2.Distance( new Vector2( m_targetPos.x, m_targetPos.z ), new Vector2( m_mainRole.transform.localPosition.x, m_mainRole.transform.localPosition.z ) ) > .05f )
				{
					m_mainRole.transform.LookAt( new Vector3( m_targetPos.x, m_mainRole.transform.localPosition.y, m_targetPos.z ) );
					Vector3 offset = m_targetPos - m_mainRole.transform.localPosition;
					m_mainRole.transform.localPosition += offset.normalized * m_roleMoveSpeed * Time.deltaTime;

					RaycastHit hit;
					Vector3 startPos = m_mainRole.transform.position + Vector3.up * 100f;
					if ( Physics.Raycast( startPos, Vector3.down, out hit ) )
					{
						if ( hit.transform.gameObject.layer == LayerMask.NameToLayer( "TerrainMesh" ) )
						{
							if ( hit.point.y != m_mainRole.transform.localPosition.y )
							{
								m_mainRole.transform.localPosition = hit.point;
							}
						}
					}
				}
				else
				{
					isMove = isPress;
				}
			}
		}

		void CheckMouseButtonEvent()
		{
			if ( !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() )
			{
				if ( Input.GetButtonDown( "Fire1" ) )
				{
					start_time = Time.time;
					isPressUp = false;
				}
				else if ( Input.GetButton( "Fire1" ) )
				{
					if ( Time.time - start_time >= press_duration )
					{
						isPress = true;
						FindTargetPosition();
					}
				}
				else if ( Input.GetButtonUp( "Fire1" ) )
				{
					if ( isPress )
					{
						m_targetPos = m_mainRole.transform.localPosition;
						isPress = false;
						isPressUp = true;
					}
					else
					{
						FindTargetPosition();
					}
				}

				isMove = !isPressUp;
			}
		}

		void FindTargetPosition()
		{
			m_ray = m_mainCam.ScreenPointToRay( Input.mousePosition );
			if ( Physics.Raycast( m_ray, out m_hit ) && m_hit.transform.gameObject.layer == LayerMask.NameToLayer( "TerrainMesh" ) )
			{
				m_targetPos = m_hit.point;
			}
			else
			{
				Plane plane = new Plane( Vector3.up, m_mainRole.transform.position );
				float d = -1.0f;
				if ( plane.Raycast( m_ray, out d ) )
				{
					if ( d > 0 )
					{
						Vector3 point = m_ray.GetPoint( d );
						m_targetPos = point;
					}
				}
			}
		}

		void Preview()
		{
			if ( !playTogetherToggle.isOn )
			{
				if ( isDrag )
				{
					if ( !m_roleAnim.isPlaying )
						m_roleAnim.Play( currentState.name );
					curFrame = ( int ) animSlider.value;
					currentState.normalizedTime = curFrame / ( float ) frameCount;
				}
				else
				{
					if ( m_roleAnim.isPlaying )
					{
						if ( currentState.normalizedTime >= 1f )
						{
							if ( currentState.wrapMode == WrapMode.Loop )
								currentState.normalizedTime -= 1f;
						}
					}
					else
					{
						currentState.normalizedTime = 1f;
					}
				}

				if ( currentState != null )
				{
					Image image = animationButtons[ currentState.name ].gameObject.GetComponent<Image>();
					image.fillAmount = currentState.normalizedTime;
					image.color = Color.green;
				}
				if ( lastState != null )
				{
					Image image = animationButtons[ lastState.name ].gameObject.GetComponent<Image>();
					image.fillAmount = 1f;
					image.color = Color.white;
				}

				curFrame = Mathf.FloorToInt( frameCount * currentState.normalizedTime );
			}
			else
			{
				if ( isDrag )
				{
					curFrame = ( int ) animSlider.value;
					AnimationState tempState = null;
					int tempFrame = curFrame;
					int i = -1;
					while ( tempFrame > 0 )
					{
						i++;
						tempState = m_roleAnim[ stateNameList[ i ] ];
						tempFrame -= ( int ) ( tempState.length * tempState.clip.frameRate ) + ( i == 0 ? 0 : 1 );
					}

					if ( i >= 0 )
					{
						tempFrame += ( int ) ( tempState.length * tempState.clip.frameRate );
						tempState = m_roleAnim[ stateNameList[ i ] ];
					}
					else
						tempState = m_roleAnim[ stateNameList[ i + 1 ] ];

					currentState = m_roleAnim[ stateNameList[ i == -1 ? 0 : i ] ];
					m_roleAnim.Play( currentState.name );
					tempState.normalizedTime = tempFrame / ( tempState.length * tempState.clip.frameRate );
				}
				else
				{
					int num = -1;
					for ( int i = 0; i < stateList.Count; i++ )
					{
						if ( m_roleAnim.IsPlaying( stateNameList[ i ] ) )
						{
							currentState = m_roleAnim[ stateNameList[ i ] ];
							int checkFrame = 1;
							for ( int j = 0; j < i; j++ )
								checkFrame += ( int ) ( m_roleAnim[ stateNameList[ j ] ].length * m_roleAnim[ stateNameList[ j ] ].clip.frameRate );
							if ( curFrame < checkFrame )
								PlaySFX( stateNameList[ i ], skillIDDict[ stateNameList[ i ] ], m_SFXLevel );
							num = i;
							break;
						}
					}
					for ( int i = 0; i <= num; i++ )
					{
						AnimationState tempState = ( i == num && stateList[ i ] != null ) ? stateList[ i ] : m_roleAnim[ stateNameList[ i ] ];
						int floor = Mathf.FloorToInt( ( int ) ( tempState.length * tempState.clip.frameRate ) * tempState.normalizedTime );
						int count = ( int ) ( tempState.length * tempState.clip.frameRate );

						if ( i == 0 )
							curFrame = i == num ? floor : count;
						else
							curFrame += ( i == num ? floor : count ) + 1;

						if ( i == num && count - floor == 1 && stateList[ i ] == null && num != stateList.Count - 1 )
							m_roleAnim.Play( stateNameList[ i + 1 ] );
					}

					if ( !m_roleAnim.isPlaying && frameCount - curFrame == 1 )
						curFrame = frameCount;
				}

				if ( playTogetherButtons != null )
				{
					int tempFrame = curFrame;
					for ( int i = 0; i < playTogetherButtons.Length; i++ )
					{
						if ( playTogetherButtons[ i ] != null )
						{
							AnimationState tempState = m_roleAnim[ playTogetherButtons[ i ].name.TrimStart( ' ' ) ];
							if ( "  " + currentState.name == playTogetherButtons[ i ].name && m_roleAnim.isPlaying )
							{
								playTogetherButtons[ i ].gameObject.GetComponent<Image>().color = Color.green;
								playTogetherButtons[ i ].gameObject.GetComponent<Image>().fillAmount = tempFrame / ( currentState.length * currentState.clip.frameRate );
							}
							else
							{
								tempFrame -= ( int ) ( tempState.length * tempState.clip.frameRate ) + ( i == 0 ? 0 : 1 );
								playTogetherButtons[ i ].gameObject.GetComponent<Image>().color = Color.white;
								playTogetherButtons[ i ].gameObject.GetComponent<Image>().fillAmount = 1f;
							}
						}
					}
				}
			}
			animSlider.value = curFrame;
			frameText.text = string.Format( " {0:000}/{1:000}", curFrame, frameCount );
			if ( currentState != null )
				currentStateName = currentState.name;
		}

		void PlaySFX( string sfxName, int skillID, SFXLevel level )
		{
			string tempName = sfxName.Substring( 2, sfxName.IndexOf( '/' ) - 2 );
			if ( m_PrefabSkillID != null )
			{
				for ( int i = 0; i < m_PrefabSkillID.m_Animations.Count; i++ )
				{
					if ( m_PrefabSkillID.m_Animations[ i ] != null )
					{
						if ( m_PrefabSkillID.m_Animations[ i ].name == tempName )
						{
							if ( m_PrefabSkillID.m_SkillIDList[ i ] == skillID && skillID != 0 )
							{
								var skill = Skill.Get( m_PrefabSkillID.m_SkillIDList[ i ] );
								int pos = skill.SkillAttackSfx.IndexOf( '/' ) + 1;
								string name = skill.SkillAttackSfx.Substring( pos, skill.SkillAttackSfx.Length - pos );
								GameObject sfx = null;
								if ( m_SFXPrefabParent == null )
								{
									m_SFXPrefabParent = m_mainRole.transform.Find( "slot_origin" );
									if ( m_SFXPrefabParent == null )
									{
										GameObject go = new GameObject( "slot_origin" );
										go.transform.SetParent( m_mainRole.transform );
										m_SFXPrefabParent = go.transform;
									}
								}
								if ( m_SFXPrefabParent.Find( name ) != null )
									sfx = m_SFXPrefabParent.Find( name ).gameObject;
								if ( sfx == null )
								{
									sfx = AssetDatabase.LoadAssetAtPath<GameObject>( string.Format( "{0}{1}.prefab", m_VFXFolderPath, skill.SkillAttackSfx ) );
									sfx = Instantiate( sfx, m_SFXPrefabParent );
									if ( sfx.GetComponent<VFXffectData>() != null )
										DestroyImmediate( sfx.GetComponent<VFXffectData>() );
									if ( sfx.GetComponent<MonoBehaviourBinderRootData>() != null )
										DestroyImmediate( sfx.GetComponent<MonoBehaviourBinderRootData>() );
									if ( sfx.GetComponent<VFXffect>() != null )
										DestroyImmediate( sfx.GetComponent<VFXffect>() );
									sfx.name = sfx.name.Replace( "(Clone)", "" );
									sfx.transform.localPosition = Vector3.zero;
								}

								if ( sfx.transform.Find( level.ToString() ) != null )
								{
									GameObject levelGo = sfx.transform.Find( level.ToString() ).gameObject;
									ParticleSystem ps = levelGo.GetComponent<ParticleSystem>();
									if ( ps == null )
										ps = levelGo.AddComponent<ParticleSystem>();
									var main = ps.main;
									main.loop = false;
									var emission = ps.emission;
									emission.enabled = false;
									ps.Play();
									if ( lastLevel != null )
									{
										lastLevel.GetComponent<ParticleSystem>().Stop();
									}
									lastLevel = levelGo;
								}
								else
								{
									Debug.LogErrorFormat( "FX \"{0}\" has no {1}.", sfxName, level.ToString() );
								}

								break;
							}
						}
					}
				}
			}
		}

		private void OnDrawGizmos()
		{
			Color c = Gizmos.color;
			try
			{
				Gizmos.color = Color.cyan;
				Gizmos.DrawSphere( m_bornPos, 1f );
				SceneView.RepaintAll();
			}
			finally
			{
				Gizmos.color = c;
			}
		}
	}

	class NoneObjectException : System.Exception
	{
		public NoneObjectException( string message ) : base( message )
		{
		}
	}

}
#endif