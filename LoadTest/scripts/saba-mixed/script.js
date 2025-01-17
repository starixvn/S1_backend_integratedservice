import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { extractOptions, formatDateToCustomFormat } from "../../data-generator/optionGenerator.js";
import settleScenario from "../../scenarios/new-saba-sport/settle-scenario.js";
import placebetConfirmbetScenario from "../../scenarios/new-saba-sport/placebet-confirmbet-scenario.js";
import placebetConfirmbetSettleScenario from "../../scenarios/new-saba-sport/placebet-confirmbet-settle-scenario.js";
import placebetparlayConfirmbetparlayScenario from "../../scenarios/new-saba-sport/placebetparlay-confirmbetparlay-scenario.js";
import placebetEndgameScenario from "../../scenarios/saba-plus/placebet-endgame-scenario..js";
import placebetsettleScenario from "../../scenarios/saba-plus/placebetsettle-scenario..js";
import freespinScenario from "../../scenarios/saba-plus/freespin-scenario.js";
import getBalanceScenario from "../../scenarios/saba-plus/get-balance-scenario.js";

const suffix = `${__ENV.RUN_ENV ?? 'development'}_${formatDateToCustomFormat()}`;

export function setup() {
  console.log(`Begin test: Saba_Mixed_${suffix}.html.html`);
}

export const options = extractOptions(
  placebetConfirmbetScenario.options,
  placebetConfirmbetSettleScenario.options,
  placebetparlayConfirmbetparlayScenario.options,
  settleScenario.options,
  getBalanceScenario.options,
  placebetEndgameScenario.options,
  placebetsettleScenario.options,
  freespinScenario.options
);

export function handleSummary(data) {
  return {
    [`Saba_Mixed_${suffix}.html`]: htmlReport(data),
  };
}

export const placebetConfirmbetTest = placebetConfirmbetScenario.scenario;
export const placeBetConfirmBetParlayTest = placebetparlayConfirmbetparlayScenario.scenario;
export const placebetConfirmbetSettleTest = placebetConfirmbetSettleScenario.scenario;
export const settleTest = settleScenario.scenario;

export const placebetEndgameTest = placebetEndgameScenario.scenario;
export const placebetAndSettleTest = placebetsettleScenario.scenario;
export const freeSpinTest = freespinScenario.scenario;
