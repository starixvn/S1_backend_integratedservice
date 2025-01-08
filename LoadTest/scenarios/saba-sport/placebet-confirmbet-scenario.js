import { vu } from 'k6/execution';
import { sleep, check, fail } from "k6";

import { GetSapaSportUserId } from "../../data-generator/userGenerator.js";
import { useOptions } from "../../data-generator/optionGenerator.js";
import SabaSportApi from '../../api/saba-sport/index.js';

const options = useOptions("placebetConfirmbetTest");

function scenario() {
  const userId = GetSapaSportUserId();

  const placebetResult = SabaSportApi.placebet(userId);
  const placebetCheck = check(placebetResult.response, {
    "[Placebet-Confirmbet]Placebet content status is 0": (r) =>
      r?.status && r.status == "0",
  });

  if (!placebetCheck) {
    console.log('[Placebet-Confirmbet]Placebet', placebetResult.fullRequest);
    fail("[Placebet-Confirmbet]Placebet failed");
  }

  const confirmBetResult = SabaSportApi.confirmbet(userId, placebetResult.response);
  const confirmBetCheck = check(confirmBetResult.response, {
    "[Placebet-Confirmbet]Confirmbet content status is 0": (r) => r?.status && r.status == "0",
  });

  if (!confirmBetCheck) {
    console.log('[Placebet-Confirmbet]Confirmbet', confirmBetResult.fullRequest);
    fail("[Placebet-Confirmbet]PlaceBet failed");
  }

  sleep(3);
}

export default { options, scenario };