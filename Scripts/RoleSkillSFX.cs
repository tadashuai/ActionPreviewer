#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ActionPreviewer
{
	//[CreateAssetMenu]
	[Serializable]
	public class RoleSkillSFX : ScriptableObject
	{
		public List<PrefabSkillID> m_RoleSkill = new List<PrefabSkillID>();

		[Serializable]
		public class PrefabSkillID
		{
			public bool isFoldout = false;
			public GameObject m_RolePrefab = null;
			public List<AnimationClip> m_Animations = new List<AnimationClip>();
			public List<int> m_SkillIDList = new List<int>();
			public int m_InitCount = 0; //生成时有效Clip的个数，避免计入空Clip
		}

		internal void CheckListCount<T>( List<T> list, int count, int minCount = 0 )
		{
			if ( list != null )
			{
				int listCount = list.Count;
				if ( count < minCount )
					count = minCount;

				if ( count == listCount )
					return;
				else if ( count > listCount )
					list.AddRange( new T[ count - listCount ] );
				else
					list.RemoveRange( count, listCount - count );
			}
		}

		internal bool Contains( GameObject prefab )
		{
			for ( int i = 0; i < m_RoleSkill.Count; i++ )
				if ( m_RoleSkill[ i ].m_RolePrefab.name == prefab.name )
					return true;

			return false;
		}
	}
}
#endif