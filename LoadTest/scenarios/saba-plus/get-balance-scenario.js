import { sleep, check, fail } from "k6";
import { useOptions } from "../../data-generator/optionGenerator.js";
import { GetSabaPlusCustId } from "../../data-generator/userGenerator.js";
import SabaPlusApi from "../../api/saba-plus/index.js";

const options = useOptions("sabaplusgetBalanceTest", {
  scenario: (defaultScenario) => {
    switch (defaultScenario.executor) {
      case 'constant-arrival-rate': 
        return { rate: defaultScenario.rate * 3 }
      case 'ramping-arrival-rate':
        return { stages: defaultScenario.stages.map(stage => ({...stage, target: stage.target * 3})) }
      default:
        return {}
    }
  }
});

function scenario(config = {}) {
  const custId = GetSabaPlusCustId(config);
  const getbalanceResult = SabaPlusApi.getbalance(custId)
  
  const getbalanceCheck = check(getbalanceResult.response, {
    "[GetBalance]GetBalance content status is 0": (r) => r?.ErrorCode != null && r.ErrorCode == 0,
  });

  if (!getbalanceCheck) {
    console.log('[GetBalance]GetBalance', getbalanceResult.fullRequest)
    fail("[GetBalance]GetBalance failed")
  }
}

export default { options, scenario };