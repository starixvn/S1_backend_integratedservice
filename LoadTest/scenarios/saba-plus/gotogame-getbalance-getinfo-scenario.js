import { check, fail } from "k6";
import { useOptions } from "../../data-generator/optionGenerator.js";
import { GetUser, getTpCustId } from "../../data-generator/userGenerator.js";
import SabaPlusApi from "../../api/saba-plus/index.js";
import { GetGameOption } from "../../data-generator/gameGenerator.js";
import PlayerSiteApi from "../../api/player-site/index.js";

const options = useOptions("playerInfoTest", {
  scenario: (defaultScenario) => {
    switch (defaultScenario.executor) {
      case 'constant-arrival-rate': 
        return { rate: defaultScenario.rate * 3 }
      case 'ramping-arrival-rate':
        return { stages: defaultScenario.stages.map(stage => ({...stage, target: stage.target * 3})) }
      default:
        return {}
    }
  }
});

function scenario() {
  const user = GetUser();
  const gameOption = GetGameOption();

  const loginResult = PlayerSiteApi.login(user.UserName);
  const loginCheck = check(loginResult.response, {
    "[Gotogame-Getbalance-Getinfo]Login content error code is 0": (r) =>
      r?.errorCode != null && r.errorCode == 0,
  });

  if (!loginCheck) {
    console.log('[Gotogame-Getbalance-Getinfo]Login', loginResult.response ?? loginResult.fullRequest);
    fail("[Gotogame-Getbalance-Getinfo]Login failed");
  }

  const gotoGameResult = PlayerSiteApi.openGame({
    tpId: gameOption.TpId,
    gameType: gameOption.TpGameType,
    gameId: gameOption.GameId,
  });

  const gotoGameCheck = check(gotoGameResult.fullRequest, {
    "[Gotogame-Getbalance-Getinfo]GotoGame content error code is 0": (r) =>
      r.status == 200,
  });

  if (!gotoGameCheck) {
    console.log('[Gotogame-Getbalance-Getinfo]GotoGame', gotoGameResult.fullRequest);
    fail("[Gotogame-Getbalance-Getinfo]Login failed");
  }

  const balanceResult = SabaPlusApi.getbalance(getTpCustId(user.CustId));
  const getBalanceCheck = check(balanceResult.response, {
    "[Gotogame-Getbalance-Getinfo]Getbalance content error code is 0": (r) =>
      r?.ErrorCode != null && r.ErrorCode == 0,
  });

  if (!getBalanceCheck) {
    console.log('[Gotogame-Getbalance-Getinfo]Getbalance', balanceResult.response ?? balanceResult.fullRequest);
    fail("[Gotogame-Getbalance-Getinfo]Getbalance failed");
  }

  const playerInfoResult = SabaPlusApi.playerinfo({
    gameId: gameOption.gameId,
    gameType: gameOption.TpGameType.toLowerCase(),
    operatorCustId: getTpCustId(user.CustId)
  });
  
  const playerInfoCheck = check(playerInfoResult.response, {
    "[Gotogame-Getbalance-Getinfo]Getinfo content error code is 0": (r) =>
      r?.ErrorCode != null && r.ErrorCode == 0,
  });

  if (!playerInfoCheck) {
    console.log('[Gotogame-Getbalance-Getinfo]Getinfo', playerInfoResult.fullRequest);
    fail("[Gotogame-Getbalance-Getinfo]Getinfo failed");
  }
}

export default { options, scenario };
