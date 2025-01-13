import http from "k6/http";
import { sleep, check, fail } from "k6";

import { GetSapaSportUserId } from "../../data-generator/userGenerator.js";
import { useOptions } from "../../data-generator/optionGenerator.js";
import SabaSportApi from "../../api/new-saba-sport/index.js";
import {
  GetSingleConfirmBetDetail,
  GetSingleSettleBetDetail,
} from "../../data-generator/betDetailGenerator.js";
import { BetDetailApiUrl } from "../../constant/api.js";

const options = useOptions("settleTest", {
  // scenario: (defaultScenario) => ({
  //   preAllocatedVUs: defaultScenario.preAllocatedVUs * 5,
  //   maxVUs: defaultScenario.maxVUs * 2,
  // }),
});

function scenario() {
  const userId = GetSapaSportUserId();

  const placebetResult = SabaSportApi.placebet(userId);
  const placebetCheck = check(placebetResult.response, {
    "[Settle]Placebet content status is 0": (r) => r?.status && r.status == "0",
  });

  if (!placebetCheck) {
    console.log("[Settle]Placebet", placebetResult.fullRequest);
    fail("[Settle]Placebet failed");
  }

  const confirmBetResult = SabaSportApi.confirmbet(
    userId,
    placebetResult.response
  );
  const confirmBetCheck = check(confirmBetResult.response, {
    "[Settle]Confirmbet content status is 0": (r) =>
      r?.status && r.status == "0",
  });

  if (!confirmBetCheck) {
    console.log("[Settle]Confirmbet", confirmBetResult.fullRequest);
    fail("[Settle]Confirmbet failed");
  }

  const confirmBetBetDetailRequest = GetSingleConfirmBetDetail(
    placebetResult.request,
    confirmBetResult.request
  );

  http.post(
    BetDetailApiUrl.AddBetDetail,
    JSON.stringify([confirmBetBetDetailRequest]),
    {
      headers: { "Content-Type": "application/json" },
      tags: { noMetrics: "true" },
      //responseCallback: (req, res) => {},
    }
  );

  sleep(50);

  const settledResult = SabaSportApi.settle(userId, confirmBetResult.request);
  const settleCheck = check(settledResult.response, {
    "[Settle]Settle content status is 0": (r) => r?.status && r.status == "0",
  });

  if (!settleCheck) {
    console.log("[Settle]Settle", settledResult.fullRequest);
    fail("[Settle]Settle failed");
  }

  const balanceResult = SabaSportApi.getBalance(userId);
  if (
    !check(balanceResult.response, {
      "[Settle]GetBalance content status is 0": (r) =>
        r?.status && r.status == "0",
    })
  ) {
    console.log(balanceResult.fullRequest);
  }

  const betDetailRequest = GetSingleSettleBetDetail(
    confirmBetBetDetailRequest,
    settledResult.request
  );

  http.post(BetDetailApiUrl.AddBetDetail, JSON.stringify([betDetailRequest]), {
    headers: { "Content-Type": "application/json" },
    //tags: { noMetrics: "true" },
    //responseCallback: (req, res) => {},
  });
}

export default { options, scenario };
