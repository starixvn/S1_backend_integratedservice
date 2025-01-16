import { sleep, check, fail } from "k6";
import { useOptions } from "../../data-generator/optionGenerator.js";
import { GetSabaPlusCustId } from "../../data-generator/userGenerator.js";
import SabaPlusApi from "../../api/saba-plus/index.js";

const options = useOptions("placebetTest");

function scenario(config = {}) {
  const custId = GetSabaPlusCustId(config);
  const placeBetResult = SabaPlusApi.placebet(custId)

  const placeBetCheck = check(placeBetResult.response, {
    "[Placebet]Placebet content status is 0": (r) => r?.ErrorCode != null && r.ErrorCode == 0,
  });

  if (!placeBetCheck) {
    console.log('[Placebet]Placebet', placeBetResult.fullRequest)
    fail("[Placebet]Placebet failed")
  }
}

export default { options, scenario };