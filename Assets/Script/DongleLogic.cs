using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DongleLogic : MonoBehaviour
{
    public int level;
    public bool isDrag;
    public bool isMerge;
    public bool isAttach;

    public Rigidbody2D rigid;
    Animator anim;
    CircleCollider2D circle;
    SpriteRenderer spriteRenderer;

    public GameManager manager;
    public ParticleSystem effect;

    float deadTime;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        circle = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        anim.SetInteger("Level", level);    
    }

    void OnDisable()
    {
        //동글 속성 초기화
        level = 0;
        isDrag = false;
        isMerge = false;
        isAttach = false;

        //동글 좌표 초기화
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.zero;

        //동글 물리 초기화
        rigid.simulated = false;
        rigid.velocity = Vector2.zero;
        rigid.angularVelocity = 0;
        circle.enabled = true;
    }

    void Update()
    {
        //드래그인 상태라면
        if(isDrag)
        {
            //마우스를 클릭한 위치의 스크린 좌표를 월드 좌표로 변환
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            //x축 경계 설정
            //벽을 넘어가지 않게 하기 위해서 보더 크기 지정 localScale은 동글 오브젝트의 크기 , / 2f는 반지름 계산
            float leftBorder = -4.2f + transform.localScale.x / 2f;
            float rightBorder = 4.2f - transform.localScale.x / 2f;

            //마우스의 x축이 왼쪽 보더 보다 작다면
            if (mousePos.x < leftBorder)
            {
                //x축을 왼쪽 보더로 고정
                mousePos.x = leftBorder;
            }
            //마우스의 x축이 오른쪽 보더 보다 크다면
            else if (mousePos.x > rightBorder)
            {
                //x축을 오른쪽 보더로 고정
                mousePos.x = rightBorder;
            }


            //마우스 y축도 고정으로해서 동글 오브젝트 위치 고정
            mousePos.y = 8;
            //마우스의 z 좌표는 0으로 고정(고정하지 않으면 카메라 뒤에서 움직임)
            mousePos.z = 0;
            //Lerp 메소드를 사용해 마우스를 따라가는 모션을 부드럽게 
            transform.position = Vector3.Lerp(transform.position, mousePos, 0.2f);
        }        
    }

    public void Drag()
    {
        isDrag = true;
    }

    public void Drop()
    {
        isDrag = false;
        rigid.simulated = true;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        StartCoroutine("AttachRoutine");
    }

    IEnumerator AttachRoutine()
    {
        if(isAttach)
        {
            yield break;
        }

        isAttach = true;
        manager.SfxPlay(GameManager.SFx.Attach);

        yield return new WaitForSeconds(0.2f);

        isAttach = false;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Dongle")
        {
            DongleLogic other = collision.gameObject.GetComponent<DongleLogic>();

            if(level == other.level && !isMerge && !other.isMerge && level < 7)
            {
                //나와 상대편 위치 가져오기
                float meX = transform.position.x;
                float meY = transform.position.y;
                float otherX = other.transform.position.x;
                float otherY = other.transform.position.y;
                //1. 내가 아래에 있을 때
                //2. 동일한 높이일 때, 내가 오른쪽에 있을 때
                if(meY < otherY || (meY == otherY && meX > otherX))
                {
                    //상대방은 숨기기
                    other.Hide(transform.position);

                    //나는 레벨업
                    LevelUp();
                }
            }
        }    
    }

    public void Hide(Vector3 targetPos)
    {
        isMerge = true;

        rigid.simulated = false;
        circle.enabled = false;

        if (targetPos == Vector3.up * 100)
            effect.Play();

        StartCoroutine(HideRoutine(targetPos));
    }

    IEnumerator HideRoutine(Vector3 targetPos)
    {
        int frameCount = 0;

        while(frameCount < 20)
        {
            frameCount++;
            if(targetPos != Vector3.up * 100)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, 0.5f);
            } else if (targetPos == Vector3.up * 100)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 0.2f);
            }

                yield return null;
        }

        manager.score += (int) Mathf.Pow(2, level);

        isMerge = false;
        gameObject.SetActive(false);
    }

    void LevelUp()
    {
        isMerge = true;

        //속도 제어
        rigid.velocity = Vector2.zero;
        //회전 제어
        rigid.angularVelocity = 0;

        StartCoroutine(LevelUpRoutine());
    }

    IEnumerator LevelUpRoutine()
    {
        yield return new WaitForSeconds(0.2f);

        anim.SetInteger("Level", level + 1);
        EffectPlay();
        manager.SfxPlay(GameManager.SFx.LevelUp);

        yield return new WaitForSeconds(0.3f);
        level++;

        //레벨 비교 후 큰 수를 반환
        manager.maxLevel = Mathf.Max(level, manager.maxLevel);

        isMerge = false;
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.tag == "Finish")
        {
            deadTime += Time.deltaTime;

            if(deadTime > 2)
            {
                spriteRenderer.color = new Color(0.9f, 0.2f, 0.2f);
            }
            if(deadTime > 5)
            {
                manager.GameOver();
            }
        }    
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Finish")
        {
            deadTime = 0;
            spriteRenderer.color = Color.white;
        }
    }

    void EffectPlay()
    {
        effect.transform.position = transform.position;
        effect.transform.localScale = transform.localScale;
        effect.Play();
    }
}
