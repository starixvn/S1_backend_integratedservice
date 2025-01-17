import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { extractOptions, formatDateToCustomFormat } from "../../data-generator/optionGenerator.js";
import placebetConfirmbetScenario from "../../scenarios/new-saba-sport/placebet-confirmbet-scenario.js";
import placebetConfirmbetSettleScenario from "../../scenarios/new-saba-sport/placebet-confirmbet-settle-scenario.js";
import placebetparlayConfirmbetparlayScenario from "../../scenarios/new-saba-sport/placebetparlay-confirmbetparlay-scenario.js";
import settleScenario from "../../scenarios/new-saba-sport/settle-scenario.js";

const suffix = `${__ENV.RUN_ENV ?? 'development'}_${formatDateToCustomFormat()}`;

export function handleSummary(data) {
  return {
    [`SabaSport_${suffix}.html`]: htmlReport(data),
  };
}

export function setup() {
  console.log(`Begin test: SabaSport_Custom_${suffix}`)
}

export const options = extractOptions(
  placebetConfirmbetScenario.options,
  placebetConfirmbetSettleScenario.options,
  placebetparlayConfirmbetparlayScenario.options,
  settleScenario.options
);

export const placebetConfirmbetTest = placebetConfirmbetScenario.scenario;
export const placeBetConfirmBetParlayTest = placebetparlayConfirmbetparlayScenario.scenario;
export const placebetConfirmbetSettleTest = placebetConfirmbetSettleScenario.scenario;
export const settleTest = settleScenario.scenario;