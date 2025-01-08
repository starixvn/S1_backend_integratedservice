import { check, fail } from "k6";

import { GetSapaSportUserId } from "../../data-generator/UserGenerator.js";
import { useOptions } from "../../data-generator/optionGenerator.js";
import SabaSportApi from '../../api/saba-sport/index.js';

const options = useOptions("placeBetConfirmBetParlayTest", {
  thresholds: {
    http_req_duration: ["p(85)<250", "p(90)<350"],
    http_req_failed: ["rate<0.01"],
  },
});

function scenario() {
  const userId = GetSapaSportUserId();
  const placebetResult = SabaSportApi.placebetParlay(userId);
  const placebetCheck = check(placebetResult.response, {
    "[PlacebetParlay-ConfirmbetParlay]PlacebetParlay content status is 0": (
      r
    ) => r?.status && r.status === "0",
  });

  if (!placebetCheck) {
    console.log('[PlacebetParlay-ConfirmbetParlay]PlacebetParlay', placebetResult.fullRequest);
    fail("[PlacebetParlay-ConfirmbetParlay]PlacebetParlay failed");
  }

  const confirmBetResult = SabaSportApi.confirmbetParlay(placebetResult.request, placebetResult.response);
  const confirmBetCheck = check(confirmBetResult.response, {
    "[PlacebetParlay-ConfirmbetParlay]ConfirmbetParlay content status is 0": (r) => r?.status && r.status === "0",
  });

  if (!confirmBetCheck) {
    console.log('[PlacebetParlay-ConfirmbetParlay]ConfirmbetParlay', confirmBetResult.fullRequest);
    fail("[PlacebetParlay-ConfirmbetParlay]ConfirmbetParlay failed");
  }
}

export default { options, scenario };