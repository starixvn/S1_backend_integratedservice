import { check, fail } from "k6";
import { useOptions } from "../../data-generator/optionGenerator.js";
import { GetSabaPlusCustId } from "../../data-generator/userGenerator.js";
import SabaPlusApi from "../../api/saba-plus/index.js";

const options = useOptions("freeSpinTest");

function scenario(config = {}) {
  const custId = GetSabaPlusCustId(config);
  const placeBetResult = SabaPlusApi.freespin(custId)

  const placeBetCheck = check(placeBetResult.response, {
    "[FreeSpin]FreeSpin error code is 0": (r) => r?.ErrorCode != null && r.ErrorCode == 0,
  });

  if (!placeBetCheck) {
    console.log('[FreeSpin]FreeSpin', placeBetResult.fullRequest)
    fail("[FreeSpin]FreeSpin failed")
  }
}

export default { options, scenario };