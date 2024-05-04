using System.Linq;
using DG.Tweening;
using UniRx;
using Unity.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UnityProjectScripts
{
    public class StudentInfoScreenUI : MonoBehaviour
    {
        public StudentInfoScreenAccessor Accessor;
        public ReactiveProperty<int> CharId = new ReactiveProperty<int>(0);

        private void Start()
        {
            CharId
                .Where(x => x > 0)
                .Subscribe(UpdateChar);

            CharId.Value = 1001;
        }

        private void UpdateChar(int _id)
        {
            var _data = GameResource.StudentTable[_id];

            // 왼쪽에 위치한 캐릭터 정보와 레벨, 경험치를 표시합니다.
            Accessor.FullshotImage.sprite =
                GameResource.Load<Sprite>($"Student/{_data.Id}", $"Student_Fullshot_{_data.Id}");
            Accessor.FullshotHaloImage.sprite =
                GameResource.Load<Sprite>($"Student/{_data.Id}", $"Student_Fullshot_Halo_{_data.Id}");

            Accessor.NameText.text = _data.Name;

            var _yellowStarPrefab = GameResource.Load<GameObject>("Prefab/UI", "YellowStar");
            Accessor.StarHolder.Children().Destroy();
            for (int i = 0; i < 5; ++i)
                Instantiate(_yellowStarPrefab, Accessor.StarHolder.transform);

            Accessor.AttributeText.text = _data.Attribute.ToStringKr();
            // todo: attribute icon 설정
            // Accessor.AttributeIcon.sprite 

            // todo: level text 설정
            // Accessor.LevelText.text = 
            // todo: exp fill 설정
            // Accessor.ExpBar.fillAmount

            // 캐릭터와 헤일로 애니메이션을 재생합니다.
            var _haloRect = Accessor.FullshotHaloImage.GetComponent<RectTransform>();
            var _fullshotGroup = Accessor.FullshotParent.GetComponent<CanvasGroup>();
            var _fullshotRect = Accessor.FullshotParent.GetComponent<RectTransform>();

            _fullshotRect.anchoredPosition = new Vector2(0f, -20f);
            _fullshotGroup.alpha = 0f;
            _haloRect.sizeDelta = _data.FullshotHaloSize;
            DOTween.Kill(Accessor.FullshotParent);
            DOTween.Sequence()
                .Append(_fullshotGroup.DOFade(1f, 1f))
                .Join(_fullshotRect.DOAnchorPos(Vector3.zero, 1f))
                .OnComplete(() =>
                {
                    _haloRect.DOKill();
                    _haloRect.anchoredPosition = _data.FullshotHaloStartPos;
                    _haloRect.DOAnchorPos(_data.FullshotHaloEndPos, 2f)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo);

                    _fullshotRect.DOKill();
                    _fullshotRect.anchoredPosition = Vector2.zero;
                    _fullshotRect.DOAnchorPos(Vector2.up * 5f, 3f)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo);
                })
                .SetId(Accessor.FullshotParent);

            // 기본 정보 탭의 내용을 표시합니다.
            Accessor.BasicTab_StatInfo_SDFullShot.sprite =
                GameResource.Load<Sprite>($"Student/{_data.Id}", $"Student_SD_Fullshot_{_data.Id}");

            Accessor.BasicTab_StatInfo_StatPage1Value.text =
                $"{_data.DefaultShooting}\n{_data.DefaultMelee}\n{_data.DefaultConstruction}\n{_data.DefaultMining}\n{_data.DefaultCooking}\n{_data.DefaultPlants}";
            Accessor.BasicTab_StatInfo_StatPage2Value.text =
                $"{_data.DefaultAnimals}\n{_data.DefaultCrafting}\n{_data.DefaultArtistic}\n{_data.DefaultMedical}\n{_data.DefaultSocial}\n{_data.DefaultIntellectual}";

            var _skillData = GameResource.SkillTable[_data.SkillId];
            var _skillLevelData = GameResource.SkillLevelTable[(_data.SkillId, 1)]; // temp: 임시적으로 스킬 레벨을 1으로 간주합니다.
            var _skillLevels =
                GameResource.SkillLevelTable.Values
                    .Where(x => x.Id.SkillId == _skillData.Id).ToList();
            var _skillInfoUI = Accessor.BasicTab_ExSkillInfo.GetComponent<ExSkillInfoUI>();
            _skillInfoUI.UpdateUI(_skillData, _skillLevelData, _skillLevelData.Id.Level == _skillLevels.Count - 1,
                false);

            var _weaponData = GameResource.WeaponTable[_data.WeaponId];
            Accessor.BasicTab_WeaponInfo_WeaponTypeText.text = _weaponData.Type.ToString();
            Accessor.BasicTab_WeaponInfo_WeaponImage.sprite =
                GameResource.Load<Sprite>($"Weapon/{_weaponData.Id}", $"Weapon_Icon_{_weaponData.Id}");

            var _blueStarPrefab = GameResource.Load<GameObject>("Prefab/UI", "BlueStar");
            Accessor.BasicTab_WeaponInfo_StarHolder.Children().Destroy();
            for (int i = 0; i < _weaponData.Star; ++i)
                Instantiate(_blueStarPrefab, Accessor.BasicTab_WeaponInfo_StarHolder.transform);

            // 레벨 업 탭의 내용을 표시합니다.
            var _exSkillInfoPrefab = GameResource.Load<GameObject>("Prefab/UI", "ExSkillInfo");
            Accessor.LevelUpTab_ExSkillInfo_ExSkillHolder.Children().Destroy();
            for (int i = 0; i < _skillLevels.Count; i++)
            {
                var _go = Instantiate(_exSkillInfoPrefab, Accessor.LevelUpTab_ExSkillInfo_ExSkillHolder.transform);
                var _accessor = _go.GetComponent<ExSkillInfoUI>();
                bool _isMaxLevel = i == _skillLevels.Count - 1;
                bool _isUnlocked = i > _skillLevelData.Id.Level - 1;
                _accessor.UpdateUI(_skillData, _skillLevels[i], _isMaxLevel, _isUnlocked);
            }

            // 현재 레벨 이후의 스킬들에 잠김 아이콘을 표시합니다.

            // 탭 전환 버튼 이벤트 액션을 설정합니다.
            Accessor.TabButtonBox_BasicTabButton.OnClickAsObservable()
                .Subscribe(_ => SetVisibleTab(0));
            Accessor.TabButtonBox_LevelUpTabButton.OnClickAsObservable()
                .Subscribe(_ => SetVisibleTab(1));

            // 탭의 기본 활성 상태를 설정합니다.
            SetVisibleTab(0);
        }

        private void SetVisibleTab(int _tabIndex)
        {
            ColorUtility.TryParseHtmlString("#D7EAF1", out var _enableButtonColor);
            ColorUtility.TryParseHtmlString("#2F363C", out var _enableTextColor);
            ColorUtility.TryParseHtmlString("#2D4A75", out var _disableButtonColor);
            ColorUtility.TryParseHtmlString("#FFFFFF", out var _disableTextColor);

            Accessor.BasicTab.gameObject.SetActive(false);
            Accessor.LevelUpTab.gameObject.SetActive(false);
            Accessor.TabButtonBox_BasicTabButton.GetComponent<Image>().color = _disableButtonColor;
            Accessor.TabButtonBox_BasicTabButton_Text.color = _disableTextColor;
            Accessor.TabButtonBox_LevelUpTabButton.GetComponent<Image>().color = _disableButtonColor;
            Accessor.TabButtonBox_LevelUpTabButton_Text.color = _disableTextColor;
            Accessor.TabButtonBox_ShinbiTabButton.GetComponent<Image>().color = _disableButtonColor;
            Accessor.TabButtonBox_ShinbiTabButton_Text.color = _disableTextColor;

            switch (_tabIndex)
            {
                case 0:
                    Accessor.BasicTab.gameObject.SetActive(true);
                    Accessor.TabButtonBox_BasicTabButton.GetComponent<Image>().color = _enableButtonColor;
                    Accessor.TabButtonBox_BasicTabButton_Text.color = _enableTextColor;
                    break;
                case 1:
                    Accessor.LevelUpTab.gameObject.SetActive(true);
                    Accessor.TabButtonBox_LevelUpTabButton.GetComponent<Image>().color = _enableButtonColor;
                    Accessor.TabButtonBox_LevelUpTabButton_Text.color = _enableTextColor;
                    break;
            }
        }
    }
}