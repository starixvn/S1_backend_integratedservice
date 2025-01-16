import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { extractOptions, formatDateToCustomFormat } from "../../data-generator/optionGenerator.js";
import gotogameGetbalanceGetinfoScenario from "../../scenarios/saba-plus/gotogame-getbalance-getinfo-scenario.js";
import placebetEndgameScenario from "../../scenarios/saba-plus/placebet-endgame-scenario..js";
import freespinScenario from "../../scenarios/saba-plus/freespin-scenario.js";
import placebetsettleScenario from "../../scenarios/saba-plus/placebetsettle-scenario..js";
import getBalanceScenario from "../../scenarios/saba-plus/get-balance-scenario.js";
import getinfoScenario from "../../scenarios/saba-plus/getinfo-scenario..js";

const suffix = `${__ENV.RUN_ENV ?? 'development'}_${formatDateToCustomFormat()}`;

export function setup() {
  console.log(`Begin test: SabaPlus_Custom_${suffix}`)
  placebetEndgameScenario.scenario({isSmokeTest: true});
  placebetsettleScenario.scenario({isSmokeTest: true});
  freespinScenario.scenario({isSmokeTest: true});
  getBalanceScenario.scenario({isSmokeTest: true});
  console.log(`Setup test: SabaPlus_Custom_${suffix} Done`)
}

export const options = extractOptions(
  //gotogameGetbalanceGetinfoScenario.options,
  getinfoScenario.options,
  getBalanceScenario.options,
  placebetEndgameScenario.options,
  placebetsettleScenario.options,
  freespinScenario.options,
);

export function handleSummary(data) {
  return {
    [`SabaPlus_Custom_${suffix}.html`]: htmlReport(data),
  };
}

export const getPlayerInfoTest = getinfoScenario.scenario;
export const sabaplusgetBalanceTest = getBalanceScenario.scenario;
export const placebetEndgameTest = placebetEndgameScenario.scenario;
export const placebetAndSettleTest = placebetsettleScenario.scenario;
export const freeSpinTest = freespinScenario.scenario;
export const playerInfoTest = gotogameGetbalanceGetinfoScenario.scenario;

