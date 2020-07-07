using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using static Statics;

public class SkinManager : MonoBehaviour
{

	public static SkinManager Instance;

	public static PlayerColors[] playersSkin = new PlayerColors[(int)Teams.COUNT];
	public static BotColors[] botsSkin = new BotColors[(int)Teams.COUNT];

	[SerializeField] private Sprite[] bonusSprites;

	// From lower level to upper
	[SerializeField] private Sprite[] playerBodySprites;
	[SerializeField] private Sprite[] botBodySprites;
	[SerializeField] private Sprite[] playerLightSprites;
	[SerializeField] private Sprite[] botLightSprites;
	[SerializeField] private Sprite[] playerShadowSprites;
	[SerializeField] private Sprite[] botShadowSprites;
	[SerializeField] private Sprite[] playerWheelBodySprites;
	[SerializeField] private Sprite[] botWheelBodySprites;
	[SerializeField] private Sprite[] playerWheelLightSprites;
	[SerializeField] private Sprite[] botWheelLightSprites;
	[SerializeField] private Sprite[] playerWheelShadowSprites;
	[SerializeField] private Sprite[] botWheelShadowSprites;

	// Every sprite will inherit colors
	[SerializeField] private Color[] playerBodyColors;
	[SerializeField] private Color[] botBodyColors;
	[SerializeField] private Color[] playerLightColors;
	[SerializeField] private Color[] botLightColors;
	[SerializeField] private Color[] playerShadowColors;
	[SerializeField] private Color[] botShadowColors;

	private void Awake() {
		if (Instance == null) {
			Instance = this;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Sprite GetBonusSprite(BonusTypes bonusType) => bonusSprites[(int)bonusType];

	public void SetPlayerSkin(ref TankComponent tankComponent, ref PlayerComponent playerComponent, PlayerColors color) {
		tankComponent.TankParts[(int)TankParts.BODY].sprite = playerBodySprites[(int)tankComponent.TankLevel];
		tankComponent.TankParts[(int)TankParts.LIGHT].sprite = playerLightSprites[(int)tankComponent.TankLevel];
		tankComponent.TankParts[(int)TankParts.SHADOW].sprite = playerShadowSprites[(int)tankComponent.TankLevel];
		tankComponent.TankParts[(int)TankParts.WHEEL_BODY].sprite = playerWheelBodySprites[(int)tankComponent.TankLevel];
		tankComponent.TankParts[(int)TankParts.WHEEL_LIGHT].sprite = playerWheelLightSprites[(int)tankComponent.TankLevel];
		tankComponent.TankParts[(int)TankParts.WHEEL_SHADOW].sprite = playerWheelShadowSprites[(int)tankComponent.TankLevel];
		tankComponent.TankParts[(int)TankParts.BODY].color = playerBodyColors[(int)color];
		tankComponent.TankParts[(int)TankParts.LIGHT].color = playerLightColors[(int)color];
		tankComponent.TankParts[(int)TankParts.SHADOW].color = playerShadowColors[(int)color];
		tankComponent.TankParts[(int)TankParts.WHEEL_BODY].color = playerBodyColors[(int)color];
		tankComponent.TankParts[(int)TankParts.WHEEL_LIGHT].color = playerLightColors[(int)color];
		tankComponent.TankParts[(int)TankParts.WHEEL_SHADOW].color = playerShadowColors[(int)color];
		playerComponent.ShieldRenderer.color = playerBodyColors[(int)color];
	}

	public void SetBotSkin(ref TankComponent tank, BotColors color) {
		tank.TankParts[(int)TankParts.BODY].sprite = botBodySprites[(int)tank.TankLevel];
		tank.TankParts[(int)TankParts.LIGHT].sprite = botLightSprites[(int)tank.TankLevel];
		tank.TankParts[(int)TankParts.SHADOW].sprite = botShadowSprites[(int)tank.TankLevel];
		tank.TankParts[(int)TankParts.WHEEL_BODY].sprite = botWheelBodySprites[(int)tank.TankLevel];
		tank.TankParts[(int)TankParts.WHEEL_LIGHT].sprite = botWheelLightSprites[(int)tank.TankLevel];
		tank.TankParts[(int)TankParts.WHEEL_SHADOW].sprite = botWheelShadowSprites[(int)tank.TankLevel];
		tank.TankParts[(int)TankParts.BODY].color = botBodyColors[(int)color];
		tank.TankParts[(int)TankParts.LIGHT].color = botLightColors[(int)color];
		tank.TankParts[(int)TankParts.SHADOW].color = botShadowColors[(int)color];
		tank.TankParts[(int)TankParts.WHEEL_BODY].color = botBodyColors[(int)color];
		tank.TankParts[(int)TankParts.WHEEL_LIGHT].color = botLightColors[(int)color];
		tank.TankParts[(int)TankParts.WHEEL_SHADOW].color = botShadowColors[(int)color];
	}

	public void SetUITankParts(Image[] tankParts, PlayerColors color) {
		tankParts[(int)TankParts.BODY].color = playerBodyColors[(int)color];
		tankParts[(int)TankParts.LIGHT].color = playerLightColors[(int)color];
		tankParts[(int)TankParts.SHADOW].color = playerShadowColors[(int)color];
		tankParts[(int)TankParts.WHEEL_BODY].color = playerBodyColors[(int)color];
		tankParts[(int)TankParts.WHEEL_LIGHT].color = playerLightColors[(int)color];
		tankParts[(int)TankParts.WHEEL_SHADOW].color = playerShadowColors[(int)color];
	}

	public void SetUIStar(Image star, PlayerColors color) {
		star.color = playerBodyColors[(int)color];
	}

}
