using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MissionDescModule : UIModule
{
    public Text missionName;

    public Text wyrdstones;

    public Text treasures;

    public Text description;

    public Text rating;

    public Text actText;

    public Text difficulty;

    public Image diffIcon;

    public Image icon;

    public GameObject resources;

    public GameObject ratingSection;

    public void Setup(Mission miss)
    {
        if (miss.missionSave.isCampaign)
        {
            List<CampaignMissionData> list = PandoraSingleton<DataFactory>.Instance.InitData<CampaignMissionData>(new string[2]
            {
                "fk_warband_id",
                "idx"
            }, new string[2]
            {
                ((int)PandoraSingleton<HideoutManager>.Instance.WarbandCtrlr.Warband.Id).ToString(),
                PandoraSingleton<HideoutManager>.Instance.WarbandCtrlr.Warband.GetWarbandSave().curCampaignIdx.ToString()
            });
            missionName.set_text(PandoraSingleton<LocalizationManager>.Instance.GetStringById("mission_camp_title_" + list[0].Name));
            description.set_text(PandoraSingleton<LocalizationManager>.Instance.GetStringById("mission_camp_announcement_short_" + list[0].Name));
            actText.set_text(PandoraSingleton<LocalizationManager>.Instance.GetStringById("act_" + PandoraSingleton<HideoutManager>.Instance.WarbandCtrlr.Warband.GetWarbandSave().curCampaignIdx));
            resources.SetActive(value: false);
            ratingSection.SetActive(value: true);
            rating.set_text(miss.missionSave.rating.ToString());
        }
        else
        {
            missionName.set_text(PandoraSingleton<LocalizationManager>.Instance.GetStringById("lobby_title_" + miss.GetDeploymentScenarioId().ToLowerString()));
            actText.set_text(PandoraSingleton<LocalizationManager>.Instance.GetStringById("lobby_title_" + ((PrimaryObjectiveTypeId)miss.missionSave.objectiveTypeIds[0]).ToString()));
            resources.SetActive(value: true);
            ratingSection.SetActive(value: false);
            description.set_text(PandoraSingleton<LocalizationManager>.Instance.GetStringById("lobby_desc_" + miss.GetDeploymentScenarioId().ToLowerString()));
            if (miss.GetMapId() == MissionMapId.GRND_DIS_02_PROC_08)
            {
                description.set_text(description.get_text() + " District:  Noble Quarters 4");
            }
            if (miss.GetMapId() == MissionMapId.GRND_DIS_02_PROC_07)
            {
                description.set_text(description.get_text() + " District:  Noble Quarters 3");
            }
            if (miss.GetMapId() == MissionMapId.GRND_DIS_02_PROC_06)
            {
                description.set_text(description.get_text() + " District:  Noble Quarters 2");
            }
            if (miss.GetMapId() == MissionMapId.GRND_DIS_02_PROC_05)
            {
                description.set_text(description.get_text() + " District:  Noble Quarters 1");
            }
            if (miss.GetMapId() == MissionMapId.GRND_DIS_01_PROC_01)
            {
                description.set_text(description.get_text() + " District: Merchant Quarters 1");
            }
            if (miss.GetMapId() == MissionMapId.GRND_DIS_01_PROC_02)
            {
                description.set_text(description.get_text() + " District: Merchant Quarters 2");
            }
            if (miss.GetMapId() == MissionMapId.GRND_DIS_01_PROC_03)
            {
                description.set_text(description.get_text() + " District: Merchant Quarters 3");
            }
            if (miss.GetMapId() == MissionMapId.GRND_DIS_01_PROC_04)
            {
                description.set_text(description.get_text() + " District: Merchant Quarters 4");
            }
        }
        SetupShort(miss);
    }

    public void SetupShort(Mission miss)
    {
        string str = ((ProcMissionRatingId)miss.missionSave.ratingId).ToLowerString();
        diffIcon.set_sprite(PandoraSingleton<AssetBundleLoader>.Instance.LoadResource<Sprite>("icn_mission_difficulty_" + str, cached: true));
        wyrdstones.set_text(PandoraSingleton<LocalizationManager>.Instance.GetStringById("yield_" + ((WyrdstoneDensityId)miss.missionSave.wyrdDensityId).ToString()));
        treasures.set_text(PandoraSingleton<LocalizationManager>.Instance.GetStringById("yield_" + ((SearchDensityId)miss.missionSave.searchDensityId).ToString()));
        difficulty.set_text(PandoraSingleton<LocalizationManager>.Instance.GetStringById("mission_difficulty_" + str));
    }
}
