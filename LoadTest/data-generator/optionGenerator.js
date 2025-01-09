import { SabaMixedScenarios } from "../constant/SabaMixedScenarios.js";
import { SabaPlusScenarios } from "../constant/SabaPlusScenarios.js";
import { SabaSportScenarios } from "../constant/SabaSportScenarios.js";

export const useOptions = (execName, options = {}) => {
  const scenarios = useScenarios(execName, options);
  const thresholdsResult = {};

  const { thresholds } = options;

  if (thresholds) {
    Object.keys(scenarios).forEach((scenario) => {
      Object.keys(thresholds).forEach((threshold) => {
        thresholdsResult[`${threshold}{scenario:${scenario}}`] =
          thresholds[threshold];
      });
    });
  } else {
    // default threshold
    Object.keys(scenarios).forEach((scenario) => {
      thresholdsResult[`http_req_duration{scenario:${scenario}}`] = [
        "p(85)<250",
        "p(90)<350",
      ];
      thresholdsResult[`http_req_failed{scenario:${scenario}}`] = ["rate<0.01"];
    });
  }

  return {
    scenarios: scenarios,
    thresholds: thresholdsResult,
  };
};

const Scenarios = {
  SabaPlus: SabaPlusScenarios,
  SabaSport: SabaSportScenarios,
  SabaMixed: SabaMixedScenarios
}

export const useScenarios = (execName, options = {}) => {
  const defaultScenarios = Scenarios[__ENV.OPTIONS] ?? Scenarios.SabaSport;
  const scenarios = defaultScenarios[__ENV.RUN_MODE] ?? defaultScenarios.local;
  const result = { ...scenarios };

  let getCustomScenario = options.scenario;
  if (!getCustomScenario) {
    getCustomScenario = () => {};
  }

  Object.keys(result).forEach((testType) => {
    const newTestType = `${execName}-${testType}`;

    result[newTestType] = { ...result[testType], ...getCustomScenario(result[testType]) };
    result[newTestType]["exec"] = execName;

    delete result[testType];
  });

  return result;
};

export const extractOptions = (...optionObjects) => {
  const mergedOptions = optionObjects.reduce(
    (acc, option) => {
      acc.scenarios = { ...acc.scenarios, ...option.scenarios };
      acc.thresholds = { ...acc.thresholds, ...option.thresholds };
      return acc;
    },
    { scenarios: {}, thresholds: {} }
  );

  return mergedOptions;
};


export function formatDateToCustomFormat() {
  const date = new Date();
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0'); 
  const day = String(date.getDate()).padStart(2, '0');
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  const seconds = String(date.getSeconds()).padStart(2, '0');

  return `${year}${month}${day}_${hours}_${minutes}_${seconds}`;
}