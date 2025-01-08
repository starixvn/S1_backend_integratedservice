import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { extractOptions, formatDateToCustomFormat } from "../../data-generator/optionGenerator.js";
import placebetConfirmbetScenario from "../../scenarios/saba-sport/placebet-confirmbet-scenario.js";
import placebetConfirmbetSettleScenario from "../../scenarios/saba-sport/placebet-confirmbet-settle-scenario.js";
import placebetparlayConfirmbetparlayScenario from "../../scenarios/saba-sport/placebetparlay-confirmbetparlay-scenario.js";
import settleScenario from "../../scenarios/saba-sport/settle-scenario.js";
import SabaSportApi from "../../api/saba-sport/index.js";

export const options = extractOptions(
  // placebetConfirmbetScenario.options,
  // placebetConfirmbetSettleScenario.options,
  // placebetparlayConfirmbetparlayScenario.options,
  settleScenario.options
);

export function setup() {
  const clearLoadTestDataRes = SabaSportApi.clearLoadTestData();
  console.log("Loadtest data cleared")
}

export function handleSummary(data) {
  return {
    [`Saba_Sport_${__ENV.RUN_ENV ?? 'development'}_${formatDateToCustomFormat()}.html`]: htmlReport(data),
  };
}

export const placebetConfirmbetTest = placebetConfirmbetScenario.scenario;
export const placeBetConfirmBetParlayTest = placebetparlayConfirmbetparlayScenario.scenario;
export const placebetConfirmbetSettleTest = placebetConfirmbetSettleScenario.scenario;
export const settleTest = settleScenario.scenario;