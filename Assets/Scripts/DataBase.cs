using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;

public class Blocks {
    public List<Tick> mListTick = new List<Tick>();
}

public class Tick {
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

public class TimeIndex {
    public int mIndexBlock;
    public int mIndexTick;

    public TimeIndex(int block = -1, int tick = -1) {
        mIndexBlock = block;
        mIndexTick = tick;
    }
}

public enum Difficulty {
    Light,
    Challenge,
    Extended,
    Infinite
}

public class DataBase : Singleton<DataBase> {

    public List<char> mListLaserChar = new List<char>();

    public void Open() {
        for (char c = '0'; c <= '9'; c++)
            mListLaserChar.Add(c);
        for (char c = 'A'; c <= 'Z'; c++)
            mListLaserChar.Add(c);
        for (char c = 'a'; c <= 'o'; c++)
            mListLaserChar.Add(c);

        LoadAudio("colorfulsky", AudioType.OGGVORBIS, (audio) => { Debug.Log("load complete"); });
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
}

public class MusicData {
    const string BLOCK_SEPARATOR = "--";
    public string mName;
    public int mBpm;

    TimeIndex mTime;
    List<Blocks> mListBlocks;
    Dictionary<string, string> mDicSettings = new Dictionary<string, string>();

    public void Load(string musicName, Difficulty difficulty) {
        mDicSettings.Clear();
        mListBlocks.Clear();

        string ksh = string.Format("{0}{1}{0}_{2}.ksh", musicName, Path.DirectorySeparatorChar, DataBase.inst.GetDifficultyPostfix(difficulty));
        mListBlocks = new List<Blocks>();

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
                }
            }

            // note information
            Blocks block = new Blocks();
            Tick tick = new Tick();
            mTime = new TimeIndex(0, 0);
            while (sr.Peek() >= 0) {
                line = sr.ReadLine().Trim();

                if (string.IsNullOrEmpty(line))
                    continue;

                // next block create
                if (line == BLOCK_SEPARATOR) {
                    mListBlocks.Add(block);
                    block = new Blocks();
                    mTime.mIndexBlock++;
                    mTime.mIndexTick = 0;
                } else {
                    if (line.Substring(0, 2).Equals("//"))
                        continue;
                    if (line[0].Equals(";"))
                        continue;

                    if (line[0].Equals("#")) {
                        // 커스텀 fx 이펙트를 설정할 수 있으나 일단 복잡해서 뺌
                        continue;
                    }

                    string[] splitted;
                    if(line.Contains("=")) {
                        splitted = line.Split('=');
                        tick.mDicSetting.Add(splitted[0], splitted[1]);
                        continue;
                    }

                    splitted = line.Split('|');
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
                    tick = new Tick();
                    mTime.mIndexTick++;
                }
            }
        }
    }

    bool NextTime() {
        mTime.mIndexTick++;
        if (mTime.mIndexTick >= GetCurBlock().mListTick.Count) {
            mTime.mIndexTick = 0;
            mTime.mIndexBlock++;
        }

        return GetCurBlock() != null;
    }

    public Blocks GetCurBlock() {
        if (mTime.mIndexBlock == -1)
            return null;

        if (mTime.mIndexBlock >= mListBlocks.Count)
            return null;

        return mListBlocks[mTime.mIndexBlock];
    }

    public Tick GetCurTick() {
        Blocks curBlock = GetCurBlock();
        if (mTime.mIndexTick == -1 || mTime.mIndexTick >= curBlock.mListTick.Count)
            return null;

        return curBlock.mListTick[mTime.mIndexTick];
    }

    public float TimeToFloat(Time time) {
        Blocks curBlock = GetCurBlock();
        if (curBlock == null)
            return -1f;

        return (float)mTime.mIndexBlock + (mTime.mIndexTick / curBlock.mListTick.Count);
    }
}
