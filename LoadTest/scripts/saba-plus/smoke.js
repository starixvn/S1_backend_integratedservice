import { check, fail, sleep } from "k6";

import { GetSapaSportUserId } from "../../data-generator/userGenerator.js";
import SabaPlusApi from "../../api/saba-plus/index.js";

export let options = {
  vus: 1, // 1 virtual user
  iterations: 5, // Run 1 iterations
  thresholds: {
    http_req_failed: [{ threshold: "rate<0.01", abortOnFail: true }],
    http_req_duration: ["p(95)<250", "p(99)<350"],
  },
};

export default function () {
  const userId = GetSapaSportUserId();

  const getBalanceTest = SabaPlusApi.getbalance(userId);
  console.log('getBalanceTest', getBalanceTest.response)
  sleep(1);

  const placebetTest = SabaPlusApi.placebet(userId);
  console.log('placebetTest', placebetTest.response)
  const placebetCheck = check(placebetTest.response, {
    "PlaceBet content status is 0": (r) => r.status === "0",
  });

  if (!placebetCheck) {
    console.log(placebetTest.response);
    fail("PlaceBet failed");
  }
  sleep(0.5);

  const confirmBetTest = SabaPlusApi.confirmbet(userId, placebetTest.response);
  console.log('confirmBetTest', confirmBetTest.response)
  const confirmBetCheck = check(confirmBetTest.response, {
    "Confirmbet content status is 0": (r) => r.status === "0",
  });

  if (!confirmBetCheck) {
    fail("Confirmbet failed");
  }
  sleep(3);

  const settledTest = SabaPlusApi.settle(userId, confirmBetTest.request);
  console.log('settledTest', settledTest.response)
  const settledCheck = check(settledTest.response, {
    "Settled content status is 0": (r) => r.status === "0",
  });

  if (!settledCheck) {
    fail("Settled failed");
  }
}
