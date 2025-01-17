import { sleep } from "k6";
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { extractOptions, formatDateToCustomFormat } from "../../data-generator/optionGenerator.js";
import placebetEndgameScenario from "../../scenarios/saba-plus/placebet-endgame-scenario..js";
import freespinScenario from "../../scenarios/saba-plus/freespin-scenario.js";
import placebetsettleScenario from "../../scenarios/saba-plus/placebetsettle-scenario..js";
import getBalanceScenario from "../../scenarios/saba-plus/get-balance-scenario.js";

const suffix = `${__ENV.RUN_ENV ?? 'development'}_${formatDateToCustomFormat()}`;

export function setup() {
  console.log(`Begin test: Saba_Plus_${suffix}`);
}

export const options = extractOptions(
  getBalanceScenario.options,
  placebetEndgameScenario.options,
  placebetsettleScenario.options,
  freespinScenario.options
);

export function handleSummary(data) {
  return {
    [`Saba_Plus_${suffix}.html`]: htmlReport(data),
  };
}

export const placebetEndgameTest = placebetEndgameScenario.scenario;
export const placebetAndSettleTest = placebetsettleScenario.scenario;
export const freeSpinTest = freespinScenario.scenario;
export const sabaplusgetBalanceTest = getBalanceScenario.scenario;
