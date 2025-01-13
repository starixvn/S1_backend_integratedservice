import http from "k6/http";
import { check, fail } from "k6";

import { GetSapaSportUserId } from "../../data-generator/userGenerator.js";
import { useOptions } from "../../data-generator/optionGenerator.js";
import SabaSportApi from "../../api/new-saba-sport/index.js";
import { GetSingleConfirmBetDetail } from "../../data-generator/betDetailGenerator.js";
import { BetDetailApiUrl } from "../../constant/api.js";

const options = useOptions("placebetConfirmbetTest");

function scenario() {
  const userId = GetSapaSportUserId();

  const placebetResult = SabaSportApi.placebet(userId);
  const placebetCheck = check(placebetResult.response, {
    "[Placebet-Confirmbet]Placebet content status is 0": (r) =>
      r?.status && r.status == "0",
  });

  if (!placebetCheck) {
    console.log("[Placebet-Confirmbet]Placebet", placebetResult.fullRequest);
    fail("[Placebet-Confirmbet]Placebet failed");
  }

  const confirmBetResult = SabaSportApi.confirmbet(
    userId,
    placebetResult.response
  );
  const confirmBetCheck = check(confirmBetResult.response, {
    "[Placebet-Confirmbet]Confirmbet content status is 0": (r) =>
      r?.status && r.status == "0",
  });

  if (!confirmBetCheck) {
    console.log(
      "[Placebet-Confirmbet]Confirmbet",
      confirmBetResult.fullRequest
    );
    fail("[Placebet-Confirmbet]PlaceBet failed");
  }

  const betDetailRequest = GetSingleConfirmBetDetail(
    placebetResult.request,
    confirmBetResult.request
  );

  http.post(BetDetailApiUrl.AddBetDetail, JSON.stringify([betDetailRequest]), {
    headers: { "Content-Type": "application/json" },
    tags: {  scenario: 'unknow-ignore'  },
    //responseCallback: (req, res) => {},
  });
}

export default { options, scenario };
