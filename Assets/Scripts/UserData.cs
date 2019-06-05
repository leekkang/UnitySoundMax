using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace SoundMax {
    [Serializable]
    public class UserData {
        public List<string> mListIndex = new List<string>();
        public List<MusicSaveData> mListMusicMeta = new List<MusicSaveData>();

        public MusicSaveData GetMusicData(string name) {
            int index = mListIndex.FindIndex((x) => x == name);
            if (index != -1) {
                return mListMusicMeta[index];
            } else {
                mListIndex.Add(name);
                MusicSaveData data = new MusicSaveData();
                mListMusicMeta.Add(data);
                return data;
            }
        }
    }

    [Serializable]
    public class MusicSaveData {
        public int mDifficulty;
        public int mSpeed;
        public CustomList<MusicDifficultySaveData> mListPlayData = new CustomList<MusicDifficultySaveData>();
    }

    [Serializable]
    public class MusicDifficultySaveData {
        public int mClearStatus;
        public int mScore;
        public int mClearRate; // 단위 백분율

        public int mDmaxNum;
        public int mMaxNum;
        public int mMissNum;
        public float mGuage;
    }
    [Serializable]
    public class CustomList<T> : List<T> where T : new() {
        public T this[int index] {
            get {
                if (this.Count <= index) {
                    for (int i = Count; i < index + 1; i++)
                        base.Add(new T());
                }
                return base[index];
            } set {
                if (this.Count <= index) {
                    for (int i = Count; i < index + 1; i++)
                        base.Add(new T());
                }
                base[index] = value;
            }
        }
    }


    public static class SaveDataAdapter {
        /// <summary>
        /// Serialize an object to the devices File System.
        /// </summary>
        /// <param name="objectToSave">The Object that will be Serialized.</param>
        /// <param name="fileName">Name of the file to be Serialized.</param>
        public static void SaveData(object objectToSave, string fileName) {
            // Add the File Path together with the files name and extension.
            // We will use .bin to represent that this is a Binary file.
            string FullFilePath = Application.dataPath + "/" + fileName + ".dat";
            // We must create a new Formattwr to Serialize with.
            BinaryFormatter Formatter = new BinaryFormatter();
            // Create a streaming path to our new file location.
            FileStream fileStream = new FileStream(FullFilePath, FileMode.Create);
            // Serialize the objedt to the File Stream
            Formatter.Serialize(fileStream, objectToSave);
            // FInally Close the FileStream and let the rest wrap itself up.
            fileStream.Close();

            Debug.Log("Save data complete");
        }
        /// <summary>
        /// Deserialize an object from the FileSystem.
        /// </summary>
        /// <param name="fileName">Name of the file to deserialize.</param>
        /// <returns>Deserialized Object</returns>
        public static object LoadData(string fileName) {
            string FullFilePath = Application.dataPath + "/" + fileName + ".dat";
            // Check if our file exists, if it does not, just return a null object.
            if (File.Exists(FullFilePath)) {
                BinaryFormatter Formatter = new BinaryFormatter();
                FileStream fileStream = new FileStream(FullFilePath, FileMode.Open, FileAccess.Read);
                object obj = Formatter.Deserialize(fileStream);
                fileStream.Close();

                Debug.Log("Load data complete : " + FullFilePath);
                // Return the uncast untyped object.
                return obj;
            } else {
                return null;
            }
        }
    }

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver {
        [SerializeField]
        private List<TKey> mListKey = new List<TKey>();
        [SerializeField]
        private List<TValue> mListValue = new List<TValue>();

        // save the dictionary to lists
        public void OnBeforeSerialize() {
            mListKey.Clear();
            mListValue.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in this) {
                mListKey.Add(pair.Key);
                mListValue.Add(pair.Value);
            }
        }

        // load dictionary from lists
        public void OnAfterDeserialize() {
            this.Clear();

            if (mListKey.Count != mListValue.Count)
                throw new System.Exception(string.Format("there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable."));

            for (int i = 0; i < mListKey.Count; i++)
                this.Add(mListKey[i], mListValue[i]);
        }
    }
}