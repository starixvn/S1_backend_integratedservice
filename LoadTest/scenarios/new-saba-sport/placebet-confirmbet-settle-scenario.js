import http from "k6/http";
import { sleep, check, fail } from "k6";
import { Trend } from 'k6/metrics';

import { GetSapaSportUserId } from "../../data-generator/userGenerator.js";
import { useOptions } from "../../data-generator/optionGenerator.js";
import SabaSportApi from "../../api/new-saba-sport/index.js";
import {
  GetSingleConfirmBetDetail,
  GetSingleSettleBetDetail,
} from "../../data-generator/betDetailGenerator.js";
import { BetDetailApiUrl } from "../../constant/api.js";

const customTrend = new Trend('placebetConfirmbetSettle_req_duration')

const options = useOptions("placebetConfirmbetSettleTest", {
  scenario: (defaultScenario) => {
    switch (defaultScenario.executor) {
      case 'constant-arrival-rate':
        return {
          preAllocatedVUs: defaultScenario.preAllocatedVUs * 2,
          maxVUs: defaultScenario.maxVUs * 2,
        }
      case 'ramping-arrival-rate':
        return {
          preAllocatedVUs: defaultScenario.preAllocatedVUs * 2,
          maxVUs: defaultScenario.maxVUs * 2,
        }
      default:
        return {}
    }
  },
});

function scenario() {
  const userId = GetSapaSportUserId();
  const placebetResult = SabaSportApi.placebet(userId);

  if (placebetResult.fullRequest.timings.duration > 0) {
    customTrend.add(placebetResult.fullRequest.timings.duration)
  }

  const placebetCheck = check(placebetResult.response, {
    "[Placebet-Confirmbet-Settle]PlaceBet content status is 0": (r) =>
      r?.status && r.status == "0",
  });

  if (!placebetCheck) {
    console.log(
      "[Placebet-Confirmbet-Settle]PlaceBet",
      placebetResult.fullRequest
    );
    fail("[Placebet-Confirmbet-Settle]PlaceBet failed");
  }

  const confirmBetResult = SabaSportApi.confirmbet(
    userId,
    placebetResult.response
  );

  if (confirmBetResult.fullRequest.timings.duration > 0) {
    customTrend.add(confirmBetResult.fullRequest.timings.duration)
  }

  const confirmBetCheck = check(confirmBetResult.response, {
    "[Placebet-Confirmbet-Settle]Confirmbet content status is 0": (r) =>
      r?.status && r.status == "0",
  });

  if (!confirmBetCheck) {
    console.log(
      "[Placebet-Confirmbet-Settle]Confirmbet",
      confirmBetResult.fullRequest
    );
    fail("[Placebet-Confirmbet-Settle]Confirmbet failed");
  }

  const confirmBetBetDetailRequest = GetSingleConfirmBetDetail(
    placebetResult.request,
    confirmBetResult.request
  );

  http.post(BetDetailApiUrl.AddBetDetail, JSON.stringify([confirmBetBetDetailRequest]), {
    headers: { "Content-Type": "application/json" },
  });

  sleep(10);

  const settledResult = SabaSportApi.settle(userId, confirmBetResult.request);

  if (settledResult.fullRequest.timings.duration > 0) {
    customTrend.add(settledResult.fullRequest.timings.duration)
  }
  
  const settleCheck = check(settledResult.response, {
    "[Placebet-Confirmbet-Settle]Settle content status is 0": (r) =>
      r?.status && r.status == "0",
  });

  if (!settleCheck) {
    console.log(
      "[Placebet-Confirmbet-Settle]Settle",
      settledResult.fullRequest
    );
    fail("[Settle]Settle failed");
  }

  const betDetailRequest = GetSingleSettleBetDetail(
    confirmBetBetDetailRequest,
    settledResult.request
  );

  http.post(BetDetailApiUrl.AddBetDetail, JSON.stringify([betDetailRequest]), {
    headers: { "Content-Type": "application/json" },
  });
}

export default { options, scenario };
