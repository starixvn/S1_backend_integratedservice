import { sleep, check, fail } from "k6";
import { useOptions } from "../../data-generator/optionGenerator.js";
import { GetSabaPlusCustId } from "../../data-generator/userGenerator.js";
import SabaPlusApi from "../../api/saba-plus/index.js";

const options = useOptions("placebetEndgameTest");

function scenario() {
  const custId = GetSabaPlusCustId();
  const placeBetResult = SabaPlusApi.placebet(custId)

  const placeBetCheck = check(placeBetResult.response, {
    "[Placebet-Endgame]Placebet content status is 0": (r) => r?.ErrorCode != null && r.ErrorCode == 0,
  });

  if (!placeBetCheck) {
    console.log('[Placebet-Endgame]Placebet', placeBetResult.fullRequest)
    fail("[Placebet-Endgame]placebet failed")
  }

  sleep(2);

  const endgameResult = SabaPlusApi.endgame(placeBetResult.request)

  const endGameCheck = check(endgameResult.response, {
    "[Placebet-Endgame]Endgame status is 0": (r) => r?.ErrorCode != null && r.ErrorCode == 0,
  });

  if (!endGameCheck) {
    console.log('[Placebet-Endgame]Endgame', endgameResult.fullRequest)
  }
}

export default { options, scenario };