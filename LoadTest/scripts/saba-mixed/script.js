import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { extractOptions, formatDateToCustomFormat } from "../../data-generator/optionGenerator.js";
import placebetConfirmbetScenario from "../../scenarios/grafana/saba-sport/placebet-confirmbet-scenario.js";
import placebetConfirmbetSettleScenario from "../../scenarios/grafana/saba-sport/placebet-confirmbet-settle-scenario.js";
import placebetparlayConfirmbetparlayScenario from "../../scenarios/grafana/saba-sport/placebetparlay-confirmbetparlay-scenario.js";
import settleScenario from "../../scenarios/grafana/saba-sport/settle-scenario.js";
import SabaSportApi from "../../api/saba-sport/index.js";
import gotogameGetbalanceGetinfoScenario from "../../scenarios/grafana/saba-plus/gotogame-getbalance-getinfo-scenario..js";
import placebetEndgameScenario from "../../scenarios/grafana/saba-plus/placebet-endgame-scenario..js";
import placebetsettleScenario from "../../scenarios/grafana/saba-plus/placebetsettle-scenario..js";
import freespinScenario from "../../scenarios/grafana/saba-plus/freespin-scenario.js";

export const options = extractOptions(
  placebetConfirmbetScenario.options,
  placebetConfirmbetSettleScenario.options,
  placebetparlayConfirmbetparlayScenario.options,
  settleScenario.options,
  gotogameGetbalanceGetinfoScenario.options,
  placebetEndgameScenario.options,
  placebetsettleScenario.options,
  freespinScenario.options
);

export function setup() {
  const clearLoadTestDataRes = SabaSportApi.clearLoadTestData();
  console.log("Loadtest data cleared");
}

export function handleSummary(data) {
  return {
    [`Saba_Mixed_${__ENV.RUN_ENV ?? 'development'}_${formatDateToCustomFormat()}.html.html`]: htmlReport(data),
  };
}

export const placebetConfirmbetTest = placebetConfirmbetScenario.scenario;
export const placeBetConfirmBetParlayTest = placebetparlayConfirmbetparlayScenario.scenario;
export const placebetConfirmbetSettleTest = placebetConfirmbetSettleScenario.scenario;
export const settleTest = settleScenario.scenario;

export const placebetEndgameTest = placebetEndgameScenario.scenario;
export const placebetAndSettleTest = placebetsettleScenario.scenario;
export const freeSpinTest = freespinScenario.scenario;
export const playerInfoTest = gotogameGetbalanceGetinfoScenario.scenario;
