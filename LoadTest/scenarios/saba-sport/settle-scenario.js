import { sleep, check, fail } from "k6";

import { GetSapaSportUserId } from "../../data-generator/userGenerator.js";
import { useOptions } from "../../data-generator/optionGenerator.js";
import SabaSportApi from '../../api/saba-sport/index.js';

const options = useOptions("settleTest");

function scenario() {
  const userId = GetSapaSportUserId();
  
  const placebetResult = SabaSportApi.placebet(userId);
  const placebetCheck = check(placebetResult.response, {
    "[Settle]Placebet content status is 0": (r) => r?.status && r.status == "0",
  });

  if (!placebetCheck) {
    console.log('[Settle]Placebet', placebetResult.fullRequest);
    fail("[Settle]Placebet failed");
  }

  const confirmBetResult = SabaSportApi.confirmbet(userId, placebetResult.response);
  const confirmBetCheck = check(confirmBetResult.response, {
    "[Settle]Confirmbet content status is 0": (r) => r?.status && r.status == "0",
  });

  if (!confirmBetCheck) {
    console.log('[Settle]Confirmbet', confirmBetResult.fullRequest);
    fail("[Settle]Confirmbet failed");
  }

  sleep(60);

  const settledResult = SabaSportApi.settle(userId, confirmBetResult.request);
  const settleCheck = check(settledResult.response, {
    "[Settle]Settle content status is 0": (r) => r?.status && r.status == "0",
  });

  if (!settleCheck) {
    console.log('[Settle]Settle', settledResult.fullRequest);
    fail("[Settle]Settle failed");
  }

  const balanceResult = SabaSportApi.getBalance(userId);
  const balanceCheck = check(balanceResult.response, {
    "[Settle]GetBalance content status is 0": (r) => r?.status && r.status == "0",
  });

  if (!balanceCheck) {
    console.log('[Settle]GetBalance', settledResult.fullRequest);
    fail("[Settle]GetBalance failed");
  }

}

export default { options, scenario };