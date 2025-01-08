import { vu } from "k6/execution";
import { sleep, check, fail } from "k6";

import { GetSapaSportUserId } from "../../data-generator/userGenerator.js";
import { useOptions } from "../../data-generator/optionGenerator.js";
import SabaSportApi from "../../api/saba-sport/index.js";

const options = useOptions("placebetConfirmbetSettleTest");

function scenario() {
  const userId = GetSapaSportUserId();
  const placebetResult = SabaSportApi.placebet(userId);
  const placebetCheck = check(placebetResult.response, {
    "[Placebet-Confirmbet-Settle]PlaceBet content status is 0": (r) =>
      r?.status && r.status === "0",
  });

  if (!placebetCheck) {
    console.log('[Placebet-Confirmbet-Settle]PlaceBet', placebetResult.fullRequest);
    fail("[Placebet-Confirmbet-Settle]PlaceBet failed");
  }

  const confirmBetResult = SabaSportApi.confirmbet(
    userId,
    placebetResult.response
  );
  const confirmBetCheck = check(confirmBetResult.response, {
    "[Placebet-Confirmbet-Settle]Confirmbet content status is 0": (r) =>
      r?.status && r.status === "0",
  });

  if (!confirmBetCheck) {
    console.log('[Placebet-Confirmbet-Settle]Confirmbet', confirmBetResult.fullRequest);
    fail("[Placebet-Confirmbet-Settle]Confirmbet failed");
  }

  sleep(10);

  const settledResult = SabaSportApi.settle(userId, confirmBetResult.request);
  const settleCheck = check(settledResult.response, {
    "[Placebet-Confirmbet-Settle]Settle content status is 0": (r) => r?.status && r.status === "0",
  });

  if (!settleCheck) {
    console.log('[Placebet-Confirmbet-Settle]Settle', settledResult.fullRequest);
    fail("[Settle]Settle failed");
  }
}

export default { options, scenario };
