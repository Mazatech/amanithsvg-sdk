/****************************************************************************
** Copyright (c) 2013-2023 Mazatech S.r.l.
** All rights reserved.
** 
** Redistribution and use in source and binary forms, with or without
** modification, are permitted (subject to the limitations in the disclaimer
** below) provided that the following conditions are met:
** 
** - Redistributions of source code must retain the above copyright notice,
**   this list of conditions and the following disclaimer.
** 
** - Redistributions in binary form must reproduce the above copyright notice,
**   this list of conditions and the following disclaimer in the documentation
**   and/or other materials provided with the distribution.
** 
** - Neither the name of Mazatech S.r.l. nor the names of its contributors
**   may be used to endorse or promote products derived from this software
**   without specific prior written permission.
** 
** NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED
** BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
** CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT
** NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
** A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER
** OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
** EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
** PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
** OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
** WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
** OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
** ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
** 
** For any information, please contact info@mazatech.com
** 
****************************************************************************/

#if UNITY_EDITOR || UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE || UNITY_WII || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID || UNITY_PS4 || UNITY_XBOXONE || UNITY_TIZEN || UNITY_TVOS || UNITY_WSA || UNITY_WSA_10_0 || UNITY_WINRT || UNITY_WINRT_10_0 || UNITY_WEBGL || UNITY_FACEBOOK || UNITY_ADS || UNITY_ANALYTICS
    #define UNITY_ENGINE
#endif

#if UNITY_2_6
    #define UNITY_2_X
    #define UNITY_2_PLUS
#elif UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5
    #define UNITY_3_X
    #define UNITY_2_PLUS
    #define UNITY_3_PLUS
#elif UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
    #define UNITY_4_X
    #define UNITY_2_PLUS
    #define UNITY_3_PLUS
    #define UNITY_4_PLUS
#elif UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_4_OR_NEWER
    #define UNITY_5_X
    #define UNITY_2_PLUS
    #define UNITY_3_PLUS
    #define UNITY_4_PLUS
    #define UNITY_5_PLUS
#endif
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_ENGINE
    using UnityEngine;
    #if UNITY_EDITOR
        using UnityEditor;
        using UnityEngine.SceneManagement;
        using UnityEditor.SceneManagement;
#endif
#endif // UNITY_ENGINE

#if UNITY_ENGINE

namespace SVGAssets
{
    public class Pair<F, S>
    {
        public Pair(F first, S second)
        {
            First = first;
            Second = second;
        }

        public F First { get; }

        public S Second { get; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj == this)
            {
                return true;
            }

            Pair<F, S> other = obj as Pair<F, S>;
            if (other == null)
            {
                return false;
            }
        
            return (((First == null)  && (other.First == null))  || ((First != null)  && First.Equals(other.First))) &&
                   (((Second == null) && (other.Second == null)) || ((Second != null) && Second.Equals(other.Second)));
        }
    
        public override int GetHashCode()
        {
            int hashcode = 0;

            if (First != null)
            {
                hashcode += First.GetHashCode();
            }
            if (Second != null)
            {
                hashcode += Second.GetHashCode();
            }
        
            return hashcode;
        }
    }

    public class Triplet<F, S, T>
    {
        public Triplet(F first, S second, T third)
        {
            First = first;
            Second = second;
            Third = third;
        }

        public F First { get; }

        public S Second { get; }

        public T Third { get; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj == this)
            {
                return true;
            }

            Triplet<F, S, T> other = obj as Triplet<F, S, T>;
            if (other == null)
            {
                return false;
            }

            return (((First == null) && (other.First == null)) || ((First != null) && First.Equals(other.First))) &&
                   (((Second == null) && (other.Second == null)) || ((Second != null) && Second.Equals(other.Second))) &&
                   (((Third == null) && (other.Third == null)) || ((Third != null) && Third.Equals(other.Third)));
        }

        public override int GetHashCode()
        {
            int hashcode = 0;

            if (First != null)
            {
                hashcode += First.GetHashCode();
            }
            if (Second != null)
            {
                hashcode += Second.GetHashCode();
            }
            if (Third != null)
            {
                hashcode += Third.GetHashCode();
            }

            return hashcode;
        }
    }

    // A serializable dictionary template
    [Serializable]
    public class SerializableDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
    {
        public V this[K key]
        {
            get
            {
                if (!m_DictionaryRestored)
                {
                    RestoreDictionary();
                }
                return m_ValuesList[m_Dictionary[key]];
            }
            set
            {
                if (!m_DictionaryRestored)
                {
                    RestoreDictionary();
                }
            
                int index;
                if (m_Dictionary.TryGetValue(key, out index))
                {
                    m_ValuesList[index] = value;
                }
                else
                {
                    Add(key, value);
                }
            }
        }
    
        public void Add(K key, V value)
        {
            m_Dictionary.Add(key, m_ValuesList.Count);
            m_KeysList.Add(key);
            m_ValuesList.Add(value);
        }
    
        public int Count
        {
            get
            {
                return m_ValuesList.Count;
            }
        }
    
    #region IEnumerable<KeyValuePair<K,V>> Members
        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return new Enumerator(this);
        }
    
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }
    #endregion

        public V Get(K key, V default_value)
        {
            if (!m_DictionaryRestored)
            {
                RestoreDictionary();
            }
        
            int index;
            if (m_Dictionary.TryGetValue(key, out index))
            {
                return m_ValuesList[index];
            }
            else
            {
                return default_value;
            }
        }
    
        public bool TryGetValue(K key, out V value)
        {
            if (!m_DictionaryRestored)
            {
                RestoreDictionary();
            }
        
            if (m_Dictionary.TryGetValue(key, out int index))
            {
                value = m_ValuesList[index];
                return true;
            }
            else {
                value = default(V);
                return false;
            }
        }
    
        public bool Remove(K key)
        {
            if (!m_DictionaryRestored)
            {
                RestoreDictionary();
            }
        
            if (m_Dictionary.TryGetValue(key, out int index))
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }
    
        public void RemoveAt(int index)
        {
            if (!m_DictionaryRestored)
            {
                RestoreDictionary();
            }
        
            K key = m_KeysList[index];
            m_Dictionary.Remove(key);
            m_KeysList.RemoveAt(index);
            m_ValuesList.RemoveAt(index);
        
            for (int k = index; k < m_KeysList.Count; ++k)
            {
                --m_Dictionary[m_KeysList[k]];
            }
        }
    
        public KeyValuePair<K, V> GetAt(int index)
        {
            return new KeyValuePair<K, V>(m_KeysList[index], m_ValuesList[index]);
        }
    
        public V GetValueAt(int index)
        {
            return m_ValuesList[index];
        }
    
        public bool ContainsKey(K key)
        {
            if (!m_DictionaryRestored)
            {
                RestoreDictionary();
            }
            return m_Dictionary.ContainsKey(key);
        }
    
        public void Clear()
        {
            m_Dictionary.Clear();
            m_KeysList.Clear();
            m_ValuesList.Clear();
        }
    
        public List<V> Values()
        {
            return(new List<V>(m_ValuesList));
        }
    
        private void RestoreDictionary()
        {
            for (int i = 0 ; i < m_KeysList.Count; ++i)
            {
                m_Dictionary[m_KeysList[i]] = i;
            }
            m_DictionaryRestored = true;
        }
    
        private Dictionary<K, int> m_Dictionary = new Dictionary<K, int>();
        [SerializeField]
        private List<K> m_KeysList = new List<K>();
        [SerializeField]
        private List<V> m_ValuesList = new List<V>();
        [NonSerialized]
        private bool m_DictionaryRestored = false;
    
    #region Nested type: Enumerator
        private class Enumerator : IEnumerator<KeyValuePair<K, V>>
        {
            public Enumerator(SerializableDictionary<K, V> dictionary)
            {
                m_Dictionary = dictionary;
            }
        
        #region IEnumerator<KeyValuePair<K,V>> Members
            public KeyValuePair<K, V> Current
            {
                get
                {
                    return m_Dictionary.GetAt(m_Current);
                }
            }
        
            public void Dispose()
            {
            }
        
            object IEnumerator.Current
            {
                get
                {
                    return m_Dictionary.GetAt(m_Current);
                }
            }
        
            public bool MoveNext()
            {
                ++m_Current;
                return m_Current < m_Dictionary.Count;
            }
        
            public void Reset()
            {
                m_Current = -1;
            }
        #endregion

            private readonly SerializableDictionary<K, V> m_Dictionary;
            private int m_Current = -1;
        }
    #endregion
    }

    #endif // UNITY_ENGINE

    static public class SVGUtils
    {
    #if UNITY_ENGINE
        static public Vector2 GetGameView()
        {
        #if UNITY_EDITOR
            return Handles.GetMainGameViewSize();
        #else
            return Vector2.zero;
        #endif
        }
    #endif // UNITY_ENGINE

        // Given an unsigned value greater than 0, check if it's a power of two number.
        static public bool IsPow2(uint value)
        {
            return (((value & (value - 1)) == 0) ? true : false);
        }

        // Return the smallest power of two greater than (or equal to) the specified value.
        static public uint Pow2Get(uint value)
        {
            uint v = 1;

            while (v < value)
            {
                v <<= 1;
            }
            return v;
        }

        static public int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        static public bool IsFloatValid(float v)
        {
            return ((!float.IsNaN(v)) && (!float.IsInfinity(v)));
        }

        static public float FixFloat(float v)
        {
            return IsFloatValid(v) ? v : 0;
        }

    #if UNITY_ENGINE

        static public Vector2 FixVector2(Vector2 v)
        {
            return new Vector2(FixFloat(v.x), FixFloat(v.y));
        }

        static public Vector2 FixVector3(Vector3 v)
        {
            return new Vector3(FixFloat(v.x), FixFloat(v.y), FixFloat(v.z));
        }

        static public Vector4 FixVector4(Vector4 v)
        {
            return new Vector4(FixFloat(v.x), FixFloat(v.y), FixFloat(v.z), FixFloat(v.w));
        }

    #if UNITY_EDITOR

        static public void MarkSceneDirty()
        {
        #if UNITY_5_X
            #if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3
                EditorApplication.MarkSceneDirty();
            #else
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            #endif
        #else
            EditorApplication.MarkSceneDirty();
        #endif
        }

        static public void MarkObjectDirty(UnityEngine.Object obj)
        {
            EditorUtility.SetDirty(obj);
        }

        // Generate a 1x1 texture, with the given color.
        static public Texture2D ColorTexture(Color32 color)
        {
            Color32[] pixels = new Color32[1] { color };
            Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false)
            {
                // we take care to destroy the texture, when it will be the moment
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

    #endif // UNITY_EDITOR

    #endif // UNITY_ENGINE
    }
}
