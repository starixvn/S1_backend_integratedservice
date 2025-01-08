import {  check } from "k6";
import { useOptions } from "../../data-generator/optionGenerator.js";
import { GetSabaPlusCustId } from "../../data-generator/userGenerator.js";
import SabaPlusApi from "../../api/saba-plus/index.js";

const options = useOptions("placebetAndSettleTest");

function scenario() {
  const custId = GetSabaPlusCustId();
  const placeBetResult = SabaPlusApi.placebetAndSettle(custId)

  const placebetCheck = check(placeBetResult.response, {
    "[PlacebetAndSettle]Placebet error code is 0": (r) => r?.ErrorCode != null && r.ErrorCode == 0,
  });

  if (!placebetCheck) {
    console.log('[PlacebetAndSettle]Placebet', placeBetResult.fullRequest)
  }
}

export default { options, scenario };