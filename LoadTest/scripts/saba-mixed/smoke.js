import { check, sleep } from "k6";
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";

import { GetSapaSportUserId } from "../../data-generator/userGenerator.js";
import confirmbet from "../../api/saba-sport/confirmbet.js";
import settle from "../../api/saba-sport/settle.js";
import placebet from "../../api/saba-sport/placebet.js";
import getBalance from "../../api/saba-sport/get-balance.js";

export let options = {
  vus: 1, // 1 virtual user
  iterations: 15, // Run 12 iterations
  thresholds: {
    http_req_failed: [{ threshold: "rate<0.01", abortOnFail: true }],
  },
};

export default function () {
  const userId = GetSapaSportUserId();

  const getBalanceTest = getBalance(userId);
  console.log('getBalanceTest', getBalanceTest.response)
  sleep(1);

  const placebetTest = placebet(userId);
  console.log('placebetTest', placebetTest.response)
  const placebetCheck = check(placebetTest.response, {
    "PlaceBet content status is 0": (r) => r.status === "0",
  });

  if (!placebetCheck) {
    fail("PlaceBet failed");
  }

  sleep(0.5);

  const confirmBetTest = confirmbet(userId, placebetTest.response);
  console.log('confirmBetTest', confirmBetTest.response)
  const confirmBetCheck = check(confirmBetTest.response, {
    "Confirmbet content status is 0": (r) => r.status === "0",
  });

  if (!confirmBetCheck) {
    fail("Confirmbet failed");
  }

  sleep(3);

  const settledTest = settle(userId, confirmBetTest.request);
  console.log('settledTest', settledTest.response)
  const settledCheck = check(settledTest.response, {
    "Settled content status is 0": (r) => r.status === "0",
  });

  if (!settledCheck) {
    fail("Settled failed");
  }
  
}

export function handleSummary(data) {
  return {
    [`summary.html`]: htmlReport(data),
  };
}
