using System;

[Serializable]
public class SettingData
{
    public float bgmVolume;
    public float sfxVolume;
    public float monsterSfxVolume;

    public bool shakeOn;
    public bool motionTrailOn;
    public bool camNorthFix;
    public SettingData()
    {
        bgmVolume = 0.5f;
        sfxVolume = 0.5f;
        monsterSfxVolume = 0.5f;
        shakeOn = true;
        motionTrailOn = true;
        camNorthFix = true;
    }
}