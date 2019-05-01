using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using SoundMax;

public class KShootBlock {
    public List<KShootTick> mListTick = new List<KShootTick>();
}

public class KShootTick {
    public Dictionary<string, string> mDicSetting = new Dictionary<string, string>();
    public string mButtons;
    public string mFx;
    public string mLaser;
    public string mAdd;

    public void Clear() {
        mDicSetting.Clear();
        mButtons = "0000";
        mFx = "00";
        mLaser = "--";
    }
}

public class KShootTime {
    public int mIndexBlock;
    public int mIndexTick;

    public void Reset(int block, int tick) {
        mIndexBlock = block;
        mIndexTick = tick;
    }
}

public class KShootEffectDefinition {
    public string mTypeName;
    public Dictionary<string, string> mParameters = new Dictionary<string, string>();
}

public class DataBase : Singleton<DataBase> {
    public bool mOpenComplete;
    public Texture mDefaultJacket;
    public List<char> mListLaserChar = new List<char>();
    public DefaultEffectSettings mEffectSetting;

    public Dictionary<string, List<MusicData>> mDicMusic = new Dictionary<string, List<MusicData>>();

    public void Open() {
        mOpenComplete = false;
        mDefaultJacket = Resources.Load("Default_Jacket") as Texture;

        for (char c = '0'; c <= '9'; c++)
            mListLaserChar.Add(c);
        for (char c = 'A'; c <= 'Z'; c++)
            mListLaserChar.Add(c);
        for (char c = 'a'; c <= 'o'; c++)
            mListLaserChar.Add(c);

        mEffectSetting = new DefaultEffectSettings();

        StartCoroutine(CoLoadMusicList());
        //LoadAudio("colorfulsky", AudioType.OGGVORBIS, (audio) => { Debug.Log("load complete"); });
    }

    public float TranslateLaserChar(char c) {
        int index = mListLaserChar.FindIndex((x) => x == c);
        if (index == -1) {
            Debug.Log("Invalid Error : unknown character");
            return 0f;
        }
        
        return (float)index / (mListLaserChar.Count - 1);
    }

    public string GetDifficultyPostfix(Difficulty difficulty) {
        if (difficulty == Difficulty.Light)
            return "lt";
        else if(difficulty == Difficulty.Challenge)
            return "ch";
        else if (difficulty == Difficulty.Extended)
            return "ex";
        else if (difficulty == Difficulty.Infinite)
            return "in";

        return null;
    }

    public void LoadAudio(string filename, AudioType extension, Action<AudioClip> onLoadCompleted) {
        string ksh = string.Format("{0}{1}{0}", filename, Path.DirectorySeparatorChar);
        if (extension == AudioType.OGGVORBIS) ksh += ".ogg";
        else if (extension == AudioType.WAV) ksh += ".wav";
        StartCoroutine(CoLoadAudio( Path.Combine(Application.streamingAssetsPath, ksh), extension, onLoadCompleted));
    }

    public bool LoadAudio(string path, Action<AudioClip> onLoadCompleted) {
        if (!File.Exists(path)) {
            Debug.Log("오디오 파일이 없습니다. : " + path);
            return false;
        }
        string type = path.Split('.')[1].ToLower();
        AudioType aType;
        if (type == "ogg")
            aType = AudioType.OGGVORBIS;
        else if (type == "wav")
            aType = AudioType.WAV;
        else if (type == "mp3")
            aType = AudioType.MPEG;
        else {
            Debug.Log("오디오 파일 확장자가 이상합니다. : " + path);
            return false;
        }
        StartCoroutine(CoLoadAudio(path, aType, onLoadCompleted));

        return true;
    }

    IEnumerator CoLoadAudio(string url, AudioType extension, Action<AudioClip> onLoadCompleted) {
        Debug.Log(url);
        using (var request = UnityWebRequestMultimedia.GetAudioClip(url, extension)) {
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError) {
                Debug.LogError(request.error);
                yield break;
            }
            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);

            if (onLoadCompleted != null)
                onLoadCompleted(clip);

            request.Dispose();
        }
    }

    public void LoadImage(string musicname, string filename, Action<Texture> onLoadCompleted) {
        string url = string.Format("{0}{1}{2}", musicname, Path.DirectorySeparatorChar, filename);
        StartCoroutine(CoLoadImage(Path.Combine(Application.streamingAssetsPath, url), onLoadCompleted));
    }

    IEnumerator CoLoadImage(string url, Action<Texture> onLoadCompleted) {
        Debug.Log(url);
        using (var request = UnityWebRequestTexture.GetTexture(url)) {
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError) {
                Debug.LogWarning(request.error + ", so replace default jacket");
                if (onLoadCompleted != null)
                    onLoadCompleted(mDefaultJacket);
                yield break;
            }
            Texture tex = DownloadHandlerTexture.GetContent(request);

            if (onLoadCompleted != null)
                onLoadCompleted(tex);

            request.Dispose();
        }
    }

    IEnumerator CoLoadMusicList() {
        string[] arrMusic = { "colorfulsky", "max_burning", "xross_infection" };
        for (int i = 0; i < arrMusic.Length; i++) {
            List<MusicData> dataList = new List<MusicData>();
            for (Difficulty j = Difficulty.Light; j <= Difficulty.Infinite; j++) {
                bool bComplete = false;
                bool bSuccess = false;
                MusicData data = new MusicData();
                data.Load(arrMusic[i], j, (success) => { bSuccess = success; bComplete = true; });
                yield return new WaitUntil(() => bComplete);
                if (bSuccess)
                    dataList.Add(data);
            }
            mDicMusic.Add(arrMusic[i], dataList);
        }

        for (int i = 0; i < arrMusic.Length; i++) {
            Debug.Log(arrMusic[i] + " : " + mDicMusic[arrMusic[i]].Count);
        }
        mOpenComplete = true;
    }
}

public class MusicData {
    const string BLOCK_SEPARATOR = "--";
    public string mTitle;
    public string mPathName;
    public int mBpm;
    public int mLevel;
    public Difficulty mDifficulty;
    public Texture mJacketImage;

    public KShootTime mTime = new KShootTime();
    public List<KShootBlock> mListBlocks = new List<KShootBlock>();

    public Dictionary<string, string> mDicSettings = new Dictionary<string, string>();
    public Dictionary<string, KShootEffectDefinition> mDicFilterDefine = new Dictionary<string, KShootEffectDefinition>();
    public Dictionary<string, KShootEffectDefinition> mDicFxDefine = new Dictionary<string, KShootEffectDefinition>();

    public void Load(string musicName, Difficulty difficulty, System.Action<bool> afterLoad) {
        mPathName = musicName;
        mDifficulty = difficulty;
        string ksh = string.Format("{0}{1}{0}_{2}.ksh", musicName, Path.DirectorySeparatorChar, DataBase.inst.GetDifficultyPostfix(difficulty));

        mDicSettings.Clear();
        mListBlocks.Clear();

        try {
            string imageName = null;
            using (StreamReader sr = new StreamReader(Application.streamingAssetsPath + Path.DirectorySeparatorChar + ksh)) {
                // setting
                string line = "";
                while (sr.Peek() >= 0) {
                    line = sr.ReadLine().Trim();

                    if (line == BLOCK_SEPARATOR)
                        break;

                    if (string.IsNullOrEmpty(line))
                        continue;

                    if (line.Substring(0, 2).Equals("//"))
                        continue;

                    string[] splitted = line.Split('=');
                    if (splitted.Length > 1) {
                        mDicSettings.Add(splitted[0], splitted[1]);
                        if (splitted[0] == "t")
                            mBpm = int.Parse(splitted[1]);
                        else if (splitted[0] == "title")
                            mTitle = splitted[1];
                        else if (splitted[0] == "level")
                            mLevel = int.Parse(splitted[1]);
                        else if (splitted[0] == "jacket")
                            imageName = splitted[1];
                    }
                }

                // note information
                int n_count = 0;
                KShootBlock block = new KShootBlock();
                KShootTick tick = new KShootTick();
                mTime.Reset(0, 0);
                while (sr.Peek() >= 0) {
                    line = sr.ReadLine().Trim();

                    if (string.IsNullOrEmpty(line))
                        continue;

                    n_count++;
                    // next block create
                    if (line == BLOCK_SEPARATOR) {
                        mListBlocks.Add(block);
                        block = new KShootBlock();
                        mTime.mIndexBlock++;
                        mTime.mIndexTick = 0;
                    } else {
                        if (line.Substring(0, 2).Equals("//"))
                            continue;
                        if (line[0].Equals(";"))
                            continue;

                        if (line[0].Equals("#")) {
                            // TODO : 커스텀 fx 이펙트를 설정할 수 있으나 일단 복잡해서 뺌
                            string[] custom = line.Split(' ');
                            if (custom.Length != 3) {
                                Debug.Log(string.Format("Invalid define found in ksh map @{0}: {1}", n_count, line));
                            }
                            KShootEffectDefinition def = new KShootEffectDefinition();
                            def.mTypeName = custom[1];

                            string[] parameters = custom[2].Split(';');
                            for (int i = 0; i < parameters.Length; i++) {
                                string[] param = parameters[i].Split('=');
                                if (param.Length != 2) {
                                    Debug.Log(string.Format("Invalid parameter in custom effect definition for {0}@{1}: \"{2}\"", def.mTypeName, n_count, line));
                                    continue;
                                }
                                def.mParameters.Add(param[0], param[1]);
                            }

                            if (custom[0] == "#define_fx")
                                mDicFxDefine.Add(def.mTypeName, def);
                            else if (custom[0] == "#define_filter")
                                mDicFilterDefine.Add(def.mTypeName, def);
                            else
                                Debug.Log(string.Format("Unkown define statement in ksh @{0}: {1}", n_count, line));

                            continue;
                        }

                        string[] splitted;
                        if (line.Contains("=")) {
                            splitted = line.Split('=');
                            tick.mDicSetting.Add(splitted[0], splitted[1]);
                            continue;
                        }

                        splitted = line.Split('|');
                        tick.mButtons = splitted[0];
                        tick.mFx = splitted[1];
                        tick.mLaser = splitted[2];
                        if (splitted.Length < 3) {
                            Debug.Log("text format error : note information malformed");
                            return;
                        }
                        if (splitted[0].Length != 4) {
                            Debug.Log("text format error : normal button information malformed");
                            return;
                        }
                        if (splitted[1].Length != 2) {
                            Debug.Log("text format error : fx button information malformed");
                            return;
                        }
                        if (splitted[2].Length < 2) {
                            Debug.Log("text format error : laser information malformed");
                            return;
                        }
                        if (splitted[2].Length > 2) {
                            tick.mAdd = splitted[2].Substring(2);
                            tick.mLaser = splitted[2].Substring(0, 2);
                        }

                        block.mListTick.Add(tick);
                        tick = new KShootTick();
                        mTime.mIndexTick++;
                    }
                }
            }

            if (string.IsNullOrEmpty(imageName)) {
                mJacketImage = DataBase.inst.mDefaultJacket;
                afterLoad(true);
            } else {
                DataBase.inst.LoadImage(musicName, imageName, (texture) => {
                    mJacketImage = texture;
                    afterLoad(true);
                });
            }
        } catch (Exception e) {
            Debug.Log("exception : " + e.Message);
            afterLoad(false);
        }
    }

    public bool NextTime() {
        mTime.mIndexTick++;
        if (mTime.mIndexTick >= GetCurBlock().mListTick.Count) {
            mTime.mIndexTick = 0;
            mTime.mIndexBlock++;
        }

        return GetCurBlock() != null;
    }

    public KShootBlock GetCurBlock() {
        if (mTime.mIndexBlock == -1)
            return null;

        if (mTime.mIndexBlock >= mListBlocks.Count)
            return null;

        return mListBlocks[mTime.mIndexBlock];
    }

    public KShootTick GetCurTick() {
        KShootBlock curBlock = GetCurBlock();
        if (mTime.mIndexTick == -1 || mTime.mIndexTick >= curBlock.mListTick.Count)
            return null;

        return curBlock.mListTick[mTime.mIndexTick];
    }

    public float TimeToFloat() {
        KShootBlock curBlock = GetCurBlock();
        if (curBlock == null)
            return -1f;

        return mTime.mIndexBlock + ((float)mTime.mIndexTick / curBlock.mListTick.Count);
    }
}
