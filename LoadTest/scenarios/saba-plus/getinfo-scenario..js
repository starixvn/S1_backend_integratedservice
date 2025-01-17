import { check, fail } from "k6";
import { useOptions } from "../../data-generator/optionGenerator.js";
import { GetSabaPlusCustId } from "../../data-generator/userGenerator.js";
import SabaPlusApi from "../../api/saba-plus/index.js";
import { GetGameOption } from "../../data-generator/gameGenerator.js";

const options = useOptions("getPlayerInfoTest", {
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

function scenario(config = {}) {
  const custId = GetSabaPlusCustId(config);
  const gameOption = GetGameOption();

  const playerInfoResult = SabaPlusApi.playerinfo({
    gameId: gameOption.gameId,
    gameType: gameOption.TpGameType.toLowerCase(),
    operatorCustId: custId
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
