using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using static Statics;

public class UIManager : MonoBehaviour
{
	private const string TOP_PLAYER_WON_CODE = "top_won_text";
	private const string BOTTOM_PLAYER_WON_CODE = "bottom_won_text";

	private const float SCORE_HIDE_DELAY = 2f;
	private const float RESULT_SCREEN_DELAY = 5f;

	public static GameStates gameState = GameStates.MENU;

	public static UIManager Instance;

	[SerializeField] private GameObject menuScreen, gameScreen;

	[SerializeField] private Text[] tanksScoreText; // TOP, BOTTOM
	[SerializeField] private Text[] tanksLifeText; // TOP, BOTTOM
	[SerializeField] private Text[] tanksSubScoreText; // TOP, BOTTOM
	[SerializeField] private Image[] tanksScoreStar; // TOP, BOTTOM
	[SerializeField] private Text gameResultText;

	[SerializeField] private Image[] topTankBodyParts, bottomTankBodyParts; // BODY, LIGHT, SHADOW, WHEELS_BODY, WHEELS_LIGHT, WHEELS_SHADOW

	private void Awake() {
		if (Instance == null) {
			Instance = this;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetLife(Teams team, int lifeCount) => tanksLifeText[(int)team].text = lifeCount.ToString();

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetScore(Teams team, int score) => tanksScoreText[(int)team].text = score.ToString();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTopPlayerColor(int color) { // Unity can't serialize enum in the UI events
		SkinManager.playersSkin[(int)Teams.TOP] = (PlayerColors)color;
		SkinManager.Instance.SetUITankParts(topTankBodyParts, (PlayerColors)color);
		SkinManager.Instance.SetUIStar(tanksScoreStar[(int)Teams.TOP], (PlayerColors)color);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBottomPlayerColor(int color) { // Unity can't serialize enum in the UI events
		SkinManager.playersSkin[(int)Teams.BOTTOM] = (PlayerColors)color;
		SkinManager.Instance.SetUITankParts(bottomTankBodyParts, (PlayerColors)color);
		SkinManager.Instance.SetUIStar(tanksScoreStar[(int)Teams.BOTTOM], (PlayerColors)color);
	}

	public void SetUI(GameStates state) {
		gameState = state;
		switch (gameState) {
			case GameStates.MENU:
				menuScreen.SetActive(true);
				gameScreen.SetActive(false);
				break;
			case GameStates.GAME:
				menuScreen.SetActive(false);
				gameScreen.SetActive(true);
				break;
			default:
				break;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void AddScoreCoroutine(int score, Teams team) => StartCoroutine("AddScoreRoutine", (score, team));
	private IEnumerator AddScoreRoutine((int score, Teams team) data) {
		int tempID = (int)data.team;
		PlayManager.playersScore[tempID] += data.score;
		SetScore(data.team, PlayManager.playersScore[tempID]);
		tanksSubScoreText[tempID].text = data.score.ToString();
		tanksSubScoreText[tempID].gameObject.SetActive(true);
		yield return new WaitForSeconds(SCORE_HIDE_DELAY);
		tanksSubScoreText[tempID].gameObject.SetActive(false);

		yield break;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void GameOverCoroutine(Teams wonTeam) => StartCoroutine("GameOverRoutine", wonTeam);
	private IEnumerator GameOverRoutine(Teams wonTeam) {
		StopCoroutine("AddScoreRoutine");

		gameResultText.text = wonTeam == Teams.TOP ? LocalizationManager.Instance.Localize(TOP_PLAYER_WON_CODE) : LocalizationManager.Instance.Localize(BOTTOM_PLAYER_WON_CODE);
		gameResultText.gameObject.SetActive(true);
		yield return new WaitForSeconds(RESULT_SCREEN_DELAY);
		gameResultText.gameObject.SetActive(false);
		menuScreen.SetActive(true);
		gameScreen.SetActive(false);

		yield break;
	}

}
