using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("------------------[ Core ]")]
    public bool isOver;
    public int score;
    public int maxLevel;

    [Header("------------------[ Object Pooling ]")]
    public GameObject donglePrefab;
    public Transform dongleGroup;
    public List<DongleLogic> donglePool;   
    public GameObject effectPrefab;
    public Transform effectGroup;
    public List<ParticleSystem> effectPool;
    [Range(1, 30)]
    public int poolSize;
    public int poolCursor;
    public DongleLogic lastDongle;

    [Header("------------------[ Audio ]")]
    public AudioSource bgmPlayer;
    public AudioSource[] sfxPlayer;
    public AudioClip[] sfxClip;
    public enum SFx { LevelUp, Next, Attach, Button, Over };
    int sfxCursor;

    [Header("------------------[ UI ]")]
    public GameObject startGroup;
    public GameObject endGroup;
    public Text scoreText;
    public Text maxScoreText;  
    public Text subScoreText;

    [Header("------------------[ ETC ]")]
    public GameObject line;
    public GameObject bottom;

    void Awake()
    {
        //��� �÷��������� �� ���������� ����
        Application.targetFrameRate = 60;

        donglePool = new List<DongleLogic>();
        effectPool = new List<ParticleSystem>();
        for (int index = 0; index < poolSize; index++)
        {
            MakeDongle();
        }

        if(!PlayerPrefs.HasKey("HighScore"))
        {
            PlayerPrefs.SetInt("HighScore", 0);
        }

        maxScoreText.text = PlayerPrefs.GetInt("HighScore").ToString();

    }

    public void GameStart()
    {
        // ������Ʈ Ȱ��ȭ
        line.SetActive(true);
        bottom.SetActive(true);
        scoreText.gameObject.SetActive(true);
        maxScoreText.gameObject.SetActive(true);
        startGroup.SetActive(false);

        //���� �÷���
        bgmPlayer.Play();
        SfxPlay(SFx.Button);

        //���� ����
        Invoke("NextDongle", 1.5f);
    }

    DongleLogic MakeDongle()
    {
        //����Ʈ ����
        GameObject instantEffectObj = Instantiate(effectPrefab, effectGroup);
        instantEffectObj.name = "Effect " + effectPool.Count;
        ParticleSystem instantEffect = instantEffectObj.GetComponent<ParticleSystem>();
        effectPool.Add(instantEffect);

        //���� ���� ������ & ��ġ
        GameObject instantDongleObj = Instantiate(donglePrefab, dongleGroup);
        instantDongleObj.name = "Dongle " + donglePool.Count;
        DongleLogic instantDongle = instantDongleObj.GetComponent<DongleLogic>();
        instantDongle.manager = this;
        instantDongle.effect = instantEffect;
        donglePool.Add(instantDongle);

        return instantDongle;
    }

    DongleLogic GetDongle()
    {
        for (int index = 0; index < donglePool.Count; index++)
        {
            poolCursor = (poolCursor + 1) % donglePool.Count;
            if (!donglePool[poolCursor].gameObject.activeSelf)
            {
                return donglePool[poolCursor];
            }
        }
        return MakeDongle();
    }

    void NextDongle()
    {
        if (isOver)
        {
            return;
        }

        lastDongle = GetDongle();
        //�������� ����
        lastDongle.level = Random.Range(0, maxLevel);
        lastDongle.gameObject.SetActive(true);

        SfxPlay(SFx.Next);

        //�ڷ�ƾ ����
        StartCoroutine(WaitNext());
    }

    IEnumerator WaitNext()
    {
        //���� ��ġ�е忡�� ���� ���� �ʾ��� ��� while �� ����
        while(lastDongle != null)
        {
            yield return null;
        }

        //2.5�� �� �Ʒ� ���� ����
        yield return new WaitForSeconds(2.5f);

        NextDongle();
    }
    

    public void TouchDown()
    {
        if (lastDongle == null)
            return;

        lastDongle.Drag();
    }

    public void TouchUp()
    {
        if (lastDongle == null)
            return;

        lastDongle.Drop();
        lastDongle = null;
    }

    public void GameOver()
    {
        if(isOver)
        {
            return;
        }

        isOver = true;

        StartCoroutine("GameOverRoutine");
    }

    IEnumerator GameOverRoutine()
    {
        // 1. ��� �ȿ� Ȱ��ȭ �Ǿ��ִ� ��� ���� ��������
        DongleLogic[] dongles = FindObjectsOfType<DongleLogic>();

        // 2. ����� ���� ��� ���� ����ȿ�� ��Ȱ��ȭ(���� �� ��ġ�� ��Ȱ��ȭ)
        for (int index = 0; index < dongles.Length; index++)
        {
            dongles[index].rigid.simulated = false;          
        }

        // 3. 1�� ����� �ϳ��� �����ؼ� �����
        for (int index = 0; index < dongles.Length; index++)
        {
            dongles[index].Hide(Vector3.up * 100);
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(1f);

        //�ְ� ���� ����
        int highScore = Mathf.Max(score, PlayerPrefs.GetInt("HighScore"));
        PlayerPrefs.SetInt("HighScore", highScore);

        //���� ���� UI ǥ��
        subScoreText.text = "���� : " + scoreText.text;
        endGroup.SetActive(true);

        bgmPlayer.Stop();
        SfxPlay(SFx.Over);
    }

    public void Reset()
    {
        SfxPlay(SFx.Button);
        StartCoroutine("ResetCoroutine");
    }

    IEnumerator ResetCoroutine()
    {
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("Main");
    }

    public void SfxPlay(SFx type)
    {
        switch(type)
        {
            case SFx.LevelUp:
                sfxPlayer[sfxCursor].clip = sfxClip[Random.Range(0, 3)];
                break;
            case SFx.Next:
                sfxPlayer[sfxCursor].clip = sfxClip[3];
                break;
            case SFx.Attach:
                sfxPlayer[sfxCursor].clip = sfxClip[4];
                break;
            case SFx.Button:
                sfxPlayer[sfxCursor].clip = sfxClip[5];
                break;
            case SFx.Over:
                sfxPlayer[sfxCursor].clip = sfxClip[6];
                break;
        }

        sfxPlayer[sfxCursor].Play();
        sfxCursor = (sfxCursor + 1) % sfxPlayer.Length;
    }

    void Update()
    {
        if(Input.GetButtonDown("Cancel"))
        {
            Application.Quit();
        }   
    }

    void LateUpdate()
    {
        scoreText.text = score.ToString();
    }
}

