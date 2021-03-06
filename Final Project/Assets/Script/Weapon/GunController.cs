using System.Collections;
using UnityEngine;

public class GunController : MonoBehaviour
{
    // 활성화 여부
    public static bool isActivate = false;

    // 현재 장착된 총
    [SerializeField]
    private Gun currentGun;

    // 연사 속도 계산
    private float currentFireRate;

    // 효과음
    private AudioSource audioSource;

    // 레이저 충돌 정보 받아옴
    private RaycastHit hitInfo;

    // 필요한 컴포넌트
    [SerializeField]
    private Camera theCam;
    private Crosshair theCrosshair;

    // 타격 이펙트
    [SerializeField]
    private GameObject hit_effect_prepab;
    
    // 상태 변수
    private bool isReload = false;
    [HideInInspector]
    private bool isFindSightMode = false; // false 기본 상태

    // 본래 포지션 값
    private Vector3 originPos;
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        theCrosshair = FindObjectOfType<Crosshair>();
        originPos = Vector3.zero;

        
    }
    void Update()
    {
        if (isActivate)
        {
            GunFireRateCalc();
            TryFire();
            TryReload();
            TryFineSight();
        }
    }

    // 연사속도 재계산
    private void GunFireRateCalc()
    {
        if (currentFireRate > 0)
            currentFireRate -= Time.deltaTime; // 1초에 1씩 감소
    
    }

    // 발사 시도
    private void TryFire()
    {
        if(Input.GetButton("Fire1")&& currentFireRate<=0 && !isReload)
        {
            Fire();
        }
    }

    // 발사 전 계산
    private void Fire()
    {
        if (!isReload && !WeaponManager.currentWeaponAnim.GetBool("Run"))
        {
            if (currentGun.currentBulletCount > 0)
                Shoot();
            else
            {
                CancelFineSight();
                StartCoroutine(ReloadCoroutine());
            }
        }
        
    }

    // 발사 후 계산
    private void Shoot()
    {
        theCrosshair.FireAnimation();
        currentGun.currentBulletCount--;
        currentFireRate = currentGun.fireRate; // 연사 속도 재계산
        PlaySE(currentGun.fire_Sound);
        currentGun.muzzleFlash.Play();
        HIt();
        // 총기 반동 코루틴 실행
        StopAllCoroutines(); // while 문이 겹치기 때문에
        StartCoroutine(RetroActionCoroutin());
    }

    // 총알이 발사되는 순간에 명중하는 걸로 구현
    private void HIt()
    {
        if (Physics.Raycast(theCam.transform.position, theCam.transform.forward+
            new Vector3(Random.Range(-theCrosshair.GetAccuracy() - currentGun.accuracy, theCrosshair.GetAccuracy() + currentGun.accuracy), 
                        Random.Range(-theCrosshair.GetAccuracy() - currentGun.accuracy, theCrosshair.GetAccuracy() + currentGun.accuracy),
                        0) 
            ,out hitInfo, currentGun.range))
        {
            GameObject clone = Instantiate(hit_effect_prepab, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
            Destroy(clone, 2f);
        }
    }

    // 재장전 시도
    private void TryReload()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isReload && currentGun.currentBulletCount < currentGun.reloadBulletCount)
        {
            CancelFineSight();
            StartCoroutine(ReloadCoroutine());
        }
    }
    // reload중 shoot 되지 않게 코루틴 작성

    // 재장전 취소
    public void CancelReload()
    {
        if (isReload)
        {
            StopAllCoroutines();
            isReload = false;
        }
    }

    // 재장전
    IEnumerator ReloadCoroutine()
    {
        isReload = true;
        if (currentGun.carryBulletCount > 0)
        {
            currentGun.anim.SetTrigger("Reload");
            currentGun.carryBulletCount += currentGun.currentBulletCount;
            currentGun.currentBulletCount = 0;
            yield return new WaitForSeconds(currentGun.reloadTime);
            if (currentGun.carryBulletCount >= currentGun.reloadBulletCount)
            {
                currentGun.currentBulletCount = currentGun.reloadBulletCount;
                currentGun.carryBulletCount -= currentGun.reloadBulletCount;
            }
            else
            {
                currentGun.currentBulletCount = currentGun.carryBulletCount;
                currentGun.carryBulletCount = 0;
            }
            isReload = false;

        }
        else
        {
            Debug.Log("소유한 총알이 없습니다.");
        }
    }

    // 정조준 시도
    private void TryFineSight()
    {
        if (Input.GetButtonDown("Fire2") && !isReload)
        {
            FineSight();
        }
    }
    
    // 정조준 취소
    public void CancelFineSight()
    {
        if (isFindSightMode)
            FineSight();
    }

    // 정조준 로직 가동
    private void FineSight()
    {
        isFindSightMode = !isFindSightMode;
        currentGun.anim.SetBool("FineSightMode", isFindSightMode);
        theCrosshair.FineSightAnimation(isFindSightMode);

        if (isFindSightMode)
        {
            StopAllCoroutines();
            StartCoroutine(FineSightActivateCoroutine());
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(FineSightDeactivateCoroutine());
        }
    }

    // 정조준 활성화
    IEnumerator FineSightActivateCoroutine()
    {
        while(currentGun.transform.localPosition != currentGun.fineSightOrignPos)
        {
            currentGun.transform.localPosition = Vector3.Lerp(currentGun.transform.localPosition, currentGun.fineSightOrignPos, 0.2f);
            yield return null;
        }
    }

    // 정조준 비활성화
    IEnumerator FineSightDeactivateCoroutine()
    {
        while (currentGun.transform.localPosition != originPos)
        {
            currentGun.transform.localPosition = Vector3.Lerp(currentGun.transform.localPosition, originPos, 0.2f);
            yield return null;
        }
    }

    // 반동 코루틴
    IEnumerator RetroActionCoroutin()
    {
        Vector3 recoilBack = new Vector3(currentGun.retroActionFineSightForce, originPos.y, originPos.z);
        Vector3 retroActionRecoilBack = new Vector3(currentGun.retroActionFineSightForce, currentGun.fineSightOrignPos.y, currentGun.fineSightOrignPos.z);

        if (!isFindSightMode)
        {
            currentGun.transform.localPosition = originPos;
            // 반동 시작
            while(currentGun.transform.localPosition.x<=currentGun.retroActionForce-0.02f){
                currentGun.transform.localPosition = Vector3.Lerp(currentGun.transform.localPosition, recoilBack, 0.4f);
                yield return null;
            }
            // 원위치 시키기
            while (currentGun.transform.localPosition !=originPos)
            {
                currentGun.transform.localPosition = Vector3.Lerp(currentGun.transform.localPosition, originPos, 0.1f);
                yield return null;
            }
        }
        else
        {
            currentGun.transform.localPosition = currentGun.fineSightOrignPos;
            // 반동 시작
            while (currentGun.transform.localPosition.x <= currentGun.retroActionFineSightForce - 0.02f)
            {
                currentGun.transform.localPosition = Vector3.Lerp(currentGun.transform.localPosition, retroActionRecoilBack, 0.4f);
                yield return null;
            }
            // 원위치 시키기
            while (currentGun.transform.localPosition != originPos)
            {
                currentGun.transform.localPosition = Vector3.Lerp(currentGun.transform.localPosition, currentGun.fineSightOrignPos, 0.1f);
                yield return null;
            }
        }
    }

    // 사운드 재생
    private void PlaySE(AudioClip _clip)
    {
        audioSource.clip = _clip;
        audioSource.Play();
    }

    public Gun GetGun()
    {
        return currentGun;
    }

    public bool GetFineSightMode()
    {
        return isFindSightMode;
    }

    public void GunChange(Gun _gun)
    {
        if(WeaponManager.currentWeapon != null)
        {
            WeaponManager.currentWeapon.gameObject.SetActive(false);
        }
        currentGun = _gun;
        WeaponManager.currentWeapon = currentGun.GetComponent<Transform>();
        WeaponManager.currentWeaponAnim = currentGun.anim;

        currentGun.transform.localPosition = Vector3.zero;
        currentGun.gameObject.SetActive(true);
        isActivate = true;
    }
}
