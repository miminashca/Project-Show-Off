using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private AudioMixer SFXMixer;
    [SerializeField] private AudioMixer MusicMixer;
    [SerializeField] private AudioMixer VoiceMixer;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider voiceSlider;

    private void Start()
    {
        if (PlayerPrefs.HasKey("musicVolume"))
        {
            LoadVolume();
        }
        else
        {
            setMusicVolume();
        }
        if (PlayerPrefs.HasKey("sfxVolume"))
        {
            LoadVolume();
        }
        else
        {
           setSFXVolume();           
        }
        if (PlayerPrefs.HasKey("voiceVolume"))
        {
            LoadVolume();
        }
        else
        {
           setVoiceVolume();           
        }
       
    }
    public void setMusicVolume()
    {
        float musicVolume = musicSlider.value;
        MusicMixer.SetFloat("music", Mathf.Log10(musicVolume)*20);
        PlayerPrefs.SetFloat("musicVolume", musicVolume); //store the slider info inetween scenes and whatnot
    }
    public void setSFXVolume()
    {
        float sfxVolume = sfxSlider.value;
        SFXMixer.SetFloat("sfx", Mathf.Log10(sfxVolume) * 20);
        PlayerPrefs.SetFloat("sfxVolume", sfxVolume);
    }
    public void setVoiceVolume()
    {
        float voiceVolume = voiceSlider.value;
        VoiceMixer.SetFloat("voice", Mathf.Log10(voiceVolume)*20);
        PlayerPrefs.SetFloat("voiceVolume", voiceVolume);
    }
    private void LoadVolume()
    {
        musicSlider.value = PlayerPrefs.GetFloat("musicVolume"); //get the stored val
        setMusicVolume();
        sfxSlider.value = PlayerPrefs.GetFloat("sfxVolume");
        setSFXVolume();
        voiceSlider.value = PlayerPrefs.GetFloat("voiceVolume");
    }
}
